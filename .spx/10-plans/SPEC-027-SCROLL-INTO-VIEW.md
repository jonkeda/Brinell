# SPEC-027: Scroll Into View for Off-Screen Elements

**Status:** Draft  
**Created:** 2026-01-20

## Problem

Windows MAUI apps with ScrollView containers report `element.Displayed = false` for elements that exist in the UI Automation tree but are off-screen. This causes `CheckVisible()` to fail even though the elements are interactable after scrolling.

## Current State

### Methods with Scroll-Into-View (Already Implemented)

| File | Method | Implementation |
|------|--------|----------------|
| ClickableControlBase.cs | Click() | Catches `ElementNotInteractableException`, calls `ScrollIntoView()`, retries |

### Methods Needing Scroll-Into-View

| File | Method | Current Behavior |
|------|--------|------------------|
| ClickableControlBase.cs | DoubleClick() | Calls `CheckVisible()` → fails for off-screen |
| ClickableControlBase.cs | RightClick() | Calls `CheckVisible()` → fails for off-screen |
| ClickableControlBase.cs | Hover() | Calls `CheckVisible()` → fails for off-screen |
| ClickableControlBase.cs | LongPress() | Calls `CheckVisible()` → fails for off-screen |
| RangeControlBase.cs | SetValue() | Calls `CheckVisible()` → fails for off-screen |
| RangeControlBase.cs | Increase() | May need scroll |
| RangeControlBase.cs | Decrease() | May need scroll |
| SelectorControlBase.cs | SelectByIndex() | Calls `CheckVisible()` → fails for off-screen |
| SelectorControlBase.cs | SelectByText() | Calls `CheckVisible()` → fails for off-screen |
| TextControlBase.cs | Enter() | Calls `CheckVisible()` → fails for off-screen |
| TextControlBase.cs | Clear() | Calls `CheckVisible()` → fails for off-screen |
| TextControlBase.cs | Append() | Calls `CheckVisible()` → fails for off-screen |
| TextControlBase.cs | Focus() | Calls `CheckVisible()` → fails for off-screen |

## Proposed Solution

### Option A: Modify CheckVisible to Auto-Scroll

Modify `CheckVisible()` in `ControlObjectBase` to automatically scroll into view when:
- Element exists (`IsExists() == true`)
- Element is not visible (`IsVisible() == false`)
- Expected is `true`

**Pros:**
- Single place to fix
- All methods automatically benefit
- Consistent behavior

**Cons:**
- May have unintended side effects for visibility assertions
- Changes semantics of `CheckVisible()`

### Option B: Add ScrollIntoView to ControlObjectBase (RECOMMENDED)

Add `ScrollIntoView()` method to `ControlObjectBase` and call it from action methods before `CheckVisible()`.

```csharp
/// <summary>
/// Scrolls the element into view if it exists but is not visible.
/// </summary>
public virtual void ScrollIntoView(int? timeoutMs = null)
{
    var element = FindElement();
    if (element is null) return;
    if (element.Displayed) return;
    
    // Try scrolling using keyboard navigation
    PerformScrollIntoView(element, timeoutMs);
}

protected virtual void PerformScrollIntoView(AppiumElement element, int? timeoutMs = null)
{
    // Implementation: Tab + PageDown approach
    // Or: Use touch/gesture scroll to element location
}
```

**Pattern for action methods:**
```csharp
public virtual void SomeAction(int? timeoutMs = null)
{
    CheckExists(true, timeoutMs);
    ScrollIntoView(timeoutMs);  // NEW
    CheckVisible(true, timeoutMs);
    CheckEnabled(true, timeoutMs);
    // ... action
}
```

**Pros:**
- Explicit control
- Clear intent
- No change to assertion semantics

**Cons:**
- Need to update each method

### Option C: Add EnsureVisible() Helper

Create a new method that combines scroll + visibility check:

```csharp
/// <summary>
/// Ensures the element is visible, scrolling if necessary.
/// </summary>
public virtual void EnsureVisible(int? timeoutMs = null)
{
    CheckExists(true, timeoutMs);
    
    if (!IsVisible())
    {
        ScrollIntoView(timeoutMs);
        CheckVisible(true, timeoutMs);
    }
}
```

Then replace `CheckVisible(true, ...)` with `EnsureVisible(...)` in action methods.

## Scroll Implementation Options

### 1. Keyboard Navigation (Current in ClickableControlBase)
```csharp
Actions.SendKeys(Keys.Tab).Perform();
Actions.SendKeys(Keys.PageDown).Perform();
```
- Works for some controls
- Not reliable for all scenarios

### 2. Element Location-Based Scroll
```csharp
// Get element location and scroll container to that position
var location = element.Location;
// Find ScrollView parent and scroll to location
```

### 3. Focus-Based Scroll
```csharp
// Send keys directly to the element to bring it into view
element.SendKeys(Keys.Space); // May trigger action
element.SendKeys("");  // Just focus without action
```

### 4. SetFocus via UIA Pattern
- Use Windows UI Automation SetFocus pattern
- May automatically scroll element into view

## Recommendation

**Use Option B (ScrollIntoView in ControlObjectBase)** with implementation:

1. Add `ScrollIntoView()` to `ControlObjectBase`
2. Add `EnsureInteractable()` helper that combines: Exists + ScrollIntoView + Visible + Enabled
3. Update all action methods to use `EnsureInteractable()` or explicit `ScrollIntoView()`

## Files to Modify

1. **ControlObjectBase.cs** - Add ScrollIntoView(), EnsureInteractable()
2. **ClickableControlBase.cs** - Already has scroll in Click(), update other methods
3. **RangeControlBase.cs** - Update SetValue(), Increase(), Decrease()
4. **SelectorControlBase.cs** - Update SelectByIndex(), SelectByText()
5. **TextControlBase.cs** - Update Enter(), Clear(), Append(), Focus()
6. **ToggleControlBase.cs** - May inherit from ClickableControlBase.Click()

## Test Verification

After implementation, these tests should pass:
- ToggleControlTests6 (12 tests) ✅ Already passing
- RangeControlTests6 (14 tests) - 7 currently failing
- ClickTests6
- TextInputTests6
- SelectionControlTests
