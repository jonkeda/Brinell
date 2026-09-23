# Test 1.5 — BrowserViewModel Tests

## Covers

Steps 03, 04 — `BrowserViewModel` (navigation events, address sync, history, commands)

## Tests (10)

| Test | Description |
|------|-------------|
| `OnNavigationStarting_SetsIsLoading` | IsLoading = true |
| `OnNavigationStarting_SetsStatusText` | Status shows URL |
| `OnNavigationCompleted_Success_ClearsLoading` | IsLoading = false |
| `OnNavigationCompleted_Failure_SetsErrorStatus` | StatusText shows error |
| `OnSourceChanged_UpdatesAddressUrl` | URL synced from browser |
| `OnHistoryChanged_UpdatesCanGoBack` | Back state updated |
| `OnHistoryChanged_UpdatesCanGoForward` | Forward state updated |
| `NavigateCommand_Disabled_WhenUrlEmpty` | CanExecute = false |
| `NavigateCommand_Enabled_WhenUrlSet` | CanExecute = true |
| `NavigateCommand_FiresNavigateRequested` | Event fires with URL |

## Implementation Notes

- `BrowserViewModel` has no external dependencies (logging added in Phase 3) — construct directly
- Call `OnNavigationStarting("https://example.com")` and verify property changes
- For command tests, set `AddressUrl` first, then check `NavigateCommand.CanExecute`
- Verify `OnSourceChanged` updates `AddressUrl` without re-raising `NavigateCommand.CanExecuteChanged`
