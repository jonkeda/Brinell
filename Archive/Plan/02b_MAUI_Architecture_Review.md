# MAUI Architecture Review (02b)

## Overview

This document addresses three architectural questions raised during review of the MAUI Enhancement Plan (02).

---

## Question 1: Assert Methods Scope for Controls

### Current State

The MAUI `ControlBase` already includes text assertion methods inherited from the Is/Wait/Check/Assert pattern:

```csharp
// In ControlBase.cs - Already implemented
public virtual void AssertTextEquals(string expected, string? message = null)
public virtual void AssertTextContains(string expected, string? message = null)
public virtual void AssertExists(string? message = null)
public virtual void AssertVisible(string? message = null)
public virtual void AssertEnabled(string? message = null)
public virtual void AssertDisabled(string? message = null)
```

### Missing Assert Methods

The following control-specific assertions are NOT yet in scope:

| Assert Method | Description | Applicable Controls |
|--------------|-------------|---------------------|
| `AssertIsChecked` | Checkbox/switch is checked | CheckBox, Switch |
| `AssertIsUnchecked` | Checkbox/switch is unchecked | CheckBox, Switch |
| `AssertValue` | Numeric/slider value | Slider, Stepper, ProgressBar |
| `AssertSelectedItem` | Selected item in list | Picker, CollectionView |
| `AssertSelectedIndex` | Selected index in list | Picker, CollectionView |
| `AssertItemCount` | Number of items in collection | CollectionView, ListView |
| `AssertPlaceholder` | Placeholder text visible | Entry, Editor |
| `AssertIsReadOnly` | Control is read-only | Entry, Editor |
| `AssertHasError` | Validation error visible | Entry (custom) |
| `AssertTextEmpty` | Text is empty string | Entry, Editor, Label |
| `AssertTextNotEmpty` | Text has content | Entry, Editor, Label |

### Recommendation

**Add control-specific assertions to each control type.** Example for `CheckBoxControl`:

```csharp
public class CheckBoxControl : ControlBase, ICheckBox
{
    // ... existing methods ...
    
    public void AssertIsChecked(string? message = null)
    {
        CheckVisible(expected: true);
        if (!IsChecked())
        {
            ThrowAssertionFailed("IsChecked", "false", "true",
                message ?? $"Expected CheckBox '{AutomationId}' to be checked.");
        }
        LogAssertPass("IsChecked", "true", "true");
    }
    
    public void AssertIsUnchecked(string? message = null)
    {
        CheckVisible(expected: true);
        if (IsChecked())
        {
            ThrowAssertionFailed("IsUnchecked", "true", "false",
                message ?? $"Expected CheckBox '{AutomationId}' to be unchecked.");
        }
        LogAssertPass("IsUnchecked", "false", "false");
    }
}
```

### Action Items

- [ ] Phase 2: Add `AssertIsChecked`/`AssertIsUnchecked` to CheckBox, Switch
- [ ] Phase 2: Add `AssertValue` to Slider, Stepper, ProgressBar
- [ ] Phase 2: Add `AssertSelectedItem`/`AssertSelectedIndex` to Picker
- [ ] Phase 2: Add `AssertItemCount` to CollectionView
- [ ] Phase 2: Add `AssertTextEmpty`/`AssertTextNotEmpty` to Entry, Editor, Label
- [ ] Phase 2: Add `AssertPlaceholder`/`AssertIsReadOnly` to Entry, Editor

---

## Question 2: Gesture Support in ControlBase vs Separate Service

### Current State

Plan 02 proposes a separate `GestureService`:

```csharp
// Proposed in Plan 02
public class GestureService : IGestureService
{
    Task SwipeLeft(ControlBase control, int distance = 200);
    Task SwipeRight(ControlBase control, int distance = 200);
    Task PinchZoom(ControlBase control, double scale);
    // etc.
}
```

Meanwhile, `ContentControlBase` already has basic gestures embedded:

```csharp
// In ContentControlBase.cs - Already implemented
public virtual void Click()
public virtual void DoubleClick()
public virtual void RightClick()  // Incomplete - just clicks
public virtual void LongPress()   // Incomplete - just clicks
```

### Recommendation

**YES - Embed common gestures in ControlBase, not in a separate service.**

This follows the established WPF pattern where `Click()`, `DoubleClick()`, and `RightClick()` are control methods, not service calls.

### Proposed Changes to ControlBase

Add gesture methods directly to `ControlBase`:

```csharp
public abstract class ControlBase : IControlObject
{
    // ... existing code ...

    #region Gesture Methods (Touch Actions)

    /// <summary>
    /// Tap the control (alias for Click).
    /// </summary>
    public virtual void Tap() => Click();
    
    /// <summary>
    /// Click/tap the control.
    /// </summary>
    public virtual void Click()
    {
        var element = WaitForElementVisible();
        if (element == null)
            ThrowCheckFailed("Click", $"Element '{AutomationId}' not visible for click.");
        element!.Click();
        LogAction("Click");
    }
    
    /// <summary>
    /// Double-tap the control.
    /// </summary>
    public virtual void DoubleTap()
    {
        var element = WaitForElementVisible();
        if (element == null)
            ThrowCheckFailed("DoubleTap", $"Element '{AutomationId}' not visible for double-tap.");
        _context.Driver.PerformDoubleTap(element!);
        LogAction("DoubleTap");
    }
    
    /// <summary>
    /// Long-press the control.
    /// </summary>
    public virtual void LongPress(int durationMs = 1000)
    {
        var element = WaitForElementVisible();
        if (element == null)
            ThrowCheckFailed("LongPress", $"Element '{AutomationId}' not visible for long press.");
        _context.Driver.PerformLongPress(element!, durationMs);
        LogAction("LongPress", durationMs.ToString());
    }
    
    /// <summary>
    /// Swipe in a direction starting from this control.
    /// </summary>
    public virtual void Swipe(SwipeDirection direction, int distance = 200)
    {
        var element = WaitForElementVisible();
        if (element == null)
            ThrowCheckFailed("Swipe", $"Element '{AutomationId}' not visible for swipe.");
        _context.Driver.PerformSwipe(element!, direction, distance);
        LogAction("Swipe", $"{direction}, {distance}px");
    }
    
    /// <summary>
    /// Swipe left on this control.
    /// </summary>
    public virtual void SwipeLeft(int distance = 200) => Swipe(SwipeDirection.Left, distance);
    
    /// <summary>
    /// Swipe right on this control.
    /// </summary>
    public virtual void SwipeRight(int distance = 200) => Swipe(SwipeDirection.Right, distance);
    
    /// <summary>
    /// Swipe up on this control.
    /// </summary>
    public virtual void SwipeUp(int distance = 200) => Swipe(SwipeDirection.Up, distance);
    
    /// <summary>
    /// Swipe down on this control.
    /// </summary>
    public virtual void SwipeDown(int distance = 200) => Swipe(SwipeDirection.Down, distance);
    
    #endregion
}
```

### When to Use a Separate Service

A separate `GestureService` is still useful for:
- **Multi-element gestures** (drag from control A to control B)
- **Pinch/zoom** (multi-touch on same element)
- **Complex multi-touch sequences** (custom gestures)
- **Screen-level gestures** (not tied to any control)

```csharp
// Service for advanced gestures
public interface IGestureService
{
    // Multi-element
    Task DragTo(ControlBase from, ControlBase to);
    
    // Multi-touch
    Task PinchZoom(ControlBase control, double scale);
    Task PinchClose(ControlBase control, double scale);
    
    // Screen-level
    Task SwipeScreen(SwipeDirection direction);
    Task TapAtCoordinates(int x, int y);
}
```

### Comparison: Embedded vs Service

| Aspect | Embedded in ControlBase | Separate GestureService |
|--------|-------------------------|-------------------------|
| Single-element tap/swipe | ✅ Natural API | ❌ Verbose |
| Multi-element drag | ❌ Doesn't fit | ✅ Required |
| Pinch/zoom | ❌ Awkward | ✅ Better fit |
| Discoverability | ✅ IntelliSense on control | ❌ Need to know service |
| Consistency with WPF | ✅ Same pattern | ❌ Different pattern |

### Action Items

- [ ] Phase 3: Add `Tap`, `DoubleTap`, `LongPress`, `Swipe*` methods to `ControlBase`
- [ ] Phase 3: Move gesture methods from `ContentControlBase` to `ControlBase`
- [ ] Phase 3: Create `IGestureService` for multi-element and advanced gestures only
- [ ] Phase 3: Add platform-specific adapters in `AppiumDriverAdapter`

---

## Question 3: Wait for Ready Before Gestures

### Current State

The `Click()` method already waits for element visibility:

```csharp
public virtual void Click()
{
    var element = WaitForElementVisible();  // ← Already waits!
    if (element == null)
        throw new InvalidOperationException($"Element '{AutomationId}' not visible for click.");
    element.Click();
}
```

### Recommendation

**YES - All gesture methods should wait for ready, matching the existing Click pattern.**

The proposed implementation already includes this:

```csharp
public virtual void LongPress(int durationMs = 1000)
{
    var element = WaitForElementVisible();  // ← Wait before gesture
    if (element == null)
        ThrowCheckFailed("LongPress", ...);
    _context.Driver.PerformLongPress(element!, durationMs);
    LogAction("LongPress", durationMs.ToString());
}

public virtual void Swipe(SwipeDirection direction, int distance = 200)
{
    var element = WaitForElementVisible();  // ← Wait before gesture
    if (element == null)
        ThrowCheckFailed("Swipe", ...);
    _context.Driver.PerformSwipe(element!, direction, distance);
    LogAction("Swipe", ...);
}
```

### Wait Pattern Details

All gesture methods should follow this pattern:

```csharp
public virtual void [GestureMethod](...)
{
    // 1. Wait for element to be visible and interactable
    var element = WaitForElementVisible();
    if (element == null)
        ThrowCheckFailed("[Gesture]", $"Element '{AutomationId}' not visible for [gesture].");
    
    // 2. Perform the gesture
    _context.Driver.Perform[Gesture](element!, ...);
    
    // 3. Log the action
    LogAction("[Gesture]", ...);
}
```

### Additional Wait Considerations

For some gestures, we may need to wait for **enabled** state as well:

```csharp
public virtual void Tap()
{
    var element = WaitForElementVisible();
    if (element == null)
        ThrowCheckFailed("Tap", ...);
        
    // Optional: Also check enabled
    if (!element.Enabled)
        ThrowCheckFailed("Tap", $"Element '{AutomationId}' is disabled.");
        
    element.Click();
    LogAction("Tap");
}
```

### Action Items

- [ ] Phase 3: Ensure all gesture methods call `WaitForElementVisible()` first
- [ ] Phase 3: Consider adding `WaitForInteractable()` that checks visible + enabled
- [ ] Phase 3: Document the wait behavior in XML comments

---

## Summary of Recommendations

| Question | Decision | Rationale |
|----------|----------|-----------|
| 1. Assert methods for controls | **Add to each control** | Control-specific assertions (`AssertIsChecked`, `AssertValue`, etc.) belong on controls |
| 2. Gestures in ControlBase | **YES - Embed common gestures** | Matches WPF pattern, better API discoverability, natural `control.SwipeLeft()` syntax |
| 3. Wait for ready | **YES - All gestures wait** | Already implemented for Click, extend to all gestures for consistency |

---

## Updated Phase 3 Scope

Based on this review, Phase 3 (Gesture & Touch Support) should be revised:

### Gesture Methods in ControlBase

| Method | Description | Wait Behavior |
|--------|-------------|---------------|
| `Click()` / `Tap()` | Single tap | Wait visible |
| `DoubleTap()` | Two rapid taps | Wait visible |
| `LongPress(durationMs)` | Press and hold | Wait visible |
| `SwipeLeft(distance)` | Swipe gesture left | Wait visible |
| `SwipeRight(distance)` | Swipe gesture right | Wait visible |
| `SwipeUp(distance)` | Swipe gesture up | Wait visible |
| `SwipeDown(distance)` | Swipe gesture down | Wait visible |
| `Swipe(direction, distance)` | Generic swipe | Wait visible |

### Advanced GestureService (for multi-element/multi-touch)

```csharp
public interface IGestureService
{
    // Multi-element gestures
    Task DragTo(ControlBase from, ControlBase to);
    Task DragByOffset(ControlBase control, int x, int y);
    
    // Multi-touch gestures
    Task PinchZoom(ControlBase control, double scale);
    Task PinchClose(ControlBase control, double scale);
    Task Rotate(ControlBase control, double degrees);
    
    // Screen-level gestures
    Task SwipeScreen(SwipeDirection direction);
    Task TapAtCoordinates(int x, int y);
    Task ScrollScreen(ScrollDirection direction);
}
```

### SwipeDirection Enum

```csharp
public enum SwipeDirection
{
    Left,
    Right,
    Up,
    Down
}
```

---

## Appendix: Full ControlBase Gesture Implementation

```csharp
// In Brinell.Maui/Controls/Base/ControlBase.cs

#region Gesture Methods

/// <summary>
/// Tap the control. Waits for visibility before tapping.
/// </summary>
public virtual void Tap()
{
    var element = WaitForElementVisible();
    if (element == null)
        ThrowCheckFailed("Tap", $"Element '{AutomationId}' not visible for tap.");
    element!.Click();
    LogAction("Tap");
}

/// <summary>
/// Click the control. Alias for Tap on mobile.
/// </summary>
public virtual void Click() => Tap();

/// <summary>
/// Double-tap the control. Waits for visibility before double-tapping.
/// </summary>
public virtual void DoubleTap()
{
    var element = WaitForElementVisible();
    if (element == null)
        ThrowCheckFailed("DoubleTap", $"Element '{AutomationId}' not visible for double-tap.");
    _context.Driver.PerformDoubleTap(element!);
    LogAction("DoubleTap");
}

/// <summary>
/// Long-press the control. Waits for visibility before long-pressing.
/// </summary>
/// <param name="durationMs">Duration of press in milliseconds. Default 1000ms.</param>
public virtual void LongPress(int durationMs = 1000)
{
    var element = WaitForElementVisible();
    if (element == null)
        ThrowCheckFailed("LongPress", $"Element '{AutomationId}' not visible for long press.");
    _context.Driver.PerformLongPress(element!, durationMs);
    LogAction("LongPress", durationMs.ToString());
}

/// <summary>
/// Swipe in a direction starting from this control. Waits for visibility before swiping.
/// </summary>
/// <param name="direction">Direction to swipe.</param>
/// <param name="distance">Distance in pixels. Default 200.</param>
public virtual void Swipe(SwipeDirection direction, int distance = 200)
{
    var element = WaitForElementVisible();
    if (element == null)
        ThrowCheckFailed("Swipe", $"Element '{AutomationId}' not visible for swipe.");
    _context.Driver.PerformSwipe(element!, direction, distance);
    LogAction("Swipe", $"{direction}, {distance}px");
}

/// <summary>
/// Swipe left on this control. Waits for visibility before swiping.
/// </summary>
public virtual void SwipeLeft(int distance = 200) => Swipe(SwipeDirection.Left, distance);

/// <summary>
/// Swipe right on this control. Waits for visibility before swiping.
/// </summary>
public virtual void SwipeRight(int distance = 200) => Swipe(SwipeDirection.Right, distance);

/// <summary>
/// Swipe up on this control. Waits for visibility before swiping.
/// </summary>
public virtual void SwipeUp(int distance = 200) => Swipe(SwipeDirection.Up, distance);

/// <summary>
/// Swipe down on this control. Waits for visibility before swiping.
/// </summary>
public virtual void SwipeDown(int distance = 200) => Swipe(SwipeDirection.Down, distance);

#endregion
```

---

## Change Log

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2024-12-30 | Review | Initial architecture review document |
