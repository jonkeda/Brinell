# Step 07a — Recording Mode & Sidebar Redesign

## Objective

Wire the existing `RecordingViewModel` to the UI so recording mode works end-to-end. Redesign the sidebar to show corpus pages at all times (not just during recording), and split into "This Session" / "Corpus" sections when recording is active.

## Dependencies

- Step 4.1 (DOM capture) — implemented
- Step 4.7 (`RecordingViewModel`) — implemented, wired to `MainViewModel`
- `CorpusDatabase` — implemented (sites, but no snapshot storage yet)
- `DomCaptureService` — implemented
- `BrowserViewModel.NavigationSucceeded` — implemented

---

## Current State

### What exists:
- `RecordingViewModel` — full state management: start/stop/pause/resume, `SessionSnapshots` collection, dedup logic, events
- `MainViewModel.ToggleRecording()` — toggles recording on/off
- `MainViewModel.OnNavigationSucceeded()` — auto-captures DOM on navigation when recording
- Toolbar ⏺ `ToggleButton` — toggles recording, shows checked state
- Status bar — shows `RecordingStatus` text in red when recording

### What's missing:
- Sidebar doesn't show corpus pages or session captures
- No visual recording indicator on the browser (red border)
- No pause/resume UI (only start/stop via toggle button)
- Sidebar is just two placeholder `ListView`s bound to `Sidebar.Pages` and `Sidebar.Controls` (both empty `ObservableCollection<string>`)
- No way to click a sidebar page to navigate to it
- Sidebar isn't visible without recording — it only shows when `HasActiveSite` is true, but its content is always empty

---

## UI Design

### Sidebar — Always Visible (When Site is Active)

The sidebar shows corpus information whenever a site is selected, regardless of recording state. It has two modes:

#### Normal Mode (Not Recording)

```
┌─────────────────────────┐
│ 📁 Exact Online         │
│ ─────────────────────── │
│ 47 pages · 12 controls  │
│                         │
│ ── Corpus Pages ─────── │
│ ✅ LoginPage            │
│ ✅ Dashboard            │
│ ✅ TimeEntry            │
│ ⏳ ProjectList          │
│ ⏳ Settings             │
│ ⏳ UserProfile          │
│                         │
│ ── Controls ──────────  │
│ ✅ DatePicker           │
│ ✅ NavigationMenu       │
│ ✅ DataGrid             │
│                         │
└─────────────────────────┘
```

| Icon | Meaning |
|------|---------|
| ✅ | Page has generated code |
| ⏳ | Page recorded but no code generated yet |
| (no icon) | Page in corpus but not yet recorded |

- Clicking a page navigates the browser to its URL
- The sidebar is always populated from `CorpusDatabase` when a site is selected

#### Recording Mode

```
┌─────────────────────────┐
│ 📁 Exact Online         │
│ ─────────────────────── │
│ 🔴 Recording            │
│ +3 new · 50 total       │
│                         │
│ ── This Session ──────  │
│ 🆕 SettingsPage         │
│ 🆕 UserProfile          │
│ 🆕 ReportPage           │
│                         │
│ ── Corpus Pages ─────── │
│ ✅ LoginPage            │
│ ✅ Dashboard            │
│ ✅ TimeEntry            │
│ ⏳ ProjectList          │
│                         │
│ ── Controls ──────────  │
│ ✅ DatePicker           │
│ ✅ NavigationMenu       │
│                         │
└─────────────────────────┘
```

- "This Session" section appears at the top, showing pages captured during the current recording
- 🆕 icon = new page captured this session
- Clicking a session page navigates the browser to that URL
- The "This Session" section disappears when recording stops (pages move to "Corpus Pages" after corpus storage is implemented)

### Recording Toolbar

When recording starts, the toolbar adapts:

```
Before:  ◀ ▶ ↻ [___address___] [Go]  |  🔍  ⏺  🔬  |  F12
                                            │
                                         (unchecked)

During:  ◀ ▶ ↻ [___address___] [Go]  |  🔍  ⏹ ⏸  🔬  |  F12
                                            │  │
                                         (stop) (pause)

Paused:  ◀ ▶ ↻ [___address___] [Go]  |  🔍  ⏹ ▶  🔬  |  F12
                                            │  │
                                         (stop) (resume)
```

| Button | Command | Visible When | Behavior |
|--------|---------|--------------|----------|
| ⏺ | `RecordCommand` | Not recording | Start recording |
| ⏹ | `StopRecordingCommand` | Recording | Stop recording |
| ⏸ | `PauseRecordingCommand` | Recording, not paused | Pause capture |
| ▶ | `ResumeRecordingCommand` | Recording, paused | Resume capture |

### Browser — Red Border During Recording

When recording is active, the `WebView2` control gets a red border to provide a clear visual indicator:

```
┌─────────────────────────────────────────────────┐
│ ╔═══════════════════════════════════════════════╗│
│ ║                                               ║│  ← 3px red border
│ ║            WebView2 Browser                   ║│
│ ║                                               ║│
│ ╚═══════════════════════════════════════════════╝│
└─────────────────────────────────────────────────┘
```

XAML approach:
```xml
<Border BorderThickness="3"
        BorderBrush="{Binding DataContext.Recording.IsRecording,
                      RelativeSource={RelativeSource AncestorType=Window},
                      Converter={StaticResource BoolToBrush},
                      ConverterParameter=Red}">
    <views:BrowserView x:Name="BrowserView"/>
</Border>
```

Or simpler — use a `Style.Trigger`:
```xml
<Border x:Name="BrowserBorder">
    <Border.Style>
        <Style TargetType="Border">
            <Setter Property="BorderThickness" Value="0"/>
            <Style.Triggers>
                <DataTrigger Binding="{Binding DataContext.Recording.IsRecording,
                             RelativeSource={RelativeSource AncestorType=Window}}" Value="True">
                    <Setter Property="BorderThickness" Value="3"/>
                    <Setter Property="BorderBrush" Value="Red"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Border.Style>
    <views:BrowserView x:Name="BrowserView"/>
</Border>
```

### Status Bar

```
│ Exact Online │ https://start.exactonline.nl/... │ 47 pages · 12 controls │ 🔴 +3 new │
```

When recording:
- Red dot + session count: `🔴 +3 new`
- When paused: `⏸ Paused`

When not recording but inspect is active:
- `4 selected │ DOM: 342 elements`

### Full Window Layout — Recording Mode

```
┌──────────────────┬───────────────────────────────────────────────────┐
│ 📁 Exact Online  │ ◀ ▶ ↻ [___address___________] Go │ 🔍 ⏹ ⏸ 🔬 │F12│
│ ─────────────── │─────────────────────────────────────────────────────│
│ 🔴 Recording     │ ╔═══════════════════════════════════════════════╗ │
│ +3 new · 50 total│ ║                                               ║ │
│                   │ ║                                               ║ │
│ ── This Session ─│ ║                                               ║ │
│ 🆕 SettingsPage  │ ║            WebView2 Browser                   ║ │
│ 🆕 UserProfile   │ ║          (red border = recording)             ║ │
│ 🆕 ReportPage    │ ║                                               ║ │
│                   │ ║                                               ║ │
│ ── Corpus ─────  │ ║                                               ║ │
│ ✅ LoginPage     │ ║                                               ║ │
│ ✅ Dashboard     │ ╚═══════════════════════════════════════════════╝ │
│ ✅ TimeEntry     │                                                   │
│ ⏳ ProjectList   │                                                   │
│                   │                                                   │
│ ── Controls ──── │                                                   │
│ ✅ DatePicker    │                                                   │
│ ✅ NavigationMenu│                                                   │
├──────────────────┴───────────────────────────────────────────────────┤
│ Exact Online │ https://example.com/settings │ 50 pages │ 🔴 +3 new  │
└──────────────────────────────────────────────────────────────────────┘
```

### Full Window Layout — Normal Mode (Not Recording)

```
┌──────────────────┬───────────────────────────────────────────────────┐
│ 📁 Exact Online  │ ◀ ▶ ↻ [___address___________] Go │ 🔍 ⏺ 🔬 │F12│
│ ─────────────── │─────────────────────────────────────────────────────│
│ 47 pages         │                                                   │
│ 12 controls      │                                                   │
│                   │                                                   │
│ ── Corpus ─────  │                                                   │
│ ✅ LoginPage     │              WebView2 Browser                     │
│ ✅ Dashboard     │                                                   │
│ ✅ TimeEntry     │                                                   │
│ ⏳ ProjectList   │                                                   │
│ ⏳ Settings      │                                                   │
│                   │                                                   │
│ ── Controls ──── │                                                   │
│ ✅ DatePicker    │                                                   │
│ ✅ NavigationMenu│                                                   │
│ ✅ DataGrid      │                                                   │
│                   │                                                   │
├──────────────────┴───────────────────────────────────────────────────┤
│ Exact Online │ https://example.com/ │ 47 pages · 12 controls        │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Implementation Plan

### Step 1 — Redesign `SidebarViewModel`

The current `SidebarViewModel` has empty `ObservableCollection<string>` for Pages and Controls. Replace with proper typed collections:

```csharp
public sealed class SidebarViewModel : ViewModelBase
{
    private string _corpusStats = "0 pages · 0 controls";
    private string _siteHeader = "";
    private bool _isRecording;

    public ObservableCollection<SidebarPageItem> CorpusPages { get; } = [];
    public ObservableCollection<SidebarPageItem> SessionPages { get; } = [];
    public ObservableCollection<string> Controls { get; } = [];

    public string CorpusStats { get; set; }
    public string SiteHeader { get; set; }

    public bool IsRecording
    {
        get => _isRecording;
        set => SetProperty(ref _isRecording, value);
    }

    public ICommand NavigateToPageCommand { get; }

    public void LoadCorpusPages(IEnumerable<SidebarPageItem> pages) { ... }
    public void AddSessionPage(DomSnapshot snapshot) { ... }
    public void ClearSession() { ... }
}

public sealed class SidebarPageItem
{
    public string Name { get; init; } = "";
    public string Url { get; init; } = "";
    public bool HasGeneratedCode { get; init; }
    public bool IsNewThisSession { get; init; }
}
```

### Step 2 — Populate Sidebar on Site Selection

In `MainViewModel.OnSiteSelected()`, load corpus pages from the database into the sidebar. (Currently the sidebar only gets `CorpusStats` but no page list.)

### Step 3 — Update Sidebar XAML

Replace the two placeholder `ListView`s with a proper layout:
- Header with site name + stats
- Recording indicator (visible when recording)
- "This Session" section (visible when recording, bound to `SessionPages`)
- "Corpus Pages" section (always visible, bound to `CorpusPages`)
- "Controls" section (always visible, bound to `Controls`)
- Each page item is clickable → navigates browser

### Step 4 — Toolbar Recording Controls

Replace the single ⏺ `ToggleButton` with three buttons:
- ⏺ Start (visible when not recording)
- ⏹ Stop (visible when recording)
- ⏸/▶ Pause/Resume (visible when recording)

### Step 5 — Red Border on Browser

Wrap `ContentControl` (browser area) in a `Border` with a `DataTrigger` on `Recording.IsRecording`.

### Step 6 — Wire Session Captures to Sidebar

In `MainViewModel.OnNavigationSucceeded()`, when recording auto-captures a snapshot, also add it to `Sidebar.SessionPages`.

### Step 7 — Recording Stop → Clear Session

When recording stops, clear `SessionPages`. (Once corpus storage is implemented in 4.8, pages will be persisted before clearing.)

---

## Data Flow

```
User clicks ⏺ Start
       │
       ▼
MainViewModel.ToggleRecording()
       │
       ├─ Recording.StartRecording()
       │      ├─ IsRecording = true
       │      ├─ SessionSnapshots.Clear()
       │      └─ RecordingStarted event
       │
       └─ Sidebar.IsRecording = true
              └─ "This Session" section appears in sidebar

User navigates to a new page
       │
       ▼
BrowserView.OnNavigationCompleted → BrowserViewModel.NavigationSucceeded
       │
       ▼
MainViewModel.OnNavigationSucceeded()
       │
       ├─ (if inspecting) re-inject overlay + re-capture
       │
       └─ (if recording)
              │
              ├─ DomCaptureService.CaptureAsync()
              ├─ Recording.OnPageTransition(url, snapshot)
              │      ├─ Dedup check (same URL < 2s → skip)
              │      ├─ SessionSnapshots.Add(snapshot)
              │      └─ RecordingStatus updated
              │
              └─ Sidebar.AddSessionPage(snapshot)
                     └─ 🆕 item appears in "This Session"

User clicks ⏹ Stop
       │
       ▼
MainViewModel.ToggleRecording()
       │
       ├─ Recording.StopRecording()
       │      ├─ IsRecording = false
       │      ├─ AnalyzePromptRequested event
       │      └─ RecordingStopped event
       │
       └─ Sidebar.IsRecording = false
              ├─ "This Session" section hides
              └─ Sidebar.ClearSession()
```

---

## Checklist

- [ ] `SidebarViewModel` redesigned with `CorpusPages`, `SessionPages`, `SidebarPageItem`
- [ ] Sidebar populated from corpus on site selection
- [ ] Sidebar always shows corpus pages (not just during recording)
- [ ] "This Session" section visible only during recording
- [ ] Clicking a sidebar page navigates the browser
- [ ] Toolbar shows ⏺ when not recording, ⏹+⏸ when recording
- [ ] Red border on browser during recording
- [ ] Session captures appear in sidebar in real-time
- [ ] Stop recording clears session section
- [ ] Status bar shows recording indicator
- [ ] Sidebar is resizable via GridSplitter
