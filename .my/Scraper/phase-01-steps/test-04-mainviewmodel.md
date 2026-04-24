# Test 1.4 — MainViewModel Tests

## Covers

Steps 03, 06, 07 — `MainViewModel` (window title, site selection, command states, events)

## Tests (12)

| Test | Description |
|------|-------------|
| `Constructor_SetsDefaultWindowTitle` | "Brinell Scraper" |
| `Constructor_HasActiveSite_IsFalse` | No site initially |
| `OnSiteSelected_SetsActiveSite` | ActiveSite populated |
| `OnSiteSelected_UpdatesWindowTitle` | Title includes site name |
| `OnSiteSelected_UpdatesSiteName` | SiteName property set |
| `OnSiteSelected_UpdatesBrowserAddress` | Browser.AddressUrl = StartUrl |
| `OnSiteSelected_UpdatesCorpusStats` | Sidebar stats updated |
| `OnSiteSelected_FiresBrowserViewRequested` | Event fires |
| `HasActiveSite_TrueAfterSiteSelected` | Property reflects state |
| `SwitchSiteCommand_FiresSiteSelectorRequested` | Event fires |
| `ManageControlsCommand_DisabledWithoutSite` | CanExecute = false |
| `ManageControlsCommand_EnabledWithSite` | CanExecute = true after site select |

## Implementation Notes

- Construct with real `BrowserViewModel`, `SidebarViewModel`, mock `CorpusDatabase`
- Trigger `OnSiteSelected` by raising `SiteSelection.SiteSelected` event with a test `SiteInfo`
- Use `NullLogger<MainViewModel>` from `Microsoft.Extensions.Logging.Abstractions`
- Verify all command CanExecute states toggle correctly when `ActiveSite` changes
