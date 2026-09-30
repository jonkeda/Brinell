using System.Diagnostics;
using Brinell.Bdd.Runner;
using Xunit;

namespace Brinell.Uat.Tests;

/// <summary>
/// The runner process: this test project's own output is the suite, started as a host starts it,
/// and driven over its pipe.
/// </summary>
public sealed class BddRunnerProcessTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "BrinellRunnerTests", Guid.NewGuid().ToString("N"));
    private readonly string _config;
    private readonly string _feature;
    private readonly string _pages = typeof(RunnerProcessFixture).Assembly.Location;

    public BddRunnerProcessTests()
    {
        Directory.CreateDirectory(_root);
        _config = Path.Combine(_root, "uat.config.md");
        _feature = Path.Combine(_root, "runner.feature.md");
        File.WriteAllText(_config, $"""
            # UAT Config

            ## Runtime

            | Field | Value |
            | --- | --- |
            | Target | MAUI |
            | Fixture | {nameof(RunnerProcessFixture)} |

            ## Assemblies

            | Kind | Assembly |
            | --- | --- |
            | Pages | {Path.GetFileName(_pages)} |
            """);
        File.WriteAllText(_feature, """
            # Feature: The runner

            ## Scenario: Counts to two

            ```gherkin
            Given the runner counts
            When the runner counts
            Then the runner has counted 2 times
            ```

            ## Scenario: Counts wrong

            ```gherkin
            Given the runner counts
            Then the runner has counted 5 times
            Then the runner has counted 1 times
            ```

            ## Scenario: Waits

            ```gherkin
            Given the runner waits to be stopped
            ```

            ## Scenario: Knows where it runs

            ```gherkin
            Then the runner's base directory is the suite's output
            Then the runner sees RUNNER_TEST_VALUE as from-the-host
            ```
            """);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public async Task AScenario_RunsStepByStep_AndEveryStepAnswers()
    {
        await using var runner = await StartAsync();

        var opened = Assert.IsType<BddRunnerOpened>(await runner.RequestAsync(Open("Counts to two")));
        Assert.Equal(3, opened.Steps);
        Assert.Contains("Command catalog:", opened.Catalog);

        for (var index = 0; index < 3; index++)
        {
            var step = Assert.IsType<BddRunnerStep>(await runner.RequestAsync(new BddRunnerNext()));
            Assert.Equal((index, BddStepResultStatus.Passed, index < 2), (step.Index, step.Status, step.HasNext));
            Assert.Equal(index + 1, step.Report.Count(line => line.Contains("Passed", StringComparison.OrdinalIgnoreCase)));
        }

        Assert.IsType<BddRunnerDone>(await runner.RequestAsync(new BddRunnerClose()));
    }

    [Fact]
    public async Task AFailingStep_AnswersWithItsMessage_AndTheRunGoesOn()
    {
        await using var runner = await StartAsync();
        await runner.RequestAsync(Open("Counts wrong"));
        await runner.RequestAsync(new BddRunnerNext());

        var failed = Assert.IsType<BddRunnerStep>(await runner.RequestAsync(new BddRunnerNext()));
        var skipped = Assert.IsType<BddRunnerStep>(await runner.RequestAsync(new BddRunnerSkip("Not now.")));

        Assert.Equal(BddStepResultStatus.Failed, failed.Status);
        Assert.Contains("counted 1", failed.Message);
        Assert.Equal((2, BddStepResultStatus.Skipped, false), (skipped.Index, skipped.Status, skipped.HasNext));
    }

    [Fact]
    public async Task TheRunner_RunsInTheSuitesOutput_WithTheHostsEnvironment()
    {
        await using var runner = await StartAsync(new Dictionary<string, string> { ["RUNNER_TEST_VALUE"] = "from-the-host" });
        await runner.RequestAsync(Open("Knows where it runs"));

        var baseDirectory = Assert.IsType<BddRunnerStep>(await runner.RequestAsync(new BddRunnerNext()));
        var environment = Assert.IsType<BddRunnerStep>(await runner.RequestAsync(new BddRunnerNext()));

        Assert.True(baseDirectory.Status == BddStepResultStatus.Passed, baseDirectory.Message);
        Assert.True(environment.Status == BddStepResultStatus.Passed, environment.Message);
        Assert.Null(Environment.GetEnvironmentVariable("RUNNER_TEST_VALUE"));
    }

    [Fact]
    public async Task OneRunner_OpensScenarioAfterScenario_EachWithANewFixture()
    {
        await using var runner = await StartAsync();

        foreach (var _ in Enumerable.Range(0, 2))
        {
            await runner.RequestAsync(Open("Counts to two"));
            BddRunnerStep? last = null;
            for (var index = 0; index < 3; index++)
            {
                last = Assert.IsType<BddRunnerStep>(await runner.RequestAsync(new BddRunnerNext()));
            }

            Assert.True(last!.Status == BddStepResultStatus.Passed, last.Message);
        }
    }

    [Fact]
    public async Task WhatCannotBeOpened_SaysWhy()
    {
        await using var runner = await StartAsync();

        var failed = Assert.IsType<BddRunnerFailed>(await runner.RequestAsync(Open("No such scenario")));
        var next = Assert.IsType<BddRunnerFailed>(await runner.RequestAsync(new BddRunnerNext()));

        Assert.Contains("'No such scenario' was not found", failed.Message);
        Assert.Equal("No scenario is open.", next.Message);
    }

    [Fact]
    public async Task ACanceledStep_AnswersCanceled()
    {
        await using var runner = await StartAsync();
        await runner.RequestAsync(Open("Counts to two"));
        var longIntro = new BddRunnerClips(new BddRunnerClip("A long story.", null, 600), new Dictionary<int, BddRunnerClip>(), null);
        await runner.RequestAsync(new BddRunnerRehearse(longIntro, 0, SpeakAloud: false));
        using var cancel = new CancellationTokenSource();

        var running = runner.RequestAsync(new BddRunnerNext(), cancel.Token);
        cancel.Cancel();
        var step = Assert.IsType<BddRunnerStep>(await running);

        Assert.Equal(BddStepResultStatus.Canceled, step.Status);
    }

    [Fact]
    public async Task ARehearsal_PacesTheSteps_AndAnnouncesEachLine()
    {
        await using var runner = await StartAsync();
        List<string> said = [];
        runner.Said = say => said.Add(say.Wav);
        await runner.RequestAsync(Open("Counts to two"));
        var clips = new BddRunnerClips(new BddRunnerClip("Hello.", "intro.wav", 0.3), new Dictionary<int, BddRunnerClip>(), new BddRunnerClip("Bye.", "outro.wav", 0.01));

        Assert.IsType<BddRunnerDone>(await runner.RequestAsync(new BddRunnerRehearse(clips, 0, SpeakAloud: true)));
        var watch = Stopwatch.StartNew();
        await runner.RequestAsync(new BddRunnerNext());
        var first = watch.Elapsed;
        await runner.RequestAsync(new BddRunnerNext());
        await runner.RequestAsync(new BddRunnerNext());
        var finished = Assert.IsType<BddRunnerRecorded>(await runner.RequestAsync(new BddRunnerFinishRecording()));

        Assert.True(first >= TimeSpan.FromSeconds(0.3), $"The first step waited {first} for its intro.");
        Assert.Equal(["intro.wav", "outro.wav"], said);
        Assert.Null(finished.Timeline);
    }

    [Fact]
    public async Task ARecording_WithNothingToRecord_StillWritesItsTimeline()
    {
        await using var runner = await StartAsync();
        await runner.RequestAsync(Open("Counts to two"));
        var folder = Path.Combine(_root, "scene-01");
        var none = new BddRunnerClips(null, new Dictionary<int, BddRunnerClip>(), null);

        Assert.IsType<BddRunnerDone>(await runner.RequestAsync(new BddRunnerRecord(1, "Counting", "runner.feature.md#Counts-to-two", folder, "hash-1", none, 0, false)));
        for (var index = 0; index < 3; index++)
        {
            await runner.RequestAsync(new BddRunnerNext());
        }

        var recorded = Assert.IsType<BddRunnerRecorded>(await runner.RequestAsync(new BddRunnerFinishRecording()));

        Assert.Equal(Path.Combine(folder, "timeline.json"), recorded.Timeline);
        Assert.Contains("\"hash-1\"", File.ReadAllText(recorded.Timeline!));
        Assert.Contains(recorded.Warnings, warning => warning.Contains("IRecordable"));
    }

    [Fact]
    public async Task ARunnerThatDies_EndsTheRequest_WithWhatItWrote()
    {
        await using var runner = await StartAsync();
        await runner.RequestAsync(Open("Waits"));

        var running = runner.RequestAsync(new BddRunnerNext());
        runner.Process.Kill(entireProcessTree: true);
        var exception = await Assert.ThrowsAsync<BddRunnerException>(() => running);

        Assert.Contains("ended in the middle of a run", exception.Message);
    }

    [Fact]
    public void ASuiteOutputWithoutTheRunner_SaysWhichFileIsMissing()
    {
        var pages = Path.Combine(_root, "Suite.dll");
        File.WriteAllText(pages, string.Empty);
        File.WriteAllText(Path.ChangeExtension(pages, ".runtimeconfig.json"), "{}");

        var exception = Assert.Throws<FileNotFoundException>(() => new BddRunnerStart(pages).ToStartInfo("pipe"));

        Assert.Contains("Brinell.Uat.dll", exception.Message);
        Assert.Contains("reference Brinell.Uat", exception.Message);
    }

    private Task<BddRunnerClient> StartAsync(IReadOnlyDictionary<string, string>? environment = null)
        => BddRunnerClient.StartAsync(new BddRunnerStart(_pages, Environment: environment));

    private BddRunnerOpen Open(string scenario) => new(_config, _pages, _feature, scenario);
}

/// <summary>The fixture the runner tests' scenarios run on: no app, a counter.</summary>
public sealed class RunnerProcessFixture
{
    private int _count;

    [BddGiven("the runner counts")]
    [BddWhen("the runner counts")]
    public void Count() => _count++;

    [BddThen("the runner has counted {times} times")]
    public void HasCounted(int times) => Assert.True(times == _count, $"The runner counted {_count} times, not {times}.");

    [BddGiven("the runner waits to be stopped")]
    public Task WaitAsync() => Task.Delay(Timeout.Infinite);

    [BddThen("the runner's base directory is the suite's output")]
    public void BaseDirectoryIsTheOutput()
        => Assert.Equal(Path.GetDirectoryName(typeof(RunnerProcessFixture).Assembly.Location), AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));

    [BddThen("the runner sees {name} as {value}")]
    public void Sees(string name, string value) => Assert.Equal(value, Environment.GetEnvironmentVariable(name));
}
