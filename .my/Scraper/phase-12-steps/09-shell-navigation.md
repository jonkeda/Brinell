# Step 12.9 — Shell Navigation & State Transitions

## Objective

Wire the application shell so that `MainWindow` swaps between **Start Page** and **Workspace Page** based on whether a site is active. Replace the old single-DockPanel layout.

## Dependencies

- Steps 12.1 – 12.8 (all pages and tabs exist)

## Implementation

### Files

- `MainWindow.xaml` (rewritten)
- `MainWindow.xaml.cs` (navigation logic)
- `ViewModels/ShellViewModel.cs` (optional thin host)

### `MainWindow.xaml`

```xml
<Window ...>
  <ContentControl x:Name="RootContent"/>
</Window>
```

No menu/toolbar/sidebar/statusbar at window level — those live inside specific tabs.

### Navigation logic

```csharp
public partial class MainWindow : Window
{
    private readonly IServiceProvider _services;

    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
        ShowStartPage();
    }

    private void ShowStartPage()
    {
        var vm = _services.GetRequiredService<StartPageViewModel>();
        vm.SiteSelected += OnSiteSelected;
        vm.SettingsRequested += OnSettingsRequested;
        _ = vm.LoadAsync();
        RootContent.Content = new StartPage { DataContext = vm };
    }

    private void OnSiteSelected(SiteCardItem card)
    {
        var vm = _services.GetRequiredService<WorkspaceViewModel>();
        vm.BackRequested += () => ShowStartPage();
        _ = vm.LoadAsync(card.Id);
        RootContent.Content = new WorkspacePage { DataContext = vm };
    }

    private void OnSettingsRequested()
    {
        // Show Workspace with no active site, focused on Settings tab
        var vm = _services.GetRequiredService<WorkspaceViewModel>();
        vm.LoadStandaloneSettings();
        vm.SelectedTabIndex = 5;     // Settings
        vm.BackRequested += () => ShowStartPage();
        RootContent.Content = new WorkspacePage { DataContext = vm };
    }
}
```

### Cross-tab navigation events

`WorkspaceViewModel` exposes handlers for events raised by tab VMs:

| Event source | Event | Handler |
|---|---|---|
| `CorpusTabViewModel.OpenInBrowser` | (PageUrl) | Set `SelectedTabIndex=0`, call `Scraping.Browser.NavigateTo(url)` |
| `PageObjectsTabViewModel.OpenSourcePage` | (PageUrl) | Same as above |
| `PageObjectsTabViewModel.NavigateToControlObject` | (controlName) | Set `SelectedTabIndex=1`, select control in `ControlObjects` |
| `ControlObjectsTabViewModel.NavigateToUsage` | (snapshotId) | Set `SelectedTabIndex=2`, select page in `PageObjects` |

### Window state cleanup

- When transitioning Start → Workspace, dispose previous `StartPageViewModel` (unsubscribe events).
- When transitioning Workspace → Start, dispose `WorkspaceViewModel` and child VMs (release WebView2, log subscriptions).

### Removal of legacy UI

- Delete or archive: old `MainWindow` DockPanel content, old sidebar `UserControl`s, embedded `LogViewerPanel` host.
- Existing `BrowserView`, `InspectorView`, `LogViewerPanel`, `DomTreePanel` continue to be used — only their hosts change.

## Checklist

- [ ] `MainWindow.xaml` reduced to a single `ContentControl`
- [ ] Start Page shown on launch
- [ ] Selecting a site swaps to Workspace Page
- [ ] Back from Workspace returns to Start Page
- [ ] Settings link from Start Page navigates to Settings tab in standalone mode
- [ ] Cross-tab events (open-in-browser, navigate-to-control, navigate-to-page) wired
- [ ] Old single-window layout removed
- [ ] WebView2 + log subscriptions disposed on workspace exit (no leaks)
