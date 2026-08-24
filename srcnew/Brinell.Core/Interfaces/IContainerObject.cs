namespace Brinell.Core.Interfaces;

/// <summary>
/// A scope whose element searches are rooted at a container element rather than at the
/// driver. Generalizes <see cref="IContainerControl{TElement}"/>: a container object is a
/// peer of a page object, not a control that happens to scope.
/// </summary>
/// <typeparam name="TElement">The platform's native element type.</typeparam>
public interface IContainerObject<TElement> : IElementScope<TElement>
{
    /// <summary>
    /// The element all searches within this container are relative to.
    /// </summary>
    TElement ContainerRoot { get; }

    /// <summary>
    /// Drops the cached container root so the next search re-finds it.
    /// Call after a UI refresh that may have recreated the element.
    /// </summary>
    void InvalidateCache();
}

/// <summary>
/// A container whose root is an element supplied at construction rather than one found
/// by a locator. This is what makes collection items scope correctly: the item's root is
/// already known, so its children resolve within that subtree and repeating automation
/// ids across rows stay unambiguous.
/// </summary>
/// <typeparam name="TElement">The platform's native element type.</typeparam>
public interface IItemContainer<TElement> : IContainerObject<TElement>
{
    /// <summary>
    /// The zero-based position of this item within its collection.
    /// </summary>
    int Index { get; }
}

/// <summary>
/// A container that additionally exposes typed access to a sequence of items.
/// Being a container, it also scopes its own non-item controls such as a header,
/// footer, or empty view.
/// </summary>
/// <typeparam name="TElement">The platform's native element type.</typeparam>
/// <typeparam name="TItem">The item container type.</typeparam>
public interface ICollectionObject<TElement, TItem> : IContainerObject<TElement>
{
    /// <summary>
    /// Gets the item at the given zero-based index.
    /// </summary>
    /// <exception cref="Exceptions.ElementNotFoundException">
    /// Thrown when no item exists at that index.
    /// </exception>
    TItem Item(int index);

    /// <summary>
    /// Gets the item at the given zero-based index, or null when there is none.
    /// </summary>
    TItem? TryItem(int index);

    /// <summary>
    /// The number of items currently materialized in the automation tree.
    /// A virtualizing collection may hold more than this until scrolled.
    /// </summary>
    int GetItemCount(int? timeoutMs = null);

    /// <summary>
    /// The items, yielded lazily so a consumer that stops early does not pay to
    /// materialize the rest.
    /// </summary>
    IEnumerable<TItem> Items { get; }
}
