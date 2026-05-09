using System.Windows.Input;
using Brinell.Scraper.Data;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Brinell.Scraper.ViewModels.Tabs;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels;

public sealed class WorkspaceViewModel : ViewModelBase, IDisposable
{
    private readonly CorpusDatabase _db;
    private readonly ICopilotService _copilot;
    private readonly ILogger<WorkspaceViewModel> _logger;

    private SiteInfo? _activeSite;
    private int _selectedTabIndex;
    private bool _disposed;

    public WorkspaceViewModel(
        CorpusDatabase db,
        ScrapingTabViewModel scraping,
        ControlObjectsTabViewModel controlObjects,
        PageObjectsTabViewModel pageObjects,
        CorpusTabViewModel corpus,
        LogViewerViewModel log,
        SettingsTabViewModel settings,
        ICopilotService copilot,
        ILogger<WorkspaceViewModel> logger)
    {
        _db = db;
        _copilot = copilot;
        _logger = logger;

        Scraping = scraping;
        ControlObjects = controlObjects;
        PageObjects = pageObjects;
        Corpus = corpus;
        Log = log;
        Settings = settings;

        PageObjects.OpenSourcePageRequested += OnOpenSourcePageRequested;
        PageObjects.NavigateToControlObjectRequested += OnNavigateToControlObjectRequested;
        Corpus.OpenInBrowserRequested += OnOpenSourcePageRequested;

        BackCommand = new RelayCommand(() => BackRequested?.Invoke());
    }

    private void OnOpenSourcePageRequested(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        Scraping.Browser.AddressUrl = url;
        Scraping.Browser.NavigateCommand.Execute(null);
        SelectedTabIndex = 0;
    }

    private void OnNavigateToControlObjectRequested(string controlName)
    {
        if (string.IsNullOrWhiteSpace(controlName)) return;
        SelectedTabIndex = 1;
        var match = ControlObjects.ControlObjects
            .FirstOrDefault(c => string.Equals(c.Name, controlName, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
            ControlObjects.SelectedControlObject = match;
    }

    public SiteInfo? ActiveSite
    {
        get => _activeSite;
        private set => SetProperty(ref _activeSite, value);
    }

    public ScrapingTabViewModel Scraping { get; }
    public ControlObjectsTabViewModel ControlObjects { get; }
    public PageObjectsTabViewModel PageObjects { get; }
    public CorpusTabViewModel Corpus { get; }
    public LogViewerViewModel Log { get; }
    public SettingsTabViewModel Settings { get; }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public ICommand BackCommand { get; }

    public event Action? BackRequested;

    public async Task LoadAsync(long siteId, string? navigateUrl = null)
    {
        var site = await Task.Run(() => _db.GetAllSites().FirstOrDefault(s => s.Id == siteId));
        if (site is null)
        {
            _logger.LogWarning("Workspace load failed — site not found. SiteId: {SiteId}", siteId);
            return;
        }

        ActiveSite = site;
        _db.TouchSite(siteId);

        ControlObjects.LoadControlObjects(siteId);
        PageObjects.LoadPageObjects(siteId);
        Scraping.Session.Load(siteId, site.Name);
        Corpus.Load(siteId);
        Settings.Load(siteId);

        // Step 13.8: open Copilot session for this site.
        try
        {
            await _copilot.InitializeAsync(siteId, Slugify(site.Name));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Copilot session initialization failed — Site: {Id}", siteId);
        }

        var urlToLoad = !string.IsNullOrWhiteSpace(navigateUrl) ? navigateUrl : site.StartUrl;
        if (!string.IsNullOrWhiteSpace(urlToLoad))
        {
            Scraping.Browser.AddressUrl = urlToLoad;
            Scraping.Browser.NavigateCommand.Execute(null);
            SelectedTabIndex = 0;
        }

        _logger.LogInformation("Workspace loaded — Site: {Site} ({Id})", site.Name, site.Id);
    }

    public void LoadStandaloneSettings()
    {
        ActiveSite = null;
        Settings.Load(0);
        _logger.LogInformation("Workspace loaded in standalone settings mode");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        PageObjects.OpenSourcePageRequested -= OnOpenSourcePageRequested;
        PageObjects.NavigateToControlObjectRequested -= OnNavigateToControlObjectRequested;
        Corpus.OpenInBrowserRequested -= OnOpenSourcePageRequested;

        Scraping.Dispose();

        BackRequested = null;

        // Step 13.8: tear down Copilot session on Back-to-Start.
        _ = Task.Run(async () =>
        {
            try
            {
                await _copilot.DisposeSessionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Copilot session disposal failed");
            }
        });
    }

    private static string Slugify(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "site";
        var chars = s.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        var slug = new string(chars).Trim('-');
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return string.IsNullOrEmpty(slug) ? "site" : slug;
    }
}
