namespace Brinell.Maui.Interfaces;

/// <summary>
/// A MAUI container object: a scope rooted at an element, holding controls and other
/// containers, with an explicit way back out to its parent.
/// </summary>
/// <remarks>
/// Unlike <see cref="IMauiContainer{TParent, TSelf}"/> (which a control implements as a
/// side effect of scoping), a container object is a peer of a page object. Its own
/// members return <typeparamref name="TSelf"/>, so a chain stays inside the container
/// until <see cref="Parent"/> is called.
/// </remarks>
/// <typeparam name="TParent">The parent scope type (a page or another container).</typeparam>
/// <typeparam name="TSelf">The container type itself (self-referencing).</typeparam>
public interface IMauiContainerObject<TParent, TSelf>
    : IMauiScope<TSelf>, IContainerObject<IMauiElement>
    where TParent : IMauiScope<TParent>
    where TSelf : IMauiContainerObject<TParent, TSelf>
{
    /// <summary>
    /// The parent scope. Chain <c>.Parent.Parent</c> to walk out to the root page.
    /// </summary>
    TParent Parent { get; }
}

/// <summary>
/// A MAUI container whose root element is supplied rather than located, so its children
/// resolve within that subtree. Collection rows are the motivating case.
/// </summary>
/// <typeparam name="TCollection">The owning collection scope.</typeparam>
/// <typeparam name="TSelf">The item type itself (self-referencing).</typeparam>
public interface IMauiItemContainer<TCollection, TSelf>
    : IMauiContainerObject<TCollection, TSelf>, IItemContainer<IMauiElement>
    where TCollection : IMauiScope<TCollection>
    where TSelf : IMauiItemContainer<TCollection, TSelf>
{
}

/// <summary>
/// A MAUI collection object: a container that also hands out typed items.
/// </summary>
/// <typeparam name="TParent">The parent scope type.</typeparam>
/// <typeparam name="TSelf">The collection type itself (self-referencing).</typeparam>
/// <typeparam name="TItem">The item container type.</typeparam>
public interface IMauiCollectionObject<TParent, TSelf, TItem>
    : IMauiContainerObject<TParent, TSelf>, ICollectionObject<IMauiElement, TItem>
    where TParent : IMauiScope<TParent>
    where TSelf : IMauiCollectionObject<TParent, TSelf, TItem>
    where TItem : IMauiItemContainer<TSelf, TItem>
{
}
