# Step 12.W.7a — Wire Inspect Toggle & Element Overlay

## Objective

Wire the 🔍 Inspect toolbar button to inject/remove the highlight overlay JavaScript into WebView2, so hovering elements in the browser shows a blue highlight with locator tooltip.

## Dependencies

- `BrowserViewModel.IsInspectMode` (toggle property, exists or add)
- `DomCaptureService` (already implemented)
- `CoreWebView2.ExecuteScriptAsync` for JS injection
- Phase-04 Step 4.2 overlay JS (already written)

## Implementation

### Files

| File | Action |
|------|--------|
| `BrowserViewModel.cs` | Add `IsInspectMode` property, `ToggleInspectCommand` |
| `MainViewModel.cs` or `ScrapingTabViewModel.cs` | Subscribe `IsInspectMode` change → inject/remove overlay JS |
| `Resources/inspect-overlay.js` | Embedded resource — overlay script from phase-04 step 4.2 |

### Code sketch

**BrowserViewModel.cs:**

```csharp
[ObservableProperty]
private bool _isInspectMode;

[RelayCommand]
private void ToggleInspect()
{
    IsInspectMode = !IsInspectMode;
}

partial void OnIsInspectModeChanged(bool value)
{
    InspectModeChanged?.Invoke(value);
}

public event Action<bool>? InspectModeChanged;
```

**ScrapingTabViewModel.cs** (or MainViewModel):

```csharp
Browser.InspectModeChanged += async (enabled) =>
{
    var webView = Browser.GetCoreWebView2?.Invoke();
    if (webView is null) return;

    if (enabled)
    {
        await webView.ExecuteScriptAsync(InspectOverlayScript.Inject);
    }
    else
    {
        await webView.ExecuteScriptAsync(InspectOverlayScript.Remove);
    }
};
```

### Overlay behavior

- Blue border + light blue background on hovered element
- Tooltip below element: `tag#id .class — Suggested: Locator.ByXxx("...")`
- Green border for selected elements (from multi-select, step 07c)
- Overlay div is MutationObserver-safe (does not trigger app MutationObservers)

## Checklist

- [ ] 🔍 Inspect button toggles `IsInspectMode`
- [ ] Overlay JS injected on enable, removed on disable
- [ ] Hovering elements shows blue highlight
- [ ] Tooltip shows tag, id, aria-label, suggested locator
- [ ] Overlay does not interfere with page functionality
