# Step 12.W.1d — Open Site from Corpus Page Selection

## Objective

When the user selects a recorded corpus page on the start page (e.g. from a "Recent Pages" or "Corpus" section of a site card), open the site workspace with the scraping tab active and the browser navigated to that page's URL.

## Dependencies

- `StartPageViewModel.SiteSelected` event (existing, triggers workspace open)
- `WorkspaceViewModel.LoadAsync(siteId)` (existing)
- `WorkspaceViewModel.OnOpenSourcePageRequested(url)` pattern (existing — sets `Scraping.Browser.AddressUrl`, fires `NavigateCommand`, switches to tab 0)
- `MainWindow.OnSiteSelected(SiteCardItem)` (existing transition handler)

## Current flow

1. User clicks site card → `OnOpenSite` fires `SiteSelected` with `SiteCardItem`
2. `MainWindow.OnSiteSelected` creates `WorkspaceViewModel`, calls `LoadAsync(card.Id)`
3. Workspace opens on default tab (Scraping), browser shows site's `StartUrl`

## New flow

1. User clicks a corpus page entry (URL) associated with a site
2. `StartPageViewModel` raises a new `SiteOpenWithUrlRequested` event with site ID + URL
3. `MainWindow` handles event: creates workspace, calls `LoadAsync(siteId, navigateUrl)`
4. After load, workspace sets `Scraping.Browser.AddressUrl = navigateUrl` and fires navigate
5. Scraping tab is active with the specific page loaded

## Implementation

### Files

| Action | Path |
|--------|------|
| Modify | `ViewModels/StartPageViewModel.cs` |
| Modify | `ViewModels/WorkspaceViewModel.cs` |
| Modify | `MainWindow.xaml.cs` |

### Code sketch

#### `ViewModels/StartPageViewModel.cs` — additions

```csharp
// New event — carries site ID and the URL to navigate to
public event Action<long, string>? SiteOpenWithUrlRequested;

// Called by UI when a corpus page is clicked
private void OnOpenCorpusPage(SiteCardItem card, string pageUrl)
{
    if (card is null || string.IsNullOrWhiteSpace(pageUrl)) return;
    SiteOpenWithUrlRequested?.Invoke(card.Id, pageUrl);
}
```

Optionally expose via command if the start page lists corpus pages:

```csharp
public ICommand OpenCorpusPageCommand { get; }

// In constructor:
OpenCorpusPageCommand = new RelayCommand<(SiteCardItem card, string url)>(
    t => OnOpenCorpusPage(t.card, t.url));
```

#### `ViewModels/WorkspaceViewModel.cs` — overload

Add an optional `navigateUrl` parameter to `LoadAsync`:

```csharp
public async Task LoadAsync(long siteId, string? navigateUrl = null)
{
    // ... existing load logic ...

    // After all child VMs are loaded:
    if (!string.IsNullOrWhiteSpace(navigateUrl))
    {
        Scraping.Browser.AddressUrl = navigateUrl;
        Scraping.Browser.NavigateCommand.Execute(null);
        SelectedTabIndex = 0;
    }
}
```

This reuses the exact same pattern as the existing `OnOpenSourcePageRequested`.

#### `MainWindow.xaml.cs` — subscribe and handle

```csharp
private void ShowStartPage()
{
    // ... existing ...
    var vm = _services.GetRequiredService<StartPageViewModel>();
    vm.SiteSelected += OnSiteSelected;
    vm.SiteOpenWithUrlRequested += OnSiteOpenWithUrl;  // new
    vm.SettingsRequested += OnSettingsRequested;
    _startVm = vm;
    // ...
}

private void OnSiteOpenWithUrl(long siteId, string url)
{
    DisposeStart();

    var vm = _services.GetRequiredService<WorkspaceViewModel>();
    _workspaceVm = vm;
    _workspaceBackHandler = ShowStartPage;
    vm.BackRequested += _workspaceBackHandler;

    _ = vm.LoadAsync(siteId, navigateUrl: url);

    RootContent.Content = new WorkspacePage { DataContext = vm };
}

private void DisposeStart()
{
    if (_startVm is not null)
    {
        _startVm.SiteSelected -= OnSiteSelected;
        _startVm.SiteOpenWithUrlRequested -= OnSiteOpenWithUrl;  // new
        _startVm.SettingsRequested -= OnSettingsRequested;
        _startVm = null;
    }
}
```

### UI trigger (start page)

The start page can surface corpus pages in different ways — the simplest is a list of recent pages per site card. The event is triggered when a page URL is clicked:

```xml
<!-- Inside site card DataTemplate -->
<ItemsControl ItemsSource="{Binding RecentPages}" Margin="8,4,0,0">
  <ItemsControl.ItemTemplate>
    <DataTemplate>
      <TextBlock>
        <Hyperlink Command="{Binding DataContext.OpenCorpusPageCommand, RelativeSource={RelativeSource AncestorType=ItemsControl}}"
                   CommandParameter="{Binding}">
          <Run Text="{Binding PageTitle, Mode=OneWay}" />
        </Hyperlink>
      </TextBlock>
    </DataTemplate>
  </ItemsControl.ItemTemplate>
</ItemsControl>
```

The exact UI design depends on the start page layout — the wiring pattern is what matters.

## Checklist

- [x] Add `SiteOpenWithUrlRequested` event to `StartPageViewModel`
- [x] Add `navigateUrl` optional parameter to `WorkspaceViewModel.LoadAsync`
- [x] Navigate browser to URL after workspace load when `navigateUrl` is set
- [x] Subscribe to new event in `MainWindow.xaml.cs`; create workspace with URL
- [x] Unsubscribe in `DisposeStart()`
- [ ] Ensure scraping tab (index 0) is selected when navigating
- [x] Verify browser loads the specific page URL, not just the site's StartUrl
