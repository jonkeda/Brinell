# RCA-020: Corpus Page Count Not Updated After Manual Capture

**Reported:** 2026-05-04
**Severity:** Low
**Component:** `ViewModels/MainViewModel.cs` — `RecordPageAsync`

---

## Symptoms

When a page is captured via the 📷 Record Page button outside a recording session, the page appears in the "Corpus Pages" sidebar list, but the stats line still shows the old count (e.g. "0 pages · 0 controls"). The count only updates after a recording session stops or the app is restarted.

## Root Cause

`RecordPageAsync` adds the page to `Sidebar.CorpusPages` but never updates `Sidebar.CorpusStats` or `ActiveSite.PageCount`:

```csharp
Sidebar.CorpusPages.Add(new SidebarPageItem { ... });
// Missing: ActiveSite.PageCount++ and Sidebar.CorpusStats update
```

The `RecordingStopped` handler does update the stats, but it only runs when a recording session ends — not for manual captures.

## Fix

After adding a page to `CorpusPages` in `RecordPageAsync`, update the page count:

```csharp
if (ActiveSite is not null)
{
    ActiveSite.PageCount++;
    Sidebar.CorpusStats = $"{ActiveSite.PageCount} pages · {ActiveSite.ControlCount} controls";
}
```

## Verification

- [X] With "0 pages · 0 controls" showing, click 📷 Record Page. Stats update to "1 pages · 0 controls".
- [X] Click 📷 on two more pages. Stats show "3 pages · 0 controls".
- [X] Start and stop a recording session. Stats correctly reflect all pages (manual + session).
