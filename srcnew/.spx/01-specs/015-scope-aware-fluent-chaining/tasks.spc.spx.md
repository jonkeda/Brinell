# SPX-015: Tasks - Scope-Aware Fluent Chaining

**Status:** Ready for Implementation  
**Created:** 2025-01-14  
**Author:** Copilot  
**Design:** SPX-015 Design Document  
**Requirements:** SPX-015 Requirements Document

---

## Overview

This document tracks implementation tasks for the scope-aware fluent chaining refactor. The key change is that controls return their containing scope (page or container) instead of always returning the page. Containers use a simplified `<TParent, TSelf>` pattern where `TParent` can be either a page or another container.

---

## Phase 1: Create New Interfaces

### Task 1.1: Create IMauiScope<TScope> Interface
**File:** `srcnew/Brinell.Maui/Interfaces/IMauiScope.cs`  
**Status:** ⬜ Not Started  
**Priority:** P0 - Critical  

**Description:**
Create the base scope interface that both pages and containers will implement.

**Implementation:**
```csharp
namespace Brinell.Maui.Interfaces;

/// <summary>
/// Base scope interface for element finding with self-referencing fluent return.
/// Both pages and containers implement this interface.
/// </summary>
/// <typeparam name="TScope">The scope type itself (self-referencing for fluent returns).</typeparam>
public interface IMauiScope<TScope> : IMauiElementScope
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Gets this scope for fluent chaining.
    /// Pages return themselves. Containers return themselves.
    /// </summary>
    TScope Self { get; }
}
```

**Acceptance Criteria:**
- [ ] Interface created in correct namespace
- [ ] Extends `IMauiElementScope`
- [ ] Self-referencing generic constraint applied
- [ ] XML documentation complete

---

### Task 1.2: Create IMauiPage<TSelf> Interface
**File:** `srcnew/Brinell.Maui/Interfaces/IMauiPage.cs`  
**Status:** ⬜ Not Started  
**Priority:** P0 - Critical  

**Description:**
Create the page interface that extends scope. Pages are root scopes with no Parent.

**Implementation:**
```csharp
namespace Brinell.Maui.Interfaces;

using Brinell.Core.Interfaces;

/// <summary>
/// Page interface extending scope. Pages are scopes that return themselves.
/// Pages are root scopes - they have no Parent property.
/// </summary>
/// <typeparam name="TSelf">The page type itself (self-referencing).</typeparam>
public interface IMauiPage<TSelf> : IMauiScope<TSelf>, IPageObject
    where TSelf : IMauiPage<TSelf>
{
    // Inherits:
    // - TSelf Self { get; }        from IMauiScope<TSelf>
    // - Element finding            from IMauiElementScope
    // - Page operations            from IPageObject
    // NO Parent property - pages are the root of the hierarchy
}
```

**Acceptance Criteria:**
- [ ] Interface created in correct namespace
- [ ] Extends `IMauiScope<TSelf>` and `IPageObject`
- [ ] Self-referencing generic constraint applied
- [ ] XML documentation explains no Parent property

---

### Task 1.3: Create IMauiContainer<TParent, TSelf> Interface
**File:** `srcnew/Brinell.Maui/Interfaces/IMauiContainer.cs`  
**Status:** ⬜ Not Started  
**Priority:** P0 - Critical  

**Description:**
Create the container interface with Parent property. Only ONE container interface needed - `TParent` can be page or container.

**Implementation:**
```csharp
namespace Brinell.Maui.Interfaces;

using Brinell.Core.Interfaces;

/// <summary>
/// Container interface. Containers are scopes that return themselves,
/// with access to their parent scope (page or another container).
/// </summary>
/// <typeparam name="TParent">The parent scope type (page or container).</typeparam>
/// <typeparam name="TSelf">The container type itself (self-referencing).</typeparam>
public interface IMauiContainer<TParent, TSelf> : IMauiScope<TSelf>, IContainerControl
    where TParent : IMauiScope<TParent>
    where TSelf : IMauiContainer<TParent, TSelf>
{
    /// <summary>
    /// Gets the parent scope (page or container).
    /// Navigate up the scope hierarchy by calling Parent.
    /// Chain .Parent.Parent... to reach the root page.
    /// </summary>
    TParent Parent { get; }
}
```

**Acceptance Criteria:**
- [ ] Interface created in correct namespace
- [ ] Extends `IMauiScope<TSelf>` and `IContainerControl`
- [ ] `TParent` constraint is `IMauiScope<TParent>` (not page-specific)
- [ ] `Parent` property returns `TParent`
- [ ] XML documentation complete

---

## Phase 2: Update Control Base Classes

### Task 2.1: Update MauiControlBase<TScope>
**File:** `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`  
**Status:** ⬜ Not Started  
**Priority:** P0 - Critical  

**Description:**
Refactor `MauiControlBase` to use `TScope` instead of `TPage`. The scope can be either a page or container.

**Changes:**
1. Rename generic parameter from `TPage` to `TScope`
2. Change constraint from `IPageObject` to `IMauiScope<TScope>`
3. Rename `Page` property to `Scope`
4. Update constructor to accept `IMauiScope<TScope>`
5. Remove `IPageObject` reference

**Before:**
```csharp
public class MauiControlBase<TPage> : MauiObjectBase, IControlObject
    where TPage : IPageObject
{
    private readonly IMauiPagedScope<TPage> _scope;
    public TPage Page => _scope.Page;
}
```

**After:**
```csharp
public class MauiControlBase<TScope> : MauiObjectBase, IControlObject
    where TScope : IMauiScope<TScope>
{
    private readonly IMauiScope<TScope> _scope;
    protected TScope Scope => _scope.Self;
}
```

**Acceptance Criteria:**
- [ ] Generic parameter renamed to `TScope`
- [ ] Constraint changed to `IMauiScope<TScope>`
- [ ] `Scope` property returns `_scope.Self`
- [ ] Constructor accepts `IMauiScope<TScope>`
- [ ] All internal references updated
- [ ] Compiles without errors

---

### Task 2.2: Update MauiButtonControl<TScope>
**File:** `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs`  
**Status:** ⬜ Not Started  
**Priority:** P1 - High  

**Description:**
Update button control to return `Scope` instead of `Page` from all action methods.

**Changes:**
1. Rename generic parameter from `TPage` to `TScope`
2. Change constraint to `IMauiScope<TScope>`
3. Update all action methods to return `Scope`

**Methods to Update:**
- `Click()` → return `Scope`
- `DoubleClick()` → return `Scope`
- `RightClick()` → return `Scope`
- `AssertClickable()` → return `Scope`

**Acceptance Criteria:**
- [ ] All action methods return `Scope`
- [ ] Constraint updated to `IMauiScope<TScope>`
- [ ] Compiles without errors
- [ ] Interface `IClickableControlObject<TScope>` implemented

---

### Task 2.3: Update MauiEntryControl<TScope>
**File:** `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs`  
**Status:** ⬜ Not Started  
**Priority:** P1 - High  

**Description:**
Update entry control to return `Scope` instead of `Page` from all action methods.

**Changes:**
1. Rename generic parameter from `TPage` to `TScope`
2. Change constraint to `IMauiScope<TScope>`
3. Update all action methods to return `Scope`

**Methods to Update:**
- `Enter()` → return `Scope`
- `Clear()` → return `Scope`
- `SetText()` → return `Scope`
- `AssertText()` → return `Scope`
- `AssertTextMatches()` → return `Scope`

**Acceptance Criteria:**
- [ ] All action methods return `Scope`
- [ ] Constraint updated to `IMauiScope<TScope>`
- [ ] Compiles without errors

---

### Task 2.4: Refactor MauiContainerBase<TParent, TSelf>
**File:** `srcnew/Brinell.Maui/Controls/MauiContainerBase.cs`  
**Status:** ⬜ Not Started  
**Priority:** P0 - Critical  

**Description:**
Completely refactor container base to use simplified `<TParent, TSelf>` pattern. Remove `TPage` parameter entirely.

**Changes:**
1. Change type parameters from `<TPage>` to `<TParent, TSelf>`
2. Add `TParent : IMauiScope<TParent>` constraint
3. Add `TSelf : MauiContainerBase<TParent, TSelf>` constraint
4. Add `Parent` property returning `TParent`
5. Add `Self` property returning `TSelf`
6. Remove `Page` property
7. Update base class to `MauiControlBase<TParent>`
8. Update factory methods to create controls with `TSelf` scope

**Before:**
```csharp
public class MauiContainerBase<TPage> : MauiControlBase<TPage>, ...
    where TPage : IPageObject
{
    public TPage Page => _scope.Page;
}
```

**After:**
```csharp
public abstract class MauiContainerBase<TParent, TSelf> : MauiControlBase<TParent>, IMauiContainer<TParent, TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf : MauiContainerBase<TParent, TSelf>
{
    public TSelf Self => (TSelf)this;
    public TParent Parent { get; }
    
    // Factory methods create controls scoped to TSelf
    protected MauiButtonControl<TSelf> Button(Locator locator) => ...
}
```

**Acceptance Criteria:**
- [ ] Type parameters changed to `<TParent, TSelf>`
- [ ] `Parent` property added and working
- [ ] `Self` property returns `(TSelf)this`
- [ ] Factory methods create controls with `TSelf` scope
- [ ] Implements `IMauiContainer<TParent, TSelf>`
- [ ] No `Page` property (removed)
- [ ] Compiles without errors

---

## Phase 3: Update Page Base Class

### Task 3.1: Update MauiPageObjectBase<TSelf>
**File:** `srcnew/Brinell.Maui/Pages/MauiPageObjectBase.cs`  
**Status:** ⬜ Not Started  
**Priority:** P0 - Critical  

**Description:**
Update page base to implement `IMauiPage<TSelf>` and add `Self` property.

**Changes:**
1. Implement `IMauiPage<TSelf>` interface
2. Add `Self` property returning `(TSelf)this`
3. Update factory methods to use `TSelf` as scope
4. Ensure `Container<T>` factory creates containers with `TSelf` as parent

**Implementation:**
```csharp
public abstract class MauiPageObjectBase<TSelf> : MauiObjectBase, IMauiPage<TSelf>
    where TSelf : MauiPageObjectBase<TSelf>
{
    public TSelf Self => (TSelf)this;
    
    protected MauiButtonControl<TSelf> Button(Locator locator)
        => new MauiButtonControl<TSelf>(this, locator);
    
    protected TContainer Container<TContainer>(Locator locator)
        where TContainer : IMauiContainer<TSelf, TContainer>
        => // Create container with this as parent
}
```

**Acceptance Criteria:**
- [ ] Implements `IMauiPage<TSelf>`
- [ ] `Self` property returns `(TSelf)this`
- [ ] Factory methods create controls scoped to `TSelf`
- [ ] Container factory constraint updated to `IMauiContainer<TSelf, TContainer>`
- [ ] Compiles without errors

---

## Phase 4: Update Core Interfaces

### Task 4.1: Update IClickableControlObject<TScope>
**File:** `srcnew/Brinell.Core/Interfaces/IClickableControlObject.cs`  
**Status:** ⬜ Not Started  
**Priority:** P1 - High  

**Description:**
Update interface to use `TScope` instead of `TPage`.

**Changes:**
1. Rename generic parameter from `TPage` to `TScope`
2. Update constraint (or remove page-specific constraint)
3. Update return types of all action methods

**Acceptance Criteria:**
- [ ] Generic parameter renamed to `TScope`
- [ ] Constraint is `class` (scope constraint at implementation level)
- [ ] All action methods return `TScope`

---

### Task 4.2: Update ITextControlObject<TScope> (if exists)
**File:** `srcnew/Brinell.Core/Interfaces/ITextControlObject.cs`  
**Status:** ⬜ Not Started  
**Priority:** P1 - High  

**Description:**
Update text control interface to use `TScope`.

**Acceptance Criteria:**
- [ ] Generic parameter uses `TScope`
- [ ] All action methods return `TScope`

---

### Task 4.3: Update IContainerControl Interface
**File:** `srcnew/Brinell.Core/Interfaces/IContainerControl.cs`  
**Status:** ⬜ Not Started  
**Priority:** P2 - Medium  

**Description:**
Review and update `IContainerControl` if needed for the new container pattern.

**Acceptance Criteria:**
- [ ] Interface compatible with new container design
- [ ] No breaking changes to core contract

---

## Phase 5: Remove Obsolete Files

### Task 5.1: Remove IMauiPagedScope<TPage>
**File:** `srcnew/Brinell.Maui/Interfaces/IMauiPagedScope.cs`  
**Status:** ⬜ Not Started  
**Priority:** P2 - Medium  
**Depends On:** Tasks 1.1, 1.2, 1.3, 2.1, 2.4, 3.1

**Description:**
Remove obsolete interface that has been replaced by `IMauiScope<TScope>`.

**Pre-Requisites:**
- All classes updated to use new interfaces
- No remaining references to `IMauiPagedScope`

**Acceptance Criteria:**
- [ ] File deleted
- [ ] No compilation errors
- [ ] No remaining references in codebase

---

### Task 5.2: Clean Up IPagedScope<TPage, TElement>
**File:** `srcnew/Brinell.Core/Interfaces/IPagedScope.cs`  
**Status:** ⬜ Not Started  
**Priority:** P2 - Medium  

**Description:**
Review and remove or update `IPagedScope` in Core if no longer needed.

**Acceptance Criteria:**
- [ ] Interface removed or updated
- [ ] No compilation errors

---

## Phase 6: Update Tests

### Task 6.1: Update Unit Tests for Control Base
**Status:** ⬜ Not Started  
**Priority:** P1 - High  
**Depends On:** Phase 2

**Description:**
Update existing unit tests for `MauiControlBase` to use new scope pattern.

**Changes:**
- Update mock setup to use `IMauiScope<TScope>`
- Update assertions for `Scope` property instead of `Page`

**Acceptance Criteria:**
- [ ] All existing tests pass
- [ ] Tests verify `Scope` returns correct value

---

### Task 6.2: Create Tests for Parent Navigation
**Status:** ⬜ Not Started  
**Priority:** P1 - High  
**Depends On:** Phases 1-4

**Description:**
Create new tests to verify parent navigation works correctly.

**Test Cases:**
- [ ] Container.Parent returns correct page
- [ ] Nested Container.Parent returns parent container
- [ ] Nested Container.Parent.Parent returns page
- [ ] Page has no Parent (compile-time verified)

---

### Task 6.3: Create Integration Tests for Fluent Chaining
**Status:** ⬜ Not Started  
**Priority:** P1 - High  
**Depends On:** Phases 1-4

**Description:**
Create integration tests demonstrating the fluent chaining patterns.

**Test Cases:**
- [ ] Page control actions return page
- [ ] Container control actions return container
- [ ] Nested container actions return nested container
- [ ] Parent navigation works through hierarchy
- [ ] Mixed scope operations work correctly

---

## Phase 7: Documentation

### Task 7.1: Update Inline Documentation
**Status:** ⬜ Not Started  
**Priority:** P2 - Medium  
**Depends On:** Phases 1-4

**Description:**
Ensure all updated classes have correct XML documentation.

**Acceptance Criteria:**
- [ ] All public members documented
- [ ] Examples in documentation reflect new patterns
- [ ] Navigation patterns explained

---

### Task 7.2: Update README/Getting Started
**Status:** ⬜ Not Started  
**Priority:** P3 - Low  
**Depends On:** Phase 6

**Description:**
Update any documentation showing the new fluent chaining patterns.

---

## Summary Table

| Phase | Task | Status | Priority | Dependencies |
|-------|------|--------|----------|--------------|
| 1 | Create IMauiScope | ⬜ | P0 | - |
| 1 | Create IMauiPage | ⬜ | P0 | 1.1 |
| 1 | Create IMauiContainer | ⬜ | P0 | 1.1 |
| 2 | Update MauiControlBase | ⬜ | P0 | 1.1 |
| 2 | Update MauiButtonControl | ⬜ | P1 | 2.1 |
| 2 | Update MauiEntryControl | ⬜ | P1 | 2.1 |
| 2 | Refactor MauiContainerBase | ⬜ | P0 | 1.3, 2.1 |
| 3 | Update MauiPageObjectBase | ⬜ | P0 | 1.2 |
| 4 | Update IClickableControlObject | ⬜ | P1 | - |
| 4 | Update ITextControlObject | ⬜ | P1 | - |
| 4 | Update IContainerControl | ⬜ | P2 | - |
| 5 | Remove IMauiPagedScope | ⬜ | P2 | 1-4 |
| 5 | Clean Up IPagedScope | ⬜ | P2 | 1-4 |
| 6 | Update Unit Tests | ⬜ | P1 | 2 |
| 6 | Create Parent Navigation Tests | ⬜ | P1 | 1-4 |
| 6 | Create Integration Tests | ⬜ | P1 | 1-4 |
| 7 | Update Inline Documentation | ⬜ | P2 | 1-4 |
| 7 | Update README | ⬜ | P3 | 6 |

---

## Implementation Order

**Recommended sequence:**

1. **Phase 1** - Create new interfaces (foundation)
2. **Phase 4** - Update Core interfaces (IClickableControlObject, etc.)
3. **Phase 2** - Update control base classes
4. **Phase 3** - Update page base class
5. **Phase 5** - Remove obsolete files
6. **Phase 6** - Update and create tests
7. **Phase 7** - Documentation

**Critical Path:** 1.1 → 1.2/1.3 → 2.1 → 2.4 → 3.1 → Build & Test

---

## Notes

- Per tech.md: **No backward compatibility** required during pre-release
- Breaking changes are acceptable for cleaner design
- Focus on MAUI first, Blazor implementation deferred
