# UI Test Fixes — Implementation Design

**Status:** Active | **Implements:** SPEC-026

## Fix 1: ScrollIntoView Before Interaction

Add to `MauiControlBase.RunWithElement()`:

1. Find element
2. Check visibility
3. If not visible → `element.ScrollIntoView()`
4. Re-find element (DOM may have changed after scroll)
5. Execute action

This is built into the base class, so all controls inherit the behavior.

## Fix 2: Robust Slider Value Setting

Strategy selection by driver type:

- **FlaUI (Windows):** Use `IRangePatternElement.SetRangeValue()` — direct, precise, no gesture needed
  - Also use `GetRangeValue()` for reading, `GetRangeMinimum()`/`GetRangeMaximum()` for range
  - Value clamped to min/max automatically
- **Appium (mobile/fallback):** Gesture + verify loop:
  1. Calculate target position as percentage of slider track
  2. Perform swipe/drag gesture
  3. Read actual value
  4. If not within tolerance, adjust and retry (max 3 attempts)

### IRangePatternElement Interface

```csharp
public interface IRangePatternElement
{
    bool SupportsRangeValue { get; }
    void SetRangeValue(double value);
    double? GetRangeValue();
    double? GetRangeMinimum();
    double? GetRangeMaximum();
    double? GetRangeSmallChange();
}
```

Control integration in `MauiRangeControlBase`:
```csharp
if (element is IRangePatternElement range && range.SupportsRangeValue)
    range.SetRangeValue(value);  // Direct pattern
else
    // Gesture fallback
```

## Fix 3: Toggle State Verification

After `Click()` on a toggle control:
1. Click the element
2. Poll `IsChecked()` for expected state change (up to 2 seconds)
3. If state hasn't changed, click again (handles missed clicks)
4. Fail if state doesn't match after retries

## Fix 4: Button-Based Stepper

MAUI Stepper on Windows renders with child +/- buttons:
1. Try `IRangePatternElement.SetRangeValue(currentValue + step)` first
2. If not supported, find and click increment/decrement button children
3. Search for buttons with "Increase"/"+", "Decrease"/"-" identifiers

## ScrollIntoView on Android

Windows uses `windows: scroll`. Android needs different approach:
- `mobile: scrollGesture` for explicit scroll
- `UiScrollable` for scroll-to-find
- See [SPEC-scrollintoview-android.md](SPEC-scrollintoview-android.md) for full analysis
