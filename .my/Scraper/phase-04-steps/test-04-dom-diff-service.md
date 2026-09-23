# Test 4.4 — DomDiffService Tests

**Covers:** Step 4.10 — `DomDiffService` (compare two `DomSnapshot` instances and categorize changes)

**File:** `Brinell.Scraper.Tests/Services/DomDiffServiceTests.cs`

## Test Inventory (8 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `Compare_IdenticalSnapshots_NoChanges` | Two identical snapshots produce `UnchangedCount` equal to total element count, empty `Added`/`Removed`/`Changed` lists |
| 2 | `Compare_AddedElement_InAddedList` | "After" snapshot has an extra element not in "before" → element appears in `DomDiffResult.Added` |
| 3 | `Compare_RemovedElement_InRemovedList` | "Before" snapshot has an element not in "after" → element appears in `DomDiffResult.Removed` |
| 4 | `Compare_ChangedAttribute_InChangedList` | Same element (matched by `id`) has a changed attribute (e.g., `ClassName` differs) → appears in `DomDiffResult.Changed` with attribute detail |
| 5 | `Compare_MatchesById_First` | Elements with the same `id` attribute are matched regardless of position in the tree |
| 6 | `Compare_MatchesByDataTestId_Second` | Elements without `id` but with matching `data-testid` are matched as the same element |
| 7 | `Compare_MatchesByStructuralPath_Last` | Elements without `id` or `data-testid` but at the same structural position (tag + parent path) are matched |
| 8 | `Compare_MultipleChanges_AllCategorized` | A diff with added, removed, and changed elements all appearing in one comparison — all three lists populated correctly |

## Notes

- `DomDiffService.Compare(before, after)` returns a `DomDiffResult` with `Added`, `Removed`, `Changed`, and `UnchangedCount`.
- `Changed` list contains `DomElementChange` objects with before/after element references and a list of changed attribute names.
- Build test snapshots with known `DomElement` trees — vary `Id`, `DataTestId`, `ClassName`, `Children` between before and after.
- Test 1: clone a snapshot exactly and compare — `Added`/`Removed`/`Changed` should all be empty.
- Test 4: same `id` on both elements but change `ClassName` from `"old-class"` to `"new-class"`.
- Test 5: move an element to a different position in the tree but keep the same `id` — should still match.
- Test 6: elements with `id = null` but `DataTestId = "submit-btn"` should be matched.
- Test 7: elements with no `id` and no `data-testid` matched by tag name + index in parent.
- Matching priority is `id` → `data-testid` → `name` → structural path (tag + position).
- Pure logic service — no WPF dependencies, no STA thread required.
