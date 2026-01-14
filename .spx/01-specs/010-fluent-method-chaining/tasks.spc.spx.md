# SPX-010: Tasks Document - Fluent Method Chaining

**Status:** Tasks  
**Created:** 2025-01-14  
**Author:** Copilot  

---

## Task Format Legend

- `[ ]` = Pending
- `[-]` = In-progress  
- `[x]` = Completed

---

## Phase 1: Core Interfaces

### [x] 1. Update IClickableControlObject Interface
- **File:** `srcnew/Brinell.Core/Interfaces/IClickableControlObject.cs`
- **Purpose:** Add generic `TPage` parameter to enable fluent method chaining for click actions
- **Changes:**
  - Add generic type parameter `<TPage>` with constraint `where TPage : IPageObject`
  - Change `Click()`, `DoubleClick()`, `RightClick()` return types from `void` to `TPage`
  - Keep state methods (`IsClickable`, `WaitClickable`, `AssertClickable`) unchanged
- _Leverage: Existing `IClickableControlObject.cs` interface definition_
- _Requirements: TR-1, TR-2, TR-3, FR-1_
- _Prompt: Role: C# Interface Designer specializing in generic type systems | Task: Update IClickableControlObject to IClickableControlObject<TPage> per design.spc.spx.md section 4.1, adding TPage constraint and changing action method return types | Restrictions: Do NOT modify Is/Wait/Assert method signatures, maintain IControlObject inheritance | Success: Interface compiles, action methods return TPage, constraint enforces IPageObject_

### [x] 2. Update IEditableTextControlObject Interface
- **File:** `srcnew/Brinell.Core/Interfaces/IEditableTextControlObject.cs`
- **Purpose:** Add generic `TPage` parameter to enable fluent method chaining for text entry actions
- **Changes:**
  - Add generic type parameter `<TPage>` with constraint `where TPage : IPageObject`
  - Change `Enter()`, `Clear()`, `SetText()` return types from `void` to `TPage`
  - Keep state methods (`GetPlaceholder`, `IsReadOnly`, etc.) unchanged
- _Leverage: Existing `IEditableTextControlObject.cs` interface definition_
- _Requirements: TR-1, TR-2, TR-3, FR-1_
- _Prompt: Role: C# Interface Designer specializing in generic type systems | Task: Update IEditableTextControlObject to IEditableTextControlObject<TPage> per design.spc.spx.md section 4.2, adding TPage constraint and changing action method return types | Restrictions: Do NOT modify getter/Is/Wait/Assert method signatures, maintain ITextControlObject inheritance | Success: Interface compiles, Enter/Clear/SetText return TPage, constraint enforces IPageObject_

---

## Phase 2: MAUI Control Base

### [x] 3. Update MauiControlBase to Generic
- **File:** `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`
- **Purpose:** Add generic `TPage` parameter and store page reference for fluent returns
- **Changes:**
  - Add generic type parameter `<TPage>` with constraint `where TPage : IPageObject`
  - Add `private readonly TPage _page` field
  - Add constructor parameter to accept page instance
  - Add `TPage Page` property (strongly typed)
  - Implement explicit `IControlObject.Page` for interface compatibility
- _Leverage: Existing `MauiControlBase.cs` implementation_
- _Requirements: TR-4, FR-2_
- _Prompt: Role: C# Backend Developer specializing in MAUI and generic implementations | Task: Convert MauiControlBase to MauiControlBase<TPage> per design.spc.spx.md section 4.3, storing page reference and exposing typed Page property | Restrictions: Preserve all existing functionality, maintain IControlObject interface implementation via explicit interface | Success: Class compiles with TPage constraint, Page property returns correct type, existing control functionality preserved_

---

## Phase 3: MAUI Control Implementations

### [x] 4. Update MauiButtonControl to Generic
- **File:** `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs`
- **Purpose:** Implement fluent method chaining by returning `Page` from action methods
- **Changes:**
  - Add generic type parameter `<TPage>` with constraint `where TPage : IPageObject`
  - Inherit from `MauiControlBase<TPage>`
  - Implement `IClickableControlObject<TPage>`
  - Update constructor to pass page to base
  - Change `Click()`, `DoubleClick()`, `RightClick()` to return `Page`
- _Leverage: `MauiControlBase<TPage>` from Task 3_
- _Requirements: TR-4, FR-1, FR-5_
- _Prompt: Role: C# MAUI Developer specializing in control implementations | Task: Convert MauiButtonControl to MauiButtonControl<TPage> per design.spc.spx.md section 4.4, implementing IClickableControlObject<TPage> and returning Page from action methods | Restrictions: Preserve existing click behavior and state methods, maintain element finding logic | Success: Click/DoubleClick/RightClick return Page instance, control actions execute correctly_

### [x] 5. Update MauiEntryControl to Generic
- **File:** `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs`
- **Purpose:** Implement fluent method chaining by returning `Page` from action methods
- **Changes:**
  - Add generic type parameter `<TPage>` with constraint `where TPage : IPageObject`
  - Inherit from `MauiControlBase<TPage>`
  - Implement `IEditableTextControlObject<TPage>`
  - Update constructor to pass page to base
  - Change `Enter()`, `Clear()`, `SetText()` to return `Page`
  - Implement null skip pattern (null text returns Page without action)
- _Leverage: `MauiControlBase<TPage>` from Task 3_
- _Requirements: TR-4, FR-1, FR-5, AC-2.4_
- _Prompt: Role: C# MAUI Developer specializing in control implementations | Task: Convert MauiEntryControl to MauiEntryControl<TPage> per design.spc.spx.md section 4.5, implementing IEditableTextControlObject<TPage> and returning Page from action methods | Restrictions: Preserve existing text entry behavior and state methods, implement null skip pattern | Success: Enter/Clear/SetText return Page instance, null text skips action and returns Page_

### [x] 6. Update MauiContainerBase to Generic
- **File:** `srcnew/Brinell.Maui/Controls/MauiContainerBase.cs`
- **Purpose:** Propagate page type through container hierarchy for consistent chaining
- **Changes:**
  - Add generic type parameter `<TPage>` with constraint `where TPage : IPageObject`
  - Inherit from `MauiControlBase<TPage>`
  - Update factory methods (`Button()`, `Entry()`) to pass `Page` to child controls
  - Child controls return page, not container
- _Leverage: `MauiControlBase<TPage>` from Task 3_
- _Requirements: FR-4_
- _Prompt: Role: C# MAUI Developer specializing in container patterns | Task: Convert MauiContainerBase to MauiContainerBase<TPage> per design.spc.spx.md section 5, propagating page type to child controls | Restrictions: Container factory methods must pass Page to children, controls in containers return page not container | Success: Controls created within container return page when actions execute, container scoping preserved_

---

## Phase 4: Page Object Base

### [x] 7. Update MauiPageObjectBase with CRTP Pattern
- **File:** `srcnew/Brinell.Maui/Pages/MauiPageObjectBase.cs`
- **Purpose:** Enable factory methods to return correctly typed controls using Curiously Recurring Template Pattern
- **Changes:**
  - Add generic type parameter `<TSelf>` with constraint `where TSelf : MauiPageObjectBase<TSelf>`
  - Update `Button()` factory to return `MauiButtonControl<TSelf>` and pass `(TSelf)this`
  - Update `Entry()` factory to return `MauiEntryControl<TSelf>` and pass `(TSelf)this`
  - Update `Container()` factory to return `MauiContainerBase<TSelf>` and pass `(TSelf)this`
- _Leverage: Existing `MauiPageObjectBase.cs` implementation_
- _Requirements: FR-3, FR-5, AC-3.2_
- _Prompt: Role: C# Architect specializing in generic patterns and CRTP | Task: Convert MauiPageObjectBase to MauiPageObjectBase<TSelf> per design.spc.spx.md section 4.6, implementing CRTP to enable typed control factory methods | Restrictions: Factory methods must cast this to TSelf, preserve all existing page functionality | Success: Factory methods return correctly typed controls, concrete pages inherit with LoginPage : MauiPageObjectBase<LoginPage>_

---

## Phase 5: Update Dependent Controls

### [x] 8. Update Other MAUI Controls to Generic Pattern
- **Files:** All control files in `srcnew/Brinell.Maui/Controls/`
- **Purpose:** Ensure all controls support fluent chaining consistently
- **Controls to update:**
  - `MauiLabelControl.cs` → `MauiLabelControl<TPage>` (if has action methods)
  - `MauiCheckBoxControl.cs` → `MauiCheckBoxControl<TPage>`
  - `MauiPickerControl.cs` → `MauiPickerControl<TPage>`
  - `MauiSliderControl.cs` → `MauiSliderControl<TPage>`
  - `MauiSwitchControl.cs` → `MauiSwitchControl<TPage>`
  - Any other controls with action methods
- _Leverage: Pattern from Tasks 4-5_
- _Requirements: TR-4_
- _Prompt: Role: C# MAUI Developer | Task: Apply generic TPage pattern to all remaining MAUI controls following the pattern established in Tasks 4-5 | Restrictions: Only update controls with action methods (Click, Enter, Select, Toggle, etc.), read-only controls may not need changes | Success: All controls with action methods are generic, action methods return Page_

---

## Phase 6: Verification and Testing

### [x] 9. Fix Compilation Errors
- **Files:** All modified files
- **Purpose:** Resolve any compilation errors from generic changes
- **Actions:**
  - Build solution and identify errors
  - Update any callers of modified interfaces/classes
  - Fix type inference issues
  - Update test fixtures if needed
- _Leverage: Compiler error messages_
- _Requirements: All_
- _Prompt: Role: C# Developer specializing in debugging and refactoring | Task: Build solution and fix all compilation errors resulting from generic type changes | Restrictions: Do not revert generic changes, find compatible solutions | Success: Solution builds without errors_

### [x] 10. Create Unit Tests for Fluent Chaining
- **File:** `testsnew/Brinell.Maui.UnitTests/FluentChainingTests.cs`
- **Purpose:** Verify action methods return correct page instance
- **Tests to implement:**
  - `Click_ReturnsPageInstance` - Verify Click returns the same page
  - `Enter_ReturnsPageInstance` - Verify Enter returns the same page
  - `Clear_ReturnsPageInstance` - Verify Clear returns the same page
  - `SetText_ReturnsPageInstance` - Verify SetText returns the same page
  - `NullText_SkipsActionAndReturnsPage` - Verify null skip pattern
  - `ChainedActions_ExecuteInOrder` - Verify multi-action chain
  - `ContainerControl_ReturnsPage` - Verify container children return page
- _Leverage: Existing test infrastructure in `testsnew/`_
- _Requirements: All acceptance criteria_
- _Prompt: Role: QA Engineer specializing in unit testing and xUnit | Task: Create comprehensive unit tests for fluent chaining functionality covering all acceptance criteria | Restrictions: Use Assert.Same to verify same instance, mock Appium elements appropriately | Success: All tests pass, coverage includes happy path and edge cases_

### [ ] 11. Update Sample Page Objects
- **Files:** Sample/demo page objects in `samples/` directory
- **Purpose:** Demonstrate fluent chaining in real page object examples
- **Changes:**
  - Update `LoginPage` to inherit from `MauiPageObjectBase<LoginPage>`
  - Update control property declarations
  - Add example fluent test method
- _Leverage: Design document section 4.6 usage example_
- _Requirements: NFR-3_
- _Prompt: Role: Technical Writer specializing in code examples | Task: Update sample page objects to demonstrate fluent chaining pattern per design.spc.spx.md | Restrictions: Keep examples simple and focused, show before/after comparison | Success: Sample shows working fluent chaining, serves as documentation example_

---

## Phase 7: Documentation

### [ ] 12. Update Documentation
- **Files:** Documentation in `docs/` directory
- **Purpose:** Document fluent chaining feature for test writers
- **Changes:**
  - Add fluent chaining section to test writing guide
  - Update interface usage guide with generic type examples
  - Add migration notes for existing tests
- _Leverage: Existing documentation structure_
- _Requirements: NFR-3_
- _Prompt: Role: Technical Writer specializing in developer documentation | Task: Document fluent chaining feature for test writers, including usage examples and migration guidance | Restrictions: Follow existing documentation style, keep examples practical | Success: Test writers can understand and use fluent chaining from documentation alone_

---

## Task Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| Phase 1: Core Interfaces | 1-2 | Complete |
| Phase 2: Control Base | 3 | Complete |
| Phase 3: Control Implementations | 4-6 | Complete |
| Phase 4: Page Object Base | 7 | Complete |
| Phase 5: Dependent Controls | 8 | Complete |
| Phase 6: Verification | 9-11 | In Progress |
| Phase 7: Documentation | 12 | Pending |

---

## Dependency Graph

```
Task 1 ─┐
        ├──► Task 3 ──► Task 4 ─┐
Task 2 ─┘              Task 5 ─┼──► Task 7 ──► Task 8 ──► Task 9 ──► Task 10 ──► Task 11 ──► Task 12
                       Task 6 ─┘
```

---

## Traceability Matrix

| Task | Requirements Covered |
|------|---------------------|
| 1 | TR-1, TR-2, TR-3, FR-1 |
| 2 | TR-1, TR-2, TR-3, FR-1 |
| 3 | TR-4, FR-2 |
| 4 | TR-4, FR-1, FR-5 |
| 5 | TR-4, FR-1, FR-5, AC-2.4 |
| 6 | FR-4 |
| 7 | FR-3, FR-5, AC-3.2 |
| 8 | TR-4 |
| 9 | All |
| 10 | All AC |
| 11 | NFR-3 |
| 12 | NFR-3 |
