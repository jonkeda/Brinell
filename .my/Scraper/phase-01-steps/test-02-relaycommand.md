# Test 1.2 — RelayCommand Tests

## Covers

Step 02 (`02-mvvm-foundation.md`) — `RelayCommand`, `RelayCommand<T>`

## Tests (8)

| Test | Description |
|------|-------------|
| `Execute_CallsAction` | Action invoked on Execute |
| `CanExecute_ReturnsTrue_WhenNoPredicate` | Default canExecute = true |
| `CanExecute_ReturnsFalse_WhenPredicateFails` | Predicate returns false |
| `CanExecute_ReturnsTrue_WhenPredicatePasses` | Predicate returns true |
| `RaiseCanExecuteChanged_FiresEvent` | CanExecuteChanged event fires |
| `Execute_DoesNotThrow_WhenCanExecuteFalse` | Safe to call even when disabled |
| `RelayCommandT_PassesParameter` | Generic version passes typed parameter |
| `RelayCommandT_CanExecute_ReceivesParameter` | Predicate receives typed parameter |

## Implementation Notes

- Test `Execute(null)` when `CanExecute` returns false — should not throw
- For `RelayCommand<T>`, verify the parameter value reaches both the action and the predicate
- `RaiseCanExecuteChanged` should fire `CanExecuteChanged` event once
