using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Container;

/// <summary>
/// MAUI Grid container. Scopes searches to the grid's own subtree, so child controls
/// are found within the grid rather than page-wide.
/// </summary>
/// <remarks>
/// <para>
/// This replaces the former <c>Grid&lt;TScope&gt;</c> / <c>Grid&lt;TParent, TSelf&gt;</c>
/// pair. That split existed only because the old container base made a container's own
/// inherited members return the parent scope; with
/// <see cref="ContainerObjectBase{TParent, TSelf}"/> every member returns the grid, so
/// one type serves both uses.
/// </para>
/// <para>
/// <b>Windows requires an automation handler.</b> A stock MAUI <c>Grid</c> maps to a
/// WinUI panel with no AutomationPeer, so its <c>AutomationId</c> is invisible to UI
/// Automation and this container will not resolve. The app under test must register the
/// Brinell automation handlers — see <c>samples/Brinell.Maui.AppSupport</c>, which can be
/// referenced as a project or copied into the app.
/// </para>
/// </remarks>
/// <typeparam name="TParent">The parent scope type (a page or another container).</typeparam>
/// <typeparam name="TSelf">The grid type itself (self-referencing for fluent returns).</typeparam>
public class Grid<TParent, TSelf> : ContainerObjectBase<TParent, TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf : Grid<TParent, TSelf>
{
    /// <summary>
    /// Creates a grid container within the specified scope.
    /// </summary>
    public Grid(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>
    /// Creates a grid container using the scope's default locator strategy.
    /// </summary>
    public Grid(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }
}

/// <summary>
/// A <see cref="Grid{TParent, TSelf}"/> for use where no grid-specific subclass is
/// needed.
/// </summary>
/// <remarks>
/// Declare a subclass instead when the grid has named children:
/// <code>
/// public class LoginForm : Grid&lt;LoginPage, LoginForm&gt;
/// {
///     public LoginForm(IMauiScope&lt;LoginPage&gt; scope) : base(scope, "LoginForm") { }
///     public Entry&lt;LoginForm&gt; UserName => new(this, "UserName");
/// }
/// </code>
/// </remarks>
/// <typeparam name="TParent">The parent scope type.</typeparam>
public sealed class Grid<TParent> : Grid<TParent, Grid<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>
    /// Creates a grid container within the specified scope.
    /// </summary>
    public Grid(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>
    /// Creates a grid container using the scope's default locator strategy.
    /// </summary>
    public Grid(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }
}
