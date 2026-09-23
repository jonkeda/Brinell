# Test 4.7 — InspectorViewModel Tests

**Covers:** Step 4.4 — `InspectorViewModel` (multi-select mode for DOM elements)

**File:** `Brinell.Scraper.Tests/ViewModels/InspectorViewModelTests.cs`

## Test Inventory (6 tests)

| # | Test Name | Assertion |
|---|-----------|-----------|
| 1 | `SelectedElements_InitiallyEmpty` | `SelectedElements.Count == 0` after construction |
| 2 | `AddElement_IncreasesCount` | Adding a `DomElement` to `SelectedElements` increases `SelectedElements.Count` by 1 |
| 3 | `RemoveElement_DecreasesCount` | Removing a `DomElement` from `SelectedElements` decreases `SelectedElements.Count` by 1 |
| 4 | `ClearSelection_EmptiesCollection` | Executing `ClearSelectionCommand` results in `SelectedElements.Count == 0` |
| 5 | `SelectAllForms_SelectsFormInputElements` | Given a `DomSnapshot` containing `<input>`, `<select>`, `<textarea>`, and `<button>` elements, executing `SelectAllFormsCommand` adds all form-related elements to `SelectedElements` |
| 6 | `SelectedCount_ReflectsCollectionSize` | `SelectedCount` property matches `SelectedElements.Count` — changes when elements are added or removed |

## Notes

- `InspectorViewModel` depends on a `DomSnapshot` (the current page's captured DOM tree) to operate on.
- `SelectedElements` is an `ObservableCollection<DomElement>` — test by directly adding/removing elements and verifying count.
- `SelectAllFormsCommand` scans the DOM tree recursively and selects elements with tags: `input`, `select`, `textarea`, `button`.
- `ClearSelectionCommand` calls `SelectedElements.Clear()`.
- `SelectedCount` should fire `PropertyChanged` when elements change — verify via `INotifyPropertyChanged` subscription.
- Build test DOM trees with a mix of form and non-form elements to verify `SelectAllForms` only picks form-related tags.
- Stub ViewModel — tests may need to be adjusted once the full implementation is built. Focus on the `ObservableCollection<DomElement>` behavior and command wiring.
- No WPF dispatcher dependency for the ViewModel itself — `ObservableCollection` operations are on the test thread.
