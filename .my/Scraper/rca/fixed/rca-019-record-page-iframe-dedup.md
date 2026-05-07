# RCA-019: Record Page Button Ignores IFrame Changes When Page URL Is Same

**Reported:** 2026-05-04
**Severity:** Medium
**Component:** `ViewModels/MainViewModel.cs` — `RecordPageAsync`

---

## Symptoms

When the user clicks the 📷 Record Page button outside a recording session:

1. If the page contains a cross-origin iframe, the iframe DOM is not captured (fixed by RCA-016 for same-session captures, but the outside-session path has additional issues).
2. If the top-level page URL is the same as an already-recorded corpus page but the iframe content has changed (e.g. navigating within an embedded app), the user is prompted "This page is already recorded. Overwrite?" — which is confusing because from the user's perspective it's a different page (the iframe content changed).

The overwrite prompt should not appear when the iframe content differs. Instead, the page should be recorded as a new entry reflecting the iframe's current state.

## Root Cause

`RecordPageAsync` compares pages solely by top-level URL:

```csharp
var existing = Sidebar.CorpusPages.FirstOrDefault(p =>
    string.Equals(p.Url, snapshot.PageUrl, StringComparison.OrdinalIgnoreCase));
```

When the top-level URL stays the same but iframe content changes (common in embedded apps like ExactOnline), this check treats it as a duplicate. The user must click "Yes" to overwrite every time, losing the previous iframe state.

## Fix

1. When the top-level URL matches an existing corpus page, check whether the iframe content has changed by comparing the iframe `FrameSource` URLs or a hash of the iframe DOM.
2. If the iframe content differs, auto-record without prompting and use a page name that includes the iframe context, e.g. `"Page Title — [iframe: Exact Hours Registration]"`.
3. If both the top-level URL and all iframe content are identical, then prompt for overwrite as before.

## Verification

- [X] Navigate to a page with an iframe. Click 📷. Page is recorded in corpus.
- [X] The iframe navigates to a different page (top-level URL unchanged). Click 📷 again. A new entry is added to corpus without an overwrite prompt.
- [X] Navigate the iframe back to the original content. Click 📷. Now the overwrite prompt appears (content matches existing entry).
