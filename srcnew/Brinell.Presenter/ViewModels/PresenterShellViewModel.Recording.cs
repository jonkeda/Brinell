using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;
using Brinell.Narrator.Windows;
using Brinell.Presenter.Commands;
using Brinell.Presenter.Services;

namespace Brinell.Presenter.ViewModels;

// Record (the narrator): each run is recorded on video while Record is on, narrated by the scene a
// video script has for its scenario; the settings are the Recordings folder, the voice, the beat
// and Speak aloud.
public sealed partial class PresenterShellViewModel
{
    private const string DefaultVoiceChoice = "Default voice";

    private IPresenterRecorder _recorder = null!;
    private bool _isRecording;
    private string _recordingsFolder = string.Empty;
    private string? _voice;
    private int _beat;
    private bool _speakAloud;
    private string? _lastRecording;
    private string _recordingStatus = string.Empty;

    /// <summary>Whether runs are recorded on video; the Record button turns it on and off.</summary>
    public bool IsRecording
    {
        get => _isRecording;
        private set
        {
            if (SetProperty(ref _isRecording, value))
            {
                OnPropertyChanged(nameof(RecordDescription));
            }
        }
    }

    /// <summary>The Record button's tooltip.</summary>
    public string RecordDescription => IsNarratorMode
        ? $"Record the checked scenes ({Narrator.CheckedScenes.Count}), each into its own kept recording; then Stitch joins them."
        : IsRecording
            ? "Recording is on: each run is recorded on video. Click to stop recording."
            : "Record: record the next runs on video, with the voice of a video script's scene when it has one.";

    /// <summary>Turns recording on and off.</summary>
    public ICommand ToggleRecordCommand { get; private set; } = null!;

    /// <summary>Opens the last recording in the system's video player.</summary>
    public ICommand WatchRecordingCommand { get; private set; } = null!;

    /// <summary>Whether there is a recording to watch.</summary>
    public bool HasRecording => _lastRecording is not null;

    /// <summary>What the last recording made, or why it could not.</summary>
    public string RecordingStatus
    {
        get => _recordingStatus;
        private set => SetProperty(ref _recordingStatus, value);
    }

    /// <summary>The Recordings folder setting; empty for the suite's <c>TestResults\recordings</c>.</summary>
    public string RecordingsFolderText
    {
        get => _recordingsFolder;
        set
        {
            if (SetProperty(ref _recordingsFolder, value ?? string.Empty))
            {
                SaveRecordingSettings();
            }
        }
    }

    /// <summary>The voices on this machine, the default first.</summary>
    public IReadOnlyList<string> VoiceChoices { get; } = [DefaultVoiceChoice, .. WindowsSpeech.Voices];

    /// <summary>The voice setting, by its name on the Settings page.</summary>
    public string VoiceChoice
    {
        get => _voice ?? DefaultVoiceChoice;
        set
        {
            var voice = value is null or DefaultVoiceChoice ? null : value;
            if (!string.Equals(voice, _voice, StringComparison.Ordinal))
            {
                _voice = voice;
                OnPropertyChanged();
                SaveRecordingSettings();
            }
        }
    }

    /// <summary>The beat setting in milliseconds, as the Settings page edits it.</summary>
    public string BeatText
    {
        get => _beat.ToString(CultureInfo.InvariantCulture);
        set
        {
            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var beat) && beat <= MaxExecutionDelayMilliseconds && beat != _beat)
            {
                _beat = beat;
                OnPropertyChanged();
                SaveRecordingSettings();
            }
        }
    }

    /// <summary>Whether a recording also plays each line aloud while the run waits for it.</summary>
    public bool SpeakAloud
    {
        get => _speakAloud;
        set
        {
            if (SetProperty(ref _speakAloud, value))
            {
                SaveRecordingSettings();
            }
        }
    }

    private void InitializeRecording(PresenterUserSettings settings, IPresenterRecorder? recorder)
    {
        _recorder = recorder ?? new PresenterRecorder();
        _recordingsFolder = settings.RecordingsFolder ?? string.Empty;
        _voice = settings.Voice;
        _beat = settings.Beat;
        _speakAloud = settings.SpeakAloud;
        ToggleRecordCommand = new RelayCommand(ToggleRecord);
        WatchRecordingCommand = new RelayCommand(WatchRecording, () => HasRecording);
    }

    private void ToggleRecord()
    {
        if (!IsRecording && _recorder.Unavailable is { } reason)
        {
            RecordingStatus = reason;
            return;
        }

        IsRecording = !IsRecording;
        RecordingStatus = IsRecording ? "Recording is on: the next run is recorded." : "Recording is off.";
    }

    // After the session is created, before its first step: the recording starts at that step.
    private async Task StartRecordingAsync(PresenterUatExecutionSession session, BddScenarioViewModel scenario)
    {
        // Narrator mode runs scenes; the Record toggle is Present mode's.
        if (IsNarratorMode)
        {
            await StartSceneAsync(session, scenario).ConfigureAwait(true);
            return;
        }

        if (!IsRecording || WorkspacePath is null)
        {
            return;
        }

        try
        {
            var settings = new PresenterRecordingSettings(_recordingsFolder.Length > 0 ? _recordingsFolder : null, _voice, TimeSpan.FromMilliseconds(_beat), _speakAloud);
            await _recorder.StartAsync(session, WorkspacePath, scenario.FilePath, scenario.Name, settings, _executionCancellation?.Token ?? CancellationToken.None).ConfigureAwait(true);
            RecordingStatus = $"Recording {scenario.Name}...";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            RecordingStatus = $"Not recorded: {ex.Message}";
        }
    }

    // When the run ends, however it ends: the outro plays and the video is composed, while the
    // next run may already start.
    private void FinishRecording()
    {
        if (_sceneRecording)
        {
            // The run ended before its scene did (Stop, or it could not start): the scene is dropped.
            _sceneRecording = false;
            _ = _recorder.DropSceneAsync();
            return;
        }

        _ = FinishRecordingAsync();
    }

    private async Task FinishRecordingAsync()
    {
        try
        {
            if (await _recorder.FinishAsync(CancellationToken.None).ConfigureAwait(true) is not { } recorded)
            {
                return;
            }

            _lastRecording = recorded.Video;
            OnPropertyChanged(nameof(HasRecording));
            (WatchRecordingCommand as RelayCommand)?.NotifyCanExecuteChanged();
            RecordingStatus = $"Recorded {Path.GetFileName(recorded.Video)}{(recorded.Narrated ? string.Empty : " (no video script narrates this scenario)")}"
                + (recorded.Warnings.Count > 0 ? $". {recorded.Warnings[0]}" : string.Empty);
        }
        catch (Exception ex)
        {
            RecordingStatus = $"The recording failed: {ex.Message}";
        }
    }

    private void WatchRecording()
    {
        if (_lastRecording is { } video && File.Exists(video))
        {
            // The system's video player: Presenter has none.
            using (Process.Start(new ProcessStartInfo(video) { UseShellExecute = true }))
            {
            }
        }
    }

    private void SaveRecordingSettings()
    {
        try
        {
            var settings = _settingsService.Load();
            settings.RecordingsFolder = _recordingsFolder.Length > 0 ? _recordingsFolder : null;
            settings.Voice = _voice;
            settings.Beat = _beat;
            settings.SpeakAloud = _speakAloud;
            _settingsService.Save(settings);
        }
        catch (Exception ex)
        {
            DiagnosticsText = $"Could not save the recording settings: {ex.Message}";
        }

        // The voice and the beat are part of what a scene is recorded from; the folder is where its recording is.
        RefreshSceneStates(resetChecks: true);
    }
}
