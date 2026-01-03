# PLAN-006c: WinForms ComboBox Selection Diagnosis

## Problem Statement
WinForms ComboBox selection fails for 2 of 3 tests:
- ✅ "Admin" (index 0) - PASSES (but this is the default selection)
- ❌ "Guest" (index 2) - FAILS: returns "Admin"
- ❌ "User" (index 1) - FAILS: returns "Admin"

## Observed Behavior
- Tests run in shared app instance (class fixture)
- First test "Admin" passes because it's already selected by default
- Subsequent tests call `SelectByText()` but selection doesn't change
- `GetSelectedText()` returns "Admin" even after selection attempts

## ROOT CAUSE IDENTIFIED ✅

**The `comboBox.Expand()` method does not work for WinForms ComboBox!**

Diagnostic output:
```
=== INITIAL STATE (before expand) ===
3. Items.Length (collapsed): 0
4. comboBox.Value: 'Admin'
6. ExpandCollapse pattern: True
   State: Collapsed

=== AFTER EXPAND ===
7. State after Expand(): Collapsed   <-- STILL COLLAPSED!
8. Items.Length (expanded): 0        <-- NO ITEMS!
```

**SOLUTION FOUND:**
```
=== TRYING ALTERNATIVE: Click the combobox ===
9a. State after Click(): Expanded    <-- CLICK WORKS!
9b. Items.Length (after click): 3    <-- ITEMS NOW VISIBLE!
   Item[0]: Name='Admin', Text='Admin'
   Item[1]: Name='User', Text='User'
   Item[2]: Name='Guest', Text='Guest'
```

**Fix**: Use `comboBox.Click()` instead of `comboBox.Expand()` to open the dropdown.

## Status
- [x] Step 1: Create diagnostic test
- [x] Step 2: Analyze results - ROOT CAUSE FOUND
- [x] Step 3: Solution identified - Use Click() instead of Expand()
