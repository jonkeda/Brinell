using System.Threading.Channels;
using Brinell.Bdd.Runner;
using Brinell.Core.Testing;
using Nerdbank.Streams;
using StreamJsonRpc;
using Xunit;

namespace Brinell.Bdd.LanguageServer.Tests;

/// <summary>
/// Step through on the server (1.3 of the debugger plan): a suite whose fixture is in this test
/// project, so its runner starts from this project's output, as a built suite's does.
/// </summary>
public sealed class StepTests : IAsyncLifetime
{
    private readonly string _solution = Path.Combine(Path.GetTempPath(), "Brinell", "StepTests", Guid.NewGuid().ToString("N"));
    private readonly BddLanguageServer _server;
    private readonly Channel<object> _notes = Channel.CreateUnbounded<object>();
    private JsonRpc _client = null!;
    private JsonRpc _serverRpc = null!;

    public StepTests()
    {
        _server = new BddLanguageServer { BreakpointsPath = Path.Combine(_solution, "bdd-breakpoints.json") };
    }

    private string Suite => Path.Combine(_solution, "Counter.Uat");

    private string Feature => Path.Combine(Suite, "Features", "counter.feature.md");

    private string Steps => Path.Combine(Suite, "Features", "Counter.steps.md");

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.Combine(Suite, "Features"));
        File.WriteAllText(Path.Combine(_solution, "Counter.sln"), string.Empty);
        File.WriteAllText(Path.Combine(Suite, "uat.config.md"), $"""
            # UAT Config

            ## Runtime

            | Field | Value |
            | --- | --- |
            | Target | MAUI |
            | Fixture | {nameof(StepCounterFixture)} |

            ## Assemblies

            | Kind | Assembly |
            | --- | --- |
            | Pages | {typeof(StepCounterFixture).Assembly.Location} |
            """);
        File.WriteAllText(Feature, """
            # Feature: Counter

            ## Scenario: Counts

            ```gherkin
            Given the counter counts
            When the counter counts twice
            Then the counter has counted 3 times
            ```

            ## Scenario: Fails

            ```gherkin
            Given the counter counts
            Then the counter has counted 5 times
            Then the counter has counted 1 times
            ```
            """);
        File.WriteAllText(Steps, """
            # Steps: Counter

            ## Step: the counter counts twice

            ```gherkin
            Given the counter counts
            When the counter counts
            ```
            """);

        var (clientStream, serverStream) = FullDuplexStream.CreatePair();
        _serverRpc = _server.Attach(serverStream, serverStream);
        var formatter = new SystemTextJsonFormatter { JsonSerializerOptions = { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase } };
        _client = new JsonRpc(new HeaderDelimitedMessageHandler(clientStream, clientStream, formatter));
        _client.AddLocalRpcTarget(new Notes(_notes.Writer), new JsonRpcTargetOptions { UseSingleObjectParameterDeserialization = true });
        _client.StartListening();
        await _client.InvokeWithParameterObjectAsync<InitializeResult>("initialize", new { initializationOptions = new { solutionDir = _solution }, capabilities = new { } });
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _serverRpc.Dispose();
        _server.Dispose();
        try
        {
            Directory.Delete(_solution, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A load context not collected yet; the temp folder is cleaned by the OS.
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task OverIntoAndOut_GoThroughACall_AndTheEnd_WritesTheResult_MarkedStep()
    {
        var started = await StartAsync(3);
        Assert.True(started.Started, started.Reason);
        Assert.Equal(("Counts", "Counter.Uat"), (started.Scenario, started.Suite));
        Assert.Equal((Feature, 6, false), At(await PausedAsync()));

        Assert.Equal((Feature, 7, true), At(await MoveAsync("over")));
        var inside = await MoveAsync("into");
        Assert.Equal((Steps, 6, false), At(inside));
        Assert.Equal(1, inside.Depth);
        Assert.Equal(7, Assert.Single(inside.Calls).Line);
        Assert.Equal((Feature, 8, false), At(await MoveAsync("out")));

        await MoveOnlyAsync("over");
        var ended = await EndedAsync();
        Assert.Equal("passed", ended.Status);
        var results = Path.Combine(_solution, "TestResults");
        Assert.Single(Directory.EnumerateFiles(results, "*__result.json", SearchOption.AllDirectories));
        var manifest = File.ReadAllText(Assert.Single(Directory.EnumerateFiles(results, "manifest.json", SearchOption.AllDirectories)));
        Assert.Matches("\"mode\":\\s*\"step\"", manifest);
    }

    [Fact]
    public async Task Continue_StopsAtABreakpointInAStepFile_ThenRunsToTheEnd()
    {
        File.WriteAllText(_server.BreakpointsPath, $$"""{ "files": { {{System.Text.Json.JsonSerializer.Serialize(Steps)}}: [7] } }""");
        await StartAsync(3);
        await PausedAsync();

        Assert.Equal((Steps, 7, false), At(await MoveAsync("continue")));

        await MoveOnlyAsync("continue");
        Assert.Equal("passed", (await EndedAsync()).Status);
    }

    [Fact]
    public async Task AFailedStep_Pauses_OneRunAtATime_AndStop_EndsIt()
    {
        await StartAsync(11);
        await PausedAsync();

        var failed = await MoveAsync("continue");
        Assert.Equal((Feature, 15, false), At(failed));
        Assert.True(failed.Failed);
        Assert.Contains("5", failed.Message);
        Assert.False((await StartAsync(3)).Started);

        await MoveOnlyAsync("stop");
        Assert.Equal("stopped", (await EndedAsync()).Status);
        Assert.False((await _client.InvokeWithParameterObjectAsync<StepMoveResult>("brinell/step/move", new { move = "over" })).Taken);
    }

    [Fact]
    public async Task AStop_SaysTheStepsArguments_AndAScreenshotIsTakenWhilePaused()
    {
        Assert.Equal("No Step-through run is paused.", (await ScreenshotAsync()).Reason);
        await StartAsync(3);
        var first = await PausedAsync();
        Assert.Null(first.Arguments);
        await MoveAsync("over");

        var then = await MoveAsync("over");

        Assert.Equal((Feature, 8, false), At(then));
        Assert.Equal("3", then.Arguments!["times"]);
        var shot = await ScreenshotAsync();
        Assert.True(File.Exists(shot.Path), shot.Reason);
        Assert.Contains("paused_line8", Path.GetFileName(shot.Path), StringComparison.Ordinal);
        await MoveOnlyAsync("stop");
        Assert.Equal("stopped", (await EndedAsync()).Status);
    }

    [Fact]
    public async Task VsCodesUri_WithItsEscapedDriveColon_Starts()
    {
        var uri = new Uri(Feature).AbsoluteUri.Replace(":/", "%3A/", StringComparison.Ordinal).Replace("file%3A/", "file:/", StringComparison.Ordinal);
        Assert.Contains("%3A", uri);

        var started = await _client.InvokeWithParameterObjectAsync<StepStartResult>("brinell/step/start", new { uri, line = 3 });

        Assert.True(started.Started, started.Reason);
        await PausedAsync();
        await MoveOnlyAsync("stop");
        Assert.Equal("stopped", (await EndedAsync()).Status);
    }

    [Fact]
    public async Task AHeadinglessLine_DoesNotStart()
    {
        var started = await StartAsync(6);

        Assert.False(started.Started);
        Assert.Contains("no scenario heading at line 6", started.Reason);
    }

    private Task<StepStartResult> StartAsync(int line)
        => _client.InvokeWithParameterObjectAsync<StepStartResult>("brinell/step/start", new { uri = new Uri(Feature).AbsoluteUri, line });

    private Task<StepScreenshotResult> ScreenshotAsync()
        => _client.InvokeWithParameterObjectAsync<StepScreenshotResult>("brinell/step/screenshot", new { });

    private async Task<StepPausedParams> MoveAsync(string move)
    {
        await MoveOnlyAsync(move);
        return await PausedAsync();
    }

    private async Task MoveOnlyAsync(string move)
    {
        var taken = await _client.InvokeWithParameterObjectAsync<StepMoveResult>("brinell/step/move", new { move });
        Assert.True(taken.Taken, taken.Reason);
    }

    private async Task<StepPausedParams> PausedAsync() => Assert.IsType<StepPausedParams>(await NextNoteAsync());

    private async Task<StepEndedParams> EndedAsync() => Assert.IsType<StepEndedParams>(await NextNoteAsync());

    private async Task<object> NextNoteAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        return await _notes.Reader.ReadAsync(timeout.Token);
    }

    private static (string?, int, bool) At(StepPausedParams paused)
        => (paused.At is { } at ? new Uri(at).LocalPath : null, paused.Line, paused.IsCall);

    private sealed class Notes(ChannelWriter<object> writer)
    {
        [JsonRpcMethod("brinell/step/paused")]
        public void Paused(StepPausedParams paused) => writer.TryWrite(paused);

        [JsonRpcMethod("brinell/step/ended")]
        public void Ended(StepEndedParams ended) => writer.TryWrite(ended);
    }
}

/// <summary>The fixture <see cref="StepTests"/> steps through: no app, a counter.</summary>
public sealed class StepCounterFixture
{
    private int _count;

    /// <summary>Screenshots of the counter: a file with its count, as an app's fixture has its own.</summary>
    public Brinell.Core.Interfaces.IScreenshotService ScreenshotService => new CounterShots(this);

    [BddGiven("the counter counts")]
    [BddWhen("the counter counts")]
    public void Count() => _count++;

    [BddThen("the counter has counted {times} times")]
    public void HasCounted(int times) => Assert.True(times == _count, $"The counter counted {_count} times, not {times}.");

    private sealed class CounterShots(StepCounterFixture counter) : Brinell.Core.Interfaces.IScreenshotService
    {
        public Brinell.Core.Configuration.ScreenshotSettings Settings => Brinell.Core.Configuration.ScreenshotSettings.Default;

        public string Capture(string? description = null) => Capture("Manual", "Capture", description ?? "screenshot");

        public string Capture(string testClass, string testMethod, string description)
        {
            var folder = Path.Combine(Path.GetTempPath(), "Brinell", "StepTests", "Shots");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, $"{testClass}_{testMethod}_{description}_{Guid.NewGuid():N}.txt");
            File.WriteAllText(path, $"counted {counter._count}");
            return path;
        }

        public string CaptureOnFailure(string testClass, string testMethod, Exception exception) => Capture(testClass, testMethod, "failure");
    }
}
