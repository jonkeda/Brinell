using Brinell.Core.Testing;
using Brinell.Samples.Todo.Contracts;
using Brinell.Samples.Todo.Web.UITests.Pages;
using Brinell.Samples.Todo.Web.Uat.Runtime;
using Brinell.Uat;

namespace Brinell.Samples.Todo.Web.Uat.Phrases;

/// <summary>
/// The web head's version of the MAUI suite's <c>TodoScreenPhrases</c>: the same phrases, over the
/// web fixture: arranging the fake server, the network and the app process.
/// </summary>
/// <remarks>
/// Each step delegates to a page object, control or fixture member; nothing here reaches the driver.
/// A phrase that changes here changes in the MAUI class too: the scenario files are shared.
/// </remarks>
[UatPhraseClass]
public sealed class TodoScreenPhrases(TodoWebUatFixture fixture) : UatPhraseClassBase
{
    // The waits a scenario step gets: the MAUI suite's TestConstants.PageTimeoutMs.
    private const int PageTimeoutMs = 10_000;

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

    /// <summary>
    /// The fake server received the todo from that user's app: a PUT of it that carries the app's
    /// <c>X-Todo-Client</c>. A wait on the request, not a count (<c>todo-blazor.md</c> 4.1).
    /// </summary>
    [UatThen("the server should have received the todo {title} from {user}")]
    public void ServerReceivedFrom(string title, string user)
    {
        var clientId = fixture.ClientIdOfUser(user);
        fixture.Backend.WaitForRequest(
            "PUT",
            TodoApi.TodosPath + "/*",
            request => request.BodyAs<TodoDto>(TodoApi.JsonOptions)?.Title == title
                && request.Headers.TryGetValue(TodoApi.ClientHeader, out var sender)
                && sender == clientId,
            TimeSpan.FromMilliseconds(PageTimeoutMs));
    }

    /// <summary>The device loses its network.</summary>
    [UatGiven("the device is offline")]
    public void Offline() => fixture.Network.SetOffline();

    /// <summary>The device has its network back.</summary>
    [UatWhen("the device is back online")]
    public void Online() => fixture.Network.SetOnline();

    /// <summary>Stops the app and starts it again on the same database and backend.</summary>
    [UatWhen("I restart the app")]
    public void RestartApp() => fixture.Restart();

    /// <summary>Restarts the app pointed at an address nothing listens on; the next scenario restores it.</summary>
    [UatWhen("I restart the app with the server unreachable")]
    public void RestartUnreachable() => fixture.RestartWith(settings => settings with { ApiBaseUrl = "http://127.0.0.1:1" });
}
