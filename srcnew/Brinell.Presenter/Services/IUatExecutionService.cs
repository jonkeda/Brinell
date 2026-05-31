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
        _fixture = fixture;
        _resolver = resolver;
        _environment = environment;
    }

    public UatStepExecutionSession StepSession { get; }

    public UatScenarioRunner Runner { get; }

    public UatBoundScenario Scenario { get; }

    public UatCommandCatalog Catalog { get; }

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

        _fixture?.Dispose();
        _environment?.Dispose();
        _resolver?.Dispose();
        _disposed = true;
    }
}
