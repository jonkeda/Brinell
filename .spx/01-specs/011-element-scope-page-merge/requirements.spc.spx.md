# SPX-011: Requirements Document - Element Scope and Page Object Merge

**Status:** Requirements  
**Created:** 2025-01-14  
**Author:** Copilot  

---

## 1. Introduction

### Problem Statement

Currently, the framework requires passing both:
1. **TPage** - The page type for fluent chaining  
2. **IMauiElementScope** - The scope for element finding

This creates redundancy because:
- The page object already implements `IElementScope`
- Controls need both references, leading to complex constructors
- Container controls duplicate scope behavior that pages already provide

**Current Pattern (Redundant):**
```csharp
public MauiButtonControl(TPage page, IMauiElementScope scope, Locator locator)
    : base(page, scope, locator)
```

The `page` is used for fluent returns, and `scope` is used for element finding. But when controls are created directly on a page, `page` and `scope` are the same object.

### Proposed Solution

Introduce `IPagedScope<TPage>` - a unified interface that combines:
- Element scope (element finding)
- Page reference (for fluent chaining)

**Key Principles:**
1. **Only the class where properties are defined** needs to be passed
2. **Container controls** implement `IPagedScope<TPage>` and forward `Page` to their owning page
3. Controls receive a single `IPagedScope<TPage>` parameter (scope + page in one)

**Desired Pattern:**
```csharp
// On page - page IS the scope, Page property returns itself
public MauiButtonControl<LoginPage> SubmitButton => Button(Locator.ById("submit"));

// On container - container is scope, but Page returns the owning page
container.Button(locator);  // Container.Page → LoginPage, fluent returns LoginPage
```

---

## 2. Alignment with Product Vision

This refactoring supports Brinell's goal of providing a **clean, simple API**:

1. **Reduces complexity** - Single `IPagedScope<TPage>` parameter replaces two parameters
2. **Clarifies responsibility** - Scope handles element finding, provides page for fluent returns
3. **Enables container patterns** - Containers implement scope but forward page reference
4. **Simplifies inheritance** - Base classes only need one scope reference
5. **No backward compatibility** - Clean break allows removal of non-generic classes

---

## 3. Requirements

### 3.1 User Stories

#### US-1: Simplified Control Construction
**As a** page object author  
**I want** controls to accept a single `IPagedScope<TPage>` parameter  
**So that** I don't need to pass redundant references

**Acceptance Criteria:**
- AC-1.1: Control constructors accept `IPagedScope<TPage>` + locator (no separate page)
- AC-1.2: Controls derive the page from `scope.Page`
- AC-1.3: Factory methods on pages create controls with `this` as scope
- AC-1.4: Fluent chaining returns the correct `TPage` type

#### US-2: Container Page Forwarding
**As a** page object author  
**I want** containers to implement `IPagedScope<TPage>` and return the parent page  
**So that** child controls automatically get the correct page for fluent returns

**Acceptance Criteria:**
- AC-2.1: Containers implement `IPagedScope<TPage>`
- AC-2.2: `container.Page` returns the owning page, not the container
- AC-2.3: Nested containers propagate the page through the hierarchy
- AC-2.4: Child controls in containers return `TPage`, not the container type

#### US-3: Unified IPagedScope Interface
**As a** framework developer  
**I want** a single `IPagedScope<TPage>` interface that combines scope and page access  
**So that** controls have one dependency that provides both capabilities

**Acceptance Criteria:**
- AC-3.1: `IPagedScope<TPage>` extends `IElementScope<TElement>`
- AC-3.2: `IPagedScope<TPage>` provides `TPage Page { get; }`
- AC-3.3: Pages implement `IPagedScope<TSelf>` with `Page => this`
- AC-3.4: Type safety preserved through generic constraints

---

### 3.2 Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Controls SHALL accept `IPagedScope<TPage>` as single scope/page parameter | Must |
| FR-2 | `IPagedScope<TPage>` SHALL provide `TPage Page { get; }` property | Must |
| FR-3 | Pages SHALL implement `IPagedScope<TSelf>` with `Page => this` | Must |
| FR-4 | Containers SHALL implement `IPagedScope<TPage>` with `Page => _parentPage` | Must |
| FR-5 | Fluent action methods SHALL return `scope.Page` for chaining | Must |
| FR-6 | Non-generic base classes SHALL be removed | Must |
| FR-7 | `IPagedScope<TPage>` SHALL extend `IElementScope<TElement>` | Must |

---

### 3.3 Technical Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| TR-1 | `IPagedScope<TPage, TElement>` interface SHALL be created in Brinell.Core | Must |
| TR-2 | `IMauiPagedScope<TPage>` interface SHALL be created extending `IPagedScope<TPage, AppiumElement>` | Must |
| TR-3 | `MauiControlBase<TPage>` constructor SHALL accept `IMauiPagedScope<TPage>` only | Must |
| TR-4 | `MauiPageObjectBase<TSelf>` SHALL implement `IMauiPagedScope<TSelf>` | Must |
| TR-5 | `MauiContainerBase<TPage>` SHALL implement `IMauiPagedScope<TPage>` | Must |
| TR-6 | Non-generic `MauiControlBase`, `MauiButtonControl`, `MauiEntryControl`, `MauiContainerBase`, `MauiPageObjectBase` SHALL be removed | Must |

---

## 4. Non-Functional Requirements

### 4.1 Code Architecture
- **Single Responsibility**: `IPagedScope<TPage>` combines scope + page access for controls
- **Clear Hierarchy**: Page → Container → Control with page accessible via `scope.Page`
- **No Duplication**: Single parameter replaces page + scope in constructors
- **Clean Break**: No backward compatibility - remove all non-generic versions

### 4.2 Performance
- No additional overhead from page lookup (direct property access)
- No runtime type checking required for fluent returns (generic constraint)

### 4.3 Type Safety
- `TPage` constraint ensures compile-time verification
- IntelliSense shows correct `TPage` return types throughout chain
- No casting required in control implementations

---

## 5. Out of Scope

- Changes to Blazor platform implementation (focus on MAUI first)
- Backward compatibility with non-generic classes (explicitly removed)
- Non-generic `IPagedScope` interface (only generic version needed)

---

## 6. Classes to Remove

The following non-generic classes will be **deleted** as part of this refactoring:

| Class | Location | Replacement |
|-------|----------|-------------|
| `MauiControlBase` | Brinell.Maui | `MauiControlBase<TPage>` only |
| `MauiButtonControl` | Brinell.Maui | `MauiButtonControl<TPage>` only |
| `MauiEntryControl` | Brinell.Maui | `MauiEntryControl<TPage>` only |
| `MauiContainerBase` | Brinell.Maui | `MauiContainerBase<TPage>` only |
| `MauiPageObjectBase` | Brinell.Maui | `MauiPageObjectBase<TSelf>` only |

---

## 7. Interface Design

### 7.1 Core Interface: `IPagedScope<TPage, TElement>`

```csharp
/// <summary>
/// Represents an element scope that provides access to its owning page.
/// Used by controls to get both element-finding and fluent-chaining capabilities.
/// </summary>
public interface IPagedScope<TPage, TElement> : IElementScope<TElement>
    where TPage : IPageObject
{
    /// <summary>
    /// Gets the page that owns this scope.
    /// For pages, returns 'this'. For containers, returns the parent page.
    /// </summary>
    TPage Page { get; }
}
```

### 7.2 MAUI-Specific Interface: `IMauiPagedScope<TPage>`

```csharp
/// <summary>
/// MAUI-specific paged scope using AppiumElement.
/// </summary>
public interface IMauiPagedScope<TPage> : IPagedScope<TPage, AppiumElement>, IMauiElementScope
    where TPage : IPageObject
{
}
```

### 7.3 Implementation Pattern

**Page:**
```csharp
public abstract class MauiPageObjectBase<TSelf> : IMauiPagedScope<TSelf>
    where TSelf : MauiPageObjectBase<TSelf>
{
    public TSelf Page => (TSelf)this;
    // ... rest of implementation
}
```

**Container:**
```csharp
public abstract class MauiContainerBase<TPage> : IMauiPagedScope<TPage>
    where TPage : IPageObject
{
    private readonly TPage _page;
    public TPage Page => _page;  // Returns owning page, not self
    // ... rest of implementation
}
```

**Control:**
```csharp
public class MauiButtonControl<TPage> : MauiControlBase<TPage>, IClickableControlObject<TPage>
    where TPage : IPageObject
{
    public MauiButtonControl(IMauiPagedScope<TPage> scope, Locator locator)
        : base(scope, locator)
    { }
    
    public TPage Click(int? timeoutMs = null)
    {
        // ... click logic
        return Scope.Page;  // Return page for fluent chaining
    }
}
```
---

## 8. Current vs Proposed Architecture

### Current (Redundant)

```
Controls receive: (TPage page, IMauiElementScope scope, Locator locator)
                         │                    │
                         │                    └── Used for element finding
                         └── Used for fluent returns
                         
Problem: When page IS the scope, both parameters point to same object
```

### Proposed (Unified)

```
Controls receive: (IMauiPagedScope<TPage> scope, Locator locator)
                              │
                              ├── scope.FindElement() → element finding
                              └── scope.Page → fluent returns
                              
Benefit: Single parameter provides both capabilities
```

**Usage Comparison:**

| Scenario | Current | Proposed |
|----------|---------|----------|
| Button on page | `new MauiButtonControl(page, page, locator)` | `new MauiButtonControl(this, locator)` |
| Button in container | `new MauiButtonControl(page, container, locator)` | `new MauiButtonControl(container, locator)` |
| Fluent return | `return _page;` | `return _scope.Page;` |

---

## 9. Migration Tasks

1. ✅ Create `IPagedScope<TPage, TElement>` interface in Brinell.Core
2. ✅ Create `IMauiPagedScope<TPage>` interface in Brinell.Maui
3. Update `MauiPageObjectBase<TSelf>` to implement `IMauiPagedScope<TSelf>`
4. Update `MauiContainerBase<TPage>` to implement `IMauiPagedScope<TPage>`
5. Update `MauiControlBase<TPage>` to accept `IMauiPagedScope<TPage>`
6. Update all control classes (`MauiButtonControl<TPage>`, `MauiEntryControl<TPage>`, etc.)
7. Delete non-generic base classes
8. Update unit tests

---

## Next Steps

After requirements approval, proceed to **Design Phase** to define:
- Complete interface definitions with all members
- Updated class hierarchies
- Factory method changes
- Unit test updates

When you're ready for the Design phase, say **'design'**.
