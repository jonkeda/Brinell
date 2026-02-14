# 250.003 IContainerControlObject Specification

**Block Type:** SPC (Specification)  
**ID:** 250.003  
**Title:** IContainerControlObject Generic Interface Specification  
**Status:** Draft  
**Version:** 2.0  
**Level:** 0 - Foundation

---

## 1. Overview

`IContainerControl<TElement>` is a generic interface for container controls that act as element scopes for their child controls. Containers implement `IElementScope<TElement>`, meaning child controls use the container as their scope for element finding.

### Interface Hierarchy

```
IControlObject
    └── IContainerControl : IControlObject, IElementScope
            └── IContainerControl<TElement> : IContainerControl, IElementScope<TElement>
                    ├── IMauiContainerControl : IContainerControl<AppiumElement>, IMauiElementScope
                    └── IBlazorContainerControl : IContainerControl<IWebElement>, IBlazorElementScope
```

**Key Design:**
- Container IS a scope — child controls receive container as their `IElementScope`
- Container finds elements within its bounds (scoped to container root element)
- Same pattern as Page: both are scopes for their controls

### Interface Identity

- **Package:** `Brinell.Core`
- **Namespace:** `Brinell.Core.Interfaces`
- **Dependencies:** `IControlObject`, `IElementScope<TElement>`
- **Implementors:** `ContainerBase<TElement, TScope>`, platform-specific container bases

---

## 2. Behavior

### 2.1 Container as Element Scope

Containers implement `IElementScope<TElement>` to provide scoped element finding for their child controls:

```csharp
/// <summary>
/// Non-generic container interface.
/// </summary>
public interface IContainerControl : IControlObject, IElementScope
{
    /// <summary>
    /// Root element of this container for scoped searches.
    /// </summary>
    object ContainerRoot { get; }
}

/// <summary>
/// Generic container interface with typed element finding.
/// TElement is the platform's native element type.
/// </summary>
public interface IContainerControl<TElement> : IContainerControl, IElementScope<TElement>
{
    /// <summary>
    /// Typed root element for scoped searches.
    /// </summary>
    new TElement ContainerRoot { get; }
    
    // Inherits from IElementScope<TElement>:
    // TElement? TryFindElement(Locator locator);
    // TElement FindElement(Locator locator);
    // IReadOnlyList<TElement> FindElements(Locator locator);
}
```

**Key Design:**
- Container IS an `IElementScope` — child controls receive container as their scope
- Container finds elements within its root element (scoped search)
- `ContainerRoot` is the element under which all child searches occur
- Unlike Page (which searches from driver root), Container searches from its own root

### 2.2 Container vs Page Scoping

| Aspect | IPageObject<TElement> | IContainerControl<TElement> |
|--------|----------------------|----------------------------|
| Also is | IElementScope<TElement> | IElementScope<TElement> |
| Search root | Driver root (whole page) | Container's root element |
| Use case | Top-level page views | Reusable UI regions |
| Example | LoginPage, DashboardPage | ProductCard, SettingsPanel |

```csharp
// Page delegates to context (driver root)
public class LoginPage : MauiPageObjectBase
{
    // Controls search from driver root (whole screen)
    public MauiEntryControl Username => new(this, "UsernameEntry");
}

// Container searches within its root element
public class ProductCard : MauiContainerBase
{
    // Controls search within this card only
    public MauiLabelControl ProductName => new(this, "ProductName");
    public MauiButtonControl AddToCart => new(this, "AddToCart");
}
```

### 2.3 Property-Based Child Definition

Like pages, containers define their child controls as properties:

```csharp
// Container with typed children - children are properties
public class ProductCard : MauiContainerBase
{
    public MauiLabelControl Name { get; }
    public MauiLabelControl Price { get; }
    public MauiButtonControl AddToCart { get; }
    
    public ProductCard(IMauiElementScope scope, Locator locator)
        : base(scope, locator)
    {
        // 'this' is the scope for child controls
        Name = new MauiLabelControl(this, "ProductName");
        Price = new MauiLabelControl(this, "ProductPrice");
        AddToCart = new MauiButtonControl(this, "AddToCart");
    }
}
```

**Behavior:**
- Children are defined at construction time
- Children use `this` (container) as their scope
- Child element finding is scoped to container root
- Type-safe at compile time

---

## 3. Boundary

### 3.1 Container Not Found

| Scenario | Behavior |
|----------|----------|
| Access child when container doesn't exist | ElementNotFoundException when finding container root |
| `TryFindElement()` when container not found | Returns null (container root search fails) |
| `FindElement()` when container not found | Throws ElementNotFoundException |

### 3.2 Child Element Not Found

| Scenario | Behavior |
|----------|----------|
| `TryFindElement()` when child not in container | Returns null |
| `FindElement()` when child not in container | Throws ElementNotFoundException |
| Child `IsExists()` when element not found | Returns false |

---

## 4. Acceptance Criteria

### ACC-001: Container Scoped Search

```gherkin
Given a page with multiple ProductCard containers
And each card has elements with same AutomationIds
When card1.Name.GetText() is called
Then it returns the name from within card1 only (not card2)
```

### ACC-002: Nested Container Scoping

```gherkin
Given a ProductCard containing a ReviewSection container
And ReviewSection contains ReviewText and StarRating
When card.ReviewSection.StarRating.GetValue() is called
Then it finds StarRating within ReviewSection within ProductCard
```

### ACC-003: Container Inherits IControlObject

```gherkin
Given a ProductCard that extends ContainerBase
When IsExists() is called on the ProductCard
Then it checks the container root element exists
And the container can be used as any other control
```

---

## 5. Assumptions

- **ASM-001:** Container root element is found before child searches
- **ASM-002:** Child controls use container as scope (not context/page)
- **ASM-003:** Container pattern mirrors page object pattern for scoping
- **ASM-004:** Container root remains stable during child operations

---

## 6. Exclusions

- **EXC-001:** Dynamic item lists — use ItemsControl pattern with indexers
- **EXC-002:** Untyped element access — always use typed interfaces
- **EXC-003:** Cross-container element finding — each container is isolated

---

## 7. Complete Interface Definition

```csharp
namespace Brinell.Core.Interfaces
{
    /// <summary>
    /// Non-generic container interface.
    /// </summary>
    public interface IContainerControl : IControlObject, IElementScope
    {
        /// <summary>
        /// Root element for scoped searches within this container.
        /// </summary>
        object ContainerRoot { get; }
    }
    
    /// <summary>
    /// Generic container interface with typed element finding.
    /// TElement is the platform's native element type.
    /// </summary>
    public interface IContainerControl<TElement> : IContainerControl, IElementScope<TElement>
    {
        /// <summary>
        /// Typed root element for scoped searches.
        /// </summary>
        new TElement ContainerRoot { get; }
        
        // Inherits from IElementScope<TElement>:
        // TElement? TryFindElement(Locator locator);
        // TElement FindElement(Locator locator);
        // IReadOnlyList<TElement> FindElements(Locator locator);
    }
}
```

### 7.1 Platform Container Interfaces

```csharp
/// <summary>
/// MAUI container interface - typed to AppiumElement.
/// </summary>
public interface IMauiContainerControl : IContainerControl<AppiumElement>, IMauiElementScope
{
    // IMauiElementScope.Context provides access to IMauiTestContext
    new AppiumElement ContainerRoot { get; }
}

/// <summary>
/// Blazor container interface - typed to IWebElement.
/// </summary>
public interface IBlazorContainerControl : IContainerControl<IWebElement>, IBlazorElementScope
{
    // IBlazorElementScope.Context provides access to IBlazorTestContext
    new IWebElement ContainerRoot { get; }
}
```

---

## 8. ContainerBase Implementation Pattern

The generic container base class implements `IContainerControl<TElement>`:

```csharp
/// <summary>
/// Generic container base with typed element finding.
/// TElement: Platform's native element type
/// TScope: Parent scope type (IMauiElementScope, IBlazorElementScope)
/// </summary>
public abstract class ContainerBase<TElement, TScope> : ControlBase<TElement, TScope>, IContainerControl<TElement>
    where TScope : IElementScope<TElement>
{
    private TElement? _containerRoot;
    
    protected ContainerBase(TScope scope, Locator locator) : base(scope, locator) { }
    
    /// <summary>
    /// Cached container root element.
    /// </summary>
    public TElement ContainerRoot => _containerRoot ??= FindElement();
    object IContainerControl.ContainerRoot => ContainerRoot!;
    
    // IElementScope<TElement> - Container searches within its root
    public TElement? TryFindElement(Locator locator)
    {
        var root = ContainerRoot;
        return root is null ? default : FindWithinRoot(root, locator);
    }
    
    public TElement FindElement(Locator locator)
    {
        var element = TryFindElement(locator);
        return element ?? throw new ElementNotFoundException(
            $"Element not found within container: {locator}");
    }
    
    public IReadOnlyList<TElement> FindElements(Locator locator)
    {
        var root = ContainerRoot;
        return root is null ? Array.Empty<TElement>() : FindAllWithinRoot(root, locator);
    }
    
    // Platform-specific implementations override these
    protected abstract TElement? FindWithinRoot(TElement root, Locator locator);
    protected abstract IReadOnlyList<TElement> FindAllWithinRoot(TElement root, Locator locator);
}
```

### 8.1 Platform Type Aliases

```csharp
// MAUI container base
public abstract class MauiContainerBase : ContainerBase<AppiumElement, IMauiElementScope>, IMauiContainerControl
{
    protected MauiContainerBase(IMauiElementScope scope, Locator locator) : base(scope, locator) { }
    protected MauiContainerBase(IMauiElementScope scope, string automationId) 
        : base(scope, Locator.ByAutomationId(automationId)) { }
    
    // IMauiElementScope
    IMauiTestContext IMauiElementScope.Context => ((IMauiElementScope)_scope).Context;
    
    protected override AppiumElement? FindWithinRoot(AppiumElement root, Locator locator)
        => root.FindElement(locator.ToAppiumBy());
    
    protected override IReadOnlyList<AppiumElement> FindAllWithinRoot(AppiumElement root, Locator locator)
        => root.FindElements(locator.ToAppiumBy());
}

// Blazor container base
public abstract class BlazorContainerBase : ContainerBase<IWebElement, IBlazorElementScope>, IBlazorContainerControl
{
    protected BlazorContainerBase(IBlazorElementScope scope, Locator locator) : base(scope, locator) { }
    protected BlazorContainerBase(IBlazorElementScope scope, string testId) 
        : base(scope, Locator.ByDataTestId(testId)) { }
    
    // IBlazorElementScope
    IBlazorTestContext IBlazorElementScope.Context => ((IBlazorElementScope)_scope).Context;
    
    protected override IWebElement? FindWithinRoot(IWebElement root, Locator locator)
        => root.FindElement(locator.ToSeleniumBy());
    
    protected override IReadOnlyList<IWebElement> FindAllWithinRoot(IWebElement root, Locator locator)
        => root.FindElements(locator.ToSeleniumBy()).ToList();
}
```

---

## 9. Usage Example

```csharp
// Container with child controls
public class ProductCard : MauiContainerBase
{
    public MauiLabelControl ProductName { get; }
    public MauiLabelControl Price { get; }
    public MauiButtonControl AddToCart { get; }
    
    public ProductCard(IMauiElementScope scope, Locator locator) : base(scope, locator)
    {
        // 'this' is the scope - children search within this card
        ProductName = new MauiLabelControl(this, "ProductName");
        Price = new MauiLabelControl(this, "ProductPrice");
        AddToCart = new MauiButtonControl(this, "AddToCart");
    }
}

// Page with container
public class ProductListPage : MauiPageObjectBase
{
    // Container on page - page is the scope
    public ProductCard FeaturedProduct => new(this, "FeaturedProductCard");
    
    public ProductListPage(IMauiTestContext context) : base(context, "ProductList") { }
}

// Test using container
[Test]
public void CanAccessContainerChildren()
{
    var page = new ProductListPage(context);
    page.WaitLoaded(true);
    
    // Access children through container
    var card = page.FeaturedProduct;
    card.AssertVisible(true);
    
    // Children are scoped to this specific card
    var name = card.ProductName.GetText();
    card.AddToCart.Click();
}
```

---

## 10. Validation Checklist

- [ ] `IContainerControl<TElement>` extends `IElementScope<TElement>`
- [ ] Container searches within its root element (not driver root)
- [ ] Child controls use `this` (container) as scope
- [ ] Platform container interfaces narrow element type
- [ ] `ContainerBase<TElement, TScope>` generic base class exists
- [ ] `MauiContainerBase` and `BlazorContainerBase` type aliases defined
- [ ] Container is also an `IControlObject` (has IsExists, IsVisible, etc.)
- [ ] Constructor pattern: `(scope, locator)` not `(context, locator, page)`

---

## Related Documents

- [IControlObject Specification](250_001_IControlObject.spx.md)
- [IPageObject Specification](250_002_IPageObject.spx.md)
- [IElementScope Specification](250_004_TestContext.spx.md#23-ielementscope)
- [Container Pattern](../../200_architecture/231_Patterns/231_004_ContainerPattern.spx.md)
