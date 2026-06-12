# DateTime Controls

**Source of truth:** `srcnew/Brinell.Maui/Controls/DateTime/`

## Controls

| Control | Interfaces | MAUI Control |
|---------|-----------|-------------|
| `MauiDatePickerControl` | `IDateControlObject` | `DatePicker` |
| `MauiTimePickerControl` | `ITimeControlObject` | `TimePicker` |

## Behavior

- `SetDate()` / `SetTime()` — Sets the value via the element's value property or native picker interaction
- Platform-specific: native date/time picker dialogs on mobile vs. direct value setting on desktop
- Formatting depends on locale settings
