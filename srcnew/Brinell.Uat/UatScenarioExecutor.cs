namespace Brinell.Uat;

public sealed record UatScenarioExecutionResult(
    UatScenarioRunResult Result,
    UatExecutionContext Context,
    string? EvidencePath);

public static class UatScenarioExecutor
{
    public static async Task<UatScenarioExecutionResult> RunAsync(
        UatBoundScenario scenario,
        UatConfig? config = null,
        Action<UatBoundScenario>? beforeScenario = null,
        Func<UatBoundScenario, string?>? captureEvidenceOnFailure = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        beforeScenario?.Invoke(scenario);

        var runner = new UatScenarioRunner();
        var result = config is null
            ? await runner.RunAsync(scenario, cancellationToken).ConfigureAwait(false)
            : await runner.RunAsync(scenario, config, cancellationToken: cancellationToken).ConfigureAwait(false);

        string? evidencePath = null;
        if (!result.Passed &&
            !result.Skipped &&
            config?.Reporting.ScreenshotOnFailure == true &&
            captureEvidenceOnFailure is not null)
        {
            evidencePath = captureEvidenceOnFailure(scenario);
        }

        return new UatScenarioExecutionResult(result, runner.Context, evidencePath);
    }
}
