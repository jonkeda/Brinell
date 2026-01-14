# Fix 014: Use Run Methods in Control Operations

| Field | Value |
|-------|-------|
| Status | Resolved |
| Date Created | 2026-01-14 |
| Date Resolved | 2026-01-14 |
| Affected Version | 0.1.0 |
| Fixed Version | 0.1.0 |

## Summary

Control methods in `MauiButtonControl` and `MauiEntryControl` perform operations directly without using the `Run` and `RunAssert` logging wrapper methods. Per the 221_001_Logging architecture spec, all control operations should be wrapped with Run methods to provide automatic entry/exit logging, timing, and error tracking.

## Symptoms

1. Button click operations (Click, DoubleClick, RightClick) not logged
2. Entry text operations (Enter, Clear, SetText) not logged
3. Entry assertion methods not using RunAssert pattern
4. No timing information captured for control operations

## Evidence

### Current Implementations

**MauiButtonControl.Click** - Direct operation without logging:
```csharp
public TScope Click(int? timeoutMs = null)
{
    CheckClickable();
    var element = FindElement();
    element.Click();
    return ContainingScope;
}
```

**MauiEntryControl.Enter** - Direct operation without logging:
```csharp
public TScope Enter(string? text, int? timeoutMs = null)
{
    if (text == null)
        return ContainingScope;
    CheckEnabled(timeoutMs);
    var element = FindElement();
    element.SendKeys(text);
    return ContainingScope;
}
```

### Expected Pattern (from MauiControlBase)

```csharp
protected void Run(string action, Action operation)
protected void Run<T>(string action, T? value, Action operation)
protected TScope RunAssert<T>(string assertType, T? expected, Func<T?> getActual, string? message = null)
```

## Root Cause

The Run/RunAssert helper methods were added to `MauiControlBase` in Fix 013 (Test Logging), but existing control implementations were not updated to use them. This is a gap in the logging implementation.

### Affected Components

- `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs`
- `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs`

## Proposed Solution

### Approach

Wrap all control operations with the appropriate Run method:
- **Action methods** (Click, Enter, Clear): Use `Run(action, operation)` or `Run<T>(action, value, operation)`
- **Assert methods** (AssertClickable, AssertTextMatches, etc.): Use `RunAssert<T>(...)` pattern

### Affected Files

| File | Expected Change |
|------|-----------------|
| MauiButtonControl.cs | Wrap Click, DoubleClick, RightClick with `Run("Click", () => ...)` |
| MauiEntryControl.cs | Wrap Enter, Clear, SetText with `Run` and assert methods with `RunAssert` |

### Detailed Changes

**MauiButtonControl:**
- `Click()` → `Run("Click", () => { ... })`
- `DoubleClick()` → `Run("DoubleClick", () => { ... })`  
- `RightClick()` → `Run("RightClick", () => { ... })`
- `AssertClickable()` → Use `RunAssert` pattern

**MauiEntryControl:**
- `Enter(text)` → `Run("Enter", text, () => { ... })`
- `Clear()` → `Run("Clear", () => { ... })`
- `SetText(text)` → `Run("SetText", text, () => { ... })`
- `AssertTextMatches()` → Use `RunAssert` pattern
- `AssertPlaceholder()` → Use `RunAssert` pattern
- `AssertReadOnly()` → Use `RunAssert` pattern

## Files Modified

| File | Change |
|------|--------|
| MauiButtonControl.cs | Wrapped Click, DoubleClick, RightClick with `Run("Action", () => ...)` |
| MauiEntryControl.cs | Wrapped Enter, SetText with `Run<string>("Action", text, () => ...)` and Clear with `Run("Clear", () => ...)` |

## Verification

- [x] Original symptoms resolved - all control operations now logged
- [x] No new issues introduced
- [x] Tests pass
- [x] Build succeeds

## Related

- [Fix 013: Test Logging Implementation](../01-specs/013-test-logging/) - Added Run methods to MauiControlBase
- [Spec 221_001_Logging](../../specs2/Architecture/221_001_Logging.spx.md) - Architecture specification

## Notes

This is a follow-up to the Test Logging implementation (Fix 013) which added the infrastructure. This fix completes the integration by using that infrastructure in concrete control implementations.
