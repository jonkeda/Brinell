# Step 12.W.9 — Wire SPA Page Transition Detection

## Objective

Wire the SPA-aware page transition detection so that during recording, the MutationObserver and URL-change listeners automatically trigger DOM captures even when the browser doesn't perform a traditional navigation (e.g., React/Angular/Vue/Blazor route changes).

## Dependencies

- `RecordingViewModel.IsRecording` — detection active only during recording
- `CoreWebView2.WebMessageReceived` — receives `pageTransition` messages from injected JS
- `DomCaptureService.CaptureAsync` — captures snapshot on transition
- `BrowserViewModel.NavigationSucceeded` — already handles traditional nav (this covers SPA nav)

## Implementation

### Files

| File | Action |
|------|--------|
| `Resources/spa-transition-detector.js` | Create — MutationObserver + URL watchers |
| `MainViewModel.cs` or `ScrapingTabViewModel.cs` | Inject script on recording start, handle `pageTransition` messages |
| `RecordingViewModel.cs` | Ensure `StopRecording` removes the observer |

### Code sketch

**spa-transition-detector.js:**

```javascript
(function() {
    if (window.__brinellSpaDetector) return; // already injected
    window.__brinellSpaDetector = true;

    let lastUrl = location.href;
    const MUTATION_THRESHOLD = 20; // elements
    const SETTLE_DELAY = 500; // ms
    let settleTimer = null;

    // MutationObserver — detect large DOM changes
    const observer = new MutationObserver((mutations) => {
        const totalChanged = mutations.reduce(
            (sum, m) => sum + m.addedNodes.length + m.removedNodes.length, 0);
        if (totalChanged > MUTATION_THRESHOLD) {
            scheduleTransition();
        }
    });
    observer.observe(document.body, { childList: true, subtree: true });

    // URL change detection
    window.addEventListener('popstate', () => checkUrlChange());
    window.addEventListener('hashchange', () => checkUrlChange());

    // pushState/replaceState interception
    const origPush = history.pushState;
    const origReplace = history.replaceState;
    history.pushState = function() {
        origPush.apply(this, arguments);
        checkUrlChange();
    };
    history.replaceState = function() {
        origReplace.apply(this, arguments);
        checkUrlChange();
    };

    function checkUrlChange() {
        if (location.href !== lastUrl) {
            lastUrl = location.href;
            scheduleTransition();
        }
    }

    function scheduleTransition() {
        clearTimeout(settleTimer);
        settleTimer = setTimeout(() => {
            // Check no pending fetches
            window.chrome.webview.postMessage({
                type: 'pageTransition',
                url: location.href,
                title: document.title
            });
        }, SETTLE_DELAY);
    }

    // Cleanup function
    window.__brinellSpaDetectorCleanup = () => {
        observer.disconnect();
        history.pushState = origPush;
        history.replaceState = origReplace;
        window.__brinellSpaDetector = false;
    };
})();
```

**MainViewModel.cs — inject on recording start:**

```csharp
private async void OnRecordingStarted()
{
    var webView = Browser.GetCoreWebView2?.Invoke();
    if (webView is null) return;

    await webView.ExecuteScriptAsync(SpaTransitionDetectorScript);
    webView.WebMessageReceived += OnSpaTransitionMessage;
}

private async void OnSpaTransitionMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
{
    var msg = JsonSerializer.Deserialize<JsMessage>(e.WebMessageAsJson);
    if (msg?.Type != "pageTransition") return;
    if (!Recording.IsRecording) return;

    _logger.LogDebug("SPA transition detected: {Url}", msg.Url);

    var webView = Browser.GetCoreWebView2?.Invoke();
    if (webView is null) return;

    var snapshot = await _domCapture.CaptureAsync(webView);
    snapshot.SiteName = ActiveSite?.Name ?? "";
    snapshot.PageName = msg.Title ?? snapshot.PageTitle;

    if (Recording.OnPageTransition(msg.Url, snapshot))
    {
        Sidebar.AddSessionPage(snapshot);
    }
}
```

**MainViewModel.cs — cleanup on recording stop:**

```csharp
private async void OnRecordingStopped()
{
    var webView = Browser.GetCoreWebView2?.Invoke();
    if (webView is null) return;

    await webView.ExecuteScriptAsync("window.__brinellSpaDetectorCleanup?.();");
    webView.WebMessageReceived -= OnSpaTransitionMessage;
}
```

### Manual fallback

A "Capture This State" toolbar button for SPAs where auto-detection fails:

```csharp
[RelayCommand]
private async Task CaptureCurrentState()
{
    var webView = Browser.GetCoreWebView2?.Invoke();
    if (webView is null) return;

    var snapshot = await _domCapture.CaptureAsync(webView);
    snapshot.SiteName = ActiveSite?.Name ?? "";
    snapshot.PageName = snapshot.PageTitle;

    if (Recording.IsRecording)
    {
        Recording.OnPageTransition(snapshot.PageUrl, snapshot);
        Sidebar.AddSessionPage(snapshot);
    }
}
```

## Checklist

- [ ] SPA transition detector JS injected when recording starts
- [ ] MutationObserver fires on large DOM changes (>20 elements)
- [ ] `pushState`/`replaceState` intercepted for URL changes
- [ ] `popstate` and `hashchange` events captured
- [ ] Stable-state wait (500ms settle) before triggering capture
- [ ] `WebMessageReceived` handler routes `pageTransition` to recording flow
- [ ] Observer cleaned up when recording stops
- [ ] Manual "Capture This State" button works as fallback
