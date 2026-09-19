using Brinell.Core.Interfaces;

namespace Brinell.Maui.Pages;

/// <summary>
/// The whole app as a scope, for the controls that do not live inside a page.
/// </summary>
/// <remarks>
/// <para>
/// Use this scope for controls outside every page root: a MAUI <c>ToolbarItem</c>, menu bars,
/// Shell's flyout, platform date pickers and the buttons on a native alert. A control scoped to a
/// page object cannot be resolved while that page is not loaded, which for a back button is
/// exactly when it is needed.
/// </para>
/// <example>
/// <code>
/// private readonly AppRoot _appRoot = new(context);
///
/// public ToolbarButton&lt;AppRoot&gt; BackToHub
///     =&gt; new(_appRoot, Locator.ByAccessibilityId("BackToHub"));
/// </code>
/// </example>
/// </remarks>
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
    /// <remarks>
    /// Because there is no page, page readiness checks do not apply to controls in this scope.
    /// </remarks>
    public IMauiPage? Page => null;

    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => _context.DefaultLocatorStrategy;

    /// <inheritdoc />
    /// <remarks>
    /// Ready whenever the session is: the app root has nothing of its own to wait for yet. An
    /// app-level busy signal would be read here (design Q4).
    /// </remarks>
    public ScopeReadiness ProbeReadiness() => _context.ProbeReadiness() with { ScopeName = nameof(AppRoot) };

    /// <inheritdoc cref="IMauiElementScope.IsReady"/>
    public bool IsReady() => ProbeReadiness().IsReady;

    /// <inheritdoc />
    public bool WaitReady(int? timeoutMs = null) => _context.WaitReady(timeoutMs);

    /// <inheritdoc />
    /// <remarks>
    /// Never: a scroll lookup searches whichever scroller is on screen, and the app root has no
    /// scroller of its own, so a sweep would find whatever page happens to be under the control.
    /// A scope inside it that scrolls (Shell's flyout) turns it back on.
    /// </remarks>
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
