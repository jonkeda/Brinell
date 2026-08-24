namespace Brinell.Maui.Containers;

/// <summary>
/// Base class for a collection item: a container whose root element is supplied rather
/// than located.
/// </summary>
/// <remarks>
/// This is what makes item scoping real. Because the row's root is already known, its
/// children resolve within that subtree, so an item template can use the same automation
/// ids on every row - the normal MAUI authoring style - without the rows becoming
/// ambiguous.
/// </remarks>
/// <typeparam name="TCollection">The owning collection.</typeparam>
/// <typeparam name="TSelf">The item type itself (self-referencing for fluent returns).</typeparam>
public abstract class ItemContainerBase<TCollection, TSelf>
    : ContainerObjectBase<TCollection, TSelf>, IMauiItemContainer<TCollection, TSelf>
    where TCollection : IMauiScope<TCollection>, IItemRootProvider
    where TSelf : ItemContainerBase<TCollection, TSelf>
{
    private IMauiElement _itemRoot;

    /// <summary>
    /// Creates an item bound to an already-found root element.
    /// </summary>
    /// <param name="collection">The owning collection.</param>
    /// <param name="itemRoot">The item's root element.</param>
    /// <param name="index">The item's zero-based position.</param>
    protected ItemContainerBase(TCollection collection, IMauiElement itemRoot, int index)
        : base(collection, Locator.ByAutomationId($"[item {index}]"))
    {
        _itemRoot = itemRoot ?? throw new ArgumentNullException(nameof(itemRoot));
        Index = index;
    }

    /// <inheritdoc />
    public int Index { get; }

    /// <summary>
    /// Returns the supplied root, re-resolving it from the collection if it has died.
    /// </summary>
    /// <remarks>
    /// Mutating or scrolling a collection can invalidate a captured element. Platform
    /// adapters are inconsistent about how they report that: some raise
    /// <see cref="StaleElementReferenceException"/>, others let a raw UI-automation
    /// error escape, and some return an element whose bounds have collapsed. Any of
    /// those is treated as "re-resolve from the collection", which is also correct when
    /// the element is merely still valid.
    /// </remarks>
    protected override IMauiElement FindContainerRootElement()
    {
        if (IsUsable(_itemRoot))
        {
            return _itemRoot;
        }

        var refreshed = Parent.TryGetItemRoot(Index)
            ?? throw new ElementNotFoundException(
                $"Item {Index} is no longer available and could not be re-resolved in its collection.");

        _itemRoot = refreshed;
        return _itemRoot;
    }

    /// <summary>
    /// Whether an element still answers for itself. Any failure means "not usable".
    /// </summary>
    private static bool IsUsable(IMauiElement element)
    {
        try
        {
            _ = element.TagName;
            var rect = element.Rect;
            return rect.Width > 0 && rect.Height > 0;
        }
        catch
        {
            // A dead element throws differently per adapter; all of them mean re-resolve.
            return false;
        }
    }
}

/// <summary>
/// Lets an item re-resolve its own root after virtualization invalidates it.
/// Implemented by collections; separated from the collection interface so
/// <see cref="ItemContainerBase{TCollection, TSelf}"/> can constrain on it without
/// naming the item type and creating a circular constraint.
/// </summary>
public interface IItemRootProvider
{
    /// <summary>
    /// Finds the root element of the item at the given index, or null when there is none.
    /// </summary>
    IMauiElement? TryGetItemRoot(int index);
}
