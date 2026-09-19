using Brinell.Samples.Todo.UITests.Controls;
using Brinell.Samples.Todo.UITests.Pages;

namespace Brinell.Samples.Todo.UITests.Containers;

/// <summary>
/// The list page's rows (<c>TodoList_Items</c>, a CollectionView).
/// </summary>
/// <remarks>
/// <para>
/// Rows are found by the repeating <c>TodoRow</c> id, and opened by selecting their containing
/// list item, which is what tapping a row does (probed 2026-09-18: <c>SelectionItemPattern.Select</c>
/// on the row's <c>ListItem</c> opened the detail page).
/// </para>
/// <para>
/// Counts here are realized rows. The scenarios have a handful of todos, so every row is realized;
/// a long list would need a count the app shows.
/// </para>
/// </remarks>
public sealed class TodoList : CollectionObjectBase<TodoListPage, TodoList, TodoRow>
{
    /// <summary>Creates the collection within the list page.</summary>
    /// <param name="parentScope">The list page.</param>
    /// <param name="automationId">The CollectionView's AutomationId.</param>
    public TodoList(IMauiScope<TodoListPage> parentScope, string automationId)
        : base(parentScope,
               automationId,
               ItemStrategy.ByAutomationId("TodoRow"),
               (collection, itemRoot, index) => new TodoRow(collection, itemRoot, index))
    {
    }

    /// <summary>The row with this title. Throws when there is none.</summary>
    public TodoRow Row(string title) => ItemWhere(row => row.Title.GetText() == title);

    /// <summary>The titles of the rows, in the order shown.</summary>
    public IReadOnlyList<string> Titles() => [.. Items.Select(row => row.Title.GetText() ?? string.Empty)];

    /// <summary>Waits until a row with this title is shown (or, with <c>false</c>, is gone).</summary>
    public bool WaitRow(string title, bool present = true, int? timeoutMs = null)
        => RunWait(() => (FindItem(row => row.Title.GetText() == title) is not null) == present, timeoutMs);

    /// <summary>Asserts a row with this title is shown (or, with <c>false</c>, is not).</summary>
    public TodoList AssertRow(string title, bool present = true, int? timeoutMs = null)
    {
        if (!WaitRow(title, present, timeoutMs))
        {
            throw new AssertionException(
                $"Expected {(present ? "a" : "no")} row titled '{title}' in '{Locator.Value}'. "
                + $"Rows shown: {string.Join(", ", Titles().Select(shown => $"'{shown}'"))}.");
        }

        return Self;
    }

    /// <summary>
    /// Waits until the row with this title shows (or, with <c>false</c>, no longer shows) the
    /// "not synced yet" marker.
    /// </summary>
    /// <remarks>
    /// Finds the row by title on every poll rather than holding one: a sync reloads the list, and a
    /// row found before it would be the old one.
    /// </remarks>
    public TodoList AssertPending(string title, bool expected = true, int? timeoutMs = null)
    {
        if (!RunWait(() => FindItem(row => row.Title.GetText() == title)?.Pending.IsShown() == expected, timeoutMs))
        {
            throw new AssertionException(
                $"Expected the row titled '{title}' in '{Locator.Value}' to {(expected ? "show" : "not show")} the pending marker.");
        }

        return Self;
    }

    /// <summary>Opens a row's detail page, as tapping it does.</summary>
    public TodoDetailPage Open(string title, int? timeoutMs = null)
    {
        WaitRow(title, present: true, timeoutMs);
        SelectItem(Row(title).Index, timeoutMs);

        var detail = new TodoDetailPage(Context);
        detail.AssertLoaded(true, timeoutMs: timeoutMs ?? TestConstants.PageTimeoutMs);
        return detail;
    }
}

/// <summary>
/// One todo row, scoped to its own item root.
/// </summary>
/// <remarks>
/// Every id below repeats unchanged on every row. The status is the read-only form of the
/// <see cref="TodoStatus{TScope}"/> component: it has no Next button.
/// </remarks>
public sealed class TodoRow : ItemContainerBase<TodoList, TodoRow>
{
    /// <summary>Creates a row from the root the collection found.</summary>
    public TodoRow(TodoList collection, IMauiElement itemRoot, int index)
        : base(collection, itemRoot, index)
    {
    }

    /// <summary>The title.</summary>
    public Label<TodoRow> Title => new(this, "TodoRow_Title");

    /// <summary>"Due …"; not shown when the todo has no due date.</summary>
    public ShownLabel<TodoRow> Due => new(this, "TodoRow_Due");

    /// <summary>The read-only status.</summary>
    public TodoStatus<TodoRow> Status => new(this, "TodoRow_Status");

    /// <summary>The "not synced yet" marker, shown only while the row has unsynced changes.</summary>
    public TodoPendingMark<TodoRow> Pending => new(this, "TodoRow_Pending");
}
