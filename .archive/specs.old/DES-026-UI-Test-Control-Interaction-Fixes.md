# DES-026: UI Test Control Interaction Fixes - Design

**Status:** Draft  
**Created:** January 20, 2026  
**Specification:** SPEC-026  
**Author:** Brinell Framework Team

---

## 1. Design Overview

This document details the implementation design for fixing 66 failing UI tests where control interactions (Toggle, Check, Slide) fail while state queries work correctly.

### 1.1 Design Goals

1. **Automatic scroll-into-view** before control interactions
2. **Robust slider value setting** using position-based clicks
3. **Reliable toggle operations** with state verification
4. **Minimal breaking changes** to existing API

### 1.2 Design Constraints

- Must work with WinAppDriver/Appium on Windows
- Cannot introduce platform-specific dependencies in base classes
- Must maintain fluent API pattern
- Performance: scroll only when necessary

---

## 2. Component Design

### 2.1 ScrollIntoView Enhancement

**Location:** `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`

```csharp
#region ScrollIntoView

/// <summary>
/// Scrolls the element into the visible viewport if not already visible.
/// </summary>
/// <param name="timeoutMs">Optional timeout for finding the element.</param>
/// <returns>The containing scope for fluent chaining.</returns>
public TScope ScrollIntoView(int? timeoutMs = null)
{
    return RunWithElement(nameof(ScrollIntoView), timeoutMs, element =>
    {
        ScrollIntoViewCore(element);
    });
}

/// <summary>
/// Core scroll implementation. Moves to element center to trigger scroll.
/// </summary>
/// <param name="element">The element to scroll into view.</param>
protected virtual void ScrollIntoViewCore(IMauiElement element)
{
    // Skip if already visible
    if (IsVisibleCore(element) == true)
    {
        return;
    }
    
    try
    {
        // Use Selenium Actions to move to element - this triggers scroll
        var actions = new OpenQA.Selenium.Interactions.Actions(Context.Driver);
        actions.MoveToElement(element.WrappedElement);
        actions.Perform();
        
        // Brief pause for scroll animation
        Thread.Sleep(100);
    }
    catch (Exception)
    {
        // Best effort - swallow scroll failures
    }
}

/// <summary>
/// Ensures element is scrolled into view before performing an action.
/// Called automatically by interaction methods.
/// </summary>
/// <param name="element">The element to ensure visibility for.</param>
protected void EnsureVisible(IMauiElement element)
{
    if (IsVisibleCore(element) != true)
    {
        ScrollIntoViewCore(element);
    }
}

#endregion
```

**Integration Point:** Update `RunWithElement` to call `EnsureVisible`:

```csharp
// In MauiControlBase.cs - modify existing RunWithElement
protected TScope RunWithElement(string action, int? timeoutMs, Action<IMauiElement> coreOperation)
{
    return Run(action, () =>
    {
        var element = FindElementWithWait(timeoutMs);
        EnsureVisible(element);  // ← NEW: Auto-scroll before action
        coreOperation(element);
    });
}

// Overload with value parameter
protected TScope RunWithElement<T>(string action, T value, int? timeoutMs, Action<IMauiElement> coreOperation)
{
    return Run(action, value, () =>
    {
        var element = FindElementWithWait(timeoutMs);
        EnsureVisible(element);  // ← NEW: Auto-scroll before action
        coreOperation(element);
    });
}
```

---

### 2.2 Slider Value Setting Redesign

**Location:** `srcnew/Brinell.Maui/Controls/Range/MauiSliderControl.cs`

**Problem:** Current `SetValueCore` uses `SendKeys` which doesn't work for sliders.

**Solution:** Click at calculated position within slider track.

```csharp
/// <summary>
/// Sets slider value by clicking at the calculated position on the slider track.
/// </summary>
/// <param name="element">The slider element.</param>
/// <param name="value">The target value.</param>
protected override void SetValueCore(IMauiElement element, double value)
{
    var min = GetMinimumCore(element) ?? 0;
    var max = GetMaximumCore(element) ?? 100;
    var range = max - min;
    
    if (range <= 0)
    {
        throw new InvalidOperationException($"Invalid slider range: min={min}, max={max}");
    }
    
    // Clamp value to valid range
    value = Math.Clamp(value, min, max);
    
    // Calculate target position as percentage of range
    var percentage = (value - min) / range;
    
    // Get element bounds
    var location = element.Location;
    var size = element.Size;
    
    // Calculate click position
    // Use 5% padding on each side to avoid edge issues
    var padding = (int)(size.Width * 0.05);
    var usableWidth = size.Width - (2 * padding);
    var targetX = location.X + padding + (int)(usableWidth * percentage);
    var centerY = location.Y + (size.Height / 2);
    
    // Perform click at target position
    var actions = new OpenQA.Selenium.Interactions.Actions(Context.Driver);
    actions.MoveToLocation(targetX, centerY);
    actions.Click();
    actions.Perform();
    
    // Brief pause for value to update
    Thread.Sleep(50);
}
```

**Alternative for Range Pattern (Windows UIA):**

```csharp
/// <summary>
/// Attempts to set value using RangeValue pattern, falls back to click.
/// </summary>
protected override void SetValueCore(IMauiElement element, double value)
{
    // Try RangeValue pattern first (Windows-specific)
    var isReadOnly = element.GetAttribute("RangeValue.IsReadOnly");
    if (isReadOnly == "False" || isReadOnly == "false")
    {
        try
        {
            // Some drivers support direct value setting via executeScript
            var script = $"arguments[0].SetValue({value})";
            Context.Driver.ExecuteScript(script, element.WrappedElement);
            return;
        }
        catch
        {
            // Fall through to click-based approach
        }
    }
    
    // Click-based approach (cross-platform)
    SetValueByClick(element, value);
}

private void SetValueByClick(IMauiElement element, double value)
{
    // ... click implementation as above
}
```

---

### 2.3 Stepper Control Enhancement

**Location:** `srcnew/Brinell.Maui/Controls/Range/MauiStepperControl.cs`

**Problem:** Steppers have increment/decrement buttons, not a draggable track.

**Solution:** Find and click the +/- buttons or use keyboard.

```csharp
/// <summary>
/// MAUI Stepper control for discrete value selection.
/// Uses increment/decrement child buttons for value changes.
/// </summary>
public class MauiStepperControl<TScope> : MauiRangeControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    // ... constructors ...

    /// <summary>
    /// Increments the stepper by clicking the increment button.
    /// </summary>
    protected override void IncrementCore(IMauiElement element)
    {
        // Try to find increment button child
        // MAUI Stepper on Windows has two RepeatButton children
        try
        {
            var incrementButton = FindChildButton(element, isIncrement: true);
            if (incrementButton != null)
            {
                incrementButton.Click();
                return;
            }
        }
        catch
        {
            // Fall through to base implementation
        }
        
        base.IncrementCore(element);
    }

    /// <summary>
    /// Decrements the stepper by clicking the decrement button.
    /// </summary>
    protected override void DecrementCore(IMauiElement element)
    {
        try
        {
            var decrementButton = FindChildButton(element, isIncrement: false);
            if (decrementButton != null)
            {
                decrementButton.Click();
                return;
            }
        }
        catch
        {
            // Fall through to base implementation
        }
        
        base.DecrementCore(element);
    }

    /// <summary>
    /// Sets value by repeatedly clicking increment/decrement.
    /// </summary>
    protected override void SetValueCore(IMauiElement element, double value)
    {
        var current = GetValueCore(element) ?? 0;
        var step = GetStepCore(element) ?? 1;
        var diff = value - current;
        var clicks = (int)Math.Abs(diff / step);
        
        // Limit to reasonable number of clicks
        clicks = Math.Min(clicks, 100);
        
        var increment = diff > 0;
        for (int i = 0; i < clicks; i++)
        {
            if (increment)
                IncrementCore(element);
            else
                DecrementCore(element);
            
            Thread.Sleep(20); // Brief pause between clicks
        }
    }

    private IMauiElement? FindChildButton(IMauiElement parent, bool isIncrement)
    {
        // MAUI Stepper structure:
        // - RepeatButton (decrement, typically first or has "-" text)
        // - TextBlock (value display)
        // - RepeatButton (increment, typically last or has "+" text)
        
        var buttons = Context.FindElements(
            parent, 
            Locator.ByClassName("RepeatButton"));
        
        if (buttons.Count >= 2)
        {
            // Assume first is decrement, last is increment
            return isIncrement ? buttons[^1] : buttons[0];
        }
        
        return null;
    }
}
```

---

### 2.4 Toggle Control Improvements

**Location:** `srcnew/Brinell.Maui/Controls/MauiToggleControlBase.cs`

**Problem:** Simple `Click()` doesn't always toggle the control state.

**Solution:** Add state verification and retry logic.

```csharp
/// <summary>
/// Performs toggle with state verification and retry.
/// </summary>
protected override void ToggleCore(IMauiElement element)
{
    var beforeState = IsCheckedCore(element);
    
    // Ensure visible before clicking
    EnsureVisible(element);
    
    // Attempt click
    element.Click();
    
    // Wait briefly for state change
    Thread.Sleep(100);
    
    // Verify state changed
    var afterState = IsCheckedCore(element);
    if (afterState == beforeState)
    {
        // Retry with Actions-based click (more reliable)
        RetryToggleWithActions(element);
    }
}

/// <summary>
/// Retry toggle using Selenium Actions API.
/// </summary>
private void RetryToggleWithActions(IMauiElement element)
{
    try
    {
        var actions = new OpenQA.Selenium.Interactions.Actions(Context.Driver);
        actions.MoveToElement(element.WrappedElement);
        actions.Click();
        actions.Perform();
        
        Thread.Sleep(100);
    }
    catch (Exception)
    {
        // Last resort: try clicking at element center
        var location = element.Location;
        var size = element.Size;
        var centerX = location.X + (size.Width / 2);
        var centerY = location.Y + (size.Height / 2);
        
        var actions = new OpenQA.Selenium.Interactions.Actions(Context.Driver);
        actions.MoveToLocation(centerX, centerY);
        actions.Click();
        actions.Perform();
    }
}
```

**Switch-Specific Override:**

**Location:** `srcnew/Brinell.Maui/Controls/Toggle/MauiSwitchControl.cs`

```csharp
/// <summary>
/// Override toggle for Switch which may need different click target.
/// </summary>
protected override void ToggleCore(IMauiElement element)
{
    // MAUI Switch on Windows: click on the thumb/track area
    // The clickable area might be offset from element bounds
    
    var beforeState = IsCheckedCore(element);
    EnsureVisible(element);
    
    // Try clicking at specific position on switch track
    var location = element.Location;
    var size = element.Size;
    
    // Click slightly inside the switch (not at exact edge)
    var clickX = location.X + (size.Width / 2);
    var clickY = location.Y + (size.Height / 2);
    
    var actions = new OpenQA.Selenium.Interactions.Actions(Context.Driver);
    actions.MoveToLocation(clickX, clickY);
    actions.Click();
    actions.Perform();
    
    Thread.Sleep(150); // Switch animation takes longer
    
    // Verify
    var afterState = IsCheckedCore(element);
    if (afterState == beforeState)
    {
        // Alternative: try Toggle pattern
        TryTogglePattern(element);
    }
}

private void TryTogglePattern(IMauiElement element)
{
    // Windows UIA Toggle pattern - if available
    var toggleState = element.GetAttribute("Toggle.ToggleState");
    if (toggleState != null)
    {
        try
        {
            // Invoke toggle through accessibility
            Context.Driver.ExecuteScript(
                "arguments[0].Toggle()", 
                element.WrappedElement);
        }
        catch
        {
            // Fallback - just click again
            element.Click();
        }
    }
}
```

---

### 2.5 CheckBox Click Target Fix

**Location:** `srcnew/Brinell.Maui/Controls/Toggle/MauiCheckBoxControl.cs`

**Problem:** Clicking on label next to checkbox doesn't toggle in some cases.

```csharp
/// <summary>
/// Override to ensure click targets the checkbox element, not adjacent label.
/// </summary>
protected override void ToggleCore(IMauiElement element)
{
    EnsureVisible(element);
    
    var beforeState = IsCheckedCore(element);
    
    // Click near the left side where checkbox visual is
    // (not center, which might hit associated label)
    var location = element.Location;
    var size = element.Size;
    
    // Click at 25% from left (typically where checkbox square is)
    var clickX = location.X + (int)(size.Width * 0.25);
    var clickY = location.Y + (size.Height / 2);
    
    var actions = new OpenQA.Selenium.Interactions.Actions(Context.Driver);
    actions.MoveToLocation(clickX, clickY);
    actions.Click();
    actions.Perform();
    
    Thread.Sleep(100);
    
    // Verify and retry if needed
    var afterState = IsCheckedCore(element);
    if (afterState == beforeState)
    {
        // Try direct element click as fallback
        element.Click();
    }
}
```

---

## 3. Required Imports

Add to files that use Actions:

```csharp
using OpenQA.Selenium.Interactions;
```

---

## 4. Interface Updates

### 4.1 IControlObject Enhancement

**Location:** `srcnew/Brinell.Core/Abstractions/Controls/IControlObject.cs`

```csharp
/// <summary>
/// Scrolls the element into the visible viewport.
/// </summary>
/// <param name="timeoutMs">Optional timeout for the operation.</param>
/// <returns>The containing scope for fluent chaining.</returns>
TScope ScrollIntoView(int? timeoutMs = null);
```

---

## 5. Implementation Sequence

### Phase 1: ScrollIntoView (Day 1)

| Step | Task | File |
|------|------|------|
| 1.1 | Add `IControlObject.ScrollIntoView()` interface method | `IControlObject.cs` |
| 1.2 | Implement `ScrollIntoView()` and `ScrollIntoViewCore()` | `MauiControlBase.cs` |
| 1.3 | Add `EnsureVisible()` helper | `MauiControlBase.cs` |
| 1.4 | Update `RunWithElement()` to call `EnsureVisible()` | `MauiControlBase.cs` |
| 1.5 | Build and run quick smoke test | Terminal |

### Phase 2: Slider Fix (Day 1)

| Step | Task | File |
|------|------|------|
| 2.1 | Override `SetValueCore()` with click-based approach | `MauiSliderControl.cs` |
| 2.2 | Run `SliderControlTests` | Terminal |
| 2.3 | Adjust click position if needed | `MauiSliderControl.cs` |

### Phase 3: Toggle Fixes (Day 2)

| Step | Task | File |
|------|------|------|
| 3.1 | Add retry logic to `ToggleCore()` | `MauiToggleControlBase.cs` |
| 3.2 | Override in `MauiSwitchControl` | `MauiSwitchControl.cs` |
| 3.3 | Override in `MauiCheckBoxControl` | `MauiCheckBoxControl.cs` |
| 3.4 | Run toggle test classes | Terminal |

### Phase 4: Stepper Fix (Day 2)

| Step | Task | File |
|------|------|------|
| 4.1 | Add button-finding logic | `MauiStepperControl.cs` |
| 4.2 | Override `IncrementCore()` and `DecrementCore()` | `MauiStepperControl.cs` |
| 4.3 | Override `SetValueCore()` for repeated clicks | `MauiStepperControl.cs` |
| 4.4 | Run `StepperControlTests` | Terminal |

### Phase 5: Full Test Run (Day 2)

| Step | Task | Command |
|------|------|---------|
| 5.1 | Run all UI tests | `dotnet test testsnew/Brinell.Maui.UITests` |
| 5.2 | Analyze remaining failures | Review output |
| 5.3 | Fix edge cases | As needed |

---

## 6. Testing Strategy

### 6.1 Unit Test Additions

None required - existing UI tests will validate the fixes.

### 6.2 Smoke Tests After Each Phase

```powershell
# Phase 1: Scroll
dotnet test testsnew/Brinell.Maui.UITests --filter "FullyQualifiedName~MainPageTests" --no-build

# Phase 2: Slider
dotnet test testsnew/Brinell.Maui.UITests --filter "FullyQualifiedName~SliderControlTests" --no-build

# Phase 3: Toggle
dotnet test testsnew/Brinell.Maui.UITests --filter "FullyQualifiedName~SwitchControlTests|CheckBoxControlTests" --no-build

# Phase 4: Stepper
dotnet test testsnew/Brinell.Maui.UITests --filter "FullyQualifiedName~StepperControlTests" --no-build

# Final
dotnet test testsnew/Brinell.Maui.UITests --no-build
```

---

## 7. Rollback Plan

If issues arise:

1. **Revert scroll integration**: Remove `EnsureVisible()` call from `RunWithElement()`
2. **Revert slider**: Restore base `SetValueCore()` behavior
3. **Revert toggle**: Remove retry logic from `ToggleCore()`

Each change is isolated and can be reverted independently.

---

## 8. Success Metrics

| Metric | Before | Target | Measurement |
|--------|--------|--------|-------------|
| Tests Passed | 151 | 200+ | `dotnet test` output |
| Tests Failed | 66 | <22 | `dotnet test` output |
| Pass Rate | 68% | 90%+ | Calculated |

---

**End of Design Document**
