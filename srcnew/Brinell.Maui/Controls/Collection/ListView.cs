using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Collection;

/// <summary>
/// MAUI ListView: a scrollable list that hands out typed, scoped rows.
/// </summary>
/// <remarks>
/// <para>
/// Derive from this rather than instantiating it — the base is self-referencing so that
/// every member returns the concrete collection type:
/// </para>
/// <code>
/// public class TaskList : ListView&lt;TasksPage, TaskList, TaskRow&gt;
/// {
///     public TaskList(IMauiScope&lt;TasksPage&gt; scope)
///         : base(scope, "TaskList", ItemStrategy.ByLocator(Locator.ByControlType("ListItem")),
///                (c, root, i) =&gt; new TaskRow(c, root, i)) { }
/// }
/// </code>
/// <para>
/// <c>ListView</c> is superseded by <c>CollectionView</c> in modern MAUI. It is kept for
/// apps that still use it; prefer <see cref="CollectionView{TParent, TSelf, TItem}"/> for
/// new work.
/// </para>
/// </remarks>
/// <typeparam name="TParent">The parent scope type (a page or another container).</typeparam>
/// <typeparam name="TSelf">The list type itself (self-referencing).</typeparam>
/// <typeparam name="TItem">The row type.</typeparam>
public abstract class ListView<TParent, TSelf, TItem>
    : CollectionObjectBase<TParent, TSelf, TItem>
    where TParent : IMauiScope<TParent>
    where TSelf : ListView<TParent, TSelf, TItem>
    where TItem : ItemContainerBase<TSelf, TItem>
{
    /// <summary>
    /// Creates a ListView bound to an explicit locator.
    /// </summary>
    protected ListView(
        IMauiScope<TParent> parentScope,
        Locator locator,
        IItemStrategy itemStrategy,
        Func<TSelf, IMauiElement, int, TItem> itemFactory)
        : base(parentScope, locator, itemStrategy, itemFactory)
    {
    }

    /// <summary>
    /// Creates a ListView using the scope's default locator strategy.
    /// </summary>
    protected ListView(
        IMauiScope<TParent> parentScope,
        string automationId,
        IItemStrategy itemStrategy,
        Func<TSelf, IMauiElement, int, TItem> itemFactory)
        : base(parentScope, automationId, itemStrategy, itemFactory)
    {
    }
}
