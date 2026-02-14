# Toggle Controls

**Source of truth:** `srcnew/Brinell.Maui/Controls/Toggle/`

## Controls

| Control | Interfaces | MAUI Control |
|---------|-----------|-------------|
| `MauiCheckBoxControl` | `IToggleControlObject`, `IClickableControlObject` | `CheckBox` |
| `MauiSwitchControl` | `IToggleControlObject`, `IClickableControlObject` | `Switch` |
| `MauiRadioButtonControl` | `IToggleControlObject`, `IClickableControlObject` | `RadioButton` |

## Behavior

- `Toggle()` — Click the control; state changes are verified after click
- `SetChecked(true/false)` — Only clicks if current state differs from desired
- `Check()` / `Uncheck()` — Convenience for `SetChecked(true/false)`
- Platform note: `Switch` on MAUI needs click on the toggle thumb, not the label
- `ScrollIntoView` before toggle if off-screen (see DES-026)
