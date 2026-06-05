namespace Brinell.Core.Artifacts;

public sealed class DefaultTestArtifactPathProvider : ITestArtifactPathProvider
{
    private DefaultTestArtifactPathProvider(string rootDirectory, string runId, string suiteName)
    {
        RootDirectory = Path.GetFullPath(rootDirectory);
        RunId = SanitizeSegment(runId);
        SuiteName = SanitizeSegment(suiteName);
        RunDirectory = Path.Combine(RootDirectory, RunId);
        SuiteDirectory = Path.Combine(RunDirectory, "suites", SuiteName);
        RunnerDirectory = Path.Combine(RunDirectory, "runner");
        LogsDirectory = Path.Combine(SuiteDirectory, "logs");
        ScreenshotsDirectory = Path.Combine(SuiteDirectory, "screenshots");
        UatDirectory = Path.Combine(SuiteDirectory, "uat");
        CoverageDirectory = Path.Combine(SuiteDirectory, "coverage");
        TracesDirectory = Path.Combine(SuiteDirectory, "traces");
        VideosDirectory = Path.Combine(SuiteDirectory, "videos");
        DownloadsDirectory = Path.Combine(SuiteDirectory, "downloads");
        SnapshotsDirectory = Path.Combine(SuiteDirectory, "snapshots");
        AttachmentsDirectory = Path.Combine(SuiteDirectory, "attachments");
    }

    public string RootDirectory { get; }

    public string RunId { get; }

    public string RunDirectory { get; }

    public string SuiteName { get; }

    public string SuiteDirectory { get; }

    public string RunnerDirectory { get; }

    public string LogsDirectory { get; }

    public string ScreenshotsDirectory { get; }

    public string UatDirectory { get; }

    public string CoverageDirectory { get; }

    public string TracesDirectory { get; }

    public string VideosDirectory { get; }

    public string DownloadsDirectory { get; }

    public string SnapshotsDirectory { get; }

    public string AttachmentsDirectory { get; }

    public static DefaultTestArtifactPathProvider Create(string? suiteName = null, string? baseDirectory = null)
    {
        var root = FirstNonEmpty(
            Environment.GetEnvironmentVariable(TestArtifactOptions.RootDirectoryEnvironmentVariable),
            Environment.GetEnvironmentVariable(TestArtifactOptions.LegacyRootDirectoryEnvironmentVariable),
            baseDirectory,
            Path.Combine(Environment.CurrentDirectory, "TestResults"));

        var runId = FirstNonEmpty(
            Environment.GetEnvironmentVariable(TestArtifactOptions.RunIdEnvironmentVariable),
            Environment.GetEnvironmentVariable(TestArtifactOptions.LegacyRunIdEnvironmentVariable),
            DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));

        var suite = FirstNonEmpty(
            Environment.GetEnvironmentVariable(TestArtifactOptions.SuiteEnvironmentVariable),
            Environment.GetEnvironmentVariable(TestArtifactOptions.LegacySuiteEnvironmentVariable),
            suiteName,
            "default");

        return new DefaultTestArtifactPathProvider(root, runId, suite);
    }

    public void EnsureDirectories()
    {
        foreach (var directory in new[]
        {
            RunDirectory,
            RunnerDirectory,
            LogsDirectory,
            ScreenshotsDirectory,
            UatDirectory,
            CoverageDirectory,
            TracesDirectory,
            VideosDirectory,
            DownloadsDirectory,
            SnapshotsDirectory,
            AttachmentsDirectory
        })
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        throw new InvalidOperationException("At least one value is required.");
    }

    private static string SanitizeSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars()
            .Concat(Path.GetInvalidPathChars())
            .Append(Path.DirectorySeparatorChar)
            .Append(Path.AltDirectorySeparatorChar)
            .ToHashSet();

        var chars = value
            .Select(character => invalid.Contains(character) ? '_' : character)
            .ToArray();

        var sanitized = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "default" : sanitized;
    }
}
