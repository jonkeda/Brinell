using System.Reflection;
using System.Text.Json;
using Brinell.Core.Artifacts;
using Brinell.Core.Interfaces;
using Brinell.Core.Settings;

namespace Brinell.Uat;

public abstract class UatScenarioTestBase<TFixture>
    where TFixture : class
{
    private static readonly JsonSerializerOptions ReportJsonOptions = new()
    {
        WriteIndented = true
    };

    protected UatScenarioTestBase(TFixture fixture)
    {
        Fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    }

    protected TFixture Fixture { get; }

    protected virtual string ConfigFilePath => UatScenarioSource.GetConfigFilePath();

    protected virtual UatRuntimeValidationOptions RuntimeValidation => UatRuntimeValidationOptions.Default;

    protected virtual UatConfig? GetRunConfig(UatRuntime runtime) => runtime.Config;

    protected virtual ITestSettingsProvider CreateSettingsProvider() => new JsonTestSettingsProvider();

    protected virtual TestSettings ResolveScenarioSettings(UatBoundScenario scenario, UatRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(runtime);

        var settings = runtime.Config.Settings;
        return CreateSettingsProvider().Resolve(new TestSettingsRequest(
            runtime.ConfigDirectory,
            settings.Root,
            settings.DefaultFile,
            settings.LocalFile,
            GetScenarioId(scenario),
            settings.ScenarioConvention));
    }

    protected virtual void ConfigureScenarioContext(
        UatExecutionContext context,
        UatBoundScenario scenario,
        UatRuntime runtime)
    {
        context.SetSettings(ResolveScenarioSettings(scenario, runtime));
    }

    protected virtual void BeforeScenario(UatBoundScenario scenario)
    {
    }

    protected virtual string EvidenceTestClassName => GetType().Name;

    protected virtual string EvidenceDescription => "failure";

    protected virtual int EvidenceScenarioNameMaxLength => 80;

    protected virtual string? CaptureEvidenceOnFailure(UatBoundScenario scenario)
    {
        var screenshotService = GetScreenshotService();
        return screenshotService?.Capture(
            EvidenceTestClassName,
            FormatEvidenceScenarioName(scenario),
            EvidenceDescription);
    }

    protected virtual bool IsAcceptableResult(UatScenarioExecutionResult execution) =>
        execution.Result.Passed || execution.Result.Skipped;

    protected static IEnumerable<object[]> GetScenarioFiles(
        string folderName = "Scenarios",
        string? filterEnvironmentVariable = null)
    {
        return UatScenarioSource.GetScenarioFileTheoryData(
            folderName,
            filterEnvironmentVariable: filterEnvironmentVariable);
    }

    protected async Task RunUatFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var context = BindUatFile(filePath);

        foreach (var scenario in context.Document.Scenarios)
        {
            var execution = await RunScenarioAsync(scenario, context.Runtime, cancellationToken)
                .ConfigureAwait(false);

            RecordScenarioResult(context, scenario, execution);

            if (!IsAcceptableResult(execution))
            {
                throw new InvalidOperationException(
                    UatDiagnosticsFormatter.FormatResults(
                        execution.Result,
                        execution.Context,
                        context.Runtime.DiscoveryReport,
                        context.Catalog,
                        execution.EvidencePath));
            }
        }
    }

    protected async Task RunExpectedFailureUatFileAsync(
        string filePath,
        params string[] expectedDiagnostics)
    {
        var context = BindUatFile(filePath);
        if (context.Document.Scenarios.Count != 1)
        {
            throw new InvalidOperationException(
                $"Expected '{filePath}' to contain exactly one scenario, but found {context.Document.Scenarios.Count}.");
        }

        var scenario = context.Document.Scenarios[0];
        var execution = await RunScenarioAsync(scenario, context.Runtime, CancellationToken.None)
            .ConfigureAwait(false);

        RecordScenarioResult(context, scenario, execution);

        var details = UatDiagnosticsFormatter.FormatResults(
            execution.Result,
            execution.Context,
            context.Runtime.DiscoveryReport,
            context.Catalog,
            execution.EvidencePath);

        if (execution.Result.Passed)
        {
            throw new InvalidOperationException(
                $"Expected UAT scenario '{scenario.Source.Name}' to fail, but it passed.{Environment.NewLine}{details}");
        }

        foreach (var expected in expectedDiagnostics.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (!details.Contains(expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Expected diagnostics to contain '{expected}'.{Environment.NewLine}{details}");
            }
        }
    }

    protected virtual UatRuntime CreateRuntime()
    {
        return new UatRuntime(Fixture, ConfigFilePath, RuntimeValidation);
    }

    protected virtual UatCommandCatalog CreateCommandCatalog(UatRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        return runtime.CreateCommandCatalog();
    }

    protected virtual Task<UatScenarioExecutionResult> RunScenarioAsync(
        UatBoundScenario scenario,
        UatRuntime runtime,
        CancellationToken cancellationToken)
    {
        var scope = runtime.CreateScope();
        return RunScenarioWithScopeAsync(scenario, runtime, scope, cancellationToken);
    }

    private async Task<UatScenarioExecutionResult> RunScenarioWithScopeAsync(
        UatBoundScenario scenario,
        UatRuntime runtime,
        IDisposable? scope,
        CancellationToken cancellationToken)
    {
        using (scope)
        {
            return await UatScenarioExecutor.RunAsync(
                scenario,
                GetRunConfig(runtime),
                BeforeScenario,
                context =>
                {
                    ConfigureScenarioContext(context, scenario, runtime);
                    if (scope is Microsoft.Extensions.DependencyInjection.IServiceScope serviceScope)
                    {
                        UatRuntime.ConfigureScope(context, serviceScope.ServiceProvider);
                    }
                },
                CaptureEvidenceOnFailure,
                cancellationToken).ConfigureAwait(false);
        }
    }

    protected virtual IScreenshotService? GetScreenshotService()
    {
        if (Fixture is IScreenshotService screenshotService)
        {
            return screenshotService;
        }

        var property = Fixture.GetType().GetProperty(
            "ScreenshotService",
            BindingFlags.Instance | BindingFlags.Public);

        return property is not null &&
               typeof(IScreenshotService).IsAssignableFrom(property.PropertyType)
            ? property.GetValue(Fixture) as IScreenshotService
            : null;
    }

    protected virtual string FormatEvidenceScenarioName(UatBoundScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Concat(
            scenario.Source.Name.Select(ch => invalidChars.Contains(ch) ? '_' : ch));

        return sanitized.Length <= EvidenceScenarioNameMaxLength
            ? sanitized
            : sanitized[..EvidenceScenarioNameMaxLength];
    }

    private void RecordScenarioResult(
        UatBoundFileContext context,
        UatBoundScenario scenario,
        UatScenarioExecutionResult execution)
    {
        try
        {
            var status = GetScenarioStatus(execution.Result);
            var directory = context.Runtime.Config.Reporting.OutputDirectory;
            Directory.CreateDirectory(directory);
            var reportPath = Path.Combine(
                directory,
                $"{FormatEvidenceScenarioName(scenario)}__result.json");
            var includeTrace = context.Runtime.Config.Reporting.IncludeRuntimeTrace;
            object[] steps = includeTrace
                ? execution.Result.Steps.Select(step => (object)new
                {
                    status = step.Status.ToString(),
                    source = step.Invocation.Step.Source.ToString(),
                    keyword = step.Invocation.Step.EffectiveKeyword.ToString(),
                    commandId = step.Invocation.CommandId,
                    text = step.Invocation.Step.Text,
                    message = step.Message
                }).ToArray()
                : [];
            string[] diagnostics = includeTrace
                ? execution.Context.Diagnostics.ToArray()
                : [];
            object[] commandCatalog = includeTrace
                ? context.Catalog.Patterns.Select(pattern => (object)new
                {
                    keyword = pattern.Keyword.ToString(),
                    phrase = pattern.Phrase,
                    commandId = pattern.CommandId
                }).ToArray()
                : [];

            var report = new
            {
                scenario = scenario.Source.Name,
                status,
                tags = scenario.Source.Tags,
                evidencePath = execution.EvidencePath,
                skipReason = execution.Result.SkipDecision?.Reason,
                steps,
                diagnostics,
                discovery = includeTrace
                    ? context.Runtime.DiscoveryReport
                    : null,
                commandCatalog
            };

            File.WriteAllText(reportPath, JsonSerializer.Serialize(report, ReportJsonOptions));
            TestArtifactManifestWriter.RecordArtifact(
                reportPath,
                "uat-scenario",
                scenario.Source.Name,
                status,
                new Dictionary<string, string?>
                {
                    ["target"] = context.Runtime.Config.Runtime.TryGetValue("Target", out var target) ? target : null,
                    ["fixture"] = context.Runtime.Config.Runtime.TryGetValue("Fixture", out var fixture) ? fixture : null,
                    ["evidencePath"] = execution.EvidencePath
                });
        }
        catch
        {
            // Report writing should not change scenario outcome.
        }
    }

    private static string GetScenarioStatus(UatScenarioRunResult result)
    {
        if (result.Passed)
        {
            return "passed";
        }

        if (result.Skipped)
        {
            return "skipped";
        }

        return result.Steps.Any(step => step.Status == UatStepResultStatus.Canceled)
            ? "canceled"
            : "failed";
    }

    private static string? GetScenarioId(UatBoundScenario scenario)
    {
        return scenario.Source.Tags
            .Select(UatTagConventions.NormalizeTag)
            .FirstOrDefault(tag => tag.StartsWith("uat-", StringComparison.OrdinalIgnoreCase));
    }

    private UatBoundFileContext BindUatFile(string filePath)
    {
        var parse = UatMarkdownParser.ParseFile(filePath);
        if (!parse.Success || parse.Document is null)
        {
            throw new InvalidOperationException(
                UatDiagnosticsFormatter.FormatDiagnostics(parse.Diagnostics));
        }

        var runtime = CreateRuntime();
        var catalog = CreateCommandCatalog(runtime);
        var bind = UatBinder.Bind(parse.Document, catalog);
        if (!bind.Success || bind.Document is null)
        {
            throw new InvalidOperationException(
                UatDiagnosticsFormatter.FormatBindFailure(
                    bind.Diagnostics,
                    runtime.DiscoveryReport,
                    catalog));
        }

        return new UatBoundFileContext(runtime, catalog, bind.Document);
    }

    private sealed record UatBoundFileContext(
        UatRuntime Runtime,
        UatCommandCatalog Catalog,
        UatBoundDocument Document);
}
