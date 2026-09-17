using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// A collection item that can be the current one: a tab, a flyout entry, a list row.
/// </summary>
/// <remarks>
/// Selection state is read the same way for every navigation item, so it lives here.
/// </remarks>
/// <typeparam name="TCollection">The owning collection.</typeparam>
/// <typeparam name="TSelf">The item type itself (self-referencing for fluent returns).</typeparam>
public abstract partial class SelectableItemBase<TCollection, TSelf>
    : ClickableItemBase<TCollection, TSelf>
    where TCollection : IMauiScope<TCollection>, IItemRootProvider
    where TSelf : SelectableItemBase<TCollection, TSelf>
{
    /// <summary>
    /// Creates an item bound to an already-found root element.
    /// </summary>
    protected SelectableItemBase(TCollection collection, IMauiElement itemRoot, int index)
        : base(collection, itemRoot, index)
    {
    }

    /// <summary>
    /// A selectable item is chosen, not activated.
    /// </summary>
    /// <remarks>
    /// Choosing one member of a group unchooses the others; invoking does not. A tab, a list row
    /// and a radio button are selected; a menu entry is invoked, as
    /// <see cref="ClickableItemBase{TCollection, TSelf}"/> does.
    /// </remarks>
    /// <param name="element">The item's root element.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected override void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        EnsureClickableCore(element);
        element.Select();
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Whether this is the current item.
    /// </summary>
    /// <param name="element">The item's root element (may be null).</param>
    [AbsenceTolerant]
    protected virtual bool? IsSelectedCore(IMauiElement? element)
        => element != null && IsMarkedSelected(element);

    /// <summary>
    /// Whether an element reports itself as selected, by any means the platform offers.
    /// </summary>
    /// <remarks>
    /// Reads <c>Selected</c>, then checked state, because Android reports a radio-style tab bar
    /// through checked state. An element that exposes neither is not selected.
    /// </remarks>
    protected static bool IsMarkedSelected(IMauiElement? element)
    {
        if (element == null) return false;

        return element.Selected || element.Checked == true;
    }

    #endregion
}
