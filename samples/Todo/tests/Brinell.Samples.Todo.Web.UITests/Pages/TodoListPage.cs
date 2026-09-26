using Brinell.Html.Collections;
using Brinell.Html.Controls.Buttons;
using Brinell.Html.Controls.Display;
using Brinell.Html.Controls.Selection;

namespace Brinell.Samples.Todo.Web.UITests.Pages;

/// <summary>The list page. Test ids as in the MAUI app (TodoListPage.xaml).</summary>
public sealed class TodoListPage : BlazorPageObjectBase<TodoListPage>
{
    public TodoListPage(IHtmlTestContext context)
        : base(context, "/")
    {
    }

    public override bool IsLoaded() => base.IsLoaded() && Add.IsExists();

    protected override bool CompareUrl() => new Uri(Context.CurrentUrl).LocalPath is "/" or "/todos";

    public ButtonControl<TodoListPage> SyncButton => new(this, Locator.ByDataTestId("TodoList_Sync"));

    public ButtonControl<TodoListPage> Add => new(this, Locator.ByDataTestId("TodoList_Add"));

    public SelectControl<TodoListPage> Filter => new(this, Locator.ByDataTestId("TodoList_Filter"));

    public LabelControl<TodoListPage> LastSync => new(this, Locator.ByDataTestId("TodoList_LastSync"));

    public LabelControl<TodoListPage> EmptyLabel => new(this, Locator.ByDataTestId("TodoList_EmptyLabel"));

    public TodoRows Todos => new(this);

    /// <summary>Opens a new, empty edit form.</summary>
    public TodoEditPage AddTodo()
    {
        Add.Click();
        return TodoEditPage.Arrived(Context);
    }

    /// <summary>Adds a todo with <paramref name="title"/> and returns to the list.</summary>
    public TodoListPage CreateTodo(string title) => AddTodo().EnterTitle(title).SaveNew();

    /// <summary>Syncs, and waits until the sync is over.</summary>
    public TodoListPage Sync()
    {
        SyncButton.Click();
        AssertIdle();
        return this;
    }

    /// <summary>Opens the todo with <paramref name="title"/>.</summary>
    public TodoDetailPage Open(string title)
    {
        Todos.Row(title).Open.Click();
        return TodoDetailPage.Arrived(Context);
    }

    /// <summary>The rows, by their <c>data-item-key</c> (the todo's id).</summary>
    public sealed class TodoRows(IHtmlScope<TodoListPage> page)
        : CollectionObjectBase<TodoListPage, TodoRows, TodoRow>(
            page, Locator.ByDataTestId("TodoList_Items"), Locator.ByDataTestId("TodoRow"))
    {
        public override TodoRows Self => this;

        protected override TodoRow CreateItem(ItemKey key, int position) => new(this, key, position);

        /// <summary>The row with <paramref name="title"/>, waiting for it.</summary>
        public TodoRow Row(string title, int? timeoutMs = null)
            => ItemWhere(row => row.Title.TryGetText() == title, timeoutMs);

        /// <summary>Waits for a row with <paramref name="title"/>, or for none.</summary>
        public TodoRows AssertRow(string title, bool present = true, int? timeoutMs = null)
        {
            if (present)
            {
                Row(title, timeoutMs);
            }
            else
            {
                WaitGone(title, timeoutMs);
            }

            return this;
        }

        /// <summary>Waits until the row with <paramref name="title"/> shows the pending marker, or does not.</summary>
        public TodoRows AssertPending(string title, bool expected = true, int? timeoutMs = null)
        {
            Row(title, timeoutMs).Pending.AssertExists(expected, timeoutMs: timeoutMs);
            return this;
        }

        private void WaitGone(string title, int? timeoutMs)
        {
            var row = Items.FirstOrDefault(candidate => candidate.Title.TryGetText() == title);
            row?.Title.AssertExists(false, timeoutMs: timeoutMs);
        }
    }

    /// <summary>One row: title, due date, read-only status, pending marker.</summary>
    public sealed class TodoRow(IHtmlCollection rows, ItemKey key, int position) : ItemObjectBase<TodoRow>(rows, key, position)
    {
        public override TodoRow Self => this;

        public LabelControl<TodoRow> Title => new(this, Locator.ByDataTestId("TodoRow_Title"));

        public LabelControl<TodoRow> Due => new(this, Locator.ByDataTestId("TodoRow_Due"));

        public TodoStatus<TodoRow> Status => new(this, "TodoRow_Status");

        public LabelControl<TodoRow> Pending => new(this, Locator.ByDataTestId("TodoRow_Pending"));

        /// <summary>The row's title, which opens the todo when clicked.</summary>
        public ButtonControl<TodoRow> Open => new(this, Locator.ByDataTestId("TodoRow_Title"));
    }
}
