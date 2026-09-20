using Brinell.Maui.Controls;
using Brinell.Maui.Calls;
using Brinell.Maui.Containers;

namespace Brinell.Maui.Pages;

/// <summary>
/// Base class for MAUI page objects with fluent method chaining support.
/// Uses CRTP (Curiously Recurring Template Pattern) for strongly-typed fluent returns.
/// Pages are root containers: their root is found from the driver and all child controls
/// resolve strictly within that root.
/// </summary>
/// <typeparam name="TSelf">The concrete page type (CRTP pattern).</typeparam>
[Brinell.Core.Composition.TestPage]
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
    
    #region Page state
    
    /// <inheritdoc />
    public virtual string Name => GetType().Name;

    protected override Locator Locator => new(LocatorStrategy.AutomationId, Name);

    public override IMauiPage? Page => this;

    protected override IMauiElement FindContainerRootElement()
        => Context.FindElements(Locator).FirstOrDefault(element => element.HasUsableBounds())
            ?? throw RootNotFound();

    /// <inheritdoc />
    protected override ElementNotFoundException RootNotFound()
        => new($"Page root not found. Locator: {Locator}");

    protected override bool IsCachedRootValid(IMauiElement root)
        => root.HasUsableBounds();

    protected override TSelf SetResult => Self;

    /// <summary>How this page exposes its page-local busy state.</summary>
    protected virtual BusySignalPolicy BusySignalPolicy => BusySignalPolicy.Disabled;

    /// <summary>The AutomationId of the page-local busy signal.</summary>
    protected virtual string BusyAutomationId => DefaultBusyAutomationId;

    /// <inheritdoc />
    protected override string ScopeName => Name;

    /// <inheritdoc />
    public virtual bool IsLoaded()
        => TryGetContainerRoot() is { } root && root.HasUsableBounds();

    /// <inheritdoc />
    protected override ScopeReadiness ProbeContentReadiness(IMauiElement root)
    {
        if (!IsLoaded())
            return new ScopeReadiness(Name, ScopeReadinessState.NotLoaded);

        if (BusySignalPolicy == BusySignalPolicy.Disabled)
            return ContentReady();

        var signal = root.TryFindElement(Locator.ByAutomationId(BusyAutomationId));
        if (signal == null)
            return new ScopeReadiness(
                Name,
                ScopeReadinessState.MissingBusySignal,
                $"Page '{Name}' requires a page-local busy signal with AutomationId:{BusyAutomationId}, but it was not found beneath the current page root.");

        var value = signal.Text;
        if (!bool.TryParse(value, out var busy))
            return new ScopeReadiness(
                Name,
                ScopeReadinessState.InvalidBusySignal,
                $"Page '{Name}' busy signal AutomationId:{BusyAutomationId} must contain 'True' or 'False', but contained '{value ?? "(null)"}'.");

        return busy
            ? new ScopeReadiness(Name, ScopeReadinessState.Busy, $"busy value: '{value}'")
            : ContentReady();
    }

    /// <inheritdoc />
    protected override ScopeReadiness ProbeCallReadiness() => ProbeReadiness();

    /// <inheritdoc />
    protected override int DefaultReadyTimeoutMs => Context.Timeouts.PageLoad;

    /// <inheritdoc />
    public bool IsBusy()
    {
        var readiness = ProbeReadiness();
        if (readiness.IsConfigurationError)
            throw ScopeGate.Misconfigured(readiness);

        return readiness.IsBusy;
    }

    /// <inheritdoc />
    public bool WaitBusy(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        return RunProbe(() =>
        {
            var readiness = ProbeReadiness();
            if (readiness.IsConfigurationError)
                throw ScopeGate.Misconfigured(readiness);

            return readiness.IsLoaded && readiness.IsBusy == expected.Value
                ? Observation.Done()
                : Observation.ScopeNotReady(readiness);
        }, timeoutMs ?? Context.Timeouts.PageLoad);
    }

    /// <summary>Waits until the loaded page is not busy: <see cref="WaitReady"/>.</summary>
    public bool WaitIdle(int? timeoutMs = null) => WaitReady(timeoutMs);

    /// <summary>
    /// Asserts that the page becomes ready: loaded and not busy.
    /// </summary>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <exception cref="ScopeNotReadyException">The page did not become ready within the timeout.</exception>
    public void AssertIdle(string? message = null, int? timeoutMs = null)
    {
        var budget = timeoutMs ?? Context.Timeouts.PageLoad;
        RunProbe(
            () => ScopeGate.Check(ProbeReadiness()) ?? Observation.Done(),
            budget,
            readiness => new ScopeNotReadyException(
                readiness,
                message ?? $"Page '{Name}' did not become ready within {budget} ms. Last readiness state: {readiness.State}" +
                    (readiness.Detail is null ? "." : $" ({readiness.Detail}).")));
    }

    /// <inheritdoc />
    public bool WaitLoaded(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        return RunProbe(
            () => IsLoaded() == expected.Value ? Observation.Done() : Observation.Pending(),
            timeoutMs ?? Context.Timeouts.PageLoad);
    }

    /// <inheritdoc />
    /// <exception cref="ScopeNotReadyException">The loaded state did not match within the timeout.</exception>
    public void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return;

        RunProbe(
            () => IsLoaded() == expected.Value ? Observation.Done() : Observation.Pending(),
            timeoutMs ?? Context.Timeouts.PageLoad,
            readiness => new ScopeNotReadyException(
                readiness,
                message ?? $"Expected page '{Name}' {(expected.Value ? "to be loaded" : "not to be loaded")} but it is {(expected.Value ? "not" : "still loaded")}. Last readiness state: {readiness.State}."));
    }

    /// <inheritdoc />
    public virtual string? GetTitle()
    {
        // Default implementation returns page name
        // Override for platforms that support page titles
        return Name;
    }

    /// <inheritdoc />
    public bool WaitTitle(string? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        return RunProbe(
            () => GetTitle() == expected ? Observation.Done() : Observation.Pending(),
            timeoutMs ?? Context.Timeouts.DefaultWait);
    }

    /// <inheritdoc />
    /// <exception cref="AssertionException">The title did not match within the timeout.</exception>
    public void AssertTitle(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return;

        RunProbe(
            () => GetTitle() == expected ? Observation.Done() : Observation.Pending(),
            timeoutMs ?? Context.Timeouts.DefaultWait,
            _ =>
            {
                var actual = GetTitle();
                return new AssertionException(
                    message ?? $"Expected page title '{expected}' but got '{actual ?? "(null)"}'.", expected, actual);
            });
    }

    /// <inheritdoc />
    public void TakeScreenshot(string? filename = null)
    {
        var path = filename ?? $"{Name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        Context.SaveScreenshot(path);
    }

    #endregion
}
