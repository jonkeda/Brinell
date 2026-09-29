using Brinell.Uat;

namespace Brinell.Presenter.Services;

public interface IBddExecutionService
{
    /// <summary>
    /// Variables set while a session's app is started: how its window is shown
    /// (<c>BRINELL_AUT_WINDOW</c>) and where (<c>BRINELL_AUT_BOUNDS</c>).
    /// </summary>
    IReadOnlyDictionary<string, string> WindowEnvironment
    {
        get => new Dictionary<string, string>();
        set { }
    }

    Task<PresenterUatExecutionSession> CreateSessionAsync(
        string workspacePath,
        string scenarioFilePath,
        string scenarioName,
        CancellationToken cancellationToken);
}

public sealed class PresenterUatExecutionSession : IDisposable
{
    private readonly IDisposable? _environment;
    private readonly IDisposable? _executionScope;
    private readonly IDisposable? _fixture;
    private readonly IDisposable? _resolver;
    private Func<Brinell.Core.Recording.RecordingTarget?> _recordingTarget = () => null;
    private bool _disposed;

    public PresenterUatExecutionSession(
        BddStepExecutionSession stepSession,
        BddScenarioRunner runner,
        BddBoundScenario scenario,
        BddCommandCatalog catalog,
        string discoveryReport,
        string commandCatalogReport,
        string autPlacementReport,
        IDisposable? executionScope,
        IDisposable? fixture,
        IDisposable? resolver,
        IDisposable? environment)
    {
        StepSession = stepSession;
        Runner = runner;
        Scenario = scenario;
        Catalog = catalog;
        DiscoveryReport = discoveryReport;
        CommandCatalogReport = commandCatalogReport;
        AutPlacementReport = autPlacementReport;
        _executionScope = executionScope;
        _fixture = fixture;
        _resolver = resolver;
        _environment = environment;
    }

    public BddStepExecutionSession StepSession { get; private set; }

    public BddScenarioRunner Runner { get; private set; }

    public BddBoundScenario Scenario { get; private set; }

    public BddCommandCatalog Catalog { get; private set; }

    public string DiscoveryReport { get; }

    public string CommandCatalogReport { get; }

    public string AutPlacementReport { get; }

    /// <summary>What the session's app shows, for a recording; asked when the recording starts.</summary>
    public Func<Brinell.Core.Recording.RecordingTarget?> RecordingTarget
    {
        get => _recordingTarget;
        init => _recordingTarget = value;
    }

    public int CompletedStepCount => StepSession.Results.Count;

    public bool HasNext => StepSession.HasNext;

    public Task<BddStepResult> RunNextAsync(CancellationToken cancellationToken)
    {
        return StepSession.RunNextAsync(cancellationToken);
    }

    /// <summary>Passes over the next step without running it (Run from here).</summary>
    /// <param name="message">Why it was skipped.</param>
    public BddStepResult SkipNext(string? message = null)
    {
        return StepSession.SkipNext(message);
    }

    public BddScenarioRunResult ToResult()
    {
        return StepSession.ToResult();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _executionScope?.Dispose();
        _fixture?.Dispose();
        _environment?.Dispose();

        // Dropped before the resolver unloads its load context. These hold the fixture and the
        // bound steps, whose types live in that context: while anything here is reachable the
        // context cannot be collected, the Pages assembly stays mapped, and the next build of
        // the workspace fails. Reading them after Dispose is not valid.
        StepSession = null!;
        Runner = null!;
        _recordingTarget = () => null;
        Scenario = null!;
        Catalog = null!;

        _resolver?.Dispose();
        _disposed = true;
    }
}
