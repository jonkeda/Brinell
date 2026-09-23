namespace Brinell.Core.Artifacts;

public interface ITestArtifactPathProvider
{
    string RootDirectory { get; }

    string RunId { get; }

    string RunDirectory { get; }

    string SuiteName { get; }

    string SuiteDirectory { get; }

    string RunnerDirectory { get; }

    string LogsDirectory { get; }

    string ScreenshotsDirectory { get; }

    string UatDirectory { get; }

    string CoverageDirectory { get; }

    string TracesDirectory { get; }

    string VideosDirectory { get; }

    string DownloadsDirectory { get; }

    string SnapshotsDirectory { get; }

    string AttachmentsDirectory { get; }

    void EnsureDirectories();
}
