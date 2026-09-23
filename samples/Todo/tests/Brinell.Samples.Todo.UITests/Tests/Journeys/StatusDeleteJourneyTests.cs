using Brinell.Samples.Todo.UITests.Controls;

namespace Brinell.Samples.Todo.UITests.Tests.Journeys;

/// <summary>TOD.05 and TOD.06, hermetic: the status control, and deleting.</summary>
/// <remarks>
/// Each test puts a todo of its own on the fake server and syncs it in, so the scenario's three
/// keep their status and nothing depends on test order.
/// </remarks>
[Collection(ThreeTodosCollection.Name)]
[Trait("Category", "UITest")]
[Trait("Module", "Todo")]
[Trait("Pyramid", "UiHermetic")]
public sealed class StatusDeleteJourneyTests
{
    private readonly ThreeTodosFixture _fixture;

    public StatusDeleteJourneyTests(ThreeTodosFixture fixture)
    {
        _fixture = fixture;
        fixture.StartTest();
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.05.1")]
    public Task Next_CyclesOpen_InProgress_Done_Open()
    {
        var detail = OpenOwnTodo("Water plants");

        detail.Status.AssertStatus(StatusText.Open).AssertGlyph("○")
            .Next.Click().AssertStatus(StatusText.InProgress).AssertGlyph("◐")
            .Next.Click().AssertStatus(StatusText.Done).AssertGlyph("●")
            .Next.Click().AssertStatus(StatusText.Open);

        detail.Back();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.05.5")]
    public Task StatusChangedOnTheDetail_IsSaved_WithoutOpeningEdit()
    {
        var title = _fixture.ServerHasTodo("Fix the bike");
        _fixture.List.Sync().Todos.AssertRow(title);
        var detail = _fixture.List.Open(title);

        detail.Status.AdvanceTo(StatusText.Done);
        detail.SyncState.AssertText("Waiting to sync");

        var list = detail.Back();
        list.Todos.Row(title).Status.AssertStatus(StatusText.Done);
        list.Todos.AssertPending(title);
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.06.1")]
    public Task Delete_AsksFirst_AndCancelKeepsTheTodo()
    {
        var detail = OpenOwnTodo("Sell the sofa");

        detail.DeleteButton.Click();
        detail.Dialog
            .AssertTitle("Delete todo?")
            .AssertButtonTextsHasItem("Delete")
            .AssertButtonTextsHasItem("Cancel")
            .DialogButton("Cancel").Click();

        detail.Dialog.AssertExists(false);
        detail.AssertLoaded(true);
        detail.Back();
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.06.2")]
    public Task Delete_Confirmed_ReturnsToTheList_WithoutTheRow()
    {
        var title = _fixture.ServerHasTodo("Cancel gym");
        _fixture.List.Sync().Todos.AssertRow(title);
        var detail = _fixture.List.Open(title);

        detail.DeleteButton.Click();
        detail.Dialog.DialogButton("Delete").Click();

        Pages.TodoListPage.Arrived(_fixture.Context).Todos.AssertRow(title, present: false);
        return Task.CompletedTask;
    }

    private Pages.TodoDetailPage OpenOwnTodo(string title)
    {
        var unique = _fixture.ServerHasTodo(title);
        _fixture.List.Sync().Todos.AssertRow(unique);
        return _fixture.List.Open(unique);
    }
}
