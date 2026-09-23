using Brinell.Samples.Todo.Contracts;
using Brinell.Samples.Todo.UITests.Controls;

namespace Brinell.Samples.Todo.UITests.Tests.Journeys;

/// <summary>TOD.02 and TOD.04, hermetic: creating and editing a todo through the form.</summary>
/// <remarks>
/// Every test works on a todo of its own - created here, or put on the fake server and synced -
/// so none depends on another having run first. Validation has one example here (the error label
/// appears); the variants are unit tests.
/// </remarks>
[Collection(ThreeTodosCollection.Name)]
[Trait("Category", "UITest")]
[Trait("Module", "Todo")]
[Trait("Pyramid", "UiHermetic")]
public sealed class CreateEditJourneyTests
{
    private readonly ThreeTodosFixture _fixture;

    public CreateEditJourneyTests(ThreeTodosFixture fixture)
    {
        _fixture = fixture;
        fixture.StartTest();
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.02.1")]
    public Task Add_OpensAnEmptyForm_Open_WithoutADueDate()
    {
        var edit = _fixture.List.Add();

        edit.Title.AssertTextEmpty();
        edit.Status.AssertStatus(StatusText.Open);
        edit.HasDueDate.AssertChecked(false);
        edit.DueDate.AssertEnabled(false);
        edit.TitleError.AssertExists(false);

        edit.CancelNew();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.02.2")]
    public Task Save_AValidTodo_ShowsItInTheList_WaitingToSync()
    {
        var title = Unique("Walk the dog");

        var list = _fixture.List.CreateTodo(title);

        list.Todos.AssertRow(title).AssertPending(title);
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.02.3")]
    public Task Save_WithoutATitle_ShowsTitleRequired_AndStays()
    {
        var edit = _fixture.List.Add().SaveExpectingError();

        edit.TitleError.AssertText("Title is required");
        edit.AssertLoaded(true);

        edit.CancelNew();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.02.5")]
    public Task DueDateSwitch_EnablesThePicker_AndTheDateIsSaved()
    {
        var title = Unique("Pay the bill");
        var edit = _fixture.List.Add().EnterTitle(title);

        edit.HasDueDate.SetChecked(true);
        edit.DueDate.AssertEnabled(true);
        edit.HasDueDate.SetChecked(false);
        edit.DueDate.AssertEnabled(false);
        edit.HasDueDate.SetChecked(true);

        var list = edit.SaveNew();

        list.Todos.Row(title).Due.AssertTextStartsWith("Due ");
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.04.1")]
    public Task Edit_OpensTheFormFilledIn()
    {
        var title = _fixture.ServerHasTodo("Renew insurance", TodoStatusDto.InProgress, new DateOnly(2026, 4, 1));
        _fixture.List.Sync().Todos.AssertRow(title);

        var edit = _fixture.List.Open(title).Edit();

        edit.Title.AssertText(title);
        edit.Status.AssertStatus(StatusText.InProgress);
        edit.HasDueDate.AssertChecked(true);
        edit.DueDate.AssertEnabled(true);

        edit.CancelEdit();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.04.2")]
    public Task Save_AnEdit_UpdatesDetailAndList_AndWaitsToSync()
    {
        var title = _fixture.ServerHasTodo("Call mum");
        _fixture.List.Sync().Todos.AssertRow(title);
        var renamed = title.Replace("Call mum", "Call dad", StringComparison.Ordinal);

        var detail = _fixture.List.Open(title).Edit().EnterTitle(renamed).SaveEdit();

        detail.Title.AssertText(renamed);
        detail.SyncState.AssertText("Waiting to sync");

        detail.Back().Todos
            .AssertRow(renamed)
            .AssertRow(title, present: false)
            .AssertPending(renamed);
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.04.4")]
    public Task Cancel_WithChanges_AsksFirst_KeepEditingStays_DiscardLeavesItUnchanged()
    {
        var title = _fixture.ServerHasTodo("Book flights");
        _fixture.List.Sync().Todos.AssertRow(title);
        var edit = _fixture.List.Open(title).Edit().EnterTitle("Something else");

        edit.CancelButton.Click();
        edit.Dialog
            .AssertTitle("Discard changes?")
            .AssertButtonTextsHasItem("Discard")
            .AssertButtonTextsHasItem("Keep editing")
            .DialogButton("Keep editing").Click();
        edit.Dialog.AssertExists(false);
        edit.Title.AssertText("Something else");

        edit.CancelButton.Click();
        edit.Dialog.AssertExists(true).DialogButton("Discard").Click();

        TodoDetailPageArrived().Title.AssertText(title);
        return Task.CompletedTask;
    }

    [WindowsOnlyFact(WindowsOnlyFactAttribute.ShellTitle)]
    [Trait("Journey", "TOD.04.1")]
    public Task EditPage_Heading_NamesTheMode()
    {
        var add = _fixture.List.Add();
        add.Heading.AssertText("New todo");
        add.CancelNew();

        var title = _fixture.ServerHasTodo("Name the mode");
        _fixture.List.Sync().Todos.AssertRow(title);
        var edit = _fixture.List.Open(title).Edit();
        edit.Heading.AssertText("Edit todo");

        edit.CancelEdit().Back();
        return Task.CompletedTask;
    }

    private Pages.TodoDetailPage TodoDetailPageArrived() => Pages.TodoDetailPage.Arrived(_fixture.Context);

    private static string Unique(string title) => $"{title} {Guid.NewGuid().ToString("N")[..6]}";
}
