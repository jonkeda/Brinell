using Brinell.Maui.CommunityToolkit.Controls.Layouts;
using Brinell.Samples.Todo.UITests.Pages;

namespace Brinell.Samples.Todo.UITests.Containers;

/// <summary>
/// The list page's CommunityToolkit <c>StateContainer</c>: Loading, Empty, Error, or the rows.
/// </summary>
/// <remarks>
/// A state is named by the AutomationId of the view the container shows for it, which is what
/// Brinell's <c>StateContainer</c> reads (<c>IsShowing</c>, <c>WaitShowing</c>, <c>AssertShowing</c>).
/// The constants below are those ids; the rows are the container's default content.
/// </remarks>
public sealed class TodoListState : StateContainer<TodoListPage, TodoListState>
{
    /// <summary>The view shown while the first load or sync runs.</summary>
    public const string Loading = "TodoList_Loading";

    /// <summary>The view shown when there are no todos.</summary>
    public const string Empty = "TodoList_Empty";

    /// <summary>The view shown when a sync failed in a way the user has to act on.</summary>
    public const string Error = "TodoList_Error";

    /// <summary>The rows: the container's default content.</summary>
    public const string Content = "TodoList_Items";

    /// <summary>Creates the state container within the list page.</summary>
    /// <param name="parentScope">The list page.</param>
    /// <param name="automationId">The container's AutomationId.</param>
    public TodoListState(IMauiScope<TodoListPage> parentScope, string automationId)
        : base(parentScope, automationId)
    {
    }

    /// <summary>"Nothing to do", in the Empty state.</summary>
    public Label<TodoListState> EmptyLabel => new(this, "TodoList_EmptyLabel");

    /// <summary>What went wrong, in the Error state.</summary>
    public Label<TodoListState> ErrorLabel => new(this, "TodoList_ErrorLabel");

    /// <summary>Retries the sync, in the Error state.</summary>
    public Button<TodoListState> Retry => new(this, "TodoList_Retry");
}
