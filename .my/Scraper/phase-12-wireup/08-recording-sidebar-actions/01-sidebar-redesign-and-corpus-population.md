# Step 12.W.8a — Wire Sidebar Redesign & Corpus Population

## Objective

Replace the placeholder `SidebarViewModel` (empty `ObservableCollection<string>`) with a properly typed view model that loads corpus pages and controls from the database whenever a site is selected, and supports navigation on click.

## Dependencies

- `CorpusService.GetSnapshots(siteId)` — returns recorded pages
- `CorpusService.GetPageObjects(siteId)` — returns generation status
- `IControlRegistry.GetAllControls()` — returns generated controls
- `BrowserViewModel.AddressUrl` + `NavigateCommand` — for page click navigation
- `MainViewModel.ActiveSite` — currently selected site

## Implementation

### Files

| File | Action |
|------|--------|
| `Models/SidebarPageItem.cs` | Create — typed model with Name, Url, StatusIcon |
| `ViewModels/SidebarViewModel.cs` | Create (move from bottom of BrowserViewModel) — full implementation |
| `MainViewModel.cs` | Wire `ActiveSite` changes → `Sidebar.LoadFromCorpus(siteId)` |

### Code sketch

**SidebarPageItem.cs:**

```csharp
public sealed class SidebarPageItem
{
    public string Name { get; init; } = "";
    public string Url { get; init; } = "";
    public string StatusIcon { get; init; } = ""; // ✅ ⏳ 🆕 or ""
    public bool HasGeneratedCode { get; init; }
}
```

**SidebarViewModel.cs:**

```csharp
public sealed partial class SidebarViewModel : ViewModelBase
{
    private readonly CorpusService _corpusService;
    private readonly IControlRegistry _controlRegistry;

    [ObservableProperty] private string _corpusStats = "0 pages · 0 controls";
    [ObservableProperty] private bool _isRecording;

    public ObservableCollection<SidebarPageItem> CorpusPages { get; } = [];
    public ObservableCollection<SidebarPageItem> SessionPages { get; } = [];
    public ObservableCollection<string> Controls { get; } = [];

    public event Action<string>? NavigateRequested;

    public void LoadFromCorpus(string siteId)
    {
        CorpusPages.Clear();
        Controls.Clear();

        var snapshots = _corpusService.GetSnapshots(siteId);
        var pageObjects = _corpusService.GetPageObjects(siteId);
        var poLookup = pageObjects.ToDictionary(po => po.SnapshotId);

        foreach (var snap in snapshots)
        {
            var hasCode = poLookup.TryGetValue(snap.Id, out var po) &&
                          po.Status == PageObjectStatus.Generated;
            CorpusPages.Add(new SidebarPageItem
            {
                Name = snap.Title,
                Url = snap.Url,
                StatusIcon = hasCode ? "✅" : "⏳",
                HasGeneratedCode = hasCode
            });
        }

        var controls = _controlRegistry.GetAllControls();
        foreach (var ctrl in controls)
            Controls.Add(ctrl.Name);

        CorpusStats = $"{CorpusPages.Count} pages · {Controls.Count} controls";
    }

    [RelayCommand]
    private void NavigateToPage(SidebarPageItem item)
    {
        NavigateRequested?.Invoke(item.Url);
    }

    public void ClearSession()
    {
        SessionPages.Clear();
        IsRecording = false;
    }
}
```

**MainViewModel.cs:**

```csharp
// In constructor or ActiveSite setter:
partial void OnActiveSiteChanged(SiteInfo? value)
{
    if (value is not null)
        Sidebar.LoadFromCorpus(value.Id);
}

// Wire navigation
Sidebar.NavigateRequested += url =>
{
    Browser.AddressUrl = url;
    Browser.NavigateCommand.Execute(null);
};
```

## Validated Notes (Current Implementation)

- Sidebar population is currently orchestrated from `MainViewModel.RefreshSnapshotBackedCorpusUi(...)`, not from a `Sidebar.LoadFromCorpus(siteId)` method.
- `SidebarViewModel` remains a lightweight UI state holder with:
  - `LoadCorpusPages(IEnumerable<SidebarPageItem>)`
  - `AddSessionPage(DomSnapshot)`
  - `SetNavigateCallback(Action<string>)`
- `SidebarPageItem` includes `PageId`, which is used later for duplicate/overwrite and iframe-diff checks when manually recording pages.
- Corpus status icon in the implemented flow is currently `📄` for loaded corpus pages; generated/pending icon differentiation is not wired in this step.
- Navigation wiring is callback-based (`Sidebar.SetNavigateCallback(...)`) and updates `Browser.AddressUrl` + executes `Browser.NavigateCommand`.

## Checklist

- [x] `SidebarViewModel` owns typed `CorpusPages` and `Controls` collections
- [x] Sidebar populated from corpus when site is selected
- [x] Clicking a page navigates the browser to its URL
- [x] Corpus stats line shows page and control count
- [ ] Per-page generated/pending icons surfaced in this panel (future enhancement)
