# Phase 4 — DOM Inspection, Recording & Corpus Management

## Goal

Capture DOM snapshots from WebView2 pages, store them in a per-site SQLite corpus, and provide tools for browsing, diffing, and managing the corpus. This phase is split into two sub-phases:

- **4A — DOM Inspection & Recording**: Capture DOM snapshots, element highlighting, tree view, multi-select, SPA handling, recording mode.
- **4B — Corpus Management**: Per-site SQLite storage, snapshot history, diffing, corpus browsing.

Recording captures DOM snapshots to the corpus — no code generation happens during recording (that's Phase 5).

---

## 4A — DOM Inspection & Recording

### 4.1 — Inject JS to Capture DOM Snapshot

Inject JavaScript into WebView2 to capture a full DOM snapshot on demand.

**Captured attributes per element:**

- `tag`, `id`, `class`, `name`, `type`
- `data-testid`, `data-automation-id`
- `role`, `aria-label`, `aria-labelledby`, `aria-describedby`
- `placeholder`, `value` (inputs)
- `href` (links), `src` (images)
- Visible text content
- Bounding box (`getBoundingClientRect()`) for visual overlay

**Implementation:**

```csharp
public sealed class DomCaptureService
{
    public async Task<DomSnapshot> CaptureAsync(CoreWebView2 webView)
    {
        var json = await webView.ExecuteScriptAsync(DomCaptureScript);
        return JsonSerializer.Deserialize<DomSnapshot>(json, _jsonOptions)!;
    }
}
```

```javascript
// DomCaptureScript — injected into WebView2
(function() {
    function captureElement(el) {
        const rect = el.getBoundingClientRect();
        return {
            tag: el.tagName.toLowerCase(),
            id: el.id || null,
            className: el.className || null,
            name: el.getAttribute('name'),
            type: el.getAttribute('type'),
            dataTestId: el.getAttribute('data-testid'),
            role: el.getAttribute('role'),
            ariaLabel: el.getAttribute('aria-label'),
            placeholder: el.getAttribute('placeholder'),
            textContent: el.childNodes.length === 1 && el.childNodes[0].nodeType === 3
                ? el.textContent.trim().substring(0, 200) : null,
            boundingBox: { x: rect.x, y: rect.y, width: rect.width, height: rect.height },
            children: Array.from(el.children).map(captureElement)
        };
    }
    return JSON.stringify(captureElement(document.documentElement));
})();
```

---

### 4.2 — Element Highlight Overlay

Highlight elements in the browser when the user hovers over them in inspect mode.

**Implementation:**

- Inject a `MutationObserver`-safe overlay `<div>` that follows the cursor.
- On `mousemove`, find the element under the cursor and position the overlay using `getBoundingClientRect()`.
- Show a tooltip below the element with: tag, id, aria-label, type, and **suggested locator** (e.g. `Locator.ByText("Email:")`).
- Colors:
  - Blue border + light blue bg = hovered
  - Green border + light green bg = selected (clicked / checked in tree)
- Toggle via the 🔍 Inspect button in the toolbar.

**Locator suggestion logic (in JS overlay):**

```javascript
function suggestLocator(el) {
    if (el.getAttribute('data-testid')) return `Locator.ByDataTestId("${el.getAttribute('data-testid')}")`;
    if (el.id && !isDynamicId(el.id)) return `Locator.ById("${el.id}")`;
    // Text-based: find adjacent label
    const label = findAssociatedLabel(el);
    if (label) return `Locator.ByText("${label.textContent.trim()}")`;
    if (el.getAttribute('aria-label')) return `Locator.ByAriaLabel("${el.getAttribute('aria-label')}")`;
    return `Locator.ByCss("${generateMinimalSelector(el)}")`;
}
```

Labels and visible text are preferred as locator anchors — this produces the most resilient locators.

---

### 4.3 — DOM Tree View Panel

Build a tree view in the sidebar from the captured DOM snapshot.

**Implementation:**

- WPF `TreeView` with `HierarchicalDataTemplate` bound to `DomElement.Children`.
- Each node shows: `<tag id="..." class="...">` with attribute details on expand.
- Hovering a tree node highlights the corresponding element in the browser.
- Clicking a tree node scrolls the browser to that element.
- Filter text box at top to search by tag, id, class, or text content.

---

### 4.4 — Multi-Select Mode

Let users pick multiple elements that should become control properties on the generated PageObject.

**Two selection modes:**

1. **TreeView checkboxes** — each `TreeViewItem` has a `CheckBox` in its template. Checked items are added to the selected set.
2. **Browser overlay Ctrl+click** — hold Ctrl and click elements in the browser to add/remove from selection. Selected elements get a persistent green border overlay.

**Selected elements collection:**

- `ObservableCollection<DomElement> SelectedElements` on the InspectorViewModel.
- Badge count shown in status bar: `4 selected │ DOM: 342 elements`.
- Buttons: Select All Forms, Select All Inputs, Clear Selection.

---

### 4.5 — Auto-Detect Control Groups

Automatically identify forms, tables, lists, and nav regions as candidate `ContainerBase<TParent, TScope>` groups.

**Heuristics:**

| Pattern | Detection Rule | Container Suggestion |
|---------|---------------|---------------------|
| `<form>` | Any `<form>` element | `FormContainer` with child inputs |
| `<table>` | Any `<table>` with `<thead>` and `<tbody>` | `TableContainer` with row/cell controls |
| `<ul>` / `<ol>` | List with 2+ `<li>` children | `ListContainer` |
| `<nav>` | Any `<nav>` element | `NavigationContainer` with link controls |
| Fieldset | `<fieldset>` with `<legend>` | Named container from legend text |
| Div with role | `<div role="dialog|form|tablist">` | Role-based container |

**Auto-suggestion UI:**

- After DOM capture, scan for these patterns and present a list: "Found 2 forms, 1 navigation, 1 table — include as containers?"
- User can accept/reject each suggestion.

---

### 4.6 — SPA-Aware Page Transition Detection

Modern SPAs (React, Angular, Vue, Blazor) don't trigger traditional navigation events. The tool must detect "virtual" page transitions.

**Implementation:**

- Inject a `MutationObserver` that watches for large DOM subtree changes:
  ```javascript
  const observer = new MutationObserver((mutations) => {
      const totalChanged = mutations.reduce((sum, m) => sum + m.addedNodes.length + m.removedNodes.length, 0);
      if (totalChanged > threshold) {
          // Signal potential page transition to WPF
          window.chrome.webview.postMessage({ type: 'pageTransition', url: location.href });
      }
  });
  observer.observe(document.body, { childList: true, subtree: true });
  ```
- **Threshold**: If >30% of visible elements changed → likely a "page transition".
- **URL change detection**: `hashchange` and `popstate` events + URL polling for pushState changes.
- **Wait for stable state**: After mutation detected, wait for:
  - No more mutations for 500ms
  - No pending XHR/fetch requests (intercept via `PerformanceObserver` or XHR monkey-patch)
  - No visible loading spinners (`[class*="loading"], [class*="spinner"]`)
- **User-triggered capture**: Manual "Capture This State" button for tricky SPAs where auto-detection fails.

---

### 4.7 — Recording Mode

Navigate through a site, capture each page automatically to the corpus. Recording only stores DOM snapshots — no LLM generation during recording.

**Implementation:**

```csharp
private bool _isRecording;
private readonly List<DomSnapshot> _sessionSnapshots = [];

private async void OnPageTransitionDetected(string url)
{
    if (!_isRecording) return;

    // Wait for page to stabilize
    await WaitForStableState();

    var snapshot = await _domCaptureService.CaptureAsync(_webView.CoreWebView2);
    snapshot.SiteName = _activeSite.Name;
    snapshot.PageName = InferPageName(url, snapshot.PageTitle);

    await _corpusService.StoreSnapshotAsync(_activeSite, snapshot);
    _sessionSnapshots.Add(snapshot);

    RecordingStatus = $"+{_sessionSnapshots.Count} new │ {_activeSite.TotalPages} total";
}
```

- **Start**: Set `_isRecording = true`, show red border, attach transition detector.
- **Pause**: Temporarily stop capturing without ending the session.
- **Stop**: End recording. Prompt: "Analyze corpus now?" → if yes, switch to Analysis view.
- Sidebar splits into "This Session" (new pages) and "Previous" (existing corpus pages).
- Re-recording a known page overwrites its snapshot (old version kept in history for diffing).
- Filter out duplicate transitions (same URL within 2-second window).

---

## 4B — Corpus Management

### 4.8 — SQLite Corpus Store

Store all DOM snapshots in a per-site SQLite database.

**Schema:**

```sql
CREATE TABLE Sites (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    StartUrl TEXT NOT NULL,
    Namespace TEXT NOT NULL,
    OutputPath TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    LastRecordedAt TEXT
);

CREATE TABLE SiteAliases (
    SiteId INTEGER NOT NULL REFERENCES Sites(Id),
    AliasUrl TEXT NOT NULL,
    PRIMARY KEY (SiteId, AliasUrl)
);

CREATE TABLE Snapshots (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SiteId INTEGER NOT NULL REFERENCES Sites(Id),
    PageName TEXT NOT NULL,
    PageUrl TEXT NOT NULL,
    PageTitle TEXT,
    CapturedAt TEXT NOT NULL,
    DomJson TEXT NOT NULL,  -- full DOM snapshot as JSON
    ElementCount INTEGER NOT NULL,
    SnapshotSizeBytes INTEGER NOT NULL,
    IsLatest INTEGER NOT NULL DEFAULT 1  -- 0 = historical version
);

CREATE TABLE Elements (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SnapshotId INTEGER NOT NULL REFERENCES Snapshots(Id),
    Tag TEXT NOT NULL,
    ElementId TEXT,        -- HTML id attribute
    ClassName TEXT,
    DataTestId TEXT,
    AriaLabel TEXT,
    Role TEXT,
    TextContent TEXT,
    ParentPath TEXT,       -- structural path for pattern matching
    AttributesJson TEXT    -- all attributes as JSON
);

CREATE INDEX IX_Elements_Tag ON Elements(Tag);
CREATE INDEX IX_Elements_DataTestId ON Elements(DataTestId);
CREATE INDEX IX_Snapshots_SiteId ON Snapshots(SiteId);
CREATE INDEX IX_Snapshots_PageName ON Snapshots(SiteId, PageName);
```

**Implementation:**

- NuGet: `Microsoft.Data.Sqlite`
- `CorpusService` — manages CRUD on the corpus:
  ```csharp
  public sealed class CorpusService : ICorpusService
  {
      Task<SiteCorpus> CreateSiteAsync(string name, string startUrl, string ns, string outputPath);
      Task<SiteCorpus?> GetSiteAsync(string name);
      Task<IReadOnlyList<SiteCorpus>> ListSitesAsync();
      Task StoreSnapshotAsync(SiteCorpus site, DomSnapshot snapshot);
      Task<DomSnapshot?> GetLatestSnapshotAsync(int siteId, string pageName);
      Task<IReadOnlyList<SnapshotSummary>> ListSnapshotsAsync(int siteId);
      Task<IReadOnlyList<DomElement>> SearchElementsAsync(int siteId, string query);
  }
  ```
- Database location: `%APPDATA%\Brinell.Scraper\corpus\{site-name}.db`
- When a page is re-recorded, mark the old snapshot as `IsLatest = 0` and insert the new one.
- Index individual elements in the `Elements` table for cross-page pattern queries in Phase 5.

---

### 4.9 — Corpus Browser View

UI for browsing all recorded snapshots with metadata, filtering, and diff capability.

**Implementation:**

- DataGrid showing: Page Name, URL, Recorded Date, Element Count, Generation Status.
- Sort by any column, filter by text search.
- Selecting a page shows: snapshot count (history), element count change, and action buttons.
- Actions: View Snapshot (loads in tree view), View Diff, Re-record, Delete Page, Regenerate Code.

---

### 4.10 — Snapshot Diff

Compare two snapshots of the same page to see what changed.

**Implementation:**

```csharp
public sealed class DomDiffService
{
    public DomDiffResult Compare(DomSnapshot before, DomSnapshot after)
    {
        // Match elements by id > data-testid > name > structural path
        // Categorize: Added, Removed, Changed, Unchanged
    }
}

public class DomDiffResult
{
    public List<DomElement> Added { get; init; } = [];
    public List<DomElement> Removed { get; init; } = [];
    public List<DomElementChange> Changed { get; init; } = [];
    public int UnchangedCount { get; init; }
}
```

- Inline diff view with color coding: green = added, red = removed, yellow = changed.
- Shows element-level changes with attribute-level detail.
- Accessible from Corpus Browser when a page has multiple snapshots.

---

### 4.11 — Export/Import DOM Snapshot

Serialize DOM snapshots for sharing.

**Implementation:**

```csharp
public sealed class DomSnapshot
{
    public string SiteName { get; set; } = "";
    public string PageName { get; set; } = "";
    public string PageUrl { get; init; } = "";
    public string PageTitle { get; init; } = "";
    public DateTimeOffset CapturedAt { get; init; }
    public DomElement RootElement { get; init; } = new();
    public List<DomElement> SelectedElements { get; init; } = [];
}
```

- Export: `System.Text.Json` with `WriteIndented = true`, `JsonNamingPolicy.CamelCase`.
- Default filename: `{site}-{page}-{timestamp}.json`.
- Import: Load JSON, store into corpus via `CorpusService.StoreSnapshotAsync()`.
- Also supports batch export/import of entire site corpus.

---

## UI Design — Inspector Mode (3-Panel Layout)

User clicks 🔍 (Inspect). Browser shrinks, DOM inspector + code preview panels appear.

```
┌──────────────────┬──────────────────┬──────────────┬─────────────────┐
│ 📁 Exact Online  │                  │ DOM Inspector│ Generated Code  │
│ ─────────────── │                  │              │                  │
│ Corpus: 47 pages │                  │ ▼ <html>     │ // TimePage.cs   │
│ Controls: 5      │                  │   ▼ <body>   │                  │
│ Generated: 42/47 │  WebView2        │     ▼ <form> │ namespace Exact..│
│                   │  Browser         │       ☑ inp  │                  │
│ ── Pages ──────  │                  │       ☑ sel  │ public sealed ..│
│ ✅ LoginPage     │  ┌──────────┐   │       ☐ div  │ {                │
│ ✅ Dashboard     │  │░Highlight░│   │       ☑ btn  │   public Text..│
│ ✅ TimeEntry ◄── │  │░ element ░│   │     ▶ footer │     Hours =>   │
│ ⏳ ProjectList   │  └──────────┘   │              │     new(this,..│
│                   │                  │ Select All   │ }                │
│ ── Controls ──── │                  │ Clear        │ ✅ 0 errors     │
│ ✅ DatePicker    │                  │ Save Snapshot│ [📋 Copy]       │
└──────────────────┴──────────────────┴──────────────┴─────────────────┘
```

### Panel Sizes (default)

| Panel | Width | Resizable |
|-------|-------|-----------|
| Sidebar | 180px | Yes (GridSplitter) |
| Browser | 40% | Yes (GridSplitter) |
| DOM Inspector | 20% | Yes (GridSplitter) |
| Code Preview | 20% | Yes (GridSplitter) |

### DOM Inspector Details

```
┌─ DOM Inspector ──────────────────┐
│ 🔎 Filter: [_______________]    │
│                                   │
│ ▼ <html>                         │
│   ▼ <body>                       │
│     ▼ <div class="app-root">    │
│       ▼ <form id="timeEntry">   │
│         ☑ <input id="hours">    │
│           ├ type: number         │
│           ├ name: hours          │
│           ├ aria-label: "Hours"  │
│           └ placeholder: 0.0    │
│         ☑ <select id="project"> │
│         ☐ <label>Description</> │
│         ☑ <textarea id="desc">  │
│         ☑ <button type="submit">│
│           └ text: "Save"         │
│       ▶ <div class="sidebar">   │
│     ▶ <footer>                   │
│                                   │
│ ── Actions ─────────────────────  │
│ [Select All Forms]               │
│ [Select All Inputs]              │
│ [Clear Selection]                │
│ [Save Snapshot to Corpus]        │
└───────────────────────────────────┘
```

- ☑ = selected for code generation, ☐ = not selected
- ▼/▶ = expanded / collapsed nodes
- Hovering a tree node highlights the element in the browser
- Clicking a tree node scrolls browser to that element

---

## UI Design — Recording Mode

User clicks ⏺ (Record). Red border + recording indicator. Recording captures DOM snapshots to corpus only — no LLM generation.

```
┌──────────────────┬───────────────────────────────────────────────────┐
│ 📁 Exact Online  │ ╔════════════════════════════════════════════════╗│
│ ─────────────── │ ║ 🔴 RECORDING — Adding to corpus               ║│
│ Corpus: 47 pages │ ║ Session: 3 new pages │ ⏱ 00:01:42             ║│
│ + 3 new this run │ ╠════════════════════════════════════════════════╣│
│                   │ ║                                                ║│
│ ── This Session ─│ ║               WebView2 Browser                 ║│
│ 🆕 SettingsPage  │ ║            (red border = recording)            ║│
│ 🆕 UserProfile   │ ║                                                ║│
│ 🆕 ReportPage    │ ╠════════════════════════════════════════════════╣│
│                   │ ║ Captured this session:                         ║│
│ ── Previous ──── │ ║  1. SettingsPage      ✅ captured              ║│
│ ✅ LoginPage     │ ║  2. UserProfilePage   ✅ captured              ║│
│ ...               │ ║  3. ReportPage        ⏳ awaiting navigate... ║│
│                   │ ╚════════════════════════════════════════════════╝│
├──────────────────┴───────────────────────────────────────────────────┤
│ 🔴 Recording │ +3 new │ 50 total │ https://start.exactonline.nl/..  │
└──────────────────────────────────────────────────────────────────────┘
```

- ⏺ becomes ⏹ (stop) + ⏸ (pause) during recording
- Red border around browser indicates active recording
- Sidebar separates "This Session" (new) from "Previous" (existing corpus)
- Each navigation auto-triggers DOM snapshot capture
- After stopping, user is prompted: "Analyze corpus now?"

---

## UI Design — Corpus Browser

Accessible via `Site → Browse Corpus`. Shows all recorded snapshots with metadata and diff capability.

```
┌──────────────────┬───────────────────────────────────────────────────┐
│ 📁 Exact Online  │ 📚 Corpus Browser                                 │
│ ─────────────── │ Sort: [▼ Last Recorded] Filter: [🔎          ]   │
│ Corpus: 50 pages │                                                   │
│                   │ ┌─────────────────────────────────────────────┐  │
│                   │ │ Page             │ URL       │ Rec'd  │ Gen │  │
│                   │ ├─────────────────────────────────────────────┤  │
│                   │ │ ReportPage       │ /reports  │ Apr 20 │ ✅  │  │
│                   │ │ TimeEntryPage    │ /time     │ Apr 15 │ ⚠️  │  │
│                   │ │   └ re-recorded Apr 20 (changed)           │  │
│                   │ │ LoginPage        │ /login    │ Apr 10 │ ✅  │  │
│                   │ └─────────────────────────────────────────────┘  │
│                   │                                                   │
│                   │ Selected: TimeEntryPage                           │
│                   │ Snapshots: 2 (Apr 15, Apr 20)                    │
│                   │ Elements: 342 → 358 (+16)                        │
│                   │                                                   │
│                   │ [View Snapshot] [View Diff] [Re-record]          │
│                   │ [Delete Page] [Regenerate Code]                   │
└──────────────────┴───────────────────────────────────────────────────┘
```

### Diff View

```
│ ── Diff: TimeEntryPage (Apr 15 → Apr 20) ─────────────────  │
│   <form id="timeEntry">                                      │
│     <input id="hours" type="number">            (unchanged)  │
│ +   <div class="date-picker">                   (added)      │
│ +     <input type="date" aria-label="Date">                  │
│ +   </div>                                                    │
│ -   <button type="submit">Save</button>         (removed)    │
│ +   <div class="btn-group">                     (added)      │
│ +     <button type="submit">Save</button>                    │
│ +     <button type="button">Save & New</button>             │
│ +   </div>                                                    │
```

---

## UI Design — Element Highlight Overlay

When inspector is active, hovering elements in the browser shows an overlay:

```
┌─ Browser ────────────────────────────────────┐
│   Email:                                      │
│   ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓   │ ← blue border = hovered
│   ┃ user@company.com                      ┃   │
│   ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛   │
│   ┌─────────────────────────────────────────┐ │
│   │ input#email  aria-label="Email address" │ │ ← tooltip
│   │ Suggested: Locator.ByText("Email:")     │ │ ← locator suggestion
│   └─────────────────────────────────────────┘ │
│   ╔══════════════════════════════════════╗     │
│   ║ ░░░ Sign In ░░░░░░░░░░ ║            │     │ ← green border = selected
│   ╚══════════════════════════════════════╝     │
└───────────────────────────────────────────────┘
```

| Color | Meaning |
|-------|---------|
| Blue border + light blue bg | Hovered (mouse over) |
| Green border + light green bg | Selected (clicked / checked in tree) |
| Tooltip below element | Shows tag, id, aria-label, type, locator suggestion |

---

## Acceptance Criteria

- [ ] DOM snapshot captures all specified attributes for every visible element
- [ ] Highlight overlay follows cursor and shows element info + locator suggestion
- [ ] Tree view displays full DOM hierarchy with parent-child relationships
- [ ] Multi-select works via TreeView checkboxes and Ctrl+click in browser
- [ ] Auto-detect identifies `<form>`, `<table>`, `<ul>`/`<ol>`, `<nav>` regions
- [ ] SPA page transitions are detected via MutationObserver and URL changes
- [ ] Recording mode captures snapshots on each page transition without LLM generation
- [ ] Recording can be started, paused, and stopped; sidebar shows session vs previous pages
- [ ] After stopping recording, user is prompted to analyze
- [ ] Snapshots are stored in per-site SQLite databases with element indexing
- [ ] Re-recording a page keeps old version in history and stores new as latest
- [ ] Corpus browser shows all pages with sort, filter, and status icons
- [ ] Diff view correctly identifies added, removed, and changed elements
- [ ] Snapshots can be exported as JSON and re-imported into the corpus
- [ ] DOM capture completes in < 2 seconds for pages with up to 5,000 elements

## Dependencies

| Dependency | Purpose |
|---|---|
| `Microsoft.Web.WebView2` NuGet | WebView2 browser control |
| `Microsoft.Data.Sqlite` NuGet | Per-site corpus SQLite storage |
| `System.Text.Json` | DOM snapshot serialization |
| Phase 1 | WebView2 browser shell, sidebar layout |
| Phase 1, step 1.2 | ViewModels, commands, DI container |
| Phase 3 | Logging for capture, corpus, and recording operations |

---

## Unit Test Plan

> Full test details in [unittest-roadmap.md](unittest-roadmap.md)

### Testable Components (~58 tests)

| Component | Tests | Strategy |
|-----------|-------|----------|
| `DomElement` / `DomSnapshot` models | 5 | Defaults, JSON round-trip, record equality |
| `DomCaptureService` | 6 | JSON deserialization, nested elements, missing attributes, timestamp |
| `ControlGroupDetector` | 8 | Form/table/list/nav/fieldset/role detection, edge cases |
| `DomDiffService` | 8 | Added/removed/changed detection, matching priority (id > data-testid > path) |
| `CorpusService` | 12 | Site CRUD, snapshot storage, re-recording history, element indexing, search |
| Export/Import | 6 | JSON serialization, camelCase, indentation, round-trip, invalid input |
| `InspectorViewModel` | 6 | Selection add/remove/clear, bulk select, count tracking |
| `RecordingViewModel` | 7 | Start/stop/pause, page capture, duplicate filtering, session tracking |

### Not Unit-Tested (UI/WebView2-dependent)

- Element highlight overlay — injected JavaScript in WebView2
- DOM tree view panel — WPF TreeView with HierarchicalDataTemplate
- Browser Ctrl+click selection — WebView2 JS interop
- SPA MutationObserver injection — JavaScript in WebView2
- Corpus browser DataGrid — WPF view

### Test Infrastructure

- **Database:** In-memory SQLite for `CorpusService` tests
- **Mocking:** `CoreWebView2` mocked via NSubstitute for `DomCaptureService`
- **Test data:** Fixture JSON files with sample DOM snapshots of varying complexity
