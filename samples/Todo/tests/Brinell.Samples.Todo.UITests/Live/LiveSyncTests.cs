using System.Net;
using Brinell.Samples.Todo.UITests.Pages;

namespace Brinell.Samples.Todo.UITests.Live;

/// <summary>
/// TOD.07, live: the app and the real Todo API, over the real wire, checked through the API.
/// </summary>
/// <remarks>
/// <para>
/// Only what a fake cannot prove: that the real server accepts what the app sends, and that the app
/// understands what the real server answers. Failures, retries and edge cases stay in the hermetic
/// and integration tiers, where the server can be told to fail.
/// </para>
/// <para>
/// Each test arranges its own todo with a unique title, and checks the server through its own HTTP
/// client rather than the app's.
/// </para>
/// </remarks>
[Collection(LiveApiCollection.Name)]
[Trait("Category", "UITest")]
[Trait("Module", "Todo")]
[Trait("Pyramid", "UiLive")]
[Trait("Tag", "live-api")]
public sealed class LiveSyncTests
{
    private readonly LiveTodoAppFixture _fixture;

    public LiveSyncTests(LiveAppHolder holder)
    {
        _fixture = holder.App;
        _fixture.StartTest();
    }

    [LiveApiFact]
    [Trait("Journey", "TOD.07.3")]
    public async Task ATodoCreatedOnTheServer_AppearsInTheAppAfterSync()
    {
        var title = LiveTodoAppFixture.Unique("From another device");
        await _fixture.Api.PutAsync(_fixture.NewTodo(title));

        _fixture.List.Sync().Todos.AssertRow(title, timeoutMs: TestConstants.PageTimeoutMs);
    }

    [LiveApiFact]
    [Trait("Journey", "TOD.07.4")]
    public async Task ATodoCreatedInTheApp_IsOnTheServerAfterSync()
    {
        var title = LiveTodoAppFixture.Unique("Created in the app");
        var list = _fixture.List.CreateTodo(title);
        Assert.Null(await _fixture.Api.FindAsync(title));

        list.Sync().Todos.AssertPending(title, expected: false, timeoutMs: TestConstants.PageTimeoutMs);

        var stored = await _fixture.Api.FindAsync(title);
        Assert.NotNull(stored);
        Assert.Equal(Contracts.TodoStatusDto.Open, stored.Status);
    }

    [LiveApiFact]
    [Trait("Journey", "TOD.07.5")]
    public async Task ATodoDeletedInTheApp_IsGoneFromTheServerAfterSync()
    {
        var todo = _fixture.NewTodo(LiveTodoAppFixture.Unique("Delete me"));
        await _fixture.Api.PutAsync(todo);
        _fixture.List.Sync().Todos.AssertRow(todo.Title, timeoutMs: TestConstants.PageTimeoutMs);

        Assert.Equal(HttpStatusCode.OK, await _fixture.Api.StatusOfAsync(todo.Id));

        var detail = _fixture.List.Open(todo.Title);
        detail.DeleteButton.Click();
        detail.Dialog.DialogButton("Delete").Click();
        TodoListPage.Arrived(_fixture.Context).Sync();

        await _fixture.Api.WaitUntilAsync(
            async () => await _fixture.Api.StatusOfAsync(todo.Id) == HttpStatusCode.NotFound,
            $"'{todo.Title}' deleted (404)");
    }
}
