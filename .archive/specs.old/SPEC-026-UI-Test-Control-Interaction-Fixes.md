# SPEC-026: UI Test Control Interaction Fixes

**Status:** Draft  
**Created:** January 19, 2026  
**Priority:** High  
**Related:** SPEC-025 (UI Tests), SPEC-024 (MAUI Control Objects), SPEC-023 (TabbedPage Automation)  
**Author:** Brinell Framework Team

---

## 1. Executive Summary

This specification addresses the remaining UI test failures after implementing SPEC-025 UI tests. While state querying methods work correctly, control interaction methods (Toggle, Check, Click, SlideToValue) fail to properly interact with MAUI controls on Windows.

### 1.1 Current Test Status

| Metric | Count | Percentage |
|--------|-------|------------|
| **Total Tests** | 222 | 100% |
| **Passed** | 151 | 68% |
| **Failed** | 66 | 30% |
| **Skipped** | 5 | 2% |

### 1.2 Problem Pattern

All failures follow the same pattern:
- ✅ **State queries work:** `IsExists()`, `IsChecked()`, `GetValue()`, `IsVisible()`
- ❌ **Interactions fail:** `Toggle()`, `Check()`, `Click()`, `SlideToValue()`, `SetValue()`

### 1.3 Root Causes

1. **Controls not scrolled into view** before interaction
2. **SetValueCore uses SendKeys** which doesn't work for Slider/Range controls
3. **ToggleCore uses Click()** which may not work for all toggle control types
4. **Element visibility** - controls below fold aren't visible without scrolling

---

## 2. Failure Analysis

### 2.1 Toggle Control Failures (Switch, CheckBox, RadioButton)

**Affected Tests:**
- `SwitchControlTests`: 5/10 failing
- `CheckBoxControlTests`: 4/9 failing  
- `RadioButtonControlTests`: 4/10 failing

**Current Implementation:**
```csharp
// MauiToggleControlBase.cs
protected virtual void ToggleCore(IMauiElement element)
{
    element.Click();  // ← Simple click doesn't always work on Windows
}
```

**Issues:**
1. `Click()` may not trigger toggle on Windows MAUI controls
2. Switch controls may need specific accessibility actions
3. Elements may not be visible/scrolled into view

**Evidence:**
```
Assert.True() Failure
Expected: True
Actual:   False  // IsOn() returns false after TurnOn() call
```

### 2.2 Range Control Failures (Slider, Stepper)

**Affected Tests:**
- `SliderControlTests`: 4/9 failing
- `StepperControlTests`: ~4 failing

**Current Implementation:**
```csharp
// MauiRangeControlBase.cs
protected virtual void SetValueCore(IMauiElement element, double value)
{
    // Default implementation: try to use SendKeys
    element.Clear();
    element.SendKeys(value.ToString());  // ← Doesn't work for Slider
}
```

**Issues:**
1. Sliders don't accept keyboard input - need drag gestures or accessibility API
2. Steppers should use increment/decrement buttons, not SendKeys
3. Windows sliders expose `RangeValue` pattern for programmatic value setting

**Evidence:**
```
Slider_SlideToPercentage_SetsPercentage [FAIL]
Assert.True() Failure - Expected: True, Actual: False
```

### 2.3 Visibility/Scrolling Issues

**Affected Tests:**
- Any test on FormsView where controls are below fold
- Switch, CheckBox, Slider, Stepper, DatePicker, TimePicker tests

**Issues:**
1. FormsView has multiple frames - controls at bottom aren't visible
2. `IsVisible()` returns `false` for off-screen elements
3. Interactions fail silently on non-visible elements

---

## 3. Proposed Solutions

### 3.1 Solution A: Scroll Into View Before Interaction

**Scope:** All control interactions

**Implementation:**
Add `ScrollIntoView()` method to `MauiControlBase` and call it before interactions.

```csharp
// MauiControlBase.cs - Add new method
protected virtual void ScrollIntoViewCore(IMauiElement element)
{
    // Try JavaScript scroll for web views
    // Try UIA scroll pattern for native views
    // Fallback: move to element center
    var location = element.Location;
    var size = element.Size;
    
    // Use Actions to scroll element into view
    // Platform-specific implementation needed
}

// Update RunWithElement to scroll first
protected TScope RunWithElement(string action, int? timeoutMs, Action<IMauiElement> coreOperation)
{
    return Run(action, () =>
    {
        var element = FindElementWithWait(timeoutMs);
        ScrollIntoViewCore(element);  // ← Add this
        coreOperation(element);
    });
}
```

### 3.2 Solution B: Platform-Specific Toggle Implementation

**Scope:** Toggle controls (Switch, CheckBox, RadioButton)

**Implementation:**
Override `ToggleCore` in platform-specific controls to use accessibility APIs.

```csharp
// MauiSwitchControl.cs - Override for Windows
protected override void ToggleCore(IMauiElement element)
{
    // Try Toggle pattern first (Windows UIA)
    var togglePattern = element.GetAttribute("Toggle.ToggleState");
    if (togglePattern != null)
    {
        // Use Toggle pattern
        // element.PerformAction("Toggle");
    }
    
    // Fallback to click with scroll into view
    ScrollIntoView();
    base.ToggleCore(element);
}
```

### 3.3 Solution C: Platform-Specific Range Value Implementation

**Scope:** Range controls (Slider, Stepper)

**Implementation:**
Override `SetValueCore` in slider to use RangeValue pattern or drag gestures.

```csharp
// MauiSliderControl.cs - Override for Windows
protected override void SetValueCore(IMauiElement element, double value)
{
    // Option 1: Try RangeValue pattern (Windows UIA)
    var rangePattern = element.GetAttribute("RangeValue.IsReadOnly");
    if (rangePattern == "false" || rangePattern == "False")
    {
        // Use RangeValue pattern to set value directly
        // This requires Appium's mobile:setValue or similar
    }
    
    // Option 2: Calculate position and perform drag gesture
    var min = GetMinimumCore(element) ?? 0;
    var max = GetMaximumCore(element) ?? 100;
    var range = max - min;
    if (range <= 0) return;
    
    var percentage = (value - min) / range;
    var size = element.Size;
    var location = element.Location;
    
    var startX = location.X + (int)(size.Width * 0.1);  // Start near left
    var targetX = location.X + (int)(size.Width * percentage);
    var y = location.Y + (size.Height / 2);
    
    // Perform drag from current position to target
    // Using TouchAction or W3C Actions
}
```

### 3.4 Solution D: Use Test Helpers with Explicit Scrolling

**Scope:** Test code

**Implementation:**
Update tests to explicitly scroll to controls before interaction.

```csharp
// SliderControlTests.cs
[Fact]
public Task Slider_SlideToPercentage_SetsPercentage()
{
    // Arrange - Scroll to control first
    Page.FontSizeSlider.ScrollIntoView();
    
    // Act
    Page.FontSizeSlider.SlideToPercentage(75);
    
    // Assert
    var value = Page.FontSizeSlider.GetValue();
    Assert.True(value.HasValue);
    // ...
}
```

---

## 4. Recommended Approach

### 4.1 Phase 1: Scroll Into View (High Impact)

Add automatic scroll-into-view before all control interactions:

| Task | File | Change |
|------|------|--------|
| 4.1.1 | `MauiControlBase.cs` | Add `ScrollIntoView()` and `ScrollIntoViewCore()` methods |
| 4.1.2 | `MauiControlBase.cs` | Update `RunWithElement()` to call scroll before operation |
| 4.1.3 | `IMauiElement.cs` | Add scroll support to element interface if needed |
| 4.1.4 | `MauiElement.cs` | Implement scroll using Actions API |

### 4.2 Phase 2: Fix Slider SetValue (Medium Impact)

Replace SendKeys with drag gesture for sliders:

| Task | File | Change |
|------|------|--------|
| 4.2.1 | `MauiSliderControl.cs` | Override `SetValueCore` with drag gesture |
| 4.2.2 | `MauiRangeControlBase.cs` | Make `SetValueCore` more robust |
| 4.2.3 | `MauiStepperControl.cs` | Override to use increment/decrement child buttons |

### 4.3 Phase 3: Fix Toggle Operations (Medium Impact)

Improve toggle reliability:

| Task | File | Change |
|------|------|--------|
| 4.3.1 | `MauiToggleControlBase.cs` | Add retry logic to `ToggleCore` |
| 4.3.2 | `MauiSwitchControl.cs` | Override with Windows-specific toggle |
| 4.3.3 | `MauiCheckBoxControl.cs` | Verify click targets checkbox, not label |

### 4.4 Phase 4: Update Tests (Low Impact)

Adjust tests for edge cases:

| Task | File | Change |
|------|------|--------|
| 4.4.1 | Various test files | Add explicit scroll calls where needed |
| 4.4.2 | Various test files | Adjust timing/tolerance for flaky tests |

---

## 5. Technical Specifications

### 5.1 ScrollIntoView Implementation

```csharp
/// <summary>
/// Scrolls the element into the visible viewport.
/// </summary>
/// <param name="timeoutMs">Optional timeout for the operation.</param>
/// <returns>The containing scope for fluent chaining.</returns>
public TScope ScrollIntoView(int? timeoutMs = null)
{
    return RunWithElement(nameof(ScrollIntoView), timeoutMs, element =>
    {
        ScrollIntoViewCore(element);
    });
}

protected virtual void ScrollIntoViewCore(IMauiElement element)
{
    // Check if element is already visible
    if (IsVisibleCore(element) == true)
    {
        return;
    }
    
    // Try multiple approaches:
    // 1. Use ScrollPattern if available (Windows UIA)
    // 2. Use TouchActions to scroll container
    // 3. Use JavaScript scroll for WebView
    // 4. Fallback: Move to element location
    
    try
    {
        // Approach: Use Appium's mobile:scroll or similar
        // Or use Actions to move to element
        var actions = new Actions(Driver);
        actions.MoveToElement(element.WrappedElement);
        actions.Perform();
    }
    catch
    {
        // Swallow - best effort scroll
    }
}
```

### 5.2 Slider SetValue with Drag

```csharp
protected override void SetValueCore(IMauiElement element, double value)
{
    var min = GetMinimumCore(element) ?? 0;
    var max = GetMaximumCore(element) ?? 100;
    var range = max - min;
    
    if (range <= 0)
    {
        throw new InvalidOperationException($"Invalid slider range: min={min}, max={max}");
    }
    
    // Clamp value to range
    value = Math.Max(min, Math.Min(max, value));
    
    // Calculate target position as percentage
    var percentage = (value - min) / range;
    
    var size = element.Size;
    var location = element.Location;
    
    // Calculate x positions (with padding to avoid edge issues)
    var padding = (int)(size.Width * 0.05);
    var usableWidth = size.Width - (2 * padding);
    var targetX = location.X + padding + (int)(usableWidth * percentage);
    var centerY = location.Y + (size.Height / 2);
    
    // Perform click at target position (simpler than drag)
    var actions = new Actions(Context.Driver);
    actions.MoveToLocation(targetX, centerY);
    actions.Click();
    actions.Perform();
}
```

### 5.3 Toggle with Verification

```csharp
protected override void ToggleCore(IMauiElement element)
{
    var beforeState = IsCheckedCore(element);
    
    // Scroll into view first
    ScrollIntoViewCore(element);
    
    // Perform click
    element.Click();
    
    // Wait briefly for state change
    Thread.Sleep(100);
    
    // Verify state changed (or retry)
    var afterState = IsCheckedCore(element);
    if (afterState == beforeState)
    {
        // Retry with different approach
        var actions = new Actions(Context.Driver);
        actions.MoveToElement(element.WrappedElement);
        actions.Click();
        actions.Perform();
    }
}
```

---

## 6. Acceptance Criteria

### 6.1 Pass Rate Target

| Metric | Current | Target |
|--------|---------|--------|
| Total Passed | 151 (68%) | 200+ (90%) |
| Total Failed | 66 (30%) | <22 (10%) |

### 6.2 Specific Control Targets

| Control Type | Current Pass Rate | Target |
|--------------|-------------------|--------|
| Switch | 50% | 90% |
| CheckBox | 56% | 90% |
| RadioButton | 60% | 90% |
| Slider | 56% | 90% |
| Stepper | ~50% | 90% |

### 6.3 Test Verification

After implementation, run:
```powershell
dotnet test testsnew/Brinell.Maui.UITests --no-build
```

Expected output:
```
Test summary: total: 222, failed: <22, passed: >200, skipped: 5
```

---

## 7. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Scroll breaks existing tests | Low | Medium | Run full suite after each change |
| Drag gesture platform-specific | High | Medium | Use Actions API for cross-platform |
| Toggle retry causes flakiness | Medium | Low | Add configurable retry count |
| Performance impact | Low | Low | Scroll only when not visible |

---

## 8. Implementation Order

1. **Phase 1: ScrollIntoView** - Highest impact, fixes visibility issues
2. **Phase 2: Slider SetValue** - Fixes all slider interaction tests
3. **Phase 3: Toggle verification** - Fixes switch/checkbox/radio tests
4. **Phase 4: Test adjustments** - Handle remaining edge cases

---

## 9. Files to Modify

### Core Framework (srcnew/Brinell.Maui/Controls/)

| File | Changes |
|------|---------|
| `MauiControlBase.cs` | Add `ScrollIntoView()`, update `RunWithElement()` |
| `MauiToggleControlBase.cs` | Add verification/retry to `ToggleCore()` |
| `MauiRangeControlBase.cs` | Improve `SetValueCore()` base implementation |
| `Range/MauiSliderControl.cs` | Override `SetValueCore()` with drag gesture |
| `Range/MauiStepperControl.cs` | Override to find and click +/- buttons |
| `Toggle/MauiSwitchControl.cs` | Override `ToggleCore()` if needed |

### Element Interface (srcnew/Brinell.Maui/)

| File | Changes |
|------|---------|
| `IMauiElement.cs` | Add `ScrollIntoView()` if needed |
| `MauiElement.cs` | Implement scroll action |

### Tests (testsnew/Brinell.Maui.UITests/)

| File | Changes |
|------|---------|
| Various test files | Minimal - mostly automatic via framework |

---

## 10. Appendix: Failing Test Details

### A.1 Switch Tests (5 failures)

```
Switch_IsVisible_ReturnsTrue - Assert.True() Expected: True, Actual: False
Switch_TurnOn_SetsSwitchToOn - Assert.True() Expected: True, Actual: False
Switch_Toggle_InvertsState - Assert.NotEqual() Expected: Not False, Actual: False
Switch_AssertOn_PassesWhenOn - AssertionException: Expected switch to be on
Switch_TurnOn_IsIdempotent - Assert.True() Expected: True, Actual: False
```

### A.2 CheckBox Tests (4 failures)

```
CheckBox_Check_SetsCheckedToTrue - Assert.True() Expected: True, Actual: False
CheckBox_Toggle_InvertsState - Assert.NotEqual() Expected: Not False, Actual: False
CheckBox_AssertChecked_PassesWhenChecked - AssertionException: Expected element to be checked
CheckBox_MultipleControls_OperateIndependently - Assert.True() Expected: True, Actual: False
```

### A.3 Slider Tests (4 failures)

```
Slider_SlideToPercentage_SetsPercentage - Assert.True() Expected: True, Actual: False
Slider_SlideToMinimum_SetsToMin - Assert.True() Expected: True, Actual: False
Slider_SlideToMaximum_SetsToMax - Assert.True() Expected: True, Actual: False
Slider_MultipleControls_OperateIndependently - Assert.True() Expected: True, Actual: False
```

---

**End of Specification**
