using System.Text;
using System.Text.Json;

namespace Brinell.Core.Artifacts;

public static class TestArtifactManifestWriter
{
    private static readonly object SyncRoot = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static void RecordArtifact(
        string path,
        string kind,
        string name,
        string reason,
        Dictionary<string, string?>? metadata = null)
    {
        var provider = DefaultTestArtifactPathProvider.Create();
        RecordArtifact(provider, path, kind, name, reason, metadata);
    }

    public static void RecordArtifact(
        ITestArtifactPathProvider provider,
        string path,
        string kind,
        string name,
        string reason,
        Dictionary<string, string?>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(provider);
        provider.EnsureDirectories();

        var fullPath = Path.GetFullPath(path);
        var manifestPath = Path.Combine(provider.RunDirectory, "manifest.json");
        var summaryPath = Path.Combine(provider.RunDirectory, "summary.md");

        lock (SyncRoot)
        {
            var manifest = LoadManifest(manifestPath, provider.RunId);
            var suite = manifest.Suites.FirstOrDefault(x => x.Name == provider.SuiteName);
            if (suite is null)
            {
                suite = new TestArtifactSuite { Name = provider.SuiteName };
                manifest.Suites.Add(suite);
            }

            suite.Artifacts.Add(new TestArtifactRecord
            {
                Kind = kind,
                Name = name,
                Path = GetManifestPath(provider.RunDirectory, fullPath),
                Reason = reason,
                CreatedAt = DateTimeOffset.UtcNow,
                Metadata = metadata ?? []
            });

            manifest.UpdatedAt = DateTimeOffset.UtcNow;

            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions));
            File.WriteAllText(summaryPath, BuildSummary(manifest));
        }
    }

    public static void RecordArtifact(
        string path,
        string kind,
        string name,
        string reason)
        => RecordArtifact(path, kind, name, reason, null);

    private static TestArtifactManifest LoadManifest(string manifestPath, string runId)
    {
        if (!File.Exists(manifestPath))
        {
            return new TestArtifactManifest { RunId = runId };
        }

        try
        {
            return JsonSerializer.Deserialize<TestArtifactManifest>(
                File.ReadAllText(manifestPath),
                JsonOptions) ?? new TestArtifactManifest { RunId = runId };
        }
        catch (JsonException)
        {
            return new TestArtifactManifest { RunId = runId };
        }
    }

    private static string GetManifestPath(string runDirectory, string artifactPath)
    {
        try
        {
            return Path.GetRelativePath(runDirectory, artifactPath);
        }
        catch
        {
            return artifactPath;
        }
    }

    private static string BuildSummary(TestArtifactManifest manifest)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# Test Artifacts: {manifest.RunId}");
        builder.AppendLine();

        foreach (var suite in manifest.Suites.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"## {suite.Name}");
            builder.AppendLine();

            foreach (var artifact in suite.Artifacts)
            {
                builder.AppendLine($"- {artifact.Kind}: {artifact.Name} ({artifact.Reason}) - {artifact.Path}");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }
}
