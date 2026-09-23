# Step 12.1 — Start Page (Full-Screen Site Manager)

## Objective

Replace the embedded site selector with a dedicated full-screen Start Page shown on app launch and when no site is active. Site management happens here only — no toolbar, no log, no sidebar.

## Dependencies

- Phase 1 (WPF shell, MVVM foundation, DI)
- Existing `SiteService`, `NewSiteDialog`, `CorpusService`

## Implementation

### Files

- `Views/StartPage.xaml` (+ code-behind)
- `ViewModels/StartPageViewModel.cs`
- `Models/SiteCardItem.cs`

### `StartPageViewModel`

```csharp
public class StartPageViewModel : ViewModelBase
{
    private readonly SiteService _sites;
    private readonly CorpusService _corpus;

    public ObservableCollection<SiteCardItem> Sites { get; } = new();
    public ICollectionView FilteredSites { get; }

    public string SearchText { get => _search; set { ... FilteredSites.Refresh(); } }

    public ICommand OpenSiteCommand { get; }     // SiteCardItem → raises SiteSelected
    public ICommand EditSiteCommand { get; }     // opens NewSiteDialog in edit mode
    public ICommand DeleteSiteCommand { get; }   // confirm → deletes site + corpus
    public ICommand NewSiteCommand { get; }      // opens NewSiteDialog → SiteSelected
    public ICommand OpenSettingsCommand { get; }

    public event Action<SiteCardItem>? SiteSelected;
    public event Action? SettingsRequested;

    public async Task LoadAsync();   // populates Sites with PageCount/ControlCount/LastOpenedAt
}
```

### `SiteCardItem`

```csharp
public class SiteCardItem
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string StartUrl { get; set; } = "";
    public string DomainShort { get; set; } = "";   // host only
    public int PageCount { get; set; }
    public int ControlCount { get; set; }
    public DateTime? LastOpenedAt { get; set; }
    public string LastOpenedRelative { get; set; } = "never";
}
```

### `StartPage.xaml` layout

- Root: `Grid` filling whole window with subtle gradient background.
- Centered column ~960px max width.
- Top: large title "🔍 Brinell Scraper" + version label bottom-right.
- Search `TextBox` (rounded, full width).
- Header row "Recent Sites" + `[+ New]` button.
- `ItemsControl` bound to `FilteredSites` with `WrapPanel` `ItemsPanelTemplate`.
- Each card: `Border` (CornerRadius=8, drop shadow, hover trigger to raise) containing site name, domain, counts, relative date, `[Open] [⚙] [🗑]` buttons.
- Bottom bar: `⚙ Settings` button (left), version (right).

### Behavior

- Search filters `Name` and `StartUrl` (case-insensitive contains).
- `Open` raises `SiteSelected` → handled by shell to navigate to Workspace (Step 12.2 / 12.9).
- `Edit` opens existing `NewSiteDialog` populated with site values.
- `Delete` shows confirmation, then `SiteService.DeleteAsync(id)` + reload list.
- `+ New` opens empty `NewSiteDialog`; on save, raises `SiteSelected` for the new site.

## DI registration

```csharp
services.AddTransient<StartPageViewModel>();
services.AddTransient<StartPage>();
```

## Checklist

- [ ] `StartPage.xaml` is full-window content (no menu/toolbar/statusbar)
- [ ] `StartPageViewModel` exposes `FilteredSites` ICollectionView with text filter
- [ ] Site cards display name, domain, page count, control count, relative last-opened
- [ ] Open / Edit / Delete commands wired with confirmation on delete
- [ ] `+ New` opens `NewSiteDialog` and selects newly created site
- [ ] Settings link raises `SettingsRequested`
- [ ] DI registrations added
