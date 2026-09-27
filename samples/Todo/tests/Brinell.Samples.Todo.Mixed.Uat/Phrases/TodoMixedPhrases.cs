using Brinell.Core.Testing;
using Brinell.Mocking;
using Brinell.Samples.Todo.Mixed.Uat.Runtime;
using Brinell.Samples.Todo.UITests;
using Brinell.Samples.Todo.Web.UITests;
using Brinell.Uat;

namespace Brinell.Samples.Todo.Mixed.Uat.Phrases;

/// <summary>
/// Steps about a user's app rather than its screen, the same for every head: its network, and
/// what the backend received from it.
/// </summary>
[UatPhraseClass]
public sealed class TodoMixedPhrases(TodoMixedUatFixture fixture) : UatPhraseClassBase
{
    /// <summary>The backend received the todo from the user's app: a PUT that carries its <c>X-Todo-Client</c>.</summary>
    [UatThen("the server should have received the todo {title} from {user}")]
    public void ServerReceivedFrom(string title, string user) => fixture.WaitForTodoFrom(fixture.Apps.Get(user), title);

    /// <summary>The user's app loses its network.</summary>
    [UatGiven("{user} is offline")]
    public void Offline(string user) => NetworkOf(user).SetOffline();

    /// <summary>The user's app loses its network.</summary>
    [UatWhen("{user} goes offline")]
    public void GoesOffline(string user) => NetworkOf(user).SetOffline();

    /// <summary>The user's app has its network back.</summary>
    [UatWhen("{user} is back online")]
    public void BackOnline(string user) => NetworkOf(user).SetOnline();

    private NetworkStateFile NetworkOf(string user) => fixture.Apps.Get(user) switch
    {
        TodoMauiDevice maui => maui.Network,
        TodoWebDevice web => web.Network,
        ITestApp app => throw new NotSupportedException($"{app.Definition.Name} has no network switch."),
    };
}
