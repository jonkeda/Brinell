using System.Text.Json;
using Brinell.Core.Artifacts;

namespace Brinell.Core.Tests;

public sealed class TestArtifactPathProviderTests
{
    [Fact]
    public void Create_UsesEnvironmentRootRunIdAndSuite()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brinell-results-{Guid.NewGuid():N}");
        using var environment = new ArtifactEnvironment(
            rootDirectory: root,
            runId: "run:42",
            suite: "Suite/Name");

        var provider = DefaultTestArtifactPathProvider.Create(baseDirectory: root);

        Assert.Equal(Path.GetFullPath(root), provider.RootDirectory);
        Assert.EndsWith(Path.Combine("run_42", "suites", "Suite_Name"), provider.SuiteDirectory);
        Assert.EndsWith(Path.Combine("run_42", "suites", "Suite_Name", "screenshots"), provider.ScreenshotsDirectory);
        Assert.EndsWith(Path.Combine("run_42", "suites", "Suite_Name", "uat"), provider.UatDirectory);
    }

    [Fact]
    public void Create_UsesEnvironmentSuiteNameBeforeProvidedSuite()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brinell-results-{Guid.NewGuid():N}");
        using var environment = new ArtifactEnvironment(
            rootDirectory: root,
            runId: "run-1",
            suite: "EnvironmentSuite");

        var provider = DefaultTestArtifactPathProvider.Create(
            suiteName: "ProvidedSuite",
            baseDirectory: root);

        Assert.EndsWith(Path.Combine("run-1", "suites", "EnvironmentSuite"), provider.SuiteDirectory);
    }

    [Fact]
    public void EnsureDirectories_CreatesTypedArtifactDirectories()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brinell-results-{Guid.NewGuid():N}");
        using var environment = new ArtifactEnvironment(
            rootDirectory: root,
            runId: "run-1",
            suite: "Suite");
        var provider = DefaultTestArtifactPathProvider.Create(baseDirectory: root);

        provider.EnsureDirectories();

        Assert.True(Directory.Exists(provider.RunnerDirectory));
        Assert.True(Directory.Exists(provider.LogsDirectory));
        Assert.True(Directory.Exists(provider.ScreenshotsDirectory));
        Assert.True(Directory.Exists(provider.UatDirectory));
        Assert.True(Directory.Exists(provider.CoverageDirectory));
        Assert.True(Directory.Exists(provider.TracesDirectory));
        Assert.True(Directory.Exists(provider.VideosDirectory));
        Assert.True(Directory.Exists(provider.DownloadsDirectory));
        Assert.True(Directory.Exists(provider.SnapshotsDirectory));
        Assert.True(Directory.Exists(provider.AttachmentsDirectory));

        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void RecordArtifact_WritesManifestAndSummary()
    {
        var root = Path.Combine(Path.GetTempPath(), $"brinell-results-{Guid.NewGuid():N}");
        using var environment = new ArtifactEnvironment(
            rootDirectory: root,
            runId: "run-1",
            suite: "Suite");
        var provider = DefaultTestArtifactPathProvider.Create(baseDirectory: root);
        Directory.CreateDirectory(provider.ScreenshotsDirectory);
        var screenshotPath = Path.Combine(provider.ScreenshotsDirectory, "sample.png");
        File.WriteAllText(screenshotPath, "fake png");

        TestArtifactManifestWriter.RecordArtifact(
            screenshotPath,
            "screenshot",
            "SampleTest",
            "Manual");

        var manifestPath = Path.Combine(provider.RunDirectory, "manifest.json");
        var summaryPath = Path.Combine(provider.RunDirectory, "summary.md");
        Assert.True(File.Exists(manifestPath));
        Assert.True(File.Exists(summaryPath));

        var manifest = JsonSerializer.Deserialize<TestArtifactManifest>(
            File.ReadAllText(manifestPath));
        Assert.NotNull(manifest);
        var suite = Assert.Single(manifest.Suites);
        Assert.Equal("Suite", suite.Name);
        var artifact = Assert.Single(suite.Artifacts);
        Assert.Equal("screenshot", artifact.Kind);
        Assert.Equal("SampleTest", artifact.Name);
        Assert.EndsWith(Path.Combine("suites", "Suite", "screenshots", "sample.png"), artifact.Path);
        Assert.Contains("SampleTest", File.ReadAllText(summaryPath));

        Directory.Delete(root, recursive: true);
    }

    private sealed class ArtifactEnvironment : IDisposable
    {
        private readonly string? _rootDirectory;
        private readonly string? _runId;
        private readonly string? _suite;

        public ArtifactEnvironment(string rootDirectory, string runId, string suite)
        {
            _rootDirectory = Environment.GetEnvironmentVariable(TestArtifactOptions.RootDirectoryEnvironmentVariable);
            _runId = Environment.GetEnvironmentVariable(TestArtifactOptions.RunIdEnvironmentVariable);
            _suite = Environment.GetEnvironmentVariable(TestArtifactOptions.SuiteEnvironmentVariable);

            Environment.SetEnvironmentVariable(TestArtifactOptions.RootDirectoryEnvironmentVariable, rootDirectory);
            Environment.SetEnvironmentVariable(TestArtifactOptions.RunIdEnvironmentVariable, runId);
            Environment.SetEnvironmentVariable(TestArtifactOptions.SuiteEnvironmentVariable, suite);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(TestArtifactOptions.RootDirectoryEnvironmentVariable, _rootDirectory);
            Environment.SetEnvironmentVariable(TestArtifactOptions.RunIdEnvironmentVariable, _runId);
            Environment.SetEnvironmentVariable(TestArtifactOptions.SuiteEnvironmentVariable, _suite);
        }
    }
}
