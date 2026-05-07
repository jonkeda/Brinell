# Phase 12 — UI Redesign: Start Page, Tabbed Workspace & Full-Page Views

## Goal

Replace the current single-window multi-pane layout with a two-screen architecture: a clean **Start Page** for site management, and a **Tabbed Workspace** that organizes scraping, log, control objects, page objects, corpus, and settings into dedicated tabs. Each tab gets a purpose-built full layout instead of sharing screen real-estate.

---

## Current State

The existing `MainWindow.xaml` packs everything into one DockPanel: menu, toolbar, sidebar, browser, inspector, log viewer, and status bar — all visible simultaneously. Views are swapped into a single `ContentArea` ContentControl. The sidebar mixes site info, session pages, corpus pages, and controls into one narrow 180px column. The log viewer is a collapsible bottom pane.

**Problems this phase solves:**

- Start page is embedded inside the scraping UI (site selection is just another view swap)
- Log viewer competes with browser height
- Controls manager and corpus browser are ephemeral view-swaps — no persistent workspace
- No dedicated page object management workspace
- No dedicated settings area
- Inspector, sidebar, and browser all fight for horizontal space

---

## 12.1 — Start Page (Full Screen)

A dedicated full-screen page shown on app launch and when no site is active. No toolbar, no log, no sidebar — just site management.

### Design

```
┌──────────────────────────────────────────────────────────────────┐
│                                                                  │
│                     🔍  Brinell Scraper                          │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐     │
│  │  🔎 Search sites...                                     │     │
│  └─────────────────────────────────────────────────────────┘     │
│                                                                  │
│  Recent Sites                                          [+ New]   │
│  ┌──────────────────────────────────────────────────────────┐    │
│  │ ┌────────────────┐ ┌────────────────┐ ┌────────────────┐ │    │
│  │ │  📄 Bouw7       │ │  📄 ExactOnline │ │  📄 Synergy    │ │    │
│  │ │  bouw7.nl      │ │  exact.com     │ │  synergy.nl   │ │    │
│  │ │  12 pages      │ │  8 pages       │ │  3 pages      │ │    │
│  │ │  5 controls    │ │  2 controls    │ │  0 controls   │ │    │
│  │ │  Last: 2 days  │ │  Last: 1 week  │ │  Last: today  │ │    │
│  │ │                │ │                │ │                │ │    │
│  │ │  [Open] [⚙][🗑]│ │  [Open] [⚙][🗑]│ │  [Open] [⚙][🗑]│ │    │
│  │ └────────────────┘ └────────────────┘ └────────────────┘ │    │
│  │                                                          │    │
│  │ ┌────────────────┐                                       │    │
│  │ │  📄 MyApp       │                                       │    │
│  │ │  myapp.dev     │                                       │    │
│  │ │  0 pages       │                                       │    │
│  │ │  0 controls    │                                       │    │
│  │ │  Last: never   │                                       │    │
│  │ │                │                                       │    │
│  │ │  [Open] [⚙][🗑]│                                       │    │
│  │ └────────────────┘                                       │    │
│  └──────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ─────────────────────────────────────────────────────────────   │
│  ⚙ Settings                                       v1.0.0-beta   │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Elements

| Element | Type | Binding / Behavior |
|---------|------|--------------------|
| App title | TextBlock | Static "Brinell Scraper" with app icon |
| Search box | TextBox | Filters `Sites` collection in real-time |
| Site cards | ItemsControl + WrapPanel | Bound to `FilteredSites` ICollectionView |
| Card: site name | TextBlock | `SiteInfo.Name` |
| Card: URL | TextBlock | `SiteInfo.StartUrl` (truncated to domain) |
| Card: page count | TextBlock | `SiteInfo.PageCount` + " pages" |
| Card: control count | TextBlock | `SiteInfo.ControlCount` + " controls" |
| Card: last opened | TextBlock | `SiteInfo.LastOpenedAt` relative ("2 days ago") |
| Open button | Button | Navigates to Tabbed Workspace for this site |
| Settings button (⚙) | Button | Opens site-specific edit dialog (existing `NewSiteDialog`) |
| Delete button (🗑) | Button | Confirmation → deletes site + all corpus data |
| "+ New" button | Button | Opens `NewSiteDialog` → on save, opens workspace |
| Settings link | Button/Hyperlink | Scrolls to or opens Settings tab |
| Version | TextBlock | Assembly version |

### ViewModel: `StartPageViewModel`

```csharp
public class StartPageViewModel : ViewModelBase
{
    // Collections
    public ObservableCollection<SiteInfo> Sites { get; }
    public ICollectionView FilteredSites { get; }

    // Properties
    public string SearchText { get; set; }       // filters FilteredSites
    public string AppVersion { get; }

    // Commands
    public ICommand NewSiteCommand { get; }      // opens NewSiteDialog
    public ICommand OpenSiteCommand { get; }     // param: SiteInfo → fires SiteOpened
    public ICommand EditSiteCommand { get; }     // param: SiteInfo → opens edit dialog
    public ICommand DeleteSiteCommand { get; }   // param: SiteInfo → confirm + delete
    public ICommand OpenSettingsCommand { get; } // fires SettingsRequested

    // Events
    public event Action<SiteInfo>? SiteOpened;
    public event Action? SettingsRequested;
}
```

### View: `StartPage.xaml`

- Full window content (replaces entire MainWindow content)
- Background: subtle gradient or solid light/dark theme color
- Site cards: `Border` with rounded corners, shadow, hover effect
- WrapPanel inside ScrollViewer for responsive card layout
- No Menu, no Toolbar, no StatusBar

---

## 12.2 — Tabbed Workspace

When a site is opened, the window transitions to a tabbed workspace. A `TabControl` occupies the full content area. Each tab has its own complete layout.

### Top-Level Layout

```
┌──────────────────────────────────────────────────────────────────┐
│  🔙 Back to Start  │  Site: Bouw7  │  bouw7.nl                  │
├──────────────────────────────────────────────────────────────────┤
│  [ Scraping ]  [ Control Objects ]  [ Page Objects ]  [ Corpus ]  [ Log ]  [ Settings ]  │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│                    << Tab Content Area >>                         │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Header Bar

| Element | Type | Binding |
|---------|------|---------|
| Back button | Button | Returns to Start Page, closes site context |
| Site name | TextBlock | `ActiveSite.Name` |
| Site URL | TextBlock | `ActiveSite.StartUrl` |

### Tabs

| Tab | Header | Content |
|-----|--------|---------|
| Scraping | "Scraping" | Browser + Inspector + Recording (existing core workflow) |
| Control Objects | "Control Objects" | Full control object management — analysis, approval, code gen |
| Page Objects | "Page Objects" | Page object management — generation, preview, export |
| Corpus | "Corpus" | Full corpus browser + diff + snapshots |
| Log | "Log" | Full-height log viewer with filtering |
| Settings | "Settings" | Site + app settings |

### ViewModel: `WorkspaceViewModel`

```csharp
public class WorkspaceViewModel : ViewModelBase
{
    // Properties
    public SiteInfo ActiveSite { get; }
    public int SelectedTabIndex { get; set; }

    // Tab ViewModels (created once per site session)
    public ScrapingTabViewModel ScrapingTab { get; }
    public ControlObjectsTabViewModel ControlObjectsTab { get; }
    public PageObjectsTabViewModel PageObjectsTab { get; }
    public CorpusTabViewModel CorpusTab { get; }
    public LogTabViewModel LogTab { get; }
    public SettingsTabViewModel SettingsTab { get; }

    // Commands
    public ICommand BackToStartCommand { get; }  // fires BackRequested
    public ICommand SwitchTabCommand { get; }     // param: tab index

    // Events
    public event Action? BackRequested;
}
```

### View: `WorkspacePage.xaml`

- DockPanel: header bar (DockPanel.Dock=Top) + TabControl (fill)
- TabControl with `TabStripPlacement="Top"`, styled tabs
- Each TabItem's Content is a UserControl for that tab
- No sidebar — sidebar content is absorbed into relevant tabs

---

## 12.3 — Scraping Tab

The existing browser + inspector + recording workflow, reorganized without the sidebar. Session info and corpus page list move into a collapsible left panel within this tab.

### Design

```
┌──────────────────────────────────────────────────────────────────┐
│ Toolbar: [◀][▶][🔄] [_________________URL________________][Go]  │
│          [⏺ Record] [⏸] [⏹]  [🔍 Inspect]  [📷 Capture]       │
├──────────────────────────────────────────────────────────────────┤
│ Session     │                              │ Inspector           │
│ Panel       │                              │ (when inspecting)   │
│ (240px)     │     WebView2 Browser         │ (300px)             │
│             │                              │                     │
│ ┌─────────┐ │                              │ [Forms][Inputs][Clr]│
│ │This Sess│ │                              │ Control groups: ... │
│ │ page1   │ │                              │ ┌─────────────────┐ │
│ │ page2   │ │                              │ │ DOM Tree        │ │
│ │ page3   │ │                              │ │  <html>         │ │
│ │[Analyze]│ │                              │ │    <body>       │ │
│ ├─────────┤ │                              │ │      <div#app>  │ │
│ │Corpus   │ │                              │ │        <form>   │ │
│ │ home    │ │                              │ │          ...    │ │
│ │ login   │ │                              │ └─────────────────┘ │
│ │ dash    │ │                              │                     │
│ └─────────┘ │                              │ Selected: 3         │
├─────────────┴──────────────────────────────┴─────────────────────┤
│ Status: Ready  │  Pages: 12  │  Elements: 847  │  Recording: Off │
└──────────────────────────────────────────────────────────────────┘
```

### Changes from Current Layout

- **Sidebar absorbed into tab**: Session pages + corpus pages list lives inside the Scraping tab only, not globally
- **No log viewer**: Log has its own tab now
- **Menu bar removed**: Actions moved to toolbar buttons and context menus
- **Status bar scoped to tab**: Only scraping-relevant stats

### ViewModel: `ScrapingTabViewModel`

Mostly delegates to existing ViewModels — this is a composition wrapper:

```csharp
public class ScrapingTabViewModel : ViewModelBase
{
    // Composed sub-VMs (existing)
    public BrowserViewModel Browser { get; }
    public InspectorViewModel Inspector { get; }
    public RecordingViewModel Recording { get; }
    public SidebarViewModel SessionPanel { get; }

    // Properties
    public bool IsSessionPanelVisible { get; set; } = true;
    public string StatusText { get; }

    // Commands (delegating to existing)
    public ICommand ToggleInspectCommand { get; }
    public ICommand RecordPageCommand { get; }
    public ICommand CaptureSnapshotCommand { get; }
    public ICommand AnalyzeSessionCommand { get; }
    public ICommand ToggleSessionPanelCommand { get; }
}
```

---

## 12.4 — Control Objects Tab (Full Design)

A dedicated workspace for viewing, editing, generating, and organizing Brinell control objects. Control objects are reusable `ContainerBase<TParent, TScope>` classes that encapsulate a DOM pattern (e.g., a login form, a date picker, a navigation menu) and expose its interactive elements as typed properties. Replaces the current minimal `ControlsManagerView`.

### Design

```
┌──────────────────────────────────────────────────────────────────┐
│ Toolbar: [🔬 Analyze Corpus] [⚡ Generate All Pending]           │
│          [📥 Import Control Objects] [📤 Export Control Objects]   │
├──────────────┬───────────────────────────────────────────────┤
│ ControlObject │ Control Object Detail                                │
│ List         │                                                   │
│              │ ┌───────────────────────────────────────────────┐ │
│ 🔎 Filter... │ │ Name: LoginFormControl                        │ │
│              │ │ Namespace: Bouw7.Controls                     │ │
│ ┌──────────┐ │ │ Confidence: 92%  │  Status: ✅ Approved       │ │
│ │ ✅ Login  │ │ │ DOM Signature: form.login-form               │ │
│ │   Form   │ │ │ Created: 2026-05-01                          │ │
│ │   92%    │ │ └───────────────────────────────────────────────┘ │
│ ├──────────┤ │                                                   │
│ │ ✅ Nav    │ │ ┌─────────────────────────────────────────────┐  │
│ │   Menu   │ │ │ Properties                                  │  │
│ │   88%    │ │ │ ┌────────────┬──────────┬─────────────────┐ │  │
│ ├──────────┤ │ │ │ Name       │ Type     │ Locator         │ │  │
│ │ ⏳ Data   │ │ │ ├────────────┼──────────┼─────────────────┤ │  │
│ │   Table  │ │ │ │ Username   │ TextInput│ [name=user]     │ │  │
│ │   75%    │ │ │ │ Password   │ TextInput│ [name=pass]     │ │  │
│ │ [Pending]│ │ │ │ SubmitBtn  │ Button   │ button[type=sub]│ │  │
│ ├──────────┤ │ │ │ RememberMe │ Checkbox │ #remember       │ │  │
│ │ ❌ Footer │ │ │ └────────────┴──────────┴─────────────────┘ │  │
│ │   Links  │ │ └─────────────────────────────────────────────┘  │
│ │   45%    │ │                                                   │
│ │[Rejected]│ │ ┌─────────────────────────────────────────────┐  │
│ └──────────┘ │ │ Generated Code                    [Copy][📋] │  │
│              │ │                                             │  │
│ ──────────── │ │ public class LoginFormControl               │  │
│ Summary:     │ │     : HtmlControl                           │  │
│  4 controls  │ │ {                                           │  │
│  2 approved  │ │     public TextInputControl Username =>     │  │
│  1 pending   │ │         Find<TextInputControl>("[name=..]");│  │
│  1 rejected  │ │     public TextInputControl Password =>     │  │
│              │ │         Find<TextInputControl>("[name=..]");│  │
│              │ │     public ButtonControl SubmitBtn =>        │  │
│              │ │         Find<ButtonControl>("button[..]");   │  │
│              │ │ }                                           │  │
│              │ └─────────────────────────────────────────────┘  │
│              │                                                   │
│              │ ┌─────────────────────────────────────────────┐  │
│              │ │ DOM Preview                                 │  │
│              │ │ <form class="login-form">                   │  │
│              │ │   <input name="user" type="text"/>          │  │
│              │ │   <input name="pass" type="password"/>      │  │
│              │ │   <button type="submit">Login</button>      │  │
│              │ │   <label><input type="checkbox"/> Remember  │  │
│              │ │ </form>                                     │  │
│              │ └─────────────────────────────────────────────┘  │
└──────────────┴───────────────────────────────────────────────────┘
```

### Left Panel — Control Object List

| Element | Type | Binding |
|---------|------|---------|
| Filter box | TextBox | Filters by name, status, tag |
| Control object items | ListBox | `FilteredControlObjects` ICollectionView |
| Item: status icon | TextBlock | ✅ approved, ⏳ pending, ❌ rejected |
| Item: name | TextBlock | `ControlProposal.Name` or `GeneratedControl.Name` |
| Item: confidence | TextBlock | `Confidence` as percentage |
| Item: status label | TextBlock | "Approved" / "Pending" / "Rejected" |
| Summary section | StackPanel | Total, approved, pending, rejected counts |

### Right Panel — Control Object Detail

Split into 4 sections (vertically scrollable):

**1. Header Card**

| Element | Type | Binding |
|---------|------|---------|
| Name | TextBlock (large) | `SelectedControlObject.Name` |
| Namespace | TextBlock | `SelectedControlObject.Namespace` |
| Confidence | ProgressBar + label | `SelectedControlObject.Confidence` |
| Status | TextBlock + icon | Approved / Pending / Rejected |
| DOM Signature | TextBlock (mono) | `SelectedControlObject.DomSignature` |
| Created date | TextBlock | `SelectedControlObject.CreatedAt` |

**2. Properties Table**

| Column | Binding |
|--------|---------|
| Name | `SuggestedProperties[].Name` |
| Type | `SuggestedProperties[].ControlType` |
| Locator | `SuggestedProperties[].Selector` |

Editable in-place. Add/Remove buttons below.

**3. Generated Code**

| Element | Type | Binding |
|---------|------|---------|
| Code block | TextBox (readonly, mono font) | `SelectedControlObject.Code` |
| Copy button | Button | Copies code to clipboard |
| Regenerate button | Button | Re-runs LLM generation for this control object |

**4. DOM Preview**

| Element | Type | Binding |
|---------|------|---------|
| HTML snippet | TextBox (readonly, mono) | `SelectedControlObject.ExampleSnippet` |

### Context Menu on Control Object Items

- Approve / Reject
- Regenerate Code
- Delete Control Object
- Copy Code

### ViewModel: `ControlObjectsTabViewModel`

```csharp
public class ControlObjectsTabViewModel : ViewModelBase
{
    // Collections
    public ObservableCollection<ControlObjectListItem> ControlObjects { get; }
    public ICollectionView FilteredControlObjects { get; }

    // Properties
    public string FilterText { get; set; }
    public ControlObjectListItem? SelectedControlObject { get; set; }
    public string CodePreview { get; }
    public string DomPreview { get; }
    public int TotalCount { get; }
    public int ApprovedCount { get; }
    public int PendingCount { get; }
    public int RejectedCount { get; }

    // Detail: selected control object properties
    public ObservableCollection<ControlPropertyItem> Properties { get; }

    // Commands
    public ICommand AnalyzeCorpusCommand { get; }      // async — runs full analysis
    public ICommand GenerateAllPendingCommand { get; }  // generates code for all approved
    public ICommand ApproveCommand { get; }             // param: ControlObjectListItem
    public ICommand RejectCommand { get; }              // param: ControlObjectListItem
    public ICommand RegenerateCommand { get; }          // regenerates selected control object
    public ICommand DeleteControlObjectCommand { get; } // removes from registry
    public ICommand CopyCodeCommand { get; }            // clipboard
    public ICommand ImportControlObjectsCommand { get; }// file dialog
    public ICommand ExportControlObjectsCommand { get; }// file dialog
    public ICommand AddPropertyCommand { get; }
    public ICommand RemovePropertyCommand { get; }

    // Methods
    public void LoadControlObjects();  // populates from ControlRegistry + AnalysisResults
}
```

### Supporting Model: `ControlObjectListItem`

```csharp
public class ControlObjectListItem : ViewModelBase
{
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string DomSignature { get; set; }
    public double Confidence { get; set; }
    public ControlObjectStatus Status { get; set; }  // enum: Approved, Pending, Rejected
    public string Code { get; set; }
    public string ExampleSnippet { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ControlPropertyItem> SuggestedProperties { get; set; }
    public int UsedByPageCount { get; set; }  // how many page objects reference this
}

public class ControlPropertyItem : ViewModelBase
{
    public string Name { get; set; }
    public string ControlType { get; set; }
    public string Selector { get; set; }
}

public enum ControlObjectStatus { Pending, Approved, Rejected }
```

---

## 12.5 — Page Objects Tab (Full Design)

A dedicated workspace for managing generated page objects. Page objects are `HtmlPageObjectBase<Self>` classes that represent a full page and expose its elements (including control objects) as typed properties. Each page object corresponds to a corpus page snapshot.

### Design

```
┌──────────────────────────────────────────────────────────────────┐
│ Toolbar: [⚡ Generate All] [🔄 Regenerate Selected]              │
│          [📤 Export Page Objects] [📂 Open Output Folder]        │
├──────────────┬───────────────────────────────────────────────────┤
│ Page Object  │ Page Object Detail                                │
│ List (280px) │                                                   │
│              │ ┌───────────────────────────────────────────────┐ │
│ 🔎 Filter... │ │ Name: LoginPage                               │ │
│              │ │ Namespace: Bouw7.Pages                        │ │
│ ┌──────────┐ │ │ Source: /login (28 elements)                  │ │
│ │ ✅ Login  │ │ │ Status: ✅ Generated  │  Validated: ✅        │ │
│ │   Page   │ │ │ Control Objects Used: 2                      │ │
│ │ 28 elems │ │ │ Generated: 2026-05-03                        │ │
│ ├──────────┤ │ └───────────────────────────────────────────────┘ │
│ │ ✅ Dash   │ │                                                   │
│ │   board  │ │ ┌─────────────────────────────────────────────┐  │
│ │   Page   │ │ │ Properties                                  │  │
│ │ 45 elems │ │ │ ┌────────────┬───────────────┬────────────┐ │  │
│ ├──────────┤ │ │ │ Name       │ Type          │ Locator    │ │  │
│ │ ⏳ Users  │ │ │ ├────────────┼───────────────┼────────────┤ │  │
│ │   Page   │ │ │ │ Username   │ TextInput     │ ByText     │ │  │
│ │ 67 elems │ │ │ │ Password   │ TextInput     │ ByText     │ │  │
│ │[Not Gen] │ │ │ │ LoginForm  │ LoginFormCtrl │ ByCss      │ │  │
│ ├──────────┤ │ │ │ Submit     │ Button        │ ByText     │ │  │
│ │ ❌ Home   │ │ │ │ NavMenu    │ NavMenuCtrl   │ ByRole     │ │  │
│ │   Page   │ │ │ └────────────┴───────────────┴────────────┘ │  │
│ │ 12 elems │ │ └─────────────────────────────────────────────┘  │
│ │[Errors]  │ │                                                   │
│ └──────────┘ │ ┌─────────────────────────────────────────────┐  │
│              │ │ Generated Code                    [Copy][📋] │  │
│ ──────────── │ │                                             │  │
│ Summary:     │ │ public sealed class LoginPage               │  │
│  4 pages     │ │     : HtmlPageObjectBase<LoginPage>         │  │
│  2 generated │ │ {                                           │  │
│  1 pending   │ │   public LoginPage(IHtmlTestContext ctx)    │  │
│  1 errors    │ │       : base(ctx) { }                       │  │
│              │ │                                             │  │
│              │ │   public TextInputControl<LoginPage>        │  │
│              │ │       Username => Control<...>(             │  │
│              │ │         Locator.ByText("Username"));        │  │
│              │ │                                             │  │
│              │ │   public LoginFormControl<LoginPage>        │  │
│              │ │       LoginForm => Control<...>(            │  │
│              │ │         Locator.ByCss(".login-form"));      │  │
│              │ │ }                                           │  │
│              │ └─────────────────────────────────────────────┘  │
│              │                                                   │
│              │ ┌─────────────────────────────────────────────┐  │
│              │ │ Validation                                  │  │
│              │ │  ✅ Syntax: OK                               │  │
│              │ │  ✅ Control types: All resolved              │  │
│              │ │  ⚠️ Locators: 1 ByCss usage (LoginForm)     │  │
│              │ │  ✅ Compilation: Roslyn OK                   │  │
│              │ └─────────────────────────────────────────────┘  │
│              │                                                   │
│              │ ┌─────────────────────────────────────────────┐  │
│              │ │ Used Control Objects                        │  │
│              │ │  • LoginFormControl (form.login-form)       │  │
│              │ │  • NavMenuControl (nav.main-nav)            │  │
│              │ └─────────────────────────────────────────────┘  │
└──────────────┴───────────────────────────────────────────────────┘
```

### Left Panel — Page Object List

| Element | Type | Binding |
|---------|------|---------|
| Filter box | TextBox | Filters by page name or URL |
| Page object items | ListBox | `FilteredPageObjects` ICollectionView |
| Item: status icon | TextBlock | ✅ generated, ⏳ pending, ❌ errors |
| Item: page name | TextBlock | `PageName` |
| Item: element count | TextBlock | Source snapshot element count |
| Item: status label | TextBlock | "Generated" / "Not Generated" / "Errors" |
| Summary section | StackPanel | Total, generated, pending, error counts |

### Right Panel — Page Object Detail

Split into 5 sections (vertically scrollable):

**1. Header Card**

| Element | Type | Binding |
|---------|------|---------|
| Name | TextBlock (large) | `SelectedPageObject.ClassName` |
| Namespace | TextBlock | `SelectedPageObject.Namespace` |
| Source page | TextBlock | Page URL + element count |
| Status | TextBlock + icon | Generated / Pending / Error |
| Control objects used | TextBlock | Count of referenced control objects |
| Generated date | TextBlock | `SelectedPageObject.GeneratedAt` |

**2. Properties Table**

| Column | Binding |
|--------|---------|
| Name | Property name from generated code |
| Type | Control type (built-in or custom control object) |
| Locator | Locator method used (ByText, ByDataTestId, etc.) |

Read-only (regenerate to change).

**3. Generated Code**

| Element | Type | Binding |
|---------|------|---------|
| Code block | TextBox (readonly, mono font) | `SelectedPageObject.MainCode` |
| Container code blocks | Expander per container | `SelectedPageObject.ContainerCodes[]` |
| Copy button | Button | Copies all code to clipboard |
| Regenerate button | Button | Re-runs LLM generation for this page |

**4. Validation Results**

| Element | Type | Binding |
|---------|------|---------|
| Syntax status | TextBlock + icon | Roslyn parse result |
| Type resolution | TextBlock + icon | All control types resolved? |
| Locator warnings | TextBlock + icon | ByCss usage warnings |
| Compilation status | TextBlock + icon | Full Roslyn compile result |
| Error list | ListBox | Individual errors/warnings if any |

**5. Used Control Objects**

| Element | Type | Binding |
|---------|------|---------|
| Control object list | ItemsControl | `SelectedPageObject.UsedControlObjects` |
| Each item | TextBlock | Control name + DOM signature |
| Navigate link | Button | Switches to Control Objects tab, selects this control |

### Context Menu on Page Object Items

- Generate / Regenerate
- Copy Code
- Open Source Page in Browser (switches to Scraping tab)
- Export Page Object (.cs file)
- Delete

### ViewModel: `PageObjectsTabViewModel`

```csharp
public class PageObjectsTabViewModel : ViewModelBase
{
    // Collections
    public ObservableCollection<PageObjectListItem> PageObjects { get; }
    public ICollectionView FilteredPageObjects { get; }

    // Properties
    public string FilterText { get; set; }
    public PageObjectListItem? SelectedPageObject { get; set; }
    public string MainCodePreview { get; }
    public int TotalCount { get; }
    public int GeneratedCount { get; }
    public int PendingCount { get; }
    public int ErrorCount { get; }

    // Detail: properties extracted from generated code
    public ObservableCollection<PageObjectPropertyItem> Properties { get; }
    public ObservableCollection<ValidationEntry> ValidationResults { get; }
    public ObservableCollection<ControlObjectReference> UsedControlObjects { get; }

    // Commands
    public ICommand GenerateAllCommand { get; }          // generates for all corpus pages
    public ICommand RegenerateSelectedCommand { get; }   // regenerates selected page object
    public ICommand CopyCodeCommand { get; }             // clipboard
    public ICommand ExportPageObjectsCommand { get; }    // writes .cs files to output path
    public ICommand OpenOutputFolderCommand { get; }     // opens output directory in Explorer
    public ICommand OpenSourcePageCommand { get; }       // fires NavigateRequested
    public ICommand DeletePageObjectCommand { get; }
    public ICommand NavigateToControlObjectCommand { get; } // fires ControlObjectNavigateRequested

    // Events
    public event Action<string>? NavigateRequested;                  // URL → Scraping tab
    public event Action<string>? ControlObjectNavigateRequested;     // control name → Control Objects tab

    // Methods
    public void LoadPageObjects(long siteId);
}
```

### Supporting Models

```csharp
public class PageObjectListItem : ViewModelBase
{
    public long SnapshotId { get; set; }
    public string PageName { get; set; }
    public string PageUrl { get; set; }
    public string ClassName { get; set; }
    public string Namespace { get; set; }
    public string MainCode { get; set; }
    public List<string> ContainerCodes { get; set; } = [];
    public PageObjectStatus Status { get; set; }
    public int SourceElementCount { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public List<string> UsedControlObjectNames { get; set; } = [];
    public ValidationResult? Validation { get; set; }
}

public class PageObjectPropertyItem
{
    public string Name { get; set; }
    public string ControlType { get; set; }
    public string LocatorMethod { get; set; }
    public bool IsCustomControlObject { get; set; }
}

public class ControlObjectReference
{
    public string Name { get; set; }
    public string DomSignature { get; set; }
}

public class ValidationEntry
{
    public string Category { get; set; }    // "Syntax", "Types", "Locators", "Compilation"
    public string Status { get; set; }      // "OK", "Warning", "Error"
    public string Message { get; set; }
}

public enum PageObjectStatus { NotGenerated, Generated, Error }
```

---

## 12.6 — Corpus Tab (Full Design)

A complete corpus management workspace. Replaces the current `CorpusBrowserView` with richer features: snapshot comparison, page grouping, bulk operations, inline DOM preview, and generation status per page (shows whether control objects and page objects have been generated).

### Design

```
┌──────────────────────────────────────────────────────────────────┐
│ Toolbar: [📷 Re-Record All] [🔄 Refresh] [📤 Export] [📥 Import] │
│          [🗑 Delete Selected]                                    │
├──────────────┬───────────────────────────────────────────────────┤
│ Page List    │ Snapshot Detail                                   │
│ (280px)      │                                                   │
│              │ Page: Login                                       │
│ 🔎 Filter... │ URL: https://bouw7.nl/login                      │
│              │ Versions: 3  │  Latest: 2026-05-03               │
│ ┌──────────┐ │                                                   │
│ │ 📄 Home   │ │ ┌─────────────────────────────────────────────┐  │
│ │  12 elem  │ │ │ Version History                             │  │
│ │  3 vers.  │ │ │ ┌────────┬──────┬───────┬────────┬───────┐ │  │
│ ├──────────┤ │ │ │ Version│ Date │ Elems │ Size   │       │ │  │
│ │ 📄 Login  │ │ │ ├────────┼──────┼───────┼────────┼───────┤ │  │
│ │  28 elem  │ │ │ │ v3 ★  │ May 3│ 28    │ 4.2 KB │[View] │ │  │
│ │  3 vers.  │ │ │ │ v2     │ May 1│ 25    │ 3.8 KB │[View] │ │  │
│ ├──────────┤ │ │ │ v1     │ Apr28│ 22    │ 3.1 KB │[View] │ │  │
│ │ 📄 Dash   │ │ │ └────────┴──────┴───────┴────────┴───────┘ │  │
│ │  45 elem  │ │ │                                             │  │
│ │  1 vers.  │ │ │ [Compare v3 ↔ v2]   [Compare v3 ↔ v1]     │  │
│ ├──────────┤ │ └─────────────────────────────────────────────┘  │
│ │ 📄 Users  │ │                                                   │
│ │  67 elem  │ │ ┌─────────────────────────────────────────────┐  │
│ │  2 vers.  │ │ │ DOM Preview (selected version)              │  │
│ └──────────┘ │ │                                              │  │
│              │ │  <html>                                      │  │
│ ──────────── │ │    <head>...</head>                          │  │
│ Totals:      │ │    <body>                                    │  │
│  4 pages     │ │      <div id="app">                         │  │
│  9 snapshots │ │        <form class="login-form">            │  │
│  152 elements│ │          <input name="user" type="text"/>   │  │
│  15.1 KB     │ │          <input name="pass" type="pass...   │  │
│              │ │          <button type="submit">Login</but.. │  │
│              │ │        </form>                               │  │
│              │ │      </div>                                  │  │
│              │ │    </body>                                   │  │
│              │ │  </html>                                     │  │
│              │ └─────────────────────────────────────────────┘  │
│              │                                                   │
│              │ ┌─────────────────────────────────────────────┐  │
│              │ │ Element Stats                               │  │
│              │ │  Tags: div(12) input(4) button(2) form(1)   │  │
│              │ │  With ID: 8  │  With class: 15  │  Inputs: 4│  │
│              │ │  Stable locators: 6  │  Unstable: 2         │  │
│              │ └─────────────────────────────────────────────┘  │
└──────────────┴───────────────────────────────────────────────────┘
```

### Left Panel — Page List

| Element | Type | Binding |
|---------|------|---------|
| Filter box | TextBox | Filters by page name or URL |
| Page items | ListBox | `FilteredPages` ICollectionView |
| Item: icon | TextBlock | 📄 static icon |
| Item: name | TextBlock | `PageName` |
| Item: element count | TextBlock | Latest snapshot element count |
| Item: version count | TextBlock | Number of snapshots for this page |
| Totals section | StackPanel | Total pages, snapshots, elements, size |

### Right Panel — Snapshot Detail

**1. Page Header**

| Element | Binding |
|---------|---------|
| Page name | `SelectedPage.PageName` |
| URL | `SelectedPage.PageUrl` |
| Version count | `SelectedPage.Versions.Count` |
| Latest date | `SelectedPage.LatestSnapshot.CapturedAt` |

**2. Version History Table**

| Column | Binding |
|--------|---------|
| Version | Sequential (v1, v2, ...), star (★) on latest |
| Date | `CapturedAt` formatted |
| Elements | `ElementCount` |
| Size | `SnapshotSizeBytes` formatted |
| View button | Selects this version for DOM preview |
| Compare buttons | Opens `DiffWindow` comparing two selected versions |

**3. DOM Preview**

- TreeView or indented TextBlock showing the DOM structure of the selected snapshot version
- Uses `DomTreePanel` UserControl (existing) embedded inline

**4. Element Stats**

- Tag frequency breakdown
- Counts: elements with ID, with class, input elements
- Locator stability assessment (how many have unique stable selectors)

### Context Menu on Page Items

- Re-Record Page
- Export Page (single snapshot JSON)
- Delete All Versions
- Open URL in Browser (switches to Scraping tab)

### ViewModel: `CorpusTabViewModel`

```csharp
public class CorpusTabViewModel : ViewModelBase
{
    // Collections
    public ObservableCollection<CorpusPageGroup> Pages { get; }
    public ICollectionView FilteredPages { get; }

    // Properties
    public string FilterText { get; set; }
    public CorpusPageGroup? SelectedPage { get; set; }
    public SnapshotSummary? SelectedVersion { get; set; }
    public DomSnapshot? PreviewSnapshot { get; set; }
    public DomTreeViewModel DomPreview { get; }

    // Stats
    public int TotalPages { get; }
    public int TotalSnapshots { get; }
    public int TotalElements { get; }
    public string TotalSize { get; }
    public string ElementStatsSummary { get; }

    // Commands
    public ICommand RefreshCommand { get; }
    public ICommand ReRecordAllCommand { get; }
    public ICommand ExportCorpusCommand { get; }
    public ICommand ImportCorpusCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand ViewVersionCommand { get; }         // param: SnapshotSummary
    public ICommand CompareVersionsCommand { get; }     // param: (v1, v2) tuple
    public ICommand ReRecordPageCommand { get; }        // param: CorpusPageGroup
    public ICommand DeletePageCommand { get; }          // param: CorpusPageGroup
    public ICommand ExportPageCommand { get; }          // param: CorpusPageGroup
    public ICommand OpenInBrowserCommand { get; }       // fires NavigateRequested

    // Events
    public event Action<string>? NavigateRequested;     // URL to navigate to in Scraping tab
    public event Action<DomDiffResult, string>? DiffRequested;

    // Methods
    public void Load(long siteId);
}
```

### Supporting Model: `CorpusPageGroup`

```csharp
public class CorpusPageGroup
{
    public string PageName { get; set; }
    public string PageUrl { get; set; }
    public List<SnapshotSummary> Versions { get; set; }
    public SnapshotSummary LatestSnapshot => Versions.OrderByDescending(v => v.CapturedAt).First();
    public int TotalElements => LatestSnapshot.ElementCount;
}
```

---

### Additional Corpus Columns — Generation Status

The page list items additionally show:

| Element | Type | Binding |
|---------|------|---------|
| Control objects icon | TextBlock | ✅ if control objects detected, ⏳ if pending analysis, — if none |
| Page object icon | TextBlock | ✅ if page object generated, ⏳ if pending, ❌ if errors |

The version history table additionally shows:

| Column | Binding |
|--------|---------|
| Has Page Object | Icon indicating whether a page object was generated from this version |
| Generate button | Button to generate/regenerate page object from this version |

---

## 12.7 — Log Tab

The existing `LogViewerPanel` promoted to a full tab. No changes to the underlying `LogViewerViewModel` — just full vertical space.

### Design

```
┌──────────────────────────────────────────────────────────────────┐
│ Toolbar: Level: [All ▾]  │  🔎 Search...  │  [Clear] [Export]   │
├──────────────────────────────────────────────────────────────────┤
│ Timestamp           │ Level │ Source              │ Message      │
│ 2026-05-04 10:23:01 │ INFO  │ MainViewModel       │ Site opened │
│ 2026-05-04 10:23:02 │ DEBUG │ DomCaptureService   │ Capturing..│
│ 2026-05-04 10:23:03 │ INFO  │ PageTransitionDet.. │ SPA nav ..  │
│ 2026-05-04 10:23:04 │ WARN  │ CopilotService      │ Rate limit │
│ 2026-05-04 10:23:05 │ ERROR │ CodeValidator       │ CS0246 ...  │
│ ...                                                              │
│                                                                  │
│                                                                  │
│                                                                  │
│ ─────────────────────────────────────────────────────────────── │
│ 342 entries  │  Showing: 342  │  Auto-scroll: ✅                │
└──────────────────────────────────────────────────────────────────┘
```

### Additions over Current LogViewerPanel

| Element | Type | Purpose |
|---------|------|---------|
| Search box | TextBox | Text search across all log fields |
| Export button | Button | Saves filtered log to `.log` or `.json` file |
| DataGrid | DataGrid (replaces ListBox) | Sortable columns, better formatting |
| Status bar | StackPanel | Entry count, filter count, auto-scroll toggle |

### ViewModel: `LogTabViewModel`

```csharp
public class LogTabViewModel : ViewModelBase
{
    // Delegates to existing LogViewerViewModel
    public LogViewerViewModel LogViewer { get; }

    // Additional
    public string SearchText { get; set; }    // additional text filter
    public int ShownCount { get; }
    public ICommand ExportLogCommand { get; } // saves to file
}
```

---

## 12.8 — Settings Tab

A single settings page covering both app-wide and site-specific configuration. Consolidates the settings from Phase 11 into the tabbed workspace.

### Design

```
┌──────────────────────────────────────────────────────────────────┐
│                         Settings                                 │
│                                                                  │
│  ┌─── GitHub Copilot ───────────────────────────────────────┐    │
│  │                                                          │    │
│  │  Analyzer Model:  [ gpt-4o-mini           ▾ ]           │    │
│  │  Generator Model: [ gpt-4o                ▾ ]           │    │
│  │  Temperature:     [====●=============] 0.20              │    │
│  │  Max Tokens:      [ 4096          ]                      │    │
│  │  Timeout (sec):   [ 120           ]                      │    │
│  │                                                          │    │
│  └──────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ┌─── GitHub Integration ───────────────────────────────────┐    │
│  │                                                          │    │
│  │  GitHub Token:    [●●●●●●●●●●●●●●●●●●●     ] [Show][Test]│    │
│  │  Status:          ✅ Connected as @jonk435                │    │
│  │                                                          │    │
│  │  Default Repository: [ jonk435/brinell-output  ▾ ]       │    │
│  │  Branch:             [ main                    ▾ ]       │    │
│  │  Auto-push generated code:  [ ] ☐                        │    │
│  │                                                          │    │
│  └──────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ┌─── Corpus Storage ──────────────────────────────────────┐     │
│  │                                                          │    │
│  │  Database Path:   [C:\Users\...\scraper.db     ][Browse] │    │
│  │  Auto-analyze after recording:  [✅]                      │    │
│  │  Keep snapshot history:         [✅]                      │    │
│  │                                                          │    │
│  └──────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ┌─── Site: Bouw7 (current) ───────────────────────────────┐     │
│  │                                                          │    │
│  │  Namespace:       [ Bouw7.Pages           ]              │    │
│  │  Output Path:     [ E:\repos\Bouw7\Tests  ] [Browse]     │    │
│  │  Start URL:       [ https://bouw7.nl      ]              │    │
│  │  URL Aliases:     [ bouw7.nl, www.bouw7.nl ]             │    │
│  │                                                          │    │
│  │  Custom Prompt Additions:                                │    │
│  │  ┌──────────────────────────────────────────────────┐    │    │
│  │  │ Always use data-testid for element locators.     │    │    │
│  │  │ Prefix all page classes with "Bouw7".            │    │    │
│  │  └──────────────────────────────────────────────────┘    │    │
│  │  [Preview Prompt] [Reset to Default]                     │    │
│  │                                                          │    │
│  │  Control Type Mappings:                                  │    │
│  │  ┌──────────────┬──────────────────┬──────────┐          │    │
│  │  │ Selector     │ Brinell Control  │ Priority │          │    │
│  │  ├──────────────┼──────────────────┼──────────┤          │    │
│  │  │ div.btn      │ ButtonControl    │ 10       │          │    │
│  │  │ [role=button]│ ButtonControl    │ 5        │          │    │
│  │  │ select.custom│ SelectControl    │ 10       │          │    │
│  │  └──────────────┴──────────────────┴──────────┘          │    │
│  │  [Add] [Edit] [Delete] [▲ Up] [▼ Down]                   │    │
│  │                                                          │    │
│  └──────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ┌─── Application ─────────────────────────────────────────┐     │
│  │                                                          │    │
│  │  Theme:  (●) Light  (○) Dark  (○) System                │    │
│  │  Log Level:  [ Information ▾ ]                           │    │
│  │  WebView2 User Data Folder: [auto          ] [Browse]    │    │
│  │                                                          │    │
│  └──────────────────────────────────────────────────────────┘    │
│                                                                  │
│                                [Save]  [Reset to Defaults]       │
└──────────────────────────────────────────────────────────────────┘
```

### Settings Sections

**1. GitHub Copilot** — LLM configuration

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| Analyzer Model | ComboBox | gpt-4o-mini | Cheaper model for analysis |
| Generator Model | ComboBox | gpt-4o | Smarter model for code gen |
| Temperature | Slider (0.0–1.0) | 0.20 | Step 0.05 |
| Max Tokens | TextBox (int) | 4096 | Range 1024–16384 |
| Timeout | TextBox (int) | 120 | Seconds |

**2. GitHub Integration** — Token + repo for pushing generated code

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| GitHub Token | PasswordBox | empty | PAT or fine-grained token |
| Show button | ToggleButton | — | Toggles password visibility |
| Test button | Button | — | Validates token against GitHub API |
| Status | TextBlock | — | Shows connection status + username |
| Default Repository | ComboBox | — | Loaded from GitHub API after token validation |
| Branch | ComboBox | main | Loaded from selected repo |
| Auto-push | CheckBox | false | Push generated files on approval |

**3. Corpus Storage** — Database and behavior

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| Database Path | TextBox + Browse | `%LOCALAPPDATA%\Brinell.Scraper\scraper.db` | Read-only display, Browse relocates |
| Auto-analyze | CheckBox | true | Run analysis after recording stops |
| Keep history | CheckBox | true | Retain all snapshot versions |

**4. Current Site** — Site-specific settings (only visible when a site is active)

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| Namespace | TextBox | — | .NET namespace for generated code |
| Output Path | TextBox + Browse | — | Directory for generated .cs files |
| Start URL | TextBox | — | Initial URL for browser |
| URL Aliases | TextBox | — | Comma-separated, for multi-domain sites |
| Custom Prompt | TextBox (multiline) | — | Appended to SKILL.md content |
| Preview Prompt | Button | — | Shows resolved full prompt |
| Reset to Default | Button | — | Clears custom prompt additions |
| Control Mappings | DataGrid | — | Editable selector → control type rules |

**5. Application** — General app settings

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| Theme | RadioButtons | System | Light / Dark / System |
| Log Level | ComboBox | Information | Trace, Debug, Info, Warn, Error |
| WebView2 User Data | TextBox + Browse | auto | Custom path for WebView2 profile |

### ViewModel: `SettingsTabViewModel`

```csharp
public class SettingsTabViewModel : ViewModelBase
{
    // GitHub Copilot
    public string AnalyzerModel { get; set; }
    public string GeneratorModel { get; set; }
    public ObservableCollection<string> AvailableModels { get; }
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public int TimeoutSeconds { get; set; }

    // GitHub Integration
    public string GitHubToken { get; set; }
    public bool IsTokenVisible { get; set; }
    public string GitHubStatus { get; set; }
    public bool IsGitHubConnected { get; set; }
    public string SelectedRepository { get; set; }
    public ObservableCollection<string> Repositories { get; }
    public string SelectedBranch { get; set; }
    public ObservableCollection<string> Branches { get; }
    public bool AutoPushGenerated { get; set; }

    // Corpus
    public string DatabasePath { get; }
    public bool AutoAnalyzeAfterRecording { get; set; }
    public bool KeepSnapshotHistory { get; set; }

    // Current Site
    public string SiteNamespace { get; set; }
    public string SiteOutputPath { get; set; }
    public string SiteStartUrl { get; set; }
    public string SiteUrlAliases { get; set; }
    public string CustomPromptAdditions { get; set; }
    public ObservableCollection<ControlMappingRule> ControlMappings { get; }
    public bool HasActiveSite { get; }

    // Application
    public string SelectedTheme { get; set; }
    public string SelectedLogLevel { get; set; }
    public string WebViewUserDataPath { get; set; }

    // Commands
    public ICommand TestGitHubConnectionCommand { get; }    // async — validates token
    public ICommand BrowseDatabasePathCommand { get; }
    public ICommand BrowseOutputPathCommand { get; }
    public ICommand BrowseWebViewPathCommand { get; }
    public ICommand PreviewPromptCommand { get; }
    public ICommand ResetPromptCommand { get; }
    public ICommand AddMappingCommand { get; }
    public ICommand EditMappingCommand { get; }
    public ICommand DeleteMappingCommand { get; }
    public ICommand MoveMappingUpCommand { get; }
    public ICommand MoveMappingDownCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }
    public ICommand ToggleTokenVisibilityCommand { get; }

    // Methods
    public void Load(SiteInfo? activeSite);
    public void Save();
}
```

### Supporting Model: `ControlMappingRule`

```csharp
public class ControlMappingRule : ViewModelBase
{
    public string Selector { get; set; }
    public string BrinellControlType { get; set; }
    public int Priority { get; set; }
    public string Notes { get; set; }
    public bool IsBuiltIn { get; set; }  // built-in rules can't be deleted
}
```

### Persistence: `ScraperSettings`

```csharp
public class ScraperSettings
{
    // Copilot
    public string AnalyzerModel { get; set; } = "gpt-4o-mini";
    public string GeneratorModel { get; set; } = "gpt-4o";
    public double Temperature { get; set; } = 0.20;
    public int MaxTokens { get; set; } = 4096;
    public int TimeoutSeconds { get; set; } = 120;

    // GitHub
    public string GitHubToken { get; set; } = "";
    public string DefaultRepository { get; set; } = "";
    public string DefaultBranch { get; set; } = "main";
    public bool AutoPushGenerated { get; set; }

    // Corpus
    public bool AutoAnalyzeAfterRecording { get; set; } = true;
    public bool KeepSnapshotHistory { get; set; } = true;

    // Application
    public string Theme { get; set; } = "System";
    public string LogLevel { get; set; } = "Information";
    public string WebViewUserDataPath { get; set; } = "";
}
```

Stored at `%APPDATA%\Brinell.Scraper\settings.json`. Loaded via `IOptions<ScraperSettings>`, live-reload via `IOptionsMonitor<ScraperSettings>`.

---

## Navigation Flow Summary

```
App Launch
    │
    ▼
┌──────────┐   Open Site    ┌──────────────────────────┐
│ Start    │ ─────────────→ │ Tabbed Workspace         │
│ Page     │                │  ├─ Scraping Tab          │
│          │ ←───────────── │  ├─ Control Objects Tab   │
│          │   Back button  │  ├─ Page Objects Tab      │
└──────────┘                │  ├─ Corpus Tab            │
                            │  ├─ Log Tab               │
                            │  └─ Settings Tab          │
                            └──────────────────────────┘
```

### Window Content Switching

```csharp
// MainWindow.xaml.cs — simplified
private void ShowStartPage()
{
    Content = new StartPage { DataContext = _startPageVM };
}

private void ShowWorkspace(SiteInfo site)
{
    var workspaceVM = new WorkspaceViewModel(site, _services);
    Content = new WorkspacePage { DataContext = workspaceVM };
    workspaceVM.BackRequested += () => ShowStartPage();
}
```

---

## Implementation Steps

| Step | Task | Files |
|------|------|-------|
| 12.1a | Create `StartPageViewModel` | `ViewModels/StartPageViewModel.cs` |
| 12.1b | Create `StartPage.xaml` + code-behind | `Views/StartPage.xaml`, `Views/StartPage.xaml.cs` |
| 12.2a | Create `WorkspaceViewModel` | `ViewModels/WorkspaceViewModel.cs` |
| 12.2b | Create `WorkspacePage.xaml` + code-behind | `Views/WorkspacePage.xaml`, `Views/WorkspacePage.xaml.cs` |
| 12.3a | Create `ScrapingTabViewModel` | `ViewModels/ScrapingTabViewModel.cs` |
| 12.3b | Extract scraping content from `MainWindow.xaml` into `ScrapingTab.xaml` | `Views/ScrapingTab.xaml` |
| 12.4a | Create `ControlObjectsTabViewModel` + models | `ViewModels/ControlObjectsTabViewModel.cs` |
| 12.4b | Create `ControlObjectsTab.xaml` (full layout) | `Views/ControlObjectsTab.xaml` |
| 12.5a | Create `PageObjectsTabViewModel` + models | `ViewModels/PageObjectsTabViewModel.cs` |
| 12.5b | Create `PageObjectsTab.xaml` (full layout) | `Views/PageObjectsTab.xaml` |
| 12.6a | Create `CorpusTabViewModel` + `CorpusPageGroup` | `ViewModels/CorpusTabViewModel.cs` |
| 12.6b | Create `CorpusTab.xaml` (full layout) | `Views/CorpusTab.xaml` |
| 12.7a | Create `LogTabViewModel` | `ViewModels/LogTabViewModel.cs` |
| 12.7b | Create `LogTab.xaml` | `Views/LogTab.xaml` |
| 12.8a | Create `SettingsTabViewModel` + `ScraperSettings` + `ControlMappingRule` | `ViewModels/SettingsTabViewModel.cs`, `Models/ScraperSettings.cs` |
| 12.8b | Create `SettingsTab.xaml` (full layout) | `Views/SettingsTab.xaml` |
| 12.9 | Rewire `MainWindow.xaml` to host Start/Workspace switch | `MainWindow.xaml`, `MainWindow.xaml.cs` |
| 12.10 | Rewire `App.xaml.cs` DI for new ViewModels | `App.xaml.cs` |
| 12.11 | Migrate `MainViewModel` orchestration into `WorkspaceViewModel` / tab VMs | `ViewModels/MainViewModel.cs` |
| 12.12 | Update tests for new ViewModel structure | `Brinell.Scraper.Tests/` |
| 12.13 | Build + test verification | — |

---

## Scope Notes

- Existing sub-ViewModels (`BrowserViewModel`, `InspectorViewModel`, `RecordingViewModel`, `SidebarViewModel`, `LogViewerViewModel`) are **reused** inside the tab VMs — not rewritten.
- `DomTreePanel`, `LogViewerPanel`, `BrowserView` UserControls are reused inside tabs.
- `MainViewModel` is gradually decomposed: scraping logic → `ScrapingTabViewModel`, view-switching → `WorkspaceViewModel`, site management → `StartPageViewModel`.
- The `DiffWindow` (Phase step 6) remains a standalone window opened from Corpus tab.
- `NewSiteDialog` is reused as-is from Start Page and Settings tab.
- The Control Objects tab and Page Objects tab are linked: navigating from a page object's "Used Control Objects" section can jump to the Control Objects tab.
- Page Objects tab reads from corpus (snapshots) and control object registry — it requires approved control objects before generating meaningful page objects.
