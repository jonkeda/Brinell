using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Container;

/// <summary>
/// MAUI Border container. Scopes searches to the border's own subtree, so child
/// controls are found within the border rather than page-wide.
/// </summary>
/// <remarks>
/// <b>Windows requires an automation handler.</b> A stock MAUI <c>Border</c> maps to a
/// WinUI <c>ContentPanel</c> with no AutomationPeer, so its <c>AutomationId</c> is
/// invisible to UI Automation and this container will not resolve. The app under test
/// must register the Brinell automation handlers — see
/// <c>samples/Brinell.Maui.AppSupport</c>, which can be referenced as a project or
/// copied into the app. Note that <c>Border</c> needs its own registration: it is not
/// covered by the <c>ContentView</c> one, despite sharing a platform view.
/// </remarks>
/// <typeparam name="TParent">The parent scope type (a page or another container).</typeparam>
/// <typeparam name="TSelf">The border type itself (self-referencing for fluent returns).</typeparam>
public partial class Border<TParent, TSelf> : ContainerObjectBase<TParent, TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf : Border<TParent, TSelf>
{
    /// <summary>
    /// Creates a border container within the specified scope.
    /// </summary>
    public Border(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>
    /// Creates a border container using the scope's default locator strategy.
    /// </summary>
    public Border(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }
}

/// <summary>
/// A <see cref="Border{TParent, TSelf}"/> for use where no border-specific subclass is
/// needed.
/// </summary>
/// <typeparam name="TParent">The parent scope type.</typeparam>
public sealed class Border<TParent> : Border<TParent, Border<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>
    /// Creates a border container within the specified scope.
    /// </summary>
    public Border(IMauiScope<TParent> parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }

    /// <summary>
    /// Creates a border container using the scope's default locator strategy.
    /// </summary>
    public Border(IMauiScope<TParent> parentScope, string locatorValue)
        : base(parentScope, locatorValue)
    {
    }
}
