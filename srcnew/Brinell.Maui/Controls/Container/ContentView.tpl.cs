using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Container;

/// <summary>
/// MAUI ContentView container. Scopes searches to the view's own subtree, so child
/// controls are found within it rather than page-wide.
/// </summary>
/// <remarks>
/// <para>
/// <c>ContentView</c> is the base of every custom MAUI view, so this is the container to
/// reach for when modelling a reusable composite control.
/// </para>
/// <para>
/// <b>Windows requires an automation handler.</b> A stock MAUI <c>ContentView</c> maps to
/// a WinUI <c>ContentPanel</c> with no AutomationPeer, so its <c>AutomationId</c> is
/// invisible to UI Automation and this container will not resolve. The app under test
/// must register the Brinell automation handlers — see
/// <c>samples/Brinell.Maui.AppSupport</c>, which can be referenced as a project or copied
/// into the app.
/// </para>
/// </remarks>
/// <typeparam name="TParent">The parent scope type (a page or another container).</typeparam>
/// <typeparam name="TSelf">The view type itself (self-referencing for fluent returns).</typeparam>
public partial class ContentView<TParent, TSelf> : ContainerObjectBase<TParent, TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf : ContentView<TParent, TSelf>
{
    /// <summary>
    /// Creates a ContentView container within the specified scope.
    /// </summary>
    public ContentView(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>
    /// Creates a ContentView container using the scope's default locator strategy.
    /// </summary>
    public ContentView(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }
}

/// <summary>
/// A <see cref="ContentView{TParent, TSelf}"/> for use where no view-specific subclass
/// is needed.
/// </summary>
/// <typeparam name="TParent">The parent scope type.</typeparam>
public sealed class ContentView<TParent> : ContentView<TParent, ContentView<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>
    /// Creates a ContentView container within the specified scope.
    /// </summary>
    public ContentView(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>
    /// Creates a ContentView container using the scope's default locator strategy.
    /// </summary>
    public ContentView(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }
}
