using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Channels;
using Brinell.Bdd.Runner;
using Brinell.Narrator;
using Brinell.Recording;

namespace Brinell.Uat.Runner;

/// <summary>
/// The runner's end of the protocol: opens a scenario of the suite it was started in, runs it a
/// step at a time as a host asks, and paces or records it. See AD-025.
/// </summary>
public sealed class BddRunnerHost
{
    private static readonly Lazy<ScreenRecorders> Recorders = new(ScreenRecorders.Discover);
    private readonly BddRunnerChannel _channel;
    private readonly object _stepLock = new();
    private CancellationTokenSource? _step;
    private OpenScenario? _open;
    private BddSceneRecording? _recording;
    private BddNarrationPacer? _rehearsal;

    /// <summary>Creates the host over a connected channel.</summary>
    /// <param name="channel">The channel to the host process.</param>
    public BddRunnerHost(BddRunnerChannel channel)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    /// <summary>Says hello, then answers requests until the host says exit or goes away.</summary>
    /// <param name="cancellationToken">Ends the loop.</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await _channel.SendAsync(new BddRunnerHello(BddRunnerProtocol.Version), cancellationToken).ConfigureAwait(false);

        // Requests are answered one at a time, off the reading loop: a cancel has to be read
        // while the step it cancels is running.
        var requests = Channel.CreateUnbounded<BddRunnerMessage>();
        var worker = Task.Run(() => AnswerAsync(requests.Reader, cancellationToken), cancellationToken);
        try
        {
            while (await _channel.ReceiveAsync(cancellationToken).ConfigureAwait(false) is { } message and not BddRunnerExit)
            {
                if (message is BddRunnerCancel)
                {
                    CancelStep();
                }
                else
                {
                    requests.Writer.TryWrite(message);
                }
            }
        }
        finally
        {
            CancelStep();
            requests.Writer.TryComplete();
            await worker.ConfigureAwait(false);
            await DropRecordingAsync().ConfigureAwait(false);
            Close();
        }
    }

    private async Task AnswerAsync(ChannelReader<BddRunnerMessage> requests, CancellationToken cancellationToken)
    {
        await foreach (var request in requests.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            BddRunnerMessage answer;
            try
            {
                answer = await AnswerAsync(request).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                answer = new BddRunnerFailed(Describe(exception));
            }

            await _channel.SendAsync(answer, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<BddRunnerMessage> AnswerAsync(BddRunnerMessage request)
    {
        switch (request)
        {
            case BddRunnerOpen open:
                await DropRecordingAsync().ConfigureAwait(false);
                Close();
                _open = OpenScenario.Open(open);
                return new BddRunnerOpened(_open.Scenario.Invocations.Count, _open.Discovery, _open.Catalog, Position(_open.Session.Next));

            case BddRunnerNext:
                return await StepAsync(Opened()).ConfigureAwait(false);

            case BddRunnerSkip skip:
                var session = Opened();
                var passedOver = session.Session.Next;
                var skipped = Step(session, session.Session.SkipNext(skip.Message), passedOver);
                session.RecordIfFinished();
                return skipped;

            case BddRunnerRecord record:
                await DropRecordingAsync().ConfigureAwait(false);
                var fixture = Opened().Fixture;
                var scene = new BddSceneRequest(record.Number, record.Name, record.Scenario, record.Folder, Clips(record.Clips), TimeSpan.FromMilliseconds(record.BeatMilliseconds)) { Hash = record.Hash };
                _recording = new BddSceneRecording(scene, () => UatRecording.TargetOf(fixture), Recorders.Value, record.SpeakAloud ? SayAsync : null);
                Opened().Runner.Context.Pacer = _recording;
                return new BddRunnerDone();

            case BddRunnerRehearse rehearse:
                await DropRecordingAsync().ConfigureAwait(false);
                _rehearsal = new BddNarrationPacer(Clips(rehearse.Clips), TimeSpan.FromMilliseconds(rehearse.BeatMilliseconds), rehearse.SpeakAloud ? SayAsync : null);
                Opened().Runner.Context.Pacer = _rehearsal;
                return new BddRunnerDone();

            case BddRunnerFinishRecording:
                return await FinishRecordingAsync().ConfigureAwait(false);

            case BddRunnerDropRecording:
                await DropRecordingAsync().ConfigureAwait(false);
                return new BddRunnerDone();

            case BddRunnerClose:
                await DropRecordingAsync().ConfigureAwait(false);
                Close();
                return new BddRunnerDone();

            default:
                return new BddRunnerFailed($"The runner does not answer a '{request.GetType().Name}'.");
        }
    }

    private OpenScenario Opened()
        => _open ?? throw new InvalidOperationException("No scenario is open.");

    private async Task<BddRunnerStep> StepAsync(OpenScenario open)
    {
        var cancel = new CancellationTokenSource();
        lock (_stepLock)
        {
            _step = cancel;
        }

        try
        {
            var ran = open.Session.Next;
            var step = Step(open, await open.Session.RunNextAsync(cancel.Token).ConfigureAwait(false), ran);
            open.RecordIfFinished();
            return step;
        }
        finally
        {
            lock (_stepLock)
            {
                _step = null;
            }

            cancel.Dispose();
        }
    }

    private static BddRunnerStep Step(OpenScenario open, BddStepResult result, BddStepInvocation? ran)
        => new(
            open.Session.Results.Count - 1,
            result.Status,
            result.Message,
            open.Session.HasNext,
            [.. BddDiagnosticsFormatter.FormatSteps(open.Session.Results)],
            [.. open.Runner.Context.Diagnostics],
            Position(ran),
            Position(open.Session.Next));

    // Plain text and numbers: a host holds no engine types (AD-025).
    private static BddRunnerPosition? Position(BddStepInvocation? invocation)
        => invocation is null
            ? null
            : new BddRunnerPosition(
                invocation.Step.Source.FilePath,
                invocation.Step.Source.LineNumber,
                invocation.Step.Text,
                invocation.Call?.Depth ?? 0,
                invocation.Call?.Chain.Select(frame => new BddRunnerCall(frame.Describe(), frame.Location.FilePath, frame.Location.LineNumber, frame.Depth)).ToList() ?? []);

    private void CancelStep()
    {
        lock (_stepLock)
        {
            _step?.Cancel();
        }
    }

    private async Task<BddRunnerRecorded> FinishRecordingAsync()
    {
        if (_recording is { } recording)
        {
            _recording = null;
            ClearPacer();
            await using (recording.ConfigureAwait(false))
            {
                var timeline = await recording.FinishAsync().ConfigureAwait(false);
                return new BddRunnerRecorded(recording.Request.TimelinePath, [.. timeline.Warnings]);
            }
        }

        if (_rehearsal is { } rehearsal)
        {
            _rehearsal = null;
            ClearPacer();
            await rehearsal.FinishAsync(CancellationToken.None).ConfigureAwait(false);
        }

        return new BddRunnerRecorded(null, []);
    }

    private async Task DropRecordingAsync()
    {
        _rehearsal = null;
        ClearPacer();
        if (_recording is { } recording)
        {
            _recording = null;
            await recording.DisposeAsync().ConfigureAwait(false);
        }
    }

    private void ClearPacer()
    {
        if (_open is { } open)
        {
            open.Runner.Context.Pacer = null;
        }
    }

    private void Close()
    {
        _open?.Dispose();
        _open = null;
    }

    // The host plays the line: it has the voice's player, and a suite need not.
    private Task SayAsync(SpeechClip clip, CancellationToken cancellationToken)
        => clip.WavPath is { } wav ? _channel.SendAsync(new BddRunnerSay(wav, clip.Duration.TotalSeconds), cancellationToken) : Task.CompletedTask;

    private static BddSceneClips Clips(BddRunnerClips clips)
    {
        static SpeechClip? Clip(BddRunnerClip? clip) => clip is null ? null : new SpeechClip(clip.Text, clip.Wav, TimeSpan.FromSeconds(clip.Seconds));
        return new BddSceneClips(Clip(clips.Intro), clips.Says.ToDictionary(pair => pair.Key, pair => Clip(pair.Value)!), Clip(clips.Outro));
    }

    // Every message of the chain once, without reflection's own wrapper.
    private static string Describe(Exception exception)
    {
        List<string> messages = [];
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is not TargetInvocationException && !messages.Contains(current.Message, StringComparer.Ordinal))
            {
                messages.Add(current.Message);
            }
        }

        return messages.Count == 0 ? exception.Message : string.Join(Environment.NewLine, messages);
    }

    // A scenario with its fixture, bound and ready for its first step.
    private sealed class OpenScenario : IDisposable
    {
        private readonly IDisposable? _scope;

        private readonly UatRuntime _runtime;
        private readonly BddCommandCatalog _commands;
        private readonly string _mode;
        private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
        private bool _recorded;

        private OpenScenario(object fixture, BddScenarioRunner runner, BddBoundScenario scenario, IDisposable? scope, string discovery, string catalog, UatRuntime runtime, BddCommandCatalog commands, string mode)
        {
            Fixture = fixture;
            Runner = runner;
            Scenario = scenario;
            Session = runner.CreateSession(scenario);
            _scope = scope;
            Discovery = discovery;
            Catalog = catalog;
            _runtime = runtime;
            _commands = commands;
            _mode = mode;
        }

        public object Fixture { get; }

        public BddScenarioRunner Runner { get; }

        public BddBoundScenario Scenario { get; }

        public BddStepExecutionSession Session { get; }

        public string Discovery { get; }

        public string Catalog { get; }

        // Once its last step has run, the scenario's result is written as the gate writes it, so a
        // run from a host shows in the IDEs' preview and the BDD Explorer (D4 of the debugger plan).
        public void RecordIfFinished()
        {
            if (_recorded || Session.HasNext)
            {
                return;
            }

            _recorded = true;
            var result = Session.ToResult();
            var evidence = !result.Passed && !result.Skipped && _runtime.Config.Reporting.ScreenshotOnFailure
                ? UatScenarioResults.ScreenshotServiceOf(Fixture)?.Capture("BddRunner", UatScenarioResults.EvidenceName(Scenario), "failure")
                : null;
            UatScenarioResults.Record(
                _runtime,
                _commands,
                Scenario,
                new BddScenarioExecutionResult(result, Runner.Context, evidence),
                _clock.Elapsed,
                Fixture.GetType().Assembly.GetName().Name,
                UatScenarioResults.EvidenceName(Scenario),
                _mode);
        }

        public static OpenScenario Open(BddRunnerOpen open)
        {
            var config = BddConfigParser.ParseFile(open.Config);
            var fixture = CreateFixture(config, LoadPages(open.Pages));
            IDisposable? scope = null;
            try
            {
                // As the suite's test class does, and before binding: it may restart the app on
                // the file's test set, and binding reads the fixture's composition.
                var lifecycle = fixture as IUatScenarioLifecycle;
                if (lifecycle is not null && BddMarkdownParser.ParseFile(open.Feature).Document is { } document)
                {
                    lifecycle.BeforeFile(document);
                }

                var runtime = new UatRuntime(fixture, open.Config);
                var catalog = runtime.CreateCommandCatalog();
                var file = BddScenarioFiles.Load(open.Feature);
                if (!file.Success)
                {
                    throw new InvalidOperationException(BddDiagnosticsFormatter.FormatDiagnostics(file.Diagnostics));
                }

                var catalogReport = BddDiagnosticsFormatter.FormatCatalog(catalog);
                var bind = file.Bind(runtime.CreateCatalogSet(catalog));
                if (!bind.Success || bind.Document is null)
                {
                    throw new InvalidOperationException(string.Join(
                        Environment.NewLine,
                        BddDiagnosticsFormatter.FormatDiagnostics(bind.Diagnostics),
                        runtime.DiscoveryReport,
                        catalogReport));
                }

                var scenario = bind.Document.Scenarios.FirstOrDefault(candidate => candidate.Source.Name.Equals(open.Scenario, StringComparison.Ordinal))
                               ?? throw new InvalidOperationException($"Scenario '{open.Scenario}' was not found in {open.Feature}.");

                lifecycle?.BeforeScenario(scenario);
                var runner = new BddScenarioRunner();
                runtime.ConfigureContext(runner.Context);

                // The host pauses between steps itself; the engine's own breaks are for a debugger.
                runner.Context.Breaks = null;
                scope = runtime.CreateScope();
                if (scope is Microsoft.Extensions.DependencyInjection.IServiceScope serviceScope)
                {
                    UatRuntime.ConfigureScope(runner.Context, serviceScope.ServiceProvider);
                }

                return new OpenScenario(fixture, runner, scenario, scope, runtime.DiscoveryReport, catalogReport, runtime, catalog, open.Mode ?? UatScenarioResults.RunnerMode);
            }
            catch
            {
                scope?.Dispose();
                (fixture as IDisposable)?.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            _scope?.Dispose();
            (Fixture as IDisposable)?.Dispose();
        }

        // By name first: the suite's own deps.json knows it, and its types are then the ones the
        // suite's other assemblies see.
        private static Assembly LoadPages(string path)
        {
            try
            {
                return AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(path)));
            }
            catch (FileNotFoundException)
            {
                return AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(path));
            }
        }

        private static object CreateFixture(BddConfig config, Assembly pages)
        {
            if (!config.Runtime.TryGetValue("Fixture", out var name) || string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("The suite's config must set Runtime Fixture.");
            }

            name = name.Trim();
            var type = pages.GetTypes().FirstOrDefault(type =>
                           type is { IsAbstract: false, IsClass: true } &&
                           type.GetConstructor(Type.EmptyTypes) is not null &&
                           (type.FullName?.Equals(name, StringComparison.Ordinal) == true ||
                            type.Name.Equals(name, StringComparison.Ordinal) ||
                            type.Name.Equals(name + "Fixture", StringComparison.Ordinal)))
                       ?? throw new InvalidOperationException($"Fixture '{name}' was not found in {pages.GetName().Name}.");

            return Activator.CreateInstance(type)
                   ?? throw new InvalidOperationException($"Fixture '{type.FullName}' could not be created.");
        }
    }
}
