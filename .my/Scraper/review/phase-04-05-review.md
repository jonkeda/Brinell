# Phase 4 & 5 — Implementation Review

**Reviewed:** 2026-05-04
**Reviewer:** Codebase audit against phase specs and UATs

---

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Fully implemented and functional |
| ⚠️ | Implemented but has known issues or gaps |
| 🔨 | Code exists but not wired up (dead code / broken DI / empty handler) |
| ❌ | Not implemented |

---

## Phase 4A — DOM Inspection & Recording

### 4.1 — DOM Snapshot Capture ✅

**Status:** Fully functional
**Files:** `Services/DomCaptureService.cs`, `Models/DomSnapshot.cs`, `Models/DomElement.cs`

- Captures: tag, id, className, name, type, data-testid, role, aria-label, placeholder, textContent, frameSource, boundingBox
- Same-origin iframe traversal via `el.contentDocument`
- Cross-origin iframe capture via `CoreWebView2Frame.ExecuteScriptAsync` (RCA-016 fix)
- Registered in DI, wired into inspect and recording flows

**UAT-4.1:** All items ✅

---

### 4.2 — Element Highlight Overlay ✅

**Status:** Fully functional
**Files:** `Services/ElementHighlightService.cs`

- Blue border + light blue bg overlay follows cursor
- Tooltip shows tag, id, aria-label, type, and suggested locator
- Locator suggestion logic: data-testid → id (non-dynamic) → label → aria-label → CSS selector
- Ctrl+click selection with green persistent border
- Toggle on/off via 🔍 Inspect button

**UAT-4.2:** All items ✅

---

### 4.2a — iFrame Overlay Support ⚠️

**Status:** Implemented, known issues remain
**Files:** `Services/ElementHighlightService.cs` (TrackFrames, IFrameOverlayScript)

- Overlay injected into iframes via `CoreWebView2Frame`
- `DOMContentLoaded` handler for proper timing (RCA-013 fix)
- `[iframe]` prefix in tooltip
- Page-level coordinate mapping for selections

**Known issues:**
- RCA-013: Overlay re-injection after navigation — **fixed**
- RCA-019: Record Page button dedup doesn't account for iframe content changes — **open**

**UAT-4.2a:** Not manually verified (all items unchecked in UAT doc)

---

### 4.2b — Locator Suggestion Priority ⚠️

**Status:** Logic implemented, not fully tested
**Files:** `Services/ElementHighlightService.cs` (suggestLocator, findAssociatedLabel)

- data-testid → id (dynamic filter) → label → aria-label → CSS selector: ✅
- `isDynamicId()` regex filter: ✅
- `findAssociatedLabel()` checks `for` attribute, parent `<label>`, and previous sibling: ✅
- `generateMinimalSelector()` for CSS fallback: ✅

**UAT-4.2b:** 1/5 items verified ✅, 4 unchecked

---

### 4.3 — DOM Tree View Panel ⚠️

**Status:** Mostly functional, gaps in interactivity
**Files:** `Views/DomTreePanel.xaml`, `ViewModels/DomTreeViewModel.cs`

- TreeView with `HierarchicalDataTemplate` bound to `DomElement.Children`: ✅
- Tag name with id/class color coding: ✅
- Expand/collapse nodes: ✅
- Filter text box with live search: ✅
- `ShowFilteredByTags()` for structured filtering (used by Select All commands): ✅

**Missing:**
- ❌ Hover over tree node → highlight corresponding element in browser (no callback wired)
- ❌ Click tree node → scroll browser to element

**UAT-4.3:** 2/4 items ✅, filter and browser sync unchecked

---

### 4.3a — Inspect Persistence Across Navigation ✅

**Status:** Fully functional
**Files:** `ViewModels/MainViewModel.cs` (OnNavigationSucceeded)

- `OnNavigationSucceeded` re-captures DOM and re-enables overlay with `force: true`
- DOM tree refreshes with new snapshot
- Inspect toggle button stays checked

**UAT-4.3a:** 3/3 items ✅ (verified via prior testing)

---

### 4.4 — Multi-Select Mode ⚠️

**Status:** Core selection works, bulk commands partially tested
**Files:** `ViewModels/InspectorViewModel.cs`

- Ctrl+click selection in browser: ✅
- Selection count in status bar: ✅
- `SelectAllFormsCommand` (input/select/textarea/button): ✅ code exists
- `SelectAllInputsCommand` (input only): ✅ code exists
- `ClearSelectionCommand`: ✅ code exists
- Selection syncs with tree filtering: ✅

**UAT-4.4:** 1/5 items ✅, bulk commands not manually verified

---

### 4.5 — Auto-Detect Control Groups 🔨

**Status:** Code exists, not visible in UI
**Files:** `Services/ControlGroupDetector.cs`

- Detects: form, table (thead+tbody), ul/ol (≥2 items), nav, fieldset (with legend), div with role
- Returns `ControlGroupSuggestion` with container type, name, element, children
- Registered in DI
- Used by `PageGenerationService.GeneratePageAsync()` as a parameter

**Missing:**
- ❌ No auto-suggestion UI after DOM capture ("Found 2 forms, 1 navigation...")
- ❌ No accept/reject workflow for detected groups
- The detector runs only when page generation is triggered, not during inspection

**UAT-4.5:** 0/3 items verified

---

### 4.6 — SPA Page Transition Detection 🔨

**Status:** Fully coded but dead code — never started
**Files:** `Services/PageTransitionDetector.cs`

- MutationObserver (≥10 changed nodes, 500ms settle): ✅
- hashchange, popstate listeners: ✅
- URL polling (300ms interval): ✅
- `PageTransitionDetected` event via `chrome.webview.postMessage`: ✅
- `StartAsync()` / `StopAsync()` methods: ✅
- Registered in DI: ✅

**Problem:** `MainViewModel` never calls `StartAsync()`. Recording relies solely on WebView2's `NavigationCompleted` event, which misses SPA client-side navigations.

**UAT-4.6:** 0/2 items verified

---

### 4.7 — Recording Mode ✅

**Status:** Fully functional (core flow)
**Files:** `ViewModels/RecordingViewModel.cs`, `ViewModels/MainViewModel.cs`

- Start/Stop/Pause/Resume: ✅
- Red border during recording: ✅
- Session sidebar with 🆕 icons: ✅
- Auto-capture on navigation: ✅
- IFrame navigation capture (RCA-014 fix): ✅
- 2-second dedup: ✅
- Manual capture via 📷 button (RCA-015): ✅
- Session pages transfer to corpus on stop: ✅

**Known issues:**
- RCA-020: Corpus page count not updated on manual capture — **open**
- RCA-021: Session lifecycle (cleared too early, no analyze button) — **open**

**UAT-4.7:** 11/15 items ✅

---

## Phase 4B — Corpus Management

### 4.8 — SQLite Corpus Store ⚠️

**Status:** Partial — Sites table works, Pages/Snapshots not wired
**Files:** `Data/CorpusDatabase.cs` (Sites), `Services/CorpusService.cs` (Snapshots + Elements)

**What works:**
- `CorpusDatabase`: Sites table with CRUD, registered in DI ✅
- `CorpusService`: Snapshots table (DomJson, metadata, IsLatest versioning), Elements table (indexed for search) ✅ code exists

**What's broken:**
- ❌ `CorpusService` NOT registered in DI (needs `connectionString` constructor param)
- ❌ No code calls `CorpusService.StoreSnapshotAsync()` during recording or manual capture
- ❌ No code calls `CorpusService.ListSnapshotsAsync()` on site selection to populate sidebar
- ❌ `Sites.PageCount` never updated in DB after captures
- RCA-022 covers this gap

**UAT-4.8:** 0/3 items verified

---

### 4.9 — Corpus Browser View 🔨

**Status:** UI exists, backend not wired
**Files:** `Views/CorpusBrowserView.xaml`, `ViewModels/CorpusBrowserViewModel.cs`

- DataGrid with Page/URL/Recorded/Elements/Size columns: ✅
- Text filter: ✅
- View Snapshot, View Diff, Re-record, Delete Page buttons: ✅ UI exists

**What's broken:**
- ❌ `BrowseCorpusCommand` in MainViewModel is `() => { }` (empty handler)
- ❌ `DeletePageCommand` handler is a placeholder
- ❌ `ViewDiffRequested`, `ViewSnapshotRequested`, `ReRecordRequested` events fired but never subscribed
- Depends on `CorpusService` which isn't in DI

**UAT-4.9:** 0/3 items verified

---

### 4.10 — Snapshot Diff 🔨

**Status:** Code exists, not accessible from UI
**Files:** `Services/DomDiffService.cs`, `Models/DomDiffResult.cs`

- Flattens both trees, matches by id → data-testid → name → structural path: ✅
- Reports Added/Removed/Changed elements with attribute-level detail: ✅
- Registered in DI: ✅

**What's broken:**
- ❌ No diff view in UI (no XAML)
- ❌ `CorpusBrowserViewModel.ViewDiffCommand` fires event but nothing handles it

**UAT-4.10:** 0/3 items verified

---

### 4.11 — Export/Import 🔨

**Status:** Code exists, no UI
**Files:** `Services/SnapshotExportService.cs`

- `Export()` serializes to indented camelCase JSON: ✅
- `Import()` deserializes back: ✅
- `GenerateFilename()` creates timestamped filenames: ✅
- Registered in DI: ✅

**What's broken:**
- ❌ No button/command/menu item triggers export or import

**UAT-4.11:** 0/4 items verified

---

## Phase 5 — LLM Code Generation

### 5.1 — Copilot SDK Integration 🔨

**Status:** Code exists, initialization never called
**Files:** `Services/CopilotService.cs`, `Services/ICopilotService.cs`

- `GitHub.Copilot.SDK` NuGet referenced: ✅
- `CopilotClient` with `UseLoggedInUser = true`: ✅
- `InitializeAsync()` creates client, starts, creates two sessions: ✅
- Registered in DI as `ICopilotService → CopilotService`: ✅

**What's broken:**
- ❌ `InitializeAsync()` never called in app startup or MainViewModel
- Will fail at runtime until called and until dependent services' DI is fixed

**UAT-5.0.1 / 5.0.2:** 0/7 items verified

---

### 5.2 — Custom Agents (analyzer, generator) ✅ (code)

**Files:** `Services/CopilotService.cs`

- `_analyzerSession`: gpt-4o-mini, analysis system prompt, corpus tools ✅
- `_generatorSession`: gpt-4o, generation system prompt, corpus tools ✅
- Both configured with system messages, tool arrays, `PermissionHandler.ApproveAll` ✅

**Blocked by:** CopilotService initialization (5.1)

---

### 5.3 — Custom Tools ✅ (code)

**Files:** `Services/CorpusTools.cs`

- `search_corpus(query, tag?, attribute?)`: ✅
- `get_page_snapshot(pageId)`: ✅
- `get_generated_controls()`: ✅
- `list_recorded_pages()`: ✅
- Full formatting logic for DOM elements: ✅

**Blocked by:** `CorpusService` not in DI (4.8)

---

### 5.4 — Skills System ✅ (code)

**Files:** `Services/SkillService.cs`

- `EnsureBrinellConventionsSkill()`: writes comprehensive SKILL.md ✅
- `GenerateSiteControlsSkill()`: generates per-site control skill ✅
- Called from `ControlGenerationService.GenerateAllApprovedAsync()` ✅

**Blocked by:** `SkillService` needs `skillsDirectory` param — DI will fail

---

### 5.5 — Analysis Pass 🔨

**Files:** `Services/AnalysisService.cs`, `Services/AnalysisResultParser.cs`, `ViewModels/AnalysisViewModel.cs`

- `AnalyzeCorpusAsync()`: builds prompt, sends to analyzer, parses JSON response ✅
- `AnalysisResultParser`: extracts JSON from fenced blocks or raw braces ✅
- `AnalysisViewModel`: approve/reject/approve-all commands ✅

**What's broken:**
- ❌ `AnalyzeCommand` in MainViewModel is `() => { }` (empty handler)
- ❌ No UI navigation to the Analysis View

**UAT-5.1.x:** 0/18 items verified

---

### 5.6 — Custom Control Generation 🔨

**Files:** `Services/ControlGenerationService.cs`

- Builds prompt from `ControlProposal`, sends to generator ✅
- Extracts C# code blocks, validates with Roslyn ✅
- Auto-retry on validation errors (max 2) ✅
- Stores in registry, generates site skills ✅

**Blocked by:** CopilotService init, ControlRegistry DI, SkillService DI

---

### 5.7 — Page Object Generation 🔨

**Files:** `Services/PageGenerationService.cs`, `Services/PromptBuilder.cs`

- Prompt includes className, namespace, pageUrl, elements, custom controls, locator report, container groups ✅
- Code block extraction, registry-aware Roslyn validation ✅
- Auto-retry on errors ✅
- Batch generation with progress tracking ✅
- `PromptBuilder` generates comprehensive prompts ✅

**Blocked by:** CopilotService init, CorpusService DI

---

### 5.8 — Code Validation (Roslyn) ✅ (code)

**Files:** `Services/CodeValidator.cs`, `Services/CodeBlockParser.cs`, `Services/RetryService.cs`

- `Microsoft.CodeAnalysis.CSharp` NuGet referenced ✅
- Syntax tree parsing, error detection ✅
- Locator method whitelist validation ✅
- ByCss usage warnings ✅
- `ValidateWithRegistry()` cross-references custom types ✅
- `CodeBlockParser` handles fenced and unfenced code ✅
- `RetryService` wraps validation + retry (max 2) ✅

---

### 5.9 — Control Registry ⚠️

**Files:** `Data/ControlRegistry.cs`, `Services/IControlRegistry.cs`

- SQLite `GeneratedControls` table with full schema ✅
- CRUD: `GetAllControls()`, `GetControl()`, `StoreControl()`, `DeleteControl()` ✅

**What's broken:**
- ❌ Registered in DI as `IControlRegistry → ControlRegistry` but needs `connectionString` — DI fails

---

## Summary

### Phase 4A — DOM Inspection & Recording

| Feature | Status |
|---------|--------|
| 4.1 DOM Snapshot Capture | ✅ Done |
| 4.2 Element Highlight Overlay | ✅ Done |
| 4.2a iFrame Overlay Support | ⚠️ Mostly done, RCA-019 open |
| 4.2b Locator Suggestion Priority | ⚠️ Code done, not fully tested |
| 4.3 DOM Tree View | ⚠️ Missing browser sync |
| 4.3a Inspect Persistence | ✅ Done |
| 4.4 Multi-Select Mode | ⚠️ Code done, bulk commands untested |
| 4.5 Auto-Detect Control Groups | 🔨 Code exists, no UI |
| 4.6 SPA Transition Detection | 🔨 Dead code, never started |
| 4.7 Recording Mode | ✅ Done (core), open RCAs |

### Phase 4B — Corpus Management

| Feature | Status |
|---------|--------|
| 4.8 SQLite Corpus Store | ⚠️ Sites works, Pages not wired |
| 4.9 Corpus Browser View | 🔨 UI exists, backend broken |
| 4.10 Snapshot Diff | 🔨 Code exists, no UI |
| 4.11 Export/Import | 🔨 Code exists, no UI |

### Phase 5 — LLM Code Generation

| Feature | Status |
|---------|--------|
| 5.1 Copilot SDK Integration | 🔨 Code exists, init never called |
| 5.2 Custom Agents | ✅ Code done |
| 5.3 Custom Tools | ✅ Code done (blocked by DI) |
| 5.4 Skills System | ✅ Code done (blocked by DI) |
| 5.5 Analysis Pass | 🔨 Code exists, no UI wiring |
| 5.6 Control Generation | 🔨 Code exists (blocked) |
| 5.7 Page Object Generation | 🔨 Code exists (blocked) |
| 5.8 Code Validation (Roslyn) | ✅ Code done |
| 5.9 Control Registry | ⚠️ Code done, DI broken |

---

## Critical Blockers

### 1. DI Registration Gaps

Three services have constructor parameters DI can't resolve:

| Service | Missing Param | Used By |
|---------|--------------|---------|
| `CorpusService` | `connectionString` | CorpusTools, AnalysisService, CorpusBrowser |
| `ControlRegistry` | `connectionString` | CodeValidator, ControlGeneration, CorpusTools |
| `SkillService` | `skillsDirectory` | ControlGenerationService |

**Fix:** Register with factory delegates in `App.xaml.cs`:
```csharp
services.AddSingleton<CorpusService>(sp => new CorpusService(connectionString));
services.AddSingleton<IControlRegistry>(sp => new ControlRegistry(connectionString));
services.AddSingleton<SkillService>(sp => new SkillService(skillsDirectory));
```

### 2. Empty Command Handlers

Three toolbar/menu commands do nothing:

| Command | Current | Should |
|---------|---------|--------|
| `ManageControlsCommand` | `() => { }` | Navigate to Controls Manager view |
| `BrowseCorpusCommand` | `() => { }` | Navigate to Corpus Browser view |
| `AnalyzeCommand` | `() => { }` | Run analysis pipeline |

### 3. CopilotService Never Initialized

`ICopilotService.InitializeAsync()` is never called. Must be called during app startup or on first use.

### 4. PageTransitionDetector Never Started

Fully implemented SPA detection is dead code. `MainViewModel` should call `StartAsync()` when recording starts and `StopAsync()` when recording stops.

---

## Open RCAs

| RCA | Summary | Severity |
|-----|---------|----------|
| RCA-019 | Record Page button doesn't detect iframe content changes | Medium |
| RCA-020 | Corpus page count not updated after manual capture | Low |
| RCA-021 | Session lifecycle — cleared too early, no analyze button | High |
| RCA-022 | SQLite corpus store — pages not persisted or retrieved | High |

---

## Recommended Next Steps (Priority Order)

1. **Fix DI registration** for CorpusService, ControlRegistry, SkillService (unblocks all of Phase 4B + Phase 5)
2. **Wire CorpusService into recording flow** — save pages on capture, load on site selection (RCA-022)
3. **Fix session lifecycle** — don't clear until analyzed, add analyze button (RCA-021)
4. **Wire empty command handlers** — ManageControls, BrowseCorpus, Analyze
5. **Wire PageTransitionDetector** into recording start/stop (SPA support)
6. **Call CopilotService.InitializeAsync()** on startup (unblocks Phase 5 runtime)
7. **Add DOM tree → browser sync** (hover/click tree node highlights element)
8. **Add auto-detect control groups UI** after DOM capture
