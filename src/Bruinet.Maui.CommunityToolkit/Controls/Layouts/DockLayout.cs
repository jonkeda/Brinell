using Bruinet.Maui.Containers;

namespace Bruinet.Maui.CommunityToolkit.Controls.Layouts;

/// <summary>
/// CommunityToolkit.Maui <c>DockLayout</c>: a layout that docks its children to its top, bottom,
/// left and right edges and fills the rest with the last child. Scopes searches to the layout's
/// own subtree, so child controls are found within the dock rather than page-wide.
/// Scoping only: dock position and spacing are not published, so there are no reads for them.
/// Routes and platform limits: <c>docs/controls/community-toolkit.md</c>.
/// </summary>
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
