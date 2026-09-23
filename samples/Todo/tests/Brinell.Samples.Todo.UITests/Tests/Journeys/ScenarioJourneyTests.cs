using Brinell.Samples.Todo.Contracts;
using Brinell.Samples.Todo.UITests.Containers;
using Brinell.Samples.Todo.UITests.Controls;

namespace Brinell.Samples.Todo.UITests.Tests.Journeys;

/// <summary>TOD.01.3 and TOD.08.3, hermetic: an app with no todos.</summary>
[Collection(EmptyCollection.Name)]
[Trait("Category", "UITest")]
[Trait("Module", "Todo")]
[Trait("Pyramid", "UiHermetic")]
public sealed class EmptyJourneyTests
{
    private readonly EmptyFixture _fixture;

    public EmptyJourneyTests(EmptyFixture fixture)
    {
        _fixture = fixture;
        fixture.StartTest();
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.01.3")]
    public Task NoTodos_SaysNothingToDo_AndAddStaysReachable()
    {
        var list = _fixture.List;

        list.State.AssertShowing(TodoListState.Empty, timeoutMs: TestConstants.PageTimeoutMs);
        list.State.EmptyLabel.AssertText("Nothing to do");
        list.AddButton.AssertEnabled(true);
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.08.3")]
    public Task SlowServer_ShowsLoading_UntilTheAnswerArrives()
    {
        _fixture.Backend.Get(TodoApi.TodosPath).AtPriority(1).WithDelay(TimeSpan.FromSeconds(2)).ReturnsJson(Array.Empty<TodoDto>());

        var list = _fixture.List.Sync();

        list.State.AssertShowing(TodoListState.Loading, timeoutMs: TestConstants.PageTimeoutMs);
        list.State.AssertShowing(TodoListState.Empty, timeoutMs: TestConstants.PageTimeoutMs);
        return Task.CompletedTask;
    }
}

/// <summary>TOD.01.6 and TOD.03.3, hermetic: local changes the server has not seen.</summary>
/// <remarks>The fixture does not sync on start, and no test here syncs: the changes are the subject.</remarks>
[Collection(PendingChangesCollection.Name)]
[Trait("Category", "UITest")]
[Trait("Module", "Todo")]
[Trait("Pyramid", "UiHermetic")]
public sealed class PendingChangesJourneyTests
{
    private readonly PendingChangesFixture _fixture;

    public PendingChangesJourneyTests(PendingChangesFixture fixture)
    {
        _fixture = fixture;
        fixture.StartTest();
    }

    [WindowsOnlyFact(WindowsOnlyFactAttribute.LocalState)]
    [Trait("Journey", "TOD.01.6")]
    public Task RowsWithUnsyncedChanges_ShowTheMarker()
    {
        _fixture.List.Todos
            .AssertPending("Buy oat milk")
            .AssertPending("Call plumber")
            .AssertPending("Book dentist", expected: false);
        _fixture.List.LastSync.AssertText("Not synced yet");
        return Task.CompletedTask;
    }

    [WindowsOnlyTheory(WindowsOnlyFactAttribute.LocalState)]
    [Trait("Journey", "TOD.03.3")]
    [InlineData("Book dentist", "Synced")]
    [InlineData("Buy oat milk", "Waiting to sync")]
    [InlineData("Call plumber", "Local only")]
    public Task SyncCard_SaysWhetherTheServerHasIt(string title, string expected)
    {
        var detail = _fixture.List.Open(title);

        detail.SyncState.AssertText(expected);

        detail.Back();
        return Task.CompletedTask;
    }
}

/// <summary>TOD.05.3, hermetic: past-due todos, against the pinned date of 10 March 2026.</summary>
[Collection(OverdueCollection.Name)]
[Trait("Category", "UITest")]
[Trait("Module", "Todo")]
[Trait("Pyramid", "UiHermetic")]
public sealed class OverdueJourneyTests
{
    private readonly OverdueFixture _fixture;

    public OverdueJourneyTests(OverdueFixture fixture)
    {
        _fixture = fixture;
        fixture.StartTest();
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.05.3")]
    public Task PastDueTodos_ShowOverdue_InTheListAndOnTheDetail()
    {
        var todos = _fixture.List.Todos;
        todos.Row("Renew passport").Status.AssertStatus(StatusText.Overdue).AssertGlyph("⚠");
        todos.Row("Pay rent").Status.AssertStatus(StatusText.Overdue);
        todos.Row("Water plants").Status.AssertStatus(StatusText.Done);
        todos.Row("Plan trip").Status.AssertStatus(StatusText.Open);

        var detail = _fixture.List.Open("Renew passport");
        detail.Status.AssertStatus(StatusText.Overdue).AssertChangeable(true);

        detail.Back();
        return Task.CompletedTask;
    }
}

/// <summary>TOD.09.4, hermetic: the app starts while its backend is unreachable.</summary>
[Collection(UnreachableServerCollection.Name)]
[Trait("Category", "UITest")]
[Trait("Module", "Todo")]
[Trait("Pyramid", "UiHermetic")]
public sealed class UnreachableServerJourneyTests
{
    private readonly UnreachableServerFixture _fixture;

    public UnreachableServerJourneyTests(UnreachableServerFixture fixture)
    {
        _fixture = fixture;
        fixture.StartTest();
    }

    [WindowsOnlyFact(WindowsOnlyFactAttribute.LocalState)]
    [Trait("Journey", "TOD.09.4")]
    public Task Startup_WithTheServerUnreachable_ShowsTheLocalTodos()
    {
        var list = _fixture.List;

        list.State.AssertShowing(TodoListState.Content, timeoutMs: TestConstants.PageTimeoutMs);
        list.Todos.AssertRow("Buy milk").AssertRow("Book dentist").AssertRow("File taxes");
        list.LastSync.AssertText("Server unreachable");
        return Task.CompletedTask;
    }
}
