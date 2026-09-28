using Brinell.Presenter.Models;
using Brinell.Uat;

namespace Brinell.Presenter.Services;

public sealed class BddWorkspaceService : IBddWorkspaceService
{
    private readonly IProjectEvaluator? _projectEvaluator;

    /// <summary>Creates the service with the shared MSBuild evaluator.</summary>
    public BddWorkspaceService()
        : this(null)
    {
    }

    /// <summary>Creates the service with a specific project evaluator.</summary>
    /// <param name="projectEvaluator">The evaluator to resolve <c>Runtime Project</c> with, or null for the default.</param>
    public BddWorkspaceService(IProjectEvaluator? projectEvaluator)
    {
        _projectEvaluator = projectEvaluator;
    }

    public string? FindDefaultWorkspace()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "testsnew", "Brinell.Maui.Uat.Tests");
            if (File.Exists(BddFileFormat.FindConfig(candidate)))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    public BddWorkspaceLoadResult LoadFolder(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        var catalog = UatSpecCommandCatalog.CreateDefault();
        List<BddFileLoadResult> files = [];
        List<BddScenarioLoadResult> scenarios = [];
        List<string> diagnostics = [];

        var configInfo = BddWorkspaceConfigInspector.Inspect(folderPath, _projectEvaluator);
        diagnostics.AddRange(configInfo.Diagnostics);

        var configPath = BddFileFormat.FindConfig(folderPath);
        var config = File.Exists(configPath) ? BddConfigParser.ParseFile(configPath) : null;
        var discoveryReport = config is not null
            ? FormatDiscoveryReport(config)
            : "Discovery report:\n- uat.config.md not found.";

        // Subject steps (Daan taps Save) bind here too; the spec catalog is the same for every head.
        var catalogs = config is null
            ? new BddCatalogSet(catalog)
            : new BddCatalogSet(catalog, config.Apps, suiteHead: null, new Dictionary<string, BddCommandCatalog>(), []);

        foreach (var filePath in EnumerateUatFiles(folderPath))
        {
            var parse = BddScenarioFiles.Load(filePath);
            var parseDiagnostics = FormatDiagnostics(parse.Diagnostics);
            BddBindResult? bind = null;

            if (parse.Document is not null)
            {
                bind = parse.Bind(catalogs);
            }

            var bindDiagnostics = bind is null ? [] : FormatDiagnostics(bind.Diagnostics);
            files.Add(new BddFileLoadResult(
                filePath,
                Path.GetFileName(filePath),
                parse.Success,
                bind?.Success == true,
                [.. parseDiagnostics, .. bindDiagnostics]));

            diagnostics.Add($"{Path.GetFileName(filePath)}: Parse: {(parse.Success ? "ok" : "error")}");
            diagnostics.Add($"{Path.GetFileName(filePath)}: Bind: {(bind?.Success == true ? "ok" : "error")}");
            diagnostics.AddRange(parseDiagnostics);
            diagnostics.AddRange(bindDiagnostics);

            if (bind?.Document is not null)
            {
                scenarios.AddRange(bind.Document.Scenarios.Select(scenario =>
                    new BddScenarioLoadResult(
                        scenario.Source.Name,
                        bind.Document.Source.Title,
                        filePath,
                        scenario.Source.Tags,
                        scenario.Invocations.Select(invocation => new BddStepLoadResult(
                            "wait",
                            invocation.Call is { } call ? $"{new string(' ', 2 * call.Depth)}{invocation.Step.Text}" : invocation.Step.Text,
                            invocation.CommandId,
                            // One row per step keeps the run pointer's index; a callable's step opens the call.
                            invocation.Call is { } frame ? frame.Chain[0].Location.LineNumber : invocation.Step.Source.LineNumber,
                            invocation.Call is { } root ? $"{root.Chain[0].Call.Keyword} {root.Chain[0].Call.Text}" : null)).ToArray())));
            }
            else if (parse.Document is not null)
            {
                scenarios.AddRange(parse.Document.Scenarios.Select(scenario =>
                    new BddScenarioLoadResult(
                        scenario.Name,
                        parse.Document.Title,
                        filePath,
                        scenario.Tags,
                        scenario.Steps.Select(step => new BddStepLoadResult(
                            "wait",
                            step.Text,
                            "(unbound)",
                            step.Source.LineNumber)).ToArray())));
            }
        }

        if (files.Count == 0)
        {
            diagnostics.Add("No .feature.md files were found.");
        }

        var workspaceName = new DirectoryInfo(folderPath).Name;
        return new BddWorkspaceLoadResult(
            folderPath,
            workspaceName,
            configInfo,
            files,
            scenarios,
            diagnostics,
            discoveryReport,
            FormatCatalog(catalog));
    }

    private static IEnumerable<string> EnumerateUatFiles(string folderPath)
    {
        return Directory
            .EnumerateFiles(folderPath, BddFileFormat.FeatureFilePattern, SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) &&
                           !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => IsExpectedFailurePath(path) ? 1 : 0)
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsExpectedFailurePath(string path)
    {
        return path.Contains(
            $"{Path.DirectorySeparatorChar}ExpectedFailures{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> FormatDiagnostics(IEnumerable<BddDiagnostic> diagnostics)
    {
        return diagnostics
            .Select(diagnostic => $"{diagnostic.Location}: {diagnostic.Code} {diagnostic.Message}")
            .ToArray();
    }

    private static string FormatDiscoveryReport(BddConfig config)
    {
        var assemblies = config.Assemblies.Count == 0
            ? "- No assemblies registered."
            : string.Join(Environment.NewLine, config.Assemblies.Select(assembly => $"- {assembly.Kind}: {assembly.Assembly}"));

        return string.Join(
            Environment.NewLine,
            "Discovery report:",
            $"Target: {config.Runtime.GetValueOrDefault("Target", "(not set)")}",
            $"Fixture: {config.Runtime.GetValueOrDefault("Fixture", "(not set)")}",
            assemblies);
    }

    private static string FormatCatalog(BddCommandCatalog catalog)
    {
        return "Command catalog:" + Environment.NewLine + string.Join(
            Environment.NewLine,
            catalog.Patterns
                .OrderBy(pattern => pattern.Keyword)
                .ThenBy(pattern => pattern.Phrase, StringComparer.Ordinal)
                .Select(pattern => $"- {pattern.Keyword}: {pattern.Phrase} -> {pattern.CommandId}"));
    }
}
