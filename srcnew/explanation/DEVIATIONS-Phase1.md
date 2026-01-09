# Phase 1 Implementation Deviations

**Document:** DEVIATIONS-Phase1.md  
**Implementation Date:** January 2026  
**Author:** AI Implementation Agent

---

## Overview

This document records deviations from the specification documents made during Phase 1 (Core Infrastructure) implementation. Each deviation is documented with its rationale and the intent being preserved.

---

## 1. Locator Strategy Simplification

### Specification Reference
- SPEC-250_003 (Locator & Element Finding)
- Architecture Pattern: Locator class with factory methods

### Deviation
The `Locator` class was implemented without `MobileBy` or platform-specific locator builders in Core. Instead:
- `LocatorStrategy` enum contains all possible strategies across platforms
- Platform-specific conversion happens in `LocatorConverter` classes within each platform project

### Rationale
The Core layer should remain platform-agnostic. Including MobileBy or web-specific By conversions in Core would create platform dependencies. This approach:
- Keeps Core free of Selenium/Appium dependencies
- Allows each platform to interpret strategies appropriately
- Maintains the single Locator type for API consistency

### Intent Preserved
✅ Locator remains an immutable value object  
✅ Factory methods (ByAutomationId, ByCss, etc.) provide consistent API  
✅ Strategies can be translated to platform-specific locators  

---

## 2. IElementScope Generic Covariance

### Specification Reference
- SPEC-250_001 (Control Object Model)
- SPEC-250_002 (Page Object Model)

### Deviation
`IElementScope<TElement>` was not made covariant (`out TElement`) because:
- `FindElement(Locator)` returns `TElement` (covariant use)
- But `FindElements(Locator)` returns `IReadOnlyList<TElement>` which complicates variance

### Rationale
C# generic variance rules limit practical covariance when return types include collections. The implementation favors explicit typing over complex variance scenarios.

### Intent Preserved
✅ Typed element finding works correctly  
✅ Platform implementations narrow TElement appropriately  
✅ API is consistent across platforms  

---

## 3. IControlObject State Methods Return Nullable Bool

### Specification Reference
- SPEC-250_001 (Control Object Model)
- Returns: `bool?` for `IsVisible()`, `IsEnabled()`

### Deviation
As per spec, `IsExists()` returns `bool` (non-nullable), while `IsVisible()` and `IsEnabled()` return `bool?`. This is intentional:
- `IsExists()` can definitively answer yes/no
- `IsVisible()` returns null if element doesn't exist
- `IsEnabled()` returns null if element doesn't exist

### Rationale
This matches the spec exactly. Element non-existence is different from "not visible" or "not enabled".

### Intent Preserved
✅ Clear semantics for each state query  
✅ Null indicates "cannot determine" vs false indicating "determined to be false"  

---

## 4. Capability Interfaces on IControlObject vs Separate Hierarchies

### Specification Reference
- SPEC-250_001 mentions capability interfaces (IClickableControl, ITextControl, etc.)
- Architecture docs show interface hierarchy diagrams

### Deviation
Capability interfaces are defined as standalone interfaces that controls can implement alongside `IControlObject`. They don't require inheriting from `IControlObject`:

```csharp
// Interface is self-contained
public interface IClickableControlObject
{
    bool? IsClickable();
    void Click();
    // ...
}

// Controls implement both
public class ButtonControl : MauiControlBase, IClickableControlObject
{
    // Inherits IControlObject from MauiControlBase
    // Implements IClickableControlObject explicitly
}
```

### Rationale
This provides maximum flexibility for control composition. A control can implement any combination of capabilities without complex multiple inheritance trees.

### Intent Preserved
✅ Controls can declare their capabilities through interfaces  
✅ Test code can use capability interfaces for polymorphism  
✅ No breaking changes when adding new capabilities  

---

## 5. WaitHelper in Core vs Platform-Specific

### Specification Reference
- SPEC-250_005 (Wait Mechanism)
- Architecture ADR on polling vs WebDriverWait

### Deviation
A simple `WaitHelper` class using `Stopwatch` polling was implemented in Core rather than using Selenium's `WebDriverWait` or Appium's wait mechanisms.

### Rationale
- Core must remain driver-agnostic
- Simple polling with `Stopwatch` works universally
- Platform layers can use native waits for element-specific scenarios
- `WaitHelper` provides generic condition-based waiting for any async operation

### Intent Preserved
✅ Configurable timeouts and polling intervals  
✅ Works with any condition (lambda)  
✅ Returns timing information when needed  
✅ Consistent behavior across platforms  

---

## 6. Logger Interface Simplification

### Specification Reference
- SPEC-250_007 (Logging)
- Architecture mentions structured logging

### Deviation
`ITestLogger` was simplified to string-based methods rather than structured logging with key-value pairs:

```csharp
void LogAction(string message);  // vs LogAction(string action, params KeyValuePair<string, object>[] data)
```

### Rationale
- Simpler API is easier to implement and use
- Structured logging can be added internally by implementations
- Test logging needs differ from production logging
- Most test output goes to console/test runners that prefer strings

### Intent Preserved
✅ Semantic log methods (LogAction, LogAssert, LogWait, etc.)  
✅ Levels for filtering (Info, Debug, Warning, Error)  
✅ NullTestLogger for silent operation  
✅ Easy to extend with structured implementations later  

---

## 7. Platform Page Interfaces Add Methods

### Specification Reference
- SPEC-250_002 (Page Object Model)

### Deviation
Platform-specific page interfaces (`IMauiPageObject`, `IBlazorPageObject`) add platform-specific methods not in the base `IPageObject`:
- `IMauiPageObject.WaitForLoaded()`
- `IBlazorPageObject.Path`, `NavigateTo()`, `WaitForLoaded()`

### Rationale
Different platforms have different page lifecycle needs:
- MAUI apps have app-specific loading indicators
- Blazor pages have URLs and can be navigated to directly
- Web apps need to wait for Blazor to become idle

### Intent Preserved
✅ Base interface provides common abstraction  
✅ Platform interfaces extend with platform-appropriate behavior  
✅ Test code can use platform interface for full functionality  

---

## 8. MobileBy Implementation as XPath

### Specification Reference
- Appium documentation specifies accessibility-id as a first-class locator

### Deviation
In `MauiLocatorConverter`, the `MobileBy.AccessibilityId()` is implemented using XPath:

```csharp
public static By AccessibilityId(string id) =>
    By.XPath($"//*[@content-desc='{id}' or @accessibility-id='{id}' or @AutomationId='{id}']");
```

### Rationale
- Appium.WebDriver NuGet doesn't expose `MobileBy` as cleanly as older versions
- XPath works universally across Android/iOS/Windows
- Covers multiple attribute names used by different platforms

### Intent Preserved
✅ AutomationId/AccessibilityId finding works  
✅ Cross-platform compatibility maintained  
✅ Can be optimized later with native MobileBy if needed  

---

## Summary

All deviations were made to:
1. **Maintain clean architecture** - Keep Core platform-agnostic
2. **Simplify implementation** - Avoid unnecessary complexity
3. **Preserve intent** - Ensure the spec's goals are achieved
4. **Enable extensibility** - Allow future enhancements without breaking changes

No deviations compromise the fundamental goals of the Brinell framework:
- ✅ Clean separation between Core, MAUI, and Blazor
- ✅ Interface-first design for testability
- ✅ Control object pattern for maintainable tests
- ✅ Page object pattern for page organization
- ✅ Configurable timeouts and logging
- ✅ Platform-appropriate element finding

---

## Document History

| Date | Author | Changes |
|------|--------|---------|
| Jan 2026 | AI Agent | Initial Phase 1 documentation |
