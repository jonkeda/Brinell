using Brinell.Core.Testing;
using Brinell.Maui.Controls.Dialogs;
using Brinell.Maui.Pages;
using Brinell.Samples.Todo.Contracts;
using Brinell.Samples.Todo.UITests;
using Brinell.Samples.Todo.UITests.Containers;
using Brinell.Samples.Todo.UITests.Controls;
using Brinell.Samples.Todo.UITests.Pages;
using Brinell.Samples.Todo.Uat.Runtime;
using Brinell.Uat;

namespace Brinell.Samples.Todo.Uat.Phrases;

/// <summary>
/// Steps the page graph cannot say alone: one row of the list, the list's state, the native
/// dialog, and arranging the fake server, the network and the app process.
/// </summary>
/// <remarks>
/// Each step delegates to a page object, control or fixture member; nothing here reaches the driver.
/// </remarks>
[UatPhraseClass]
public sealed class TodoScreenPhrases(TodoUatFixture fixture) : UatPhraseClassBase
{
    private TodoList Todos => fixture.List.Todos;

    // The dialog is window chrome, the same on the edit and the detail page.
    private ContentDialog<AppRoot> Dialog => new TodoDetailPage(fixture.Context).Dialog;

    /// <summary>Opens the row's detail page, as tapping the row does.</summary>
    [UatPhrase(UatEffectiveStepKeyword.When, "I open the todo {title}")]
    public void OpenTodo(string title) => fixture.List.Open(title);

    /// <summary>The row shows this status name: Open, In progress, Done or Overdue.</summary>
    [UatPhrase(UatEffectiveStepKeyword.Then, "the todo {title} should show status {status}")]
    public void RowShowsStatus(string title, string status) => Row(title).Status.AssertStatus(status);

    /// <summary>The row's status cannot be changed there: it has no Next button.</summary>
    [UatPhrase(UatEffectiveStepKeyword.Then, "the todo {title} should have a read-only status")]
    public void RowStatusIsReadOnly(string title) => Row(title).Status.AssertChangeable(false);

    /// <summary>The row shows "Due …".</summary>
    [UatPhrase(UatEffectiveStepKeyword.Then, "the todo {title} should show a due date")]
    public void RowShowsDueDate(string title) => Row(title).Due.AssertTextStartsWith("Due ");

    /// <summary>The row shows no due date.</summary>
    [UatPhrase(UatEffectiveStepKeyword.Then, "the todo {title} should show no due date")]
    public void RowShowsNoDueDate(string title) => Row(title).Due.AssertShown(false);

    /// <summary>The row shows the "not synced yet" marker.</summary>
    [UatPhrase(UatEffectiveStepKeyword.Then, "the todo {title} should be waiting to sync")]
    public void RowIsPending(string title) => Todos.AssertPending(title, timeoutMs: TestConstants.PageTimeoutMs);

    /// <summary>The row no longer shows the "not synced yet" marker.</summary>
    [UatPhrase(UatEffectiveStepKeyword.Then, "the todo {title} should be synced")]
    public void RowIsSynced(string title) => Todos.AssertPending(title, expected: false, timeoutMs: TestConstants.PageTimeoutMs);

    /// <summary>The list shows the Loading, Empty, Error or Content state.</summary>
    [UatPhrase(UatEffectiveStepKeyword.Then, "the list should show the {state} state")]
    public void ListShowsState(string state)
    {
        var view = state switch
        {
            "Loading" => TodoListState.Loading,
            "Empty" => TodoListState.Empty,
            "Error" => TodoListState.Error,
            "Content" => TodoListState.Content,
            _ => throw new ArgumentException($"Unknown list state '{state}'. Use Loading, Empty, Error or Content.", nameof(state)),
        };

        fixture.List.State.AssertShowing(view, timeoutMs: TestConstants.PageTimeoutMs);
    }

    /// <summary>A confirmation dialog is shown with this title.</summary>
    [UatPhrase(UatEffectiveStepKeyword.Then, "the dialog should ask {title}")]
    public void DialogAsks(string title) => Dialog.AssertExists(true, timeoutMs: TestConstants.PageTimeoutMs).AssertTitle(title);

    /// <summary>Presses a button of the confirmation dialog.</summary>
    [UatPhrase(UatEffectiveStepKeyword.When, "I choose {button} in the dialog")]
    public void ChooseInDialog(string button) => Dialog.DialogButton(button).Click();

    /// <summary>No confirmation dialog is shown.</summary>
    [UatPhrase(UatEffectiveStepKeyword.Then, "no dialog should be shown")]
    public void NoDialog() => Dialog.AssertExists(false, timeoutMs: TestConstants.PageTimeoutMs);

    /// <summary>Every request for the todo list fails with this status until the next scenario.</summary>
    [UatPhrase(UatEffectiveStepKeyword.Given, "the server fails with status {status}")]
    public void ServerFails(int status) => fixture.Backend.Get(TodoApi.TodosPath).AtPriority(1).Fails(status);

    /// <summary>The next request for the todo list fails with this status; later ones succeed.</summary>
    [UatPhrase(UatEffectiveStepKeyword.Given, "the server fails once with status {status}")]
    public void ServerFailsOnce(int status)
        => fixture.Backend.Get(TodoApi.TodosPath).AtPriority(1).FailsFirst(1, status).ReturnsJson(fixture.Server.Todos);

    /// <summary>The todo list comes back only after this many seconds.</summary>
    [UatPhrase(UatEffectiveStepKeyword.Given, "the server answers after {seconds} seconds")]
    public void ServerIsSlow(int seconds)
        => fixture.Backend.Get(TodoApi.TodosPath).AtPriority(1).WithDelay(TimeSpan.FromSeconds(seconds)).ReturnsJson(fixture.Server.Todos);

    /// <summary>Drops the failures arranged above: the server answers normally again, with the todos it held.</summary>
    [UatPhrase(UatEffectiveStepKeyword.When, "the server recovers")]
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
    [UatPhrase(UatEffectiveStepKeyword.Given, "the device is offline")]
    public void Offline() => fixture.Network.SetOffline();

    /// <summary>The device has its network back.</summary>
    [UatPhrase(UatEffectiveStepKeyword.When, "the device is back online")]
    public void Online() => fixture.Network.SetOnline();

    /// <summary>Closes the app and starts it again on the same database and backend.</summary>
    [UatPhrase(UatEffectiveStepKeyword.When, "I restart the app")]
    public void RestartApp(UatExecutionContext context)
    {
        fixture.Restart();
        fixture.RescopeAfterRestart(context);
    }

    /// <summary>Restarts the app pointed at an address nothing listens on; the next scenario restores it.</summary>
    [UatPhrase(UatEffectiveStepKeyword.When, "I restart the app with the server unreachable")]
    public void RestartUnreachable(UatExecutionContext context)
    {
        fixture.RestartWith(settings => settings with { ApiBaseUrl = "http://127.0.0.1:1" });
        fixture.RescopeAfterRestart(context);
    }

    private TodoRow Row(string title) => Todos.AssertRow(title, timeoutMs: TestConstants.PageTimeoutMs).Row(title);
}
