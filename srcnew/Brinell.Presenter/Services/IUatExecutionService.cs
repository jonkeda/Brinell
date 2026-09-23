using Brinell.Uat;

namespace Brinell.Presenter.Services;

public interface IUatExecutionService
{
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
    private bool _disposed;

    public PresenterUatExecutionSession(
        UatStepExecutionSession stepSession,
        UatScenarioRunner runner,
        UatBoundScenario scenario,
        UatCommandCatalog catalog,
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

    public UatStepExecutionSession StepSession { get; private set; }

    public UatScenarioRunner Runner { get; private set; }

    public UatBoundScenario Scenario { get; private set; }

    public UatCommandCatalog Catalog { get; private set; }

    public string DiscoveryReport { get; }

    public string CommandCatalogReport { get; }

    public string AutPlacementReport { get; }

    public int CompletedStepCount => StepSession.Results.Count;

    public bool HasNext => StepSession.HasNext;

    public Task<UatStepResult> RunNextAsync(CancellationToken cancellationToken)
    {
        return StepSession.RunNextAsync(cancellationToken);
    }

    public UatScenarioRunResult ToResult()
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
        Scenario = null!;
        Catalog = null!;

        _resolver?.Dispose();
        _disposed = true;
    }
}
