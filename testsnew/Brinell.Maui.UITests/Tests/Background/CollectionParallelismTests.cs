using Xunit;
using Xunit.Abstractions;

namespace Brinell.Maui.UITests.Tests.Background;

/// <summary>
/// Step 16: the payoff. Two sample apps on screen at once, driven by two collections.
/// </summary>
/// <remarks>
/// <para>
/// <b>What the other stage B steps bought.</b> The suite used to disable xUnit's parallelism
/// assembly-wide, and the constraint behind that was never the runner: two apps competing for one
/// foreground, one pointer and one keyboard focus queue. Steps 13 to 15 removed the suite's need
/// for all three. These two tests are the first thing that would notice if that stopped being
/// true - the second app would launch, take the foreground from the first, and the run would fail
/// somewhere else entirely.
/// </para>
/// <para>
/// <b>Two classes, one assertion, and they have to be a pair.</b> Each is in a different
/// collection, which is the only way to have two fixtures - and so two apps - alive at once. Each
/// asks whether the other's app is up. Selecting one without the other therefore fails by design,
/// after the wait: there is no third state where the question is not worth asking. Both live in
/// this namespace so <c>--filter FullyQualifiedName~Tests.Background</c> takes them together.
/// </para>
/// <para>
/// <b>Windows only, and not by omission.</b> This folder is excluded from the mobile head, which
/// serialises its collections unconditionally: two Appium sessions share one emulator. There the
/// constraint is hardware, and nothing the driver does changes it.
/// </para>
/// </remarks>
internal static class ParallelRun
{
    /// <summary>
    /// How long a side waits for the other app.
    /// </summary>
    /// <remarks>
    /// Covers launching a MAUI app and attaching a driver to it from cold, with room to spare.
    /// It is not a scheduling allowance: the other side announces itself when its fixture is
    /// built, at the start of its collection, not when its test happens to be reached.
    /// </remarks>
    internal const int MeetTimeoutMs = 60_000;

    /// <summary>Above <see cref="MeetTimeoutMs"/>, so a failure to meet reports as itself.</summary>
    internal const int FactTimeoutMs = 90_000;

    /// <summary>
    /// Asserts that the other collection's app is up at the same time as this one.
    /// </summary>
    /// <remarks>
    /// There used to be a second, opposite assertion for runs with physical input allowed, which
    /// serialised the collections behind a desktop lease. The driver no longer has such a mode, so
    /// the two apps are always expected up together.
    /// </remarks>
    /// <param name="side">The caller's side of the pair.</param>
    /// <param name="ownWindow">This side's app window, for comparison against the other's.</param>
    /// <param name="output">Where to record what was observed, so a pass is still readable.</param>
    internal static void AssertPolicyIsHonoured(string side, string ownWindow, ITestOutputHelper output)
    {
        var other = ParallelismProbe.WaitForOverlap(side, MeetTimeoutMs);

        Assert.True(
            other is not null,
            $"Waited {MeetTimeoutMs / 1000}s for the other collection's app and it never came up. "
            + "Either the collections are still being serialised - check that AssemblyInfo.cs has "
            + "DisableTestParallelization = false and MaxParallelThreads of at least 2 - or the "
            + "other side's fixture failed to launch its app, or its test was not selected by the "
            + "filter. These two tests are a pair.");

        Assert.True(
            other != ownWindow,
            $"Both collections reported the same window, {ownWindow}. That is one app being "
            + "driven twice, not two apps running side by side.");

        output.WriteLine(
            $"{side}: own window {ownWindow}, other app's window {other}, stays overlapped.");
    }
}

/// <summary>
/// The hub sample app's half of the pair. See <see cref="ParallelRun"/>.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "Parallelism")]
[Trait("Stage", "Background")]
public class HubCollectionParallelismTests
{
    private readonly MauiFixture _fixture;
    private readonly ITestOutputHelper _output;

    public HubCollectionParallelismTests(MauiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    /// <summary>
    /// The Shell app is up at the same time as this one, in a different window.
    /// </summary>
    [Fact(Timeout = ParallelRun.FactTimeoutMs)]
    public Task TheShellAppIsRunningAtTheSameTime()
    {
        // The window is read here, on the test's own thread, and the waiting is handed to
        // another: xUnit only honours Timeout on a test that returns a task, and a task it can
        // abandon is the only way the pair's deadlock guard is the runner's rather than its own.
        var ownWindow = _fixture.Context.Driver.CurrentWindowHandle;

        return Task.Run(() => ParallelRun.AssertPolicyIsHonoured(
            ParallelismProbe.Hub,
            ownWindow,
            _output));
    }
}

/// <summary>
/// The Shell sample app's half of the pair. See <see cref="ParallelRun"/>.
/// </summary>
/// <remarks>
/// <b>The only Shell test that is not skipped</b>, and it can be: everything else in that suite
/// is parked under stage G step 32, waiting on chrome the app does not draw - a flyout, a tab
/// strip. This one asks nothing of the app's contents. It needs the process to exist and the
/// driver to have attached to it, which is exactly what a second collection running in parallel
/// means, and no more.
/// </remarks>
[Collection("Shell")]
[Trait("Category", "UITest")]
[Trait("Pattern", "Parallelism")]
[Trait("Stage", "Background")]
public class ShellCollectionParallelismTests
{
    private readonly ShellFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ShellCollectionParallelismTests(ShellFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    /// <summary>
    /// The hub app is up at the same time as this one, in a different window.
    /// </summary>
    [Fact(Timeout = ParallelRun.FactTimeoutMs)]
    public Task TheHubAppIsRunningAtTheSameTime()
    {
        // The window is read here, on the test's own thread, and the waiting is handed to
        // another: xUnit only honours Timeout on a test that returns a task, and a task it can
        // abandon is the only way the pair's deadlock guard is the runner's rather than its own.
        var ownWindow = _fixture.Context.Driver.CurrentWindowHandle;

        return Task.Run(() => ParallelRun.AssertPolicyIsHonoured(
            ParallelismProbe.Shell,
            ownWindow,
            _output));
    }
}
