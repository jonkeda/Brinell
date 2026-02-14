# Element Lookup Optimization

**Status:** Implemented in srcnew | **Priority:** High

## Problem

Original implementation made excessive `FindElement` calls — up to 53 calls for a single `Click()` operation. Each Is/Wait/Check/Assert method independently called `FindElement`.

## Solution: Element-Aware Overloads

Introduced `RunWithElement` and `PollWithElement` patterns in `MauiControlBase`:

1. **Find element once** at the start of the operation
2. **Pass element** to the action/condition lambda
3. **Reuse** between Is → Wait → Assert delegation chains

### Result

| Operation | Before | After |
|-----------|--------|-------|
| `Click()` | 53 FindElement calls | 1-3 calls |
| `AssertText()` | ~20 calls | 1-2 calls |
| `SetChecked()` | ~30 calls | 2-4 calls |

## Phase 2 (SPEC-015b)

Extended the optimization to all remaining controls (beyond Click/Text):
- Toggle controls
- Range controls
- Selection controls
- DateTime controls
- Scrollable controls

All control base classes now use the element-aware pattern consistently.
