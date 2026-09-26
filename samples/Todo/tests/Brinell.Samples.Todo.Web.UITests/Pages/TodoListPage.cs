using Brinell.Core.Exceptions;
using Brinell.Core.Testing;
using Brinell.Html.Collections;
using Brinell.Html.Controls.Buttons;
using Brinell.Html.Controls.Display;
using Brinell.Html.Controls.Selection;
using Brinell.Uat;

namespace Brinell.Samples.Todo.Web.UITests.Pages;

/// <summary>
/// The list page. Test ids as in the MAUI app (TodoListPage.xaml), and member names as on the MAUI
/// page object, so one scenario file reads the same on both heads.
/// </summary>
public sealed class TodoListPage : BlazorPageObjectBase<TodoListPage>
{
    public TodoListPage(IHtmlTestContext context)
        : base(context, "/")
    {
    }

    public override bool IsLoaded() => base.IsLoaded() && AddButton.IsExists();

    protected override bool CompareUrl() => new Uri(Context.CurrentUrl).LocalPath is "/" or "/todos";

    public ButtonControl<TodoListPage> SyncButton => new(this, Locator.ByDataTestId("TodoList_Sync"));

    public ButtonControl<TodoListPage> AddButton => new(this, Locator.ByDataTestId("TodoList_Add"));

    public SelectControl<TodoListPage> Filter => new(this, Locator.ByDataTestId("TodoList_Filter"));

    public LabelControl<TodoListPage> LastSync => new(this, Locator.ByDataTestId("TodoList_LastSync"));

    /// <summary>Loading, Empty, Error or the rows: exactly one shows.</summary>
    public TodoListState State => new(this);

    [UatName("Empty Message")]
    public LabelControl<TodoListPage> EmptyMessage => new(this, Locator.ByDataTestId("TodoList_EmptyLabel"));

    [UatName("Error Message")]
    public LabelControl<TodoListPage> ErrorMessage => new(this, Locator.ByDataTestId("TodoList_ErrorLabel"));

    public ButtonControl<TodoListPage> RetryButton => new(this, Locator.ByDataTestId("TodoList_Retry"));

    [UatName("Todos")]
    public TodoRows Todos => new(this);

    /// <summary>Opens a new, empty edit form.</summary>
    public TodoEditPage AddTodo()
    {
        AddButton.Click();
        return TodoEditPage.Arrived(Context);
    }

    /// <summary>Adds a todo with <paramref name="title"/> and returns to the list.</summary>
    public TodoListPage CreateTodo(string title) => AddTodo().EnterTitle(title).SaveNew();

    /// <summary>Syncs, and waits until the sync has finished.</summary>
    public TodoListPage Sync()
    {
        SyncButton.Click();
        AssertIdle();
        return this;
    }

    /// <summary>Opens the todo titled <paramref name="title"/>.</summary>
    public TodoDetailPage Open(string title)
    {
        Todos.Row(title).Open.Click();
        return TodoDetailPage.Arrived(Context);
    }

    /// <summary>The rows, one per todo shown. Each row follows its todo by key.</summary>
    public sealed class TodoRows(IHtmlScope<TodoListPage> page)
        : CollectionObjectBase<TodoListPage, TodoRows, TodoRow>(
            page, Locator.ByDataTestId("TodoList_Items"), Locator.ByDataTestId("TodoRow"))
    {
        public override TodoRows Self => this;

        protected override TodoRow CreateItem(ItemKey key, int position) => new(this, key, position);

        /// <summary>The row titled <paramref name="title"/>, waiting for it.</summary>
        public TodoRow Row(string title, int? timeoutMs = null)
            => ItemWhere(row => row.Title.TryGetText() == title, timeoutMs);

        /// <summary>Waits until a row titled <paramref name="title"/> is shown, or is not.</summary>
        [UatThen("{control} should have todo {value}")]
        public TodoRows AssertRow(string title, bool present = true, int? timeoutMs = null)
        {
            if (present)
            {
                Row(title, timeoutMs);
            }
            else if (!WaitNoItemWhere(row => row.Title.TryGetText() == title, timeoutMs))
            {
                throw new AssertionException($"Expected no row titled '{title}' in the list.");
            }

            return this;
        }

        /// <summary>Waits until no row titled <paramref name="title"/> is shown.</summary>
        [UatThen("{control} should not have todo {value}")]
        public TodoRows AssertNoRow(string title, int? timeoutMs = null) => AssertRow(title, present: false, timeoutMs);

        /// <summary>Waits until the row with <paramref name="title"/> shows the pending marker, or does not.</summary>
        public TodoRows AssertPending(string title, bool expected = true, int? timeoutMs = null)
        {
            Row(title, timeoutMs).Pending.AssertExists(expected, timeoutMs: timeoutMs);
            return this;
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
