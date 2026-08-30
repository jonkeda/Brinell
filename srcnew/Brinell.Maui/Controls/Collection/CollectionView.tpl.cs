using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Collection;

/// <summary>
/// MAUI CollectionView: a scrollable collection that hands out typed, scoped rows.
/// </summary>
/// <remarks>
/// <para>
/// Derive from this rather than instantiating it — <see cref="CollectionObjectBase{TParent, TSelf, TItem}"/>
/// is self-referencing so that every member returns the concrete collection type and a
/// chain stays inside it:
/// </para>
/// <code>
/// public class ProductCollection : CollectionView&lt;ProductsPage, ProductCollection, ProductRow&gt;
/// {
///     public ProductCollection(IMauiScope&lt;ProductsPage&gt; scope)
///         : base(scope, "ProductList", ItemStrategy.ByAutomationId("ProductRow"),
///                (c, root, i) =&gt; new ProductRow(c, root, i)) { }
/// }
/// </code>
/// <para>
/// This replaces the former <c>CollectionView&lt;TScope, TItem&gt; : List&lt;&gt;</c>, which
/// looked rows up page-wide by an indexed AutomationId. Rows are now found within the
/// collection and scoped to their own root, so an item template can repeat the same ids on
/// every row — the normal MAUI authoring style.
/// </para>
/// </remarks>
/// <typeparam name="TParent">The parent scope type (a page or another container).</typeparam>
/// <typeparam name="TSelf">The collection type itself (self-referencing).</typeparam>
/// <typeparam name="TItem">The row type.</typeparam>
public abstract partial class CollectionView<TParent, TSelf, TItem>
    : CollectionObjectBase<TParent, TSelf, TItem>
    where TParent : IMauiScope<TParent>
    where TSelf : CollectionView<TParent, TSelf, TItem>
    where TItem : ItemContainerBase<TSelf, TItem>
{
    /// <summary>
    /// Creates a CollectionView bound to an explicit locator.
    /// </summary>
    protected CollectionView(
        IMauiScope<TParent> parentScope,
        Locator locator,
        IItemStrategy itemStrategy,
        Func<TSelf, IMauiElement, int, TItem> itemFactory)
        : base(parentScope, locator, itemStrategy, itemFactory)
    {
    }

    /// <summary>
    /// Creates a CollectionView using the scope's default locator strategy.
    /// </summary>
    protected CollectionView(
        IMauiScope<TParent> parentScope,
        string automationId,
        IItemStrategy itemStrategy,
        Func<TSelf, IMauiElement, int, TItem> itemFactory)
        : base(parentScope, automationId, itemStrategy, itemFactory)
    {
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Gets the collection's selection mode.
    /// </summary>
    /// <param name="element">The collection's own element (may be null).</param>
    /// <returns>The selection mode string, or null when the attribute is unavailable.</returns>
    [AbsenceTolerant]
    protected virtual string? GetSelectionModeCore(IMauiElement? element)
        => element?.GetAttribute("SelectionMode");

    /// <summary>
    /// Whether multiple selection is enabled.
    /// </summary>
    /// <remarks>
    /// Derived from <see cref="GetSelectionModeCore"/> rather than read separately: MAUI
    /// reports one SelectionMode attribute, and "multiple" is a reading of it.
    /// </remarks>
    /// <param name="element">The collection's own element (may be null).</param>
    /// <returns>True for multiple selection; false for single or none; null if unknown.</returns>
    [AbsenceTolerant]
    protected virtual bool? IsMultiSelectEnabledCore(IMauiElement? element)
        => GetSelectionModeCore(element)?.Equals("Multiple", StringComparison.OrdinalIgnoreCase);

    #endregion
}
