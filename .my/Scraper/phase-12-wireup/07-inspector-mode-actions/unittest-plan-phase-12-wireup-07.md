# Unit Test Plan - Phase 12 Wireup 07 (Inspector Mode Actions)

## Scope

This plan covers unit test strategy for all Phase-12-07 parts:

- 07a Inspect toggle + overlay lifecycle
- 07b DOM tree panel behavior and tree/browser sync logic
- 07c Multi-select + Ctrl+click selection flow
- 07d Auto-detect control groups and accept/reject actions

Target test project:

- `tools/Brinell.Scraper.Tests`

---

## Current Architecture Notes

### Already unit-test friendly

- `InspectorViewModel` selection logic and control-group actions are pure VM logic.
- `ControlGroupDetector` is pure service logic over `DomElement` tree.
- `DomTreeViewModel` filter behavior is pure VM logic.

### Needs seam for high-value unit tests

The following behaviors currently depend on concrete WebView2 types and script execution:

- `ScrapingTabViewModel` inspect refresh on navigation (`F1`, `F2`)
- `ElementHighlightService` script re-injection and force behavior

To unit test those cleanly, add thin adapters/interfaces around:

- capture (`IDomCaptureGateway`)
- highlight overlay (`IHighlightGateway`)
- webview availability (`IBrowserRuntime`)

Without adapters, only null-branch and side-effect-limited tests are practical.

---

## Proposed Test File Layout

- `tools/Brinell.Scraper.Tests/Services/ControlGroupDetectorTests.cs`
- `tools/Brinell.Scraper.Tests/ViewModels/InspectorViewModelTests.cs`
- `tools/Brinell.Scraper.Tests/ViewModels/DomTreeViewModelTests.cs`
- `tools/Brinell.Scraper.Tests/ViewModels/Tabs/ScrapingTabViewModelInspectTests.cs`
- `tools/Brinell.Scraper.Tests/Services/ElementHighlightServiceTests.cs` (with seam)

---

## Test Cases by 07 Item

## 07a - Inspect Toggle & Overlay

### A1 - Toggle inspect ON updates inspect state

- Arrange: create `InspectorViewModel`
- Act: execute `ToggleInspectCommand`
- Assert: `IsInspecting == true`

### A2 - Toggle inspect OFF updates inspect state

- Arrange: set `IsInspecting = true`
- Act: execute `ToggleInspectCommand`
- Assert: `IsInspecting == false`

### A3 - Scraping VM handles inspect toggle with null webview safely

- Arrange: `Browser.GetCoreWebView2 = () => null`
- Act: toggle `Inspector.IsInspecting`
- Assert: no exception; inspector visibility property updated

### A4 - Navigation while inspecting triggers inspect refresh path (requires seam)

- Arrange: inspect active + fake capture/highlight gateways
- Act: trigger `Browser.NavigationSucceeded`
- Assert:
  - capture called once
  - `Inspector.LoadSnapshot` effects visible (`TotalElementCount` updated)
  - highlight enable called with `force=true`

### A5 - Iframe navigation while inspecting triggers inspect refresh path (requires seam)

- Arrange: inspect active + fake capture/highlight gateways
- Act: trigger `Browser.IFrameNavigationSucceeded`
- Assert: same as A4, with iframe naming semantics applied

---

## 07b - DOM Tree View Panel

### B1 - LoadSnapshot populates tree and total count

- Arrange: snapshot with nested DOM
- Act: `InspectorViewModel.LoadSnapshot(snapshot)`
- Assert:
  - `Snapshot` set
  - `DomTree.RootElements` contains root
  - `TotalElementCount` equals recursive count

### B2 - FilterText narrows tree results

- Arrange: `DomTreeViewModel.LoadSnapshot(snapshot)`
- Act: set `FilterText` to matching term
- Assert: only matching branches remain

### B3 - Clearing filter restores full tree

- Arrange: apply filter first
- Act: set `FilterText = ""`
- Assert: root restored; `IsFilterActive == false`

### B4 - Hover/click events are raised

- Arrange: subscribe to `ElementHovered` and `ElementClicked`
- Act: call `OnElementHover` / `OnElementClick`
- Assert: handlers invoked with expected element

---

## 07c - Multi-Select & Ctrl+Click

### C1 - ToggleElement adds element to selection

- Arrange: empty `SelectedElements`
- Act: `ToggleElement(el)`
- Assert: element added; `SelectedCount == 1`

### C2 - ToggleElement removes element from selection

- Arrange: add element first
- Act: `ToggleElement(el)` again
- Assert: element removed; `SelectedCount == 0`

### C3 - ElementSelectionChanged fires true on select

- Arrange: subscribe event
- Act: `ToggleElement(el)`
- Assert: event payload `(el, true)`

### C4 - ElementSelectionChanged fires false on deselect

- Arrange: select once, subscribe/capture
- Act: toggle same element
- Assert: event payload `(el, false)`

### C5 - ClearSelection empties selected and raises SelectionCleared

- Arrange: add several elements
- Act: `ClearSelection()`
- Assert:
  - `SelectedElements` empty
  - `SelectedCount == 0`
  - `SelectionCleared` fired once

### C6 - SelectAllForms selects expected tags only

- Arrange: snapshot containing form controls and non-controls
- Act: execute `SelectAllFormsCommand`
- Assert: all selected items are in `{input, select, textarea, button}`

### C7 - SelectAllInputs selects input only

- Arrange: mixed DOM
- Act: execute `SelectAllInputsCommand`
- Assert: all selected items have tag `input`

### C8 - Browser message mapping by bounding box finds correct node

- Arrange: build root with unique bounding boxes
- Act: call internal find/mapping path via VM event trigger (through `Browser.ElementSelected` in VM test)
- Assert: expected element toggled in selection

---

## 07d - Auto-Detect Control Groups

### D1 - Detect finds form container

- Arrange: DOM with `<form>` and children
- Act: `ControlGroupDetector.Detect(root)`
- Assert: contains `FormContainer`

### D2 - Detect finds table only with thead+tbody

- Arrange: one table with both, one incomplete
- Act: detect
- Assert: only complete table is returned

### D3 - Detect finds list for 2+ li items

- Arrange: list with 1 li and list with 2 li
- Act: detect
- Assert: only 2+ li list detected as `ListContainer`

### D4 - Detect finds nav container

- Arrange: DOM with `<nav>`
- Act: detect
- Assert: `NavigationContainer` present

### D5 - Detect finds fieldset with legend

- Arrange: fieldset with legend and controls
- Act: detect
- Assert: `FieldsetContainer` entry present with children

### D6 - Detect finds role-based container

- Arrange: `<div role="dialog">`
- Act: detect
- Assert: `RoleContainer` present

### D7 - LoadControlGroups sets summary text

- Arrange: load non-empty suggestions into `InspectorViewModel`
- Act: `LoadControlGroups(groups)`
- Assert: `ControlGroupSummary` starts with `Found`

### D8 - AcceptGroup marks accepted and selects children

- Arrange: suggestion with child elements, subscribe selection event
- Act: execute `AcceptGroupCommand`
- Assert:
  - `IsAccepted == true`
  - child elements added to `SelectedElements`

### D9 - RejectGroup marks rejected and does not select children

- Arrange: suggestion with children
- Act: execute `RejectGroupCommand`
- Assert:
  - `IsAccepted == false`
  - no child auto-add side effects

### D10 - AcceptAllGroups applies all suggestions

- Arrange: multiple groups
- Act: execute `AcceptAllGroupsCommand`
- Assert: each group is accepted and children selected (de-duplicated)

### D11 - DismissGroups clears groups and summary

- Arrange: loaded suggestions
- Act: execute `DismissGroupsCommand`
- Assert:
  - `ControlGroups` empty
  - `ControlGroupSummary == ""`

---

## Priority Order

### P0 (must-have)

- C1, C2, C5, C6, C7
- D1, D2, D3, D7, D8, D11
- B1, B2

### P1 (important)

- B3, B4
- C3, C4, D9, D10

### P2 (with test seams)

- A4, A5 and any direct `ElementHighlightService` force-injection tests

---

## Suggested Execution Commands

Run full relevant set:

```powershell
Set-Location "e:/repos/Private/Hours/Brinell"
dotnet test tools/Brinell.Scraper.Tests/Brinell.Scraper.Tests.csproj -v minimal --filter "FullyQualifiedName~InspectorViewModelTests|FullyQualifiedName~ControlGroupDetectorTests|FullyQualifiedName~DomTreeViewModelTests|FullyQualifiedName~ScrapingTabViewModelInspectTests"
```

Run only Phase-12-07 tests (if classes follow `Phase12Wireup07*` naming):

```powershell
dotnet test tools/Brinell.Scraper.Tests/Brinell.Scraper.Tests.csproj -v minimal --filter "FullyQualifiedName~Phase12Wireup07"
```

---

## Definition of Done for 07 Unit Test Coverage

- All P0 tests implemented and passing in CI
- No flaky tests across 3 repeated runs
- P1 tests implemented or explicitly deferred with rationale
- P2 seam work captured as backlog item if not implemented in this phase
