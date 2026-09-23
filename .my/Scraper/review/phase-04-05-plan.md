# Phase 4 & 5 — Fix Plan

**Created:** 2026-05-04
**Based on:** [phase-04-05-review.md](phase-04-05-review.md)

---

## Overview

RCA-019 through RCA-022 are now implemented. This plan covers the remaining gaps identified in the review: broken DI, dead code, empty command handlers, and missing UI wiring.

Work is grouped into 8 steps. Steps 1–4 are critical (they unblock everything else). Steps 5–8 are enhancements.

---

## Step 1 — Fix DI Registration (unblocks Phase 4B + Phase 5)

**Priority:** Critical
**Files:** `App.xaml.cs`, `Data/CorpusDatabase.cs`

Three services can't be resolved because their constructors need primitive params DI can't supply.

### 1a. Register `CorpusService` (currently missing entirely)

```csharp
services.AddSingleton<CorpusService>(sp =>
{
    var dbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Brinell.Scraper", "scraper.db");
    var connectionString = $"Data Source={dbPath}";
    return new CorpusService(connectionString, sp.GetRequiredService<ILogger<CorpusService>>());
});
```

### 1b. Fix `IControlRegistry → ControlRegistry` registration

Replace `services.AddSingleton<IControlRegistry, ControlRegistry>()` with factory:

```csharp
services.AddSingleton<IControlRegistry>(sp =>
{
    var dbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Brinell.Scraper", "scraper.db");
    var connectionString = $"Data Source={dbPath}";
    return new ControlRegistry(connectionString, sp.GetRequiredService<ILogger<ControlRegistry>>());
});
```

### 1c. Fix `SkillService` registration

Replace `services.AddSingleton<SkillService>()` with factory:

```csharp
services.AddSingleton<SkillService>(sp =>
{
    var skillsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Brinell.Scraper", "skills");
    return new SkillService(skillsDir, sp.GetRequiredService<ILogger<SkillService>>());
});
```

### Verification

- App starts without DI resolution exceptions
- `CorpusTools`, `AnalysisService`, `ControlGenerationService`, `PageGenerationService` all resolve

---

## Step 2 — Initialize CopilotService on Startup

**Priority:** Critical (unblocks all Phase 5 runtime)
**Files:** `ViewModels/MainViewModel.cs` or `App.xaml.cs`

Call `ICopilotService.InitializeAsync()` once after startup. Two options:

**Option A — Lazy init on first use (preferred):**
Add to `MainViewModel` a helper that ensures init before any LLM call:

```csharp
private async Task EnsureCopilotInitializedAsync()
{
    if (!_copilotInitialized)
    {
        var copilot = App.Services.GetRequiredService<ICopilotService>();
        await copilot.InitializeAsync();
        _copilotInitialized = true;
    }
}
```

**Option B — Eager init at startup:**
After `services.BuildServiceProvider()`, call:

```csharp
_ = Task.Run(async () =>
{
    var copilot = Services.GetRequiredService<ICopilotService>();
    await copilot.InitializeAsync();
});
```

### Verification

- No `InvalidOperationException("Call InitializeAsync first")` when analyze is triggered

---

## Step 3 — Wire PageTransitionDetector into Recording

**Priority:** High (SPA navigation capture)
**Files:** `ViewModels/MainViewModel.cs`

### 3a. Inject `PageTransitionDetector` into MainViewModel constructor

### 3b. Subscribe to `PageTransitionDetected` event

When fired, capture a snapshot (same logic as `OnNavigationSucceeded` recording path).

### 3c. Start/stop with recording lifecycle

```
RecordingStarted → _pageTransition.StartAsync(webView)
RecordingStopped → _pageTransition.StopAsync(webView)
```

### Verification

- Navigate to a SPA app, start recording, click links that don't cause full page loads
- Session list captures the SPA transitions

---

## Step 4 — Wire Empty Command Handlers

**Priority:** High
**Files:** `ViewModels/MainViewModel.cs`, `MainWindow.xaml` (if new views need content presenter routing)

### 4a. `BrowseCorpusCommand`

- Set `ActiveView` to a `CorpusBrowserView` instance
- Call `CorpusBrowserViewModel.Load(corpusService)` to populate the DataGrid
- Subscribe to `ViewSnapshotRequested`, `ViewDiffRequested`, `ReRecordRequested` events

### 4b. `ManageControlsCommand`

- Set `ActiveView` to a `ControlsManagerView` instance
- Load controls from `IControlRegistry`

### 4c. `AnalyzeCommand`

- Call `EnsureCopilotInitializedAsync()` (Step 2)
- Call `AnalysisService.AnalyzeCorpusAsync()`
- Set `ActiveView` to `AnalysisView` with the results

### 4d. `DeletePageCommand` in CorpusBrowserViewModel

- Call `CorpusDatabase.DeletePage(pageId)`
- Call `CorpusDatabase.UpdateSitePageCount(siteId)`
- Refresh the page list

### Verification

- Menu → Browse Corpus opens the corpus browser with persisted pages
- Menu → Manage Controls shows the controls manager
- 🔬 Analyze triggers LLM analysis of corpus

---

## Step 5 — Add Export/Import UI

**Priority:** Medium
**Files:** `MainWindow.xaml` (menu), `ViewModels/MainViewModel.cs`

### 5a. Add menu items

Under `_Site` menu:
- `_Export Corpus...` → opens SaveFileDialog, calls `SnapshotExportService.Export()` for each page
- `_Import Snapshot...` → opens OpenFileDialog, calls `SnapshotExportService.Import()`, saves to DB

### 5b. Wire the calls

Export path:
1. Get all pages via `CorpusDatabase.GetPages(siteId)`
2. For each page, get snapshot JSON, deserialize, call `Export()`, write to file
3. Or export all as a ZIP/folder

Import path:
1. Read file, call `Import()`, get `DomSnapshot`
2. Call `CorpusDatabase.SavePage()` to persist
3. Refresh sidebar

### Verification

- Export a corpus page to JSON file, verify it's valid and human-readable
- Import the JSON file into a different site, verify it appears in corpus

---

## Step 6 — Add Diff View UI

**Priority:** Medium
**Files:** New `Views/DiffView.xaml`, `ViewModels/DiffViewModel.cs`

### 6a. Create DiffView

A simple two-column or list view showing:
- Added elements (green)
- Removed elements (red)
- Changed elements (yellow) with attribute-level diff detail
- Summary stats (unchanged count)

### 6b. Wire CorpusBrowserViewModel.ViewDiffRequested

When the event fires:
1. Get the two snapshots to compare (latest vs. selected, or two selected)
2. Call `DomDiffService.Compare()`
3. Show DiffView with the `DomDiffResult`

### Verification

- Record a page, modify the page, re-record. View diff shows changes.

---

## Step 7 — Add DOM Tree ↔ Browser Sync

**Priority:** Medium
**Files:** `Views/DomTreePanel.xaml`, `ViewModels/DomTreeViewModel.cs`, `Services/ElementHighlightService.cs`

### 7a. Hover over tree node → highlight in browser

- Add `MouseEnter`/`MouseLeave` handlers on tree items
- On hover, call a new `HighlightElementAsync(webView, element)` method in `ElementHighlightService` that positions the overlay at the element's bounding box

### 7b. Click tree node → scroll browser to element

- On click, inject JS: `document.querySelector(selector).scrollIntoView({behavior: 'smooth', block: 'center'})`

### Verification

- Hover over a `<button>` in the tree → browser highlights it
- Click an offscreen element in the tree → browser scrolls to it

---

## Step 8 — Add Auto-Detect Control Groups UI

**Priority:** Low
**Files:** `Views/ControlGroupPanel.xaml` (new), `ViewModels/InspectorViewModel.cs`

### 8a. Run detection after DOM capture

After `DomCaptureService.CaptureAsync()` in inspect mode, run `ControlGroupDetector.Detect()` and store results.

### 8b. Show suggestion bar

"Found 2 forms, 1 navigation, 1 table" with Accept/Reject per group.

### 8c. Accepted groups feed into page generation

Store accepted groups so `PageGenerationService` can use them as container hints.

### Verification

- Inspect a page with forms and tables → suggestion bar shows detected groups
- Accept a form group → it appears in generation context

---

## Dependency Graph

```
Step 1 (DI) ──┬──→ Step 2 (Copilot init) ──→ Step 4c (Analyze command)
              ├──→ Step 4a (Browse Corpus)
              ├──→ Step 4b (Manage Controls)
              ├──→ Step 5 (Export/Import)
              └──→ Step 6 (Diff View)

Step 3 (SPA detection) — independent

Step 7 (Tree sync) — independent
Step 8 (Control groups) — independent
```

Steps 1, 3, 7, 8 can be done in parallel.
Steps 2, 4 depend on Step 1.
Steps 5, 6 depend on Step 1 (for CorpusService).

---

## Estimated Scope

| Step | Files Changed | New Files | Complexity |
|------|--------------|-----------|------------|
| 1. Fix DI | 1 | 0 | Low |
| 2. Copilot init | 1–2 | 0 | Low |
| 3. SPA detection | 1 | 0 | Medium |
| 4. Command handlers | 2–3 | 0 | Medium |
| 5. Export/Import UI | 2 | 0 | Medium |
| 6. Diff View | 1–2 | 2 | Medium |
| 7. Tree sync | 2–3 | 0 | Medium |
| 8. Control groups UI | 1–2 | 1–2 | Medium |
