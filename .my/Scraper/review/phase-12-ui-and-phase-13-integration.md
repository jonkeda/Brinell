# Phase 12 UI (02/03/04) + Phase 13 Integration

## Goal

Describe how the new Phase 12 UI wiring for:

- 02 Control Objects actions
- 03 Page Objects actions
- 04 Corpus actions

works together with the Phase 13 backend pipeline components.

This document is a runtime integration view (user action -> service orchestration -> persistence -> UI refresh).

## Components and Roles

### Phase 12 UI surfaces

- Control Objects tab (02):
  - Load proposals + approval state
  - Analyze corpus
  - Generate all approved controls
  - Regenerate one control
  - Import/export controls/proposals JSON
- Page Objects tab (03):
  - Load snapshots joined with page-object generation status
  - Generate all page objects
  - Regenerate selected page object
  - Delete single generated page object
  - Export generated code to output folder
- Corpus tab (04):
  - Generate page object for one snapshot version
  - Import/export corpus snapshots
  - CRUD and reconciliation for pages/snapshots visibility

### Phase 13 backend services

- ControlObjectAnalyzer (13.1)
- ControlGenerationService integration (13.2)
- SkillService auto-generation (13.3)
- PageGenerationService control-aware integration (13.4)
- ControlObjectMatcher (13.5)
- PipelineOrchestrator (13.6)
- Corpus persistence extensions (13.7)
- Copilot SDK + corpus tools (13.8)
- Retry/error policy (13.9)
- CodeOutputService (13.10)

## End-to-End Lifecycle

1. Corpus data is captured/imported and reconciled in Phase 12.04.
2. User runs Analyze from Phase 12.02.
3. Analyzer writes proposals + locator report to AnalysisResults (current run).
4. User approves/rejects proposals in Control Objects UI; decisions are persisted.
5. User runs Generate All Pending in 12.02.
6. Control generation persists generated controls and regenerates site controls SKILL.md.
7. User moves to Page Objects (12.03) and runs Generate All.
8. Page generation uses registered controls + matcher to emit typed page code; results are persisted in PageObjects.
9. User exports output from Page Objects tab; CodeOutputService writes deterministic project files.
10. Any single-row fix is done with Regenerate (control or page) and persisted back.

## UI Action -> Phase 13 Service Map

## 02 Control Objects Actions

### Load + approval persistence (12.W.1)

- UI load path:
  - reads generated controls from registry
  - reads current proposals from CorpusService.GetCurrentAnalysisResult
  - merges by control name (case-insensitive)
- Persistence:
  - Approve/Reject updates current AnalysisResults row (proposal status in ProposalsJson)
- Phase 13 dependency:
  - Requires 13.7 AnalysisResults accessors and current-row semantics

### Analyze corpus (12.W.2)

- UI command calls PipelineOrchestrator.AnalyzeForControlObjectsAsync
- Orchestrator delegates to ControlObjectAnalyzer (13.1)
- Analyzer pipeline:
  - local pattern detection
  - LLM analysis via CopilotService/tools (13.8)
  - parse structured proposals + locator report
  - store run in AnalysisResults (13.7)
- UI reloads control list from persisted current analysis

### Generate all pending + regenerate one (12.W.3)

- Generate all pending:
  - UI command calls PipelineOrchestrator.GenerateControlObjectsAsync
  - orchestrator calls ControlGenerationService.GenerateAllApprovedAsync (13.2)
  - each success stored in registry
  - then SkillService.GenerateSiteControlsSkillAsync (13.3)
- Regenerate one:
  - UI uses single-control generation path
  - existing generated control can be removed then regenerated
- Error policy:
  - auth/rate-limit/validation behavior follows 13.9

### Import/export controls (12.W.4)

- Export:
  - serializes current proposals + generated controls for portability
- Import:
  - restores proposals into current analysis result
  - generated controls are not auto-imported into registry by default (re-generation path stays authoritative)
- Phase 13 relationship:
  - imports feed the approval/generation path without needing a fresh analyze run

## 03 Page Objects Actions

### Load and status join (12.W.1)

- UI loads snapshot list and joins with PageObjects records by SnapshotId
- Status rendering source of truth:
  - PageObjects table (Generated/Error)
  - fallback NotGenerated if no row exists
- Phase 13 dependency:
  - Requires 13.7 PageObjects table + accessor methods

### Generate all + regenerate selected (12.W.2)

- Generate all:
  - UI calls PipelineOrchestrator.GeneratePageObjectsAsync
  - per snapshot, orchestrator calls PageGenerationService.GeneratePageAsync (13.4)
  - result persisted to PageObjects (13.7)
  - progress callback updates rows live
- Regenerate selected:
  - UI calls single-page generation
  - reloads that row from persisted record
- Matching/control-aware generation:
  - PageGenerationService pulls registry controls
  - ControlObjectMatcher (13.5) computes best matches
  - prompt includes available controls + suggested matches + locator guidance

### Delete + export (12.W.3)

- Delete:
  - removes one PageObjects row for snapshot
  - row resets to NotGenerated in UI
- Export:
  - calls CodeOutputService.WriteProjectAsync (13.10)
  - opens output folder
- Phase 13 dependency:
  - deterministic output from controls + pages persisted stores

## 04 Corpus Actions

### Generate page object for snapshot (12.W.4b)

- Corpus tab command invokes PageGenerationService.GeneratePageAsync for one snapshot
- Updates version-level status (Generating/Generated/Error)
- This is a narrow entry-point into same 13.4 generation engine

### Corpus import/export + CRUD visibility (12.W.4c/4d/4e)

- Import/export controls corpus snapshots data movement
- Reconciliation logic ensures DB state is reflected in UI collections
- Delete snapshot/page paths keep UI and DB synchronized
- Tests in 12.W.4e are the guardrails for these sync paths

### Store consolidation and source-of-truth alignment (12.W.4f/4g)

- Identified mismatch: start-page count from Pages vs Corpus tab from Snapshots
- Consolidation direction: snapshots-backed corpus as single source of truth
- Why it matters for Phase 13:
  - Analyzer/page generation depend on snapshot corpus
  - any source divergence causes pipeline input mismatch and confusing UI counts

## Data Contracts Across UI and Pipeline

### AnalysisResults (13.7)

- Stores:
  - proposals JSON (including approval state)
  - locator report JSON
  - metadata (snapshot count, local group count, analyzed timestamp)
  - IsCurrent pointer per site
- Used by:
  - Control Objects load/approve/reject/import flows

### Generated controls registry (13.2)

- Stores generated control code and metadata
- Used by:
  - Control Objects status merge
  - Skill generation (13.3)
  - Page generation type-resolution context (13.4)

### PageObjects (13.7)

- Stores per-snapshot page-object generation result:
  - main code
  - inline containers
  - used control references
  - validation/status
- Used by:
  - Page Objects tab status join, detail pane, delete
  - Code output export (13.10)

## State Transitions in UI Terms

Pipeline state machine from 13.6 mapped to tabs:

- Empty -> CorpusReady: Corpus tab import/capture/reconciliation complete
- CorpusReady -> ProposalsPending: Analyze completed, proposals loaded in Control Objects
- ProposalsPending -> ControlsGenerated: approved controls generated and stored
- ControlsGenerated -> PagesGenerated: page objects generated and stored
- PagesGenerated -> OutputComplete: code exported to output project

## Cross-Cutting Runtime Behavior

### Busy state and command guards

- 12.02 and 12.03 commands use IsBusy and CanExecute guards
- prevents overlapping analyze/generate/export operations

### Retry and resilience (13.9)

- one retry on validation/schema failures for LLM stages
- rate-limit backoff behavior
- auth-required surfaced to UI without hard crash
- partial progress preserved (failed proposals/pages do not abort peers)

### Progress and observability

- PipelineProgress drives per-stage feedback
- tab-level StatusMessage updates from orchestrator and command callbacks
- RunId/correlation should be included in logs for multi-stage runs

## Recommended Invocation Paths (Practical)

### Typical full run via tabs

1. Corpus tab: ensure snapshots are present and visible.
2. Control Objects: Analyze.
3. Control Objects: approve/reject.
4. Control Objects: Generate All Pending.
5. Page Objects: Generate All.
6. Page Objects: Export.

### Surgical fixes

- Bad control object: Control Objects -> Regenerate item
- Bad page object: Page Objects -> Regenerate selected row
- Remove stale generated page object: Page Objects -> Delete row

## Integration Risks to Watch

- Snapshot/source divergence (Pages table vs Snapshots table) causing empty pipeline input.
- Analysis current-pointer drift (wrong row marked IsCurrent).
- Registry-to-skill lag if skill regeneration is skipped after control changes.
- Type resolution failures in page generation if registry metadata is stale.
- Output path misconfiguration causing export success in memory but failure on disk.

## Minimal Acceptance Checks

- Analyze creates current AnalysisResults row and Control Objects list reload reflects it.
- Approve/reject survives app restart.
- Generate controls writes registry entries and regenerates site skill file.
- Generate pages updates row status live and persists PageObjects rows.
- Page Objects reload after restart shows Generated/Error statuses from DB.
- Export writes deterministic project with Controls and Pages folders.
- Start page/corpus counts are aligned after consolidation work.

## Summary

Phase 12 provides the user-driven tab actions and feedback loops. Phase 13 provides the orchestrated pipeline, model-aware generation, persistence contracts, and resilience policies. Together they form a single workflow where each UI action maps to a specific stage service, writes durable state, and refreshes the next UI stage from that persisted source of truth.
