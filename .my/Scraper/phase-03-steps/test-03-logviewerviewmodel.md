# Test 3.3 — LogViewerViewModel Tests

**Covers:** Step 03 — `LogViewerViewModel` (`ICollectionView` filtering, auto-scroll, clear command)

**File:** `Brinell.Scraper.Tests/ViewModels/LogViewerViewModelTests.cs`

## Test Inventory (6 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `Constructor_DefaultsToDebugLevel` | `SelectedLogLevel == LogLevel.Debug` |
| 2 | `Constructor_AutoScrollIsTrue` | `IsAutoScroll == true` |
| 3 | `LogLevels_ContainsExpectedLevels` | Array contains Trace, Debug, Information, Warning, Error (5 entries) |
| 4 | `SelectedLogLevel_RaisesPropertyChanged` | Setting `SelectedLogLevel = LogLevel.Warning` fires `PropertyChanged` |
| 5 | `IsAutoScroll_RaisesPropertyChanged` | Setting `IsAutoScroll = false` fires `PropertyChanged` |
| 6 | `ClearLogsCommand_IsNotNull` | `ClearLogsCommand` is non-null after construction |

## Notes

- `LogViewerViewModel` constructor takes `InAppLogService` and calls `CollectionViewSource.GetDefaultView()` — this requires STA thread in tests because it touches WPF `CollectionViewSource`.
- Test class must be annotated with `[Collection("STA")]` or use `STAThreadAttribute` if the test runner supports it, or wrap construction in an STA thread.
- `FilteredLogEntries` is an `ICollectionView` — filtering behavior is WPF-internal, tested indirectly via the filter predicate logic.
- `ClearLogsCommand` delegates to `InAppLogService.Clear()` — verifying the command exists is sufficient at unit level.
- `RawEntries` returns `_logService.Entries` — trivial pass-through, no dedicated test needed.
