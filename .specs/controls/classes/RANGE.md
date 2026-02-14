# Range Controls

**Source of truth:** `srcnew/Brinell.Maui/Controls/Range/`

## Controls

| Control | Interfaces | MAUI Control |
|---------|-----------|-------------|
| `MauiSliderControl` | `IRangeControlObject` | `Slider` |
| `MauiStepperControl` | `IRangeControlObject` | `Stepper` |
| `MauiProgressBarControl` | `IProgressControlObject` | `ProgressBar` |
| `MauiActivityIndicatorControl` | `IProgressControlObject` | `ActivityIndicator` |

## Slider Behavior

- `SetValue()` — Uses `IRangePatternElement.SetRangeValue()` when supported (FlaUI on Windows), falls back to gesture-based sliding
- Tolerance: `AssertValue()` and `WaitValue()` accept a `tolerance` parameter for floating-point comparison
- `ScrollIntoView` before interaction (see DES-026 — slider tests fail when off-screen)

## Stepper Behavior

- `Increment()` / `Decrement()` — Click the +/- buttons
- `SetValue()` — Repeatedly increment/decrement to reach target value
