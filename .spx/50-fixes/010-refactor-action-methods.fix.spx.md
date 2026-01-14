# Fix 010: Refactor action methods to use CheckClickable pattern

| Field | Value |
|-------|-------|
| Status | Resolved |
| Date Created | January 14, 2026 |
| Date Resolved | January 14, 2026 |
| Affected Version | 1.0.0 |
| Fixed Version | 1.0.0 |

## Summary

Action methods in `MauiButtonControl` and `MauiEntryControl` have inconsistent patterns and compilation errors. The `Click()` method was refactored to use a clean `CheckClickable()` helper pattern, but `DoubleClick()` and `RightClick()` still use verbose inline logic with bugs (undefined `element` variable). Additionally, `MauiEntryControl` has similar verbose patterns in `Enter()` and `Clear()` that should use a consistent `CheckEnabled()` pattern.

## Symptoms

1. `DoubleClick()` references undefined variable `element` after calling `FindElement()` without assignment
2. `CheckClickable()` references undefined parameter `timeoutMs` 
3. Inconsistent code patterns between `Click()` (clean) and `DoubleClick()`/`RightClick()` (verbose)
4. `MauiEntryControl.Enter()` and `Clear()` have redundant wait + find + stale retry logic

## Evidence

### Error Messages

```
MauiButtonControl.cs: 'element' does not exist in the current context (line 51-52)
MauiButtonControl.cs: 'timeoutMs' does not exist in the current context (line 99)
```

### Steps to Reproduce

1. Open `MauiButtonControl.cs`
2. Attempt to build
3. Compilation fails due to undefined variables

## Root Cause

Partial refactoring was applied to `Click()` method but not propagated to:
- `DoubleClick()` - still has old verbose pattern with missing variable assignment
- `RightClick()` - still has old verbose pattern
- `CheckClickable()` - missing parameter that was removed during refactoring
- `MauiEntryControl.Enter()` / `Clear()` - similar verbose patterns that should be simplified

### Affected Components

- `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs`
- `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs`

## Proposed Solution

### Approach

1. Fix `CheckClickable(int? timeoutMs = null)` to accept the timeout parameter
2. Refactor `DoubleClick()` to use `CheckClickable()` + `FindElement()` pattern
3. Refactor `RightClick()` to use `CheckClickable()` + `FindElement()` pattern
4. Add `CheckEnabled(int? timeoutMs = null)` to `MauiEntryControl`
5. Refactor `Enter()` to use `CheckEnabled()` + `FindElement()` pattern
6. Refactor `Clear()` to use `CheckEnabled()` + `FindElement()` pattern

### Affected Files

Files that will need modification:

| File | Expected Change |
|------|-----------------|
| `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs` | Fix CheckClickable parameter, refactor DoubleClick and RightClick |
| `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs` | Add CheckEnabled helper, refactor Enter and Clear |

## Files Modified

| File | Change |
|------|--------|
| `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs` | Fixed `CheckClickable(int? timeoutMs)` parameter, refactored `DoubleClick()` and `RightClick()` to use clean pattern |
| `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs` | Added `CheckEnabled(int? timeoutMs)` helper, refactored `Enter()` and `Clear()` to use clean pattern |

## Verification

- [x] Original symptoms resolved
- [x] No new issues introduced
- [x] Tests pass
- [x] Project compiles successfully

## Related

- [Spec 009: MAUI Minimal Controls](../01-specs/009-maui-minimal-controls/)

## Notes

The target pattern is:
```csharp
public void Click(int? timeoutMs = null)
{
    CheckClickable(timeoutMs);
    var element = FindElement();
    element.Click();
}
```

This pattern:
1. Delegates wait/validation to a helper method
2. Finds element after validation passes
3. Performs the action
4. Simple, readable, consistent
