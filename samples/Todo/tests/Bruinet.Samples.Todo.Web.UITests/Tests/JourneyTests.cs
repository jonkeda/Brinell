using Bruinet.Samples.Todo.Web.UITests.Pages;

namespace Bruinet.Samples.Todo.Web.UITests.Tests;

/// <summary>
/// The TOD journeys on the web head, hermetic: the same subtasks the MAUI UI tier proves, one
/// example each, through Html page objects only.
/// </summary>
[Collection(ThreeTodosWebCollection.Name)]
[Trait("Category", "UITest")]
[Trait("Module", "Todo")]
[Trait("Pyramid", "UiHermetic")]
[Trait("Head", "Web")]
[Spike.MeasureDatabaseShare]
public sealed class JourneyTests : IDisposable
{
    private readonly ThreeTodosWebFixture _fixture;
    private readonly TodoListPage _list;

    public JourneyTests(ThreeTodosWebFixture fixture, Xunit.Abstractions.ITestOutputHelper output)
    {
        _fixture = fixture;
        _list = fixture.StartTest(output);
    }

    // The test's SQL replay session (BRUINET_SQL_REPLAY), ended in the test it belongs to.
    public void Dispose() => _fixture.EndTest();

    private static string Unique(string title) => $"{title} {Guid.NewGuid().ToString("N")[..6]}";

    [Fact]
    [Trait("Journey", "TOD.01.2")]
    public void Row_ShowsTitleDueDateAndStatus()
    {
        var title = _fixture.ServerHasTodo("Plan the trip", TodoStatusDto.InProgress, new DateOnly(2026, 12, 1));

        var row = _list.Sync().Todos.Row(title);

        row.Due.AssertTextStartsWith("Due ");
        row.Status.AssertStatus("In progress");
        row.Status.Next.AssertExists(false);
    }

    [Fact]
    [Trait("Journey", "TOD.02.1")]
    [Trait("Rule", "TOD.02.R1")]
    public void Add_OpensAnEmptyForm_Open_WithoutADueDate()
    {
        var edit = _list.Add();

        edit.Title.AssertTextEmpty(true);
        edit.Status.AssertStatus("Open");
        edit.HasDueDate.AssertChecked(false);
        edit.DueDate.AssertEnabled(false);
        edit.TitleError.AssertExists(false);
        edit.Heading.AssertText("New todo");

        edit.CancelNew();
    }

    [Fact]
    [Trait("Journey", "TOD.02.2")]
    [Trait("Rule", "TOD.02.R10")]
    public void Save_AValidTodo_ShowsItInTheList_WaitingToSync()
    {
        var title = Unique("Walk the dog");

        _list.CreateTodo(title).Todos.AssertRow(title).AssertPending(title);
    }

    [Fact]
    [Trait("Journey", "TOD.02.3")]
    [Trait("Rule", "TOD.02.R2")]
    [Trait("Rule", "TOD.02.R4")]
    public void Save_WithoutATitle_ShowsTitleRequired_AndStays()
    {
        var edit = _list.Add().SaveExpectingError();

        edit.TitleError.AssertText("Title is required");
        edit.AssertLoaded(true);

        edit.CancelNew();
    }

    [Fact]
    [Trait("Journey", "TOD.04.2")]
    public void Save_AnEdit_UpdatesDetailAndList_AndWaitsToSync()
    {
        var title = _fixture.ServerHasTodo("Call mum");
        _list.Sync().Todos.AssertRow(title);
        var renamed = title.Replace("Call mum", "Call dad", StringComparison.Ordinal);

        var detail = _list.Open(title).Edit().EnterTitle(renamed).SaveEdit();

        detail.Title.AssertText(renamed);
        detail.SyncState.AssertText("Waiting to sync");
        detail.Back().Todos
            .AssertRow(renamed)
            .AssertRow(title, present: false)
            .AssertPending(renamed);
    }

    [Fact]
    [Trait("Journey", "TOD.04.4")]
    public void Cancel_WithChanges_AsksFirst_KeepEditingStays_DiscardLeavesItUnchanged()
    {
        var title = _fixture.ServerHasTodo("Book flights");
        _list.Sync().Todos.AssertRow(title);
        var edit = _list.Open(title).Edit().EnterTitle("Something else");

        edit.CancelButton.Click();
        edit.Dialog.Title.AssertText("Discard changes?");
        edit.Dialog.Cancel.AssertText("Keep editing");
        edit.Dialog.Cancel.Click();
        edit.DialogTitle.AssertExists(false);
        edit.Title.AssertValue("Something else");

        edit.CancelButton.Click();
        edit.Dialog.Accept.AssertText("Discard");
        edit.Dialog.Accept.Click();

        TodoDetailPage.Arrived(_fixture.Context).Title.AssertText(title);
    }

    [Fact]
    [Trait("Journey", "TOD.05.1")]
    public void Next_CyclesOpen_InProgress_Done_Open()
    {
        var title = _fixture.ServerHasTodo("Water the plants");
        var detail = _list.Sync().Open(title);

        detail.Status.AssertStatus("Open");
        detail.Status.Next.Click();
        detail.Status.AssertStatus("In progress");
        detail.Status.Next.Click();
        detail.Status.AssertStatus("Done");
        detail.Status.Next.Click();
        detail.Status.AssertStatus("Open");
        detail.SyncState.AssertText("Waiting to sync");
    }

    [Fact]
    [Trait("Journey", "TOD.06.1")]
    public void Delete_AsksFirst_ThenRemovesTheRow()
    {
        var title = _fixture.ServerHasTodo("Old receipt");
        var detail = _list.Sync().Open(title);

        detail.DeleteButton.Click();
        detail.Dialog.Title.AssertText("Delete todo?");
        detail.Dialog.Cancel.Click();
        detail.DialogTitle.AssertExists(false);
        detail.Title.AssertText(title);

        detail.Delete().Todos.AssertRow(title, present: false);
    }

    [Fact]
    [Trait("Journey", "TOD.07.2")]
    [Trait("Journey", "TOD.07.10")]
    public void Sync_PushesTheNewTodo_NamingThisClient_AndClearsTheMarker()
    {
        var title = Unique("Buy stamps");
        var list = _list.CreateTodo(title);
        list.Todos.AssertPending(title);

        list.Sync();

        var put = _fixture.Backend.WaitForRequest("PUT", TodoApi.TodosPath + "/*",
            request => request.BodyAs<TodoDto>(TodoApi.JsonOptions)?.Title == title);
        Assert.Equal(TodoWebFixture.ClientId, put.Headers[TodoApi.ClientHeader]);
        Assert.Equal(TodoApi.DevelopmentApiKey, put.Headers[TodoApi.ApiKeyHeader]);
        list.Todos.AssertPending(title, expected: false);
    }
}

/// <summary>An empty list says so, and Add stays reachable.</summary>
[Collection(EmptyWebCollection.Name)]
[Trait("Category", "UITest")]
[Trait("Module", "Todo")]
[Trait("Pyramid", "UiHermetic")]
[Trait("Head", "Web")]
[Spike.MeasureDatabaseShare]
public sealed class EmptyListTests(EmptyWebFixture fixture, Xunit.Abstractions.ITestOutputHelper output) : IDisposable
{
    public void Dispose() => fixture.EndTest();

    [Fact]
    [Trait("Journey", "TOD.01.3")]
    public void NoTodos_SaysNothingToDo_AndAddStaysReachable()
    {
        var list = fixture.StartTest(output);

        list.EmptyMessage.AssertText("Nothing to do");
        list.AddButton.AssertEnabled(true);
    }
}
