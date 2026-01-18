# Issue 001: Framework Architecture Violations - Hanging Tests and Bypassed Logging

## Status: Open
## Date: 2026-01-18
## Version: Current development

## Summary

Multiple test code and page object implementations bypass the Brinell framework's core patterns, causing tests to hang indefinitely and miss logging. Code uses raw driver methods (`driver.FindElements()`) instead of framework's `Context.FindElements()` or control-based methods, and action methods don't use the `Run()` wrapper for consistent logging and error handling.

## Symptoms

1. Tests hang indefinitely during execution (especially after clicking flyout items)
2. No logging output for element finding operations in page objects
3. Inconsistent error handling when elements aren't found
4. Raw driver calls bypass framework's timeout and polling logic

## Evidence

### Code Examples - Bypassing Framework

**AppShellPage.cs - Using raw driver:**
```csharp
// WRONG - bypasses framework logging, timeout handling
var driver = Context.Driver.UnwrapDriver();
var elements = driver.FindElements(MobileBy.AccessibilityId("FlyoutTitle"));
return elements.Count > 0;
```

**Should use:**
```csharp
// CORRECT - uses framework's element finding with proper timeouts
return Context.TryFindElement(new Locator(LocatorStrategy.AccessibilityId, "FlyoutTitle")) != null;
```

**ScrollFlyoutToBottom - Not using Run for logging:**
```csharp
// WRONG - no logging, no error handling wrapper
public AppShellPage ScrollFlyoutToBottom()
{
    var driver = Context.Driver.UnwrapDriver();
    var menuScrollViewer = driver.FindElements(MobileBy.AccessibilityId("MenuItemsScrollViewer"));
    if (menuScrollViewer.Count > 0)
    {
        var scroller = menuScrollViewer[0];
        scroller.Click();
        scroller.SendKeys(Keys.End);
    }
    return this;
}
```

### Affected Files

- `testsnew/Brinell.Maui.UITests/Pages/AppShellPage.cs`
- `testsnew/Brinell.Maui.UITests/AppiumFixture.cs` (NavigateToContainerDemo)
- Possibly other page objects and tests

### Steps to Reproduce

1. Run FlyoutItemControlTests
2. Observe tests hanging on second or third test
3. Note lack of logging for element finding in page objects
4. Observe that errors don't go through consistent error handling

## Environment

- **Framework**: Brinell MAUI
- **OS**: Windows
- **Driver**: Windows Application Driver / Appium

## Root Cause Analysis

### Investigation Findings

**4 locations using raw driver:**
1. `AppiumFixture.cs:45` - `NavigateToContainerDemo()`
2. `AppShellPage.cs:36` - `IsLoaded()`
3. `AppShellPage.cs:79` - `ScrollFlyoutToBottom()`
4. `AppShellPage.cs:97` - `ScrollFlyoutToTop()`

**Framework provides proper methods:**
- `Context.TryFindElement(Locator)` - returns null if not found, no wait
- `Context.FindElement(Locator)` - polls until found or timeout
- `Context.FindElements(Locator)` - returns list, no exception
- `MauiControlBase.Run()` - wraps actions with logging and error handling

**Root cause of hanging:**
- `FindElements` on raw driver still respects ImplicitWait
- When ImplicitWait was 5000ms, every element check took 5 seconds if not found
- Changed ImplicitWait to 0 but some code paths still hang

**Page object missing Run method:**
- `MauiPageObjectBase` doesn't have `Run()` method like `MauiControlBase`
- Page-level actions (scroll, navigate) can't use consistent logging pattern

### Hypotheses Tested

| Hypothesis | Result |
|------------|--------|
| Raw driver calls don't respect framework timeout settings | Confirmed - bypasses all framework control |
| SendKeys operations may hang without proper element focus handling | Pending - needs more testing |
| FindElements with implicit wait causes unpredictable delays | Confirmed - fixed by setting ImplicitWait to 0 |

### Affected Components

- `AppShellPage.cs` - `IsLoaded()`, `ScrollFlyoutToBottom()`, `ScrollFlyoutToTop()`
- `AppiumFixture.cs` - `NavigateToContainerDemo()`
- Any other code using `Context.Driver.UnwrapDriver()` followed by raw Selenium calls

## Solution

### Approach

1. Replace all `driver.FindElements()` calls with framework's `Context.TryFindElement()` or `Context.FindElements()`
2. Wrap all action methods in `Run()` for consistent logging
3. For scroll operations, create proper framework controls or use existing Wait* methods
4. Audit all page objects and tests for similar violations

### Implementation

[To be filled during resolution]

### Files to Modify

| File | Change |
|------|--------|
| AppShellPage.cs | Replace raw driver calls with framework methods |
| AppiumFixture.cs | Replace raw driver calls with framework methods |

## Verification

### Test Steps

1. Run all FlyoutItemControlTests - should complete without hanging
2. Check test output - should see logging for element operations
3. Force element not found - should get consistent error messages

### Verified In

- [ ] Development environment
- [ ] All UI tests pass

## Related Issues

- None currently identified

## Learnings

1. Never use `Context.Driver.UnwrapDriver()` followed by raw Selenium calls
2. Always use framework's element finding methods
3. Always wrap action methods in `Run()` for logging
4. Add code review checklist for these patterns

## Resolution

**Fixed in version**: [pending]
**Resolution date**: [pending]
