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
| `MainViewModel.cs` | Wire `OnNavigationSucceeded` → add to sidebar, wire toggle |
| `SidebarViewModel.cs` | Add `AddSessionPage(DomSnapshot)` method |
| `MainWindow.xaml` | Wrap browser `ContentControl` in `Border` with recording DataTrigger |

### Code sketch

**SidebarViewModel.cs — session support:**

```csharp
public void AddSessionPage(DomSnapshot snapshot)
{
    // Dedup by URL
    if (SessionPages.Any(p => p.Url == snapshot.PageUrl))
        return;

    SessionPages.Insert(0, new SidebarPageItem
    {
        Name = snapshot.PageName,
        Url = snapshot.PageUrl,
        StatusIcon = "🆕"
    });
}
```

**MainViewModel.cs — OnNavigationSucceeded:**

```csharp
private async void OnNavigationSucceeded()
{
    if (!Recording.IsRecording) return;

    var webView = Browser.GetCoreWebView2?.Invoke();
    if (webView is null) return;

    var snapshot = await _domCapture.CaptureAsync(webView);
    snapshot.SiteName = ActiveSite?.Name ?? "";
    snapshot.PageName = snapshot.PageTitle;

    if (Recording.OnPageTransition(snapshot.PageUrl, snapshot))
    {
        Sidebar.AddSessionPage(snapshot);
    }
}
```

**MainViewModel.cs — ToggleRecording:**

```csharp
private void ToggleRecording()
{
    if (Recording.IsRecording)
    {
        Recording.StopRecording();
        Sidebar.ClearSession(); // clears SessionPages + IsRecording = false
    }
    else
    {
        Recording.StartRecording();
        Sidebar.IsRecording = true;
    }
}
```

**MainWindow.xaml — red border:**

```xml
<Border x:Name="BrowserBorder" Grid.Column="0">
    <Border.Style>
        <Style TargetType="Border">
            <Setter Property="BorderThickness" Value="3"/>
            <Setter Property="BorderBrush" Value="Transparent"/>
            <Style.Triggers>
                <DataTrigger Binding="{Binding Recording.IsRecording}" Value="True">
                    <Setter Property="BorderBrush" Value="Red"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Border.Style>
    <ContentControl x:Name="ContentArea"/>
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

## Checklist

- [ ] Starting recording sets `Sidebar.IsRecording = true`
- [ ] Each auto-captured page appears in "This Session" section
- [ ] Session pages show 🆕 icon
- [ ] Duplicate URLs are not added twice
- [ ] Red 3px border appears around browser when recording
- [ ] Stopping recording clears session pages and removes border
