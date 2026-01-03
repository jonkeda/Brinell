# PLAN-006b: WinForms Control Fixes

## Overview

Fix issues discovered during Phase 3 testing. Focus on eliminating `Thread.Sleep` calls in favor of wait-for-condition patterns.

## Current Test Results

- **51 of 71 tests pass** (72%)
- **20 tests fail**

## Issues Found

### Issue 1: ComboBox Selection Not Working Reliably

**Problem:** ComboBox `SelectByText` works in isolation but fails in batch tests. The selection click happens but the selection doesn't take effect.

**Root Cause:** Using `Thread.Sleep` instead of waiting for the selection to actually change.

**Fix:** After clicking an item, wait for `GetSelectedText()` to return the expected value.

```csharp
// Before (unreliable)
targetItem.Click();
Thread.Sleep(100);

// After (reliable)
targetItem.Click();
WaitForCondition(() => GetSelectedText() == text, 2000, "selection changed");
```

### Issue 2: DateTimePicker SetDate Not Working

**Problem:** Keyboard segment navigation doesn't work reliably - locale-dependent, timing issues.

**Root Cause:** WinForms DateTimePicker doesn't support Value pattern writes, and keyboard navigation is fragile.

**Fix:** Use the LegacyIAccessible pattern's `SetValue` method which works for WinForms controls, or use keyboard with proper wait conditions.

Alternative approach: Focus, select all text with keyboard, type the date string directly using the system's short date format.

### Issue 3: RichTextBox Clear Returns "\n"

**Problem:** After clearing, `GetText()` returns `"\n"` instead of empty string.

**Root Cause:** RichTextBox always maintains at least one paragraph mark.

**Fix:** Trim the result in `GetText()` or `GetContent()`.

```csharp
// Before
return textBox.Text ?? string.Empty;

// After  
return (textBox.Text ?? string.Empty).TrimEnd('\r', '\n');
```

## Implementation Plan

### Step 1: Add WaitForCondition Helper to ControlBase

Add a protected helper method that other controls can use:

```csharp
protected bool WaitForCondition(Func<bool> condition, int timeoutMs, string description)
{
    return _context.WaitFor(condition, timeoutMs, description);
}
```

### Step 2: Fix ComboBoxControl

1. After clicking item, wait for selection to change
2. Remove Thread.Sleep calls
3. Add timeout parameter for robustness

### Step 3: Fix DateTimePickerControl

1. Remove Thread.Sleep calls
2. After setting each segment, wait for the value to update
3. Use simpler approach: clear and type complete date string

### Step 4: Fix RichTextBoxControl

1. Trim trailing newlines in GetContent/GetText

### Step 5: Verify All Tests Pass

Run full test suite.

## Files to Modify

| File | Changes |
|------|---------|
| `Brinell.WinForms/Controls/ComboBoxControl.cs` | Wait for selection after click |
| `Brinell.WinForms/Controls/DateTimePickerControl.cs` | Wait-based date setting |
| `Brinell.WinForms/Controls/RichTextBoxControl.cs` | Trim newlines |

## Success Criteria

- All 71 WinForms tests pass
- No `Thread.Sleep` calls in control implementations (except minimal waits for UI to render if absolutely necessary)
- Controls use wait-for-condition patterns
