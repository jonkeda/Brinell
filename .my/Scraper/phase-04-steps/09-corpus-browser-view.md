# Step 4.9 — Corpus Browser View

## Objective

UI for browsing all recorded snapshots with metadata, filtering, and action buttons.

## Dependencies

- Step 4.8 (CorpusService for data)
- Phase 1 (sidebar + content area layout)

## Implementation

### DataGrid layout

| Column | Description |
|--------|-------------|
| Page Name | Name of the captured page |
| URL | Page URL |
| Recorded | Date of last recording |
| Elements | Element count |
| Gen | Generation status icon (✅ ⚠️ ⏳) |

- Sort by any column.
- Text search filter at top.
- Re-recorded pages show history count inline.

### Selection detail panel

When a page is selected, show below the grid:
- Snapshot count (history)
- Element count change between versions
- Action buttons: View Snapshot, View Diff, Re-record, Delete Page, Regenerate Code

### Navigation

Accessible via `Site → Browse Corpus` menu item.

## Checklist

- [ ] DataGrid displays all recorded pages with metadata columns
- [ ] Sort by any column works
- [ ] Text search filter narrows displayed pages
- [ ] Selecting a page shows snapshot history and element count
- [ ] Action buttons: View Snapshot, View Diff, Re-record, Delete Page, Regenerate Code
