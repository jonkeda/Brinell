using System.Globalization;
using System.Windows.Input;
using Bruinet.Narrator;
using Bruinet.Presenter.Commands;

namespace Bruinet.Presenter.ViewModels;

// Azure Speech (Settings, Narrator, Azure Speech): the speech object of the same voice.secrets.json the Cloud
// voice part writes (so the workspace choice and the path are that part's), set up, tried, saved and removed here.
// The key is typed in and never shown again: once saved, the field is emptied and the part says "key set".
public sealed partial class PresenterShellViewModel
{
    private SpeechSecretsDescription _speech = new(null, null, SpeechSecrets.DefaultSampleRate, false, null, []);
    private string _speechEndpoint = string.Empty;
    private string _speechKey = string.Empty;
    private string _speechDefaultVoice = string.Empty;
    private int _speechSampleRate = SpeechSecrets.DefaultSampleRate;
    private string _speechLocale = CultureInfo.CurrentUICulture.Name;
    private string _speechStatus = string.Empty;
    private bool _speechRemoveArmed;
    private IReadOnlyList<string> _speechVoiceChoices = [];

    /// <summary>The sample rates offered.</summary>
    public IReadOnlyList<int> SpeechSampleRateChoices { get; } = [SpeechSecrets.DefaultSampleRate, SpeechSecrets.HighSampleRate];

    /// <summary>Whether Azure Speech is set up: an endpoint and a key, from a file or the environment.</summary>
    public bool IsSpeechSetUp => _speech.IsSetUp;

    /// <summary>Where Azure Speech's settings come from, in words.</summary>
    public string SpeechState => _speech switch
    {
        { FromEnvironment.Count: > 0 } => $"Set up from the environment ({string.Join(", ", _speech.FromEnvironment)})"
            + (_speech.File is { } file ? $", over {file}." : "."),
        { IsSetUp: true, File: { } file } => $"Set up from {file}.",
        { File: { } file } => $"{file} has no Azure Speech endpoint or key yet.",
        _ => "Not set up.",
    };

    /// <summary>A region (eastus2) or the resource's URL.</summary>
    public string SpeechEndpointText
    {
        get => _speechEndpoint;
        set => SetProperty(ref _speechEndpoint, value ?? string.Empty);
    }

    /// <summary>The key as it is typed; emptied once saved. Empty on Save keeps the saved key.</summary>
    public string SpeechKeyText
    {
        get => _speechKey;
        set => SetProperty(ref _speechKey, value ?? string.Empty);
    }

    /// <summary>What the key field says when it is empty.</summary>
    public string SpeechKeyPlaceholder => _speech.HasKey ? "Key set: leave empty to keep it" : "The Speech resource's key";

    /// <summary>The voice when a script names none, as the service names it; empty for Emily.</summary>
    public string SpeechDefaultVoice
    {
        get => _speechDefaultVoice;
        set => SetProperty(ref _speechDefaultVoice, value ?? string.Empty);
    }

    /// <summary>The voice picked from the list; it becomes <see cref="SpeechDefaultVoice"/>.</summary>
    public string? SpeechPickedVoice
    {
        get => SpeechVoiceChoices.Contains(_speechDefaultVoice) ? _speechDefaultVoice : null;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                SpeechDefaultVoice = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>24000 or 48000.</summary>
    public int SpeechSampleRate
    {
        get => _speechSampleRate;
        set => SetProperty(ref _speechSampleRate, value);
    }

    /// <summary>The language the voice list shows, e.g. en-GB; the system's to begin with.</summary>
    public string SpeechLocale
    {
        get => _speechLocale;
        set
        {
            if (SetProperty(ref _speechLocale, value ?? string.Empty))
            {
                ReloadSpeechVoiceChoices();
            }
        }
    }

    /// <summary>The voices of <see cref="SpeechLocale"/> from the kept list; empty until the list is loaded.</summary>
    public IReadOnlyList<string> SpeechVoiceChoices
    {
        get => _speechVoiceChoices;
        private set
        {
            if (SetProperty(ref _speechVoiceChoices, value))
            {
                OnPropertyChanged(nameof(SpeechPickedVoice));
                OnPropertyChanged(nameof(VoiceChoices));
            }
        }
    }

    /// <summary>What Load, Try, Save or Remove did.</summary>
    public string SpeechStatus
    {
        get => _speechStatus;
        private set => SetProperty(ref _speechStatus, value);
    }

    /// <summary>The Remove button's text: it asks once before it deletes.</summary>
    public string RemoveSpeechText => _speechRemoveArmed ? "Click again to delete" : "Remove";

    /// <summary>Saves the fields to <see cref="CloudSavePath"/>, keeping the Azure OpenAI voice's.</summary>
    public ICommand SaveSpeechCommand { get; private set; } = null!;

    /// <summary>Speaks a sample line with the fields as they are, before they are saved.</summary>
    public ICommand TrySpeechCommand { get; private set; } = null!;

    /// <summary>Removes the Azure Speech settings from the file; a second click confirms.</summary>
    public ICommand RemoveSpeechCommand { get; private set; } = null!;

    /// <summary>Asks the service for its voices and keeps the list.</summary>
    public ICommand LoadSpeechVoicesCommand { get; private set; } = null!;

    private void InitializeSpeech()
    {
        SaveSpeechCommand = new RelayCommand(SaveSpeech);
        TrySpeechCommand = new AsyncRelayCommand(TrySpeechAsync);
        RemoveSpeechCommand = new RelayCommand(RemoveSpeech, () => _speech.File is not null);
        LoadSpeechVoicesCommand = new AsyncRelayCommand(LoadSpeechVoicesAsync);
        RefreshSpeech(fields: true);
    }

    private void RefreshSpeech(bool fields)
    {
        try
        {
            _speech = SpeechSecrets.Describe(WorkspacePath, _readVariable, _userFolder);
        }
        catch (InvalidDataException ex)
        {
            _speech = new SpeechSecretsDescription(null, null, SpeechSecrets.DefaultSampleRate, false, null, []);
            SpeechStatus = ex.Message;
        }

        if (fields)
        {
            SpeechEndpointText = _speech.Endpoint ?? string.Empty;
            SpeechDefaultVoice = _speech.Voice ?? string.Empty;
            SpeechSampleRate = _speech.SampleRate;
            SpeechKeyText = string.Empty;
        }

        _speechRemoveArmed = false;
        ReloadSpeechVoiceChoices();
        OnPropertyChanged(nameof(IsSpeechSetUp));
        OnPropertyChanged(nameof(SpeechState));
        OnPropertyChanged(nameof(SpeechKeyPlaceholder));
        OnPropertyChanged(nameof(RemoveSpeechText));
        OnPropertyChanged(nameof(VoiceChoices));
        (RemoveSpeechCommand as RelayCommand)?.NotifyCanExecuteChanged();
    }

    // The kept list only: showing the part never calls the service.
    private void ReloadSpeechVoiceChoices()
    {
        IReadOnlyList<string> choices = [];
        try
        {
            if (SpeechSecrets.Find(WorkspacePath, _readVariable, _userFolder) is { } secrets && AzureSpeechVoices.Kept(secrets) is { } kept)
            {
                choices = [.. AzureSpeechVoices.Of(kept, SpeechLocale).Select(voice => voice.ShortName)];
            }
        }
        catch (InvalidDataException)
        {
            // Broken settings are said by RefreshSpeech.
        }

        SpeechVoiceChoices = choices;
    }

    // The voices a script may name, for the Default voice picker: Azure Speech's default voice, then the kept list's for the locale.
    private IEnumerable<string> SpeechVoicesForPicker()
    {
        if (!_speech.IsSetUp)
        {
            return [];
        }

        var voices = new List<string> { _speech.Voice ?? SpeechSecrets.DefaultVoice };
        voices.AddRange(SpeechVoiceChoices.Where(voice => !voices.Contains(voice)));
        return voices.Select(voice => AzureSpeech.Prefix + voice);
    }

    private void SaveSpeech()
    {
        if (SpeechSecrets.EndpointProblem(SpeechEndpointText) is { } problem)
        {
            SpeechStatus = problem;
            return;
        }

        var path = CloudSavePath;
        try
        {
            SpeechSecrets.Save(path, SpeechEndpointText, SpeechKeyText, SpeechDefaultVoice, SpeechSampleRate);
            RefreshSpeech(fields: true);
            SpeechStatus = $"Saved to {path}.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            SpeechStatus = $"Not saved: {ex.Message}";
        }
    }

    // The settings as typed: the key typed in, else the one that is set up, so Try and Load work before Save, and after it.
    private SpeechSecrets? TypedSpeech()
    {
        string? key;
        try
        {
            key = SpeechKeyText.Trim().Length > 0 ? SpeechKeyText.Trim() : SpeechSecrets.Find(WorkspacePath, _readVariable, _userFolder)?.Key;
        }
        catch (InvalidDataException ex)
        {
            SpeechStatus = ex.Message;
            return null;
        }

        if (SpeechEndpointText.Trim().Length == 0 || key is null)
        {
            SpeechStatus = "Azure Speech needs an endpoint and a key.";
            return null;
        }

        if ((SpeechSecrets.EndpointProblem(SpeechEndpointText) ?? SpeechSecrets.SampleRateProblem(SpeechSampleRate)) is { } problem)
        {
            SpeechStatus = problem;
            return null;
        }

        return new SpeechSecrets(SpeechEndpointText.Trim(), key)
        {
            Voice = SpeechDefaultVoice.Trim() is { Length: > 0 } named ? named : null,
            SampleRate = SpeechSampleRate,
        };
    }

    private async Task TrySpeechAsync()
    {
        if (TypedSpeech() is not { } secrets)
        {
            return;
        }

        var voice = AzureSpeech.Prefix + (secrets.Voice ?? SpeechSecrets.DefaultVoice);
        SpeechStatus = "Speaking...";
        try
        {
            await _recorder.TryAzureSpeechAsync("This is the voice of your video.", voice, secrets, CancellationToken.None).ConfigureAwait(true);
            SpeechStatus = $"Spoken in {voice}.";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SpeechStatus = ex is VoiceServiceException ? ex.Message : $"The voice could not be tried: {ex.Message}";
        }
    }

    private async Task LoadSpeechVoicesAsync()
    {
        if (TypedSpeech() is not { } secrets)
        {
            return;
        }

        SpeechStatus = "Asking the service for its voices...";
        try
        {
            var voices = await _recorder.ListSpeechVoicesAsync(secrets, SpeechLocale, CancellationToken.None).ConfigureAwait(true);
            SpeechVoiceChoices = [.. voices.Select(voice => voice.ShortName)];
            SpeechStatus = voices.Count == 0
                ? $"The service lists no voice for {SpeechLocale}: pick another locale, or type the voice's name."
                : $"{voices.Count} voices for {SpeechLocale}.";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SpeechStatus = ex is VoiceServiceException ? ex.Message : $"The voices could not be listed: {ex.Message}";
        }
    }

    private void RemoveSpeech()
    {
        if (_speech.File is not { } file)
        {
            return;
        }

        if (!_speechRemoveArmed)
        {
            _speechRemoveArmed = true;
            OnPropertyChanged(nameof(RemoveSpeechText));
            SpeechStatus = $"Click again to remove Azure Speech from {file}.";
            return;
        }

        SpeechSecrets.Delete(file);
        RefreshSpeech(fields: true);
        SpeechStatus = $"Removed Azure Speech from {file}." + (_speech.FromEnvironment.Count > 0 ? " The environment's settings stay." : string.Empty);
    }
}
