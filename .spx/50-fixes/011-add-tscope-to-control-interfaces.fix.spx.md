# Fix 011: Add TScope Generic Parameter to Control Interfaces

| Field | Value |
|-------|-------|
| Status | ✅ Resolved |
| Date Created | January 14, 2026 |
| Date Resolved | January 14, 2026 |
| Affected Version | Current |
| Fixed Version | Current |

## Summary

The `IControlObject` base interface and several derived interfaces (`IToggleControlObject`, `IRangeControlObject`, `ISelectorControlObject`, `IScrollableControlObject`) are missing the `TScope` generic type parameter. Methods returning `void` (like `Assert*`, action methods) should return `TScope` instead to enable fluent method chaining, consistent with interfaces that already have this pattern (`IClickableControlObject<TScope>`, `ITextControlObject<TScope>`, `IEditableTextControlObject<TScope>`).

## Symptoms

1. Cannot chain assertion methods on `IControlObject` (e.g., `control.AssertExists(true).AssertVisible(true)`)
2. Cannot chain action methods on toggle, range, selector, and scrollable controls
3. Inconsistent API design between interfaces - some have TScope, others don't

## Evidence

### Current State

**Interfaces WITH TScope (correct pattern):**
- `IClickableControlObject<TScope>` - Click/DoubleClick/RightClick return TScope
- `ITextControlObject<TScope>` - AssertTextMatches returns TScope
- `IEditableTextControlObject<TScope>` - Enter/Clear/SetText/AssertPlaceholder/AssertReadOnly return TScope

**Interfaces WITHOUT TScope (need update):**
- `IControlObject` - AssertExists/AssertVisible/AssertEnabled/AssertText/AssertTextContains return void
- `IToggleControlObject` - Toggle/SetChecked/Check/Uncheck/AssertChecked return void
- `IRangeControlObject` - SetValue/AssertValue/Increment/Decrement return void
- `ISelectorControlObject` - SelectByText/SelectByIndex/SelectByValue/AssertSelectedText/AssertSelectedIndex/AssertItemCount return void
- `IScrollableControlObject` - ScrollToTop/ScrollToEnd/ScrollBy/ScrollTo/SetScrollPosition/AssertScrollPosition return void

## Root Cause

Initial interface design didn't consistently apply the fluent chaining pattern. Some interfaces were designed with TScope from the start, while the base `IControlObject` and several capability interfaces were designed with `void` return types.

### Affected Components

- `srcnew/Brinell.Core/Interfaces/IControlObject.cs`
- `srcnew/Brinell.Core/Interfaces/IToggleControlObject.cs`
- `srcnew/Brinell.Core/Interfaces/IRangeControlObject.cs`
- `srcnew/Brinell.Core/Interfaces/ISelectorControlObject.cs`
- `srcnew/Brinell.Core/Interfaces/IScrollableControlObject.cs`

## Proposed Solution

### Approach

1. Add `<TScope>` generic type parameter to `IControlObject`
2. Change assertion methods (`AssertExists`, `AssertVisible`, `AssertEnabled`, `AssertText`, `AssertTextContains`) to return `TScope`
3. Update derived interfaces to inherit from `IControlObject<TScope>` and add TScope parameter where missing
4. Change action methods in derived interfaces to return `TScope` for fluent chaining:
   - `IToggleControlObject<TScope>`: Toggle, SetChecked, Check, Uncheck, AssertChecked
   - `IRangeControlObject<TScope>`: SetValue, AssertValue, Increment, Decrement
   - `ISelectorControlObject<TScope>`: SelectByText, SelectByIndex, SelectByValue, AssertSelectedText, AssertSelectedIndex, AssertItemCount
   - `IScrollableControlObject<TScope>`: ScrollToTop, ScrollToEnd, ScrollBy, ScrollTo, SetScrollPosition, AssertScrollPosition

### Affected Files

Files that will need modification:

| File | Expected Change |
|------|-----------------|
| `srcnew/Brinell.Core/Interfaces/IControlObject.cs` | Add `<TScope>`, change Assert methods to return TScope |
| `srcnew/Brinell.Core/Interfaces/IToggleControlObject.cs` | Add `<TScope>`, inherit from IControlObject<TScope>, change methods to return TScope |
| `srcnew/Brinell.Core/Interfaces/IRangeControlObject.cs` | Add `<TScope>`, inherit from IControlObject<TScope>, change methods to return TScope |
| `srcnew/Brinell.Core/Interfaces/ISelectorControlObject.cs` | Add `<TScope>`, inherit from IControlObject<TScope>, change methods to return TScope |
| `srcnew/Brinell.Core/Interfaces/IScrollableControlObject.cs` | Add `<TScope>`, inherit from IControlObject<TScope>, change methods to return TScope |
| `srcnew/Brinell.Core/Interfaces/IClickableControlObject.cs` | Update to inherit from IControlObject<TScope> |
| `srcnew/Brinell.Core/Interfaces/ITextControlObject.cs` | Update to inherit from IControlObject<TScope> |
| `srcnew/Brinell.Core/Interfaces/IEditableTextControlObject.cs` | Already inherits from ITextControlObject<TScope> - no change needed |

## Files Modified

| File | Change |
|------|--------|
| `srcnew/Brinell.Core/Interfaces/IControlObject.cs` | Added `<TScope>` parameter, changed 5 Assert methods to return TScope |
| `srcnew/Brinell.Core/Interfaces/IClickableControlObject.cs` | Updated to inherit from `IControlObject<TScope>` |
| `srcnew/Brinell.Core/Interfaces/ITextControlObject.cs` | Added `<TScope>` typeparam doc, updated to inherit from `IControlObject<TScope>` |
| `srcnew/Brinell.Core/Interfaces/IToggleControlObject.cs` | Added `<TScope>`, changed 6 action/assert methods to return TScope |
| `srcnew/Brinell.Core/Interfaces/IRangeControlObject.cs` | Added `<TScope>`, changed 4 action/assert methods to return TScope |
| `srcnew/Brinell.Core/Interfaces/ISelectorControlObject.cs` | Added `<TScope>`, changed 6 action/assert methods to return TScope |
| `srcnew/Brinell.Core/Interfaces/IScrollableControlObject.cs` | Added `<TScope>`, changed 6 action/assert methods to return TScope |

## Verification

- [x] Original symptoms resolved
- [x] No new issues introduced
- [x] No compilation errors in interface files
- [x] All interfaces consistently use TScope pattern

## Related

- Existing pattern reference: `IClickableControlObject<TScope>` 
- Existing pattern reference: `ITextControlObject<TScope>`
- Existing pattern reference: `IEditableTextControlObject<TScope>`

## Notes

This is a breaking change for any implementations of these interfaces. Implementations will need to be updated to:
1. Implement the generic version of the interface
2. Return `this` (or the appropriate TScope instance) from action/assertion methods
