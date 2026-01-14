# Fix 012: Add String Locator Constructor to MAUI Controls

| Field | Value |
|-------|-------|
| Status | Open |
| Date Created | January 14, 2026 |
| Date Resolved | _Pending_ |
| Affected Version | Current |
| Fixed Version | _Pending_ |

## Summary

`MauiControlBase<TScope>` currently only has a constructor that takes a `Locator` object. For convenience, it should also have a constructor that takes a `string` and creates the `Locator` using the scope's `DefaultLocatorStrategy`. Each child control (`MauiButtonControl`, `MauiEntryControl`) should also have this string-based constructor for consistency.

## Symptoms

1. Users must always create `Locator` objects explicitly when creating controls
2. Cannot use simple string-based construction like `new MauiButtonControl(page, "btnSubmit")`
3. Inconsistent with the ergonomic API design goal of the framework

## Evidence

### Current Usage (Verbose)

```csharp
// Current - requires explicit Locator creation
var button = new MauiButtonControl<MyPage>(page, Locator.ByAutomationId("btnSubmit"));
var entry = new MauiEntryControl<MyPage>(page, Locator.ByAutomationId("txtUsername"));
```

### Desired Usage (Ergonomic)

```csharp
// Desired - use scope's DefaultLocatorStrategy automatically
var button = new MauiButtonControl<MyPage>(page, "btnSubmit");
var entry = new MauiEntryControl<MyPage>(page, "txtUsername");
```

## Root Cause

The commented-out constructor in `MauiControlBase.cs` was never completed:
```csharp
/*
    public MauiControlBase(IMauiScope<TScope> scope, string locator)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _locator = scope.Default new Locator(locator);  // Incomplete code
    }
*/
```

### Affected Components

- `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`
- `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs`
- `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs`
- `srcnew/Brinell.Maui/Controls/MauiContainerBase.cs` (if applicable)

## Proposed Solution

### Approach

1. Add a static helper method or use `Locator` constructor to create locator from strategy + string
2. Add string-based constructor to `MauiControlBase<TScope>` that:
   - Takes `(IMauiScope<TScope> scope, string locatorValue)`
   - Creates `Locator` using `new Locator(scope.DefaultLocatorStrategy, locatorValue)`
3. Add corresponding string-based constructor to each child control class
4. Remove the commented-out incomplete constructor code

### Affected Files

Files that will need modification:

| File | Expected Change |
|------|-----------------|
| `srcnew/Brinell.Maui/Controls/MauiControlBase.cs` | Add `(scope, string)` constructor, remove commented code |
| `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs` | Add `(scope, string)` constructor |
| `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs` | Add `(scope, string)` constructor |
| `srcnew/Brinell.Maui/Controls/MauiContainerBase.cs` | Add `(scope, string)` constructor if it has similar pattern |

## Files Modified

_To be completed during implementation (Phase 2)_

| File | Change |
|------|--------|
| | |

## Verification

_To be completed during implementation (Phase 2)_

- [ ] Original symptoms resolved
- [ ] No new issues introduced
- [ ] Tests pass
- [ ] Both constructor overloads work correctly

## Related

- Fix 011: Add TScope Generic Parameter to Control Interfaces

## Notes

The `DefaultLocatorStrategy` is defined in `IElementScope` and typically returns `LocatorStrategy.AutomationId` for MAUI controls. This means `new MauiButtonControl(page, "btnSubmit")` will automatically create `Locator.ByAutomationId("btnSubmit")`.
