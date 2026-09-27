using Brinell.Core.Testing;
using Brinell.Uat;

namespace Brinell.Samples.Todo.IntegrationTests.Service.Steps;

/// <summary>
/// The app's process, below the UI: a restart throws the services away and builds them again on the
/// same database file (<see cref="TodoHost.Restart"/>). The same words as each UI head's
/// <c>AppLifecyclePhrases</c>.
/// </summary>
[UatPhraseClass(Namespace = "AppLifecycle")]
public sealed class AppLifecycleSteps(TodoServiceFixture fixture) : UatPhraseClassBase
{
    /// <summary>Restarts the app on the same database and backend.</summary>
    [UatWhen("I restart the app")]
    public void Restart() => fixture.Host.Restart();
}
