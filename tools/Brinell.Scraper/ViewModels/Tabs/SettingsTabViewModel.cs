using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Brinell.Scraper.Data;
using Brinell.Scraper.Services;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels.Tabs;

public sealed class SettingsTabViewModel : ViewModelBase
{
    private readonly CorpusDatabase _db;
    private readonly AppSettings _settings;
    private readonly ICopilotService _copilot;
    private readonly ISessionContext _sessionContext;
    private readonly ILogger<SettingsTabViewModel> _logger;

    private long _siteId;
    private bool _isSiteContextActive;
    private string _siteName = "";
    private string _startUrl = "";
    private string _outputPath = "";
    private string _targetNamespace = "";
    private List<string> _aliases = [];
    private static readonly string[] FallbackModels =
    [
        "auto",
        "claude-haiku-4.5",
        "claude-opus-4.6",
        "claude-sonnet-4.6",
        "gpt-4.1",
        "gpt-5.3-codex",
        "gpt-5.4",
        "gpt-5.4-mini",
    ];

    private string _analyzerModel = "claude-haiku-4.5";
    private string _generatorModel = "claude-haiku-4.5";
    private bool _logLlmPrompts;
    private bool _logLlmResponses;
    private string _corpusRoot = "";
    private string _skillsRoot = "";
    private bool _isCopilotAuthenticated;
    private string _copilotStatus = "Not signed in";

    public SettingsTabViewModel(
        CorpusDatabase db,
        AppSettings settings,
        ICopilotService copilot,
        ISessionContext sessionContext,
        ILogger<SettingsTabViewModel> logger)
    {
        _db = db;
        _settings = settings;
        _copilot = copilot;
        _sessionContext = sessionContext;
        _logger = logger;

        AvailableModels = new(FallbackModels);

        SaveCommand = new AsyncRelayCommand(_ => Task.Run(Save));
        ResetCommand = new RelayCommand(Reset);
        BrowseOutputPathCommand = new RelayCommand(BrowseOutputPath);
        SignInToGitHubCommand = new AsyncRelayCommand(SignInAsync);
        RefreshModelsCommand = new AsyncRelayCommand(RefreshModelsAsync);

        LoadAppSettings();
        RefreshCopilotStatus();
    }

    public ObservableCollection<string> AvailableModels { get; }

    public bool IsSiteContextActive
    {
        get => _isSiteContextActive;
        private set => SetProperty(ref _isSiteContextActive, value);
    }

    public string SiteName { get => _siteName; set => SetProperty(ref _siteName, value); }
    public string StartUrl { get => _startUrl; set => SetProperty(ref _startUrl, value); }
    public string OutputPath { get => _outputPath; set => SetProperty(ref _outputPath, value); }
    public string TargetNamespace { get => _targetNamespace; set => SetProperty(ref _targetNamespace, value); }

    public string AnalyzerModel { get => _analyzerModel; set => SetProperty(ref _analyzerModel, value); }
    public string GeneratorModel { get => _generatorModel; set => SetProperty(ref _generatorModel, value); }
    public bool LogLlmPrompts { get => _logLlmPrompts; set => SetProperty(ref _logLlmPrompts, value); }
    public bool LogLlmResponses { get => _logLlmResponses; set => SetProperty(ref _logLlmResponses, value); }
    public string CorpusRoot { get => _corpusRoot; private set => SetProperty(ref _corpusRoot, value); }
    public string SkillsRoot { get => _skillsRoot; private set => SetProperty(ref _skillsRoot, value); }

    public bool IsCopilotAuthenticated
    {
        get => _isCopilotAuthenticated;
        private set
        {
            if (SetProperty(ref _isCopilotAuthenticated, value))
                CopilotStatus = value ? "Authenticated" : "Not signed in";
        }
    }

    public string CopilotStatus
    {
        get => _copilotStatus;
        private set => SetProperty(ref _copilotStatus, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand BrowseOutputPathCommand { get; }
    public ICommand SignInToGitHubCommand { get; }
    public ICommand RefreshModelsCommand { get; }

    public void Load(long siteId)
    {
        _siteId = siteId;

        if (siteId > 0)
        {
            var site = _db.GetAllSites().FirstOrDefault(s => s.Id == siteId);
            if (site is not null)
            {
                SiteName = site.Name;
                StartUrl = site.StartUrl;
                OutputPath = site.OutputPath;
                TargetNamespace = site.Namespace;
                _aliases = [.. site.UrlAliases];
                IsSiteContextActive = true;
            }
            else
            {
                _logger.LogWarning("Settings load — site not found. SiteId: {SiteId}", siteId);
                ClearSiteContext();
            }
        }
        else
        {
            ClearSiteContext();
        }

        LoadAppSettings();
        RefreshCopilotStatus();
    }

    private void ClearSiteContext()
    {
        IsSiteContextActive = false;
        SiteName = "";
        StartUrl = "";
        OutputPath = "";
        TargetNamespace = "";
        _aliases = [];
    }

    private void LoadAppSettings()
    {
        AnalyzerModel = _settings.AnalyzerModel;
        GeneratorModel = _settings.GeneratorModel;
        LogLlmPrompts = _settings.LogLlmPrompts;
        LogLlmResponses = _settings.LogLlmResponses;
        CorpusRoot = _settings.CorpusRoot;
        SkillsRoot = _settings.SkillsRoot;
    }

    private void Save()
    {
        try
        {
            if (IsSiteContextActive && _siteId > 0)
            {
                _db.UpdateSite(
                    _siteId,
                    SiteName.Trim(),
                    StartUrl.Trim(),
                    TargetNamespace.Trim(),
                    OutputPath.Trim(),
                    _aliases);
                _logger.LogInformation("Settings saved — Site: {Site} ({Id})", SiteName, _siteId);
            }

            _settings.AnalyzerModel = AnalyzerModel;
            _settings.GeneratorModel = GeneratorModel;
            _settings.LogLlmPrompts = LogLlmPrompts;
            _settings.LogLlmResponses = LogLlmResponses;
            _settings.Save();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Settings save failed");
        }
    }

    private void Reset() => Load(_siteId);

    private void BrowseOutputPath()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select output folder",
            InitialDirectory = string.IsNullOrWhiteSpace(OutputPath) ? "" : OutputPath,
        };
        if (dialog.ShowDialog() == true)
            OutputPath = dialog.FolderName;
    }

    private async Task SignInAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Copilot re-authentication requested");
            CopilotStatus = "Connecting...";

            var siteId = _sessionContext.CurrentSiteId ?? 0;
            var slug = _sessionContext.CurrentSiteSlug ?? "default";

            if (siteId > 0)
                await _copilot.InitializeAsync(siteId, slug, ct);

            RefreshCopilotStatus();

            if (!_copilot.IsAuthenticated)
                await TryCliAuthLoginAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copilot connection attempt failed");
            IsCopilotAuthenticated = false;
            await TryCliAuthLoginAsync(ct);
        }
    }

    private async Task TryCliAuthLoginAsync(CancellationToken ct)
    {
        var cliPath = _copilot.GetCliPath();
        if (cliPath is null)
        {
            CopilotStatus = "Copilot CLI not found \u2014 cannot open login";
            return;
        }

        CopilotStatus = "Waiting for browser login...";
        try
        {
            var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoExit -Command \"\"{cliPath}\" login\"",
                UseShellExecute = true,
            });
            if (proc is not null)
                await proc.WaitForExitAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to launch copilot login");
            CopilotStatus = "Failed to launch Copilot login";
            return;
        }

        // Retry after login completes.
        CopilotStatus = "Login complete \u2014 reconnecting...";
        var siteId = _sessionContext.CurrentSiteId ?? 0;
        var slug = _sessionContext.CurrentSiteSlug ?? "default";
        if (siteId > 0)
            await _copilot.InitializeAsync(siteId, slug, ct);

        RefreshCopilotStatus();

        if (!_copilot.IsAuthenticated)
            CopilotStatus = FormatCopilotFailureStatus(_copilot.LastInitError);
    }

    private static string FormatCopilotFailureStatus(string? rawError)
    {
        if (string.IsNullOrWhiteSpace(rawError))
            return "Authentication failed - check browser login";

        if (rawError.Contains("session.create", StringComparison.OrdinalIgnoreCase))
            return "Copilot session could not be created - complete login and retry";

        if (rawError.Contains("model", StringComparison.OrdinalIgnoreCase))
            return "Selected model is unavailable - choose a listed model and retry";

        const int max = 110;
        return rawError.Length <= max ? rawError : rawError[..max] + "...";
    }

    private async Task RefreshModelsAsync(CancellationToken ct)
    {
        var models = await _copilot.ListModelsAsync(ct);
        if (models.Count == 0)
            return;

        var currentAnalyzer = AnalyzerModel;
        var currentGenerator = GeneratorModel;

        AvailableModels.Clear();
        foreach (var model in models)
            AvailableModels.Add(model);

        AnalyzerModel = currentAnalyzer;
        GeneratorModel = currentGenerator;
    }

    private void RefreshCopilotStatus()
    {
        IsCopilotAuthenticated = _copilot.IsAuthenticated;
        if (_copilot.IsAuthenticated)
            RefreshModelsCommand.Execute(null);
    }
}
