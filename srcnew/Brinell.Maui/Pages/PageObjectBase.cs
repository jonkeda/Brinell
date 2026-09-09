using Brinell.Maui.Controls;
using Brinell.Maui.Controls.Display;
using Brinell.Maui.Containers;
using Brinell.Maui.Scopes;

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
    private readonly IMauiScope<TSelf> _driverRootScope;
    
    /// <summary>
    /// Creates a new page object with the specified context.
    /// </summary>
    /// <param name="context">The MAUI test context.</param>
    protected PageObjectBase(IMauiTestContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _driverRootScope = new DriverRootScope<TSelf>(this);
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
    
    /// <inheritdoc />
    public Label<TSelf> BusySentinel => new (_driverRootScope, "UITest_IsBusy");

    /// <summary>
    /// Whether child resolution requires this page root to be loaded first.
    /// </summary>
    protected virtual bool RequiresLoadedPage => true;

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
        => !RequiresLoadedPage || EnsureLoaded(wait);

    protected override ElementNotFoundException CreateScopeNotReadyException(Locator locator)
        => new($"Page '{Name}' is not loaded, so '{locator}' cannot be found in it. " +
               $"The page root is located by AutomationId:{Name}.");
    /// <inheritdoc />
    public virtual bool IsLoaded(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? 0;
        return timeout > 0
            ? Poll(IsVisiblePageRootLoaded, timeout)
            : IsVisiblePageRootLoaded();
    }

    private bool IsVisiblePageRootLoaded()
        => TryGetContainerRoot() is { } root && root.HasUsableBounds();

    /// <summary>
    /// Waits for the page to finish loading.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True when page becomes idle; otherwise false.</returns>
    public bool WaitIdle(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? Context.Timeouts.PageLoad;
        return Poll(() => BusySentinel.GetText() == "False", timeout);
    }

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
            var actual = BusySentinel.GetText();
            throw new PageLoadException(
                message ?? $"Page '{Name}' did not become idle within timeout. UITest_IsBusy text: '{actual ?? "(not found)"}'.");
        }
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
    {
        // For pages, ready means loaded
        return IsLoaded(timeoutMs);
    }
    
    /// <inheritdoc />
    public override bool WaitReady(int? timeoutMs = null)
    {
        // For pages, wait ready means wait loaded
        return WaitLoaded(true, timeoutMs);
    }
    
    #endregion
}
