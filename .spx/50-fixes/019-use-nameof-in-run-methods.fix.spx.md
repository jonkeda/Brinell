# Fix 019: Use nameof() Instead of String Literals in Run Methods

| Field | Value |
|-------|-------|
| Status | Resolved |
| Date Created | January 15, 2026 |
| Date Resolved | January 15, 2026 |
| Affected Version | 0.1.0 |
| Fixed Version | 0.1.0 |

## Summary

The `Run()` and `RunAssert()` helper methods use hardcoded string literals for action names (e.g., `"DoubleClick"`, `"Click"`). These should use `nameof()` to ensure compile-time safety and automatic refactoring support.

## Symptoms

1. String literals don't update when methods are renamed
2. No compile-time verification that the string matches the method name
3. Typos in action names won't be caught by the compiler

## Evidence

### Current Code

```csharp
Run("DoubleClick", () =>
{
    // ...
});

RunAssert("AssertClickable", expected, () =>
{
    // ...
});
```

### Expected Code

```csharp
Run(nameof(DoubleClick), () =>
{
    // ...
});

RunAssert(nameof(AssertClickable), expected, () =>
{
    // ...
});
```

## Root Cause

Original implementation used string literals for simplicity, but `nameof()` provides better maintainability.

## Proposed Solution

Replace all `Run("MethodName", ...)` calls with `Run(nameof(MethodName), ...)` and all `RunAssert("MethodName", ...)` calls with `RunAssert(nameof(MethodName), ...)`.

### Affected Files

| File | Expected Change |
|------|-----------------|
| `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs` | Update Run/RunAssert calls |
| `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs` | Update Run/RunAssert calls |
| Any other control files with Run/RunAssert calls | Update Run/RunAssert calls |

## Files Modified

| File | Change |
|------|--------|
| `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs` | `Run("Click"` → `Run(nameof(Click)`, `Run("DoubleClick"` → `Run(nameof(DoubleClick)`, `Run("RightClick"` → `Run(nameof(RightClick)`, `RunAssert("AssertClickable"` → `RunAssert(nameof(AssertClickable)` |
| `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs` | `Run<string>("Enter"` → `Run<string>(nameof(Enter)`, `Run("Clear"` → `Run(nameof(Clear)`, `Run<string>("SetText"` → `Run<string>(nameof(SetText)`, `RunAssert("AssertTextMatches"` → `RunAssert(nameof(AssertTextMatches)`, `RunAssert("AssertPlaceholder"` → `RunAssert(nameof(AssertPlaceholder)`, `RunAssert("AssertReadOnly"` → `RunAssert(nameof(AssertReadOnly)` |

## Verification

- [x] Original symptoms resolved
- [x] No new issues introduced
- [x] Tests pass (14 total: 13 passed, 1 skipped)
- [x] Build succeeds

## Notes

- `nameof()` returns the method name at compile time
- Provides refactoring support - renaming method updates the string automatically
- No runtime performance difference (evaluated at compile time)
