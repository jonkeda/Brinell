# Fix 016: Move Wait Calls Inside RunAssert

| Field | Value |
|-------|-------|
| Status | Resolved |
| Date Created | 2026-01-14 |
| Date Resolved | 2026-01-14 |
| Affected Version | 0.1.0 |
| Fixed Version | 0.1.0 |

## Summary

In Fix 015, the Wait calls were placed outside the RunAssert lambda, meaning the logged timing only captures the final state check, not the actual wait/polling duration. For accurate test reporting, the full wait+check operation should be timed together inside RunAssert.

## Symptoms

1. Wait calls (WaitClickable, WaitPlaceholder, WaitReadOnly, Poll) are outside RunAssert
2. Timing logged by RunAssert doesn't include wait time
3. Log entry/exit timing is misleading - shows only the final check (~1ms), not the polling duration (potentially seconds)

## Evidence

### Current Pattern (Incorrect)

**MauiButtonControl.AssertClickable:**
```csharp
WaitClickable(expected, timeoutMs);  // Wait happens OUTSIDE - not timed
return RunAssert("AssertClickable", expected, () => IsClickable(), message);  // Only final check timed
```

**MauiEntryControl.AssertPlaceholder:**
```csharp
WaitPlaceholder(expected, timeoutMs);  // Wait happens OUTSIDE - not timed
return RunAssert("AssertPlaceholder", expected, () => GetPlaceholder(), message);  // Only final check timed
```

### Expected Pattern

```csharp
return RunAssert("AssertClickable", expected, () => {
    WaitClickable(expected, timeoutMs);  // Wait happens INSIDE - properly timed
    return IsClickable();
}, message);  // Full operation timed
```

## Root Cause

In Fix 015, the refactoring separated Wait and RunAssert calls for clarity, but this broke the timing semantics. The RunAssert stopwatch should capture the entire assertion operation including any polling/waiting.

### Affected Components

- `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs`
- `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs`

## Proposed Solution

### Approach

Move all Wait/Poll calls inside the getActual lambda of RunAssert so the timing includes the full operation:

### Affected Files

| File | Expected Change |
|------|-----------------|
| MauiButtonControl.cs | Move WaitClickable inside RunAssert lambda |
| MauiEntryControl.cs | Move Poll (for AssertTextMatches) inside RunAssert lambda |
| MauiEntryControl.cs | Move WaitPlaceholder inside RunAssert lambda |
| MauiEntryControl.cs | Move WaitReadOnly inside RunAssert lambda |

### Detailed Changes

**MauiButtonControl.AssertClickable:**
```csharp
return RunAssert("AssertClickable", expected, () => {
    WaitClickable(expected, timeoutMs);
    return IsClickable();
}, message);
```

**MauiEntryControl.AssertTextMatches:**
```csharp
return RunAssert("AssertTextMatches", pattern, () => {
    Poll(() => { var t = GetText(); return t != null && regex.IsMatch(t); }, timeoutMs ?? DefaultTimeoutMs);
    return GetText();
}, (actual, exp) => actual != null && regex.IsMatch(actual), message);
```

**MauiEntryControl.AssertPlaceholder:**
```csharp
return RunAssert("AssertPlaceholder", expected, () => {
    WaitPlaceholder(expected, timeoutMs);
    return GetPlaceholder();
}, message);
```

**MauiEntryControl.AssertReadOnly:**
```csharp
return RunAssert("AssertReadOnly", expected, () => {
    WaitReadOnly(expected, timeoutMs);
    return IsReadOnly();
}, message);
```

## Files Modified

| File | Change |
|------|--------|
| MauiButtonControl.cs | AssertClickable now has WaitClickable inside the RunAssert lambda |
| MauiEntryControl.cs | AssertTextMatches now has Poll inside the RunAssert lambda |
| MauiEntryControl.cs | AssertPlaceholder now has WaitPlaceholder inside the RunAssert lambda |
| MauiEntryControl.cs | AssertReadOnly now has WaitReadOnly inside the RunAssert lambda |

## Verification

- [x] Original symptoms resolved - timing now includes wait duration
- [x] No new issues introduced
- [x] Tests pass
- [x] Build succeeds

## Related

- [Fix 015: Use RunAssert Methods in Control Assertions](./015-use-runassert-in-control-assertions.fix.spx.md) - Initial RunAssert implementation
- [Fix 014: Use Run Methods in Control Operations](./014-use-run-methods-in-controls.fix.spx.md) - Run methods for action methods
- [Fix 013: Test Logging Implementation](../01-specs/013-test-logging/) - Added RunAssert methods to MauiControlBase

## Notes

This is a refinement of Fix 015 to ensure timing accuracy. The logged duration should reflect the actual time spent waiting for conditions, not just the final state check.
