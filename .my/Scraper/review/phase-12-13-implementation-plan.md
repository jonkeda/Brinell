# Implementation Plan: Phase 12 + 13 Remaining Work

Status as of 2026-05-09.

## What is Already Done

### Phase 13 Backend — Fully Implemented

All Phase 13 services exist with real implementations (not stubs):

- 13.1 `ControlObjectAnalyzer` — two-phase local detection + LLM synthesis, persists to AnalysisResults
- 13.2 `ControlGenerationService` — `GenerateAllApprovedAsync`, `GenerateControlAsync`, one retry on validation errors
- 13.3 `SkillService` — `GenerateSiteControlsSkillAsync` writes `{slug}-controls/SKILL.md`  
  ⚠️ Minor gap: filters to all controls (no per-site filter) — TODO comment in code
- 13.4 `PageGenerationService` — file exists (control-aware with matcher integration — needs verification below)
- 13.5 `ControlObjectMatcher` — `MatchAll` with CSS signature scoring, threshold 0.75
- 13.6 `PipelineOrchestrator` — all stages: Analyze, GenerateControls, GeneratePageObjects, Output, full pipeline runner
- 13.7 DB persistence — `CorpusService` has `StoreAnalysisResult`, `GetCurrentAnalysisResult`, `UpdateProposalApproval`, `StorePageObject`, `GetPageObjects`, `GetPageObjectBySnapshot`, `DeletePageObject`
- 13.8 `CopilotService` + `CorpusTools` — session management, stub mode fallback, five corpus query tools
- 13.9 `LlmRetryHelper` — shared retry with validation callbacks
- 13.10 `CodeOutputService` — writes Controls/Pages/Containers project structure

### Phase 12 CorpusTabViewModel — Mostly Done

- Reconciliation: `LoadPagesWithReconciliationAsync`, `RefreshPageAsync`, `DeleteSnapshotAsync`, `DeletePageAsync`
- CRUD commands: `RefreshPageCommand`, `DeleteSnapshotCommand`, `DeletePageCommand`
- Source-of-truth alignment: `StartPageViewModel` already uses `GetDistinctPageCount` which reads from `Snapshots` table (correct source)

### Phase 12 Corpus Tests — Done

- `CorpusServiceCrudTests` exists
- `CorpusTabViewModelReconciliationTests` exists
- `CorpusTabViewModelCommandTests` exists

---

## What Still Needs to Be Implemented

### 1. ControlObjectsTabViewModel — Wire Phase 13 (12.W.1 → 12.W.4)

**File:** `ViewModels/Tabs/ControlObjectsTabViewModel.cs`

#### 1a. LoadControlObjects — join with AnalysisResults (12.W.1)

Currently only reads from `IControlRegistry`. Must also read current proposals from `CorpusService.GetCurrentAnalysisResult(siteId)` and merge by name.

Missing:
- Inject `CorpusService` into `ControlObjectsTabViewModel`
- Deserialize proposals from `AnalysisResult.ProposalsJson`
- Merge proposals with generated controls (name match, case-insensitive)
- Status logic: `Generated` if registry hit, else `Approved`/`Rejected`/`Pending` from proposal state
- Proposals with no registry match appear as Pending/Approved/Rejected
- Controls in registry with no proposal appear as Generated

#### 1b. Approve / Reject — persist to AnalysisResults (12.W.1)

Currently only sets in-memory status. Must also call `CorpusService.UpdateProposalApproval(siteId, name, status)`.

Missing:
- Call `UpdateProposalApproval` in both `Approve` and `Reject`

#### 1c. AnalyzeCorpusAsync — wire to orchestrator (12.W.2)

Currently a log-only stub. Must:
- Set `IsBusy = true`
- Call `_pipelineOrchestrator.AnalyzeForControlObjectsAsync(siteId, ct)`
- Reload list via `LoadControlObjects` on success
- Catch `OperationCanceledException`, general exception
- Reset `IsBusy` in finally

Missing:
- Add `IsBusy` and `StatusMessage` properties
- Implement the command body

#### 1d. GenerateAllPendingAsync — wire to orchestrator (12.W.3)

Currently a log-only stub. Must:
- Get approved proposals from current `ControlObjects` collection
- Call `_pipelineOrchestrator.GenerateControlObjectsAsync(siteId, approved, namespace, locatorReport, ct)`
- Reload list on success
- Handle `LlmAuthRequiredException` with user-facing message

Missing:
- Implement the command body (same `IsBusy`/`StatusMessage` pattern)
- Resolve `targetNamespace` (from settings or site config)
- Retrieve locator report from current AnalysisResult

#### 1e. RegenerateAsync — wire single-item regeneration (12.W.3)

Currently a log-only stub. Must:
- Delete existing generated control from registry (if present)
- Call `ControlGenerationService.GenerateControlAsync(proposal, namespace, ct)` directly (not via orchestrator batch)
- Update item status inline

Missing:
- Inject `ControlGenerationService` (or expose single-item regen via orchestrator)
- Implement the command body

#### 1f. Import / Export (12.W.4)

Both are log-only stubs. Must implement per spec:
- `Export`: `SaveFileDialog` → serialize proposals + generated controls as `ControlObjectsExportModel` JSON
- `Import`: `OpenFileDialog` → deserialize, validate version, call `CorpusService.StoreAnalysisResult`, reload list

Missing:
- Create `ControlObjectsExportModel` DTO
- Implement both methods

---

### 2. PageObjectsTabViewModel — Wire Phase 13 (12.W.1 → 12.W.3)

**File:** `ViewModels/Tabs/PageObjectsTabViewModel.cs`

#### 2a. LoadPageObjects — join with PageObjects table (12.W.1)

Currently marks every row `NotGenerated`. Must join with `CorpusService.GetPageObjects(siteId)` by `SnapshotId`.

Missing:
- Build `poLookup` from `GetPageObjects(siteId)`
- Set `Status`, `GeneratedAt`, `MainCode`, `UsedControlObjects` per matching record
- Populate `ControlObjectReferences` detail pane on selection

#### 2b. GenerateAllAsync — wire to orchestrator (12.W.2)

Currently a log-only stub. Must:
- Call `_pipelineOrchestrator.GeneratePageObjectsAsync(siteId, namespace, locatorReport, ct)`
- Update rows live via progress callback (match by SnapshotId)
- Reload list from DB on completion
- Handle `LlmAuthRequiredException`, `LlmRateLimitedException`, `OperationCanceledException`

Missing:
- Implement the command body with `IsBusy`/`StatusMessage`
- Progress callback to update rows in-place

#### 2c. RegenerateSelectedAsync — wire single-page (12.W.2)

Currently a log-only stub. Must:
- Call `PageGenerationService.GeneratePageAsync` for the selected snapshot
- Reload that single row from `GetPageObjectBySnapshot` after success

Missing:
- Inject `PageGenerationService` (or call via orchestrator single-page path)
- Implement the command body

#### 2d. Delete — persist to DB (12.W.3)

Currently only removes from the UI collection. Must also call `CorpusService.DeletePageObject(snapshotId)` and reset row status (not remove) per the spec — or confirm the remove-from-list behaviour is intentional.

Missing:
- Add confirmation dialog
- Call `CorpusService.DeletePageObject(item.SnapshotId)`
- Reset row status to `NotGenerated` instead of removing from list (keeps snapshot row visible)

#### 2e. Export — wire to CodeOutputService (12.W.3)

Currently a log-only stub. Must:
- Resolve output path from site config / AppSettings
- Call `_pipelineOrchestrator.OutputAsync(siteId, outputPath, namespace, ct)`
- Open output folder on success

Missing:
- Inject `AppSettings` (or get output path via a resolver)
- Implement the command body

---

### 3. CorpusTabViewModel — Remaining Stubs (12.W.4b / 12.W.4c)

**File:** `ViewModels/Tabs/CorpusTabViewModel.cs`

#### 3a. GeneratePageObjectAsync — wire to PageGenerationService (12.W.4b)

Currently a log-only stub. Must:
- Inject `PageGenerationService` (nullable, same pattern as orchestrator)
- Update version status: `NotGenerated` → `Generating` → `Generated` / `Error`
- Call `PageGenerationService.GeneratePageAsync` for the snapshot

Missing:
- Inject `PageGenerationService?` into constructor
- Expose `CanGeneratePageObject` property
- Implement the command body

#### 3b. Export — full corpus JSON (12.W.4c)

Currently logs "not yet implemented". Must:
- `SaveFileDialog` → serialize pages + snapshots as `CorpusExportModel` JSON
- Include HTML from `GetSnapshotById` for each version

Missing:
- Create `CorpusExportModel` DTO
- Implement `Export()` via `BuildExportModel`

#### 3c. ExportPage — single-page corpus JSON (12.W.4c)

Currently logs "not yet implemented". Same as above but scoped to `SelectedPage`.

Missing:
- Implement `ExportPage()` using same builder

#### 3d. Import — corpus JSON import with progress (12.W.4c)

Currently logs "not yet implemented". Must:
- `OpenFileDialog` → deserialize `CorpusExportModel`, validate version
- For each snapshot, call `CorpusService.AddSnapshot`
- Report `ImportProgress` (0–100) during import
- Reload page list after completion

Missing:
- Add `ImportProgress` property
- Implement `Import()` async

#### 3e. Corpus tab Load — join PageObject status (bonus, lower priority)

`Load` currently sets `HasPageObject = false` and `PageObjectStatus = NotGenerated` for all rows. After 2a is done, the same join logic should also be applied here so corpus page rows reflect PageObjects table state.

---

### 4. SkillService — Per-site Registry Filtering

**File:** `Services/SkillService.cs`

`GenerateSiteControlsSkillAsync` ignores `siteId` and reads all controls. Once `IControlRegistry` is extended to support per-site queries, remove the `_ = siteId` workaround and filter to site.

Missing:
- Add `GetControlsBySite(long siteId)` to `IControlRegistry` + `ControlRegistry`
- Update `SkillService` to use it

---

### 5. Tests — ControlObjectsTabViewModel and PageObjectsTabViewModel

**Folder:** `tests/Brinell.Scraper.Tests/ViewModels/Tabs/`

No tests exist for either ViewModel (only ScrapingTab tests are there). Both require:

- `ControlObjectsTabViewModelTests.cs`
  - `LoadControlObjects` merges proposals + generated correctly
  - `Approve`/`Reject` persist via UpdateProposalApproval
  - `AnalyzeCorpusAsync` calls orchestrator, reloads, handles errors
  - `GenerateAllPendingAsync` calls orchestrator with approved proposals only
  - `RegenerateAsync` deletes then regenerates
  - `Import`/`Export` round-trip

- `PageObjectsTabViewModelTests.cs`
  - `LoadPageObjects` joins with PageObjects table
  - `GenerateAllAsync` calls orchestrator + updates rows via progress
  - `RegenerateSelectedAsync` reloads single row
  - `Delete` persists to DB and resets status
  - `Export` calls OutputAsync

---

## Suggested Execution Order

These are grouped so each block compiles and is testable before moving to the next.

### Block A — ControlObjectsTabViewModel data layer (unblocks the UI flow)

1. Inject `CorpusService` into `ControlObjectsTabViewModel`
2. Implement `LoadControlObjects` with proposal merge (1a)
3. Implement `Approve`/`Reject` persistence (1b)

### Block B — ControlObjectsTabViewModel commands

4. Add `IsBusy` + `StatusMessage` properties (shared by all three commands)
5. Implement `AnalyzeCorpusAsync` (1c)
6. Implement `GenerateAllPendingAsync` (1d)
7. Implement `RegenerateAsync` — inject `ControlGenerationService` (1e)
8. Implement `Import` + `Export` + `ControlObjectsExportModel` (1f)

### Block C — PageObjectsTabViewModel data layer

9. Implement `LoadPageObjects` with PageObjects join (2a)
10. Fix `Delete` to persist to DB and reset row (2d)

### Block D — PageObjectsTabViewModel commands

11. Implement `GenerateAllAsync` with progress (2b)
12. Implement `RegenerateSelectedAsync` (2c) — inject `PageGenerationService`
13. Implement `Export` via orchestrator OutputAsync (2e)

### Block E — CorpusTabViewModel remaining stubs

14. Wire `GeneratePageObjectAsync` — inject `PageGenerationService?` (3a)
15. Implement corpus `Export` / `ExportPage` / `Import` + DTOs (3b/3c/3d)

### Block F — Cross-cutting

16. Fix `SkillService` per-site registry filtering (4)
17. Corpus tab Load: join PageObject status (3e)
18. Write `ControlObjectsTabViewModelTests` (5)
19. Write `PageObjectsTabViewModelTests` (5)

---

## Items Confirmed Already Correct (no action needed)

- `StartPageViewModel` uses `GetDistinctPageCount` which reads from `Snapshots` table — source-of-truth already aligned
- `CorpusTabViewModel` reconciliation (Load/Refresh/Delete) — fully implemented
- All Phase 13 backend services — real implementations, no stubs
- `CorpusService` DB accessors for AnalysisResults and PageObjects — all methods exist
- `CopilotService` stub-mode fallback — handles missing auth gracefully
