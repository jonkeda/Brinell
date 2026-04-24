# Step 1.4 — Navigation Support

## Objective

Wire up WebView2 navigation events so the address bar stays in sync, status updates during navigation, and errors are handled gracefully.

## Dependencies

- Step 1.3 (WebView2 control exists)

## Implementation

Subscribe to WebView2 navigation events:

```csharp
webView.CoreWebView2.NavigationStarting += OnNavigationStarting;
webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
webView.CoreWebView2.SourceChanged += OnSourceChanged;
```

### Event handlers

- **`NavigationStarting`** — update status bar ("Navigating to ..."), show loading indicator, validate URL
- **`NavigationCompleted`** — hide loading indicator, update status bar with result, handle navigation errors (`e.IsSuccess`, `e.WebErrorStatus`)
- **`SourceChanged`** — sync address bar `TextBox` with the current URL so it reflects redirects and in-page navigations

### Address bar behavior

- User types a URL and presses Enter → navigate to that URL
- User clicks Go button → same
- After navigation completes (including redirects), address bar updates to reflect the actual final URL

### Error handling

- Check `e.IsSuccess` and `e.WebErrorStatus` in `NavigationCompleted`
- Display error status in the status bar (e.g. "Navigation failed: ConnectionAborted")
- No crash on invalid URLs or network failures

## Checklist

- [ ] Typing a URL and pressing Enter navigates the browser
- [ ] Address bar updates to reflect current URL after redirects
- [ ] Status bar shows "Navigating to ..." during load
- [ ] Navigation errors display in status bar without crashing
- [ ] Back/Forward/Refresh buttons work after navigating to multiple pages
