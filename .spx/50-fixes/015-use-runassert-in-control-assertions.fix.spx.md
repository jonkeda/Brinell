# Fix 015: Use RunAssert Methods in Control Assertions

| Field | Value |
|-------|-------|
| Status | Resolved |
| Date Created | 2026-01-14 |
| Date Resolved | 2026-01-14 |
| Affected Version | 0.1.0 |
| Fixed Version | 0.1.0 |

## Summary

Assert methods in `MauiButtonControl` and `MauiEntryControl` perform assertions directly without using the `RunAssert` logging wrapper methods. Per the 221_001_Logging architecture spec, all assertions should be wrapped with RunAssert to provide automatic entry/exit logging, timing, and expected vs actual value tracking.

## Symptoms

1. AssertClickable in MauiButtonControl not using RunAssert pattern
2. AssertTextMatches in MauiEntryControl not using RunAssert pattern
3. AssertPlaceholder in MauiEntryControl not using RunAssert pattern
4. AssertReadOnly in MauiEntryControl not using RunAssert pattern
5. No timing or expected/actual value logging for assertions

## Evidence

### Current Implementations

**MauiButtonControl.AssertClickable** - Direct assertion without logging:
```csharp
public TScope AssertClickable(bool? expected, string? message = null, int? timeoutMs = null)
{
    if (expected == null) return ContainingScope;
    if (!WaitClickable(expected, timeoutMs))
    {
        var actual = IsClickable();
        throw new AssertionException(
            message ?? $"Expected element {(expected.Value ? "to be clickable" : "not to be clickable")} but clickable state is {actual?.ToString() ?? "unknown (element not found)"}. Locator: {Locator}");
    }
    return ContainingScope;
}
```

### Expected Pattern (from MauiControlBase)

```csharp
protected TScope RunAssert<T>(string assertType, T? expected, Func<T?> getActual, string? message = null)
protected TScope RunAssert<T>(string assertType, T? expected, Func<T?> getActual, Func<T?, T?, bool> compare, string? message = null)
```

## Root Cause

The RunAssert helper methods were added to `MauiControlBase` in Fix 013 (Test Logging), but existing assertion implementations in control classes were not updated to use them. Fix 014 addressed Run methods for action methods; this fix addresses RunAssert for assertion methods.

### Affected Components

- `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs`
- `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs`

## Proposed Solution

### Approach

Wrap all assertion methods with the RunAssert helper method:
- Use `RunAssert<T>(assertType, expected, getActual, message)` for simple equality comparisons
- Use `RunAssert<T>(assertType, expected, getActual, compare, message)` for custom comparison logic

### Affected Files

| File | Expected Change |
|------|-----------------|
| MauiButtonControl.cs | AssertClickable → `RunAssert<bool?>("AssertClickable", expected, () => IsClickable(), message)` |
| MauiEntryControl.cs | AssertTextMatches → Use RunAssert with regex match comparison |
| MauiEntryControl.cs | AssertPlaceholder → `RunAssert<string>("AssertPlaceholder", expected, () => GetPlaceholder(), message)` |
| MauiEntryControl.cs | AssertReadOnly → `RunAssert<bool?>("AssertReadOnly", expected, () => IsReadOnly(), message)` |

### Detailed Changes

**MauiButtonControl:**
- `AssertClickable(expected)` → `RunAssert<bool?>("AssertClickable", expected, () => IsClickable(), message)`

**MauiEntryControl:**
- `AssertTextMatches(pattern)` → Custom RunAssert with regex comparison
- `AssertPlaceholder(expected)` → `RunAssert<string>("AssertPlaceholder", expected, () => GetPlaceholder(), message)`
- `AssertReadOnly(expected)` → `RunAssert<bool?>("AssertReadOnly", expected, () => IsReadOnly(), message)`

## Files Modified

| File | Change |
|------|--------|
| MauiButtonControl.cs | AssertClickable now uses `RunAssert("AssertClickable", expected, () => IsClickable(), message)` |
| MauiEntryControl.cs | AssertTextMatches now uses `RunAssert` with custom regex comparison |
| MauiEntryControl.cs | AssertPlaceholder now uses `RunAssert("AssertPlaceholder", expected, () => GetPlaceholder(), message)` |
| MauiEntryControl.cs | AssertReadOnly now uses `RunAssert("AssertReadOnly", expected, () => IsReadOnly(), message)` |

## Verification

- [x] Original symptoms resolved - all assertions now logged
- [x] No new issues introduced
- [x] Tests pass
- [x] Build succeeds

## Related

- [Fix 013: Test Logging Implementation](../01-specs/013-test-logging/) - Added RunAssert methods to MauiControlBase
- [Fix 014: Use Run Methods in Control Operations](./014-use-run-methods-in-controls.fix.spx.md) - Run methods for action methods
- [Spec 221_001_Logging](../../specs2/Architecture/221_001_Logging.spx.md) - Architecture specification

## Notes

This is a companion to Fix 014 which addressed action methods. Together they complete the logging integration for control operations:
- Fix 014: Run methods for Click, Enter, Clear, SetText, etc.
- Fix 015: RunAssert methods for AssertClickable, AssertPlaceholder, AssertReadOnly, etc.
