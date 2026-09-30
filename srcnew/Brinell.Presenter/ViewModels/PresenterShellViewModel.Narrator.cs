using System.Windows.Input;
using Brinell.Narrator;
using Brinell.Presenter.Commands;

namespace Brinell.Presenter.ViewModels;

// Narrator mode: a video script open as a tree of scenes, run and recorded with the run engine.
public sealed partial class PresenterShellViewModel
{
    private bool _isNarratorMode;

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
