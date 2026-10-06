using Brinell.Bdd.Runner;
using Brinell.Uat;

namespace Brinell.Presenter.Services;

/// <summary>
/// Runs a suite's scenarios in a runner process started in the suite's output (AD-025), one
/// session a scenario. Presenter loads none of the suite's code for a run.
/// </summary>
public sealed class BddExecutionService : IBddExecutionService, IDisposable
{
    private readonly object _lock = new();
    private readonly RunnerJob _job = new();
    private readonly List<Task> _ending = [];
    private Idle? _idle;
    private int _runs;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> WindowEnvironment { get; set; } = new Dictionary<string, string>();

    public async Task<PresenterUatExecutionSession> CreateSessionAsync(
        string workspacePath,
        string scenarioFilePath,
        string scenarioName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);
        cancellationToken.ThrowIfCancellationRequested();

        var configPath = BddFileFormat.FindConfig(workspacePath);
        if (!File.Exists(configPath))
        {
            throw new InvalidOperationException($"UAT config was not found: {configPath}");
        }

        var config = BddConfigParser.ParseFile(configPath);
        ValidateConfig(config, configPath);
        var pages = ResolvePagesAssembly(workspacePath, config);
        var start = new BddRunnerStart(
            pages,
            BddWorkspaceConfigInspector.ResolveWorkingDirectory(workspacePath, config),
            CreateTargetEnvironment(config, BddWorkspaceConfigInspector.ResolveConfiguredAppPath(workspacePath, config), WindowEnvironment));

        var runner = await TakeOrStartAsync(start, cancellationToken).ConfigureAwait(false);
        try
        {
            var opened = await runner.RequestAsync(new BddRunnerOpen(configPath, pages, scenarioFilePath, scenarioName), cancellationToken).ConfigureAwait(false);
            return opened switch
            {
                BddRunnerOpened ready => new PresenterUatExecutionSession(runner, ready, (ended, closing) => Release(ended, start, closing)),
                BddRunnerFailed failed => throw new InvalidOperationException(failed.Message),
                _ => throw new BddRunnerException($"The runner answered '{opened.GetType().Name}' to an open request."),
            };
        }
        catch
        {
            await runner.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public IDisposable BeginRun()
    {
        lock (_lock)
        {
            _runs++;
        }

        return new Run(this);
    }

    /// <inheritdoc />
    public Task EndRunnersAsync()
    {
        EndIdle();
        lock (_lock)
        {
            return Task.WhenAll(_ending);
        }
    }

    /// <summary>Ends the runner that waits for a next session, and every runner when Presenter closes.</summary>
    public void Dispose()
    {
        EndIdle();
        _job.Dispose();
    }

    // The runner a run's previous scenario left, when it was started the same way; else a new one.
    private async Task<BddRunnerClient> TakeOrStartAsync(BddRunnerStart start, CancellationToken cancellationToken)
    {
        Idle? idle;
        lock (_lock)
        {
            idle = _idle;
            _idle = null;
        }

        if (idle is not null)
        {
            if (idle.Matches(start) && !idle.Runner.Process.HasExited)
            {
                return idle.Runner;
            }

            await idle.Runner.DisposeAsync().ConfigureAwait(false);
        }

        var runner = await BddRunnerClient.StartAsync(start, cancellationToken).ConfigureAwait(false);
        _job.Add(runner.Process);
        return runner;
    }

    // A session ended. In a run of several scenarios its runner waits for the next one; otherwise
    // it ends, once the scenario is closed, and the suite's files are free again.
    private void Release(BddRunnerClient runner, BddRunnerStart start, Task closing)
    {
        Idle? replaced = null;
        bool keep;
        lock (_lock)
        {
            keep = _runs > 0 && !runner.Process.HasExited;
            if (keep)
            {
                replaced = _idle;
                _idle = new Idle(runner, start);
            }
        }

        End(replaced?.Runner, Task.CompletedTask);
        if (keep)
        {
            _ = ObserveAsync(closing);
        }
        else
        {
            End(runner, closing);
        }
    }

    private void EndRun()
    {
        bool last;
        lock (_lock)
        {
            last = --_runs == 0;
        }

        if (last)
        {
            EndIdle();
        }
    }

    private void EndIdle()
    {
        Idle? idle;
        lock (_lock)
        {
            idle = _idle;
            _idle = null;
        }

        End(idle?.Runner, Task.CompletedTask);
    }

    private void End(BddRunnerClient? runner, Task closing)
    {
        if (runner is null)
        {
            return;
        }

        var ending = EndAsync(runner, closing);
        lock (_lock)
        {
            _ending.RemoveAll(task => task.IsCompleted);
            _ending.Add(ending);
        }
    }

    private static async Task EndAsync(BddRunnerClient runner, Task closing)
    {
        await ObserveAsync(closing).ConfigureAwait(false);
        await runner.DisposeAsync().ConfigureAwait(false);
    }

    // A scenario that could not be closed nicely is closed with its runner.
    private static async Task ObserveAsync(Task closing)
    {
        try
        {
            await closing.ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is BddRunnerException or OperationCanceledException or ObjectDisposedException or IOException)
        {
        }
    }

    private sealed record Idle(BddRunnerClient Runner, BddRunnerStart Start)
    {
        public bool Matches(BddRunnerStart start)
            => string.Equals(Start.PagesAssembly, start.PagesAssembly, StringComparison.OrdinalIgnoreCase)
               && string.Equals(Start.WorkingDirectory, start.WorkingDirectory, StringComparison.OrdinalIgnoreCase)
               && Same(Start.Environment, start.Environment);

        private static bool Same(IReadOnlyDictionary<string, string>? left, IReadOnlyDictionary<string, string>? right)
            => (left?.Count ?? 0) == (right?.Count ?? 0)
               && (left ?? new Dictionary<string, string>()).All(pair => right!.TryGetValue(pair.Key, out var value) && value == pair.Value);
    }

    private sealed class Run(BddExecutionService service) : IDisposable
    {
        private int _ended;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _ended, 1) == 0)
            {
                service.EndRun();
            }
        }
    }

    private static void ValidateConfig(BddConfig config, string configPath)
    {
        if (!config.Runtime.TryGetValue("Target", out var target) ||
            !BddTargetRegistry.TryGet(target, out _))
        {
            throw new InvalidOperationException(
                $"UAT config '{configPath}' must set Runtime Target to one of: {BddTargetRegistry.SupportedTargetList}.");
        }

        if (!config.Runtime.TryGetValue("Fixture", out var fixture) ||
            string.IsNullOrWhiteSpace(fixture))
        {
            throw new InvalidOperationException(
                $"UAT config '{configPath}' must set Runtime Fixture.");
        }

        // A Project row replaces the Pages registration, so either is enough.
        if (config.Assemblies.Count == 0 &&
            !config.Runtime.ContainsKey(BddConfigFields.Project))
        {
            throw new InvalidOperationException(
                $"UAT config '{configPath}' must set Runtime Project or register at least one assembly.");
        }
    }

    // What the runner, and so the app it starts, gets beside Presenter's own environment.
    private static Dictionary<string, string> CreateTargetEnvironment(
        BddConfig config,
        string? appPath,
        IReadOnlyDictionary<string, string> windowEnvironment)
    {
        // How the app's window is shown; Presenter places it itself once it is open
        // (PresenterWindowService), for every target.
        Dictionary<string, string> environment = new(windowEnvironment, StringComparer.OrdinalIgnoreCase);

        // Only an explicit override travels to the environment. With no AppPath the fixture
        // resolves the app itself, which is what every fixture base already requires of it.
        if (appPath is not null)
        {
            environment[BddTargetRegistry.GetRequired(config).AppPathEnvironmentVariable] = appPath;
        }

        // The recorders a suite does not reference itself (Brinell.Narrator.Windows) are Presenter's.
        environment.TryAdd(Brinell.Recording.RecordingPackages.Variable, AppContext.BaseDirectory);
        return environment;
    }

    /// <summary>
    /// The Pages assembly: an explicit registration when there is one, otherwise the output
    /// of the project named by <c>Runtime Project</c>.
    /// </summary>
    private static string ResolvePagesAssembly(string workspacePath, BddConfig config)
    {
        var registration = config.Assemblies.FirstOrDefault(assembly =>
            assembly.Kind.Equals("Pages", StringComparison.OrdinalIgnoreCase));
        if (registration is not null)
        {
            // As written, then by name below the solution, then as a suite Presenter itself was built with.
            var hosted = Path.Combine(AppContext.BaseDirectory, Path.GetFileName(registration.Assembly));
            return BddWorkspaceConfigInspector.ResolveAssemblyPath(workspacePath, registration.Assembly)
                   ?? (File.Exists(hosted)
                       ? hosted
                       : throw new InvalidOperationException($"Assembly '{registration.Assembly}' was not found. Build the UAT page-object project first."));
        }

        var inspection = BddWorkspaceConfigInspector.Inspect(workspacePath);
        if (inspection.Project is { PagesAssemblyPath.Length: > 0 } project)
        {
            return project.PagesAssemblyExists
                ? project.PagesAssemblyPath
                : throw new InvalidOperationException(
                    $"{Path.GetFileNameWithoutExtension(project.ResolvedPath)} is not built. " +
                    $"Its output belongs at {project.PagesAssemblyPath}.");
        }

        throw new InvalidOperationException(
            "UAT config must set Runtime Project or register a Pages assembly.");
    }
}
