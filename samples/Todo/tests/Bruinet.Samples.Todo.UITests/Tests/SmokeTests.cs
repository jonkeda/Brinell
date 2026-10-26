using Bruinet.Samples.Todo.UITests.Containers;
using Bruinet.Samples.Todo.UITests.Controls;

namespace Bruinet.Samples.Todo.UITests.Tests;

/// <summary>
/// The fast gate: the app starts against its hermetic backend, shows the list, and opens a todo.
/// </summary>
/// <remarks>
/// Deep on the critical path, nothing else. If these fail, nothing else in the UI tiers is worth
/// running.
/// </remarks>
[Collection(ThreeTodosCollection.Name)]
[Trait("Category", "UITest")]
[Trait("Module", "Todo")]
[Trait("Pyramid", "UiHermetic")]
[Trait("Gate", "Smoke")]
public sealed class SmokeTests : IDisposable
{
    private readonly ThreeTodosFixture _fixture;

    // The test\'s SQL replay session (BRUINET_SQL_REPLAY), ended in the test it belongs to.
    public void Dispose() => _fixture.EndTest();

    public SmokeTests(ThreeTodosFixture fixture, Xunit.Abstractions.ITestOutputHelper output)
    {
        _fixture = fixture;
        fixture.StartTest(output);
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.01.1")]
    public Task App_Starts_AndShowsTheSeededTodos()
    {
        var list = _fixture.List;

        list.State.AssertShowing(TodoListState.Content, timeoutMs: TestConstants.PageTimeoutMs);
        list.Todos
            .AssertRow("Buy milk")
            .AssertRow("Book dentist")
            .AssertRow("File taxes");
        list.LastSync.AssertTextStartsWith("Synced");

        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.03.1")]
    public Task Row_WhenTapped_OpensItsDetail()
    {
        var detail = _fixture.List.Open("Buy milk");

        detail.Title.AssertText("Buy milk");
        detail.Status.AssertStatus(StatusText.Open);
        detail.Notes.AssertText("Oat, not dairy");

        detail.Back();
        return Task.CompletedTask;
    }
}
