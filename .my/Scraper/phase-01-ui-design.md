# Phase 01 — UI Design

## Layout Philosophy

Single-window app with a **left sidebar** (site corpus navigation) and a **main content area** that changes based on workflow stage. The browser is the primary view. Side panels (inspector, code) and bottom panel (logs) slide in/out as needed. Toolbar at top, status bar at bottom.

The workflow is **iterative, corpus-based**: Record → Analyze → Approve Controls → Generate → Record More. The UI reflects this cyclical flow rather than a linear one.

---

## 1. Start Screen — Site Selector

First launch or when no site is active. Shows existing site corpuses and option to create new.

```
┌──────────────────────────────────────────────────────────────────────┐
│ 🔧 Brinell Scraper                                        _ □ ✕    │
├──────────────────────────────────────────────────────────────────────┤
│ File   View   Tools   Help                                          │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│                                                                      │
│                    Welcome to Brinell Scraper                        │
│                                                                      │
│            Select a site corpus or create a new one:                 │
│                                                                      │
│            ┌────────────────────────────────────────┐                │
│            │ 📁 Exact Online           47 pages    │                │
│            │    exactonline.nl, .be, .de            │                │
│            │    Last recorded: Apr 18, 2026         │                │
│            ├────────────────────────────────────────┤                │
│            │ 📁 Synergy HR              12 pages   │                │
│            │    synergy.mycompany.com               │                │
│            │    Last recorded: Apr 10, 2026         │                │
│            ├────────────────────────────────────────┤                │
│            │ 📁 Internal Portal          3 pages   │                │
│            │    portal.mycompany.com                │                │
│            │    Last recorded: Apr 5, 2026          │                │
│            ├────────────────────────────────────────┤                │
│            │ ＋ New Site Corpus...                   │                │
│            └────────────────────────────────────────┘                │
│                                                                      │
│                                                                      │
├──────────────────────────────────────────────────────────────────────┤
│ ● Ready │ No site selected                                          │
└──────────────────────────────────────────────────────────────────────┘
```

### New Site Dialog

```
┌─ New Site Corpus ────────────────────────────┐
│                                               │
│  Site name:       [Exact Online          ]   │
│  Start URL:       [https://start.exactonl]   │
│                                               │
│  URL aliases: (same app, different regions)   │
│  ┌──────────────────────────────────────┐    │
│  │ https://start.exactonline.be/        │    │
│  │ https://start.exactonline.de/        │    │
│  └──────────────────────────────────────┘    │
│  [+ Add alias]                                │
│                                               │
│  Namespace:       [ExactOnline.Pages     ]   │
│  Output path:     [E:\repos\Private\Hour ] 📂│
│                                               │
│                         [Cancel]  [Create]    │
└───────────────────────────────────────────────┘
```

---

## 2. Main Window — Browser with Sidebar

After selecting a site, the main workspace appears. Left sidebar shows corpus state, main area is browser.

```
┌──────────────────────────────────────────────────────────────────────┐
│ 🔧 Brinell Scraper — Exact Online                          _ □ ✕   │
├──────────────────────────────────────────────────────────────────────┤
│ File   Site   View   Tools   Help                                   │
├──────────────────────────────────────────────────────────────────────┤
│ ◀ ▶ ↻ │ https://start.exactonline.nl/            │ 🔍  ▶  ⏺  🔬 │
├──────────────────┬───────────────────────────────────────────────────┤
│ 📁 Exact Online  │                                                   │
│ ─────────────── │                                                   │
│ Corpus: 47 pages │                                                   │
│ Controls: 5      │                                                   │
│ Generated: 42/47 │               WebView2 Browser                    │
│                   │                                                   │
│ ── Pages ──────  │            (full width, full height)              │
│ ✅ LoginPage     │                                                   │
│ ✅ Dashboard     │                                                   │
│ ✅ TimeEntry     │                                                   │
│ ⏳ ProjectList   │                                                   │
│ ⏳ InvoiceEdit   │                                                   │
│ 🆕 (3 new)      │                                                   │
│                   │                                                   │
│ ── Controls ──── │                                                   │
│ ✅ DatePicker    │                                                   │
│ ✅ Autocomplete  │                                                   │
│ ✅ DataGrid      │                                                   │
│ ⏳ FileUpload    │                                                   │
│ ⏳ RichText      │                                                   │
├──────────────────┴───────────────────────────────────────────────────┤
│ ● Ready │ Exact Online │ 47 pages │ https://start.exactonline.nl/   │
└──────────────────────────────────────────────────────────────────────┘
```

### Toolbar Elements

| # | Element | Type | Action |
|---|---------|------|--------|
| 1 | Title bar | Window chrome | Shows active site name |
| 2 | Menu bar | Menu | File / Site / View / Tools / Help |
| 3 | Nav buttons | ToolBar | ◀ GoBack, ▶ GoForward, ↻ Refresh |
| 4 | Address bar | TextBox + Button | URL entry, ▶ = navigate |
| 5 | 🔍 Inspect | ToggleButton | Open inspector panels for current page |
| 6 | ⏺ Record | ToggleButton | Start/stop recording session |
| 7 | 🔬 Analyze | Button | Run LLM analysis on corpus |

### Sidebar Elements

| # | Element | Type | Binding |
|---|---------|------|---------|
| 1 | Site header | TextBlock | Active site name |
| 2 | Corpus stats | TextBlock | Page count, control count, generation progress |
| 3 | Pages list | ListView | All recorded pages with status icons |
| 4 | Controls list | ListView | All generated custom controls with status |

### Status Icons

| Icon | Meaning |
|------|---------|
| ✅ | Generated and up to date |
| ⏳ | Recorded but not yet generated |
| 🆕 | New since last analysis |
| ⚠️ | Changed since last recording (re-record recommended) |
| ❌ | Generation failed (Roslyn errors) |

---

## 3. Inspector Mode — 3-Panel Layout

User clicks 🔍 (Inspect). Browser shrinks, DOM inspector + code preview panels appear. Used for exploring a single page in detail.

```
┌──────────────────────────────────────────────────────────────────────┐
│ 🔧 Brinell Scraper — Exact Online                          _ □ ✕   │
├──────────────────────────────────────────────────────────────────────┤
│ File   Site   View   Tools   Help                                   │
├──────────────────────────────────────────────────────────────────────┤
│ ◀ ▶ ↻ │ https://start.exactonline.nl/app/#/time  │ 🔍̲  ▶  ⏺  🔬 │
├──────────────────┬──────────────────┬──────────────┬─────────────────┤
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
│ ⏳ InvoiceEdit   │                  │              │   ...            │
│ 🆕 (3 new)      │                  │ Select All   │ }                │
│                   │                  │ Clear        │                  │
│ ── Controls ──── │                  │ Save Snapshot│ ✅ 0 errors     │
│ ✅ DatePicker    │                  │              │                  │
│ ✅ Autocomplete  │                  │              │ [📋 Copy]       │
│                   │                  │              │ [💾 Save]       │
├──────────────────┴──────────────────┴──────────────┴─────────────────┤
│ ● Inspecting │ 4 selected │ DOM: 342 elements │ ⏱ 120ms             │
└──────────────────────────────────────────────────────────────────────┘
```

### Panel Sizes (default)

| Panel | Width | Resizable |
|-------|-------|-----------|
| Sidebar | 180px | Yes (GridSplitter) |
| Browser | 40% | Yes (GridSplitter) |
| DOM Inspector | 20% | Yes (GridSplitter) |
| Code Preview | 20% | Yes (GridSplitter) |

Clicking a page in the sidebar navigates the browser and shows that page's snapshot/generated code.

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
│           ├ name: projectCode    │
│           └ 12 <option> items    │
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

- ☑ = selected for code generation
- ☐ = not selected
- ▼/▶ = expanded / collapsed nodes
- Hovering a tree node highlights the element in the browser
- Clicking a tree node scrolls browser to that element

---

## 4. Code Preview Panel (AvalonEdit)

Uses **ICSharpCode.AvalonEdit** for C# syntax highlighting, line numbers, and code folding.

```
┌─ Generated Code ─────────────────┐
│ Class: ExactTimePage       [≡]   │
│ Namespace: ExactOnline.Pages     │
│ Using controls: DatePicker ✅    │
│ ─────────────────────────────── │
│  1 │ using Brinell.Core.Locators;│
│  2 │ using Brinell.Html.Abstract…│
│  3 │ using Brinell.Html.Controls;│
│  4 │ using ExactOnline.Controls; │
│  5 │                              │
│  6 │ namespace ExactOnline.Pages;│
│  7 │                              │
│  8 │▼public sealed class Exact…  │
│  9 │ {                            │
│ 10 │   public ExactTimePage(      │
│ 11 │     IHtmlTestContext context)│
│ 12 │     : base(context) { }     │
│ 13 │                              │
│ 14 │   public TextInputControl    │
│ 15 │     <ExactTimePage> Hours…   │
│ 16 │     => new(this,             │
│ 17 │        Locator.ById("hours")│
│ 18 │                              │
│ 19 │   public DatePickerControl   │
│ 20 │     <ExactTimePage> Date…    │
│ 21 │     => new(this,             │
│ 22 │        Locator.ByCss(".dp…"))│
│ 23 │ }                            │
│                                   │
│ ─────────────────────────────── │
│  ✅ Roslyn: No errors            │
│ ─────────────────────────────── │
│ [📋 Copy] [📂 Open in VS Code]  │
│ [💾 Save to Project]            │
│ [🔄 Regenerate]                  │
└───────────────────────────────────┘
```

- Monospace font, read-only, C# syntax highlighted (AvalonEdit)
- Line numbers in gutter
- Code folding (▼) for class bodies
- Header shows which custom controls are in use
- Roslyn status bar (✅ No errors / ❌ 2 errors)
- Buttons at bottom: Copy, Open in editor, Save, Regenerate

---

## 5. Recording Mode

User clicks ⏺ (Record) in toolbar. Red border + recording indicator appear. Recording only captures DOM snapshots to the corpus — no code generation happens during recording.

```
┌──────────────────────────────────────────────────────────────────────┐
│ 🔧 Brinell Scraper — Exact Online                          _ □ ✕   │
├──────────────────────────────────────────────────────────────────────┤
│ File   Site   View   Tools   Help                                   │
├──────────────────────────────────────────────────────────────────────┤
│ ◀ ▶ ↻ │ https://start.exactonline.nl/login       │ 🔍  ▶  ⏹ ⏸ 🔬│
├──────────────────┬───────────────────────────────────────────────────┤
│ 📁 Exact Online  │ ╔════════════════════════════════════════════════╗│
│ ─────────────── │ ║ 🔴 RECORDING — Adding to corpus               ║│
│ Corpus: 47 pages │ ║ Session: 3 new pages │ ⏱ 00:01:42             ║│
│ + 3 new this run │ ╠════════════════════════════════════════════════╣│
│                   │ ║                                                ║│
│ ── This Session ─│ ║                                                ║│
│ 🆕 SettingsPage  │ ║               WebView2 Browser                 ║│
│ 🆕 UserProfile   │ ║                                                ║│
│ 🆕 ReportPage    │ ║            (red border = recording)            ║│
│                   │ ║                                                ║│
│ ── Previous ──── │ ║                                                ║│
│ ✅ LoginPage     │ ║                                                ║│
│ ✅ Dashboard     │ ╠════════════════════════════════════════════════╣│
│ ✅ TimeEntry     │ ║ Captured this session:                         ║│
│ ⏳ ProjectList   │ ║  1. SettingsPage      ✅ captured              ║│
│ ...               │ ║  2. UserProfilePage   ✅ captured              ║│
│                   │ ║  3. ReportPage        ⏳ awaiting navigate... ║│
│                   │ ╚════════════════════════════════════════════════╝│
├──────────────────┴───────────────────────────────────────────────────┤
│ 🔴 Recording │ +3 new │ 50 total │ https://start.exactonline.nl/..  │
└──────────────────────────────────────────────────────────────────────┘
```

- ⏺ becomes ⏹ (stop) + ⏸ (pause) during recording
- Red border around browser indicates active recording
- Sidebar separates "This Session" (new) from "Previous" (existing corpus)
- Each navigation auto-triggers DOM snapshot capture (no LLM generation)
- Bottom strip shows pages captured in this session
- Re-recording a known page overwrites its snapshot (old version kept for diffing)
- After stopping, user is prompted: "Analyze corpus now?" → goes to Analysis view

---

## 6. Analysis View

User clicks 🔬 (Analyze) or answers "yes" after stopping recording. LLM analyzes the entire corpus for patterns and proposes custom controls.

```
┌──────────────────────────────────────────────────────────────────────┐
│ 🔧 Brinell Scraper — Exact Online                          _ □ ✕   │
├──────────────────────────────────────────────────────────────────────┤
│ File   Site   View   Tools   Help                                   │
├──────────────────────────────────────────────────────────────────────┤
│ ◀ ▶ ↻ │ https://start.exactonline.nl/            │ 🔍  ▶  ⏺  🔬̲ │
├──────────────────┬───────────────────────────────────────────────────┤
│ 📁 Exact Online  │ 🔬 Analysis Results                               │
│ ─────────────── │ ─────────────────────────────────────────────────  │
│ Corpus: 50 pages │                                                   │
│ Controls: 5      │ Analyzed 50 pages in 12.4s (analyzer model)       │
│ Generated: 42/50 │                                                   │
│                   │ ── Proposed Custom Controls ─────────────────── │
│ ── Pages ──────  │                                                   │
│ ✅ LoginPage     │ 1. 📦 DatePickerControl (NEW)                    │
│ ✅ Dashboard     │    Found on: 8 pages                              │
│ ✅ TimeEntry     │    Pattern: div.date-picker > input + button.cal  │
│ ⏳ ProjectList   │    Confidence: 94%                                │
│ ⏳ InvoiceEdit   │    [Preview Code] [✅ Approve] [❌ Reject]       │
│ ⏳ SettingsPage  │                                                   │
│ ⏳ UserProfile   │ 2. 📦 AutocompleteControl (NEW)                  │
│ ⏳ ReportPage    │    Found on: 12 pages                             │
│                   │    Pattern: div.autocomplete > input + ul.suggest │
│ ── Controls ──── │    Confidence: 89%                                │
│ ✅ DataGrid      │    [Preview Code] [✅ Approve] [❌ Reject]       │
│ ⏳ DatePicker    │                                                   │
│ ⏳ Autocomplete  │ 3. 📦 FileUploadControl (UPDATED)                │
│                   │    Found on: 3 pages                              │
│                   │    Pattern changed: now includes drag-drop area   │
│                   │    [Preview Code] [✅ Approve] [❌ Reject]       │
│                   │                                                   │
│                   │ ── Pattern Summary ──────────────────────────── │
│                   │ • 38 pages use standard Brinell controls only    │
│                   │ • 12 pages have custom widget patterns            │
│                   │ • 3 new patterns detected since last analysis     │
│                   │ • Locator strategy: aria-label (72%), id (18%),   │
│                   │   text (10%)                                      │
│                   │                                                   │
│                   │ [✅ Approve All] [Generate Controls] [Re-analyze] │
├──────────────────┴───────────────────────────────────────────────────┤
│ ● Analysis complete │ 3 new controls proposed │ 50 pages analyzed    │
└──────────────────────────────────────────────────────────────────────┘
```

### Analysis Actions

| Button | Action |
|--------|--------|
| Preview Code | Opens code preview panel with generated ControlObject class |
| ✅ Approve | Mark control for generation |
| ❌ Reject | Skip this control (use standard Brinell controls instead) |
| Approve All | Approve all proposed controls |
| Generate Controls | Generate approved custom control classes (prerequisite for page generation) |
| Re-analyze | Run analysis again (e.g. after recording more pages) |

---

## 7. Custom Controls Manager

After approving and generating controls, or via `Site → Manage Controls`. Shows all custom controls for the active site.

```
┌──────────────────────────────────────────────────────────────────────┐
│ 🔧 Brinell Scraper — Exact Online                          _ □ ✕   │
├──────────────────────────────────────────────────────────────────────┤
│ File   Site   View   Tools   Help                                   │
├──────────────────────────────────────────────────────────────────────┤
│ ◀ ▶ ↻ │ https://start.exactonline.nl/            │ 🔍  ▶  ⏺  🔬 │
├──────────────────┬──────────────────────────┬────────────────────────┤
│ 📁 Exact Online  │  Custom Controls          │  Code Preview          │
│ ─────────────── │  ─────────────────────── │                         │
│ Corpus: 50 pages │                           │  // DatePickerControl  │
│ Controls: 5      │  ✅ DatePickerControl     │                         │
│ Generated: 42/50 │     8 pages │ 94% conf   │  1 │ namespace Exact..  │
│                   │     Created: Apr 18       │  2 │                    │
│ ── Pages ──────  │     ◄ selected             │  3 │ public sealed ..  │
│ ✅ LoginPage     │                           │  4 │   DatePickerCon..  │
│ ✅ Dashboard     │  ✅ AutocompleteControl   │  5 │   : ContainerBa..  │
│ ✅ TimeEntry     │     12 pages │ 89% conf   │  6 │ {                  │
│ ⏳ ProjectList   │     Created: Apr 18       │  7 │   public Text..   │
│ ⏳ InvoiceEdit   │                           │  8 │     DateInput =>  │
│ ...               │  ✅ DataGridControl      │  9 │     new(this, ..  │
│                   │     15 pages │ 97% conf   │ 10 │                    │
│ ── Controls ──── │     Created: Apr 12       │ 11 │   public Button.. │
│ ✅ DatePicker ◄  │                           │ 12 │     CalendarBtn  │
│ ✅ Autocomplete  │  ⏳ FileUploadControl     │ 13 │     => new(this..  │
│ ✅ DataGrid      │     Approved, not yet gen │ 14 │ }                  │
│ ⏳ FileUpload    │                           │                         │
│ ⏳ RichText      │  ⏳ RichTextControl       │  ✅ Roslyn: No errors  │
│                   │     Approved, not yet gen │                         │
│                   │                           │  [📋 Copy] [✏️ Edit]  │
│                   │  [Generate Pending]       │  [💾 Save to Project] │
│                   │  [+ Manual Control]       │  [🔄 Regenerate]      │
├──────────────────┴──────────────────────────┴────────────────────────┤
│ ● Controls │ 3 generated │ 2 pending │ 50 pages use these controls   │
└──────────────────────────────────────────────────────────────────────┘
```

### Control Actions

| Button | Action |
|--------|--------|
| Generate Pending | Generate all approved but not yet generated controls |
| + Manual Control | Create a custom control manually (power user) |
| ✏️ Edit | Open control code in editable AvalonEdit mode |
| 🔄 Regenerate | Re-generate this control from latest corpus patterns |

---

## 8. Generation View — Batch Page Generation

After controls are approved and generated, user triggers page generation. Accessible via `Tools → Generate Pages` or after control generation completes.

```
┌──────────────────────────────────────────────────────────────────────┐
│ 🔧 Brinell Scraper — Exact Online                          _ □ ✕   │
├──────────────────────────────────────────────────────────────────────┤
│ File   Site   View   Tools   Help                                   │
├──────────────────────────────────────────────────────────────────────┤
│ ◀ ▶ ↻ │                                          │ 🔍  ▶  ⏺  🔬 │
├──────────────────┬───────────────────────────────────────────────────┤
│ 📁 Exact Online  │ 📝 Page Generation                                │
│ ─────────────── │ ──────────────────────────────────────────────── │
│ Corpus: 50 pages │                                                   │
│ Controls: 5 ✅   │ Using 5 custom controls │ Generator model          │
│ Generated: 42/50 │                                                   │
│                   │ ── Progress ─────────────────────────────────── │
│ ── Pages ──────  │                                                   │
│ ✅ LoginPage     │  ☑ LoginPage.cs          ✅ generated (no change) │
│ ✅ Dashboard     │  ☑ DashboardPage.cs      ✅ generated (no change) │
│ ✅ TimeEntry     │  ☑ TimeEntryPage.cs      ✅ generated (updated)   │
│ ⏳ ProjectList   │  ☑ ProjectListPage.cs    ⏳ generating...         │
│ ⏳ InvoiceEdit   │  ☑ InvoiceEditPage.cs    ⬚ queued                │
│ ⏳ SettingsPage  │  ☑ SettingsPage.cs       ⬚ queued                │
│ ⏳ UserProfile   │  ☑ UserProfilePage.cs    ⬚ queued                │
│ ⏳ ReportPage    │  ☑ ReportPage.cs         ⬚ queued                │
│                   │                                                   │
│ ── Controls ──── │ ── Generation Stats ──────────────────────────  │
│ ✅ DatePicker    │ Pages: 3/8 complete                               │
│ ✅ Autocomplete  │ Tokens used: 12,400 / ~32,000 estimated           │
│ ✅ DataGrid      │ Time: 8.2s elapsed                                │
│ ✅ FileUpload    │ Errors: 0                                         │
│ ✅ RichText      │                                                   │
│                   │ [⏸ Pause] [⏹ Stop] [Skip Current]              │
│                   │                                                   │
│                   │ When complete:                                     │
│                   │ [💾 Save All to Project] [Review Individual]      │
├──────────────────┴───────────────────────────────────────────────────┤
│ ● Generating │ 3/8 pages │ ProjectListPage.cs │ ⏱ 8.2s              │
└──────────────────────────────────────────────────────────────────────┘
```

### Generation Options

| Option | Description |
|--------|-------------|
| Generate all | Generate/regenerate all pages |
| Generate new only | Only pages without existing generated code |
| Generate changed | Only pages whose snapshots changed since last generation |
| Checkboxes | Select specific pages to generate |

---

## 9. Corpus Browser

Accessible via `Site → Browse Corpus` or clicking the corpus stats in the sidebar. Shows all recorded snapshots with metadata and diff capability.

```
┌──────────────────────────────────────────────────────────────────────┐
│ 🔧 Brinell Scraper — Exact Online                          _ □ ✕   │
├──────────────────────────────────────────────────────────────────────┤
│ File   Site   View   Tools   Help                                   │
├──────────────────────────────────────────────────────────────────────┤
│ ◀ ▶ ↻ │                                          │ 🔍  ▶  ⏺  🔬 │
├──────────────────┬───────────────────────────────────────────────────┤
│ 📁 Exact Online  │ 📚 Corpus Browser                                 │
│ ─────────────── │ ──────────────────────────────────────────────── │
│ Corpus: 50 pages │                                                   │
│ Controls: 5 ✅   │ Sort: [▼ Last Recorded] Filter: [🔎          ]   │
│ Generated: 50/50 │                                                   │
│                   │ ┌─────────────────────────────────────────────┐  │
│                   │ │ Page             │ URL       │ Rec'd  │ Gen │  │
│                   │ ├─────────────────────────────────────────────┤  │
│                   │ │ ReportPage       │ /reports  │ Apr 20 │ ✅  │  │
│                   │ │ UserProfilePage  │ /profile  │ Apr 20 │ ✅  │  │
│                   │ │ SettingsPage     │ /settings │ Apr 20 │ ✅  │  │
│                   │ │ InvoiceEditPage  │ /invoice  │ Apr 18 │ ✅  │  │
│                   │ │ ProjectListPage  │ /projects │ Apr 18 │ ✅  │  │
│                   │ │ TimeEntryPage    │ /time     │ Apr 15 │ ⚠️  │  │
│                   │ │   └ re-recorded Apr 20 (changed)           │  │
│                   │ │ DashboardPage    │ /dash     │ Apr 12 │ ✅  │  │
│                   │ │ LoginPage        │ /login    │ Apr 10 │ ✅  │  │
│                   │ │ ...              │           │        │     │  │
│                   │ └─────────────────────────────────────────────┘  │
│                   │                                                   │
│                   │ Selected: TimeEntryPage                           │
│                   │ Snapshots: 2 (Apr 15, Apr 20)                    │
│                   │ Elements: 342 → 358 (+16)                        │
│                   │                                                   │
│                   │ [View Snapshot] [View Diff] [Re-record]          │
│                   │ [Delete Page] [Regenerate Code]                   │
├──────────────────┴───────────────────────────────────────────────────┤
│ ● Corpus │ 50 pages │ 2 with changes │ Last recorded: Apr 20        │
└──────────────────────────────────────────────────────────────────────┘
```

### Diff View (in-line expansion)

When user clicks "View Diff" for a re-recorded page:

```
│ ── Diff: TimeEntryPage (Apr 15 → Apr 20) ─────────────────  │
│                                                               │
│   <form id="timeEntry">                                      │
│     <input id="hours" type="number">            (unchanged)  │
│     <select id="project">                       (unchanged)  │
│ +   <div class="date-picker">                   (added)      │
│ +     <input type="date" aria-label="Date">                  │
│ +     <button class="cal-btn">📅</button>                    │
│ +   </div>                                                    │
│     <textarea id="desc">                        (unchanged)  │
│ -   <button type="submit">Save</button>         (removed)    │
│ +   <div class="btn-group">                     (added)      │
│ +     <button type="submit">Save</button>                    │
│ +     <button type="button">Save & New</button>             │
│ +   </div>                                                    │
│   </form>                                                    │
```

---

## 10. Log Viewer (Bottom Panel — Collapsible)

Toggle via `View → Logs` or status bar click.

```
┌──────────────────────────────────────────────────────────────────────┐
│  ◀ ▶ ↻ │ https://start.exactonline.nl/app/#/time  │ 🔍  ▶  ⏺  🔬│
├──────────────────┬───────────────────────────────────────────────────┤
│ 📁 Exact Online  │                                                   │
│ ...               │    (any content panel, shorter height)            │
│                   │                                                   │
├──────────────────┴───────────────────────────────────────────────────┤
│ 📋 Logs                                          [▼ Level: All ▼] ▲ │
│ ───────────────────────────────────────────────────────────────────── │
│ 14:32:01  INFO   Browser        Navigated to /app/#/time             │
│ 14:32:02  INFO   Corpus         Snapshot captured: 342 elements      │
│ 14:32:02  DEBUG  Corpus         Capture took 120ms, 48KB JSON        │
│ 14:35:10  INFO   Analyzer       Analysis started: 50 pages           │
│ 14:35:18  INFO   Analyzer       3 custom control patterns detected   │
│ 14:36:01  INFO   Generator      Generating ProjectListPage.cs        │
│ 14:36:03  INFO   Generator      Response: 890 tokens, 1.8s           │
│ 14:36:03  INFO   Roslyn         Parse OK — 0 errors, 0 warnings      │
│ 14:36:03  INFO   Generator      Generated ProjectListPage.cs ✅      │
│                                                                       │
└──────────────────────────────────────────────────────────────────────┘
```

- DataGrid with columns: Time, Level, Source, Message
- Dropdown filter by level (All, Debug, Info, Warning, Error)
- Resizable via GridSplitter (drag top edge)
- Auto-scroll to latest, pause button

---

## 11. Settings Dialog

`Tools → Settings` or `Ctrl+,`

```
┌─ Settings ──────────────────────────────────────────────┐
│                                                          │
│  ── LLM — Analyzer ──────────────────────────────────   │
│  Model:        [▼ GPT-5.4                         ▼]    │
│  Temperature:  [====●==========] 0.3                     │
│  Purpose: Pattern detection, control proposals           │
│                                                          │
│  ── LLM — Generator ─────────────────────────────────   │
│  Model:        [▼ Claude Opus 4.6                 ▼]    │
│  Temperature:  [==●============] 0.2                     │
│  Purpose: Code generation (controls + pages)             │
│                                                          │
│  ── System Prompt (shared) ───────────────────────────   │
│  ┌──────────────────────────────────────────────────┐   │
│  │ You are a code generator for the Brinell UI      │   │
│  │ testing framework.                                │   │
│  │ Generate classes following these rules:            │   │
│  │ - Extend HtmlPageObjectBase<{ClassName}>          │   │
│  │ - Constructor takes IHtmlTestContext               │   │
│  │ - Use expression-bodied properties                │   │
│  │ - Use labels/text as primary locator hooks        │   │
│  │ ...                                               │   │
│  └──────────────────────────────────────────────────┘   │
│  [Reset to Default]                                      │
│                                                          │
│  ── Corpus ───────────────────────────────────────────   │
│  Storage path:     [%APPDATA%\Brinell.Scraper\c ] [📂] │
│  Auto-analyze after recording: [✓]                      │
│  Keep snapshot history:        [✓]                      │
│                                                          │
│  ── Output ───────────────────────────────────────────   │
│  Default namespace:     [ExactOnline.Pages       ]      │
│  (per-site, also configurable in site settings)          │
│                                                          │
│  ── Control Mappings (fallback) ──────────────────────   │
│  CSS Selector          →  Control Type                   │
│  input[type=text]      →  TextInputControl               │
│  button, [role=button] →  ButtonControl                  │
│  select                →  SelectControl                  │
│  [+ Add Mapping]                                         │
│                                                          │
│  ── Browser ──────────────────────────────────────────   │
│  Cookie persistence:   [✓] Keep sessions across runs    │
│  User data folder:     [%APPDATA%\Brinell.Scraper] [📂] │
│                                                          │
│  ── Logging ──────────────────────────────────────────   │
│  Log level:            [▼ Information             ▼]    │
│  Log folder:           [logs/                      ] [📂]│
│                                                          │
│                              [Cancel]  [Save]            │
└──────────────────────────────────────────────────────────┘
```

---

## 12. Element Highlight — Browser Overlay

When inspector is active, hovering elements in the browser shows an overlay:

```
┌─ Browser ────────────────────────────────────┐
│                                               │
│   Welcome to Exact Online                     │
│                                               │
│   Email:                                      │
│   ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓   │
│   ┃ user@company.com                      ┃   │ ← blue border = hovered
│   ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛   │
│   ┌─────────────────────────────────────────┐ │
│   │ input#email                             │ │ ← tooltip with selector
│   │ type=email  aria-label="Email address"  │ │
│   │ Suggested: Locator.ByText("Email:")     │ │ ← locator suggestion
│   └─────────────────────────────────────────┘ │
│                                               │
│   Password:                                   │
│   ┌──────────────────────────────────────┐    │
│   │ ••••••••                             │    │
│   └──────────────────────────────────────┘    │
│                                               │
│   ╔══════════════════════════╗                │
│   ║ ░░░ Sign In ░░░░░░░░░░ ║                │ ← green border = selected
│   ╚══════════════════════════╝                │
│                                               │
└───────────────────────────────────────────────┘
```

| Color | Meaning |
|-------|---------|
| Blue border + light blue bg | Hovered (mouse over) |
| Green border + light green bg | Selected (clicked / checked in tree) |
| Tooltip below element | Shows tag, id, aria-label, type, locator suggestion |

---

## 13. Navigation Map — View Flow

```
                    ┌─────────────┐
                    │  App Start  │
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │ Site Select │  pick or create site corpus
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │   Browser   │  main workspace (sidebar + browser)
                    │  + Sidebar  │
                    └──────┬──────┘
                           │
         ┌─────────────────┼─────────────────┐
         │                 │                 │
  ┌──────▼──────┐   ┌──────▼──────┐   ┌──────▼──────┐
  │  Inspect 🔍 │   │  Record ⏺  │   │  Corpus 📚  │
  │  (single    │   │  → capture  │   │  Browser    │
  │   page)     │   │  to corpus  │   │  (all pages)│
  └──────┬──────┘   └──────┬──────┘   └──────┬──────┘
         │                 │                 │
         │          ┌──────▼──────┐          │
         │          │ Stop ⏹     │          │
         │          │ "Analyze?" │          │
         │          └──────┬──────┘          │
         │                 │                 │
         │          ┌──────▼──────┐          │
         └─────────►│ Analyze 🔬 │◄─────────┘
                    │ (patterns + │
                    │  controls)  │
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │  Controls   │  approve / reject / edit
                    │  Review     │  custom controls
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │  Generate   │  uses approved controls
                    │  Controls   │
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │  Generate   │  batch page generation
                    │  Pages      │
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │  Save to    │  write to standalone project
                    │  Project    │
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐
                    │  Record     │  ← back to record more pages
                    │  More... ↺  │    site evolves, corpus grows
                    └─────────────┘
```

### Key Flow Rules

1. **Controls before pages** — custom controls must be approved and generated before page generation can use them
2. **Analysis is repeatable** — re-analyze after recording more pages
3. **Generation is incremental** — only generate new/changed pages
4. **Any step is optional** — can go from Record straight to manual Inspect
5. **Sidebar is always visible** — provides navigation to any stage at any time

---

## 14. WPF Visual Tree Summary

```
MainWindow
├── DockPanel
│   ├── Menu (Top)
│   ├── ToolBar (Top) — nav buttons, address bar, inspect/record/analyze
│   ├── StatusBar (Bottom)
│   └── Grid (Fill)
│       ├── Column 0: Sidebar (180px)
│       │   ├── Site header + stats
│       │   ├── Pages ListView
│       │   └── Controls ListView
│       ├── GridSplitter
│       ├── Column 1: ContentPresenter (main content)
│       │   ├── BrowserView (WebView2)
│       │   ├── InspectorView (3-panel: browser + DOM tree + code)
│       │   ├── RecordingView (browser + capture list)
│       │   ├── AnalysisView (pattern proposals)
│       │   ├── ControlsView (control list + code preview)
│       │   ├── GenerationView (batch progress)
│       │   └── CorpusView (snapshot table + diff)
│       ├── GridSplitter (horizontal)
│       └── Row 1: DataGrid (log viewer, collapsible)
│
├── StartWindow (shown if no site selected)
│   └── Site list + New Site button
│
└── Dialogs
    ├── NewSiteDialog
    ├── SettingsDialog
    └── SiteSettingsDialog
```

---

## Color / Theme Notes

- Follow system theme (light/dark) via `SystemColors`
- Monospace font for code: `Cascadia Code` → `Consolas` fallback
- Recording indicator: `#FF4444` red
- Selected element: `#4CAF50` green
- Hovered element: `#2196F3` blue
- Analysis / Analyze: `#9C27B0` purple
- Status indicators: ✅ green, ⏳ amber, 🆕 blue, ⚠️ orange, ❌ red
- Confidence bar: green (>80%), amber (60-80%), red (<60%)
