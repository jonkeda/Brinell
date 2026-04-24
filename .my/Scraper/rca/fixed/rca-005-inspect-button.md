# RCA-005: Inspect Button Does Nothing

**Reported:** 2026-04-22
**Severity:** High
**Component:** `ViewModels/MainViewModel.cs`

---

## Symptoms

Clicking the 🔍 Inspect button in the toolbar does nothing. No DOM capture occurs, no highlight overlay appears, and no log entry is generated.

## Root Cause

The `InspectCommand` in `MainViewModel` is a **no-op stub**. It was created as a placeholder during Phase 1 and was never wired to the Phase 4 inspector logic.

**File:** `ViewModels/MainViewModel.cs`, line 33
```csharp
InspectCommand = new RelayCommand(() => { }, () => HasActiveSite);
```

The execute action is an empty lambda `() => { }`. Phase 4 implemented `InspectorViewModel`, `DomCaptureService`, and `ElementHighlightService` as standalone services, but they were never connected to the toolbar button.

The same is true for `RecordCommand` and `AnalyzeCommand` — all three are empty stubs.

## What Needs to Happen When Inspect Is Clicked

1. Toggle `InspectorViewModel.IsInspecting` on/off
2. If turning on:
   a. Call `DomCaptureService.CaptureAsync(webView)` to capture the current page DOM
   b. Load the snapshot into `InspectorViewModel.LoadSnapshot(snapshot)`
   c. Call `ElementHighlightService.EnableAsync(webView)` to inject the hover/click overlay JS
   d. Show the DOM tree panel in the sidebar or a docked panel
3. If turning off:
   a. Call `ElementHighlightService.DisableAsync(webView)` to remove overlays
   b. Hide the DOM tree panel

## Fix

### 1. Inject Services into MainViewModel

```csharp
public MainViewModel(
    CorpusDatabase db,
    BrowserViewModel browser,
    SidebarViewModel sidebar,
    SiteSelectionViewModel siteSelection,
    InspectorViewModel inspector,
    DomCaptureService domCapture,
    ElementHighlightService highlight,
    ILogger<MainViewModel> logger)
```

### 2. Replace InspectCommand with Real Implementation

```csharp
InspectCommand = new AsyncRelayCommand(ToggleInspectAsync, () => HasActiveSite);

private async Task ToggleInspectAsync(CancellationToken ct)
{
    var webView = Browser.CoreWebView2;
    if (webView is null) return;

    if (Inspector.IsInspecting)
    {
        await _highlight.DisableAsync(webView);
        Inspector.IsInspecting = false;
        // Hide inspector panel
    }
    else
    {
        var snapshot = await _domCapture.CaptureAsync(webView);
        Inspector.LoadSnapshot(snapshot);
        await _highlight.EnableAsync(webView);
        Inspector.IsInspecting = true;
        // Show inspector panel
    }
}
```

### 3. Expose CoreWebView2 from BrowserViewModel

The `BrowserViewModel` needs to expose the underlying `CoreWebView2` instance so `MainViewModel` can pass it to the capture and highlight services. This may require adding a property or accessor.

### 4. Wire Inspector UI

Add a `ContentControl` or panel in `MainWindow.xaml` that becomes visible when `Inspector.IsInspecting` is true, showing the `DomTreePanel` and selection controls.

## Affected Tests

- `MainViewModelTests` — existing tests only verify command `CanExecute` states
- **New tests needed:**
  - InspectCommand toggles `IsInspecting` state
  - InspectCommand calls capture service
  - InspectCommand enables/disables highlight service

## Status

- [ ] Services injected into MainViewModel
- [ ] InspectCommand wired to capture + highlight services
- [ ] CoreWebView2 exposed from BrowserViewModel
- [ ] Inspector UI panel shown/hidden on toggle
- [ ] RecordCommand wired (same pattern)
- [ ] Unit tests for inspect toggle flow
