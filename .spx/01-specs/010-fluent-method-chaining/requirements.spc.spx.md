# SPX-010: Fluent Method Chaining for Action Methods

**Status:** Requirements  
**Created:** 2025-01-05  
**Author:** Copilot  

---

## 1. Introduction

### Problem Statement

Currently, all action methods (`Click()`, `Enter()`, `Clear()`, etc.) return `void`, which forces test writers to use separate statements for each control interaction:

```csharp
// Current verbose pattern
loginPage.Username.Enter("testuser");
loginPage.Password.Enter("testpass");
loginPage.LoginButton.Click();
```

This pattern is repetitive and doesn't follow the fluent API pattern that many modern test frameworks use.

### Proposed Solution

Modify action methods to return the parent page object, enabling fluent method chaining:

```csharp
// Desired fluent pattern
loginPage
    .Username.Enter("testuser")
    .Password.Enter("testpass")
    .LoginButton.Click();
```

---

## 2. Alignment with Product Vision

This feature directly supports Brinell's goal of providing a **clean, readable test API** that:

1. **Reduces test verbosity** - Eliminates repeated page variable references
2. **Improves readability** - Tests read like natural user flows
3. **Maintains type safety** - Generic constraints ensure compile-time verification
4. **Follows industry patterns** - Aligns with Fluent Assertions, Page Object Model best practices

---

## 3. Requirements

### 3.1 User Stories

#### US-1: Fluent Click Chaining
**As a** test writer  
**I want** the `Click()` method to return the page where the control is defined  
**So that** I can chain multiple control interactions in a single statement

**Acceptance Criteria:**
- AC-1.1: `Click()` returns the parent page object type
- AC-1.2: Return type is strongly typed (not `object` or base class)
- AC-1.3: Existing void-returning behavior remains available for backward compatibility
- AC-1.4: Chained calls work with IntelliSense for next control selection

#### US-2: Fluent Text Entry Chaining
**As a** test writer  
**I want** the `Enter()` and `Clear()` methods to return the page  
**So that** I can chain text input operations with other control interactions

**Acceptance Criteria:**
- AC-2.1: `Enter()` returns the parent page object type
- AC-2.2: `Clear()` returns the parent page object type
- AC-2.3: `SetText()` returns the parent page object type
- AC-2.4: Methods work with nullable skip pattern (null text skips and returns page)

#### US-3: Generic Type Safety
**As a** framework developer  
**I want** fluent methods to use generic type parameters  
**So that** return types are compile-time verified and IntelliSense works correctly

**Acceptance Criteria:**
- AC-3.1: Controls have generic type parameter for parent page type `<TPage>`
- AC-3.2: Factory methods preserve type information through generics
- AC-3.3: No runtime casting required in test code
- AC-3.4: Type constraints ensure `TPage` is a valid page object

---

### 3.2 Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Action methods (Click, DoubleClick, RightClick, Enter, Clear, SetText) shall return the parent page object | Must |
| FR-2 | Controls shall track their parent page via generic type parameter | Must |
| FR-3 | Page factory methods shall create controls with correct generic type binding | Must |
| FR-4 | Nested container controls shall return the page, not the container | Should |
| FR-5 | Return type shall be the concrete page type, not IPageObject | Must |

---

### 3.3 Technical Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| TR-1 | Existing interfaces shall be made generic (IClickableControlObject<TPage>, IEditableTextControlObject<TPage>) | Must |
| TR-2 | Action methods shall return TPage instead of void | Must |
| TR-3 | Generic constraints shall enforce TPage : IPageObject | Must |
| TR-4 | MAUI controls shall be generic with TPage type parameter | Must |

---

## 4. Non-Functional Requirements

| ID | Requirement | Category |
|----|-------------|----------|
| NFR-1 | IntelliSense shall show correct return type and available properties | Usability |
| NFR-2 | No performance overhead from generic implementation | Performance |
| NFR-3 | Documentation shall include fluent chaining examples | Documentation |

---

## 5. Out of Scope

- Async/await fluent chaining (future consideration)
- Cross-page navigation chaining (Click returns different page type)
- Fluent assertion chaining (use existing Assert methods)

---

## 6. Design Considerations

### 6.1 Interface Design

**Direct Generic Interfaces (Breaking Change - Accepted)**
```csharp
public interface IClickableControlObject<TPage> : IControlObject
    where TPage : IPageObject
{
    TPage Click(int? timeoutMs = null);
    TPage DoubleClick(int? timeoutMs = null);
    TPage RightClick(int? timeoutMs = null);
    // Is/Wait/Assert methods unchanged
}
```

### 6.2 Implementation Pattern

```csharp
public class MauiButtonControl<TPage> : MauiControlBase, IClickableControlObject<TPage>
    where TPage : IPageObject
{
    private readonly TPage _page;
    
    public MauiButtonControl(TPage page, IMauiElementScope scope, Locator locator)
        : base(scope, locator)
    {
        _page = page;
    }
    
    public TPage Click(int? timeoutMs = null)
    {
        CheckClickable();
        var element = FindElement();
        element.Click();
        return _page;
    }
}
```

---

## 7. Dependencies

- Core interfaces: IControlObject, IClickableControlObject, IEditableTextControlObject
- MAUI implementations: MauiButtonControl, MauiEntryControl
- Page base: MauiPageObjectBase

---

## 8. Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Generic complexity confuses test writers | Medium | Clear documentation, examples |
| Container scoping with generics | Medium | Design phase analysis |

---

## Next Steps

1. **Design Phase** - Define complete interface signatures and implementation patterns
2. **Tasks Phase** - Break down into implementable work items
3. **Implement Phase** - Create generic interfaces and update MAUI controls
