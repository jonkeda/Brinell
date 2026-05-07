# Step 12.3 — Scraping Tab

## Objective

Migrate the existing browser + inspector + recording workflow into a self-contained Scraping tab. Sidebar content (session pages, corpus pages list) is absorbed as a collapsible left panel inside this tab. The log viewer is removed (it has its own tab).

## Dependencies

- Step 12.2 (Workspace shell)
- Existing `BrowserViewModel`, `InspectorViewModel`, `RecordingViewModel`, `SessionPanelViewModel`

## Implementation

### Files

- `Views/Tabs/ScrapingTabView.xaml` (UserControl)
- `ViewModels/ScrapingTabViewModel.cs`

### `ScrapingTabViewModel`

```csharp
public class ScrapingTabViewModel : ViewModelBase
{
    public BrowserViewModel Browser { get; }
    public InspectorViewModel Inspector { get; }
    public RecordingViewModel Recording { get; }
    public SessionPanelViewModel Session { get; }

    public bool IsSessionPanelVisible { get; set; } = true;
    public bool IsInspectorVisible => Inspector.IsActive;

    public ICommand ToggleSessionPanelCommand { get; }
}
```

### Layout

```
DockPanel
├─ Toolbar (Top): nav (◀▶↻), URL bar, Go, separator,
│                 ⏺ Record / ⏸ / ⏹, 🔍 Inspect, 📷 Capture
├─ Tab status bar (Bottom): Status | Pages | Elements | Recording
└─ Grid (fill)
    ├─ Col0: Session panel (240px, collapsible)
    │       - "This Session" ListView (Session.RecordedPages) + [Analyze]
    │       - "Corpus" ListView (Session.CorpusPages)
    ├─ Col1: WebView2 (Browser, fill)
    └─ Col2: Inspector (300px, visible when IsInspectorVisible)
            - Form / Inputs / Cleared filter buttons
            - Control groups list
            - DOM tree
            - Selected count
```

- Use `GridSplitter` between columns.
- Inspector column collapses (`Width=0`) when `IsInspectorVisible=false`.
- Session panel toggle hides Col0.

### Behavior

- All existing browser/inspector/recording logic stays unchanged — this is a composition wrapper.
- URL bar binds to `Browser.AddressUrl` with `KeyDown=Enter → NavigateCommand`.
- Log writes still flow to `LogViewerViewModel` (visible in the Log tab).

## Checklist

- [ ] `ScrapingTabView` is a UserControl, not a Window
- [ ] Toolbar contains navigation + recording + inspect + capture buttons
- [ ] Session panel hosts session + corpus page lists; collapsible
- [ ] Inspector panel only visible when active
- [ ] No log viewer in this tab
- [ ] Tab-local status bar shows scraping stats only
- [ ] Existing VM behavior preserved (no regressions)
