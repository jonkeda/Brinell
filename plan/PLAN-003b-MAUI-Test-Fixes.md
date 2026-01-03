# PLAN-003b: MAUI Test Fixes

**Created:** 2026-01-03  
**Completed:** 2026-01-03  
**Status:** ✅ Complete  
**Depends On:** PLAN-003 (MAUI Update)

## Summary

Fixed failing MAUI UI tests:
- **Initial:** 16/21 tests passed (76%)
- **Final:** 21/21 tests passed (100%) ✅

## Issues Fixed

### 1. Toggle State Detection (Switch & CheckBox)
- **Root Cause:** Windows UIA uses `Toggle.ToggleState` ("1"/"0") not `checked` ("true"/"false")
- **Fix:** Updated `SwitchControl.IsChecked()` and `CheckBoxControl.IsChecked()` to check:
  - `Toggle.ToggleState` first (Windows UIA pattern)
  - `checked` attribute (Android/iOS)
  - `IsToggled`/`IsChecked` for MAUI-specific properties

### 2. Switch Toggle Action  
- **Root Cause:** `element.Click()` didn't toggle Windows Switch
- **Fix:** SwitchControl.Toggle() now uses `TapAtCoordinates()` on element center

### 3. Slider SetValue
- **Root Cause:** 
  - GetMinimum()/GetMaximum() returned defaults (0/1) instead of actual values (0/100)
  - Windows Actions API doesn't support mouse pointer, only pen/touch
- **Fix:** 
  - Added `RangeValue.Minimum/Maximum` attribute reading for Windows UIA
  - Changed default maximum to 100 for percentage-based sliders
  - Used `TapAtCoordinates()` with minimal padding (2px) for accurate positioning

### 4. ActivityIndicator State Detection
- **Root Cause:** Windows doesn't expose standard `IsRunning` attribute
- **Fix:** Added fallback to check `Displayed && Enabled` state on Windows

### 5. Post-Tap Delay
- **Root Cause:** Button clicks completing before UI updates
- **Fix:** Added `PostTapDelayMs` (100ms) to ControlBase.Tap()

## Files Modified

| File | Changes |
|------|---------|
| `src/Brinell.Maui/Controls/SwitchControl.cs` | Updated `IsChecked()` for Windows UIA, added custom `Toggle()` with TapAtCoordinates |
| `src/Brinell.Maui/Controls/CheckBoxControl.cs` | Updated `IsChecked()` for Windows UIA |
| `src/Brinell.Maui/Controls/SliderControl.cs` | Added RangeValue attribute reading, fixed `SetValue()` tap positioning |
| `src/Brinell.Maui/Controls/ActivityIndicatorControl.cs` | Added `IsActive` check and Displayed/Enabled fallback |
| `src/Brinell.Maui/Controls/Base/ControlBase.cs` | Added `PostTapDelayMs` property with 100ms delay after tap |
| `docs/run/MAUI.md` | Complete rewrite with Appium setup, troubleshooting, Windows UIA notes |

## Key Learnings - Windows UIA Patterns

| Control | Attribute | Values |
|---------|-----------|--------|
| Toggle (Switch/CheckBox) | `Toggle.ToggleState` | "1" = on, "0" = off |
| Slider | `RangeValue.Minimum`, `RangeValue.Maximum`, `RangeValue.Value` | Numeric |
| Actions API | `PointerKind` | Only `Pen` and `Touch` supported (no `Mouse`) |
| ActivityIndicator | N/A | Use `Displayed && Enabled` as fallback |

## Verification

```
Test summary: total: 21, failed: 0, succeeded: 21, skipped: 0, duration: 136.3s
```
