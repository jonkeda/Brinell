using System.Text.Json;
using Brinell.Core.Artifacts;
using Brinell.Uat.Preview;

namespace Brinell.Uat.LanguageServer;

/// <summary>
/// The newest run of each scenario of a file, found through the runs' artifact manifests
/// (<c>TestResults/&lt;run-id&gt;/manifest.json</c>, AD-007), not by guessing a run id. A report
/// counts for a file when its suite and its file (relative to the <c>Features</c> folder) match.
/// </summary>
internal sealed class UatRunResults
{
    // Enough runs to find a file's last run without reading years of history on each preview.
    private const int RunsSearched = 40;

    private static readonly JsonSerializerOptions ManifestJson = new(JsonSerializerDefaults.General) { PropertyNameCaseInsensitive = true };

    private readonly Dictionary<string, (DateTime Written, TestArtifactManifest? Manifest)> _manifests = new(StringComparer.OrdinalIgnoreCase);

    public UatRunResults(string resultsRoot) => Root = resultsRoot;

    /// <summary>The folder the runs are in: <c>TestResults</c> at the repository root.</summary>
    public string Root { get; }

    /// <summary>The <c>TestResults</c> folder for a solution folder: at the nearest folder above with <c>.git</c> or a <c>.sln</c>, as the runs write it.</summary>
    /// <param name="solutionDir">The solution folder.</param>
    public static string RootFor(string solutionDir)
    {
        if (Environment.GetEnvironmentVariable(TestArtifactOptions.RootDirectoryEnvironmentVariable) is { Length: > 0 } configured)
        {
            return configured;
        }

        for (var folder = new DirectoryInfo(solutionDir); folder is not null; folder = folder.Parent)
        {
            if (Directory.Exists(Path.Combine(folder.FullName, ".git")) || folder.EnumerateFiles("*.sln").Any())
            {
                return Path.Combine(folder.FullName, "TestResults");
            }
        }

        return Path.Combine(solutionDir, "TestResults");
    }

    /// <summary>The newest run of each scenario of a file in a suite, by scenario name.</summary>
    /// <param name="suite">The suite's name (its project's).</param>
    /// <param name="filePath">The feature file.</param>
    public IReadOnlyDictionary<string, UatScenarioRun> For(string suite, string filePath)
    {
        Dictionary<string, UatScenarioRun> runs = new(StringComparer.Ordinal);
        if (!Directory.Exists(Root))
        {
            return runs;
        }

        var key = FeatureKey(filePath, BddScenarioFiles.ScenarioRoot(filePath));
        var manifests = Directory.EnumerateDirectories(Root)
            .Select(run => Path.Combine(run, "manifest.json"))
            .Where(File.Exists)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Take(RunsSearched);
        foreach (var manifestPath in manifests)
        {
            if (Read(manifestPath) is not { } manifest)
            {
                continue;
            }

            var runDirectory = Path.GetDirectoryName(manifestPath)!;
            foreach (var record in manifest.Suites.SelectMany(s => s.Artifacts).Reverse())
            {
                if (record.Kind != "uat-scenario" ||
                    runs.ContainsKey(record.Name) ||
                    !string.Equals(record.Metadata.GetValueOrDefault("suite"), suite, StringComparison.OrdinalIgnoreCase) ||
                    record.Metadata.GetValueOrDefault("file") is not { } file ||
                    !string.Equals(FeatureKey(file, null), key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var reportPath = Path.IsPathRooted(record.Path) ? record.Path : Path.Combine(runDirectory, record.Path);
                if (File.Exists(reportPath) && UatScenarioRun.Parse(File.ReadAllText(reportPath), record.CreatedAtUtc) is { } run)
                {
                    runs[record.Name] = run;
                }
            }
        }

        return runs;
    }

    // A file as a report and a source tree both name it: its path under the Features folder.
    private static string FeatureKey(string path, string? scenarioRoot)
    {
        var normalized = path.Replace('\\', '/');
        if (scenarioRoot is not null)
        {
            return Path.GetRelativePath(scenarioRoot, path).Replace('\\', '/');
        }

        var at = normalized.LastIndexOf(BddFileFormat.FeatureFolder + "/", StringComparison.OrdinalIgnoreCase);
        return at >= 0 ? normalized[(at + BddFileFormat.FeatureFolder.Length + 1)..] : Path.GetFileName(normalized);
    }

    private TestArtifactManifest? Read(string manifestPath)
    {
        var written = File.GetLastWriteTimeUtc(manifestPath);
        if (_manifests.TryGetValue(manifestPath, out var cached) && cached.Written == written)
        {
            return cached.Manifest;
        }

        TestArtifactManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<TestArtifactManifest>(File.ReadAllText(manifestPath), ManifestJson);
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            manifest = null;
        }

        _manifests[manifestPath] = (written, manifest);
        return manifest;
    }
}
