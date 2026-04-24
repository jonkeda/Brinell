# Step 07a-2 — Populate Sidebar on Site Selection

## Objective

When a site is selected, load its corpus pages into the sidebar so the user always sees what pages exist — regardless of whether recording is active.

## Current State

In `MainViewModel.OnSiteSelected()`:
```csharp
private void OnSiteSelected(SiteInfo site)
{
    ActiveSite = site;
    SiteName = site.Name;
    WindowTitle = $"Brinell Scraper — {site.Name}";
    Browser.AddressUrl = site.StartUrl;
    Sidebar.CorpusStats = $"{site.PageCount} pages · {site.ControlCount} controls";
    BrowserViewRequested?.Invoke();
}
```

The sidebar gets `CorpusStats` set, but no pages are loaded. `CorpusDatabase` currently has `GetAllSites()` but no method to get pages for a site (snapshot storage is Step 4.8, not yet implemented).

## Changes

### 1. Update `MainViewModel.OnSiteSelected()`

Set the sidebar header and clear any stale data:

```csharp
private void OnSiteSelected(SiteInfo site)
{
    ActiveSite = site;
    SiteName = site.Name;
    WindowTitle = $"Brinell Scraper — {site.Name}";
    Browser.AddressUrl = site.StartUrl;

    Sidebar.SiteHeader = site.Name;
    Sidebar.CorpusStats = $"{site.PageCount} pages · {site.ControlCount} controls";
    Sidebar.ClearSession();
    // Corpus page loading will be added when snapshot storage (4.8) is implemented.
    // For now the CorpusPages collection stays empty — that's fine, the sidebar
    // still shows the header, stats, and session pages during recording.

    BrowserViewRequested?.Invoke();
}
```

### 2. Placeholder for future corpus page loading

When Step 4.8 (SQLite corpus store) is implemented, this method will also call:
```csharp
var pages = _db.GetPagesForSite(site.Id);
Sidebar.LoadCorpusPages(pages.Select(p => new SidebarPageItem
{
    Name = p.PageName,
    Url = p.PageUrl,
    StatusIcon = p.HasGeneratedCode ? "✅" : "⏳"
}));
```

This step is documented here so the wiring is understood, but the actual `GetPagesForSite` method doesn't exist yet.

## Files Modified

| File | Action |
|------|--------|
| `ViewModels/MainViewModel.cs` | **Edit** — update `OnSiteSelected` |

## Verification

- Build succeeds
- Sidebar shows site name in header when a site is selected
- `CorpusPages` is empty (expected until 4.8)
- Switching sites clears any previous session data

## Checklist

- [ ] `OnSiteSelected` sets `SiteHeader`
- [ ] `OnSiteSelected` calls `ClearSession()` to reset stale recording state
- [ ] Build succeeds, tests pass
