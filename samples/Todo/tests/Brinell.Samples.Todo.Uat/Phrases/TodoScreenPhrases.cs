using Brinell.Core.Testing;
using Brinell.Samples.Todo.Contracts;
using Brinell.Samples.Todo.UITests;
using Brinell.Samples.Todo.UITests.Containers;
using Brinell.Samples.Todo.UITests.Controls;
using Brinell.Samples.Todo.UITests.Pages;
using Brinell.Samples.Todo.Uat.Runtime;
using Brinell.Uat;

namespace Brinell.Samples.Todo.Uat.Phrases;

/// <summary>
/// Steps the page graph cannot say alone: one row of the list, and arranging the fake server,
/// the network and the app process.
/// </summary>
/// <remarks>
/// Each step delegates to a page object, control or fixture member; nothing here reaches the driver.
/// </remarks>
[UatPhraseClass]
public sealed class TodoScreenPhrases(TodoUatFixture fixture) : UatPhraseClassBase
{
    private TodoList Todos => fixture.List.Todos;

    /// <summary>Opens the row's detail page, as tapping the row does. The phrase is the name: I open the todo {title}.</summary>
    [UatWhen]
    public void I_open_the_todo_TITLE(string title) => fixture.List.Open(title);

    /// <summary>The list shows a row for each title: <c>the list should show "A", "B" and "C"</c>.</summary>
    [UatThen]
    public void The_list_should_show_TITLES(string[] titles)
    {
        foreach (var title in titles)
        {
            Todos.AssertRow(title, timeoutMs: TestConstants.PageTimeoutMs);
        }
    }

    /// <summary>The row shows this status name: Open, In progress, Done or Overdue.</summary>
    [UatThen("the todo {title} should show status {status}")]
    public void RowShowsStatus(string title, string status) => Row(title).Status.AssertStatus(status);

    /// <summary>The row's status cannot be changed there: it has no Next button.</summary>
    [UatThen("the todo {title} should have a read-only status")]
    public void RowStatusIsReadOnly(string title) => Row(title).Status.AssertChangeable(false);

    /// <summary>The row shows "Due …".</summary>
    [UatThen("the todo {title} should show a due date")]
    public void RowShowsDueDate(string title) => Row(title).Due.AssertTextStartsWith("Due ");

    /// <summary>The row shows no due date.</summary>
    [UatThen("the todo {title} should show no due date")]
    public void RowShowsNoDueDate(string title) => Row(title).Due.AssertShown(false);

    /// <summary>The row shows the "not synced yet" marker.</summary>
    [UatThen("the todo {title} should be waiting to sync")]
    public void RowIsPending(string title) => Todos.AssertPending(title, timeoutMs: TestConstants.PageTimeoutMs);

    /// <summary>The row no longer shows the "not synced yet" marker.</summary>
    [UatThen("the todo {title} should be synced")]
    public void RowIsSynced(string title) => Todos.AssertPending(title, expected: false, timeoutMs: TestConstants.PageTimeoutMs);

    /// <summary>Every request for the todo list fails with this status until the next scenario.</summary>
    [UatGiven("the server fails with status {status}")]
    public void ServerFails(int status) => fixture.Backend.Get(TodoApi.TodosPath).AtPriority(1).Fails(status);

    /// <summary>The next request for the todo list fails with this status; later ones succeed.</summary>
    [UatGiven("the server fails once with status {status}")]
    public void ServerFailsOnce(int status)
        => fixture.Backend.Get(TodoApi.TodosPath).AtPriority(1).FailsFirst(1, status).ReturnsJson(fixture.Server.Todos);

    /// <summary>The todo list comes back only after this many seconds.</summary>
    [UatGiven("the server answers after {seconds} seconds")]
    public void ServerIsSlow(int seconds)
        => fixture.Backend.Get(TodoApi.TodosPath).AtPriority(1).WithDelay(TimeSpan.FromSeconds(seconds)).ReturnsJson(fixture.Server.Todos);

    /// <summary>Drops the failures arranged above: the server answers normally again, with the todos it held.</summary>
    [UatWhen("the server recovers")]
    public void ServerRecovers()
    {
        var todos = fixture.Server.Todos.ToList();
        fixture.ResetBackend();
        foreach (var todo in todos)
        {
            fixture.Server.Store(todo);
        }
    }

    /// <summary>The device loses its network.</summary>
    [UatGiven("the device is offline")]
    public void Offline() => fixture.Network.SetOffline();

    /// <summary>The device has its network back.</summary>
    [UatWhen("the device is back online")]
    public void Online() => fixture.Network.SetOnline();

    /// <summary>Closes the app and starts it again on the same database and backend.</summary>
    [UatWhen("I restart the app")]
    public void RestartApp(UatExecutionContext context)
    {
        fixture.Restart();
        fixture.RescopeAfterRestart(context);
    }

    /// <summary>Restarts the app pointed at an address nothing listens on; the next scenario restores it.</summary>
    [UatWhen("I restart the app with the server unreachable")]
    public void RestartUnreachable(UatExecutionContext context)
    {
        fixture.RestartWith(settings => settings with { ApiBaseUrl = "http://127.0.0.1:1" });
        fixture.RescopeAfterRestart(context);
    }

    private TodoRow Row(string title) => Todos.AssertRow(title, timeoutMs: TestConstants.PageTimeoutMs).Row(title);
}
