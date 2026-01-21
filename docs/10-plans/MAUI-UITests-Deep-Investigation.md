# MAUI UITests Deep Investigation Plan

**Created:** January 20, 2026  
**Updated:** January 20, 2026  
**Status:** In Progress

---

## Executive Summary

After initial implementation attempts, several test categories are still failing. This document provides a deeper investigation into root causes and required fixes.

### Current Test Results (Last Run)
- **Total Tests:** 224
- **Passing:** 153 (68%)
- **Failing:** 66 (30%)
- **Skipped:** 5 (2%)

### Session Progress (January 20, 2026)

**DateTime Control Value Reading - FIXED ✅**

| Test | Before | After |
|------|--------|-------|
| TimePicker_GetTime_ReturnsTime | ❌ null | ✅ PASS |
| TimePicker_IsExists_ReturnsTrue | ✅ PASS | ✅ PASS |
| TimePicker_IsVisible_ReturnsTrue | ✅ PASS | ✅ PASS |

**Root Cause Discovered:**
Windows MAUI TimePicker/DatePicker elements embed values in **child elements** with Unicode control characters:

```xml
<Group AutomationId="PreferredTimePicker" ClassName="TimePicker">
  <Button AutomationId="FlyoutButton" Name=" ‎9‎:‎00‎ ‎AM time picker" />
</Group>

<Button AutomationId="BirthDatePicker" ClassName="CalendarDatePicker">
  <Text AutomationId="DateText" Name="‎20‎-‎Jan‎-‎01" />
</Button>
```

**Fixes Applied:**
1. Updated `MauiTimePickerControl.GetTimeCore()` to search for `FlyoutButton` child
2. Updated `MauiDatePickerControl.GetDateCore()` to search for `DateText` child  
3. Updated `TryParseTimeString()` to strip Unicode control characters (U+200E LTR marks)
4. Updated `TryParseDateString()` to strip Unicode control characters
5. Added "time picker" suffix removal from Name attribute

**Remaining Issues:**
- `SetTime()` doesn't work on Windows MAUI - requires flyout interaction
- Test expectations assume SetTime works, causing cascading failures

### Remaining Failures Summary (66 tests)

| Category | Count | Root Cause | Fix Priority |
|----------|-------|------------|--------------|
| **Slider SetValue** | ~6 | Windows driver doesn't support mouse Actions | Medium |
| **Stepper** | ~8 | Element not found with `AutomationId:QuantityStepper` | Medium |
| **Toggle Controls** | ~12 | Click/Toggle not persisting state | High |
| **SearchBar** | ~5 | Enter/GetText returning empty | Medium |
| **Picker** | ~6 | `GetItemsCore()` returns 0 items - needs flyout interaction | Medium |
| **List/Container** | ~12 | `Task_0` elements not found - missing in sample app | Low |
| **TimePicker Set** | ~5 | SetTime doesn't work on Windows flyout | Medium |

### Key Error Patterns

1. **"Currently only pen and touch pointer input source types are supported"**
   - Affects: Slider SetValue, SlideToMin/Max
   - Cause: Windows Appium driver doesn't support mouse-based W3C Actions API
   - **Research Finding:** The Appium Windows Driver has custom `windows: click` and `windows: clickAndDrag` extensions that bypass W3C Actions. However, the most reliable fix is using **keyboard arrow keys** to control sliders.
   - Fix: Use `SendKeys` with arrow keys to increment/decrement slider value

2. **"Index X is out of range. Available items: 0"**
   - Affects: Picker SelectByIndex, SelectByText
   - Cause: `GetItemsCore()` returns empty list - Picker not exposing items
   - Fix: Need to open picker flyout first to read items

3. **"Element not found with locator: AutomationId:QuantityStepper"**
   - Affects: All Stepper tests
   - Cause: Either control doesn't exist in sample app or wrong AutomationId
   - Fix: Verify sample app has Stepper control

4. **Toggle controls not persisting state after Click**
   - Affects: CheckBox, Switch, RadioButton
   - Cause: W3C Actions click API fails similar to sliders
   - **Research Finding:** Use `windows: click` driver extension or direct `element.Click()` without Actions fallback
   - Fix: Remove Actions-based retry, use `windows: click` extension

### Internet Research Summary (January 20, 2026)

**Key Findings from Appium Windows Driver Documentation:**

1. **WinAppDriver Not Maintained:** Microsoft has not maintained WinAppDriver for years. The Appium Windows Driver is just a thin wrapper over this closed-source server. Consider [NovaWindows Driver](https://github.com/AutomateThePlanet/appium-novawindows-driver) as alternative.

2. **Custom Extension Commands Available:**
   - `windows: click` - Direct mouse click bypassing W3C Actions
   - `windows: clickAndDrag` - Drag operations for sliders
   - `windows: keys` - Keyboard input with virtual key codes
   - `windows: scroll` - Mouse wheel scroll

3. **Executing Extensions in C#:**
```csharp
// Dotnet example for custom commands
driver.ExecuteScript("windows: click", new Dictionary<string, object>() {
    {"x", 100},
    {"y", 200}
});
```

4. **UI Automation Patterns:**
   - `RangeValueProvider` pattern exposes `SetValue` method for sliders
   - Toggle controls use `TogglePattern` with `Toggle()` method
   - These patterns may be more reliable than click-based automation

**WinAppDriver Issues (1,100+ open):**
- Multiple reports of Actions API issues
- Scrolling and mouse movement problems documented
- Click not registering on certain control types

---

## Issue 1: Multiple App Instances Opening

### Symptom
User observed two app windows opening during test runs.

### Root Cause Analysis

The test infrastructure uses xUnit Collection fixtures for sharing:

```csharp
// AppiumCollection.cs - Correct usage
[CollectionDefinition("Appium")]
public class AppiumCollection : ICollectionFixture<AppiumFixture> { }

// Test classes use [Collection("Appium")] - should share ONE fixture
```

**Potential Causes:**

1. **Test Project Build Issues**
   - If `testhost.exe` or old app processes weren't killed, the previous app instance may remain open
   - Windows Driver creates a new session without closing old apps

2. **Multiple Test Assemblies Running**
   - If both old (`tests/`) and new (`testsnew/`) projects run simultaneously
   - Each would create its own fixture/driver/app instance

3. **Failed Dispose Not Closing App**
   - The `MauiTestContext.Dispose()` calls `_rawDriver.Quit()` but doesn't explicitly terminate the app
   - If Quit() fails silently, the app process remains

4. **Windows Driver Session Management**
   - WinAppDriver may launch a new app while old instance still runs
   - Not using "attach to existing window" capability

### Investigation Steps

```powershell
# Check for orphaned processes before test run
Get-Process | Where-Object { $_.Name -like "*Brinell*" -or $_.Name -like "*testhost*" }

# Kill all before running tests
taskkill /F /IM "Brinell.Samples.Maui.App.exe" 2>$null
taskkill /F /IM "testhost.exe" 2>$null
```

### Recommended Fixes

1. **Add explicit app termination in fixture dispose:**

```csharp
// MauiTestContext.Dispose()
protected virtual void Dispose(bool disposing)
{
    if (_disposed) return;
    
    if (disposing)
    {
        try
        {
            // Try to terminate app first
            var appId = _rawDriver?.Capabilities?.GetCapability("app")?.ToString();
            if (!string.IsNullOrEmpty(appId))
            {
                try { _rawDriver.Close(); } catch { }
            }
            _rawDriver?.Quit();
        }
        catch { }
    }
    
    _disposed = true;
}
```

2. **Add mutex/lock to prevent parallel fixture creation:**

```csharp
// AppiumFixture.cs
private static readonly SemaphoreSlim _lock = new(1, 1);
private static AppiumFixture? _instance;

public AppiumFixture()
{
    _lock.Wait();
    try
    {
        if (_instance != null)
            throw new InvalidOperationException("Fixture already exists!");
        _instance = this;
        // ... existing init
    }
    finally { _lock.Release(); }
}
```

---

## Issue 2: DateTime Controls Return Null

### Symptom
- `TimePicker_GetTime_ReturnsTime` fails: "Time should not be null"
- `DatePicker_GetDate_ReturnsDate` fails: "Date should not be null"
- Element IS found (IsExists passes, IsVisible passes)
- GetTime()/GetDate() return null

### Current Implementation Analysis

```csharp
// MauiTimePickerControl.GetTimeCore()
protected TimeSpan? GetTimeCore(IMauiElement? element)
{
    // Tries these attributes in order:
    // 1. "Time" - MAUI mobile attribute
    // 2. "SelectedTime" - alternative
    // 3. "Value" - generic value
    // 4. "value.value" - Windows UIA RangeValue pattern
    // 5. "Name" - display name (added as fix)
    // 6. element.Text - raw text content
}
```

### Windows MAUI TimePicker Automation Tree

On Windows, MAUI TimePicker renders as a complex control:
- **Container:** Custom control with AutomationId
- **Display:** TextBlock showing formatted time (e.g., "9:00 AM")
- **Popup:** Opens on click with hour/minute/AM-PM selectors

**The actual value is NOT on the container element** - it's in a child TextBlock!

### Deep Investigation Required

```powershell
# Use Appium Inspector or Windows Accessibility Insights to examine:
# 1. What element type is the TimePicker container?
# 2. What child elements exist?
# 3. What attributes are exposed on each element?
# 4. Where is the displayed time value located?
```

### Hypothesis: Need to Find Child Element

```csharp
// The fix may need to find a child text element:
protected TimeSpan? GetTimeCore(IMauiElement? element)
{
    if (element == null) return null;

    // First try direct attributes (in case platform exposes them)
    var timeAttr = element.GetAttribute("Time") ?? element.GetAttribute("Value");
    if (!string.IsNullOrEmpty(timeAttr) && TimeSpan.TryParse(timeAttr, out var ts))
        return ts;

    // *** NEW: Try finding child TextBlock containing the time ***
    try
    {
        var children = element.FindElements(By.XPath(".//*"));
        foreach (var child in children)
        {
            var text = child.Text ?? child.GetAttribute("Name");
            if (!string.IsNullOrEmpty(text) && TryParseTimeString(text, out var timeValue))
            {
                return timeValue;
            }
        }
    }
    catch { }

    // Try element's own Name attribute
    var nameAttr = element.GetAttribute("Name");
    if (!string.IsNullOrEmpty(nameAttr) && TryParseTimeString(nameAttr, out var nameTime))
        return nameTime;

    return null;
}
```

### Test Data Issue

The tests use hardcoded expected values but the app initializes with different defaults:

```csharp
// UserProfile.cs defaults:
public TimeSpan PreferredTime { get; set; } = new TimeSpan(9, 0, 0);  // 9:00 AM
public DateTime BirthDate { get; set; } = DateTime.Today.AddYears(-25);

// Tests expect (from TimePickerControlTests.cs):
// - Hours: 10 (line 108)
// - Minutes: 45 (line 126)  
// - Time: 09:15 (line 146)
// - SetTime to 16:00 then assert (line 163)
```

**Problem:** Tests either:
1. Need to SET the time first, THEN read it back
2. Or read whatever is currently set (9:00 AM default)

---

## Issue 3: Tab Navigation Failures

### Symptom
~40 tests fail when navigation to the correct tab doesn't work or page doesn't load.

### Root Cause Analysis

```csharp
// Current navigation in AppiumFixture.cs
public void NavigateToUserForm()
{
    _appShell.FormsTab.Click();  // Click the tab
    if (!_userFormPage.WaitReady(5000))  // Wait for page
    {
        throw new InvalidOperationException(...);
    }
}
```

**Potential Issues:**

1. **Click Not Registering**
   - MAUI Shell TabBar may need specific click coordinates
   - Tab may need double-click or tap action

2. **Tab Selection State Not Checked**
   - Click happens but tab doesn't select
   - No verification tab is actually selected before proceeding

3. **Page Ready Detection Failure**
   - WaitReady checks for a specific element
   - That element may load before the controls being tested

4. **Scroll Position Issues**
   - Forms page has ScrollView with many controls
   - Some controls may be off-screen initially

### Recommended Fixes

```csharp
// Add post-click verification:
public void NavigateToUserForm()
{
    // Ensure we're not already on the page
    if (_userFormPage.IsReady()) return;
    
    // Click tab and verify it becomes selected
    _appShell.FormsTab.Click();
    _appShell.FormsTab.WaitSelected(true, timeoutMs: 2000);
    
    // Wait for page content
    if (!_userFormPage.WaitReady(5000))
    {
        // Debug: capture page source
        var pageSource = Context.Driver.PageSource;
        throw new InvalidOperationException(
            $"UserFormPage not ready. Available elements: {pageSource.Substring(0, 500)}");
    }
}
```

---

## Issue 4: ActivityIndicator Not Found

### Symptom
- `ActivityIndicator_IsExists_ReturnsTrue` fails
- `ActivityIndicator_IsVisible_ReflectsState` fails

### Root Cause

ActivityIndicator in MAUI only renders when `IsRunning="True"`. If bound to a ViewModel property that defaults to false, the control doesn't exist in the visual tree.

**Check MainPage.xaml for ActivityIndicator definition:**
- Is there actually an ActivityIndicator with the expected AutomationId?
- What is `IsRunning` bound to?
- When is it set to true?

### Investigation

```powershell
# Search for ActivityIndicator in sample app
Select-String -Path "samples/Brinell.Samples.Maui.App/**/*.xaml" -Pattern "ActivityIndicator"
```

### Potential Fixes

1. **Add static ActivityIndicator to sample app:**
```xaml
<ActivityIndicator IsRunning="True" 
                   AutomationId="TestActivityIndicator" 
                   IsVisible="True" />
```

2. **Update tests to trigger IsRunning first:**
```csharp
[Fact]
public void ActivityIndicator_IsVisible_WhenRunning()
{
    // Click button that starts loading
    _fixture.MainPage.StartLoadingButton.Click();
    
    // NOW the indicator should exist
    Assert.True(_fixture.MainPage.LoadingIndicator.IsExists());
}
```

---

## Issue 5: ProgressBar IsVisible Returns False

### Symptom
- `ProgressBar_IsVisible_ReturnsTrue` fails even though ProgressBar exists

### Root Cause Analysis

ProgressBar may:
1. Have `IsVisible="False"` in XAML
2. Be collapsed (Height=0) until progress starts
3. Be on a tab that isn't currently selected

### Investigation
Check what page the ProgressBar is on and ensure test navigates there first.

---

## Issue 6: WebView Navigation State

### Symptom
- `WebView_CanGoBack_ReturnsState` fails
- `WebView_CanGoForward_ReturnsState` fails

### Root Cause

WebView navigation history state isn't exposed through simple attributes. These states are maintained internally by the WebView and require:
1. JavaScript bridge to query
2. Multiple page navigations to build history
3. Waiting for navigation to complete

### Required Investigation

1. How is WebView exposed in Windows MAUI automation tree?
2. Can we access CanGoBack/CanGoForward properties?
3. Do we need to execute JavaScript to get this info?

---

## Issue 7: Range Controls (Slider/Stepper)

### Slider Implementation

Current `MauiSliderControl.SetValueCore()` uses click-based positioning:

```csharp
protected override void SetValueCore(IMauiElement element, double value)
{
    // Calculate percentage of range
    // Calculate click position
    // Perform click at target position
}
```

**Potential Issues:**
1. Click coordinates may be miscalculated for MAUI's rendering
2. Windows may have different slider thumb behavior
3. RangeValue pattern might be available but not used for reading

### Stepper Implementation

Stepper consists of increment/decrement buttons. Need to verify:
1. Can we find the stepper container?
2. Can we find increment/decrement buttons?
3. Is RangeValue pattern exposed for reading current value?

---

## Priority Investigation Order

### Priority 1: Multiple App Instances (CRITICAL)
- Affects test reliability and cleanup
- May cause resource leaks
- Easy to diagnose with process monitoring

### Priority 2: DateTime Controls (HIGH)
- 12 failing tests
- Core functionality - date/time selection is common
- Need Appium Inspector to examine automation tree

### Priority 3: Tab Navigation (HIGH)  
- ~40 failing tests depend on correct page
- May be simple fix with verification

### Priority 4: ActivityIndicator/ProgressBar (MEDIUM)
- Sample app may need updates
- May be missing controls or wrong configuration

### Priority 5: WebView (LOW)
- Complex control with limited automation support
- May need platform-specific workarounds or skip

---

## Investigation Tools Needed

1. **Appium Inspector**
   - View live automation tree
   - Inspect element attributes
   - Try different locator strategies

2. **Windows Accessibility Insights**
   - See UIA tree structure
   - Identify available patterns
   - Check exposed properties

3. **Process Monitor**
   - Track app launches/closes
   - Identify orphaned processes

---

## Action Items

| # | Item | Priority | Owner | Status |
|---|------|----------|-------|--------|
| 1 | Kill orphaned processes before test run | P1 | - | Not Started |
| 2 | Add app termination in fixture Dispose | P1 | - | Not Started |
| 3 | Use Appium Inspector on TimePicker | P2 | - | Not Started |
| 4 | Implement child element search for DateTime | P2 | - | Not Started |
| 5 | Add tab selection verification | P3 | - | Not Started |
| 6 | Add ActivityIndicator to sample app | P4 | - | Not Started |
| 7 | Check ProgressBar page location | P4 | - | Not Started |
| 8 | Investigate WebView automation | P5 | - | Not Started |

---

## Appendix: Test Failure Categories

| Category | Count | Root Cause | Fix Complexity |
|----------|-------|------------|----------------|
| DateTime (Time/Date) | 12 | Value in child element | Medium |
| Tab Navigation | ~40 | No selection verification | Low |
| ActivityIndicator | 2 | Missing in sample app | Low |
| ProgressBar | 1 | Wrong page or hidden | Low |
| WebView | 4 | Complex automation | High |
| Range Controls | ~15 | SetValue issues | Medium |
| Multiple App Issue | - | Fixture lifecycle | Medium |

---

## Next Steps

1. **Immediate:** Run tests with process cleanup first
2. **Today:** Use Appium Inspector to examine DateTime controls
3. **This Week:** Implement child element search for DateTime
4. **This Week:** Add tab selection verification
5. **Next Week:** Address remaining control issues

