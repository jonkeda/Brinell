# User Acceptance Tests - Phase 12 Wireup 07 (Inspector Mode Actions)

Manual test scenarios for phase-12-07 updates:

- 07a Inspect toggle and browser overlay behavior
- 07b DOM tree panel rendering and browser sync
- 07c Multi-select and Ctrl+click selection flow
- 07d Auto-detect control groups with accept/reject actions

## Prerequisites

- Windows 10/11 with .NET 10 runtime
- WebView2 runtime installed
- At least 1 active site opened in Workspace > Scraping tab
- Test page that includes:
  - regular inputs/buttons in top-level DOM
  - at least one iframe (same-origin preferred, cross-origin optional)
  - at least one form and one list or table for group detection
- Optional evidence capture folder: `image/uat-phase-12-wireup-07/`

---

## W7.1 - Inspect Toggle and Overlay (07a)

### UAT-W7.1.1 - Inspect Toggle Enables Inspector Mode

- [X] Open Scraping tab and navigate to a loaded page.
- [X] Click Inspect toggle (magnifier).
- [X] Right inspector panel becomes visible.
- [X] DOM snapshot is captured and tree is populated.

### UAT-W7.1.2 - Overlay Appears on Hover

- [X] With Inspect enabled, move mouse over several elements in browser.
- [X] Blue overlay follows hovered element.
- [X] Tooltip appears with element context and locator suggestion.

### UAT-W7.1.3 - Inspect Toggle Disables Overlay

- [X] Click Inspect toggle again to disable.
- [X] Blue overlay and tooltip disappear.
- [X] Inspector panel fallback content is shown.

### UAT-W7.1.4 - Iframe Overlay Coverage

- [X] Enable Inspect on a page containing iframe content.
- [X] Hover elements inside iframe.
- [X] Blue overlay and tooltip also work inside iframe.

---

## W7.2 - DOM Tree Panel and Browser Sync (07b)

### UAT-W7.2.1 - Tree Hierarchy and Element Count

- [X] Enable Inspect.
- [X] Tree shows hierarchical DOM structure.
- [X] Selected/total status area shows a non-zero total element count.

### UAT-W7.2.2 - Filter Narrows Tree Results

- [X] Enter a filter term (tag/id/class/text) in the tree filter box.
- [X] Tree is reduced to matching branches.
- [X] Clear filter and verify full tree returns.

### UAT-W7.2.3 - Hover Tree Node Highlights Browser Element

- [X] Move mouse over a tree node representing a visible element.
- [X] Orange tree-highlight rectangle appears on matching browser element.
- [X] Move mouse away from tree node.
- [X] Tree-highlight rectangle is cleared.

### UAT-W7.2.4 - Click Tree Node Scrolls To Element

- [X] Click a tree node for an element outside current viewport.
- [X] Browser scrolls the element into view.

### UAT-W7.2.5 - Iframe Content Present in Tree

- [X] On a page with iframe content, enable Inspect.
- [X] Verify iframe subtree content appears in DOM tree under iframe container.

---

## W7.3 - Multi-Select and Ctrl+Click (07c)

### UAT-W7.3.1 - Ctrl+Click Selects Element in Main Frame

- [ ] With Inspect enabled, Ctrl+click an element in top-level document.
- [ ] Element gets green outline in browser.
- [ ] Selected count increments by 1.

### UAT-W7.3.2 - Ctrl+Click Toggles Selection Off

- [ ] Ctrl+click the same selected element again.
- [ ] Green outline is removed.
- [ ] Selected count decrements.

### UAT-W7.3.3 - Ctrl+Click Selects Element Inside Iframe

- [ ] Ctrl+click an element inside iframe.
- [ ] Green outline appears for iframe element.
- [ ] Selected count updates correctly.
- [ ] No incorrect top-level element is toggled.

### UAT-W7.3.4 - Select Forms Bulk Action

- [ ] Click Select Forms.
- [ ] Matching form controls (input/select/textarea/button) are selected.
- [ ] Selected count increases and browser shows green outlines.

### UAT-W7.3.5 - Select Inputs Bulk Action

- [ ] Click Select Inputs.
- [ ] Input elements are selected.
- [ ] Selected count reflects input-only selection.

### UAT-W7.3.6 - Clear Selection Clears Browser and VM State

- [ ] Create a multi-selection set.
- [ ] Click Clear.
- [ ] Selected count returns to 0.
- [ ] All green outlines are removed from browser.

---

## W7.4 - Auto-Detect Control Groups (07d)

### UAT-W7.4.1 - Detection Runs After Capture

- [ ] Enable Inspect on page with forms/lists/tables/nav.
- [ ] Control group summary appears (e.g., found forms/lists/tables/nav).
- [ ] Group suggestion list is populated.

### UAT-W7.4.2 - Accept Group Adds Suggested Children to Selection

- [ ] In group list, click Accept for one detected group.
- [ ] Selected count increases.
- [ ] Suggested child elements are highlighted in browser.

### UAT-W7.4.3 - Reject Group Does Not Add Selection

- [ ] Click Reject on a pending detected group.
- [ ] No additional elements are selected from that group.

### UAT-W7.4.4 - Accept All Applies All Suggestions

- [ ] Click Accept All.
- [ ] Selected count reflects union of suggested child elements.
- [ ] Browser highlights appear for accepted suggestions.

### UAT-W7.4.5 - Dismiss Clears Suggestion Banner/List

- [ ] Click Dismiss.
- [ ] Summary text clears.
- [ ] Group suggestion list is hidden/empty.

---

## W7.5 - Regression and Stability

### UAT-W7.5.1 - Recording Flow Still Works

- [ ] Start recording and navigate to another page.
- [ ] Session capture still occurs as expected.
- [ ] Inspect/group features do not block recording behavior.

### UAT-W7.5.2 - Inspect Re-enable Refreshes Snapshot

- [ ] Disable Inspect, navigate to different page section/route, enable Inspect again.
- [ ] New snapshot/tree reflects current page state.
- [ ] No duplicate event behavior or stale highlights remain.

### UAT-W7.5.3 - Iframe Navigation During Recording Remains Stable

- [ ] While recording, trigger iframe navigation.
- [ ] Session capture still works for iframe navigation.
- [ ] Inspect overlays and selections remain functional afterward.

---

## Sign-off

| Section                              | Tester | Date | Result |
| ------------------------------------ | ------ | ---- | ------ |
| W7.1 Inspect Toggle and Overlay      |        |      |        |
| W7.2 DOM Tree Panel and Browser Sync |        |      |        |
| W7.3 Multi-Select and Ctrl+Click     |        |      |        |
| W7.4 Auto-Detect Control Groups      |        |      |        |
| W7.5 Regression and Stability        |        |      |        |
