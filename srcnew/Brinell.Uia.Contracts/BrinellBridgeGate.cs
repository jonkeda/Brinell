namespace Brinell.Uia;

/// <summary>
/// Whether this build has a bridge, and whether it was asked to turn it on.
/// </summary>
/// <remarks>
/// <para>
/// <b>The bridge is a remotely invocable command channel into application logic.</b> UI
/// Automation has no per-caller authentication: any process at the same or higher integrity
/// level on the same desktop can enumerate the tree, find the fragment root and call the
/// pattern. There is nothing to authenticate and nobody to ask, so the bridge is off unless two
/// separate things say otherwise, and the second is only a convenience - the first is the
/// control.
/// </para>
/// <list type="number">
/// <item>
/// <b>Compile time, and this is the real one.</b> <c>BRINELL_UIA_BRIDGE</c> puts the provider
/// in the build; without it there is no provider to find, which is the only guarantee UI
/// Automation actually offers. See <c>Brinell.Uia.Bridge.props</c>.
/// </item>
/// <item>
/// <b>Run time.</b> The environment variable of the same name, set to <c>1</c> by whatever
/// launches the app under test. This exists so one build can serve development and testing;
/// it is not a security boundary on its own, because anything that can set an environment
/// variable on the app could have launched a different build of it.
/// </item>
/// </list>
/// <para>
/// <b>In the contract rather than in either end, because both ends compile it.</b> The app
/// under test reads this to decide whether to publish; the test host reads the same lines to
/// decide the same thing. Two copies of the rule would be two chances to answer differently,
/// and the symptom of disagreement is an app that publishes nothing and a client that cannot
/// say why.
/// </para>
/// <para>
/// <b>Never inside <c>#if BRINELL_UIA_BRIDGE</c>.</b> The build that has no bridge is exactly
/// the one that has to be able to explain itself.
/// </para>
/// </remarks>
public static class BrinellBridgeGate
{
    /// <summary>
    /// The environment variable that turns an instrumented build on, and the name of the
    /// compile-time constant that put the bridge in it.
    /// </summary>
    /// <remarks>
    /// One name for both gates on purpose: they are two halves of one decision, and someone who
    /// finds either should find the other.
    /// </remarks>
    public const string EnableVariable = "BRINELL_UIA_BRIDGE";

    /// <summary>Whether the assembly compiling this file contains the provider.</summary>
    /// <remarks>
    /// Per-assembly, and that is the useful reading: the app under test links these sources and
    /// gets its own answer, which is the one that decides whether the app can publish anything.
    /// </remarks>
    public static bool CompiledIn =>
#if BRINELL_UIA_BRIDGE
        true;
#else
        false;
#endif

    /// <summary>Whether the environment asked for the bridge.</summary>
    /// <remarks>
    /// <b>Exactly <c>1</c>.</b> Not "any non-empty value": <c>BRINELL_UIA_BRIDGE=0</c> and
    /// <c>=false</c> are what people write when they mean off, and a gate that reads those as on
    /// is worse than no gate, because it looks like one.
    /// </remarks>
    public static bool RequestedByEnvironment
        => string.Equals(
            Environment.GetEnvironmentVariable(EnableVariable), "1", StringComparison.Ordinal);

    /// <summary>Whether the bridge may run: compiled in, and asked for.</summary>
    public static bool IsOpen => CompiledIn && RequestedByEnvironment;

    /// <summary>
    /// Says in one line why the bridge is on or off, and what to change.
    /// </summary>
    /// <remarks>
    /// The audience is someone whose tests all failed with "nothing answers" - a symptom shared
    /// by an uninstrumented build, a missing variable, a collapsed automation tree and a crash.
    /// Naming which of those it is, in the app's own log, is the difference between a minute and
    /// an afternoon.
    /// </remarks>
    /// <returns>The reason, for the bridge log.</returns>
    public static string Explain()
    {
        if (!CompiledIn)
        {
            return "bridge OFF: compiled without " + EnableVariable
                + ", so this build contains no provider at all. Build in Debug, or with "
                + "-p:BrinellUiaBridge=true.";
        }

        if (!RequestedByEnvironment)
        {
            var value = Environment.GetEnvironmentVariable(EnableVariable);

            return "bridge OFF: compiled in, but " + EnableVariable + " is "
                + (value is null ? "not set" : $"'{value}' rather than '1'")
                + ". Nothing will be published.";
        }

        return "bridge ON: compiled in and " + EnableVariable + "=1.";
    }
}
