# Step 12.W.8e - Remove Right Sidebar Fallback Actions and Fix Capture DOM Snapshot

## Objective

Refine the phase-12-08 sidebar UX by removing redundant fallback action buttons from the right sidebar and fixing the `Capture DOM Snapshot` button so it performs the expected capture action.

## Change Scope

### 1) Remove fallback buttons from the right sidebar

Current behavior:

- The right sidebar shows fallback action buttons when the inspector is hidden.

Required behavior:

- Remove the fallback button row from the right sidebar.
- Keep the right sidebar usable for context display only when inspector is hidden.
- Preserve the selected corpus/recording summary text if still useful, but no action buttons should remain there.

### 2) Fix `Capture DOM Snapshot` button

Current behavior:

- Clicking `Capture DOM Snapshot` appears to do nothing.

Required behavior:

- The button must trigger the DOM snapshot capture flow.
- The capture action should create the expected snapshot result and surface it through the existing inspector/recording workflow.
- If the current handler is not wired, attach it to the correct command or event path.

### 3) Additional follow-up item

- Item 3 was not specified in the request and should be added once clarified.

## UX Design

### Right sidebar content

- Inspector-visible mode remains unchanged.
- Inspector-hidden mode should not show.
- The rightsidebar should be sizable

### Snapshot capture button behavior

- Capture should be immediate and obvious.
- If the capture depends on inspector state, the UI should show a short status update or selection change after the click.
- The button should not silently fail when inspector is hidden or when no element is selected; it should either capture a valid DOM snapshot or provide a clear user-facing reason.

## Technical Design

### Right sidebar cleanup

- Remove the fallback button container from `ScrapingTabView.xaml`.

### DOM snapshot capture wiring

- Trace the `Capture DOM Snapshot` button to its command or click handler.
- Verify the handler is bound in the active tab view model and not shadowed by an inactive DataContext.
- Ensure the command target has access to the current browser/inspector state.
- If the capture path is async, surface completion or failure through the existing log/status flow.

## Files (expected)

| File                                                                                    | Action                                                   |
| --------------------------------------------------------------------------------------- | -------------------------------------------------------- |
| `tools/Brinell.Scraper/Views/Tabs/ScrapingTabView.xaml`                               | Remove fallback action buttons from the right sidebar    |
| `tools/Brinell.Scraper/ViewModels/Tabs/ScrapingTabViewModel.cs`                       | Verify/repair DOM snapshot capture wiring                |
| `tools/Brinell.Scraper/ViewModels/InspectorViewModel.cs` or related inspector surface | Confirm snapshot command/event is exposed correctly      |
| `tools/Brinell.Scraper/Views/Tabs/ScrapingTabView.xaml.cs`                            | Optional if the button requires direct view-level hookup |

## Acceptance Criteria

1. Right sidebar no longer shows fallback action buttons.
2. Right sidebar still shows useful read-only context when inspector is hidden.
3. Clicking `Capture DOM Snapshot` visibly executes the capture flow.
4. A captured DOM snapshot appears in the expected inspector/recording path.
5. The button does not silently no-op in normal use.

## Test Impact

Add or adjust tests to cover:

- right sidebar hidden-state no longer renders action buttons
- `Capture DOM Snapshot` command/handler is invoked on click
- snapshot capture updates expected state or selection
- failure path reports a clear reason if capture cannot run

## Notes

- This step is intentionally narrower than the earlier sidebar-selection work: it removes redundant controls rather than introducing new ones.
- The unspecified third item should be folded into this step after clarification so the plan stays atomic.
