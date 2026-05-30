namespace Brinell.Presenter.Models;

public sealed record UatWorkspaceLoadResult(
    string FolderPath,
    string WorkspaceName,
    UatWorkspaceConfigLoadResult Config,
    IReadOnlyList<UatFileLoadResult> Files,
    IReadOnlyList<UatScenarioLoadResult> Scenarios,
    IReadOnlyList<string> Diagnostics,
    string DiscoveryReport,
    string CommandCatalogReport)
{
    public int ErrorCount => Diagnostics.Count(line => line.Contains("error", StringComparison.OrdinalIgnoreCase));
}

public sealed record UatWorkspaceConfigLoadResult(
    bool ConfigExists,
    string ConfigPath,
    string Target,
    string Fixture,
    string AppPath,
    string ResolvedAppPath,
    bool AppPathExists,
    string WorkingDirectory,
    string ResolvedWorkingDirectory,
    bool WorkingDirectoryExists,
    IReadOnlyList<UatAssemblyLoadResult> Assemblies,
    IReadOnlyList<string> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(line => line.StartsWith("Error:", StringComparison.OrdinalIgnoreCase));

    public string Summary
    {
        get
        {
            if (!ConfigExists)
            {
                return "Config missing";
            }

            var target = string.IsNullOrWhiteSpace(Target) ? "Target missing" : Target;
            var fixture = string.IsNullOrWhiteSpace(Fixture) ? "Fixture missing" : Fixture;
            var app = AppPathExists ? "App ok" : "App missing";
            return $"{target}  {fixture}  {app}";
        }
    }
}

public sealed record UatAssemblyLoadResult(
    string Kind,
    string Assembly,
    string ResolvedPath,
    bool Exists);

public sealed record UatFileLoadResult(
    string FilePath,
    string Name,
    bool ParseSucceeded,
    bool BindSucceeded,
    IReadOnlyList<string> Diagnostics);

public sealed record UatScenarioLoadResult(
    string Name,
    string FilePath,
    IReadOnlyList<string> Tags,
    IReadOnlyList<UatStepLoadResult> Steps);

public sealed record UatStepLoadResult(
    string Status,
    string Text,
    string CommandId,
    int LineNumber);
