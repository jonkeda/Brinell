# Fix 021: Refactor ScrollToElement into IMauiElement interface

| Field | Value |
|-------|-------|
| Status | Resolved |
| Date Created | 2026-01-16 |
| Date Resolved | 2026-01-16 |
| Affected Version | srcnew/Brinell.Maui |
| Fixed Version | srcnew/Brinell.Maui |

## Summary

The `ScrollToElement` logic in `MauiButtonControl.CheckClickable()` uses raw Selenium Actions API with unwrapped element and driver. This code should be moved to a new `ScrollIntoView()` method on the `IMauiElement` interface to enable reuse across all controls, improve encapsulation, and maintain testability through the interface abstraction.

## Symptoms

1. ScrollToElement code is inline in MauiButtonControl, not reusable by other controls
2. Controls must access unwrapped driver and element to scroll
3. Scroll functionality cannot be easily mocked for unit testing
4. Code duplication will occur if other controls need scroll-into-view capability

## Evidence

### Current Implementation

```csharp
// In MauiButtonControl.CheckClickable():
var unwrappedElement = element.UnwrapElement();
var unwrappedDriver = Context.Driver.UnwrapDriver();
var actions = new OpenQA.Selenium.Interactions.Actions(unwrappedDriver);
actions.ScrollToElement(unwrappedElement).Perform();
```

### Steps to Reproduce

1. Open `MauiButtonControl.cs`
2. See inline ScrollToElement logic in `CheckClickable()` method
3. Note it requires access to unwrapped element and driver

## Root Cause

The scroll-into-view functionality was added directly to `MauiButtonControl` during Issue 024 fix without refactoring it into the element abstraction layer. This violates the interface abstraction pattern used elsewhere in the codebase.

### Affected Components

- `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs` - Contains inline scroll code
- `srcnew/Brinell.Maui/Interfaces/IMauiElement.cs` - Missing ScrollIntoView method
- `srcnew/Brinell.Maui/Wrappers/MauiElement.cs` - Missing ScrollIntoView implementation

## Proposed Solution

### Approach

1. Add `ScrollIntoView(IMauiDriver driver)` method to `IMauiElement` interface
2. Implement the method in `MauiElement` wrapper using Selenium 4 `Actions.ScrollToElement()`
3. Update `MauiButtonControl.CheckClickable()` to use `element.ScrollIntoView(Context.Driver)`
4. The method requires the driver to create the Actions instance

### Affected Files

Files that will need modification:

| File | Expected Change |
|------|-----------------|
| `srcnew/Brinell.Maui/Interfaces/IMauiElement.cs` | Add `ScrollIntoView(IMauiDriver driver)` method to interface |
| `srcnew/Brinell.Maui/Wrappers/MauiElement.cs` | Implement `ScrollIntoView()` using Actions.ScrollToElement |
| `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs` | Replace inline code with `element.ScrollIntoView(Context.Driver)` |

## Files Modified

| File | Change |
|------|--------|
| `srcnew/Brinell.Maui/Interfaces/IMauiElement.cs` | Added `ScrollIntoView(IMauiDriver driver)` method to interface |
| `srcnew/Brinell.Maui/Wrappers/MauiElement.cs` | Implemented `ScrollIntoView()` using Actions.MoveToElement |
| `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs` | Replaced inline code with `element.ScrollIntoView(Context.Driver)` |

## Verification

- [x] Original symptoms resolved
- [x] No new issues introduced
- [x] Tests pass (29 passed, 0 failed)
- [ ] Verified in packaged build

## Related

- [Issue 024: MAUI UITests Remaining Failures](../60-issues/024-maui-uitests-remaining-failures.isu.spx.md) - Original fix that introduced the scroll logic

## Notes

The `ScrollIntoView` method uses `Actions.MoveToElement` instead of `Actions.ScrollToElement` because:
- Windows driver only supports pen and touch pointer types, not wheel actions
- `ScrollToElement` uses wheel actions which throws "Requested value 'wheel' was not found"
- `MoveToElement` achieves similar effect on most drivers and is more widely supported

The caller wraps `ScrollIntoView` in try-catch because scroll support varies by driver:
```csharp
try
{
    element.ScrollIntoView(Context.Driver);
    Thread.Sleep(200);
}
catch
{
    // Ignore scroll errors - element may still be clickable
}
```
