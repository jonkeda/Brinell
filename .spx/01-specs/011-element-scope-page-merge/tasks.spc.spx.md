# SPX-011: Tasks Document - Element Scope and Page Object Merge

**Status:** Tasks  
**Created:** 2025-01-14  
**Author:** Copilot  
**Spec:** [requirements.spc.spx.md](requirements.spc.spx.md) | [design.spc.spx.md](design.spc.spx.md)

---

## Task Format

- `[ ]` = Pending, `[-]` = In-progress, `[x]` = Completed
- Include File path, Purpose, _Leverage, _Requirements, and _Prompt fields

---

## Phase 1: Core Interface Creation

### [ ] 1. Create IPagedScope<TPage, TElement> interface in Brinell.Core

- **File:** `srcnew/Brinell.Core/Interfaces/IPagedScope.cs`
- **Purpose:** Create the unified interface combining element scope with page access
- **_Leverage:** `srcnew/Brinell.Core/Interfaces/IElementScope.cs`, `srcnew/Brinell.Core/Interfaces/IPageObject.cs`
- **_Requirements:** FR-2, FR-7, TR-1
- **_Prompt:** Role: C# Interface Designer | Task: Create IPagedScope<TPage, TElement> interface extending IElementScope<TElement> with TPage Page property, following design.spc.spx.md Section 5.1 | Restrictions: Do not modify existing IElementScope, use proper generic constraints (TPage : IPageObject) | Success: Interface compiles, extends IElementScope<TElement>, provides typed Page property

---

## Phase 2: MAUI Interface Creation

### [ ] 2. Create IMauiPagedScope<TPage> interface in Brinell.Maui

- **File:** `srcnew/Brinell.Maui/Interfaces/IMauiPagedScope.cs`
- **Purpose:** Create MAUI-specific paged scope combining IPagedScope with IMauiElementScope
- **_Leverage:** `srcnew/Brinell.Maui/Interfaces/IMauiElementScope.cs`, `srcnew/Brinell.Core/Interfaces/IPagedScope.cs`
- **_Requirements:** FR-7, TR-2
- **_Prompt:** Role: C# Interface Designer | Task: Create IMauiPagedScope<TPage> extending both IPagedScope<TPage, AppiumElement> and IMauiElementScope, following design.spc.spx.md Section 5.2 | Restrictions: No new members needed, just combine existing interfaces | Success: Interface compiles, multiple inheritance works correctly, provides Page, Context, and element finding

---

## Phase 3: Page Object Updates

### [ ] 3. Update MauiPageObjectBase<TSelf> to implement IMauiPagedScope<TSelf>

- **File:** `srcnew/Brinell.Maui/Pages/MauiPageObjectBase.cs`
- **Purpose:** Make page objects implement the new unified interface with Page => this
- **_Leverage:** `srcnew/Brinell.Maui/Interfaces/IMauiPagedScope.cs`
- **_Requirements:** FR-3, TR-4, AC-3.3
- **_Prompt:** Role: C# Developer | Task: Update MauiPageObjectBase<TSelf> to implement IMauiPagedScope<TSelf>, add Page property returning (TSelf)this, following design.spc.spx.md Section 6.5 | Restrictions: Maintain CRTP pattern, keep existing IMauiElementScope functionality | Success: Page compiles, implements IMauiPagedScope<TSelf>, Page property returns typed self

---

## Phase 4: Control Base Class Updates

### [ ] 4. Simplify MauiControlBase<TPage> constructor

- **File:** `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`
- **Purpose:** Change constructor from (TPage, IMauiElementScope, Locator) to (IMauiPagedScope<TPage>, Locator)
- **_Leverage:** `srcnew/Brinell.Maui/Interfaces/IMauiPagedScope.cs`
- **_Requirements:** FR-1, TR-3, AC-1.1, AC-1.2
- **_Prompt:** Role: C# Developer | Task: Refactor MauiControlBase<TPage> constructor to accept IMauiPagedScope<TPage> only, derive Page from scope.Page, following design.spc.spx.md Section 6.1 | Restrictions: Remove _page field, keep all existing methods working, Page property delegates to scope | Success: Constructor simplified, all existing functionality preserved, Page property works

### [ ] 5. Update MauiButtonControl<TPage> constructor

- **File:** `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs`
- **Purpose:** Simplify constructor to use new base class signature
- **_Leverage:** `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`
- **_Requirements:** FR-1, AC-1.1
- **_Prompt:** Role: C# Developer | Task: Update MauiButtonControl<TPage> constructor to (IMauiPagedScope<TPage>, Locator), calling base(scope, locator), following design.spc.spx.md Section 6.2 | Restrictions: No other changes needed, fluent methods continue returning Page | Success: Constructor simplified, Click/DoubleClick/RightClick still return Page

### [ ] 6. Update MauiEntryControl<TPage> constructor

- **File:** `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs`
- **Purpose:** Simplify constructor to use new base class signature
- **_Leverage:** `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`
- **_Requirements:** FR-1, AC-1.1
- **_Prompt:** Role: C# Developer | Task: Update MauiEntryControl<TPage> constructor to (IMauiPagedScope<TPage>, Locator), calling base(scope, locator), following design.spc.spx.md Section 6.3 | Restrictions: No other changes needed, fluent methods continue returning Page | Success: Constructor simplified, Enter/Clear still return Page

---

## Phase 5: Container Updates

### [ ] 7. Update MauiContainerBase<TPage> to implement IMauiPagedScope<TPage>

- **File:** `srcnew/Brinell.Maui/Controls/MauiContainerBase.cs`
- **Purpose:** Container implements IMauiPagedScope<TPage> so child controls get page through container
- **_Leverage:** `srcnew/Brinell.Maui/Interfaces/IMauiPagedScope.cs`
- **_Requirements:** FR-4, TR-5, AC-2.1, AC-2.2
- **_Prompt:** Role: C# Developer | Task: Update MauiContainerBase<TPage> to implement IMauiPagedScope<TPage>, constructor to (IMauiPagedScope<TPage>, Locator), Page property returns parent page via base.Page, following design.spc.spx.md Section 6.4 | Restrictions: Container's Page must forward to parent page (not itself), keep all IMauiElementScope functionality | Success: Container implements IMauiPagedScope, child controls get correct TPage

---

## Phase 6: Factory Method Updates

### [ ] 8. Update factory methods in MauiPageObjectBase<TSelf>

- **File:** `srcnew/Brinell.Maui/Pages/MauiPageObjectBase.cs`
- **Purpose:** Simplify factory methods from new Control(this, this, locator) to new Control(this, locator)
- **_Leverage:** N/A
- **_Requirements:** AC-1.3
- **_Prompt:** Role: C# Developer | Task: Update Button(), Entry(), Container(), Control() factory methods to pass only 'this' as scope (remove duplicate page parameter), following design.spc.spx.md Section 6.5 | Restrictions: Keep method signatures unchanged, only change internal implementation | Success: Factory methods create controls with single scope parameter

### [ ] 9. Update factory methods in MauiContainerBase<TPage>

- **File:** `srcnew/Brinell.Maui/Controls/MauiContainerBase.cs`
- **Purpose:** Add/update factory methods to pass container as scope
- **_Leverage:** N/A
- **_Requirements:** AC-2.2, AC-2.4
- **_Prompt:** Role: C# Developer | Task: Update or add factory methods (Button, Entry, etc.) to pass 'this' as scope, following design.spc.spx.md Section 6.4 usage example | Restrictions: Child controls must receive container (which provides parent page) | Success: Container factory methods create controls that return TPage for fluent chaining

---

## Phase 7: Cleanup

### [ ] 10. Remove non-generic base classes (if they exist)

- **Files:** Check for non-generic versions in:
  - `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`
  - `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs`
  - `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs`
  - `srcnew/Brinell.Maui/Controls/MauiContainerBase.cs`
  - `srcnew/Brinell.Maui/Pages/MauiPageObjectBase.cs`
- **Purpose:** Remove non-generic classes per TR-6
- **_Leverage:** N/A
- **_Requirements:** FR-6, TR-6
- **_Prompt:** Role: C# Developer | Task: Search for and remove any non-generic versions of MauiControlBase, MauiButtonControl, MauiEntryControl, MauiContainerBase, MauiPageObjectBase | Restrictions: Only remove non-generic versions, keep generic versions intact | Success: Only generic versions remain, project compiles

---

## Phase 8: Testing

### [ ] 11. Update unit tests for new constructor signatures

- **File:** `testsnew/Brinell.Maui.Tests/FluentChainingTests.cs`
- **Purpose:** Update existing tests to use new IMauiPagedScope-based construction
- **_Leverage:** Existing test patterns
- **_Requirements:** All
- **_Prompt:** Role: C# Test Developer | Task: Update FluentChainingTests to work with new constructor signatures, verify fluent chaining still works correctly | Restrictions: Keep existing test coverage, update mock setup as needed | Success: All existing tests pass with new constructors

### [ ] 12. Add interface implementation tests

- **File:** `testsnew/Brinell.Maui.Tests/PagedScopeTests.cs` (new)
- **Purpose:** Verify interface implementation correctness
- **_Leverage:** `testsnew/Brinell.Maui.Tests/FluentChainingTests.cs`
- **_Requirements:** AC-3.1, AC-3.2, AC-3.3, AC-3.4
- **_Prompt:** Role: C# Test Developer | Task: Create tests verifying: 1) MauiPageObjectBase implements IMauiPagedScope<TSelf>, 2) Page property returns self, 3) MauiContainerBase implements IMauiPagedScope<TPage>, 4) Container.Page returns parent page | Restrictions: Use similar mock patterns as FluentChainingTests | Success: All interface implementations verified, type safety confirmed

---

## Phase 9: Build Verification

### [ ] 13. Build all projects and fix any errors

- **Command:** `dotnet build srcnew/Brinell.Core/Brinell.Core.csproj && dotnet build srcnew/Brinell.Maui/Brinell.Maui.csproj && dotnet build testsnew/Brinell.Maui.Tests/Brinell.Maui.Tests.csproj`
- **Purpose:** Verify all changes compile correctly across all target frameworks
- **_Leverage:** N/A
- **_Requirements:** All
- **_Prompt:** Role: Build Engineer | Task: Build all projects (Brinell.Core, Brinell.Maui, Brinell.Maui.Tests), fix any compilation errors | Restrictions: All three target frameworks (net8.0, net9.0, net10.0) must compile | Success: All projects build without errors

---

## Summary

| Phase | Tasks | Description |
|-------|-------|-------------|
| 1 | 1 | Core interface creation |
| 2 | 2 | MAUI interface creation |
| 3 | 3 | Page object updates |
| 4 | 4-6 | Control base class updates |
| 5 | 7 | Container updates |
| 6 | 8-9 | Factory method updates |
| 7 | 10 | Cleanup non-generic classes |
| 8 | 11-12 | Testing |
| 9 | 13 | Build verification |

**Total Tasks:** 13

---

## Next Steps

When you're ready to implement, say **'implement'**.
