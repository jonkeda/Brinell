using System.Collections.Concurrent;
using System.Reflection;
using Brinell.Core.Artifacts;
using Brinell.Core.Interfaces;
using Brinell.Core.Recording;
using Brinell.Core.Settings;
using Brinell.Narrator;
using Brinell.Recording;

namespace Brinell.Uat;

public abstract class UatScenarioTestBase<TFixture>
    where TFixture : class
{
    // One recording session per suite per test process: every scenario of the run shares its scenes folder.
    private static readonly ConcurrentDictionary<string, Lazy<BddRecordingSession?>> Recordings = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Lazy<ScreenRecorders> Recorders = new(ScreenRecorders.Discover);

    protected UatScenarioTestBase(TFixture fixture)
    {
        Fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    }

    protected TFixture Fixture { get; }

    protected virtual string ConfigFilePath => BddScenarioSource.GetConfigFilePath();

    protected virtual UatRuntimeValidationOptions RuntimeValidation => UatRuntimeValidationOptions.Default;

    protected virtual BddConfig? GetRunConfig(UatRuntime runtime) => runtime.Config;

    protected virtual ITestSettingsProvider CreateSettingsProvider() => new JsonTestSettingsProvider();

    protected virtual TestSettings ResolveScenarioSettings(BddBoundScenario scenario, UatRuntime runtime)
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
        BddExecutionContext context,
        BddBoundScenario scenario,
        UatRuntime runtime)
    {
        context.SetSettings(ResolveScenarioSettings(scenario, runtime));
    }

    protected virtual void BeforeScenario(BddBoundScenario scenario)
    {
    }

    protected virtual string EvidenceTestClassName => GetType().Name;

    protected virtual string EvidenceDescription => "failure";

    protected virtual int EvidenceScenarioNameMaxLength => 80;

    protected virtual string? CaptureEvidenceOnFailure(BddBoundScenario scenario)
    {
        var screenshotService = GetScreenshotService();
        return screenshotService?.Capture(
            EvidenceTestClassName,
            FormatEvidenceScenarioName(scenario),
            EvidenceDescription);
    }

    /// <summary>
    /// What the app shows, for a recording (<c>BRINELL_RECORD</c>): the fixture, when it is
    /// <see cref="IRecordable"/>, else its first public property that is (its test context).
    /// </summary>
    protected virtual RecordingTarget? RecordingTarget
    {
        get
        {
            if (Fixture is IRecordable recordable)
            {
                return recordable.RecordingTarget;
            }

            return Fixture.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.GetIndexParameters().Length == 0 && typeof(ITestContext).IsAssignableFrom(property.PropertyType))
                .Select(property => property.GetValue(Fixture))
                .OfType<IRecordable>()
                .Select(context => context.RecordingTarget)
                .FirstOrDefault(target => target is not null);
        }
    }

    protected virtual bool IsAcceptableResult(BddScenarioExecutionResult execution) =>
        execution.Result.Passed || execution.Result.Skipped;

    protected static IEnumerable<object[]> GetScenarioFiles(
        string folderName = BddFileFormat.FeatureFolder,
        string? configurationFilter = null)
    {
        return BddScenarioSource.GetScenarioFileTheoryData(
            folderName,
            configurationFilter: configurationFilter);
    }

    /// <summary>One theory case per scenario, <c>[filePath, scenarioName]</c>; run each with <see cref="RunUatScenarioAsync"/>.</summary>
    protected static IEnumerable<object[]> GetScenarioTheoryData(
        string folderName = BddFileFormat.FeatureFolder,
        string? configurationFilter = null)
    {
        return BddScenarioSource.GetScenarioTheoryData(
            folderName,
            configurationFilter: configurationFilter);
    }

    /// <summary>
    /// Called before the first scenario of a file runs, and again whenever the file changes: the
    /// place to load what the file's metadata asks for, such as its <c>TestData</c>.
    /// </summary>
    /// <param name="document">The file, as parsed.</param>
    protected virtual void BeforeFile(BddDocument document)
    {
    }

    protected async Task RunUatFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        EnterFile(filePath);
        var context = BindUatFile(filePath);

        foreach (var scenario in context.Document.Scenarios)
        {
            await RunBoundScenarioAsync(context, scenario, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Runs one scenario of a file and asserts it passes.</summary>
    /// <param name="filePath">The scenario file.</param>
    /// <param name="scenarioName">The scenario's name, as <see cref="GetScenarioTheoryData"/> gives it.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    protected async Task RunUatScenarioAsync(
        string filePath,
        string scenarioName,
        CancellationToken cancellationToken = default)
    {
        EnterFile(filePath);
        var context = BindUatFile(filePath);
        var matches = context.Document.Scenarios.Where(scenario => scenario.Source.Name == scenarioName).ToList();
        if (matches.Count != 1)
        {
            throw new InvalidOperationException(matches.Count == 0
                ? $"'{filePath}' has no scenario named '{scenarioName}'. It has: {string.Join(", ", context.Document.Scenarios.Select(s => $"'{s.Source.Name}'"))}."
                : $"'{filePath}' has {matches.Count} scenarios named '{scenarioName}'. Give each scenario a name of its own.");
        }

        await RunBoundScenarioAsync(context, matches[0], cancellationToken).ConfigureAwait(false);
    }

    // xUnit creates a test-class instance per case, so the file last entered is kept per closed type.
    private static string? _currentFile;

    // Before binding: BeforeFile may restart the app, and binding reads the fixture's composition.
    private void EnterFile(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        if (_currentFile == fullPath || BddMarkdownParser.ParseFile(filePath).Document is not { } document)
        {
            return;
        }

        BeforeFile(document);
        _currentFile = fullPath;
    }

    private async Task RunBoundScenarioAsync(
        UatBoundFileContext context,
        BddBoundScenario scenario,
        CancellationToken cancellationToken)
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        var execution = await RunScenarioAsync(scenario, context.Runtime, cancellationToken)
            .ConfigureAwait(false);

        RecordScenarioResult(context, scenario, execution, started.Elapsed);

        if (!IsAcceptableResult(execution))
        {
            throw new InvalidOperationException(
                BddDiagnosticsFormatter.FormatResults(
                    execution.Result,
                    execution.Context,
                    context.Runtime.DiscoveryReport,
                    context.Catalog,
                    execution.EvidencePath));
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
        var started = System.Diagnostics.Stopwatch.StartNew();
        var execution = await RunScenarioAsync(scenario, context.Runtime, CancellationToken.None)
            .ConfigureAwait(false);

        RecordScenarioResult(context, scenario, execution, started.Elapsed);

        var details = BddDiagnosticsFormatter.FormatResults(
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

    /// <summary>
    /// Binds a scenario file that must fail binding, without running it, and checks the bind
    /// diagnostics contain each expected text.
    /// </summary>
    /// <param name="filePath">The scenario file.</param>
    /// <param name="expectedDiagnostics">Texts the formatted bind diagnostics must contain.</param>
    protected void RunExpectedBindFailureUatFile(
        string filePath,
        params string[] expectedDiagnostics)
    {
        var file = BddScenarioFiles.Load(filePath);
        if (!file.Success)
        {
            throw new InvalidOperationException(
                BddDiagnosticsFormatter.FormatDiagnostics(file.Diagnostics));
        }

        var runtime = CreateRuntime();
        var catalog = CreateCommandCatalog(runtime);
        var bind = file.Bind(runtime.CreateCatalogSet(catalog));
        if (bind.Success)
        {
            throw new InvalidOperationException($"Expected '{filePath}' to fail binding, but it bound.");
        }

        var details = BddDiagnosticsFormatter.FormatBindFailure(bind.Diagnostics, runtime.DiscoveryReport, catalog);
        foreach (var expected in expectedDiagnostics.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (!details.Contains(expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Expected bind diagnostics to contain '{expected}'.{Environment.NewLine}{details}");
            }
        }
    }

    protected virtual UatRuntime CreateRuntime()
    {
        return new UatRuntime(Fixture, ConfigFilePath, RuntimeValidation);
    }

    protected virtual BddCommandCatalog CreateCommandCatalog(UatRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        return runtime.CreateCommandCatalog();
    }

    protected virtual Task<BddScenarioExecutionResult> RunScenarioAsync(
        BddBoundScenario scenario,
        UatRuntime runtime,
        CancellationToken cancellationToken)
    {
        var scope = runtime.CreateScope();
        return RunScenarioWithScopeAsync(scenario, runtime, scope, cancellationToken);
    }

    private async Task<BddScenarioExecutionResult> RunScenarioWithScopeAsync(
        BddBoundScenario scenario,
        UatRuntime runtime,
        IDisposable? scope,
        CancellationToken cancellationToken)
    {
        using (scope)
        {
            var session = Recordings.GetOrAdd(runtime.ConfigDirectory, directory => new Lazy<BddRecordingSession?>(() => BddRecordingSession.FromEnvironment(directory))).Value;
            await using var recording = session is null ? null : await RecordingFor(session, scenario, cancellationToken).ConfigureAwait(false);
            var execution = await BddScenarioExecutor.RunAsync(
                scenario,
                GetRunConfig(runtime),
                BeforeScenario,
                context =>
                {
                    runtime.ConfigureContext(context);
                    ConfigureScenarioContext(context, scenario, runtime);
                    if (scope is Microsoft.Extensions.DependencyInjection.IServiceScope serviceScope)
                    {
                        UatRuntime.ConfigureScope(context, serviceScope.ServiceProvider);
                    }

                    if (recording is not null)
                    {
                        context.Pacer = recording;
                    }
                },
                CaptureEvidenceOnFailure,
                cancellationToken).ConfigureAwait(false);
            if (recording is not null)
            {
                await session!.FinishAsync(recording, cancellationToken).ConfigureAwait(false);
            }

            return execution;
        }
    }

    // The scene this scenario records under BRINELL_RECORD; null when the run does not record it.
    private async Task<BddSceneRecording?> RecordingFor(BddRecordingSession session, BddBoundScenario scenario, CancellationToken cancellationToken)
    {
        if (scenario.Source.Source.FilePath is not { } featureFile)
        {
            return null;
        }

        var scene = await session.SceneForAsync(featureFile, scenario.Source.Name, cancellationToken).ConfigureAwait(false);
        return scene is null ? null : new BddSceneRecording(scene, () => RecordingTarget, Recorders.Value);
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

    protected virtual string FormatEvidenceScenarioName(BddBoundScenario scenario)
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
        BddBoundScenario scenario,
        BddScenarioExecutionResult execution,
        TimeSpan duration)
    {
        try
        {
            var status = GetScenarioStatus(execution.Result);
            var directory = context.Runtime.Config.Reporting.OutputDirectory;
            Directory.CreateDirectory(directory);
            var file = scenario.Source.Source.FilePath;
            var reportPath = Path.Combine(directory, UatScenarioReport.FileName(file, FormatEvidenceScenarioName(scenario)));
            var trace = context.Runtime.Config.Reporting.IncludeRuntimeTrace
                ? new UatReportTrace(context.Catalog, context.Runtime.DiscoveryReport)
                : null;

            File.WriteAllText(reportPath, UatScenarioReport.Build(scenario, execution, status, duration, context.Runtime.ConfigDirectory, trace));

            // The suite, file and line let a tool find the newest run of a file's scenarios (P5, AD-007).
            TestArtifactManifestWriter.RecordArtifact(
                reportPath,
                "uat-scenario",
                scenario.Source.Name,
                status,
                new Dictionary<string, string?>
                {
                    ["target"] = context.Runtime.Config.Runtime.TryGetValue("Target", out var target) ? target : null,
                    ["fixture"] = context.Runtime.Config.Runtime.TryGetValue("Fixture", out var fixture) ? fixture : null,
                    ["evidencePath"] = execution.EvidencePath,
                    ["suite"] = GetType().Assembly.GetName().Name,
                    ["file"] = file is null ? null : Path.GetRelativePath(context.Runtime.ConfigDirectory, Path.GetFullPath(file)).Replace('\\', '/'),
                    ["line"] = scenario.Source.Source.LineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                });
        }
        catch
        {
            // Report writing should not change scenario outcome.
        }
    }
    private static string GetScenarioStatus(BddScenarioRunResult result)
    {
        if (result.Passed)
        {
            return "passed";
        }

        if (result.Skipped)
        {
            return "skipped";
        }

        return result.Steps.Any(step => step.Status == BddStepResultStatus.Canceled)
            ? "canceled"
            : "failed";
    }

    private static string? GetScenarioId(BddBoundScenario scenario)
    {
        return scenario.Source.Tags
            .Select(BddTagConventions.NormalizeTag)
            .FirstOrDefault(tag => tag.StartsWith("uat-", StringComparison.OrdinalIgnoreCase));
    }

    private UatBoundFileContext BindUatFile(string filePath)
    {
        var file = BddScenarioFiles.Load(filePath);
        if (!file.Success)
        {
            throw new InvalidOperationException(
                BddDiagnosticsFormatter.FormatDiagnostics(file.Diagnostics));
        }

        var runtime = CreateRuntime();
        var catalog = CreateCommandCatalog(runtime);
        var bind = file.Bind(runtime.CreateCatalogSet(catalog));
        if (!bind.Success || bind.Document is null)
        {
            throw new InvalidOperationException(
                BddDiagnosticsFormatter.FormatBindFailure(
                    bind.Diagnostics,
                    runtime.DiscoveryReport,
                    catalog));
        }

        return new UatBoundFileContext(runtime, catalog, bind.Document);
    }

    private sealed record UatBoundFileContext(
        UatRuntime Runtime,
        BddCommandCatalog Catalog,
        BddBoundDocument Document);
}
