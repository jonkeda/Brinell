using Brinell.Narrator;
using Brinell.Presenter.Models;
using Brinell.Presenter.Services;
using Brinell.Presenter.ViewModels;

namespace Brinell.Presenter.Bdd.Tests.Services;

/// <summary>
/// Run in Narrator mode: the checked scenes, in the script's order, a session each; a scene that
/// fails does not stop the ones after it (Present mode stops at the first failure).
/// </summary>
public sealed class NarratorRunTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "BrinellNarratorRun", Guid.NewGuid().ToString("N"));
    private readonly string _feature;
    private readonly string _script;

    public NarratorRunTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Features"));
        File.WriteAllText(Path.Combine(_root, "uat.config.md"), "# UAT Config\n");
        _feature = Path.Combine(_root, "Features", "greeting.feature.md");
        File.WriteAllText(
            _feature,
            """
            # Feature: Greeting

            ## Scenario: First

            ```gherkin
            Given I am on the Main page
            ```

            ## Scenario: Second

            ```gherkin
            Given I am on the Main page
            ```

            ## Scenario: Third

            ```gherkin
            Given I am on the Main page
            ```
            """);
        _script = Path.Combine(_root, "Features", "tour.video.md");
        File.WriteAllText(
            _script,
            """
            # Video: A tour

            ## Scene: One

            Scenario: Features/greeting.feature.md#First

            ## Scene: Two

            Scenario: Features/greeting.feature.md#Second

            ## Scene: Three

            Scenario: Features/greeting.feature.md#Third
            """);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Run_GoesOnAfterASceneThatFails_AndSkipsAnUncheckedOne()
    {
        var execution = new FailingExecutionService();
        var shell = Shell(execution);
        shell.ShowNarratorModeCommand.Execute(null);

        Assert.True(shell.IsNarratorMode);
        Assert.False(shell.IsPresentMode);
        Assert.False(shell.IsWorkspaceMode);
        Assert.Equal("tour.video.md", shell.Narrator.ScriptName);
        Assert.Equal(3, shell.Narrator.CheckedScenes.Count);

        shell.Narrator.Scenes[1].Node.IsChecked = false;
        shell.RunCommand.Execute(null);

        // No session can start here, so every scene fails (and the run ends at once): the third is still tried.
        Assert.Equal(["First", "Third"], execution.Started);
        Assert.Equal("Failed: 0/2 scenarios passed", shell.StatusSummary);
        Assert.Equal("fail", shell.Narrator.Scenes[0].Node.Status);
        Assert.Equal("wait", shell.Narrator.Scenes[1].Node.Status);
        Assert.Equal("fail", shell.Narrator.Scenes[2].Node.Status);
    }

    [Fact]
    public void Run_InPresentMode_StopsAtTheFirstFailure()
    {
        var execution = new FailingExecutionService();
        var shell = Shell(execution);

        shell.RunCommand.Execute(null);

        Assert.Single(execution.Started);
    }

    [Fact]
    public void Run_WithNoSceneChecked_SaysSo()
    {
        var execution = new FailingExecutionService();
        var shell = Shell(execution);
        shell.ShowNarratorModeCommand.Execute(null);
        shell.Narrator.Roots[0].IsChecked = false;

        shell.RunCommand.Execute(null);

        Assert.Empty(execution.Started);
        Assert.Equal("No scenes checked", shell.StatusSummary);
    }

    [Fact]
    public void OpenScript_LeavesTheModeOnNarrator_AndSelectsTheFirstScene()
    {
        var shell = Shell(new FailingExecutionService());

        shell.OpenScript(_script);

        Assert.True(shell.IsNarratorMode);
        Assert.Equal("First", shell.SelectedScenario?.Name);

        shell.Narrator.SelectedNode = shell.Narrator.Scenes[2].Node;
        Assert.Equal("Third", shell.SelectedScenario?.Name);

        shell.ShowPresentModeCommand.Execute(null);
        Assert.False(shell.IsNarratorMode);
        Assert.True(shell.IsPresentMode);
    }

    private PresenterShellViewModel Shell(IBddExecutionService execution)
        => new(new Workspace(_root, _feature), execution, new NoPicker(), new MemorySettings(), recorder: new NoRecorder());

    private sealed class FailingExecutionService : IBddExecutionService
    {
        public List<string> Started { get; } = [];

        public IReadOnlyDictionary<string, string> WindowEnvironment { get; set; } = new Dictionary<string, string>();

        public Task<PresenterUatExecutionSession> CreateSessionAsync(string workspacePath, string scenarioFilePath, string scenarioName, CancellationToken cancellationToken)
        {
            Started.Add(scenarioName);
            throw new InvalidOperationException("No app here.");
        }
    }

    private sealed class Workspace(string root, string feature) : IBddWorkspaceService
    {
        public string? FindDefaultWorkspace() => root;

        public BddWorkspaceLoadResult LoadFolder(string folderPath)
        {
            BddScenarioLoadResult Scenario(string name, int line)
                => new(name, "Greeting", feature, [], [new BddStepLoadResult("wait", "Given I am on the Main page", "Page.Open", line)]);

            return new BddWorkspaceLoadResult(
                root,
                "Workspace",
                new BddWorkspaceConfigLoadResult(true, Path.Combine(root, "uat.config.md"), "MAUI", "Fixture", "App.exe", "App.exe", true, string.Empty, string.Empty, true, [], []),
                [new BddFileLoadResult(feature, "greeting.feature.md", true, true, [])],
                [Scenario("First", 6), Scenario("Second", 12), Scenario("Third", 18)],
                [],
                "Discovery report:",
                "Command catalog:");
        }
    }

    private sealed class NoPicker : IFolderPickerService
    {
        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);

        public Task<string?> PickProjectAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);

        public Task<string?> PickScriptAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }

    private sealed class MemorySettings : IPresenterUserSettingsService
    {
        private PresenterUserSettings _settings = new();

        public PresenterUserSettings Load() => _settings;

        public void Save(PresenterUserSettings settings) => _settings = settings;

        public PresenterUserSettings RecordOpenedFolder(string folderPath) => _settings;
    }

    private sealed class NoRecorder : IPresenterRecorder
    {
        public string? Unavailable => "No ffmpeg in a unit test.";

        public Task StartAsync(PresenterUatExecutionSession session, string workspacePath, string featureFile, string scenarioName, PresenterRecordingSettings settings, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<PresenterRecorded?> FinishAsync(CancellationToken cancellationToken) => Task.FromResult<PresenterRecorded?>(null);

        public Task<BddNarrationPacer?> RehearseAsync(BddVideoScenePlan scene, string? voice, TimeSpan beat, CancellationToken cancellationToken)
            => Task.FromResult<BddNarrationPacer?>(null);

        public Task StartSceneAsync(PresenterUatExecutionSession session, PresenterSceneRequest scene, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<BddSceneTimeline?> FinishSceneAsync(CancellationToken cancellationToken) => Task.FromResult<BddSceneTimeline?>(null);

        public Task DropSceneAsync() => Task.CompletedTask;

        public Task<PresenterRecorded> StitchAsync(string title, string scenesFolder, string video, int sceneCount, CancellationToken cancellationToken)
            => Task.FromResult(new PresenterRecorded(video, true, []));
    }
}
