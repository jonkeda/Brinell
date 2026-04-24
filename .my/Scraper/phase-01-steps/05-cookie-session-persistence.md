# Step 1.5 — Cookie / Session Persistence

## Objective

Ensure cookies, localStorage, and session state survive app restarts so users stay logged in across sessions.

## Dependencies

- Step 1.3 (WebView2 initialized)

## Implementation

Create a `CoreWebView2Environment` with a custom user data folder:

```csharp
var userDataFolder = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Brinell.Scraper", "WebView2Data");

var environment = await CoreWebView2Environment.CreateAsync(
    browserExecutableFolder: null,
    userDataFolder: userDataFolder);

await webView.EnsureCoreWebView2Async(environment);
```

### Key points

- The user data folder persists cookies, cache, IndexedDB, etc.
- On application exit, do **not** clear the user data folder (preserve sessions)
- Optionally expose a "Clear Session Data" menu item that deletes the user data folder contents

### User data folder location

```
%LOCALAPPDATA%\Brinell.Scraper\WebView2Data\
```

## Checklist

- [ ] WebView2 environment uses custom user data folder
- [ ] Logging into a site, closing the app, and reopening keeps the session
- [ ] User data folder is created on first launch
- [ ] (Optional) "Clear Session Data" menu item works
