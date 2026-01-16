# Issue 021: UI Tests Fail - Element Locator Strategy Mismatch Between Old and New Infrastructure

## Status: Open
## Date: 2026-01-15
## Version: Brinell srcnew (new infrastructure)

## Summary

Fix 020 UI tests were created in `testsnew/Brinell.Maui.UITests` to validate MauiButtonControl and MauiEntryControl. The tests compile and execute against the Appium server, but 28 out of 29 tests fail because elements cannot be found. The new infrastructure (`srcnew/Brinell.Maui`) uses `MobileBy.Id()` for AutomationId locators, while the working old infrastructure (`src/Brinell.Maui/Infrastructure`) uses `MobileBy.AccessibilityId()`. This difference in locator strategy causes the new tests to fail even though the AutomationIds in the MAUI sample app are correct.

## Symptoms

1. Tests fail with `ElementNotFoundException: Element not found within 10000ms. Locator: Id:NameEntry`
2. Tests fail with `ElementNotFoundException: Element not found within 10000ms. Locator: Id:IncrementButton`
3. Tests fail with `TimeoutException: Element was not clickable within 10000ms. Locator: Id:ResetButton`
4. 28 of 29 tests fail; only 1 passes (likely a test without element interaction)
5. Appium connects successfully and app launches correctly

## Evidence

### Error Messages

```
ElementNotFoundException: Element not found within 10000ms. Locator: Id:NameEntry
   at Brinell.Maui.Controls.MauiControlBase`1.FindElement()
   
ElementNotFoundException: Element not found within 10000ms. Locator: Id:IncrementButton
   at Brinell.Maui.Controls.MauiControlBase`1.FindElement()

TimeoutException: Element was not clickable within 10000ms. Locator: Id:ResetButton
```

### Console/Log Output

```
Test Run Failed.
Total tests: 29
     Passed: 1
     Failed: 28
Total time: 2.0266 Minutes
```

### Steps to Reproduce

1. Start Appium server: `appium --base-path /`
2. Build MAUI sample app: `dotnet build samples/Brinell.Samples.Maui.App -f net10.0-windows10.0.19041.0 -c Debug`
3. Run UI tests: `dotnet test testsnew/Brinell.Maui.UITests --no-build`
4. Expected: Tests pass
5. Actual: 28 of 29 tests fail with element not found errors

## Environment

- **Version**: .NET 10, Appium 3.1.2, Windows driver 5.1.3
- **OS**: Windows 10/11
- **Related Config**: MAUI app with AutomationId properties set in XAML

## Root Cause Analysis

### Investigation Findings

Comparing the two implementations:

**Old/Working Infrastructure** (`src/Brinell.Maui/Infrastructure/AppiumDriverAdapter.cs` line 133):
```csharp
public IElementAdapter? FindElement(string automationId)
{
    try
    {
        // Try accessibility ID first (MAUI AutomationId maps to this)
        var element = _driver.FindElement(MobileBy.AccessibilityId(automationId));
        return element != null ? new AppiumElementAdapter((AppiumElement)element) : null;
    }
    // ...
}
```

**New/Failing Infrastructure** (`srcnew/Brinell.Maui/Extensions/LocatorExtensions.cs` line 21):
```csharp
return locator.Strategy switch
{
    LocatorStrategy.AutomationId => MobileBy.Id(locator.Value),  // ❌ Wrong!
    LocatorStrategy.AccessibilityId => MobileBy.AccessibilityId(locator.Value),
    // ...
};
```

### Hypotheses Tested

| Hypothesis | Result |
|------------|--------|
| App not launching | Rejected - app launches successfully |
| Wrong AutomationIds in tests | Rejected - IDs match MAUI XAML |
| Appium server issue | Rejected - server responds correctly |
| Locator strategy mismatch | **CONFIRMED** - `MobileBy.Id()` vs `MobileBy.AccessibilityId()` |

### Root Cause

In MAUI on Windows, the `AutomationId` property maps to **AccessibilityId** in the Windows automation tree, not to "Id". The new infrastructure's `LocatorExtensions.ToBy()` method incorrectly uses `MobileBy.Id()` for `LocatorStrategy.AutomationId`, when it should use `MobileBy.AccessibilityId()`.

### Affected Components

- `srcnew/Brinell.Maui/Extensions/LocatorExtensions.cs` - line 21

## Solution

### Approach

Change `LocatorStrategy.AutomationId` to use `MobileBy.AccessibilityId()` instead of `MobileBy.Id()` in the `ToBy()` extension method.

### Implementation

```csharp
return locator.Strategy switch
{
    LocatorStrategy.AutomationId => MobileBy.AccessibilityId(locator.Value),  // ✅ Fixed
    LocatorStrategy.AccessibilityId => MobileBy.AccessibilityId(locator.Value),
    LocatorStrategy.Id => By.Id(locator.Value),
    // ...
};
```

### Files Modified

| File | Change |
|------|--------|
| `srcnew/Brinell.Maui/Extensions/LocatorExtensions.cs` | Change AutomationId to use AccessibilityId |

## Verification

### Test Steps

1. Apply the fix to `LocatorExtensions.cs`
2. Rebuild: `dotnet build testsnew/Brinell.Maui.UITests`
3. Run tests: `dotnet test testsnew/Brinell.Maui.UITests`
4. Expected: All 29 tests pass

### Verified In

- [ ] Development environment
- [ ] Packaged extension
- [ ] Production

## Related Issues

- Fix 020: Create example UI tests (completed, tests created but failing)

## Learnings

- MAUI `AutomationId` maps to **AccessibilityId** in Windows automation, not "Id"
- When creating new infrastructure, verify locator strategies match working implementations
- The `MobileBy.Id()` vs `MobileBy.AccessibilityId()` distinction is critical for MAUI apps

## Resolution

**Fixed in version**: [pending]
**Resolution date**: [pending]
