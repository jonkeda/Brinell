using System.Collections.ObjectModel;
using System.Windows.Input;
using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels;

public sealed class RecordingViewModel : ViewModelBase
{
    private readonly ILogger<RecordingViewModel> _logger;
    private bool _isRecording;
    private bool _isPaused;
    private bool _isStopped;
    private string _recordingStatus = string.Empty;
    private DateTime _lastCaptureTime;
    private string? _lastCapturedUrl;

    public RecordingViewModel(ILogger<RecordingViewModel> logger)
    {
        _logger = logger;

        StartRecordingCommand = new RelayCommand(StartRecording, () => !IsRecording);
        StopRecordingCommand = new RelayCommand(StopRecording, () => IsRecording);
        PauseRecordingCommand = new RelayCommand(PauseRecording, () => IsRecording && !IsPaused);
        ResumeRecordingCommand = new RelayCommand(ResumeRecording, () => (IsRecording && IsPaused) || IsStopped);
    }

    public ObservableCollection<DomSnapshot> SessionSnapshots { get; } = [];

    public bool IsRecording
    {
        get => _isRecording;
        private set
        {
            if (SetProperty(ref _isRecording, value))
                RaiseCommandStates();
        }
    }

    public bool IsPaused
    {
        get => _isPaused;
        private set
        {
            if (SetProperty(ref _isPaused, value))
                RaiseCommandStates();
        }
    }

    public bool IsStopped
    {
        get => _isStopped;
        private set
        {
            if (SetProperty(ref _isStopped, value))
                RaiseCommandStates();
        }
    }

    public string RecordingStatus
    {
        get => _recordingStatus;
        private set => SetProperty(ref _recordingStatus, value);
    }

    public ICommand StartRecordingCommand { get; }
    public ICommand StopRecordingCommand { get; }
    public ICommand PauseRecordingCommand { get; }
    public ICommand ResumeRecordingCommand { get; }

    /// <summary>Fired when recording stops, prompting the user to analyze the corpus.</summary>
    public event Action? AnalyzePromptRequested;

    /// <summary>Fired when recording starts, so the view can attach transition detection.</summary>
    public event Action? RecordingStarted;

    /// <summary>Fired when recording stops, so the view can detach transition detection.</summary>
    public event Action? RecordingStopped;

    public void ClearSnapshots()
    {
        SessionSnapshots.Clear();
        _lastCapturedUrl = null;
    }

    public void StartRecording()
    {
        IsRecording = true;
        IsPaused = false;
        IsStopped = false;
        RecordingStatus = "Recording...";
        _logger.LogInformation("Recording started");
        RecordingStarted?.Invoke();
    }

    public void StopRecording()
    {
        IsRecording = false;
        IsPaused = false;
        IsStopped = true;
        RecordingStatus = $"Stopped — {SessionSnapshots.Count} pages captured";
        _logger.LogInformation("Recording stopped. {PageCount} pages captured", SessionSnapshots.Count);
        RecordingStopped?.Invoke();
        AnalyzePromptRequested?.Invoke();
    }

    public void PauseRecording()
    {
        IsPaused = true;
        RecordingStatus = "Paused";
        _logger.LogDebug("Recording paused");
    }

    public void ResumeRecording()
    {
        if (IsStopped)
        {
            IsRecording = true;
            IsStopped = false;
            RecordingStarted?.Invoke();
        }
        IsPaused = false;
        RecordingStatus = "Recording...";
        _logger.LogDebug("Recording resumed");
    }

    /// <summary>
    /// Called when a page transition is detected during recording.
    /// Handles deduplication (same URL within 2 seconds) and paused state.
    /// </summary>
    public bool OnPageTransition(string url, DomSnapshot snapshot)
    {
        if (!IsRecording || IsPaused)
            return false;

        // Dedup: skip if same URL within 2 seconds
        var now = DateTime.UtcNow;
        if (_lastCapturedUrl == url && (now - _lastCaptureTime).TotalSeconds < 2)
        {
            _logger.LogDebug("Skipping duplicate transition to {Url} (within 2s window)", url);
            return false;
        }

        _lastCapturedUrl = url;
        _lastCaptureTime = now;

        SessionSnapshots.Add(snapshot);
        RecordingStatus = $"+{SessionSnapshots.Count} new";
        _logger.LogInformation("Page captured: {PageName} ({Url})", snapshot.PageName, url);
        return true;
    }

    private void RaiseCommandStates()
    {
        ((RelayCommand)StartRecordingCommand).RaiseCanExecuteChanged();
        ((RelayCommand)StopRecordingCommand).RaiseCanExecuteChanged();
        ((RelayCommand)PauseRecordingCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ResumeRecordingCommand).RaiseCanExecuteChanged();
    }
}
