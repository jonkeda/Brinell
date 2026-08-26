using Brinell.Maui.Containers;
using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Containers2;

/// <summary>
/// The task list, modelled as a collection of scoped rows.
/// </summary>
/// <remarks>
/// <para>
/// Rows are discovered by their control type, not by an indexed AutomationId. The item
/// template in <c>ContainerDemoView.xaml</c> gives each row a bare <c>Border</c> root
/// with no id at all, and its children (<c>TaskCheckBox</c>, <c>TaskNameLabel</c>,
/// <c>TaskDeleteButton</c>) repeat identically on every row. Item scoping is the only
/// thing that keeps them apart.
/// </para>
/// <para>
/// This replaces a <c>List&lt;ContainerDemoPage, TaskItemContainer&gt;</c> that looked
/// rows up page-wide by <c>Task_{index}</c>. Those ids were never in the markup, which is
/// why the item-scoping test that depended on them was skipped.
/// </para>
/// </remarks>
public class TaskCollection : CollectionObjectBase<ContainerDemoPage, TaskCollection, TaskRow>
{
    public TaskCollection(IMauiScope<ContainerDemoPage> parentScope, string automationId)
        : base(parentScope,
               automationId,
               ItemStrategy.ByLocator(Locator.ByControlType("ListItem")),
               (collection, itemRoot, index) => new TaskRow(collection, itemRoot, index))
    {
    }
}
