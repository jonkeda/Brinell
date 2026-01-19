# Tasks Document: MAUI Base Control Hierarchy

## Task Format

- `[ ]` = Pending, `[-]` = In-progress, `[x]` = Completed
- Include File path, Purpose, _Leverage, _Requirements, and _Prompt fields

---

## Phase 1: Create Base Classes

### [x] 1. Create MauiClickableControlBase

- **File:** `srcnew/Brinell.Maui/Controls/MauiClickableControlBase.cs`
- **Purpose:** Base class implementing IClickableControlObject with Click, DoubleClick, RightClick, Hover, LongPress
- _Leverage: MauiControlBase, MauiButtonControl (existing implementation to extract)_
- _Requirements: REQ-001_
- _Prompt: Role: C# control developer | Task: Create MauiClickableControlBase<TScope> inheriting from MauiControlBase<TScope> implementing IClickableControlObject<TScope>, extracting click logic from MauiButtonControl | Restrictions: Keep all Core methods protected virtual, use RunWithElement pattern, do not duplicate code from MauiControlBase | Success: Class compiles, implements full interface, all methods follow Is/Wait/Assert pattern_

### [x] 2. Create MauiToggleControlBase

- **File:** `srcnew/Brinell.Maui/Controls/MauiToggleControlBase.cs`
- **Purpose:** Base class implementing IToggleControlObject with Toggle, Check, Uncheck, SetChecked
- _Leverage: MauiControlBase, design.spc.spx.md Toggle section_
- _Requirements: REQ-002_
- _Prompt: Role: C# control developer | Task: Create MauiToggleControlBase<TScope> inheriting from MauiControlBase<TScope> implementing IToggleControlObject<TScope>, reading toggle state from ToggleState/IsChecked attributes | Restrictions: Check/Uncheck must be no-op if already in target state, use RunWithElement pattern | Success: Class compiles, Toggle clicks element, SetChecked only toggles when needed_

### [x] 3. Create MauiRangeControlBase

- **File:** `srcnew/Brinell.Maui/Controls/MauiRangeControlBase.cs`
- **Purpose:** Base class implementing IRangeControlObject with GetValue, SetValue, Increment, Decrement
- _Leverage: MauiControlBase, design.spc.spx.md Range section_
- _Requirements: REQ-003_
- _Prompt: Role: C# control developer | Task: Create MauiRangeControlBase<TScope> inheriting from MauiControlBase<TScope> implementing IRangeControlObject<TScope>, reading value from RangeValue.Value attribute | Restrictions: SetValueCore should be abstract/virtual for platform-specific implementation, AssertValue must use tolerance comparison | Success: Class compiles, GetValue reads from element, AssertValue uses tolerance_

### [x] 4. Create MauiSelectorControlBase

- **File:** `srcnew/Brinell.Maui/Controls/MauiSelectorControlBase.cs`
- **Purpose:** Base class implementing ISelectorControlObject with SelectByText, SelectByIndex, GetSelectedText
- _Leverage: MauiControlBase, design.spc.spx.md Selector section_
- _Requirements: REQ-004_
- _Prompt: Role: C# control developer | Task: Create MauiSelectorControlBase<TScope> inheriting from MauiControlBase<TScope> implementing ISelectorControlObject<TScope> | Restrictions: SelectByTextCore should be virtual for picker-specific implementations, GetItemTexts may need platform-specific logic | Success: Class compiles, implements full interface, nullable skip pattern for all methods_

### [x] 5. Create MauiScrollableControlBase

- **File:** `srcnew/Brinell.Maui/Controls/MauiScrollableControlBase.cs`
- **Purpose:** Base class implementing IScrollableControlObject with ScrollToTop, ScrollToEnd, ScrollBy
- _Leverage: MauiControlBase, MauiListControl (existing scroll logic), Appium Actions API_
- _Requirements: REQ-005_
- _Prompt: Role: C# control developer | Task: Create MauiScrollableControlBase<TScope> inheriting from MauiControlBase<TScope> implementing IScrollableControlObject<TScope>, using Appium Actions for swipe gestures | Restrictions: Use element bounds for swipe calculations, ScrollByCore should handle both directions | Success: Class compiles, scroll methods use Actions API, GetScrollPosition returns 0-100 percentage_

### [x] 6. Create MauiExpandableControlBase

- **File:** `srcnew/Brinell.Maui/Controls/MauiExpandableControlBase.cs`
- **Purpose:** Base class implementing IExpandableControlObject with Expand, Collapse, ToggleExpanded
- _Leverage: MauiClickableControlBase (extends it), design.spc.spx.md_
- _Requirements: REQ-006_
- _Prompt: Role: C# control developer | Task: Create MauiExpandableControlBase<TScope> inheriting from MauiClickableControlBase<TScope> implementing IExpandableControlObject<TScope>, reading state from ExpandCollapseState attribute | Restrictions: Expand/Collapse must be no-op if already in target state, inherit click capability | Success: Class compiles, IsExpanded reads state correctly, Expand/Collapse are idempotent_

### [x] 7. Create MauiFocusableControlBase

- **File:** `srcnew/Brinell.Maui/Controls/MauiFocusableControlBase.cs`
- **Purpose:** Base class implementing IFocusableControlObject with Focus, Blur, IsFocused
- _Leverage: MauiControlBase, design.spc.spx.md Focus section_
- _Requirements: REQ-007_
- _Prompt: Role: C# control developer | Task: Create MauiFocusableControlBase<TScope> inheriting from MauiControlBase<TScope> implementing IFocusableControlObject<TScope> | Restrictions: Focus should click element, Blur should tab away or click elsewhere, IsFocused checks HasKeyboardFocus | Success: Class compiles, Focus/Blur work, IsFocused reads correct attribute_

### [x] 8. Create MauiSwipeableControlBase

- **File:** `srcnew/Brinell.Maui/Controls/MauiSwipeableControlBase.cs`
- **Purpose:** Base class implementing ISwipeableControlObject with SwipeLeft, SwipeRight, SwipeUp, SwipeDown
- _Leverage: MauiControlBase, Appium Actions API, design.spc.spx.md Swipeable section_
- _Requirements: REQ-008_
- _Prompt: Role: C# control developer | Task: Create MauiSwipeableControlBase<TScope> inheriting from MauiControlBase<TScope> implementing ISwipeableControlObject<TScope>, using Appium Actions for swipe gestures | Restrictions: Calculate swipe coordinates from element bounds, SwipeCore should be protected virtual | Success: Class compiles, all swipe directions work, Swipe(x,y,x,y) provides custom swipe_

### [x] 9. Create MauiRefreshableControlBase

- **File:** `srcnew/Brinell.Maui/Controls/MauiRefreshableControlBase.cs`
- **Purpose:** Base class implementing IRefreshableControlObject with PullToRefresh, IsRefreshing
- _Leverage: MauiSwipeableControlBase or MauiControlBase, design.spc.spx.md_
- _Requirements: REQ-009_
- _Prompt: Role: C# control developer | Task: Create MauiRefreshableControlBase<TScope> inheriting from MauiControlBase<TScope> implementing IRefreshableControlObject<TScope> | Restrictions: PullToRefresh uses swipe-down gesture, IsRefreshing checks for refresh indicator | Success: Class compiles, PullToRefresh performs gesture, WaitRefreshing(false) waits for completion_

---

## Phase 2: Refactor Existing Controls

### [x] 10. Refactor MauiButtonControl to use MauiClickableControlBase

- **File:** `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs`
- **Purpose:** Remove duplicated click code by inheriting from MauiClickableControlBase
- _Leverage: MauiClickableControlBase (task 1)_
- _Requirements: REQ-010_
- _Prompt: Role: C# refactoring developer | Task: Refactor MauiButtonControl to inherit from MauiClickableControlBase instead of MauiControlBase, removing all click-related methods that are now inherited | Restrictions: Keep constructors, keep any button-specific code, do NOT change public API | Success: Class compiles, existing tests pass, significantly less code_

### [x] 11. Refactor MauiTabControl to use MauiClickableControlBase

- **File:** `srcnew/Brinell.Maui/Controls/MauiTabControl.cs`
- **Purpose:** Remove duplicated click code by inheriting from MauiClickableControlBase
- _Leverage: MauiClickableControlBase (task 1)_
- _Requirements: REQ-010_
- _Prompt: Role: C# refactoring developer | Task: Refactor MauiTabControl to inherit from MauiClickableControlBase instead of MauiControlBase, keeping tab-specific code (Title, IsSelected, WaitSelected, AssertSelected) | Restrictions: Keep ITabControlObject implementation, keep constructors, do NOT change public API | Success: Class compiles, existing tests pass, click code removed_

### [x] 12. Refactor MauiFlyoutItemControl to use MauiClickableControlBase

- **File:** `srcnew/Brinell.Maui/Controls/MauiFlyoutItemControl.cs`
- **Purpose:** Remove duplicated click code by inheriting from MauiClickableControlBase
- _Leverage: MauiClickableControlBase (task 1)_
- _Requirements: REQ-010_
- _Prompt: Role: C# refactoring developer | Task: Refactor MauiFlyoutItemControl to inherit from MauiClickableControlBase instead of MauiControlBase, removing click-related methods | Restrictions: Keep Title property, keep constructors, do NOT change public API | Success: Class compiles, existing tests pass, code reduced_

---

## Phase 3: Build and Verify

### [x] 13. Build solution and fix any compilation errors

- **Command:** `dotnet build srcnew/Brinell.Maui/Brinell.Maui.csproj`
- **Purpose:** Verify all new base classes and refactored controls compile
- _Leverage: N/A_
- _Requirements: All_
- _Prompt: Role: Build engineer | Task: Build Brinell.Maui project, fix any compilation errors | Restrictions: Do not change interface designs unless required for compilation | Success: Solution builds with no errors_

### [ ] 14. Run existing unit tests

- **Command:** `dotnet test testsnew/Brinell.Maui.Tests/`
- **Purpose:** Verify refactoring didn't break existing functionality
- _Leverage: Existing test infrastructure_
- _Requirements: REQ-010_
- _Prompt: Role: QA engineer | Task: Run existing MAUI tests to verify no regressions from refactoring | Restrictions: Do not modify tests unless they test removed internals | Success: All existing tests pass_

### [ ] 15. Create base class unit tests

- **File:** `testsnew/Brinell.Maui.Tests/Controls/BaseControlTests.cs`
- **Purpose:** Test base class behavior with mocked elements
- _Leverage: Existing test patterns, Moq_
- _Requirements: All_
- _Prompt: Role: C# test developer | Task: Create unit tests for new base classes verifying Is/Wait/Assert patterns, nullable skip behavior, and Core method behavior | Restrictions: Mock IMauiElement for isolation, test one capability per test class | Success: Tests cover key methods for each base class, pass independently_

---

## Summary

| Phase | Tasks | Description |
|-------|-------|-------------|
| Phase 1 | 1-9 | Create 9 capability base classes |
| Phase 2 | 10-12 | Refactor 3 existing controls |
| Phase 3 | 13-15 | Build, verify, test |

**Total Tasks:** 15

---

**Document Version:** 1.0  
**Created:** January 19, 2026  
**Spec ID:** 004  
**Status:** Draft  
**Workflow:** spec_workflow/tasks
