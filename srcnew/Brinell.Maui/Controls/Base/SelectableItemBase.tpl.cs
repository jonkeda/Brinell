using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// A collection item that can be the current one: a tab, a flyout entry, a list row.
/// </summary>
/// <remarks>
/// Selection is the one thing a navigation item knows that a plain clickable item does not,
/// and reading it is the same everywhere, so it lives here rather than in each item type.
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
    /// The distinction this class is named for, now expressed in what it does rather than only
    /// in what it reads: choosing one member of a group unchooses the others, and invoking does
    /// not. A tab, a list row and a radio button are all selected; a menu entry is invoked, which
    /// is what <see cref="ClickableItemBase{TCollection, TSelf}"/> does.
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
    /// Windows answers through <c>Selected</c>, which reads the selection pattern and falls
    /// back to the toggle pattern. Android reports selection on a tab bar but checked state on
    /// a radio-style one, so the toggle probe is asked second rather than not at all. An
    /// element that exposes neither is not selected as far as anything can tell, which is the
    /// honest answer - guessing from styling or from app state is not a control's business.
    /// </remarks>
    protected static bool IsMarkedSelected(IMauiElement? element)
    {
        if (element == null) return false;

        if (element.Selected) return true;

        return element is ITogglePatternElement { SupportsTogglePattern: true } toggle
               && toggle.IsTogglePatternChecked() == true;
    }

    #endregion
}
