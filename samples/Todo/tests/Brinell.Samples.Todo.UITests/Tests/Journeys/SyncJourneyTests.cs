using System.Diagnostics;
using Brinell.Samples.Todo.Contracts;
using Brinell.Samples.Todo.UITests.Containers;
using Xunit.Abstractions;

namespace Brinell.Samples.Todo.UITests.Tests.Journeys;

/// <summary>TOD.07, TOD.08 and TOD.09, hermetic: sync, failures, offline, restart.</summary>
/// <remarks>
/// <para>
/// The backend's behaviour is arranged per test on the WireMock fake: a 500, a failure that
/// recovers, or nothing at all when offline. Outgoing requests are asserted by waiting for them
/// (<c>Backend.WaitForRequest</c>), never by counting straight after a click.
/// </para>
/// <para>
/// Only the screen here. What the sync keeps, retries and purges is proven in the integration tier.
/// </para>
/// </remarks>
[Collection(ThreeTodosCollection.Name)]
[Trait("Category", "UITest")]
[Trait("Module", "Todo")]
[Trait("Pyramid", "UiHermetic")]
public sealed class SyncJourneyTests
{
    private const string AnyTodo = TodoApi.TodosPath + "/*";

    private readonly ThreeTodosFixture _fixture;
    private readonly ITestOutputHelper _output;

    public SyncJourneyTests(ThreeTodosFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        fixture.StartTest();
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.07.2")]
    public Task Sync_SendsTheNewTodo_AndClearsItsMarker()
    {
        var title = Unique("Order paint");
        var list = _fixture.List.CreateTodo(title);
        list.Todos.AssertPending(title);

        list.Sync();

        var put = _fixture.Backend.WaitForRequest("PUT", AnyTodo, request => request.BodyAs<TodoDto>(TodoApi.JsonOptions)?.Title == title);
        Assert.Equal(TodoApi.DevelopmentApiKey, put.Headers[TodoApi.ApiKeyHeader]);
        list.Todos.AssertPending(title, expected: false);
        Assert.Contains(_fixture.Server.Todos, todo => todo.Title == title);
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.08.1")]
    public Task ServerError_ShowsTheErrorState_AndTheTodosComeBackAfterIt()
    {
        _fixture.Backend.Get(TodoApi.TodosPath).AtPriority(1).Fails(500);

        var list = _fixture.List.Sync();

        list.State.AssertShowing(TodoListState.Error, timeoutMs: TestConstants.PageTimeoutMs);
        list.State.ErrorLabel.AssertTextContains("server had a problem");
        list.LastSync.AssertText("Sync failed");

        _fixture.ResetBackend();
        list.State.Retry.Click();
        list.State.AssertShowing(TodoListState.Content, timeoutMs: TestConstants.PageTimeoutMs);
        list.Todos.AssertRow("Buy milk").AssertRow("Book dentist").AssertRow("File taxes");
        return Task.CompletedTask;
    }

    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Journey", "TOD.08.2")]
    public Task Retry_AfterTheServerRecovers_ShowsTheContent()
    {
        _fixture.Backend.Get(TodoApi.TodosPath).AtPriority(1).FailsFirst(1, 503).ReturnsJson(_fixture.Server.Todos);

        var list = _fixture.List.Sync();
        list.State.AssertShowing(TodoListState.Error, timeoutMs: TestConstants.PageTimeoutMs);

        list.State.Retry.Click();

        list.State.AssertShowing(TodoListState.Content, timeoutMs: TestConstants.PageTimeoutMs);
        return Task.CompletedTask;
    }

    [WindowsOnlyFact(WindowsOnlyFactAttribute.NetworkSwitch)]
    [Trait("Journey", "TOD.08.8")]
    public Task Offline_SyncIsDisabled_ChangesWait_AndGoOutWhenBackOnline()
    {
        var title = Unique("Buy stamps");
        _fixture.Network.SetOffline();

        var list = _fixture.List;
        list.LastSync.AssertText("Offline");
        list.SyncButton.AssertEnabled(false);

        list.CreateTodo(title).Todos.AssertPending(title);
        Assert.Empty(_fixture.Backend.Requests());

        _fixture.Network.SetOnline();
        list.SyncButton.AssertEnabled(true);
        list.Sync();

        _fixture.Backend.WaitForRequest("PUT", AnyTodo, request => request.BodyAs<TodoDto>(TodoApi.JsonOptions)?.Title == title);
        list.Todos.AssertPending(title, expected: false);
        return Task.CompletedTask;
    }

    [WindowsOnlyFact(WindowsOnlyFactAttribute.Reinstall)]
    [Trait("Journey", "TOD.09.3")]
    public Task Todos_SurviveARestart()
    {
        var title = Unique("Survives restart");
        _fixture.List.CreateTodo(title).Todos.AssertRow(title);

        var clock = Stopwatch.StartNew();
        var list = _fixture.Restart();
        _output.WriteLine($"Restart to a settled list: {clock.ElapsedMilliseconds} ms");

        list.Todos.AssertRow(title);
        return Task.CompletedTask;
    }

    private static string Unique(string title) => $"{title} {Guid.NewGuid().ToString("N")[..6]}";
}
