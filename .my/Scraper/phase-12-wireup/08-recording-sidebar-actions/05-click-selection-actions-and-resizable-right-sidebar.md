# Step 12.W.8d - Click Selection Actions and Resizable Right Sidebar

## Objective

Update sidebar interaction behavior so list item clicks only select items (no immediate navigation), add explicit action buttons for navigation/removal, and fix scraper-tab right sidebar behavior so it is resizable and not empty when inspector is hidden.

## Change Scope

### 1) Corpus Pages list behavior change

Current behavior:
- Clicking a corpus page item navigates immediately.

Required behavior:
- Clicking a corpus page item only selects that corpus page.
- Add explicit action button to navigate to selected corpus page.
- Add explicit action button to remove selected corpus page.
- Item click must never cause cross-site navigation by itself.

### 2) Recordings list behavior change

Current behavior:
- Clicking a recording item navigates immediately.

Required behavior:
- Clicking a recording item only selects that recording.
- Add explicit action button to navigate to selected recording.
- Add explicit action button to remove selected recording.
- Add clear-all button to remove all recordings from the recording session list.
- Item click must never cause navigation by itself.

### 3) Scraper tab right sidebar usability fixes

Current behavior:
- Right sidebar cannot be resized.
- Right sidebar appears empty when inspector is not visible.

Required behavior:
- Right sidebar is user-resizable on the scraper tab.
- Right sidebar shows useful default content when inspector is hidden.
- Sidebar content should preserve context (selected corpus page and selected recording) so actions remain available even if inspector panel is collapsed/hidden.

## UX Design

### Selection model

- `CorpusPages` and `RecordedPages` (or `SessionPages`) each keep independent selection state.
- Selected state is visually distinct.
- Single click selects only.
- Double click is optional; if enabled, it may map to Navigate command but must stay same-site guarded.

### Action groups

For selected corpus page:
- `Navigate` button
- `Remove` button

For selected recording:
- `Navigate` button
- `Remove` button
- `Clear All` button (list-level action)

Action button enablement:
- `Navigate` and `Remove` enabled only when an item is selected.
- `Clear All` enabled only when recordings list has at least one item.

### Empty-state behavior for right sidebar

When inspector is hidden:
- Show a `Sidebar Actions` panel with:
  - current site name
  - selected corpus page summary (or `No corpus page selected`)
  - selected recording summary (or `No recording selected`)
  - action buttons relevant to current selection
- Show contextual hint text instead of blank area.

## Technical Design

### ViewModel contract updates

Add/update in sidebar/session panel view model:
- `SelectedCorpusPage : SidebarPageItem?`
- `SelectedRecordingPage : SidebarPageItem?`
- `NavigateSelectedCorpusCommand`
- `RemoveSelectedCorpusCommand`
- `NavigateSelectedRecordingCommand`
- `RemoveSelectedRecordingCommand`
- `ClearRecordingsCommand`
- `CanNavigateSelectedCorpus`, `CanRemoveSelectedCorpus`
- `CanNavigateSelectedRecording`, `CanRemoveSelectedRecording`, `CanClearRecordings`

Replace direct item-click navigation wiring:
- remove click handler that invokes navigate directly
- row click now updates selected item only

### Navigation safety guard

For both navigate commands:
- Use explicit command invocation only.
- Validate URL/site context before navigating.
- Block unintended cross-site jump unless user explicitly confirms (if policy requires); default behavior should keep interaction within active site workflow.

### Remove actions

`RemoveSelectedCorpusCommand`:
- Remove selected corpus page from backing collection and persistence layer (if persisted corpus removal is intended for this panel).
- If persistence is deferred, mark for deletion and commit in existing save flow.

`RemoveSelectedRecordingCommand`:
- Remove selected recording from current session collection.
- Keep recording-state flags consistent with new count.

`ClearRecordingsCommand`:
- Clear recording/session collection.
- Reset selected recording.
- Update session summary and command can-execute states.

### Scraper tab layout updates

Use a resizable column layout for the right sidebar:
- Ensure right panel column width is not fixed-only.
- Add splitter between main content and right sidebar.
- Persist user-adjusted width if the tab already persists layout settings.

Suggested layout shape:
- `Main content | GridSplitter | Right sidebar`
- Right sidebar min width to prevent collapse to unusable size.

### Right sidebar content composition

When inspector visible:
- Show inspector content plus action section.

When inspector hidden:
- Show action section and selection details only (non-empty fallback panel).

## Files (expected)

| File | Action |
|------|--------|
| `tools/Brinell.Scraper/ViewModels/Tabs/SessionPanelViewModel.cs` (or current sidebar VM) | Update selection/action commands |
| `tools/Brinell.Scraper/Views/Tabs/ScrapingTabView.xaml` | Add splitter/resizable right sidebar and fallback content |
| `tools/Brinell.Scraper/Views/Tabs/ScrapingTabView.xaml.cs` | Optional wiring for inspector visibility state |
| `tools/Brinell.Scraper/ViewModels/Tabs/ScrapingTabViewModel.cs` | Wire remove/clear/navigate actions to services |
| `tools/Brinell.Scraper/Services/*` | Optional corpus delete persistence hook |

## Acceptance Criteria

1. Clicking corpus page rows changes selection only; no automatic navigation occurs.
2. `Navigate` and `Remove` corpus buttons operate on selected corpus page.
3. Clicking recording rows changes selection only; no automatic navigation occurs.
4. `Navigate`, `Remove`, and `Clear All` recording actions are present and correctly enabled/disabled.
5. Right sidebar can be resized by the user on scraper tab.
6. Right sidebar is not empty when inspector is hidden; fallback action/content panel is visible.
7. Selection state and action availability remain consistent after remove and clear-all actions.

## Test Impact

Add/adjust tests in step-8 test plan scope:
- row click selects item and does not navigate (corpus + recordings)
- navigate command triggers navigation only when command pressed
- remove selected corpus item updates list and selection
- remove selected recording updates list and selection
- clear-all recordings empties list and resets selection
- right-sidebar fallback content visible when inspector hidden
- right-sidebar column is resizable

## Notes

- This change intentionally shifts from implicit navigation to explicit user actions to prevent accidental page transitions.
- If corpus delete is destructive/persistent, add confirmation policy consistent with existing delete UX.