using Brinell.Core.Testing;
using Brinell.Uat;

namespace Brinell.Samples.Todo.TestSupport.Uat;

/// <summary>
/// Steps about the device the app runs on: its network. The same steps for every head: each head's
/// fixture is an <see cref="ITodoArrangement"/>.
/// </summary>
[UatPhraseClass(Namespace = "Device")]
public sealed class DevicePhrases(ITodoArrangement fixture) : UatPhraseClassBase
{
    /// <summary>The device loses its network.</summary>
    [UatGiven("the device is offline")]
    public void Offline() => fixture.Network.SetOffline();

    /// <summary>The device has its network back.</summary>
    [UatWhen("the device is back online")]
    public void Online() => fixture.Network.SetOnline();
}
