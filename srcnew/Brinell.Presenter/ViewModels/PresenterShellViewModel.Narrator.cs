using System.Windows.Input;
using Brinell.Narrator;
using Brinell.Presenter.Commands;
using Brinell.Presenter.Services;

namespace Brinell.Presenter.ViewModels;

// Narrator mode: a video script open as a tree of scenes, run and recorded with the run engine.
public sealed partial class PresenterShellViewModel
{
    private bool _isNarratorMode;
    private BddNarrationPacer? _scenePacer;

    /// <summary>The open video script.</summary>
    public NarratorViewModel Narrator { get; } = new();

    /// <summary>
    /// Narrator mode: a video script's scenes and their steps in a tree, the moves, and the
    /// recording. The third mode, beside Present and Workspace.
    /// </summary>
    public bool IsNarratorMode => _isNarratorMode;

    /// <summary>Whether the Scenarios section shows: not in Narrator mode, where the script's tree takes its place.</summary>
    public bool IsScenarioSectionVisible => !IsNarratorMode;

    /// <summary>Whether the workspace tree shows.</summary>
    public bool IsScenarioTreeVisible => IsScenariosExpanded && !IsNarratorMode;

    /// <summary>Switches to Narrator mode.</summary>
    public ICommand ShowNarratorModeCommand { get; private set; } = null!;

    /// <summary>Asks for a video script and opens it.</summary>
    public ICommand OpenScriptCommand { get; private set; } = null!;

    private void InitializeNarrator()
    {
        ShowNarratorModeCommand = new RelayCommand(ShowNarratorMode);
        OpenScriptCommand = new AsyncRelayCommand(OpenScriptAsync);
        Narrator.SceneSelected += OnSceneSelected;
    }

    // The Steps list shows the selected scene's scenario, unless a run is on: it follows that.
    private void OnSceneSelected(NarratorSceneViewModel? scene)
    {
        if (_activeSession is null && scene?.Scenario is { } scenario)
        {
            SelectedScenario = scenario;
        }
    }

    // Run in Narrator mode: a rehearsal of the checked scenes, in the script's order. A scene that
    // fails does not stop the rest.
    private Task RunCheckedScenesAsync()
    {
        if (WorkspacePath is null || !Narrator.HasScript)
        {
            StatusSummary = "No video script open";
            return Task.CompletedTask;
        }

        var scenes = Narrator.CheckedScenes;
        if (scenes.Count == 0)
        {
            StatusSummary = "No scenes checked";
            return Task.CompletedTask;
        }

        // The rows of an earlier run would read as this run's until their scene's turn.
        foreach (var scene in scenes)
        {
            scene.Scenario!.Status = "wait";
            foreach (var step in scene.Scenario.Steps)
            {
                step.Status = "wait";
            }
        }

        BddScenarioViewModel[] scenarios = [.. scenes.Select(scene => scene.Scenario!)];
        var options = CaptureRunExecutionOptions("Script", Narrator.ScriptName, scenarios);
        if (SpeakAloud)
        {
            // The lines and the script's beat set the pace, as in a recording.
            options = options with { EffectiveDelayMilliseconds = 0 };
        }

        return RunScenariosAsync(scenarios, options, stopAtFailure: false);
    }

    // Before a scene's first step. A rehearsal with Speak aloud on plays each line before its step.
    private async Task StartSceneAsync(PresenterUatExecutionSession session, BddScenarioViewModel scenario)
    {
        _scenePacer = null;
        if (!SpeakAloud || Narrator.SceneOf(scenario) is not { Plan: { } plan } || Narrator.Check?.Script is not { } script)
        {
            return;
        }

        try
        {
            _scenePacer = await _recorder
                .RehearseAsync(plan, script.Voice ?? _voice, script.Beat ?? TimeSpan.FromMilliseconds(_beat), _executionCancellation?.Token ?? CancellationToken.None)
                .ConfigureAwait(true);
            session.Runner.Context.Pacer = _scenePacer;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            RecordingStatus = $"Not spoken: {ex.Message}";
        }
    }

    // After a scene's last step, before its app closes: the outro.
    private async Task EndSceneAsync()
    {
        if (_scenePacer is not { } pacer)
        {
            return;
        }

        _scenePacer = null;
        try
        {
            await pacer.FinishAsync(_executionCancellation?.Token ?? CancellationToken.None).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            // Stopped while the outro played.
        }
    }

    // Entering the mode with no script open: the workspace's last one, or its first.
    private void ShowNarratorMode()
    {
        SetNarratorMode(true);
        if (!Narrator.HasScript && DefaultScript() is { } script)
        {
            OpenScript(script);
        }
    }

    private void SetNarratorMode(bool on)
    {
        if (_isNarratorMode == on)
        {
            return;
        }

        _isNarratorMode = on;
        if (on && _isPresentMode)
        {
            _isPresentMode = false;
            OnPropertyChanged(nameof(IsPresentMode));
        }

        OnPropertyChanged(nameof(IsNarratorMode));
        OnPropertyChanged(nameof(IsWorkspaceMode));
        OnPropertyChanged(nameof(IsScenarioSectionVisible));
        OnPropertyChanged(nameof(IsScenarioTreeVisible));
        if (on)
        {
            SelectedTab = TreeTabName;
        }
    }

    private async Task OpenScriptAsync()
    {
        try
        {
            if (await _folderPickerService.PickScriptAsync().ConfigureAwait(true) is { } path)
            {
                OpenScript(path);
            }
        }
        catch (Exception ex)
        {
            StatusSummary = "The script could not be opened";
            DiagnosticsText = Describe(ex);
        }
    }

    /// <summary>
    /// Opens a video script in Narrator mode. Its suite is found by walking up from the script to
    /// the config, and loaded as the workspace when it is not the one open.
    /// </summary>
    /// <param name="path">The <c>*.video.md</c> file.</param>
    public void OpenScript(string path)
    {
        Narrator.Open(path);
        if (Narrator.ConfigDirectory is { } suite && !SamePath(suite, WorkspacePath))
        {
            // Loading reopens the script against the new workspace (OnWorkspaceLoaded).
            LoadWorkspace(suite, recordRecent: true);
        }
        else
        {
            Narrator.Link(Scenarios);
        }

        SetNarratorMode(true);
        OnSceneSelected(Narrator.Scenes.FirstOrDefault(scene => scene.CanRun));
        StatusSummary = Narrator.ConfigDirectory is null
            ? "No config above the script: its scenarios cannot be found"
            : $"Opened {Narrator.ScriptName}: {Narrator.Summary}";
        SaveLastScript(path);
    }

    // After a workspace load: the open script is read again, or closed when it is another suite's.
    private void OnWorkspaceLoaded()
    {
        if (Narrator.ScriptPath is not { } script)
        {
            return;
        }

        if (!File.Exists(script) || !SamePath(BddVideoScriptCheck.FindConfigDirectory(Path.GetDirectoryName(script)), WorkspacePath))
        {
            Narrator.Close();
            return;
        }

        Narrator.Open(script);
        Narrator.Link(Scenarios);
    }

    private string? DefaultScript()
    {
        if (WorkspacePath is not { } workspace)
        {
            return null;
        }

        if (_settingsService.Load().LastScript is { Length: > 0 } last && File.Exists(last) &&
            SamePath(BddVideoScriptCheck.FindConfigDirectory(Path.GetDirectoryName(last)), workspace))
        {
            return last;
        }

        return Directory.EnumerateFiles(workspace, "*" + BddVideoScript.Extension, SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(workspace, path).Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
            .Order(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private void SaveLastScript(string path)
    {
        try
        {
            var settings = _settingsService.Load();
            settings.LastScript = Path.GetFullPath(path);
            _settingsService.Save(settings);
        }
        catch (Exception ex)
        {
            DiagnosticsText = $"Could not save the last script: {ex.Message}";
        }
    }

    private static bool SamePath(string? left, string? right)
        => left is not null && right is not null &&
           string.Equals(Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
}
