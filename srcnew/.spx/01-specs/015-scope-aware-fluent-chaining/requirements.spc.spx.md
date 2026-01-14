# SPX-015: Requirements Document - Scope-Aware Fluent Chaining

**Status:** Requirements  
**Created:** 2025-01-14  
**Author:** Copilot  
**Related:** SPX-011 (Element Scope and Page Merge), SPX-014 (Scope Control Refactor)

---

## 1. Introduction

### Problem Statement

Currently, all control action methods return `TPage` (the page type), regardless of where the control is defined:

```csharp
// Current: Button inside a container still returns TPage
public class LoginPage : MauiPageObjectBase<LoginPage>
{
    public MauiButtonControl<LoginPage> SubmitButton => Button("submit");
    public LoginFormContainer Form => Container<LoginFormContainer>("form");
}

public class LoginFormContainer : MauiContainerBase<LoginPage>
{
    public MauiButtonControl<LoginPage> CancelButton => Button("cancel");
}

// Usage - both return LoginPage
loginPage.SubmitButton.Click()   // Returns LoginPage ✓ (correct)
loginPage.Form.CancelButton.Click()  // Returns LoginPage ✗ (should return LoginFormContainer)
```

**Issue:** When `CancelButton.Click()` is called, it returns `LoginPage`, not `LoginFormContainer`. This breaks the fluent chaining locality - you can't continue operating within the container.

### Desired Behavior

Controls should return **their containing scope** (page or container), not always the root page:

```csharp
// Desired: Controls return their immediate scope
loginPage.SubmitButton.Click()           // Returns LoginPage (button is on page)
loginPage.Form.CancelButton.Click()      // Returns LoginFormContainer (button is in container)
    .UsernameEntry.Enter("test")         // Continue within container
    .PasswordEntry.Enter("password");    // Still within container
```

### Key Principles

1. **Scope-Localized Returns**: Controls return their containing scope, not the root page
2. **Parent Navigation**: Containers expose their parent scope (page or container) via `.Parent`
3. **Unified Scope Model**: Pages and containers are both scopes - no special "page" tracking needed
4. **Type Safety**: Generic constraints ensure compile-time type checking

---

## 2. Alignment with Product Vision

This change supports Brinell's core design principles:

1. **Fluent API**: True fluent chaining within any scope boundary
2. **Composition**: Containers as reusable scopes that can be composed
3. **Clean Abstractions**: Controls don't need to know if they're in a page or container
4. **Test Readability**: Operations group naturally within their scope boundaries

---

## 3. Requirements

### 3.1 User Stories

#### US-1: Scope-Localized Fluent Returns
**As a** test author  
**I want** control actions to return their containing scope  
**So that** I can continue chaining operations within the same scope

**Acceptance Criteria:**
- AC-1.1: Control action methods (Click, Enter, etc.) return `TScope` type
- AC-1.2: If `TScope` is a page, actions return the page
- AC-1.3: If `TScope` is a container, actions return the container
- AC-1.4: Fluent chaining works correctly within containers

**Example:**
```csharp
// Before: Both return LoginPage
form.UsernameEntry.Enter("test")    // LoginPage
form.CancelButton.Click()           // LoginPage

// After: Both return LoginFormContainer
form.UsernameEntry.Enter("test")    // LoginFormContainer
form.CancelButton.Click()           // LoginFormContainer
```

#### US-2: Parent Scope Access
**As a** test author  
**I want** to access the parent scope from any container  
**So that** I can navigate up the scope hierarchy

**Acceptance Criteria:**
- AC-2.1: Containers expose `TParent Parent { get; }` property
- AC-2.2: `Parent` returns the immediate parent scope (page or container)
- AC-2.3: Top-level containers have a page as their parent
- AC-2.4: Nested containers have another container as their parent
- AC-2.5: Type safety is preserved through the hierarchy

**Example:**
```csharp
// Navigate up through parents
loginPage.Form                          // LoginFormContainer (parent is LoginPage)
    .SocialLogin                        // SocialLoginSection (parent is LoginFormContainer)
        .GoogleButton.Click()           // Returns SocialLoginSection
        .Parent                         // Returns LoginFormContainer
        .Parent                         // Returns LoginPage
        .AssertLoaded(true);
```

#### US-3: Container Self-Reference
**As a** test author  
**I want** containers to have a self-referencing generic parameter  
**So that** child controls return the correct container type

**Acceptance Criteria:**
- AC-3.1: Container classes can use self-referencing generic pattern
- AC-3.2: Child controls use container type as their scope type
- AC-3.3: Type safety is preserved through the hierarchy

**Example:**
```csharp
// Self-referencing container - parent is a page
public class LoginFormContainer : MauiContainerBase<LoginPage, LoginFormContainer>
{
    // Child controls return LoginFormContainer
    public MauiButtonControl<LoginFormContainer> CancelButton => Button("cancel");
}

// Nested container - parent is another container
public class SocialLoginSection : MauiContainerBase<LoginFormContainer, SocialLoginSection>
{
    // Child controls return SocialLoginSection
    public MauiButtonControl<SocialLoginSection> GoogleButton => Button("google");
}
```

---

### 3.2 Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Controls SHALL accept `TScope` as the fluent return type parameter | Must |
| FR-2 | Control action methods SHALL return `TScope` (the containing scope) | Must |
| FR-3 | Containers SHALL expose `TParent Parent { get; }` for parent scope access | Must |
| FR-4 | Container child controls SHALL use container type as `TScope` | Must |
| FR-5 | Pages SHALL use themselves as `TScope` (page IS the scope) | Must |
| FR-6 | Pages SHALL NOT have a `Parent` property (they are the root) | Must |
| FR-7 | `Parent` property SHALL return the immediate parent scope | Must |

---

### 3.3 Technical Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| TR-1 | `MauiControlBase<TScope>` SHALL accept `TScope : IMauiScope` constraint | Must |
| TR-2 | `MauiContainerBase<TPage, TSelf>` SHALL implement self-referencing pattern | Must |
| TR-3 | `IMauiPagedScope<TPage>` SHALL be renamed/refactored to `IMauiScope` | Should |
| TR-4 | Factory methods SHALL create controls with correct `TScope` type | Must |
| TR-5 | Existing tests SHALL continue to work after refactor | Must |

---

## 4. Interface Design

### 4.1 Core Interface: `IMauiScope<TScope>`


```csharp
/// <summary>
/// Base scope interface for element finding with fluent return support.
/// </summary>
public interface IMauiScope<TScope> : IMauiElementScope
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Gets this scope for fluent chaining.
    /// For pages: returns this.
    /// For containers: returns this.
    /// </summary>
    TScope Self { get; }
}
```

### 4.2 Page with Scope: `IMauiPage<TSelf>`

```csharp
/// <summary>
/// Page interface that is also a scope returning itself.
/// Pages are root scopes - they have no parent.
/// </summary>
public interface IMauiPage<TSelf> : IPageObject, IMauiScope<TSelf>
    where TSelf : IMauiPage<TSelf>
{
    // Self inherited from IMauiScope<TSelf>
    // Page-specific members from IPageObject
    // NO Parent property - pages are the root
}
```

### 4.3 Container with Parent: `IMauiContainer<TParent, TSelf>`

```csharp
/// <summary>
/// Container that is a scope returning itself, with access to parent scope.
/// The parent can be a page or another container - both are scopes.
/// </summary>
/// <typeparam name="TParent">The parent scope type (page or container).</typeparam>
/// <typeparam name="TSelf">The container type itself (self-referencing).</typeparam>
public interface IMauiContainer<TParent, TSelf> : IMauiScope<TSelf>
    where TParent : IMauiScope<TParent>
    where TSelf : IMauiContainer<TParent, TSelf>
{
    /// <summary>
    /// Gets the parent scope (page or container).
    /// Navigate up the scope hierarchy by calling Parent.
    /// </summary>
    TParent Parent { get; }
}
```

### 4.4 Scope Hierarchy Summary

```
Page (IMauiPage<TSelf>)                    ← Root scope, no Parent
  ├── Controls → return Page
  └── Container A (IMauiContainer<Page, ContainerA>)
        ├── Controls → return ContainerA
        ├── .Parent → returns Page
        └── Container B (IMauiContainer<ContainerA, ContainerB>)
              ├── Controls → return ContainerB
              ├── .Parent → returns ContainerA
              └── .Parent.Parent → returns Page
```

**Key insight:** Only ONE container interface needed. The parent type parameter determines whether the container is top-level (parent is page) or nested (parent is another container).

---

## 5. Usage Examples

### 5.1 Page Definition

```csharp
public class LoginPage : MauiPageObjectBase<LoginPage>
{
    // Controls return LoginPage (page IS the scope)
    public MauiButtonControl<LoginPage> SubmitButton => Button("submit");
    
    // Container - its parent is this page
    public LoginFormContainer Form => Container<LoginFormContainer>("form");
}
```

### 5.2 Container Definition (Parent is Page)

```csharp
// Container whose parent is a page
public class LoginFormContainer : MauiContainerBase<LoginPage, LoginFormContainer>
{
    // Controls return LoginFormContainer (container IS the scope)
    public MauiButtonControl<LoginFormContainer> CancelButton => Button("cancel");
    public MauiEntryControl<LoginFormContainer> UsernameEntry => Entry("username");
    public MauiEntryControl<LoginFormContainer> PasswordEntry => Entry("password");
    
    // Nested container
    public SocialLoginSection SocialLogin => Container<SocialLoginSection>("social");
}
```

### 5.3 Nested Container Definition


```csharp
// Nested container: parent is another container
public class SocialLoginSection : MauiContainerBase<LoginFormContainer, SocialLoginSection>
{
    // Controls return SocialLoginSection
    public MauiButtonControl<SocialLoginSection> GoogleButton => Button("google");
    public MauiButtonControl<SocialLoginSection> FacebookButton => Button("facebook");
}
```

### 5.4 Test Usage - Fluent Chaining

```csharp
[Test]
public void Login_WithValidCredentials_NavigatesToHome()
{
    var loginPage = new LoginPage(context);
    
    // Chain within container scope
    loginPage.Form
        .UsernameEntry.Enter("user@test.com")   // Returns LoginFormContainer
        .PasswordEntry.Enter("password123")     // Returns LoginFormContainer
        .Parent                                 // Escape to LoginPage
        .SubmitButton.Click()                   // Returns LoginPage
        .AssertLoaded(false);                   // Should navigate away
}
```

### 5.5 Test Usage - Mixed Scope Operations

```csharp
[Test]
public void Form_CancelButton_ClearsFields()
{
    var loginPage = new LoginPage(context);
    
    loginPage.Form
        .UsernameEntry.Enter("test")
        .PasswordEntry.Enter("test")
        .CancelButton.Click()                   // Returns LoginFormContainer
        .UsernameEntry.AssertText("")           // Continue in container
        .PasswordEntry.AssertText("");          // Continue in container
}
```

### 5.6 Test Usage - Nested Containers with Parent Navigation

```csharp
[Test]
public void NestedContainer_NavigateUpHierarchy()
{
    var loginPage = new LoginPage(context);
    
    loginPage.Form                              // LoginFormContainer
        .SocialLogin                            // SocialLoginSection (nested)
            .GoogleButton.Click()               // Returns SocialLoginSection
            .Parent                             // Navigate up to LoginFormContainer
        .UsernameEntry.Enter("fallback@test")   // Continue in parent container
        .Parent                                 // Navigate up to LoginPage
        .AssertLoaded(true);
}

[Test]
public void NestedContainer_ChainParentCalls()
{
    var loginPage = new LoginPage(context);
    
    // Chain .Parent calls to go up multiple levels
    loginPage.Form
        .SocialLogin
            .FacebookButton.Click()
            .Parent                             // LoginFormContainer
            .Parent                             // LoginPage
        .SubmitButton.Click();
}
```

---

## 6. Forward Calls Pattern

### 6.1 Container Forwarding Parent Operations

Containers can optionally forward certain operations to the parent:

```csharp
public class LoginFormContainer : MauiContainerBase<LoginPage, LoginFormContainer>
{
    // Forward navigation assertion to parent page
    public LoginFormContainer AssertOnLoginPage(string? message = null)
    {
        Parent.AssertLoaded(true, message);
        return this;
    }
    
    // Container-specific assertion
    public LoginFormContainer AssertFormVisible(string? message = null)
    {
        AssertVisible(true, message);
        return this;
    }
}
```

### 6.2 Usage

```csharp
loginPage.Form
    .AssertFormVisible()                // Container method
    .UsernameEntry.Enter("test")        // Container scope
    .AssertOnLoginPage()                // Forwards to page, returns container
    .PasswordEntry.Enter("password");   // Continue in container
```

---

## 7. Non-Functional Requirements

### 7.1 Code Architecture
- **Single Responsibility**: Scopes handle element finding, controls handle actions
- **Composition**: Containers compose with pages/parents through `Page` and `Parent` properties
- **Type Safety**: Generic constraints ensure correct types at compile time
- **Hierarchy Navigation**: Clear path from any depth: `Self` → `Parent` → ... → `Page`

### 7.2 Backward Compatibility
- Per tech.md: **No backward compatibility** required during pre-release (0.x.x)
- Breaking changes acceptable for cleaner design

### 7.3 Performance
- No additional overhead for scope resolution (direct property access)
- No runtime type checking required

---

## 8. Out of Scope

- Blazor platform implementation (focus on MAUI first)
- Automatic page forwarding (must be explicit)


---

## 9. Dependencies

| Spec | Relationship |
|------|--------------|
| SPX-011 | Provides `IElementScope` and `IPageObject` merge foundation |
| SPX-014 | Provides `MauiScopeFactory` for control creation |

---

## 10. Design Decisions

| Question | Decision | Rationale |
|----------|----------|-----------|
| Naming: `Self` vs `Scope` | **`Self`** | Clearer that it returns the same object for chaining |
| Container type parameters | **`<TParent, TSelf>`** | Simpler - only need parent, not separate page reference |
| Why no `.Page` property? | **Use `.Parent` chain** | Unified model - page is just the root parent scope |
| Parent access syntax | **`.Parent` property** | Type-safe navigation up the scope hierarchy |
| Deep nesting (3+ levels) | **Supported** | Chain `.Parent.Parent.Parent...` to reach any level |
| Page has no Parent | **Correct** | Pages are root scopes - the hierarchy stops there |
