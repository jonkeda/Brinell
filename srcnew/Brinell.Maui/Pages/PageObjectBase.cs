using Brinell.Maui.Controls;
using Brinell.Maui.Containers;

namespace Brinell.Maui.Pages;

/// <summary>
/// Base class for MAUI page objects with fluent method chaining support.
/// Uses CRTP (Curiously Recurring Template Pattern) for strongly-typed fluent returns.
/// Pages are root containers: their root is found from the driver and all child controls
/// resolve strictly within that root.
/// </summary>
/// <typeparam name="TSelf">The concrete page type (CRTP pattern).</typeparam>
public abstract class PageObjectBase<TSelf> : RootedScopeBase<TSelf, TSelf>, IMauiPage<TSelf>
    where TSelf : PageObjectBase<TSelf>
{
    private readonly IMauiTestContext _context;
    private const string DefaultBusyAutomationId = "Busy";
    
    /// <summary>
    /// Creates a new page object with the specified context.
    /// </summary>
    /// <param name="context">The MAUI test context.</param>
    protected PageObjectBase(IMauiTestContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public override IMauiTestContext Context => _context;
    
    #region IPageObject Implementation
    
    /// <inheritdoc />
    public virtual string Name => GetType().Name;

    protected override Locator Locator => new(LocatorStrategy.AutomationId, Name);

    public override IPageObject? Page => this;

    protected override IMauiElement FindContainerRootElement()
        => Context.FindElements(Locator).FirstOrDefault(element => element.HasUsableBounds())
            ?? throw new ElementNotFoundException($"Page root not found. Locator: {Locator}");

    protected override bool IsCachedRootValid(IMauiElement root)
        => root.HasUsableBounds();

    protected override TSelf SetResult => Self;

    /// <summary>How this page exposes its page-local busy state.</summary>
    protected virtual BusySignalPolicy BusySignalPolicy => BusySignalPolicy.Disabled;

    /// <summary>The AutomationId of the page-local busy signal.</summary>
    protected virtual string BusyAutomationId => DefaultBusyAutomationId;

    private bool _ensuringLoad;

    private bool EnsureLoaded(bool wait = false)
    {
        if (_ensuringLoad) return true;

        try
        {
            _ensuringLoad = true;
            return wait
                ? IsLoaded() || Poll(() => IsLoaded(), Context.Timeouts.PageLoad)
                : IsLoaded();
        }
        finally
        {
            _ensuringLoad = false;
        }
    }

    protected override bool CanResolveElements(bool wait = false)
        => EnsureLoaded(wait);

    protected override ElementNotFoundException CreateScopeNotReadyException(Locator locator)
        => new($"Page '{Name}' is not loaded, so '{locator}' cannot be found in it. " +
               $"The page root is located by AutomationId:{Name}.");
    /// <inheritdoc />
    public virtual bool IsLoaded(int? timeoutMs = null)
        => IsVisiblePageRootLoaded();

    private bool IsVisiblePageRootLoaded()
        => TryGetContainerRoot() is { } root && root.HasUsableBounds();

    /// <inheritdoc />
    public PageReadinessSnapshot ProbeReadiness() => ProbeReadinessCore(rootReacquired: false);

    private PageReadinessSnapshot ProbeReadinessCore(bool rootReacquired)
    {
        var root = TryGetContainerRoot();
        if (root == null)
            return Snapshot(
                rootReacquired ? PageReadinessState.StaleRoot : PageReadinessState.MissingRoot,
                rootReacquired: rootReacquired);

        if (BusySignalPolicy == BusySignalPolicy.Disabled)
            return Snapshot(PageReadinessState.Ready, rootReacquired: rootReacquired);

        IMauiElement? signal;
        try
        {
            signal = root.FindElement(Locator.ByAutomationId(BusyAutomationId), timeoutMs: 0);
        }
        catch (ElementNotFoundException)
        {
            return Snapshot(PageReadinessState.MissingBusySignal, rootReacquired: rootReacquired);
        }
        catch (StaleElementReferenceException)
        {
            if (rootReacquired)
                return Snapshot(PageReadinessState.StaleRoot, rootReacquired: true);

            InvalidateCache();
            return ProbeReadinessCore(rootReacquired: true);
        }

        string? value;
        try
        {
            value = signal.Text;
        }
        catch (StaleElementReferenceException)
        {
            if (rootReacquired)
                return Snapshot(PageReadinessState.StaleRoot, rootReacquired: true);

            InvalidateCache();
            return ProbeReadinessCore(rootReacquired: true);
        }

        return bool.TryParse(value, out var busy)
            ? Snapshot(
                busy ? PageReadinessState.Busy : PageReadinessState.Ready,
                value,
                rootReacquired)
            : Snapshot(PageReadinessState.InvalidBusySignal, value, rootReacquired);
    }

    /// <inheritdoc />
    public bool IsBusy()
    {
        var snapshot = ProbeReadiness();
        ThrowIfInvalidReadiness(snapshot);
        return snapshot.IsBusy;
    }

    /// <inheritdoc />
    public bool WaitBusy(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        return Poll(() =>
        {
            var snapshot = ProbeReadiness();
            ThrowIfInvalidReadiness(snapshot);
            return snapshot.IsLoaded && snapshot.IsBusy == expected.Value;
        }, timeoutMs ?? Context.Timeouts.PageLoad);
    }

    /// <summary>Migration alias for waiting until the loaded page is not busy.</summary>
    public bool WaitIdle(int? timeoutMs = null) => WaitReady(timeoutMs);

    /// <summary>
    /// Asserts that the page is idle.
    /// </summary>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <exception cref="PageLoadException">Thrown when page does not become idle within timeout.</exception>
    public void AssertIdle(string? message = null, int? timeoutMs = null)
    {
        if (!WaitIdle(timeoutMs))
        {
            var snapshot = ProbeReadiness();
            throw new PageLoadException(
                message ?? $"Page '{Name}' did not become ready within timeout. Last readiness state: {snapshot.State}; busy value: '{snapshot.BusySignalValue ?? "(none)"}'.");
        }
    }

    private PageReadinessSnapshot Snapshot(
        PageReadinessState state,
        string? value = null,
        bool rootReacquired = false)
        => new(Name, state, BusySignalPolicy, value, rootReacquired);

    private void ThrowIfInvalidReadiness(PageReadinessSnapshot snapshot)
    {
        if (snapshot.State == PageReadinessState.MissingBusySignal)
            throw new PageLoadException(
                $"Page '{Name}' requires a page-local busy signal with AutomationId:{BusyAutomationId}, but it was not found beneath the current page root.");

        if (snapshot.State == PageReadinessState.InvalidBusySignal)
            throw new PageLoadException(
                $"Page '{Name}' busy signal AutomationId:{BusyAutomationId} must contain 'True' or 'False', but contained '{snapshot.BusySignalValue ?? "(null)"}'.");
    }
    
    /// <inheritdoc />
    public bool WaitLoaded(bool? expected, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return true;
        
        var timeout = timeoutMs ?? Context.Timeouts.PageLoad;
        return Poll(
            () => IsLoaded() == expected.Value,
            timeout);
    }
    
    /// <inheritdoc />
    public void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return;
        
        if (!WaitLoaded(expected, timeoutMs))
        {
            var actual = IsLoaded();
            throw new PageLoadException(
                message ?? $"Expected page '{Name}' {(expected.Value ? "to be loaded" : "not to be loaded")} but loaded state is {actual}.");
        }
    }
    
    /// <inheritdoc />
    public virtual string? GetTitle(int? timeoutMs = null)
    {
        // Default implementation returns page name
        // Override for platforms that support page titles
        return Name;
    }
    
    /// <inheritdoc />
    public bool WaitTitle(string? expected, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return true;
        
        var timeout = timeoutMs ?? Context.Timeouts.DefaultWait;
        return Poll(
            () => GetTitle() == expected,
            timeout);
    }
    
    /// <inheritdoc />
    public void AssertTitle(string? expected, string? message = null, int? timeoutMs = null)
    {
        // Nullable skip pattern
        if (expected == null) return;
        
        if (!WaitTitle(expected, timeoutMs))
        {
            var actual = GetTitle();
            throw new PageLoadException(
                message ?? $"Expected page title '{expected}' but got '{actual ?? "(null)"}'.");
        }
    }
    
    /// <inheritdoc />
    public void TakeScreenshot(string? filename = null, int? timeoutMs = null)
    {
        var path = filename ?? $"{Name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        Context.SaveScreenshot(path);
    }
    
    #endregion
    
    #region IMauiElementScope Implementation
    
    /// <inheritdoc />
    public override bool IsReady(int? timeoutMs = null)
        => ProbeReadiness().IsReady;
    
    /// <inheritdoc />
    public override bool WaitReady(int? timeoutMs = null)
        => Poll(() =>
        {
            var snapshot = ProbeReadiness();
            ThrowIfInvalidReadiness(snapshot);
            return snapshot.IsReady;
        }, timeoutMs ?? Context.Timeouts.PageLoad);
    
    #endregion
}
