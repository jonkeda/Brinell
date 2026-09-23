# Phase 01 — Main UI Design

UI wireframes for the Phase 1 shell: start screen, main window, toolbar, sidebar, status bar, navigation flow, and visual tree.

---

## 1. Start Screen — Site Selector

First launch or when no site is active.

```
┌──────────────────────────────────────────────────────────────────────┐
│ 🔧 Brinell Scraper                                        _ □ ✕    │
├──────────────────────────────────────────────────────────────────────┤
│ File   View   Tools   Help                                          │
├──────────────────────────────────────────────────────────────────────┤
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

After selecting a site, the main workspace appears.

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

## 3. Navigation Flow Map

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

## 4. WPF Visual Tree

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
