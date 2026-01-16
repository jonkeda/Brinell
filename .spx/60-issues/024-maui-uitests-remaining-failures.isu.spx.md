# Issue 024: MAUI UITests - Remaining 6 Test Failures (Placeholder & GreetButton)

## Status: Resolved
## Date: 2026-01-15
## Version: srcnew/Brinell.Maui

## Summary

After resolving Issue 023 (DebugTest session interference), 6 of 29 MAUI UI tests still fail. These failures are unrelated to session management and involve:
1. `GetPlaceholder` returning null instead of expected placeholder text
2. `GreetButton` not being clickable within the 10-second timeout

## Symptoms

1. `Entry_GetPlaceholder_ReturnsPlaceholderText` fails - placeholder is null
2. Multiple tests fail with "Element was not clickable within 10000ms" for GreetButton
3. Tests pass individually but fail in full suite runs (possible UI state pollution)

## Evidence

### Error Messages

**Placeholder Issue:**
```
Expected placeholder to be "Enter your name", but found <null>.
   at Brinell.Maui.UITests.Tests.EntryControlTests.Entry_GetPlaceholder_ReturnsPlaceholderText()
   in EntryControlTests.cs:line 154
```

**GreetButton Timeout Issues:**
```
System.TimeoutException : Element was not clickable within 10000ms. Locator: AutomationId:GreetButton
   at Brinell.Maui.Controls.MauiButtonControl`1.CheckClickable(Nullable`1 timeoutMs)
   in MauiButtonControl.cs:line 98
```

### Test Results

```
Failed!  - Failed: 6, Passed: 23, Skipped: 0, Total: 29, Duration: 2 m 9 s
```

### Failing Tests

| Test Name | Error Type |
|-----------|------------|
| Entry_GetPlaceholder_ReturnsPlaceholderText | Placeholder is null |
| Entry_EnterNameAndGreet_ShowsGreetingMessage | GreetButton not clickable |
| Entry_GreetWithoutName_ShowsValidationMessage | GreetButton not clickable |
| [Others TBD] | [TBD] |

## Environment

- **Version**: srcnew/Brinell.Maui (net10.0)
- **OS**: Windows 10/11
- **Appium**: 3.1.2
- **Related Config**: Windows driver, Appium on 127.0.0.1:4723

## Root Cause Analysis

### Investigation Findings

**Two separate issues identified:**

1. **Placeholder returns null**: Windows MAUI exposes the Entry placeholder text via the `Name` automation property, not `HelpText`, `hint`, or `placeholder`. A diagnostic test confirmed `Name: 'Enter your name'`.

2. **GreetButton not clickable**: The GreetButton is inside a ScrollView and was off-screen (below the fold). Diagnostic test confirmed `Exists: True, Visible: False, Enabled: True, Clickable: False`. The button needed to be scrolled into view before clicking.

### Hypotheses

| Hypothesis | Status |
|------------|--------|
| Placeholder property not exposed by Windows Automation | ❌ Exposed as "Name" attribute |
| GreetButton covered by another element or modal | ❌ Not the cause |
| Previous test leaves UI in bad state (needs reset) | ❌ Not the cause |
| **App UI doesn't match test expectations** | ❌ Not the cause |
| **Element needs to be scrolled into view** | ✅ **CONFIRMED** |

### Root Cause

1. **Placeholder**: `GetPlaceholder()` was not checking the `Name` attribute, which is how Windows MAUI exposes Entry placeholder text via UI Automation.

2. **GreetButton**: `CheckClickable()` was checking `IsVisible()` which returns `false` for off-screen elements. The button needed to be scrolled into view using `ScrollToElement` before clicking.

### Affected Components

- `testsnew/Brinell.Maui.UITests/Tests/EntryControlTests.cs`
- `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs` - GetPlaceholder implementation
- `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs` - CheckClickable implementation
- `samples/Brinell.Samples.Maui.App/` - Sample app UI

## Solution

### Approach

1. **Placeholder Fix**: Add `Name` as the first attribute to check in `GetPlaceholder()` method
2. **Scroll Fix**: Use Selenium 4's `ScrollToElement` in `CheckClickable()` to scroll off-screen elements into view before clicking

### Implementation

**MauiEntryControl.cs - GetPlaceholder():**
```csharp
var placeholder = element.GetAttribute("Name")  // Windows MAUI
                ?? element.GetAttribute("HelpText")
                ?? element.GetAttribute("hint")  // Android
                ?? element.GetAttribute("placeholder");  // iOS
```

**MauiButtonControl.cs - CheckClickable():**
```csharp
if (element != null && IsVisible() != true)
{
    var actions = new Actions(unwrappedDriver);
    actions.ScrollToElement(unwrappedElement).Perform();
    Thread.Sleep(200);  // Allow UI to settle
}
```

### Files Modified

| File | Change |
|------|--------|
| `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs` | Added "Name" as first attribute in GetPlaceholder() |
| `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs` | Added ScrollToElement before click for off-screen elements |

## Verification

### Test Steps

1. Run all MAUI UI tests
2. Verify all tests pass including placeholder and GreetButton tests
3. No timeout or null value errors

### Test Results After Fix

```
Passed!  - Failed: 0, Passed: 31, Skipped: 0, Total: 31, Duration: 1 m 50 s
```

### Verified In

- [x] Development environment
- [ ] CI pipeline

## Related Issues

- [Issue 023: MAUI UITests NoSuchWindowException](./023-maui-uitests-nosuchwindowexception.isu.spx.md) - Resolved, unblocked these tests

## Learnings

1. **Windows MAUI Automation Properties**: Windows MAUI exposes Entry placeholder text via the `Name` UI Automation property, not `HelpText` or other commonly expected attributes. Always run diagnostic tests to discover actual attribute names.

2. **Scroll Into View is Essential**: Off-screen elements in ScrollView containers have `Displayed = false`. Controls that interact with such elements must scroll them into view first using Selenium 4's `ScrollToElement`.

3. **Platform-Specific Attribute Fallback**: When implementing cross-platform controls, use a fallback chain of attributes (Name → HelpText → hint → placeholder) to handle different platform behaviors.

4. **Diagnostic Tests Speed Investigation**: Creating quick diagnostic tests to dump element attributes accelerates root cause analysis significantly.

## Resolution

**Fixed in version**: srcnew/Brinell.Maui
**Resolution date**: 2026-01-15
**Fix**: 
- Added "Name" attribute check in `GetPlaceholder()` for Windows MAUI
- Added `ScrollToElement` call in `CheckClickable()` for off-screen buttons
