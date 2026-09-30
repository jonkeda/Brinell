using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels.Tabs;

public sealed class SessionPanelViewModel : ViewModelBase
{
    private readonly CorpusService _corpusService;
    private readonly IControlRegistry _controlRegistry;
    private readonly ILogger<SessionPanelViewModel> _logger;
    private long _siteId;
    private string _siteHeader = string.Empty;
    private string _corpusStats = "0 pages · 0 controls";
    private bool _isRecording;
    private SidebarPageItem? _selectedCorpusPage;
    private SidebarPageItem? _selectedRecordingPage;
    private Action<string>? _navigateCallback;
    private Action<SidebarPageItem>? _removeCorpusCallback;
    private Action<SidebarPageItem>? _removeRecordingCallback;
    private Action? _clearRecordingsCallback;
    private Action? _transferSessionToCorpusCallback;
    private readonly RelayCommand _navigateSelectedCorpusCommand;
    private readonly RelayCommand _removeSelectedCorpusCommand;
    private readonly RelayCommand _navigateSelectedRecordingCommand;
    private readonly RelayCommand _removeSelectedRecordingCommand;
    private readonly RelayCommand _clearRecordingsCommand;
    private readonly RelayCommand _transferSessionToCorpusCommand;

    public SessionPanelViewModel(
        CorpusService corpusService,
        IControlRegistry controlRegistry,
        ILogger<SessionPanelViewModel> logger)
    {
        _corpusService = corpusService;
        _controlRegistry = controlRegistry;
        _logger = logger;

        _navigateSelectedCorpusCommand = new RelayCommand(
            NavigateSelectedCorpus,
            () => SelectedCorpusPage is not null && !string.IsNullOrWhiteSpace(SelectedCorpusPage.Url));
        _removeSelectedCorpusCommand = new RelayCommand(
            RemoveSelectedCorpus,
            () => SelectedCorpusPage is not null);
        _navigateSelectedRecordingCommand = new RelayCommand(
            NavigateSelectedRecording,
            () => SelectedRecordingPage is not null && !string.IsNullOrWhiteSpace(SelectedRecordingPage.Url));
        _removeSelectedRecordingCommand = new RelayCommand(
            RemoveSelectedRecording,
            () => SelectedRecordingPage is not null);
        _clearRecordingsCommand = new RelayCommand(
            ClearRecordings,
            () => RecordedPages.Count > 0);
        _transferSessionToCorpusCommand = new RelayCommand(
            TransferSessionToCorpus,
            () => SiteId > 0 && RecordedPages.Count > 0);

        NavigateSelectedCorpusCommand = _navigateSelectedCorpusCommand;
        RemoveSelectedCorpusCommand = _removeSelectedCorpusCommand;
        NavigateSelectedRecordingCommand = _navigateSelectedRecordingCommand;
        RemoveSelectedRecordingCommand = _removeSelectedRecordingCommand;
        ClearRecordingsCommand = _clearRecordingsCommand;
        TransferSessionToCorpusCommand = _transferSessionToCorpusCommand;

        CorpusPages.CollectionChanged += OnCollectionChanged;
        Controls.CollectionChanged += OnCollectionChanged;
        RecordedPages.CollectionChanged += OnRecordedPagesChanged;
    }

    public ObservableCollection<SidebarPageItem> RecordedPages { get; } = [];
    public ObservableCollection<SidebarPageItem> CorpusPages { get; } = [];
    public ObservableCollection<string> Controls { get; } = [];

    public ICommand NavigateSelectedCorpusCommand { get; }
    public ICommand RemoveSelectedCorpusCommand { get; }
    public ICommand NavigateSelectedRecordingCommand { get; }
    public ICommand RemoveSelectedRecordingCommand { get; }
    public ICommand ClearRecordingsCommand { get; }
    public ICommand TransferSessionToCorpusCommand { get; }

    public SidebarPageItem? SelectedCorpusPage
    {
        get => _selectedCorpusPage;
        set
        {
            if (SetProperty(ref _selectedCorpusPage, value))
                RaiseCommandStates();
        }
    }

    public SidebarPageItem? SelectedRecordingPage
    {
        get => _selectedRecordingPage;
        set
        {
            if (SetProperty(ref _selectedRecordingPage, value))
                RaiseCommandStates();
        }
    }

    public string SiteHeader
    {
        get => _siteHeader;
        private set => SetProperty(ref _siteHeader, value);
    }

    public long SiteId
    {
        get => _siteId;
        private set
        {
            if (SetProperty(ref _siteId, value))
                RaiseCommandStates();
        }
    }

    public string CorpusStats
    {
        get => _corpusStats;
        private set => SetProperty(ref _corpusStats, value);
    }

    public bool IsRecording
    {
        get => _isRecording;
        set
        {
            if (SetProperty(ref _isRecording, value))
                OnPropertyChanged(nameof(SessionSummary));
        }
    }

    public string SessionSummary
    {
        get
        {
            if (RecordedPages.Count == 0)
                return "No pages captured yet";

            var total = CorpusPages.Count + RecordedPages.Count;
            if (IsRecording)
                return $"+{RecordedPages.Count} new · {total} total";

            return $"{RecordedPages.Count} captured this session";
        }
    }

    public void Load(long siteId, string siteName)
    {
        CorpusPages.Clear();
        Controls.Clear();
        SelectedCorpusPage = null;

        SiteId = siteId;
        SiteHeader = siteName;

        var pages = _corpusService.ListPagesBySiteId(siteId);
        foreach (var p in pages)
        {
            CorpusPages.Add(new SidebarPageItem
            {
                PageId = p.LatestSnapshotId,
                Name = p.PageName,
                Url = p.PageUrl,
                StatusIcon = "\ud83d\udcc4",
            });
        }

        foreach (var control in _controlRegistry.GetAllControls())
            Controls.Add(control.Name);

        UpdateCorpusStats();

        _logger.LogInformation("Session panel loaded — Site: {SiteId}, CorpusPages: {Count}", siteId, CorpusPages.Count);
    }

    public void SyncRecordedPages(IEnumerable<DomSnapshot> snapshots)
    {
        RecordedPages.Clear();
        foreach (var snapshot in snapshots)
        {
            RecordedPages.Add(new SidebarPageItem
            {
                Name = snapshot.PageName,
                Url = snapshot.PageUrl,
                StatusIcon = "🆕"
            });
        }

        if (SelectedRecordingPage is not null && !RecordedPages.Contains(SelectedRecordingPage))
            SelectedRecordingPage = null;

        OnPropertyChanged(nameof(SessionSummary));
        RaiseCommandStates();
    }

    public void SetNavigateCallback(Action<string> callback)
    {
        _navigateCallback = callback;
    }

    public void SetRemoveCallbacks(
        Action<SidebarPageItem> removeCorpus,
        Action<SidebarPageItem> removeRecording,
        Action clearRecordings)
    {
        _removeCorpusCallback = removeCorpus;
        _removeRecordingCallback = removeRecording;
        _clearRecordingsCallback = clearRecordings;
    }

    public void SetTransferSessionToCorpusCallback(Action transferSessionToCorpus)
    {
        _transferSessionToCorpusCallback = transferSessionToCorpus;
    }

    private void NavigateSelectedCorpus()
    {
        if (SelectedCorpusPage?.Url is { Length: > 0 } url)
            _navigateCallback?.Invoke(url);
    }

    private void RemoveSelectedCorpus()
    {
        var selected = SelectedCorpusPage;
        if (selected is null)
            return;

        _removeCorpusCallback?.Invoke(selected);
    }

    private void NavigateSelectedRecording()
    {
        if (SelectedRecordingPage?.Url is { Length: > 0 } url)
            _navigateCallback?.Invoke(url);
    }

    private void RemoveSelectedRecording()
    {
        var selected = SelectedRecordingPage;
        if (selected is null)
            return;

        _removeRecordingCallback?.Invoke(selected);
    }

    private void ClearRecordings()
    {
        _clearRecordingsCallback?.Invoke();
    }

    private void TransferSessionToCorpus()
    {
        _transferSessionToCorpusCallback?.Invoke();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateCorpusStats();
        OnPropertyChanged(nameof(SessionSummary));

        if (SelectedCorpusPage is not null && !CorpusPages.Contains(SelectedCorpusPage))
            SelectedCorpusPage = null;

        RaiseCommandStates();
    }

    private void OnRecordedPagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(SessionSummary));

        if (SelectedRecordingPage is not null && !RecordedPages.Contains(SelectedRecordingPage))
            SelectedRecordingPage = null;

        RaiseCommandStates();
    }

    private void UpdateCorpusStats()
    {
        CorpusStats = $"{CorpusPages.Count} pages · {Controls.Count} controls";
    }

    private void RaiseCommandStates()
    {
        _navigateSelectedCorpusCommand.RaiseCanExecuteChanged();
        _removeSelectedCorpusCommand.RaiseCanExecuteChanged();
        _navigateSelectedRecordingCommand.RaiseCanExecuteChanged();
        _removeSelectedRecordingCommand.RaiseCanExecuteChanged();
        _clearRecordingsCommand.RaiseCanExecuteChanged();
        _transferSessionToCorpusCommand.RaiseCanExecuteChanged();
    }
}
