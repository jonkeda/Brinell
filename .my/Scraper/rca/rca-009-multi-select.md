# RCA-009: Multi-Select Mode — Selection Broken, Missing UI Buttons

**Reported:** 2026-04-22
**Severity:** High
**Component:** `Services/ElementHighlightService.cs`, `MainWindow.xaml`, `ViewModels/MainViewModel.cs`

---

## Symptoms

1. **Multi-select worked once then stopped** — Ctrl+click selected an element once, but subsequent Ctrl+clicks no longer work.
2. **No "Select All Forms" button** visible in the UI.
3. **Status bar doesn't show selection count.**
4. **No "Clear Selection" button** visible in the UI.
5. **No "Select All Inputs" button** visible in the UI.

## Root Cause

### Issue 1 — Ctrl+Click Selection Stops After Re-Inspect

The `ElementHighlightService.OverlayScript` has a guard at the top:

**File:** `Services/ElementHighlightService.cs`, line 48
```javascript
if (window.__brinellOverlay) return;
```

When the user toggles inspect off and back on, `DisableAsync` sets `window.__brinellOverlay = false` and removes the DOM elements, but `EnableAsync` runs `ExecuteScriptAsync(OverlayScript)` again. **If the page was navigated or reloaded between toggles**, the script re-injects correctly. But if the user simply toggles inspect off/on on the same page without navigating, `__brinellOverlay` was set to `false` by `RemoveOverlayScript`, so re-injection should work.

**More likely cause:** The Ctrl+click handler uses `window.chrome.webview.postMessage()` to send selection events to C#, but **there is no `WebMessageReceived` handler registered on the C# side**. The JS fires the message, but nothing in `BrowserView.xaml.cs` or `MainViewModel` listens for it.

**File:** `Services/ElementHighlightService.cs`, lines 160–170
```javascript
window.chrome.webview.postMessage(JSON.stringify({
    type: 'elementSelected',
    tag: el.tagName.toLowerCase(),
    id: el.id || null,
    dataTestId: el.getAttribute('data-testid'),
    ...
}));
```

**File:** `Views/BrowserView.xaml.cs` — no `WebMessageReceived` subscription exists.

So the green border visual toggle works in the browser (via direct DOM manipulation in JS), but the C# `InspectorViewModel` never receives the selection event. The `SelectedElements` collection is never updated, so `SelectedCount` stays at 0.

The "worked once" observation may be because the first Ctrl+click visually applied the green border, but since no C# state update happened, subsequent UI interactions (like toggling inspect or re-capturing) cleared the visual state without the model ever knowing about it.

### Issues 2, 4, 5 — Inspector Action Buttons Not in the UI

The `InspectorViewModel` has commands for `SelectAllFormsCommand`, `SelectAllInputsCommand`, and `ClearSelectionCommand`, but **no UI elements bind to them**. The `DomTreePanel.xaml` only contains a filter bar and a TreeView — no action buttons.

The `MainWindow.xaml` inspector area only shows the `DomTreePanel`:
```xml
<views:DomTreePanel Grid.Column="2" Width="300"
                    DataContext="{Binding Inspector.DomTree}" .../>
```

There are no buttons for Select All Forms, Select All Inputs, or Clear Selection anywhere in the XAML.

### Issue 3 — Status Bar Doesn't Show Selection Count

The status bar in `MainWindow.xaml` shows `Sidebar.CorpusStats` but has no binding for `Inspector.SelectedCount` or `Inspector.TotalElementCount`:

```xml
<StatusBarItem>
    <TextBlock Text="{Binding Sidebar.CorpusStats}"/>
</StatusBarItem>
```

There is no status bar item for selection info like "12 selected │ DOM: 342 elements".

## Fix

### Issue 1 — Add WebMessageReceived Handler

In `BrowserView.xaml.cs`, subscribe to `WebMessageReceived` during initialization:

```csharp
WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
{
    try
    {
        var json = e.WebMessageAsJson;
        var msg = JsonSerializer.Deserialize<WebViewMessage>(json);
        if (msg?.Type == "elementSelected")
        {
            // Notify MainViewModel/InspectorViewModel about the selection
            _vm?.OnElementSelected(msg);
        }
    }
    catch { /* ignore malformed messages */ }
}
```

Add `OnElementSelected` to `BrowserViewModel` or use an event:
```csharp
public event Action<WebViewMessage>? ElementSelected;

public void OnElementSelected(WebViewMessage msg) 
    => ElementSelected?.Invoke(msg);
```

In `MainViewModel`, subscribe and toggle the element in the inspector:
```csharp
Browser.ElementSelected += msg =>
{
    // Find the matching DomElement in the snapshot and toggle it
    var element = FindElement(Inspector.Snapshot, msg.Id, msg.DataTestId, msg.BoundingBox);
    if (element is not null)
        Inspector.ToggleElement(element);
};
```

### Issues 2, 4, 5 — Add Inspector Action Buttons

Add a toolbar or button panel to the inspector area in `MainWindow.xaml`:

```xml
<!-- Inspector panel with action buttons -->
<DockPanel Grid.Column="2" Width="300"
           Visibility="{Binding DataContext.Inspector.IsInspecting, ...}">
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="4">
        <Button Content="Select All Forms" Command="{Binding DataContext.Inspector.SelectAllFormsCommand, 
                RelativeSource={RelativeSource AncestorType=Window}}"
                Padding="4,2" Margin="0,0,4,0" FontSize="11"/>
        <Button Content="Select Inputs" Command="{Binding DataContext.Inspector.SelectAllInputsCommand,
                RelativeSource={RelativeSource AncestorType=Window}}"
                Padding="4,2" Margin="0,0,4,0" FontSize="11"/>
        <Button Content="Clear" Command="{Binding DataContext.Inspector.ClearSelectionCommand,
                RelativeSource={RelativeSource AncestorType=Window}}"
                Padding="4,2" FontSize="11"/>
    </StackPanel>
    <views:DomTreePanel DataContext="{Binding DataContext.Inspector.DomTree,
                        RelativeSource={RelativeSource AncestorType=Window}}"/>
</DockPanel>
```

### Issue 3 — Add Selection Count to Status Bar

Add a status bar item that shows when inspecting:

```xml
<StatusBarItem Visibility="{Binding Inspector.IsInspecting, Converter={StaticResource BoolToVisibility}}">
    <TextBlock>
        <Run Text="{Binding Inspector.SelectedCount, Mode=OneWay}"/>
        <Run Text=" selected │ DOM: "/>
        <Run Text="{Binding Inspector.TotalElementCount, Mode=OneWay}"/>
        <Run Text=" elements"/>
    </TextBlock>
</StatusBarItem>
```

## Status

- [ ] `WebMessageReceived` handler added to BrowserView
- [ ] Selection messages routed to InspectorViewModel.ToggleElement
- [ ] "Select All Forms" button added to inspector panel
- [ ] "Select All Inputs" button added to inspector panel
- [ ] "Clear Selection" button added to inspector panel
- [ ] Selection count shown in status bar
- [ ] Verified Ctrl+click updates both browser overlay and ViewModel state
