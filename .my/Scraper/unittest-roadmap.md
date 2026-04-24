# Unit Test Roadmap — Phases 1–4

## Overview

This roadmap defines the unit test strategy for the Brinell Scraper across Phases 1–4. Tests use **xUnit** + **NSubstitute** and live in a separate `Brinell.Scraper.Tests` project. UI-dependent code (WebView2, WPF controls) is not unit-tested — only ViewModels, services, models, and converters.

## Test Project Setup

```
tools/
  Brinell.Scraper/            # Main app
  Brinell.Scraper.Tests/      # Unit tests
    Brinell.Scraper.Tests.csproj
    ViewModels/
    Services/
    Models/
    Converters/
    Logging/
    Data/
    TestHelpers/
```

**Dependencies:**
- `xUnit` + `xunit.runner.visualstudio`
- `NSubstitute` (mocking)
- `FluentAssertions` (optional, for readable assertions)
- `Microsoft.Extensions.Logging.Abstractions` (for `NullLogger<T>`)
- Project reference to `Brinell.Scraper`

---

## Phase 1 — WPF Shell & MVVM Foundation

### ViewModelBase (9 tests)

| Test | Description |
|------|-------------|
| `SetProperty_RaisesPropertyChanged_WhenValueChanges` | Verify event fires with correct property name |
| `SetProperty_DoesNotRaise_WhenValueUnchanged` | Same value → no event |
| `SetProperty_ReturnsTrue_WhenChanged` | Return value indicates change |
| `SetProperty_ReturnsFalse_WhenUnchanged` | Return value indicates no change |
| `SetProperty_UpdatesBackingField` | Field holds new value after set |
| `SetProperty_HandlesNullToValue` | null → non-null transition |
| `SetProperty_HandlesValueToNull` | non-null → null transition |
| `SetProperty_HandlesReferenceTypes` | Object equality check |
| `SetProperty_HandlesValueTypes` | int, bool, enum equality |

### RelayCommand (8 tests)

| Test | Description |
|------|-------------|
| `Execute_CallsAction` | Action invoked on Execute |
| `CanExecute_ReturnsTrue_WhenNoPredicate` | Default canExecute = true |
| `CanExecute_ReturnsFalse_WhenPredicateFails` | Predicate returns false |
| `CanExecute_ReturnsTrue_WhenPredicatePasses` | Predicate returns true |
| `RaiseCanExecuteChanged_FiresEvent` | CanExecuteChanged event fires |
| `Execute_DoesNotThrow_WhenCanExecuteFalse` | Safe to call even when disabled |
| `RelayCommandT_PassesParameter` | Generic version passes typed parameter |
| `RelayCommandT_CanExecute_ReceivesParameter` | Predicate receives typed parameter |

### AsyncRelayCommand (7 tests)

| Test | Description |
|------|-------------|
| `Execute_CallsAsyncAction` | Async action invoked |
| `IsRunning_TrueWhileExecuting` | Property tracks execution state |
| `IsRunning_FalseAfterCompletion` | Resets after task completes |
| `CanExecute_ReturnsFalse_WhileRunning` | Prevents re-entry |
| `Execute_HandlesException` | Exception doesn't crash |
| `CancellationToken_Propagated` | Token passed to async action |
| `AsyncRelayCommandT_PassesParameter` | Generic version passes typed parameter |

### MainViewModel (12 tests)

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

### BrowserViewModel (10 tests)

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

### SiteSelectionViewModel (6 tests)

| Test | Description |
|------|-------------|
| `LoadSites_PopulatesSitesCollection` | Sites loaded from DB |
| `SelectSiteCommand_FiresSiteSelected` | Event fires with SiteInfo |
| `NewSiteCommand_FiresNewSiteRequested` | Event fires |
| `Sites_IsEmpty_WhenNoSitesInDb` | Empty collection |
| `SelectedSite_InitiallyNull` | No selection |
| `SiteSelected_IncludesCorrectSiteInfo` | Event arg matches selected |

### Converters (4 tests)

| Test | Description |
|------|-------------|
| `BoolToVisibility_True_ReturnsVisible` | true → Visible |
| `BoolToVisibility_False_ReturnsCollapsed` | false → Collapsed |
| `InverseBoolToVisibility_True_ReturnsCollapsed` | true → Collapsed |
| `InverseBoolToVisibility_False_ReturnsVisible` | false → Visible |

### CorpusDatabase (6 tests)

| Test | Description |
|------|-------------|
| `EnsureCreated_CreatesSitesTable` | Table exists after init |
| `CreateSite_InsertsAndReturnsId` | Site persisted |
| `GetAllSites_ReturnsAllSites` | All sites returned |
| `GetAllSites_ReturnsEmpty_WhenNoSites` | Empty list |
| `TouchSite_UpdatesLastOpenedAt` | Timestamp updated |
| `CreateSite_WithAliases_StoresCorrectly` | Pipe-delimited aliases |

**Phase 1 Total: ~62 tests**

---

## Phase 3 — Logging

### InAppLogService (5 tests)

| Test | Description |
|------|-------------|
| `Add_AppendsEntryToCollection` | Entry added |
| `Add_MultipleEntries_PreservesOrder` | FIFO order |
| `Clear_RemovesAllEntries` | Collection empty after clear |
| `Add_ThreadSafe_NoExceptions` | Concurrent adds don't throw |
| `Entries_IsObservable` | CollectionChanged fires |

### InAppLogProvider (4 tests)

| Test | Description |
|------|-------------|
| `CreateLogger_ReturnsNonNull` | Logger created |
| `CreateLogger_ExtractsShortName` | "Brinell.Scraper.ViewModels.MainViewModel" → "MainViewModel" |
| `Logger_IsEnabled_Debug` | Debug level enabled |
| `Logger_Log_AddsEntryToService` | Entry forwarded |

### LogViewerViewModel (6 tests)

| Test | Description |
|------|-------------|
| `FilteredLogEntries_ShowsAll_AtDebugLevel` | No filtering at lowest level |
| `FilteredLogEntries_HidesDebug_AtInfoLevel` | Debug entries hidden |
| `FilteredLogEntries_ShowsErrorOnly_AtErrorLevel` | Only errors shown |
| `SelectedLogLevel_Change_RefreshesFilter` | Filter updates |
| `ClearLogsCommand_ClearsEntries` | Collection cleared |
| `IsAutoScroll_DefaultTrue` | Auto-scroll on by default |

### LogLevelToBrushConverter (5 tests)

| Test | Description |
|------|-------------|
| `Debug_ReturnsGrayBrush` | Gray for Debug |
| `Information_ReturnsBlueBrush` | DarkBlue for Info |
| `Warning_ReturnsOrangeBrush` | DarkOrange for Warning |
| `Error_ReturnsRedBrush` | Red for Error |
| `InvalidValue_ReturnsDefaultBrush` | Fallback for unknown |

**Phase 3 Total: ~20 tests**

---

## Phase 4 — DOM Inspection, Recording & Corpus Management

### DomElement / DomSnapshot Models (5 tests)

| Test | Description |
|------|-------------|
| `DomElement_DefaultValues_AreNull` | Optional props null by default |
| `DomElement_Children_DefaultEmpty` | Empty list |
| `DomSnapshot_RoundTrip_Json` | Serialize → deserialize preserves all fields |
| `DomSnapshot_SelectedElements_Tracked` | Selected list persisted |
| `BoundingBox_RecordEquality` | Record equality works |

### DomCaptureService (6 tests — mock WebView2)

| Test | Description |
|------|-------------|
| `CaptureAsync_DeserializesJsonToSnapshot` | JSON → DomSnapshot |
| `CaptureAsync_SetsPageUrl` | URL populated |
| `CaptureAsync_SetsPageTitle` | Title populated |
| `CaptureAsync_HandlesNestedChildren` | Nested elements preserved |
| `CaptureAsync_HandlesMissingAttributes` | Null optional attributes |
| `CaptureAsync_SetsCapturedAt` | Timestamp set |

### ControlGroupDetector (8 tests)

| Test | Description |
|------|-------------|
| `Detect_FindsForms` | `<form>` → FormContainer |
| `Detect_FindsTables` | `<table>` with thead/tbody → TableContainer |
| `Detect_FindsLists` | `<ul>` with 2+ `<li>` → ListContainer |
| `Detect_FindsNavElements` | `<nav>` → NavigationContainer |
| `Detect_FindsFieldsets` | `<fieldset>` with `<legend>` → named container |
| `Detect_FindsRoleBasedDivs` | `<div role="dialog">` → RoleContainer |
| `Detect_IgnoresSingleItemList` | `<ul>` with 1 `<li>` → skipped |
| `Detect_ReturnsEmpty_ForPlainDiv` | No containers in plain DOM |

### DomDiffService (8 tests)

| Test | Description |
|------|-------------|
| `Compare_IdenticalSnapshots_NoChanges` | UnchangedCount = total |
| `Compare_AddedElement_InAddedList` | New element detected |
| `Compare_RemovedElement_InRemovedList` | Removed element detected |
| `Compare_ChangedAttribute_InChangedList` | Attribute change detected |
| `Compare_MatchesById_First` | id-based matching priority |
| `Compare_MatchesByDataTestId_Second` | data-testid fallback |
| `Compare_MatchesByStructuralPath_Last` | Structural path fallback |
| `Compare_MultipleChanges_AllCategorized` | Mixed add/remove/change |

### CorpusService (12 tests — in-memory SQLite)

| Test | Description |
|------|-------------|
| `CreateSite_PersistsAndReturns` | Site created |
| `GetSite_ReturnsCorrectSite` | Lookup by name |
| `GetSite_ReturnsNull_WhenNotFound` | Missing site → null |
| `ListSites_ReturnsAll` | All sites listed |
| `StoreSnapshot_PersistsSnapshot` | Snapshot saved |
| `StoreSnapshot_IndexesElements` | Elements table populated |
| `StoreSnapshot_ReRecord_MarksOldAsHistory` | IsLatest = 0 on old |
| `GetLatestSnapshot_ReturnsLatest` | IsLatest = 1 returned |
| `GetLatestSnapshot_SkipsHistorical` | Historical not returned |
| `ListSnapshots_ReturnsAllForSite` | All snapshots listed |
| `SearchElements_ByTag` | Tag-based search works |
| `SearchElements_ByDataTestId` | data-testid search works |

### Export/Import (6 tests)

| Test | Description |
|------|-------------|
| `Export_ProducesValidJson` | JSON output parseable |
| `Export_UsesCamelCase` | Property names camelCase |
| `Export_Indented` | Pretty-printed JSON |
| `Import_DeserializesCorrectly` | JSON → DomSnapshot |
| `Import_InvalidJson_ThrowsOrReturnsError` | Bad input handled |
| `RoundTrip_ExportImport_Preserves` | Export → import → equal |

### InspectorViewModel (6 tests)

| Test | Description |
|------|-------------|
| `SelectedElements_InitiallyEmpty` | No selection |
| `AddElement_IncreasesCount` | Count reflects additions |
| `RemoveElement_DecreasesCount` | Count reflects removals |
| `ClearSelection_EmptiesCollection` | All cleared |
| `SelectAllForms_SelectsInputElements` | Bulk select works |
| `SelectedCount_ReflectsCollectionSize` | Badge count accurate |

### RecordingViewModel (7 tests)

| Test | Description |
|------|-------------|
| `StartRecording_SetsIsRecordingTrue` | State tracked |
| `StopRecording_SetsIsRecordingFalse` | State cleared |
| `PauseRecording_PausesCapture` | Paused state |
| `OnPageTransition_CapturesSnapshot` | Snapshot added to session |
| `OnPageTransition_SkipsDuplicateWithin2Seconds` | Dedup works |
| `SessionSnapshots_TracksNewPages` | Count incremented |
| `StopRecording_PromptsAnalyze` | Event/callback fires |

**Phase 4 Total: ~58 tests**

---

## Summary

| Phase | Test Count | Key Areas |
|-------|-----------|-----------|
| Phase 1 — WPF Shell & MVVM | ~62 | ViewModelBase, commands, ViewModels, converters, DB |
| Phase 3 — Logging | ~20 | InAppLogService/Provider, LogViewerVM, converters |
| Phase 4 — DOM Inspection | ~58 | Models, capture, diff, corpus, control detection, recording |
| **Total** | **~140** | |

## Testing Principles

1. **No WPF/UI dependencies in tests** — test ViewModels and services only
2. **In-memory SQLite** for database tests (`Data Source=:memory:`)
3. **NSubstitute** for mocking `ILogger<T>`, `CoreWebView2`, and service interfaces
4. **One assert per test** where practical — multiple asserts only for related checks
5. **Test naming**: `Method_Scenario_ExpectedResult`
6. **No test interdependence** — each test creates its own state
7. **Structured logging verification** — use `NSubstitute.Received()` to verify log calls where important
