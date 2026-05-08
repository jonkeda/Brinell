using System.Collections.ObjectModel;
using Brinell.Scraper.Data;
using Brinell.Scraper.Models;
using Microsoft.Extensions.Logging;

namespace Brinell.Scraper.ViewModels.Tabs;

public sealed class SessionPanelViewModel : ViewModelBase
{
    private readonly CorpusDatabase _db;
    private readonly ILogger<SessionPanelViewModel> _logger;

    public SessionPanelViewModel(CorpusDatabase db, ILogger<SessionPanelViewModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public ObservableCollection<SidebarPageItem> RecordedPages { get; } = [];
    public ObservableCollection<SidebarPageItem> CorpusPages { get; } = [];

    public void Load(long siteId)
    {
        CorpusPages.Clear();
        var pages = _db.GetPages(siteId);
        foreach (var p in pages)
        {
            CorpusPages.Add(new SidebarPageItem
            {
                PageId = p.Id,
                Name = p.Name,
                Url = p.Url,
                StatusIcon = "\ud83d\udcc4",
            });
        }
        _logger.LogInformation("Session panel loaded — Site: {SiteId}, CorpusPages: {Count}", siteId, CorpusPages.Count);
    }
}
