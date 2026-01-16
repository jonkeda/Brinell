namespace Brinell.Maui.Interfaces;

/// <summary>
/// Container interface. Containers are scopes that return themselves,
/// with access to their parent scope (page or another container).
/// </summary>
/// <typeparam name="TParent">The parent scope type (page or container).</typeparam>
/// <typeparam name="TSelf">The container type itself (self-referencing).</typeparam>
public interface IMauiContainer<TParent, TSelf> : IMauiScope<TSelf>, IContainerControl<IMauiElement>
    where TParent : IMauiScope<TParent>
    where TSelf : IMauiContainer<TParent, TSelf>
{
    /// <summary>
    /// Gets the parent scope (page or container).
    /// Navigate up the scope hierarchy by calling Parent.
    /// Chain .Parent.Parent... to reach the root page.
    /// </summary>
    TParent Parent { get; }
}
