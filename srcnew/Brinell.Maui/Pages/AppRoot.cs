using Brinell.Maui.Calls;
using Brinell.Core.Interfaces;

namespace Brinell.Maui.Pages;

/// <summary>
/// The whole app as a scope, for the controls that do not live inside a page.
/// </summary>
public sealed class AppRoot : ObjectBase, IMauiScope<AppRoot>
{
    private readonly IMauiTestContext _context;

    /// <summary>Creates the root scope for a session.</summary>
    /// <param name="context">The test context, which is the driver root.</param>
    public AppRoot(IMauiTestContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public override IMauiTestContext Context => _context;

    /// <inheritdoc />
    public AppRoot Self => this;

    /// <summary>
    /// None: this scope is not a page and is not in one.
    /// </summary>
    public IMauiPage? Page => null;

    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => _context.DefaultLocatorStrategy;

    /// <inheritdoc />
    public ScopeReadiness ProbeReadiness() => _context.ProbeReadiness() with { ScopeName = nameof(AppRoot) };

    /// <inheritdoc cref="IMauiElementScope.IsReady"/>
    public bool IsReady() => ProbeReadiness().IsReady;

    /// <inheritdoc />
    public bool WaitReady(int? timeoutMs = null)
    {
        var budget = timeoutMs ?? DefaultTimeoutMs;
        var context = new AttemptContext(Deadline.In(budget), Context.Timeouts.Animation);

        return Poller.Until(
            _ => ScopeGate.Check(ProbeReadiness()) ?? Observation.Done(),
            context,
            PollingIntervalMs);
    }

    /// <inheritdoc />
    public bool AllowsScrollLookup => false;

    /// <inheritdoc />
    public IMauiElement? TryFindElement(Locator locator) => _context.TryFindElement(locator);

    /// <inheritdoc />
    public IMauiElement FindElement(Locator locator) => _context.FindElement(locator);

    /// <inheritdoc />
    public ElementNotFoundException DescribeMiss(Locator locator) => _context.DescribeMiss(locator);

    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator)
        => _context.FindElements(locator);
}
