using System.Windows.Input;
using Brinell.Narrator;
using Brinell.Presenter.Commands;

namespace Brinell.Presenter.ViewModels;

// The cloud voice (Settings, Narrator, Cloud voice): the narrator's voice.secrets.json, which every
// recording on the machine reads, set up, tried, saved and removed here. The key is typed in and
// never shown again: once saved, the field is emptied and the part says "key set".
public sealed partial class PresenterShellViewModel
{
    /// <summary>
    /// The user's folder for the cloud voice's file, over <c>%USERPROFILE%</c>: the Presenter's own UAT
    /// suite points it at a temporary folder, so a developer's real file is never touched.
    /// </summary>
    public const string UserFolderVariable = "BRINELL_USERPROFILE";

    /// <summary>The cloud voices offered, as a script names them.</summary>
    public static IReadOnlyList<string> CloudVoices { get; } =
        ["alloy", "ash", "ballad", "cedar", "coral", "echo", "fable", "marin", "nova", "onyx", "sage", "shimmer", "verse"];

    private string _userFolder = null!;
    private Func<string, string?> _readVariable = Environment.GetEnvironmentVariable;
    private VoiceSecretsDescription _cloud = new(null, null, null, null, false, null, []);
    private string _cloudEndpoint = string.Empty;
    private string _cloudDeployment = string.Empty;
    private string _cloudApiVersion = string.Empty;
    private string _cloudDefaultVoice = string.Empty;
    private string _cloudKey = string.Empty;
    private bool _cloudForWorkspace;
    private string _cloudVoiceStatus = string.Empty;
    private bool _removeArmed;
    private VoiceSecretsIgnore? _workspaceIgnore;

    /// <summary>The cloud voices, for the Default voice picker.</summary>
    public IReadOnlyList<string> CloudVoiceChoices => CloudVoices;

    /// <summary>Whether a cloud voice is set up: an endpoint and a key, from a file or the environment.</summary>
    public bool IsCloudVoiceSetUp => _cloud.IsSetUp;

    /// <summary>Where the cloud voice's settings come from, in words.</summary>
    public string CloudVoiceState => _cloud switch
    {
        { FromEnvironment.Count: > 0 } => $"Set up from the environment ({string.Join(", ", _cloud.FromEnvironment)})"
            + (_cloud.File is { } file ? $", over {file}." : "."),
        { IsSetUp: true, File: { } file } => $"Set up from {file}.",
        { File: { } file } => $"{file} has no endpoint or key yet.",
        _ => "Not set up.",
    };

    /// <summary>The Azure OpenAI resource, e.g. https://name.openai.azure.com.</summary>
    public string CloudEndpointText
    {
        get => _cloudEndpoint;
        set => SetProperty(ref _cloudEndpoint, value ?? string.Empty);
    }

    /// <summary>The deployment, text to speech or gpt-audio; empty for gpt-4o-mini-tts.</summary>
    public string CloudDeploymentText
    {
        get => _cloudDeployment;
        set => SetProperty(ref _cloudDeployment, value ?? string.Empty);
    }

    /// <summary>The API version; empty for the default.</summary>
    public string CloudApiVersionText
    {
        get => _cloudApiVersion;
        set => SetProperty(ref _cloudApiVersion, value ?? string.Empty);
    }

    /// <summary>The cloud voice when a script names none.</summary>
    public string CloudDefaultVoice
    {
        get => _cloudDefaultVoice;
        set => SetProperty(ref _cloudDefaultVoice, value ?? string.Empty);
    }

    /// <summary>The key as it is typed; emptied once saved. Empty on Save keeps the saved key.</summary>
    public string CloudKeyText
    {
        get => _cloudKey;
        set => SetProperty(ref _cloudKey, value ?? string.Empty);
    }

    /// <summary>What the key field says when it is empty.</summary>
    public string CloudKeyPlaceholder => _cloud.HasKey ? "Key set: leave empty to keep it" : "The resource's key";

    /// <summary>Whether Save writes beside the open workspace instead of the user's folder.</summary>
    public bool CloudSaveForWorkspace
    {
        get => _cloudForWorkspace;
        set
        {
            if (SetProperty(ref _cloudForWorkspace, value && CanSaveCloudForWorkspace))
            {
                OnPropertyChanged(nameof(CloudSavePath));
            }
        }
    }

    /// <summary>Whether git ignores the file beside the open workspace, so it may be saved there.</summary>
    public bool CanSaveCloudForWorkspace => _workspaceIgnore == VoiceSecretsIgnore.Ignored;

    /// <summary>Why the workspace may or may not hold the file.</summary>
    public string CloudWorkspaceNote => _workspaceIgnore switch
    {
        null => "Open a workspace to save it beside it.",
        VoiceSecretsIgnore.Ignored => "Git ignores the file beside this workspace.",
        VoiceSecretsIgnore.NotIgnored => $"This workspace's repository does not ignore it: its .gitignore needs {VoiceSecretsPlaces.IgnoreRule}.",
        _ => "This workspace is in no git repository: save it for this machine instead.",
    };

    /// <summary>Whether the workspace's repository lacks the ignore line, which Add adds.</summary>
    public bool CanAddCloudIgnoreRule => _workspaceIgnore == VoiceSecretsIgnore.NotIgnored;

    /// <summary>Where Save writes.</summary>
    public string CloudSavePath => CloudSaveForWorkspace && WorkspacePath is { } workspace
        ? Path.Combine(workspace, VoiceSecrets.FileName)
        : Path.Combine(_userFolder, ".brinell", VoiceSecrets.FileName);

    /// <summary>What Try, Save, Remove or Add did.</summary>
    public string CloudVoiceStatus
    {
        get => _cloudVoiceStatus;
        private set => SetProperty(ref _cloudVoiceStatus, value);
    }

    /// <summary>The Remove button's text: it asks once before it deletes.</summary>
    public string RemoveCloudVoiceText => _removeArmed ? "Click again to delete" : "Remove";

    /// <summary>Saves the fields to <see cref="CloudSavePath"/>.</summary>
    public ICommand SaveCloudVoiceCommand { get; private set; } = null!;

    /// <summary>Speaks a sample line with the fields as they are, before they are saved.</summary>
    public ICommand TryCloudVoiceCommand { get; private set; } = null!;

    /// <summary>Deletes the file the settings come from; a second click confirms.</summary>
    public ICommand RemoveCloudVoiceCommand { get; private set; } = null!;

    /// <summary>Adds the ignore line to the workspace repository's .gitignore.</summary>
    public ICommand AddCloudIgnoreRuleCommand { get; private set; } = null!;

    private void InitializeCloudVoice(string? userFolder, Func<string, string?>? readVariable)
    {
        _readVariable = readVariable ?? Environment.GetEnvironmentVariable;
        _userFolder = userFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        SaveCloudVoiceCommand = new RelayCommand(SaveCloudVoice);
        TryCloudVoiceCommand = new AsyncRelayCommand(TryCloudVoiceAsync);
        RemoveCloudVoiceCommand = new RelayCommand(RemoveCloudVoice, () => _cloud.File is not null);
        AddCloudIgnoreRuleCommand = new RelayCommand(AddCloudIgnoreRule, () => CanAddCloudIgnoreRule);
        RefreshCloudVoice(fields: true);
    }

    // The workspace changed: which places it allows, and what is set up for it.
    private void RefreshCloudVoicePlace()
    {
        _workspaceIgnore = WorkspacePath is { } workspace && Directory.Exists(workspace) ? VoiceSecretsPlaces.IsIgnored(workspace) : null;
        if (!CanSaveCloudForWorkspace)
        {
            _cloudForWorkspace = false;
        }

        OnPropertyChanged(nameof(CanSaveCloudForWorkspace));
        OnPropertyChanged(nameof(CloudSaveForWorkspace));
        OnPropertyChanged(nameof(CloudWorkspaceNote));
        OnPropertyChanged(nameof(CanAddCloudIgnoreRule));
        OnPropertyChanged(nameof(CloudSavePath));
        (AddCloudIgnoreRuleCommand as RelayCommand)?.NotifyCanExecuteChanged();
        RefreshCloudVoice(fields: true);
    }

    private void RefreshCloudVoice(bool fields)
    {
        try
        {
            _cloud = VoiceSecrets.Describe(WorkspacePath, _readVariable, _userFolder);
        }
        catch (InvalidDataException ex)
        {
            _cloud = new VoiceSecretsDescription(null, null, null, null, false, null, []);
            CloudVoiceStatus = ex.Message;
        }

        if (fields)
        {
            CloudEndpointText = _cloud.Endpoint ?? string.Empty;
            CloudDeploymentText = _cloud.Deployment ?? string.Empty;
            CloudApiVersionText = _cloud.ApiVersion ?? string.Empty;
            CloudDefaultVoice = _cloud.Voice ?? string.Empty;
            CloudKeyText = string.Empty;
        }

        _removeArmed = false;
        OnPropertyChanged(nameof(IsCloudVoiceSetUp));
        OnPropertyChanged(nameof(CloudVoiceState));
        OnPropertyChanged(nameof(CloudKeyPlaceholder));
        OnPropertyChanged(nameof(RemoveCloudVoiceText));
        OnPropertyChanged(nameof(VoiceChoices));
        (RemoveCloudVoiceCommand as RelayCommand)?.NotifyCanExecuteChanged();
    }

    private void SaveCloudVoice()
    {
        if (VoiceSecrets.EndpointProblem(CloudEndpointText) is { } problem)
        {
            CloudVoiceStatus = problem;
            return;
        }

        var path = CloudSavePath;
        try
        {
            VoiceSecrets.Save(path, CloudEndpointText, CloudDeploymentText, CloudApiVersionText, CloudKeyText, CloudDefaultVoice);
            RefreshCloudVoice(fields: true);
            CloudVoiceStatus = $"Saved to {path}.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            CloudVoiceStatus = $"Not saved: {ex.Message}";
        }
    }

    private async Task TryCloudVoiceAsync()
    {
        // The key typed in, else the one that is set up: Try works before Save, and after it.
        var key = CloudKeyText.Trim().Length > 0 ? CloudKeyText.Trim() : VoiceSecrets.Find(WorkspacePath, _readVariable, _userFolder)?.Key;
        if (CloudEndpointText.Trim().Length == 0 || key is null)
        {
            CloudVoiceStatus = "Try needs an endpoint and a key.";
            return;
        }

        if (VoiceSecrets.EndpointProblem(CloudEndpointText) is { } problem)
        {
            CloudVoiceStatus = problem;
            return;
        }

        var secrets = new VoiceSecrets(
            CloudEndpointText.Trim(),
            CloudDeploymentText.Trim() is { Length: > 0 } deployment ? deployment : VoiceSecrets.DefaultDeployment,
            CloudApiVersionText.Trim() is { Length: > 0 } version ? version : VoiceSecrets.DefaultApiVersion,
            key);
        var voice = AzureOpenAiSpeech.Prefix + (CloudDefaultVoice.Trim() is { Length: > 0 } named ? named : AzureOpenAiSpeech.DefaultVoice);
        CloudVoiceStatus = "Speaking...";
        try
        {
            await _recorder.TryCloudVoiceAsync("This is the voice of your video.", voice, secrets, CancellationToken.None).ConfigureAwait(true);
            CloudVoiceStatus = $"Spoken in {voice}.";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            CloudVoiceStatus = ex is VoiceServiceException ? ex.Message : $"The voice could not be tried: {ex.Message}";
        }
    }

    private void RemoveCloudVoice()
    {
        if (_cloud.File is not { } file)
        {
            return;
        }

        if (!_removeArmed)
        {
            _removeArmed = true;
            OnPropertyChanged(nameof(RemoveCloudVoiceText));
            CloudVoiceStatus = $"Click again to delete {file}.";
            return;
        }

        VoiceSecrets.Delete(file);
        RefreshCloudVoice(fields: true);
        CloudVoiceStatus = $"Deleted {file}." + (_cloud.FromEnvironment.Count > 0 ? " The environment's settings stay." : string.Empty);
    }

    private void AddCloudIgnoreRule()
    {
        if (WorkspacePath is not { } workspace)
        {
            return;
        }

        try
        {
            var ignore = VoiceSecretsPlaces.AddIgnoreRule(workspace);
            RefreshCloudVoicePlace();
            CloudVoiceStatus = $"Added {VoiceSecretsPlaces.IgnoreRule} to {ignore}.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            CloudVoiceStatus = $"The line could not be added: {ex.Message}";
        }
    }
}
