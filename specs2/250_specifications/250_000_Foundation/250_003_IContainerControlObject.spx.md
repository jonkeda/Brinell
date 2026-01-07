# 250.003 IContainerControlObject Specification

**Block Type:** SPC (Specification)  
**ID:** 250.003  
**Title:** IContainerControlObject Generic Interface Specification  
**Status:** Draft  
**Version:** 1.1  
**Level:** 0 - Foundation

---

## 1. Overview

`IContainerControlObject<T>` is a generic interface for single-content container controls. These are controls that contain a single typed content area, like `ContentControl`, `Frame`, `Border`, or `Panel`. The container defines its child content as a typed property, similar to how `IPageObject` implementations define controls as properties.

For list-based containers with multiple children, see [IListContainerControlObject](250_003b_IListContainerControlObject.spx.md).

### Interface Identity

- **Package:** `Brinell.Core`
- **Namespace:** `Brinell.Core.Interfaces`
- **Dependencies:** `IControlObject`
- **Implementors:** `ContainerControlBase<T>`, `ContentControl<T>`, `Frame<T>`, `Border<T>`, `Panel<T>`

---

## 2. Behavior

### 2.1 Interface Definition

```csharp
public interface IContainerControlObject<T> : IControlObject where T : IControlObject
{
    /// <summary>
    /// The typed child content control within this container.
    /// Defined as a property in derived classes, similar to controls on a page.
    /// </summary>
    T Child { get; }
}
```

### 2.2 Property-Based Child Definition

Like `IPageObject` where controls are defined as properties, `IContainerControlObject<T>` expects the child to be defined as a property:

```csharp
// Container with typed child - child is a property like controls on a page
public class ProductCard : ContainerControlBase<ProductDetails>
{
    public override ProductDetails Child { get; }
    
    public ProductCard(ITestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
        // Child is scoped to this container
        Child = new ProductDetails(context, Locator.ScopedTo(locator), page);
    }
}

// The child type itself can have its own control properties
public class ProductDetails : ControlBase, IControlObject
{
    public LabelControl Name { get; }
    public LabelControl Price { get; }
    public ButtonControl AddToCart { get; }
    
    public ProductDetails(ITestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
        Name = new LabelControl(context, Locator.ByAutomationId("ProductName").ScopedTo(locator), page);
        Price = new LabelControl(context, Locator.ByAutomationId("ProductPrice").ScopedTo(locator), page);
        AddToCart = new ButtonControl(context, Locator.ByAutomationId("AddToCart").ScopedTo(locator), page);
    }
}
```

**Behavior:**
- Child is defined at construction time (not dynamically found)
- Child inherits the container's scope automatically
- Type-safe at compile time
- Works like page object pattern for controls

### 2.3 Scoped Child Access

```csharp
// Usage in tests - access typed child directly
var page = new ProductListPage(context);
var card = page.ProductCards[0]; // IListContainerControlObject returns ProductCard
var details = card.Child;        // Typed ProductDetails
details.AddToCart.Click();
```

**Behavior:**
- `Child` property returns the strongly-typed content
- Child controls are scoped to the container
- No runtime type casting needed

---

## 3. Boundary

### 3.1 Container Not Found

| Scenario | Behavior |
|----------|----------|
| Access `Child` when container doesn't exist | Child's operations will throw ElementNotFoundException |

### 3.2 Child Element Not Found

| Scenario | Behavior |
|----------|----------|
| Access `Child` when child element doesn't exist | Child exists but its operations throw ElementNotFoundException |
| `Child.IsExists()` when element not found | Returns false |

---

## 4. Acceptance Criteria

### ACC-001: Typed Child Access

```gherkin
Given a ProductCard container with ProductDetails as Child type
When the Child property is accessed
Then it returns a ProductDetails instance
And the ProductDetails is scoped to the ProductCard container
```

### ACC-002: Child Control Properties

```gherkin
Given a ProductCard with Child containing Name, Price, AddToCart
When Child.Name.GetText() is called
Then it returns the name from within that specific card only
```

### ACC-003: Container Inheritance

```gherkin
Given a ProductCard that extends ContainerControlBase<ProductDetails>
When IsExists() is called on the ProductCard
Then it checks the container element (inherited from IControlObject)
```

---

## 5. Assumptions

- **ASM-001:** Child type is known at compile time
- **ASM-002:** Child is scoped to the container automatically
- **ASM-003:** Container pattern mirrors page object pattern for controls

---

## 6. Exclusions

- **EXC-001:** Dynamic child finding — use IContainerControl for that
- **EXC-002:** Multiple children — use IListContainerControlObject
- **EXC-003:** Untyped access — child type must be specified

---

## 7. Implementation Example

```csharp
public abstract class ContainerControlBase<T> : ControlBase, IContainerControlObject<T> 
    where T : IControlObject
{
    protected ContainerControlBase(ITestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    /// <summary>
    /// The typed child content. Must be defined by derived classes.
    /// </summary>
    public abstract T Child { get; }
}

// Concrete implementation
public class SettingsPanel : ContainerControlBase<SettingsContent>
{
    public override SettingsContent Child { get; }
    
    public SettingsPanel(IMauiTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
        Child = new SettingsContent(context, Locator.ByAutomationId("Content").ScopedTo(locator), page);
    }
}
```

---

## 8. Usage Example

```csharp
// Page with container controls
public class SettingsPage : PageObjectBase
{
    public SettingsPanel GeneralSettings { get; }
    public SettingsPanel AdvancedSettings { get; }
    
    public SettingsPage(IMauiTestContext context) : base(context, "Settings")
    {
        GeneralSettings = new SettingsPanel(context, Locator.ByAutomationId("GeneralPanel"), this);
        AdvancedSettings = new SettingsPanel(context, Locator.ByAutomationId("AdvancedPanel"), this);
    }
}

// Test using typed container
[Test]
public void CanAccessTypedContent()
{
    var page = new SettingsPage(context);
    page.WaitLoaded(true);
    
    // Access typed child directly
    var generalContent = page.GeneralSettings.Child;
    generalContent.SaveButton.Click();
    
    // Container inherits IControlObject
    page.AdvancedSettings.AssertVisible(true);
}
```

---

## Related Documents

- [IControlObject Specification](250_001_IControlObject.spx.md)
- [IListContainerControlObject Specification](250_003b_IListContainerControlObject.spx.md)
- [IContainerControl Specification](250_003a_IContainerControl.spx.md)
- [Container Pattern](../../200_architecture/231_Patterns/231_004_ContainerPattern.spx.md)
