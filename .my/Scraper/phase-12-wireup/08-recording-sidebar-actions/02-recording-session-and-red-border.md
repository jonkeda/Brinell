# Step 12.W.8b — Wire Recording Session to Sidebar

## Objective

Wire the recording session so that when pages are captured during recording, they appear in the sidebar's "This Session" section in real-time. Show session count, toggle recording state display, and add a red border around the browser.

## Dependencies

- `SidebarViewModel` (from step 08a) — `SessionPages`, `IsRecording`
- `RecordingViewModel.IsRecording`, `SessionSnapshots`
- `DomCaptureService.CaptureAsync(webView)`
- `BrowserViewModel.NavigationSucceeded` event

## Implementation

### Files

| File | Action |
|------|--------|
| `MainViewModel.cs` | Wire navigation + SPA + iframe transition captures into recording/session flow |
| `SidebarViewModel.cs` | Add `AddSessionPage(DomSnapshot)` method |
| `Views/BrowserView.xaml.cs` | Track `CoreWebView2.FrameCreated` and iframe `NavigationCompleted` |
| `Views/Tabs/ScrapingTabView.xaml` | Add browser `Border` with `Recording.IsRecording` DataTrigger |

### Code sketch

**SidebarViewModel.cs — session support:**

```csharp
public void AddSessionPage(DomSnapshot snapshot)
{
    SessionPages.Add(new SidebarPageItem
    {
        Name = snapshot.PageName,
        Url = snapshot.PageUrl,
        StatusIcon = "🆕"
    });
}
```

Note: dedup is handled in `RecordingViewModel.OnPageTransition(...)` (same URL within a 2-second window), not in `SidebarViewModel`.

**MainViewModel.cs — top-level navigation capture:**

```csharp
private async void OnNavigationSucceeded()
{
    if (!Recording.IsRecording) return;

    var webView = Browser.GetCoreWebView2?.Invoke();
    if (webView is null) return;

    var snapshot = await _domCapture.CaptureAsync(webView, _highlight.TrackedFrames);
    snapshot.SiteName = ActiveSite?.Name ?? "";
    snapshot.PageName = snapshot.PageTitle;

    if (Recording.OnPageTransition(snapshot.PageUrl, snapshot))
        Sidebar.AddSessionPage(snapshot);
}
```

**MainViewModel.cs — iframe transition capture:**

```csharp
private async void OnIFrameNavigationSucceeded()
{
    if (!Recording.IsRecording) return;

    var webView = Browser.GetCoreWebView2?.Invoke();
    if (webView is null) return;

    var snapshot = await _domCapture.CaptureAsync(webView, _highlight.TrackedFrames);
    snapshot.SiteName = ActiveSite?.Name ?? "";
    snapshot.PageName = $"[iframe] {snapshot.PageTitle}";

    if (Recording.OnPageTransition(snapshot.PageUrl, snapshot))
        Sidebar.AddSessionPage(snapshot);
}
```

**BrowserView.xaml.cs — iframe nav event source:**

```csharp
private void OnFrameCreated(object? sender, CoreWebView2FrameCreatedEventArgs e)
{
    var frame = e.Frame;
    frame.NavigationCompleted += (_, args) =>
    {
        if (args.IsSuccess)
            _vm?.OnIFrameNavigationCompleted();
    };
}
```

**MainViewModel.cs — recording toggle (implemented):**

```csharp
private void ToggleRecording()
{
    if (Recording.IsRecording)
        Recording.StopRecording();
    else
        Recording.StartRecording();
}
```

Session clearing is intentionally handled later in the analyze/transfer flow, not immediately on stop.

**Views/Tabs/ScrapingTabView.xaml — red border:**

```xml
<Border Grid.Column="2">
    <Border.Style>
        <Style TargetType="Border">
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="BorderBrush" Value="Transparent"/>
            <Style.Triggers>
                <DataTrigger Binding="{Binding Recording.IsRecording}" Value="True">
                    <Setter Property="BorderThickness" Value="3"/>
                    <Setter Property="BorderBrush" Value="Red"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Border.Style>
    <views:BrowserView x:Name="BrowserHost"/>
</Border>
```

### Sidebar recording mode layout

```
┌─────────────────────────┐
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
│ ⏳ ProjectList          │
└─────────────────────────┘
```

## IFrame Validation

Iframe transitions are correctly captured in the current implementation.

- Same-origin iframe content is traversed directly in `DomCaptureService` (`contentDocument`).
- Cross-origin iframe content is captured via tracked `CoreWebView2Frame.ExecuteScriptAsync(...)` and merged into the parent DOM tree.
- Iframe navigations trigger recording through `FrameCreated` + `NavigationCompleted`, then surface in session list as `[iframe] {PageTitle}`.

## Learned Notes (from previous implementation)

- Pass `_highlight.TrackedFrames` to every capture path (top-level nav, iframe nav, SPA transitions, manual record, inspect refresh) to avoid partial iframe snapshots.
- Keep dedup in one place (`RecordingViewModel`) to avoid inconsistent behavior between sidebar/UI and recorder logic.
- Use URL+time dedup conservatively; with iframe-heavy apps, very fast transitions inside the same top-level URL may be intentionally collapsed by the 2-second window.

## Checklist

- [x] Starting recording sets `Sidebar.IsRecording = true`
- [x] Each auto-captured page appears in "This Session" section
- [x] Session pages show 🆕 icon
- [x] Duplicate URLs are filtered by recording dedup window
- [x] Red 3px border appears around browser when recording
- [ ] Stopping recording clears session pages (moved to analyze flow instead)
