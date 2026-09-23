using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Brinell.Scraper.Models;

namespace Brinell.Scraper.ViewModels;

public sealed class SidebarViewModel : ViewModelBase
{
    private string _corpusStats = "0 pages · 0 controls";
    private string _siteHeader = "";
    private bool _isRecording;
    private Action<string>? _navigateCallback;

    public ICommand NavigateToPageCommand { get; }

    public SidebarViewModel()
    {
        NavigateToPageCommand = new RelayCommand<SidebarPageItem>(NavigateToPage);
        SessionPages.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasSessionPages));
            OnPropertyChanged(nameof(ShowAnalyzeButton));
        };
    }

    public ObservableCollection<SidebarPageItem> CorpusPages { get; } = [];
    public ObservableCollection<SidebarPageItem> SessionPages { get; } = [];
    public ObservableCollection<string> Controls { get; } = [];

    public string CorpusStats
    {
        get => _corpusStats;
        set => SetProperty(ref _corpusStats, value);
    }

    public string SiteHeader
    {
        get => _siteHeader;
        set => SetProperty(ref _siteHeader, value);
    }

    public bool IsRecording
    {
        get => _isRecording;
        set
        {
            if (SetProperty(ref _isRecording, value))
                OnPropertyChanged(nameof(ShowAnalyzeButton));
        }
    }

    public bool HasSessionPages => SessionPages.Count > 0;

    public bool ShowAnalyzeButton => HasSessionPages && !IsRecording;

    public void LoadCorpusPages(IEnumerable<SidebarPageItem> pages)
    {
        CorpusPages.Clear();
        foreach (var page in pages)
            CorpusPages.Add(page);
    }

    public void AddSessionPage(DomSnapshot snapshot)
    {
        SessionPages.Add(new SidebarPageItem
        {
            Name = snapshot.PageName,
            Url = snapshot.PageUrl,
            StatusIcon = "🆕"
        });
    }

    public void ClearSession()
    {
        SessionPages.Clear();
        IsRecording = false;
    }

    public void SetNavigateCallback(Action<string> callback)
    {
        _navigateCallback = callback;
    }

    private void NavigateToPage(SidebarPageItem? item)
    {
        if (item?.Url is { Length: > 0 } url)
            _navigateCallback?.Invoke(url);
    }
}
