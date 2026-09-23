using Brinell.Maui.Containers;

namespace Brinell.Maui.CommunityToolkit.Controls.Layouts;

/// <summary>
/// CommunityToolkit.Maui <c>DockLayout</c>: a layout that docks its children to its top, bottom,
/// left and right edges and fills the rest with the last child. Scopes searches to the layout's
/// own subtree, so child controls are found within the dock rather than page-wide.
/// </summary>
/// <remarks>
/// <para>
/// <b>Scoping only.</b> The layout has no behaviour a test can drive, and nothing of its own
/// reaches automation: a child's <c>DockPosition</c>, <c>HorizontalSpacing</c>,
/// <c>VerticalSpacing</c> and <c>ShouldExpandLastChild</c> are layout inputs, visible to a user
/// only as positions on screen, so there is no read for them (and no bridge verb: AD-008 test 3
/// fails, the only answer would be coordinates). The children are the controls to test.
/// </para>
/// <para>
/// <b>Windows requires the automation handlers.</b> <c>DockLayout</c> derives from MAUI
/// <c>Layout</c> and maps to a WinUI <c>LayoutPanel</c> with no AutomationPeer, so its
/// <c>AutomationId</c> is invisible to UI Automation until the app registers the Brinell automation
/// handlers from <c>samples/Brinell.Maui.AppSupport</c>. The <c>Layout</c> registration covers it;
/// it needs none of its own. Probed 2026-09-18, toolkit 15.0.1: <c>Group class=Layout</c> with the
/// AutomationId and no pattern but ScrollItem; its children sit directly beneath it in declaration
/// order, not dock order, each with its own AutomationId and patterns.
/// </para>
/// <para>
/// <b>Android: not probed</b> (no emulator or device attached, 2026-09-18).
/// </para>
/// <para>
/// The root is cached: the layout keeps its children for its lifetime.
/// </para>
/// </remarks>
/// <typeparam name="TParent">The parent scope type (a page or another container).</typeparam>
/// <typeparam name="TSelf">The dock layout type itself (self-referencing for fluent returns).</typeparam>
public class DockLayout<TParent, TSelf> : ContainerObjectBase<TParent, TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf : DockLayout<TParent, TSelf>
{
    /// <summary>
    /// Creates a dock layout container within the specified scope.
    /// </summary>
    public DockLayout(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>
    /// Creates a dock layout container using the scope's default locator strategy.
    /// </summary>
    public DockLayout(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }

    // No DockPosition, spacing or ShouldExpandLastChild reads: no platform publishes them to
    // automation, and a position on screen is not a route (no coordinates).
}

/// <summary>
/// A <see cref="DockLayout{TParent, TSelf}"/> for use where no dock-specific subclass is needed.
/// </summary>
/// <remarks>
/// Declare a subclass instead when the dock has named children:
/// <code>
/// public class MainDock : DockLayout&lt;MainPage, MainDock&gt;
/// {
///     public MainDock(IMauiScope&lt;MainPage&gt; scope) : base(scope, "MainDock") { }
///     public Label&lt;MainDock&gt; Header => new(this, "Header");
/// }
/// </code>
/// </remarks>
/// <typeparam name="TParent">The parent scope type.</typeparam>
public sealed class DockLayout<TParent> : DockLayout<TParent, DockLayout<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>Creates a dock layout container within the specified scope.</summary>
    public DockLayout(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>Creates a dock layout container using the scope's default locator strategy.</summary>
    public DockLayout(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }
}
