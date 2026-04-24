# Test 1.1 — ViewModelBase Tests

## Covers

Step 02 (`02-mvvm-foundation.md`) — `ViewModelBase`, `SetProperty`, `OnPropertyChanged`

## Tests (9)

| Test | Description |
|------|-------------|
| `SetProperty_RaisesPropertyChanged_WhenValueChanges` | Verify event fires with correct property name |
| `SetProperty_DoesNotRaise_WhenValueUnchanged` | Same value → no event |
| `SetProperty_ReturnsTrue_WhenChanged` | Return value indicates change |
| `SetProperty_ReturnsFalse_WhenUnchanged` | Return value indicates no change |
| `SetProperty_UpdatesBackingField` | Field holds new value after set |
| `SetProperty_HandlesNullToValue` | null → non-null transition |
| `SetProperty_HandlesValueToNull` | non-null → null transition |
| `SetProperty_HandlesReferenceTypes` | Object equality check |
| `SetProperty_HandlesValueTypes` | int, bool, enum equality |

## Implementation Notes

- Create a `TestViewModel : ViewModelBase` helper with `Name` (string) and `Count` (int) properties
- `SetProperty` is protected — test via the property setters and `PropertyChanged` event
- Verify `[CallerMemberName]` sends the correct property name
