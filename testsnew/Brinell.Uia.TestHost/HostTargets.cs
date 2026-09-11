namespace Brinell.Uia.TestHost;

/// <summary>The targets this host publishes, named where the tests can reach them.</summary>
/// <remarks>
/// Public so the harness can address a target by the same constant the host registered it
/// under. A test that spelled the id out itself would keep passing after the host renamed it.
/// </remarks>
public static class HostTargets
{
    /// <summary>The target the tests drive.</summary>
    public const string Primary = "SpikeTarget";

    /// <summary>A second target, so the fragment has siblings to navigate between.</summary>
    public const string Secondary = "SpikeSecondTarget";
}
