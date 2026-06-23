using System.Reflection;
using Brinell.Core.Configuration;

namespace Brinell.Core.Artifacts;

public sealed class DefaultTestArtifactPathProvider : ITestArtifactPathProvider
{
    private static readonly Lazy<string> LocalRunId = new(CreateLocalRunId);

    private DefaultTestArtifactPathProvider(TestArtifactOptions options)
    {
        RootDirectory = Path.GetFullPath(options.RootDirectory);
        RunId = SanitizeSegment(options.RunId);
        SuiteName = SanitizeSegment(options.SuiteName ?? InferSuiteName());
        RunDirectory = Path.Combine(RootDirectory, RunId);
        SuiteDirectory = Path.Combine(RunDirectory, "suites", SuiteName);
        RunnerDirectory = Path.Combine(SuiteDirectory, "runner");
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

    /// <summary>
    /// Creates artifact path provider from configuration.
    /// Configuration values are required; no fallback to environment variables.
    /// </summary>
    public static DefaultTestArtifactPathProvider Create(ArtifactsOptions artifacts, string? suiteName = null)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        // Use configuration values with reasonable defaults
        var root = artifacts.RootDirectory 
            ?? Path.Combine(FindRepositoryRoot(Environment.CurrentDirectory), "TestResults");

        var runId = artifacts.RunId ?? LocalRunId.Value;

        var suite = artifacts.Suite ?? suiteName ?? InferSuiteName();

        return new DefaultTestArtifactPathProvider(new TestArtifactOptions(root, runId, suite));
    }

    /// <summary>
    /// Creates artifact path provider with default values.
    /// Used for scenarios where configuration is not available (backward compatibility).
    /// </summary>
    public static DefaultTestArtifactPathProvider Create(string? suiteName = null)
    {
        var root = Path.Combine(FindRepositoryRoot(Environment.CurrentDirectory), "TestResults");
        var runId = LocalRunId.Value;
        var suite = suiteName ?? InferSuiteName();

        return new DefaultTestArtifactPathProvider(new TestArtifactOptions(root, runId, suite));
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

    private static string FindRepositoryRoot(string startDirectory)
    {
        var directory = Directory.Exists(startDirectory)
            ? new DirectoryInfo(startDirectory)
            : new FileInfo(startDirectory).Directory;

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) ||
                Directory.GetFiles(directory.FullName, "*.sln").Length > 0)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Environment.CurrentDirectory;
    }

    private static string InferSuiteName()
    {
        return Assembly.GetEntryAssembly()?.GetName().Name ??
               Assembly.GetCallingAssembly().GetName().Name ??
               "default";
    }

    private static string CreateLocalRunId()
    {
        return $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"[..22];
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
