using Brinell.Core.Interfaces;

namespace Brinell.Maui.Pages;

/// <summary>
/// The whole app as a scope, for the controls that do not live inside a page.
/// </summary>
/// <remarks>
/// <para>
/// <b>Some controls are genuinely outside every page root.</b> A MAUI <c>ToolbarItem</c> renders
/// into native window chrome; so do menu bars, Shell's flyout, platform date pickers and the
/// buttons on a native alert. They belong to the app rather than to a page, and a page object
/// asked for one is being asked for something it does not contain.
/// </para>
/// <para>
/// <b>Scoping them to a page object does not merely offend tidiness - it cannot work.</b> Two
/// gates enforce that a control resolves within its scope, and both key off whether that scope
/// has a page: <c>PageObjectBase.CanResolveElements</c> refuses required resolution when the page
/// is not loaded, and <c>ViewBase.RunPoll</c> refuses any polled operation the same way. For a
/// back affordance the contradiction is total, because the page it returns to is not loaded
/// precisely when the control is needed. That cost the suite a full outage; see
/// <c>.my/fix/rca-page-readiness-gate.md</c>.
/// </para>
/// <para>
/// <b>This scope reports no page, so neither gate applies</b> - which is correct rather than a
/// loophole. There is no page whose readiness could bear on a control that is not in one, and
/// the driver root is always available while the session is.
/// </para>
/// <para>
/// <b>Nearly all of it already existed.</b> <see cref="IMauiTestContext"/> is itself an
/// <c>IMauiElementScope</c>, already answers <c>Page =&gt; null</c> and already calls itself the
/// root scope in its own remarks. The one thing it cannot do is satisfy
/// <see cref="IMauiScope{TScope}"/>, which is self-referencing: a scope has to name its own type
/// so controls can return it for chaining, and the context cannot without leaking its
/// implementation type into every control declared on it. So this is a name for something that
/// was already there, not a new mechanism.
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
    /// The load-bearing line of this class. Every readiness gate in the framework is written as
    /// <c>if (Page != null &amp;&amp; ...)</c>, so answering null is what makes a control here
    /// resolve against the app rather than against a page that may not be showing.
    /// </remarks>
    public IPageObject? Page => null;

    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => _context.DefaultLocatorStrategy;

    /// <inheritdoc />
    /// <remarks>Ready whenever the session is; there is nothing else to wait for.</remarks>
    public bool IsReady(int? timeoutMs = null) => _context.IsReady(timeoutMs);

    /// <inheritdoc />
    public bool WaitReady(int? timeoutMs = null) => _context.WaitReady(timeoutMs);

    /// <inheritdoc />
    public IMauiElement? TryFindElement(Locator locator) => _context.TryFindElement(locator);

    /// <inheritdoc />
    public IMauiElement FindElement(Locator locator) => _context.FindElement(locator);

    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator)
        => _context.FindElements(locator);

    /// <inheritdoc />
    public IMauiElement? TryFindElementAfterScroll(Locator locator)
        => _context.TryFindElementAfterScroll(locator);
}
