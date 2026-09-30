using System.Diagnostics;
using Brinell.Bdd.Runner;
using Brinell.Presenter.Services;

namespace Brinell.Presenter.Bdd.Tests.Services;

/// <summary>
/// A run must not make its own workspace unbuildable, nor grow Presenter: a session's scenario
/// runs in a runner process (AD-025), which ends with the session, or with the run it is part of.
/// This test project's own output is the suite.
/// </summary>
public sealed class RunnerSessionTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "BrinellRunnerSession", Guid.NewGuid().ToString("N"));
    private readonly string _feature;

    public RunnerSessionTests()
    {
        Directory.CreateDirectory(_root);
        _feature = Path.Combine(_root, "probe.feature.md");
        File.WriteAllText(
            _feature,
            """
            # Feature: Probe

            ## Scenario: The probe is ready

            ```gherkin
            Given the probe is ready
            ```

            ## Scenario: The probe knows its folder

            ```gherkin
            Given the probe runs in its own output
            ```
            """);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact(Timeout = 120000)]
    public async Task ASession_RunsInAProcessOfItsOwn_WhichEndsWithIt()
    {
        Config(nameof(UnloadProbeFixture));
        using var service = new BddExecutionService();

        var session = await service.CreateSessionAsync(_root, _feature, "The probe is ready", CancellationToken.None);
        var runner = Process.GetProcessById(session.RunnerProcessId);
        var step = await session.RunNextAsync(CancellationToken.None);
        session.Dispose();
        await runner.WaitForExitAsync();

        Assert.NotEqual(Environment.ProcessId, runner.Id);
        Assert.Equal(BddStepResultStatus.Passed, step.Status);
        Assert.True(session.Passed);
    }

    [Fact(Timeout = 120000)]
    public async Task ARun_KeepsOneRunnerForItsScenarios_AndEndsItWhenTheRunEnds()
    {
        Config(nameof(UnloadProbeFixture));
        using var service = new BddExecutionService();
        var run = service.BeginRun();

        var first = await service.CreateSessionAsync(_root, _feature, "The probe is ready", CancellationToken.None);
        var runner = Process.GetProcessById(first.RunnerProcessId);
        await first.RunNextAsync(CancellationToken.None);
        first.Dispose();
        var second = await service.CreateSessionAsync(_root, _feature, "The probe knows its folder", CancellationToken.None);
        var step = await second.RunNextAsync(CancellationToken.None);
        second.Dispose();

        Assert.Equal(first.RunnerProcessId, second.RunnerProcessId);
        Assert.True(step.Status == BddStepResultStatus.Passed, step.Message);
        Assert.False(runner.HasExited, "The runner waits for the run's next scenario.");

        run.Dispose();
        await runner.WaitForExitAsync();
    }

    [Fact(Timeout = 120000)]
    public async Task ASession_HasItsRunnerPaceAndRecordTheScenario()
    {
        Config(nameof(UnloadProbeFixture));
        using var service = new BddExecutionService();
        using var session = await service.CreateSessionAsync(_root, _feature, "The probe is ready", CancellationToken.None);
        List<string> said = [];
        session.Said = say => said.Add(say.Wav);
        var folder = Path.Combine(_root, "scene-01");
        var clips = new BddRunnerClips(new BddRunnerClip("Hello.", "intro.wav", 0.01), new Dictionary<int, BddRunnerClip>(), null);

        await session.RecordAsync(new BddRunnerRecord(1, "Probe", "probe.feature.md#The-probe-is-ready", folder, "hash-1", clips, 0, SpeakAloud: true));
        await session.RunNextAsync(CancellationToken.None);
        var recorded = await session.FinishRecordingAsync(CancellationToken.None);

        // The probe shows nothing to record: the scene still gets its timeline, and says why it has no video.
        var timeline = Brinell.Narrator.BddSceneTimeline.Load(recorded.Timeline!);
        Assert.Equal(("Probe", "hash-1", BddStepResultStatus.Passed), (timeline.Name, timeline.Hash, timeline.Status));
        Assert.Contains(recorded.Warnings, warning => warning.Contains("IRecordable"));
        Assert.Equal(["intro.wav"], said);
    }

    [Fact(Timeout = 120000)]
    public async Task ASuiteThatCannotStart_SaysWhy()
    {
        Config(nameof(FailingProbeFixture));
        using var service = new BddExecutionService();

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateSessionAsync(_root, _feature, "The probe is ready", CancellationToken.None));

        Assert.Contains(FailingProbeFixture.Reason, failure.Message, StringComparison.Ordinal);
    }

    private void Config(string fixture)
        => File.WriteAllText(
            Path.Combine(_root, "uat.config.md"),
            $$"""
            # UAT Config

            ## Runtime

            | Field | Value |
            | --- | --- |
            | Target | WPF |
            | Fixture | {{fixture}} |

            ## Assemblies

            | Kind | Assembly |
            | --- | --- |
            | Pages | {{Path.GetFileName(typeof(RunnerSessionTests).Assembly.Location)}} |
            """);
}

/// <summary>A fixture with nothing to launch, so the test measures the runner and nothing else.</summary>
public sealed class UnloadProbeFixture
{
    /// <summary>The one phrase the probe scenario uses.</summary>
    [BddGiven("the probe is ready")]
    public bool Ready() => true;

    /// <summary>Passes when the process runs in the suite's output, as under <c>dotnet test</c>.</summary>
    [BddGiven("the probe runs in its own output")]
    public void RunsInItsOutput()
        => Assert.Equal(
            Path.GetDirectoryName(typeof(UnloadProbeFixture).Assembly.Location),
            AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
}

/// <summary>A fixture whose type cannot be initialized, as one that reads a missing file when it loads.</summary>
public sealed class FailingProbeFixture
{
    /// <summary>What the failure says.</summary>
    public const string Reason = "The probe fixture cannot start.";

    private static readonly string Setting;

    static FailingProbeFixture() => Setting = Fail();

    /// <summary>What the fixture read when it loaded.</summary>
    public string Read => Setting;

    private static string Fail() => throw new InvalidOperationException(Reason);
}
