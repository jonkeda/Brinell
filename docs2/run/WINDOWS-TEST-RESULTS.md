# Windows FlaUI Test Results

**Date:** January 2025  
**Framework:** Brinell MAUI UI Tests  
**Driver:** FlaUI (native Windows UI Automation)  
**Target:** net10.0-windows10.0.19041.0

## Summary

| Metric | Value |
|--------|-------|
| **Total Tests** | 232 |
| **Passed** | 152 |
| **Failed** | 75 |
| **Skipped** | 5 |
| **Pass Rate** | 65.5% |

## Test Results by Category

### Fully Passing (100%)

| Test Class | Tests | Status |
|------------|-------|--------|
| ButtonControlTests | 22/22 | ✅ |
| MainPageTests | 15/15 | ✅ |
| EntryControlTests | 17/17 | ✅ |
| TabbedPageTests | 6/6 | ✅ |
| Toggle Tests (All) | 28/28 | ✅ |

### Partially Passing

| Test Class | Passed/Total | Issues |
|------------|--------------|--------|
| Range Tests | 13/19 | Slider/Stepper keyboard interaction |
| Selection Tests | 3/8 | Picker item enumeration |
| Text Tests | 8/14 | SearchBar text retrieval |
| DateTime Tests | ~12/20 | DatePicker/TimePicker interaction |

## Known Issues with FlaUI Driver

### 1. Switch Visibility (FIXED)
**Problem:** MAUI Switch on Windows reports `IsOffscreen=true` with zero bounds.  
**Solution:** Added fallback in `FlaUIMauiElement.Visible` to check for Toggle pattern support.

### 2. PropertyNotSupportedException (FIXED)
**Problem:** Some elements (like Slider) don't support the `Name` property.  
**Solution:** Updated `FlaUIMauiElement.Text` and `GetAttribute` to use safe property access (`ValueOrDefault`).

### 3. Slider/Stepper Keyboard Interaction
**Problem:** Setting slider/stepper values via keyboard doesn't work correctly.  
**Root Cause:** FlaUI sends keyboard commands but MAUI's WinUI controls may not respond as expected.  
**Status:** Needs platform-specific implementation or RangeValue pattern usage.

### 4. Picker Item Enumeration
**Problem:** FlaUI reports "Available items: 0" when querying picker options.  
**Root Cause:** MAUI Picker on Windows uses ComboBox which requires expansion to enumerate items.  
**Status:** Needs platform-specific handling for Windows ComboBox.

### 5. SearchBar Text Retrieval
**Problem:** `GetText()` returns `null` after entering text in SearchBar.  
**Root Cause:** SearchBar may use nested controls for text storage.  
**Status:** Needs investigation of SearchBar automation structure.

### 6. Editor Clear Operation
**Problem:** `Clear()` doesn't remove text from Editor control.  
**Root Cause:** May need different clear approach (Select All + Delete).  
**Status:** Needs platform-specific Clear implementation.

## FlaUI Driver Improvements Made

1. **`FlaUIMauiElement.Visible`** - Multi-level visibility checking:
   - Primary: `IsOffscreen` property
   - Fallback 1: Check bounding rectangle
   - Fallback 2: Check children bounds
   - Fallback 3: Toggle pattern support (for Switch)

2. **`FlaUIMauiElement.Text`** - Safe property access:
   - Try `Value` pattern (text inputs)
   - Try `RangeValue` pattern (sliders)
   - Fallback to `Name` with `ValueOrDefault`

3. **`FlaUIMauiElement.GetAttribute`** - Safe property access:
   - All property access uses `ValueOrDefault`
   - Exception handling for unsupported properties

4. **Windows interaction policy** - Background-safe local runs:
   - `BRINELL_WINDOWS_INTERACTION_MODE=semantic` is the default for MAUI/FlaUI
   - UIA patterns no longer foreground the AUT before `Invoke`, `SelectionItem`, `Toggle`, `LegacyIAccessible`, value, range, or screenshot operations
   - Pointer, global keyboard, foreground activation, and clipboard fallbacks are guarded by explicit policy flags
   - `BRINELL_WINDOWS_INTERACTION_MODE=interactive` preserves compatibility for tests that intentionally drive the active desktop

## Recommendations

### Short-term (Critical)
1. Prefer `SetText()`/`ValuePattern` over keystroke entry for Windows text controls
2. Use native invokable controls or accessible child buttons for test-critical tap/card surfaces
3. Investigate Slider RangeValue pattern gaps before falling back to keyboard input

### Medium-term
1. Add ComboBox expansion for Picker item enumeration
2. Improve DatePicker/TimePicker automation handling without global keyboard input where possible
3. Add comprehensive Windows automation tree diagnostics

### Long-term
1. Run physical gesture suites in VM/CI/isolated desktop sessions
2. Add control-specific FlaUI implementations for complex controls
3. Document MAUI -> WinUI control mapping

## Running Tests

```powershell
# Build and run all Windows tests
dotnet build testsnew/Brinell.Maui.UITests/Brinell.Maui.UITests.csproj
dotnet test testsnew/Brinell.Maui.UITests/Brinell.Maui.UITests.csproj --no-build

# Run specific test categories
dotnet test ... --filter "FullyQualifiedName~Button"
dotnet test ... --filter "FullyQualifiedName~Toggle"
dotnet test ... --filter "FullyQualifiedName~Entry"
```

## Files Modified

- `srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs` - Visibility and text property fixes
- `srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs` - Safe property access in BuildAutomationTree
