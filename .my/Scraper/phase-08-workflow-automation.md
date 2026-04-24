# Phase 8 — Workflow Automation

## Goal

Orchestrate the end-to-end pipeline (Record → Analyze → Approve Controls → Generate Controls → Generate Pages → Save to Project) with automation, guided workflows, and pipeline resumption.

## Tasks

### 8.1 — Pipeline Orchestrator

A `PipelineOrchestrator` service that manages the full workflow:

```csharp
public sealed class PipelineOrchestrator
{
    public async Task RunFullPipelineAsync(SiteCorpus site, PipelineOptions options)
    {
        // 1. Switch to Analysis view
        // 2. Run analyzer agent on corpus
        // 3. Present custom control proposals to user
        // 4. Wait for user approval
        // 5. Generate approved custom controls
        // 6. Generate pages (new/changed only, or all)
        // 7. Write to project
        // 8. Run dotnet build
        // 9. Report results
    }
}
```

Pipeline options: `GenerateAll` vs `GenerateNewOnly` vs `GenerateChanged`, `AutoApproveControls` (for re-runs), `SkipBuild`.

---

### 8.2 — Guided First-Run Wizard

When a user creates a new site corpus and stops their first recording, offer a guided wizard:

1. "You recorded 15 pages. Ready to analyze?"
2. Analysis runs → "3 custom controls proposed. Review?"
3. User approves → "Generate controls?"
4. Controls generated → "Generate page objects for all 15 pages?"
5. Pages generated → "Save to project at {path}?"
6. Build verification → "All 15 pages compile. Done!"

Each step has a "Skip" option and the wizard remembers where it left off.

---

### 8.3 — Pipeline Resume

If the app is closed mid-pipeline (e.g. after analysis but before generation), the pipeline state is persisted in the corpus SQLite and can be resumed:

- `PipelineState` table tracks: current step, pending approvals, generated controls, generated pages.
- On app restart with an active pipeline, show: "You left off at 'Generate Pages'. Resume?"

---

### 8.4 — Auto-Analyze After Recording

Option (default: on, configurable in settings): automatically trigger analysis after recording stops.

- Skip the "Analyze?" prompt and go straight to analysis.
- Show analysis progress in the sidebar.

---

### 8.5 — Incremental Re-Generation Workflow

When pages are re-recorded (site changed), the tool offers an incremental workflow:

1. Detect which pages changed (via diff from Phase 4.10).
2. Re-analyze only if new patterns are found.
3. Update custom controls if patterns changed.
4. Regenerate only changed pages.
5. Show diff of generated code changes before writing.

---

### 8.6 — Site-Specific Skills Auto-Update

After each analysis pass, automatically update the site's SKILL.md files:

- `{site}-patterns/SKILL.md` — updated with latest pattern analysis (locator strategies, element frequencies).
- `{site}-controls/SKILL.md` — updated with approved custom control definitions.

These skills feed into future generation passes for consistent output.

---

## UI Design — Generation View (Batch Page Generation)

After controls are approved and generated, user triggers page generation.

```
┌──────────────────┬───────────────────────────────────────────────────┐
│ 📁 Exact Online  │ 📝 Page Generation                                │
│ ─────────────── │                                                   │
│ Corpus: 50 pages │ Using 5 custom controls │ Generator model          │
│ Controls: 5 ✅   │                                                   │
│ Generated: 42/50 │ ── Progress ─────────────────────────────────── │
│                   │                                                   │
│ ── Pages ──────  │  ☑ LoginPage.cs          ✅ generated (no change) │
│ ✅ LoginPage     │  ☑ DashboardPage.cs      ✅ generated (no change) │
│ ✅ Dashboard     │  ☑ TimeEntryPage.cs      ✅ generated (updated)   │
│ ✅ TimeEntry     │  ☑ ProjectListPage.cs    ⏳ generating...         │
│ ⏳ ProjectList   │  ☑ InvoiceEditPage.cs    ⬚ queued                │
│ ⏳ InvoiceEdit   │  ☑ SettingsPage.cs       ⬚ queued                │
│ ⏳ SettingsPage  │  ☑ UserProfilePage.cs    ⬚ queued                │
│ ⏳ UserProfile   │  ☑ ReportPage.cs         ⬚ queued                │
│ ⏳ ReportPage    │                                                   │
│                   │ ── Generation Stats ──────────────────────────  │
│ ── Controls ──── │ Pages: 3/8 complete                               │
│ ✅ DatePicker    │ Tokens used: 12,400 / ~32,000 estimated           │
│ ✅ Autocomplete  │ Time: 8.2s elapsed                                │
│ ✅ DataGrid      │ Errors: 0                                         │
│ ✅ FileUpload    │                                                   │
│ ✅ RichText      │ [⏸ Pause] [⏹ Stop] [Skip Current]              │
│                   │                                                   │
│                   │ When complete:                                     │
│                   │ [💾 Save All to Project] [Review Individual]      │
└──────────────────┴───────────────────────────────────────────────────┘
```

### Generation Options

| Option | Description |
|--------|-------------|
| Generate all | Generate/regenerate all pages |
| Generate new only | Only pages without existing generated code |
| Generate changed | Only pages whose snapshots changed since last generation |
| Checkboxes | Select specific pages to generate |

## Acceptance Criteria

- [ ] Pipeline orchestrator runs the full workflow end-to-end.
- [ ] Guided wizard walks new users through their first pipeline.
- [ ] Pipeline state persists across app restarts.
- [ ] Auto-analyze fires after recording stops (when enabled).
- [ ] Incremental re-generation detects and processes only changed pages.
- [ ] Site-specific skills are auto-updated after analysis.

## Dependencies

- Phase 4 (Recording, Corpus, Diff)
- Phase 5 (Analysis, Generation, Copilot SDK)
- Phase 7 (Project Output)

---

## Unit Test Plan

### Testable Components (~20 tests)

| Component | Tests | Strategy |
|-----------|-------|---------|
| `PipelineOrchestrator` | 8 | Full pipeline steps execute in order, options respected (GenerateAll/NewOnly/Changed), auto-approve, skip-build, error handling at each step |
| Pipeline state persistence | 4 | State saved to SQLite, resume from correct step, state cleared on completion, handles missing state |
| Incremental re-generation | 4 | Changed pages detected via diff, unchanged pages skipped, new patterns trigger re-analysis, control updates cascade |
| Skills auto-update | 4 | SKILL.md written after analysis, content reflects latest patterns, controls SKILL.md includes approved controls, file overwrites previous version |

### Not Unit-Tested

- Guided first-run wizard — UI-driven workflow with dialogs
- Auto-analyze trigger — event-driven, verified by integration test
- Pipeline resume prompt — UI dialog

### Test Infrastructure

- **Mocking:** All phase services (corpus, analyzer, generator, output) mocked via NSubstitute
- **Database:** In-memory SQLite for pipeline state tests
