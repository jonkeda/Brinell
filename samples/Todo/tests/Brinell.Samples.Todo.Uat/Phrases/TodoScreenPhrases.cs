using Brinell.Core.Testing;
using Brinell.Samples.Todo.Contracts;
using Brinell.Samples.Todo.Uat.Runtime;
using Brinell.Uat;

namespace Brinell.Samples.Todo.Uat.Phrases;

/// <summary>
/// Steps the page graph cannot say alone: arranging the fake server, the network and the app
/// process.
/// </summary>
/// <remarks>
/// Each step delegates to a page object, control or fixture member; nothing here reaches the driver.
/// </remarks>
[UatPhraseClass]
public sealed class TodoScreenPhrases(TodoUatFixture fixture) : UatPhraseClassBase
{
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

    /// <summary>The device loses its network.</summary>
    [UatGiven("the device is offline")]
    public void Offline() => fixture.Network.SetOffline();

    /// <summary>The device has its network back.</summary>
    [UatWhen("the device is back online")]
    public void Online() => fixture.Network.SetOnline();

    /// <summary>Closes the app and starts it again on the same database and backend.</summary>
    [UatWhen("I restart the app")]
    public void RestartApp(UatExecutionContext context)
    {
        fixture.Restart();
        fixture.RescopeAfterRestart(context);
    }

    /// <summary>Restarts the app pointed at an address nothing listens on; the next scenario restores it.</summary>
    [UatWhen("I restart the app with the server unreachable")]
    public void RestartUnreachable(UatExecutionContext context)
    {
        fixture.RestartWith(settings => settings with { ApiBaseUrl = "http://127.0.0.1:1" });
        fixture.RescopeAfterRestart(context);
    }
}
