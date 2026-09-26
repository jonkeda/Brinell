namespace Brinell.Uat;

public sealed class UatExecutionContext
{
    public string? CurrentPageName { get; set; }

    /// <summary>The app the scenario is using, after an <c>{app} uses the app</c> step; null before one.</summary>
    public string? CurrentSession { get; set; }

    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    public IList<string> Diagnostics { get; } = [];
}

public enum UatStepResultStatus
{
    Waiting,
    Running,
    Passed,
    Failed,
    Skipped,
    Canceled
}

public sealed record UatStepResult(
    UatStepResultStatus Status,
    UatStepInvocation Invocation,
    string? Message = null,
    Exception? Exception = null)
{
    public static UatStepResult Passed(UatStepInvocation invocation, string? message = null)
    {
        return new UatStepResult(UatStepResultStatus.Passed, invocation, message);
    }

    public static UatStepResult Failed(UatStepInvocation invocation, string message, Exception? exception = null)
    {
        return new UatStepResult(UatStepResultStatus.Failed, invocation, message, exception);
    }

    public static UatStepResult Canceled(UatStepInvocation invocation, string? message = null)
    {
        return new UatStepResult(UatStepResultStatus.Canceled, invocation, message);
    }
}

public sealed record UatScenarioRunResult(
    UatBoundScenario Scenario,
    IReadOnlyList<UatStepResult> Steps,
    UatSkipDecision? SkipDecision = null)
{
    public bool Skipped => SkipDecision?.ShouldSkip == true;

    public bool Passed => Steps.Count > 0 && Steps.All(x => x.Status == UatStepResultStatus.Passed);
}

public sealed class UatScenarioRunner
{
    private readonly UatExecutionContext _context;

    public UatScenarioRunner(UatExecutionContext? context = null)
    {
        _context = context ?? new UatExecutionContext();
    }

    public UatExecutionContext Context => _context;

    public UatStepExecutionSession CreateSession(UatBoundScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        return new UatStepExecutionSession(_context, scenario);
    }

    public async Task<UatScenarioRunResult> RunAsync(
        UatBoundScenario scenario,
        CancellationToken cancellationToken = default)
    {
        var session = CreateSession(scenario);
        while (session.HasNext)
        {
            var result = await session.RunNextAsync(cancellationToken).ConfigureAwait(false);
            if (result.Status is UatStepResultStatus.Failed or UatStepResultStatus.Canceled)
            {
                break;
            }
        }

        return session.ToResult();
    }

    public Task<UatScenarioRunResult> RunAsync(
        UatBoundScenario scenario,
        UatConfig config,
        Func<string, string?>? getEnvironmentVariable = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(config);

        var skipDecision = config.EvaluateSkip(scenario.Source.Tags, getEnvironmentVariable);
        return skipDecision.ShouldSkip
            ? Task.FromResult(new UatScenarioRunResult(scenario, [], skipDecision))
            : RunAsync(scenario, cancellationToken);
    }
}

public sealed class UatStepExecutionSession
{
    private readonly UatExecutionContext _context;
    private readonly List<UatStepResult> _results = [];
    private readonly UatBoundScenario _scenario;
    private int _nextIndex;

    public UatStepExecutionSession(UatExecutionContext context, UatBoundScenario scenario)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
    }

    public bool HasNext => _nextIndex < _scenario.Invocations.Count;

    public IReadOnlyList<UatStepResult> Results => _results;

    public async Task<UatStepResult> RunNextAsync(CancellationToken cancellationToken = default)
    {
        if (!HasNext)
        {
            throw new InvalidOperationException("No UAT steps remain in this execution session.");
        }

        var invocation = _scenario.Invocations[_nextIndex++];
        if (cancellationToken.IsCancellationRequested)
        {
            var canceled = UatStepResult.Canceled(invocation, "Execution was canceled before the step started.");
            _results.Add(canceled);
            return canceled;
        }

        if (invocation.Command.Handler is null)
        {
            var missingHandler = UatStepResult.Failed(
                invocation,
                $"Command '{invocation.CommandId}' does not have an execution handler.");
            _results.Add(missingHandler);
            return missingHandler;
        }

        try
        {
            var result = NameTheSession(await invocation.Command.Handler(_context, invocation, cancellationToken).ConfigureAwait(false));
            _results.Add(result);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var canceled = UatStepResult.Canceled(invocation, "Execution was canceled.");
            _results.Add(canceled);
            return canceled;
        }
        catch (Exception ex)
        {
            var failed = NameTheSession(UatStepResult.Failed(invocation, ex.Message, ex));
            _results.Add(failed);
            return failed;
        }
    }

    // In a scenario that uses several apps, a failure says which one it came from.
    private UatStepResult NameTheSession(UatStepResult result)
        => result.Status == UatStepResultStatus.Failed && _context.CurrentSession is { } session
            ? result with { Message = $"[{session}] {result.Message}" }
            : result;

    public UatScenarioRunResult ToResult()
    {
        return new UatScenarioRunResult(_scenario, _results);
    }
}
