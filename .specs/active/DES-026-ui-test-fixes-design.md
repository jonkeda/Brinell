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

Strategy selection:
- **FlaUI (Windows):** Use `IRangePatternElement.SetRangeValue()` — direct, precise
- **Appium (mobile/fallback):** Gesture + verify loop:
  1. Calculate target position as percentage of slider track
  2. Perform swipe/drag gesture
  3. Read actual value
  4. If not within tolerance, adjust and retry (max 3 attempts)

## Fix 3: Toggle State Verification

After `Click()` on a toggle control:
1. Click the element
2. Poll `IsChecked()` for expected state change (up to 2 seconds)
3. If state hasn't changed, click again (handles missed clicks)
4. Fail if state doesn't match after retries

## ScrollIntoView on Android

Windows uses `windows: scroll`. Android needs different approach:
- `mobile: scrollGesture` for explicit scroll
- `UiScrollable` for scroll-to-find
- See SPEC-scrollintoview-android for full analysis
