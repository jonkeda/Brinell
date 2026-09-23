# RCA-023: Phase 12 Wireup 07 Follow-ups (Inspect Refresh, Ctrl+Click After Navigation, Group UX)

**Reported:** 2026-05-09
**Severity:** High
**Component:** `ViewModels/Tabs/ScrapingTabViewModel.cs`, `Views/Tabs/ScrapingTabView.xaml`, `ViewModels/InspectorViewModel.cs`, `Services/ControlGroupDetector.cs`
**UAT Reference:** `uat/uat-phase-12-wireup-07.md`

---

## Summary of Reported Issues

1. Moving to another page does not update the inspection sidebar to the new page.
2. Ctrl+click works on the first page but not after navigating to another page.
3. Auto-detect control groups behavior is unclear.
4. If more than 6 groups are detected, a scrollbar should be shown.
5. Requirement text is incomplete (`For ...`).

---

## Symptom Details

### S1 - Inspector sidebar stays stale after page change

Inspect mode is enabled, but after navigating, the DOM tree/sidebar still reflects the old snapshot.

### S2 - Ctrl+click stops working after navigation

Ctrl+click selection works initially, but after page navigation it no longer posts selection events from the new page context.

### S3 - Auto-detect groups is not self-explanatory in UI

The feature runs and shows summary/list entries, but UX does not explain intent clearly enough to end users.

### S4 - Many detected groups overflow without clear scrolling UX

The group list uses `ItemsControl` with `MaxHeight=120`, but no explicit scrolling container around that list.

---

## Root Cause Analysis

### RC1 - No inspect refresh on navigation in active workspace flow

In `ScrapingTabViewModel`, navigation success currently calls recording capture logic only:

- `OnNavigationSucceeded()` -> `CaptureTransitionAsync(...)`
- `CaptureTransitionAsync(...)` exits early unless `Recording.IsRecording` is true

There is no branch that says:

- if `Inspector.IsInspecting` is true, re-capture DOM for the new page
- reload inspector snapshot
- re-enable overlay with force re-injection semantics

Result: inspector side panel is stale after page navigation.

### RC2 - Overlay scripts are page-context scripts and are lost after navigation

Ctrl+click depends on overlay JS event handlers injected into the document. Navigating destroys that document and its JS listeners. Since inspect is not re-enabled on navigation in the active `ScrapingTabViewModel` path, Ctrl+click handlers are not restored for the new page.

Result: Ctrl+click appears to work only on first page.

### RC3 - Auto-detect intent not explicit in UI copy

`ControlGroupDetector` detects structural containers (`form`, `table`, `list`, `nav`, fieldset/role containers). `InspectorViewModel.AcceptGroup` adds suggested child controls to `SelectedElements` (which drives green highlights and downstream generation selection), but the panel currently presents minimal explanatory text.

Result: users do not know what problem auto-detect is solving.

### RC4 - Group list has max height but no dedicated scroll host

The inspector panel currently renders detected groups with:

- `ItemsControl ItemsSource="{Binding Inspector.ControlGroups}" MaxHeight="120"`

Without wrapping this section in a `ScrollViewer` configured for vertical auto-scroll, overflow behavior is not explicit and can feel clipped when many groups are detected.

---

## What Auto-Detect Control Groups Is Supposed To Do

Auto-detect proposes logical containers in the captured DOM and helps users select controls faster.

- Detects likely containers: forms, tables, lists, navigation blocks, and certain ARIA-role containers.
- Shows summary/list of suggestions in the inspector panel.
- `Accept` on a group selects the suggested child controls in one action.
- `Accept All` applies all suggestions.
- `Reject` marks a suggestion not to apply.
- `Dismiss` clears the suggestion list.

Expected value: reduce manual Ctrl+click and tree-by-tree selection work before code generation.

---

## Proposed Fixes

### F1 - Refresh inspect state on navigation

In `ScrapingTabViewModel.OnNavigationSucceeded`:

- if `Inspector.IsInspecting`:
  - capture new snapshot (`CaptureAsync(webView, _highlight.TrackedFrames)`)
  - `Inspector.LoadSnapshot(snapshot)`
  - run `RunAutoGroupDetection(snapshot)`
  - re-enable overlay with forced re-injection on the new page context

### F2 - Ensure overlay re-injection path is forced on navigation refresh

Use existing highlight service re-enable path that supports forced injection semantics so listeners are restored in the new page and tracked frames are valid.

### F3 - Clarify auto-detect UX text

Add brief helper text near summary, e.g.:

"Detected layout groups. Accept to auto-select likely controls for generation."

### F4 - Add explicit scroll container for detected groups

Wrap detected group list with:

- `ScrollViewer MaxHeight="120" VerticalScrollBarVisibility="Auto"`

and place `ItemsControl` inside, so when groups > ~6 rows, scroll appears predictably.

---

## Verification Checklist

- [ ] Enable inspect, navigate to another page: inspector tree updates to new page DOM.
- [ ] Enable inspect, navigate, Ctrl+click works on the new page.
- [ ] Enable inspect, navigate in iframe-enabled page, Ctrl+click still works for iframe/top-level.
- [ ] Group helper text explains purpose of auto-detect.
- [ ] With > 6 detected groups, vertical scrollbar appears in group section.

---

## Open Item

Issue 5 is incomplete in the report (`For ...`). Please provide the full requirement text so it can be included in this RCA and implemented/verified.