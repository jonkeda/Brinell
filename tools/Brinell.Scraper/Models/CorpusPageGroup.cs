using System.Collections.ObjectModel;
using Brinell.Scraper.ViewModels;

namespace Brinell.Scraper.Models;

public sealed class CorpusPageGroup : ViewModelBase
{
    private string _pageName = "";
    private string _pageUrl = "";
    private bool _hasControlObjects;
    private bool _controlObjectsPending;
    private PageObjectStatus _pageObjectStatus = PageObjectStatus.NotGenerated;

    public string PageName
    {
        get => _pageName;
        set => SetProperty(ref _pageName, value);
    }

    public string PageUrl
    {
        get => _pageUrl;
        set => SetProperty(ref _pageUrl, value);
    }

    public ObservableCollection<SnapshotVersionRow> Versions { get; } = [];

    public SnapshotVersionRow? LatestSnapshot => Versions.Count > 0 ? Versions[0] : null;

    public int TotalElements => LatestSnapshot?.ElementCount ?? 0;

    public bool HasControlObjects
    {
        get => _hasControlObjects;
        set
        {
            if (SetProperty(ref _hasControlObjects, value))
                OnPropertyChanged(nameof(ControlObjectIcon));
        }
    }

    public bool ControlObjectsPending
    {
        get => _controlObjectsPending;
        set
        {
            if (SetProperty(ref _controlObjectsPending, value))
                OnPropertyChanged(nameof(ControlObjectIcon));
        }
    }

    public PageObjectStatus PageObjectStatus
    {
        get => _pageObjectStatus;
        set
        {
            if (SetProperty(ref _pageObjectStatus, value))
                OnPropertyChanged(nameof(PageObjectIcon));
        }
    }

    public string ControlObjectIcon =>
        _hasControlObjects ? "✅" :
        _controlObjectsPending ? "⏳" :
        "—";

    public string PageObjectIcon => _pageObjectStatus switch
    {
        PageObjectStatus.Generated => "✅",
        PageObjectStatus.Error => "❌",
        _ => "—",
    };

    public string VersionsLabel => Versions.Count == 1 ? "1 ver." : $"{Versions.Count} vers.";
}
