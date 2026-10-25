using Bruinell.Core.Testing;
using Bruinell.Bdd;

namespace Bruinell.Samples.Todo.TestSupport.Uat;

/// <summary>
/// Steps about the device the app runs on: its network. The same steps for every head: each head's
/// fixture is an <see cref="ITodoArrangement"/>.
/// </summary>
[BddStepDefinitions(Namespace = "Device")]
public sealed class DevicePhrases(ITodoArrangement fixture) : BddStepDefinitionsBase
{
    /// <summary>The device loses its network.</summary>
    [BddGiven("the device is offline")]
    public void Offline() => fixture.Network.SetOffline();

    /// <summary>The device has its network back.</summary>
    [BddWhen("the device is back online")]
    public void Online() => fixture.Network.SetOnline();
}
