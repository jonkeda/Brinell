using Brinell.Samples.Todo.Contracts;
using Brinell.Samples.Todo.UITests.Containers;

namespace Brinell.Samples.Todo.UITests.Tests;

/// <summary>
/// The Reviews pass: every screen once, its key controls present by AutomationId, and a screenshot
/// for design review.
/// </summary>
/// <remarks>
/// <para>
/// Broad and shallow on purpose: no workflows, no mutations kept. Each test leaves the app on the
/// list, as it found it.
/// </para>
/// <para>
/// Not here: Brinell's gesture-bridge accessibility audit. It is a driver API, which tests do not
/// call; and this app declares no gesture verbs for it to audit, only page-level ones.
/// </para>
/// </remarks>
[Collection(ThreeTodosCollection.Name)]
[Trait("Category", "UITest")]
[Trait("Module", "Todo")]
[Trait("Pyramid", "UiHermetic")]
[Trait("Gate", "Review")]
public sealed class ReviewTests
{
    private readonly ThreeTodosFixture _fixture;

    public ReviewTests(ThreeTodosFixture fixture)
    {
        _fixture = fixture;
        fixture.StartTest();
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.10.1")]
    public Task List_ShowsItsControls()
    {
        var list = _fixture.List;

        list.SyncButton.AssertExists(true);
        list.AddButton.AssertExists(true);
        list.Filter.AssertSelectedText("All");
        list.LastSync.AssertExists(true);
        list.State.AssertShowing(TodoListState.Content, timeoutMs: TestConstants.PageTimeoutMs);
        list.Todos.Row("Buy milk").Status.AssertChangeable(false);

        _fixture.CaptureReview("list");
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.10.1")]
    public Task Detail_ShowsItsControls()
    {
        var detail = _fixture.List.Open("Book dentist");

        detail.EditButton.AssertExists(true);
        detail.DeleteButton.AssertExists(true);
        detail.Title.AssertText("Book dentist");
        detail.Status.AssertChangeable(true);
        detail.InfoCard.Header.AssertText("Details");
        detail.NotesCard.Header.AssertText("Notes");
        detail.SyncCard.Header.AssertText("Sync");
        detail.Due.AssertText("No due date");
        detail.SyncState.AssertText("Synced");

        _fixture.CaptureReview("detail");
        detail.Back();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.10.1")]
    public Task Edit_ShowsItsControls()
    {
        var edit = _fixture.List.Open("Book dentist").Edit();

        edit.SaveButton.AssertExists(true);
        edit.CancelButton.AssertExists(true);
        edit.BasicsCard.Header.AssertText("Basics");
        edit.ScheduleCard.Header.AssertText("Schedule");
        edit.Title.AssertText("Book dentist");
        edit.Notes.AssertExists(true);
        edit.HasDueDate.AssertExists(true);
        edit.DueDate.AssertExists(true);
        edit.Status.AssertChangeable(true);

        _fixture.CaptureReview("edit");
        edit.CancelEdit().Back();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.10.1")]
    public Task ErrorState_ShowsTheMessageAndRetry()
    {
        _fixture.Backend.Get(TodoApi.TodosPath).AtPriority(1).Fails(500);
        var list = _fixture.List.Sync();

        list.State.AssertShowing(TodoListState.Error, timeoutMs: TestConstants.PageTimeoutMs);
        list.State.ErrorLabel.AssertTextContains("server had a problem");
        list.State.Retry.AssertExists(true);
        _fixture.CaptureReview("error");

        _fixture.ResetBackend();
        list.State.Retry.Click();
        list.State.AssertShowing(TodoListState.Content, timeoutMs: TestConstants.PageTimeoutMs);
        return Task.CompletedTask;
    }
}

/// <summary>The Reviews pass for the one screen the three-todos app cannot show: the empty list.</summary>
[Collection(EmptyCollection.Name)]
[Trait("Category", "UITest")]
[Trait("Module", "Todo")]
[Trait("Pyramid", "UiHermetic")]
[Trait("Gate", "Review")]
public sealed class ReviewEmptyTests
{
    private readonly EmptyFixture _fixture;

    public ReviewEmptyTests(EmptyFixture fixture)
    {
        _fixture = fixture;
        fixture.StartTest();
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.10.1")]
    public Task EmptyList_SaysNothingToDo_AndKeepsAdd()
    {
        var list = _fixture.List;

        list.State.AssertShowing(TodoListState.Empty, timeoutMs: TestConstants.PageTimeoutMs);
        list.State.EmptyLabel.AssertText("Nothing to do");
        list.AddButton.AssertExists(true);

        _fixture.CaptureReview("empty");
        return Task.CompletedTask;
    }
}
