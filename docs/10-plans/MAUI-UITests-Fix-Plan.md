# MAUI UI Tests Fix Plan

**Created:** January 2025  
**Status:** In Progress  
**Test Suite:** `testsnew/Brinell.Maui.UITests`

## 1. Executive Summary

| Metric | Value |
|--------|-------|
| Total Tests | 222 |
| Passed | 57 (25.7%) |
| Failed | 160 (72.1%) |
| Skipped | 5 (2.3%) |

The MAUI UI tests have a **72% failure rate** requiring root cause analysis and fixes.

## 2. Root Cause Analysis

### 2.1 Failure Categories

| Category | Count | Root Cause |
|----------|-------|------------|
| Tab Navigation | ~40 | Tab click works but page content not loading/ready |
| DateTime Controls | 12 | `GetTime()`/`GetDate()` returning null on Windows |
| Range Controls | ~15 | Slider/Stepper value parsing returning null |
| WebView Controls | ~8 | Navigation state (CanGoBack/CanGoForward) not detected |
| ActivityIndicator | 2 | `IsRunning=False` bindings making element not exist |
| Container Tests | ~25 | ContainersTab navigation failing |
| Element Not Found | ~58 | Elements exist but not found within timeout |

### 2.2 Detailed Analysis

#### Issue 1: Tab Navigation Failure

**Symptoms:**
- `NavigateToUserForm()` throws: "UserFormPage did not become ready after clicking FormsTab"
- `NavigateToMediaGallery()` throws: "MediaGalleryPage did not become ready after clicking MediaTab"
- `NavigateToContainerDemo()` throws: "ContainerDemoPage did not become ready"

**Root Cause:**
The TabbedPage on Windows MAUI renders tab items as `TabItem` elements where:
1. `AutomationId` set on `ContentPage` does NOT propagate to the Windows NavigationViewItem
2. The fallback XPath `//TabItem[@Name='{title}']` works for clicking
3. After click, content takes time to render but `WaitReady()` fails

**Evidence:**
- `MainPage.xaml` has: `<ContentPage Title="Forms" AutomationId="FormsTab">`
- Windows Automation tree shows `TabItem[@Name="Forms"]` but NOT `@AutomationId="FormsTab"`
- Known MAUI bug: [dotnet/maui#3996](https://github.com/dotnet/maui/issues/3996)

#### Issue 2: DateTime Control Values Returning Null

**Symptoms:**
```
Assert.Equal() Failure: Values differ
Expected: 10
Actual:   null
```

**Root Cause:**
The `MauiDatePickerControl.GetDate()` and `MauiTimePickerControl.GetTime()` methods try to read the `Date` or `Time` attribute, but on Windows, these may be exposed differently in the automation tree (e.g., as `Value` pattern or `Value` attribute with formatted string).

#### Issue 3: Range Controls Returning Null

**Symptoms:**
- Slider `GetValue()` returns null
- Stepper value not readable

**Root Cause:**
Windows MAUI Slider exposes value through UIA `RangeValue` pattern, not as a simple attribute. The control implementation may need to use `GetAttribute("RangeValue.Value")` or similar.

#### Issue 4: ActivityIndicator Not Found

**Symptoms:**
```
Assert.True() Failure
Expected: True
Actual:   False
```
For `ActivityIndicator_IsExists_ReturnsTrue()`.

**Root Cause:**
ActivityIndicator with `IsRunning="{Binding IsWebLoading}"` bound to `false` is collapsed/hidden in the automation tree on Windows. When not running, it doesn't exist in the tree.

## 3. Fix Implementation Plan

### Phase 1: Tab Navigation Fixes (Priority: Critical)

**File:** `srcnew/Brinell.Maui.CommunityToolkit/Controls/TabViewControl.cs`

1. After `Click()`, add a brief wait before returning
2. Verify the clicked tab becomes selected before proceeding

**File:** `testsnew/Brinell.Maui.UITests/AppiumFixture.cs`

1. Add retry logic for page navigation
2. Increase wait timeout for content rendering
3. Consider adding scroll-into-view for content checks

### Phase 2: DateTime Control Fixes (Priority: High)

**File:** `srcnew/Brinell.Maui/Controls/DateTime/MauiDatePickerControl.cs`  
**File:** `srcnew/Brinell.Maui/Controls/DateTime/MauiTimePickerControl.cs`

1. Read `Value` attribute as fallback
2. Parse formatted date/time strings
3. Handle Windows-specific date/time format strings

### Phase 3: Range Control Fixes (Priority: High)

**File:** `srcnew/Brinell.Maui/Controls/Range/MauiSliderControl.cs`  
**File:** `srcnew/Brinell.Maui/Controls/Range/MauiStepperControl.cs`

1. Check `RangeValue.Value` attribute
2. Check `Value` attribute
3. Parse numeric strings from `Text` attribute

### Phase 4: Test Adjustments (Priority: Medium)

1. **ActivityIndicator tests:** Only test when indicator is running
2. **WebView tests:** Add navigation waits before checking CanGoBack/Forward

## 4. Implementation Steps

### Step 1: DateTime Control Investigation (Completed - Needs Further Work)

**Finding:** Windows MAUI DatePicker and TimePicker are complex controls that open dialogs for value selection. The currently selected value is displayed as formatted text but the automation tree on Windows doesn't expose `Time` or `Date` attributes directly.

**Attempted Fixes:**
- Added fallback to check `Name` attribute
- Added `TryParseTimeString` and `TryParseDateString` helpers for various formats
- Added `value.value` attribute fallback

**Still Failing:** Tests still fail because:
1. `SetTime()` / `SetDate()` use keyboard input which may not work correctly on Windows
2. The value might be in a child element within the picker control
3. Windows MAUI exposes the value through UI Automation patterns, not simple attributes

**Next Steps:**
- Use Appium Inspector or WinAppDriver to examine the actual automation tree for DatePicker/TimePicker
- Check if values are in child elements that need explicit finding
- Consider implementing Windows-specific date/time setting via native calendar/clock dialogs

### Step 2: Skip Known-Failing Tests (Immediate Mitigation)

## 5. Test File Fixes Needed

| File | Issue | Fix |
|------|-------|-----|
| `ActivityIndicatorControlTests.cs` | Element not exist when not running | Skip test or use different indicator with IsRunning=true |
| `WebViewControlTests.cs` | CanGoBack false initially | Navigate somewhere first, then check |
| `TimePickerControlTests.cs` | GetTime returns null | Fix control implementation |
| `DatePickerControlTests.cs` | GetDate returns null | Fix control implementation |
| `ProgressBarControlTests.cs` | IsVisible returns false | Verify element visible in tree |

## 6. Verification Plan

After implementing fixes:

1. Run focused test category:
   ```
   dotnet test --filter "Category=UITest&Control=TimePicker"
   ```

2. Run full suite:
   ```
   dotnet test --no-build --logger "console;verbosity=normal"
   ```

3. Compare pass/fail counts to baseline (57 passed / 160 failed)

## 7. Success Criteria

| Metric | Current | Target |
|--------|---------|--------|
| Pass Rate | 25.7% | > 80% |
| Tab Navigation Failures | ~40 | 0 |
| DateTime Test Failures | 12 | 0 |
| Range Test Failures | ~15 | < 3 |

## 8. Appendix: Sample Test Output

### Before Fixes
```
[xUnit.net 00:00:36.36]     TimePicker_GetHours_ReturnsHours [FAIL]
[xUnit.net 00:00:36.36]       Assert.Equal() Failure: Values differ
[xUnit.net 00:00:36.36]       Expected: 10
[xUnit.net 00:00:36.36]       Actual:   null
```

### After Fixes (Expected)
```
Passed TimePicker_GetHours_ReturnsHours [2 s]
```
