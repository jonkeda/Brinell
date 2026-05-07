# Step 12.2 — Tabbed Workspace Shell

## Objective

When a site is opened from the Start Page, the window transitions to a tabbed workspace. A `TabControl` fills the content area with one tab per workflow area (Scraping, Control Objects, Page Objects, Corpus, Log, Settings).

## Dependencies

- Step 12.1 (Start Page raises `SiteSelected`)
- Existing per-area ViewModels (`BrowserViewModel`, `LogViewerViewModel`, etc.)

## Implementation

### Files

- `Views/WorkspacePage.xaml` (+ code-behind)
- `ViewModels/WorkspaceViewModel.cs`

### `WorkspaceViewModel`

```csharp
public class WorkspaceViewModel : ViewModelBase
{
    public SiteInfo ActiveSite { get; }

    // Composed tab VMs
    public ScrapingTabViewModel Scraping { get; }
    public ControlObjectsTabViewModel ControlObjects { get; }
    public PageObjectsTabViewModel PageObjects { get; }
    public CorpusTabViewModel Corpus { get; }
    public LogViewerViewModel Log { get; }
    public SettingsTabViewModel Settings { get; }

    public int SelectedTabIndex { get => _idx; set { ... } }

    public ICommand BackCommand { get; }    // → BackRequested
    public event Action? BackRequested;

    public Task LoadAsync();   // initializes child VMs for ActiveSite
}
```

### `WorkspacePage.xaml` layout

```
DockPanel
├─ Top: Header bar (DockPanel.Dock=Top, height ~40)
│   ├─ [🔙 Back] button → BackCommand
│   ├─ "Site:" label + ActiveSite.Name (bold)
│   └─ ActiveSite.StartUrl (muted, right-aligned)
└─ TabControl (fill)
    ├─ TabItem "Scraping"        → ScrapingTabView
    ├─ TabItem "Control Objects" → ControlObjectsTabView
    ├─ TabItem "Page Objects"    → PageObjectsTabView
    ├─ TabItem "Corpus"          → CorpusTabView
    ├─ TabItem "Log"             → LogTabView
    └─ TabItem "Settings"        → SettingsTabView
```

- `TabStripPlacement="Top"`, custom style for flat tabs with active underline.
- Each `TabItem.Content` is the corresponding `UserControl` with `DataContext` bound to its child VM.

### Shell navigation

In `MainWindow` / app shell:

```csharp
private void OnSiteSelected(SiteCardItem card)
{
    var workspace = _services.GetRequiredService<WorkspaceViewModel>();
    workspace.Initialize(card.Id);
    workspace.BackRequested += () => ShowStartPage();
    Content = new WorkspacePage { DataContext = workspace };
}
```

Back returns to `StartPage`, disposing the workspace VM.

## Checklist

- [ ] `WorkspaceViewModel` composes all six tab VMs
- [ ] `WorkspacePage.xaml` uses DockPanel with header bar + TabControl
- [ ] Header shows site name, URL, Back button
- [ ] No global menu/toolbar/statusbar at workspace level
- [ ] Back returns to Start Page and disposes workspace state
- [ ] DI registrations for `WorkspaceViewModel` and per-tab VMs
