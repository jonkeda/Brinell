# 231b_004 Container Pattern V2

## pattern Container

- **title**: Container Pattern V2 - High-Level Overview
- **type**: Structural
- **purpose**: Scope element access to UI regions with type safety

---

## Description

The Container pattern creates hierarchical scopes for UI element access. Containers are controls that hold other controls, enabling localized searching and type-safe child access. This pattern appears in three forms to handle different container scenarios.

---

## 1. The Three Container Types

Brinell provides three container interfaces, each for a different scenario:

| Interface | Purpose | Example |
|-----------|---------|---------|
| `IContainerControlObject<T>` | Single typed child | Card with content panel |
| `IListContainerControlObject<T>` | Multiple typed children | Product grid with cards |
| `IContainerControl` | Dynamic scoped finding | Modal with unknown content |

### Decision Flow

```
Do you know the child type at compile time?
├── Yes → Is it a single child or multiple children?
│   ├── Single child → IContainerControlObject<T>
│   └── Multiple children → IListContainerControlObject<T>
└── No → IContainerControl (dynamic finding)
```

---

## 2. IContainerControlObject&lt;T&gt; — Single Typed Child

For containers with one known content type. The `Child` property provides direct typed access.

**When to use:**
- ContentControl, Frame, Border, Panel wrappers
- Cards with a specific content layout
- Expandable sections with known content

**Pattern:**
```csharp
public class SettingsPanel : ContainerControlBase<SettingsContent>
{
    public override SettingsContent Child { get; }
    
    public SettingsPanel(ITestContext context, Locator locator, IPageObject? page)
        : base(context, locator, page)
    {
        Child = new SettingsContent(context, Locator.ScopedTo(locator), page);
    }
}

// Usage
var content = page.SettingsPanel.Child;
content.SaveButton.Click();
```

**Key characteristics:**
- Child defined at construction (property-based, like page objects)
- Compile-time type safety
- Single content area

---

## 3. IListContainerControlObject&lt;T&gt; — Multiple Typed Children

For containers with multiple homogeneous items. Provides `Children` collection, indexer, and count.

**When to use:**
- ListView, CollectionView, ItemsControl
- Grids of cards or tiles
- Repeating form sections
- Table rows

**Pattern:**
```csharp
public class ProductGrid : ListContainerControlBase<ProductCard>
{
    public ProductGrid(ITestContext context, Locator locator, IPageObject? page)
        : base(context, locator, Locator.ByClassName("ProductCard"), page) { }
}

// Usage
var products = page.ProductGrid.Children;     // IReadOnlyList<ProductCard>
var first = page.ProductGrid[0];              // ProductCard
page.ProductGrid.AssertCount(5);              // Verify count
var widget = page.ProductGrid.FirstOrDefault(
    p => p.Name.GetText() == "Widget");       // Find by predicate
```

**Key characteristics:**
- `Children` returns typed collection snapshot
- Indexer provides direct item access
- `Count` returns current child count
- `FirstOrDefault` finds by predicate
- LINQ support on `Children`

---

## 4. IContainerControl — Dynamic Scoped Finding

For containers where content is unknown at compile time or varies dynamically.

**When to use:**
- Modal dialogs with varying content
- iframes with external content
- Dynamically loaded regions
- Third-party component wrappers

**Pattern:**
```csharp
// Find controls dynamically within scope
var modal = new ModalDialog(context, Locator.ByAutomationId("ConfirmModal"), page);
var okButton = modal.FindControl<ButtonControl>(Locator.ByAutomationId("OkBtn"));
var inputs = modal.FindControls<EntryControl>(Locator.ByClassName("input"));
var hasSubmit = modal.ControlExists(Locator.ByAutomationId("Submit"));
```

**Key characteristics:**
- Runtime type specification
- Scoped element searching
- `FindControl<T>` for single element
- `FindControls<T>` for multiple elements
- `ControlExists` for checking presence

---

## 5. Scoping Concept

All container types scope element searches to their bounds:

```
Page (global scope)
└── ProductGrid (scoped to grid element)
    └── ProductCard[0] (scoped to first card)
        └── AddToCart button (scoped to that card only)
```

**Without scoping (problem):**
```csharp
// ❌ Which "AddToCart" button? There are 10 on the page!
var button = new ButtonControl(context, "AddToCart", page);
```

**With scoping (solution):**
```csharp
// ✅ The "AddToCart" button inside the first product card
var button = page.ProductGrid[0].AddToCart;
```

---

## 6. Container Inheritance

All three interfaces extend `IControlObject`:

```
IControlObject
├── IContainerControlObject<T>      (single typed child)
├── IListContainerControlObject<T>  (multiple typed children)
└── IContainerControl               (dynamic finding)
```

This means containers ARE controls:
- `container.IsExists()` — checks if container element exists
- `container.IsVisible()` — checks container visibility  
- `container.AssertExists()` — asserts container presence

---

## 7. Combining Patterns

Containers can be combined for complex hierarchies:

```csharp
// Page has a typed list of cards
public class ProductListPage : PageObjectBase
{
    public ProductGrid Products { get; }  // IListContainerControlObject<ProductCard>
}

// Each card is a typed container with specific content
public class ProductCard : ContainerControlBase
{
    public LabelControl Name { get; }
    public LabelControl Price { get; }
    public ButtonControl AddToCart { get; }
}

// Test navigates the hierarchy
var firstProduct = page.Products[0];        // ProductCard
firstProduct.AddToCart.Click();             // Scoped button
```

---

## 8. Comparison Summary

| Aspect | Single Child | Multiple Children | Dynamic |
|--------|-------------|-------------------|---------|
| Interface | `IContainerControlObject<T>` | `IListContainerControlObject<T>` | `IContainerControl` |
| Type safety | Compile-time | Compile-time | Runtime |
| Child access | `Child` property | `Children`, `this[i]` | `FindControl<T>()` |
| Child count | 1 | 0..n | 0..n |
| Use case | Frame, Panel | List, Grid | Modal, iframe |

---

## 9. Anti-Patterns to Avoid

**Don't over-nest containers:**
```csharp
// ❌ Too much nesting for simple structure
var button = page.Header.LogoRegion.LogoWrapper.Logo;

// ✅ Direct access for unique elements
var button = page.Logo;
```

**Don't use containers for single elements:**
```csharp
// ❌ Container wrapping one element
public ContainerControl ButtonWrapper { get; }
public ButtonControl Submit => ButtonWrapper.FindControl<ButtonControl>(...);

// ✅ Direct control access
public ButtonControl Submit { get; }
```

**Don't use dynamic finding when type is known:**
```csharp
// ❌ Runtime finding when compile-time is possible
var products = grid.FindControls<ProductCard>(locator);

// ✅ Use typed list container
public IListContainerControlObject<ProductCard> Products { get; }
```

---

## 10. Quick Reference

**Choose `IContainerControlObject<T>` when:**
- Container has exactly one child content area
- Child type is known at compile time
- Examples: Card, Panel, Frame, Expander

**Choose `IListContainerControlObject<T>` when:**
- Container has multiple children of same type
- Need index access, count, or iteration
- Examples: ListView, Grid, Table, Repeater

**Choose `IContainerControl` when:**
- Content varies or is unknown at compile time
- Need flexible element finding within scope
- Examples: Modal dialog, iframe, dynamic region

---

## Related Documents

- [250_003 IContainerControlObject](../../250_specifications/250_000_Foundation/250_003_IContainerControlObject.spx.md)
- [250_003a IContainerControl](../../250_specifications/250_000_Foundation/250_003a_IContainerControl.spx.md)
- [250_003b IListContainerControlObject](../../250_specifications/250_000_Foundation/250_003b_IListContainerControlObject.spx.md)
- [231_004 Container Pattern (Original)](231_004_ContainerPattern.spx.md)
