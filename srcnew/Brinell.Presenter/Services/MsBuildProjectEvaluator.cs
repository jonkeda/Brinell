using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace Brinell.Presenter.Services;

/// <summary>
/// Asks MSBuild where a project's output goes, by evaluation only.
/// </summary>
/// <remarks>
/// <c>-getProperty</c> with no target runs no build and no restore, so this answers in about
/// half a second even for a project that has never been built — which is the point: it says
/// where the output <em>will</em> be. It also never touches workloads, so a clean answer here
/// is not evidence the project can build. See
/// <c>.my/present/startup/00-presenter-workspace-design.md</c>.
/// </remarks>
public sealed class MsBuildProjectEvaluator : IProjectEvaluator
{
    private static readonly TimeSpan EvaluationTimeout = TimeSpan.FromSeconds(60);

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public ProjectEvaluation Evaluate(
        string projectPath,
        string? targetFramework = null,
        string? configuration = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        if (!File.Exists(projectPath))
        {
            return ProjectEvaluation.Failed(projectPath, $"Project was not found: {projectPath}");
        }

        // Evaluation is ~0.5 s and the answer only changes when the project does.
        var key = $"{projectPath}|{targetFramework}|{configuration}";
        var writeTime = File.GetLastWriteTimeUtc(projectPath);
        if (_cache.TryGetValue(key, out var cached) && cached.WriteTime == writeTime)
        {
            return cached.Evaluation;
        }

        var evaluation = Run(projectPath, targetFramework, configuration);
        _cache[key] = new CacheEntry(writeTime, evaluation);
        return evaluation;
    }

    private static ProjectEvaluation Run(string projectPath, string? targetFramework, string? configuration)
    {
        // Always at least two properties: MSBuild prints a bare value for one and JSON for several.
        List<string> arguments =
        [
            "msbuild",
            projectPath,
            "-getProperty:TargetPath",
            "-getProperty:RunCommand",
            "-getProperty:TargetFramework",
            "-getProperty:TargetFrameworks"
        ];

        if (!string.IsNullOrWhiteSpace(targetFramework))
        {
            arguments.Add($"-p:TargetFramework={targetFramework}");
        }

        if (!string.IsNullOrWhiteSpace(configuration))
        {
            arguments.Add($"-p:Configuration={configuration}");
        }

        ProcessStartInfo startInfo = new("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(projectPath) ?? Environment.CurrentDirectory
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(startInfo)
                                ?? throw new InvalidOperationException("dotnet did not start.");

            // Both streams are drained before waiting: reading one to the end while the other
            // fills its buffer deadlocks.
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit((int)EvaluationTimeout.TotalMilliseconds))
            {
                process.Kill(entireProcessTree: true);
                return ProjectEvaluation.Failed(
                    projectPath,
                    $"Evaluating {Path.GetFileName(projectPath)} timed out after {EvaluationTimeout.TotalSeconds:0} s.");
            }

            var output = outputTask.GetAwaiter().GetResult();
            var error = errorTask.GetAwaiter().GetResult();

            return process.ExitCode == 0
                ? Parse(projectPath, output)
                : ProjectEvaluation.Failed(
                    projectPath,
                    Combine(output, error) is { Length: > 0 } message
                        ? message
                        : $"dotnet msbuild exited with {process.ExitCode}.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            return ProjectEvaluation.Failed(projectPath, $"Could not run dotnet msbuild: {ex.Message}");
        }
    }

    private static ProjectEvaluation Parse(string projectPath, string output)
    {
        try
        {
            using var document = JsonDocument.Parse(output);
            if (!document.RootElement.TryGetProperty("Properties", out var properties))
            {
                return ProjectEvaluation.Failed(projectPath, "MSBuild returned no properties.");
            }

            var targetFrameworks = Read(properties, "TargetFrameworks");
            var single = Read(properties, "TargetFramework");
            IReadOnlyList<string> frameworks = targetFrameworks.Length > 0
                ? [.. targetFrameworks.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)]
                : single.Length > 0 ? [single] : [];

            return new ProjectEvaluation(
                true,
                projectPath,
                Read(properties, "TargetPath"),
                Read(properties, "RunCommand"),
                frameworks,
                string.Empty);
        }
        catch (JsonException ex)
        {
            return ProjectEvaluation.Failed(projectPath, $"Could not read MSBuild output: {ex.Message}");
        }
    }

    private static string Read(JsonElement properties, string name)
    {
        return properties.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim() ?? string.Empty
            : string.Empty;
    }

    private static string Combine(string output, string error)
    {
        return string.Join(
            Environment.NewLine,
            new[] { output, error }.Where(text => !string.IsNullOrWhiteSpace(text)).Select(text => text.Trim()));
    }

    private sealed record CacheEntry(DateTime WriteTime, ProjectEvaluation Evaluation);
}
