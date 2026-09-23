using Brinell.Presenter.Models;
using Brinell.Uat;

namespace Brinell.Presenter.Services;

public sealed class UatWorkspaceService : IUatWorkspaceService
{
    private readonly IProjectEvaluator? _projectEvaluator;

    /// <summary>Creates the service with the shared MSBuild evaluator.</summary>
    public UatWorkspaceService()
        : this(null)
    {
    }

    /// <summary>Creates the service with a specific project evaluator.</summary>
    /// <param name="projectEvaluator">The evaluator to resolve <c>Runtime Project</c> with, or null for the default.</param>
    public UatWorkspaceService(IProjectEvaluator? projectEvaluator)
    {
        _projectEvaluator = projectEvaluator;
    }

    public string? FindDefaultWorkspace()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "testsnew", "Brinell.Maui.Uat.Tests");
            if (File.Exists(Path.Combine(candidate, "uat.config.md")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    public UatWorkspaceLoadResult LoadFolder(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        var catalog = UatSpecCommandCatalog.CreateDefault();
        List<UatFileLoadResult> files = [];
        List<UatScenarioLoadResult> scenarios = [];
        List<string> diagnostics = [];

        var configInfo = UatWorkspaceConfigInspector.Inspect(folderPath, _projectEvaluator);
        diagnostics.AddRange(configInfo.Diagnostics);

        var configPath = Path.Combine(folderPath, "uat.config.md");
        var discoveryReport = File.Exists(configPath)
            ? FormatDiscoveryReport(UatConfigParser.ParseFile(configPath))
            : "Discovery report:\n- uat.config.md not found.";

        foreach (var filePath in EnumerateUatFiles(folderPath))
        {
            var parse = UatMarkdownParser.ParseFile(filePath);
            var parseDiagnostics = FormatDiagnostics(parse.Diagnostics);
            UatBindResult? bind = null;

            if (parse.Document is not null)
            {
                bind = UatBinder.Bind(parse.Document, catalog);
            }

            var bindDiagnostics = bind is null ? [] : FormatDiagnostics(bind.Diagnostics);
            files.Add(new UatFileLoadResult(
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
                    new UatScenarioLoadResult(
                        scenario.Source.Name,
                        bind.Document.Source.Title,
                        filePath,
                        scenario.Source.Tags,
                        scenario.Invocations.Select(invocation => new UatStepLoadResult(
                            "wait",
                            invocation.Step.Text,
                            invocation.CommandId,
                            invocation.Step.Source.LineNumber)).ToArray())));
            }
            else if (parse.Document is not null)
            {
                scenarios.AddRange(parse.Document.Scenarios.Select(scenario =>
                    new UatScenarioLoadResult(
                        scenario.Name,
                        parse.Document.Title,
                        filePath,
                        scenario.Tags,
                        scenario.Steps.Select(step => new UatStepLoadResult(
                            "wait",
                            step.Text,
                            "(unbound)",
                            step.Source.LineNumber)).ToArray())));
            }
        }

        if (files.Count == 0)
        {
            diagnostics.Add("No .uat.md files were found.");
        }

        var workspaceName = new DirectoryInfo(folderPath).Name;
        return new UatWorkspaceLoadResult(
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
            .EnumerateFiles(folderPath, "*.uat.md", SearchOption.AllDirectories)
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

    private static IReadOnlyList<string> FormatDiagnostics(IEnumerable<UatDiagnostic> diagnostics)
    {
        return diagnostics
            .Select(diagnostic => $"{diagnostic.Location}: {diagnostic.Code} {diagnostic.Message}")
            .ToArray();
    }

    private static string FormatDiscoveryReport(UatConfig config)
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

    private static string FormatCatalog(UatCommandCatalog catalog)
    {
        return "Command catalog:" + Environment.NewLine + string.Join(
            Environment.NewLine,
            catalog.Patterns
                .OrderBy(pattern => pattern.Keyword)
                .ThenBy(pattern => pattern.Phrase, StringComparer.Ordinal)
                .Select(pattern => $"- {pattern.Keyword}: {pattern.Phrase} -> {pattern.CommandId}"));
    }
}
