using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// Base class for a collection item that behaves like a leaf control: it is clicked, it
/// says something, and it can be disabled.
/// </summary>
/// <remarks>
/// <para>
/// An item is a <see cref="ItemContainerBase{TCollection, TSelf}"/> rather than a control
/// because <see cref="CollectionObjectBase{TParent, TSelf, TItem}"/> hands out containers -
/// that is what gives the item its own root and lets a chain stay on the item. The price is
/// that a leaf item cannot inherit <see cref="ClickableControlBase{TScope}"/>, so the two
/// or three members a leaf needs live here instead, once, for every item type that wants
/// them.
/// </para>
/// <para>
/// Existence and visibility are not declared here: <see cref="ContainerObjectBase{TParent,
/// TSelf}"/> already answers both against the item's root.
/// </para>
/// </remarks>
/// <typeparam name="TCollection">The owning collection.</typeparam>
/// <typeparam name="TSelf">The item type itself (self-referencing for fluent returns).</typeparam>
public abstract partial class ClickableItemBase<TCollection, TSelf>
    : ItemContainerBase<TCollection, TSelf>
    where TCollection : IMauiScope<TCollection>, IItemRootProvider
    where TSelf : ClickableItemBase<TCollection, TSelf>
{
    /// <summary>
    /// Creates an item bound to an already-found root element.
    /// </summary>
    /// <param name="collection">The owning collection.</param>
    /// <param name="itemRoot">The item's root element.</param>
    /// <param name="index">The item's zero-based position.</param>
    protected ClickableItemBase(TCollection collection, IMauiElement itemRoot, int index)
        : base(collection, itemRoot, index)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Clicks the item's own root element.
    /// </summary>
    /// <param name="element">The item's root element.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    protected virtual void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        EnsureClickableCore(element);

        if (ActivationHelper.TryActivateByPattern(element))
            return;

        element.Click();
    }

    /// <summary>
    /// Reads the item's caption: its text, or failing that its accessibility name.
    /// </summary>
    /// <remarks>
    /// An item drawn by the platform rather than by the app often has no text at all - an
    /// Android tab carries its title as a content description - and the name is then the only
    /// caption there is. Windows needs no fallback: its text already reads through to the name.
    /// </remarks>
    /// <param name="element">The item's root element.</param>
    [GenerateComparisons(Comparison.Equals | Comparison.Contains | Comparison.Empty)]
    protected virtual string? GetTextCore(IMauiElement element)
    {
        var text = element.Text;

        return string.IsNullOrEmpty(text) ? element.Name : text;
    }

    /// <summary>
    /// Whether the item is enabled.
    /// </summary>
    /// <param name="element">The item's root element (may be null).</param>
    protected virtual bool? IsEnabledCore(IMauiElement? element) => element?.Enabled;

    #endregion

    #region Guards

    /// <summary>
    /// Throws when the item is not enabled.
    /// </summary>
    /// <param name="element">The item's root element.</param>
    protected virtual void EnsureEnabledCore(IMauiElement element)
    {
        if (IsEnabledCore(element) != true)
        {
            throw new TimeoutException($"Item {Index} was not enabled. Locator: {Locator}");
        }
    }

    /// <summary>
    /// Throws when the item cannot be acted on.
    /// </summary>
    /// <param name="element">The item's root element.</param>
    protected virtual void EnsureClickableCore(IMauiElement element) => EnsureEnabledCore(element);

    #endregion
}
