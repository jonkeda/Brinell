using Bruinet.Core.Testing;
using Bruinet.Samples.Todo.Uat.Runtime;
using Bruinet.Uat;

namespace Bruinet.Samples.Todo.Uat.Phrases;

/// <summary>
/// Steps about the app's process: restarting it. A restart is the head's own business, so each head
/// has this class; a phrase that changes here changes in the web one too, since the scenario files
/// are shared.
/// </summary>
[BddStepDefinitions(Namespace = "AppLifecycle")]
public sealed class AppLifecyclePhrases(TodoUatFixture fixture) : BddStepDefinitionsBase
{
    /// <summary>Closes the app and starts it again on the same database and backend.</summary>
    [BddWhen("I restart the app")]
    public void RestartApp(BddExecutionContext context)
    {
        fixture.Restart();
        fixture.RescopeAfterRestart(context);
    }

    /// <summary>Restarts the app pointed at an address nothing listens on; the next scenario restores it.</summary>
    [BddWhen("I restart the app with the server unreachable")]
    public void RestartUnreachable(BddExecutionContext context)
    {
        fixture.RestartWith(settings => settings with { ApiBaseUrl = "http://127.0.0.1:1" });
        fixture.RescopeAfterRestart(context);
    }
}
