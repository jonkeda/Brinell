# 231_004b Container Pattern V2

## pattern Container

- **title**: Container Pattern V2 - High-Level Overview
- **type**: Structural
- **purpose**: Scope element access to UI regions with type safety

---

## Description

The Container pattern creates hierarchical scopes for UI element access. Containers are controls that hold other controls, enabling localized searching and type-safe child access. This pattern appears in three forms to handle different container scenarios.

---

## 1. The Three Container Types

Brinell provides three container interfaces, each for a different scenario. Each has a non-generic and generic variant:

| Interface                           | Purpose                     | Example                       |
| ----------------------------------- | --------------------------- | ----------------------------- |
| `IContainerControlObject`           | Named child controls        | Settings panel with controls  |
| `IListContainerControlObject`       | Multiple children by index  | Product grid (dynamic types)  |
| `IListContainerControlObject<T>`    | Typed multiple children     | Product grid with typed cards |
| `IContentContainerControlObject`    | Single content region       | Card with dynamic content     |
| `IContentContainerControlObject<T>` | Single typed content        | Card with known content type  |

### Decision Flow

```
What kind of container is it?
├── Panel/Region with known child controls → IContainerControlObject
│   (Inherits IPageObject - direct properties for each control)
├── List/Grid of similar items → IListContainerControlObject / IListContainerControlObject<T>
│   (Non-generic for flexibility, generic for type safety)
└── Content wrapper with single child → IContentContainerControlObject / IContentContainerControlObject<T>
    (Non-generic uses GetControl<T>(), generic has typed Content property)
```

---

## 2. IContainerControlObject — Named Child Controls

For containers (panels, regions, sections) that group related controls. Child controls are defined as properties directly on the container. **Inherits from `IPageObject`** because it acts as a scoped page-like object with named control properties.

**When to use:**

- Settings panels with multiple input controls
- Form sections grouping related fields
- Sidebars, toolbars, or navigation regions
- Any scoped region with known child controls

**Pattern:**

```csharp
// IContainerControlObject inherits IPageObject
public class SettingsPanel : ContainerControlBase, IContainerControlObject
{
    public LabelControl Title { get; }
    public EntryControl Username { get; }
    public EntryControl Email { get; }
    public ToggleControl Notifications { get; }
    public ButtonControl SaveButton { get; }
  
    public SettingsPanel(ITestContext context, Locator locator, IPageObject? page)
        : base(context, locator, page)
    {
        Title = new LabelControl(context, Locator.ByAutomationId("Title").ScopedTo(locator), this);
        Username = new EntryControl(context, Locator.ByAutomationId("Username").ScopedTo(locator), this);
        Email = new EntryControl(context, Locator.ByAutomationId("Email").ScopedTo(locator), this);
        Notifications = new ToggleControl(context, Locator.ByAutomationId("Notifications").ScopedTo(locator), this);
        SaveButton = new ButtonControl(context, Locator.ByAutomationId("Save").ScopedTo(locator), this);
    }
}

// Usage - direct access to scoped controls
page.SettingsPanel.Username.Enter("john.doe");
page.SettingsPanel.Email.Enter("john@example.com");
page.SettingsPanel.Notifications.Toggle();
page.SettingsPanel.SaveButton.Click();
```

**Key characteristics:**

- **Inherits `IPageObject`** — acts as a scoped page for child controls
- Child controls defined as properties (like page objects)
- Controls pass `this` as their page reference (scoped to container)
- Compile-time type safety
- Direct property access without intermediary

---

## 3. IListContainerControlObject — Multiple Children by Index

For containers with multiple items accessed by index. Available in non-generic and generic variants.

**When to use:**

- ListView, CollectionView, ItemsControl
- Grids of cards or tiles
- Repeating form sections
- Table rows

### 3.1 Non-Generic: IListContainerControlObject

Returns `IControlObject` that can be cast to specific types. Use when child types may vary or for maximum flexibility.

```csharp
public class ProductGrid : ListContainerControlBase
{
    public ProductGrid(ITestContext context, Locator locator, IPageObject? page)
        : base(context, locator, Locator.ByClassName("ProductCard"), page) { }
    
    // Factory for creating typed children
    protected override IControlObject CreateChild(int index) 
        => new ProductCard(Context, GetChildLocator(index), Page);
}

// Usage - generic return types
IReadOnlyList<IControlObject> products = page.ProductGrid.Children;
IControlObject first = page.ProductGrid[0];
int count = page.ProductGrid.Count;

// Cast to specific type when needed
var firstCard = (ProductCard)page.ProductGrid[0];
firstCard.AddToCart.Click();

// Or use GetChild<T> for typed access
var widget = page.ProductGrid.GetChild<ProductCard>(0);
widget.Name.AssertTextEquals("Widget");
```

### 3.2 Generic: IListContainerControlObject&lt;T&gt;

Returns typed `T` directly. Use when all children are the same known type for compile-time safety.

```csharp
public class ProductGrid : ListContainerControlBase<ProductCard>
{
    public ProductGrid(ITestContext context, Locator locator, IPageObject? page)
        : base(context, locator, Locator.ByClassName("ProductCard"), page) { }
    
    protected override ProductCard CreateChild(int index) 
        => new ProductCard(Context, GetChildLocator(index), Page);
}

// Usage - typed return values
IReadOnlyList<ProductCard> products = page.ProductGrid.Children;  // Typed!
ProductCard first = page.ProductGrid[0];                          // Typed!
int count = page.ProductGrid.Count;

// Direct typed access - no casting needed
page.ProductGrid[0].AddToCart.Click();
page.ProductGrid[2].Name.AssertTextEquals("Widget");
```

**Key characteristics:**

| Aspect | Non-Generic | Generic |
|--------|-------------|----------|
| `Children` | `IReadOnlyList<IControlObject>` | `IReadOnlyList<T>` |
| `this[i]` | `IControlObject` | `T` |
| Type safety | Runtime (cast) | Compile-time |
| Flexibility | High | Lower |

---

## 4. IContentContainerControlObject — Single Typed Content Region

For containers that wrap a single content region. Available in non-generic and generic variants.

**When to use:**

- ContentControl, Frame, Border with varying content
- Cards with different content types
- Modal dialogs with typed content
- Expandable sections with specific content

### 4.1 Non-Generic: IContentContainerControlObject

Uses `GetControl<T>()` to retrieve typed content. Use when content type varies or is determined at runtime.

```csharp
public class ContentCard : ContentContainerControlBase
{
    public ContentCard(ITestContext context, Locator locator, IPageObject? page)
        : base(context, locator, page) { }
}

// Usage - get typed content dynamically
var profileContent = page.ContentCard.GetControl<ProfileContent>();
profileContent.Avatar.Click();
profileContent.EditButton.Click();

// Different content type in same container structure
var settingsContent = page.AnotherCard.GetControl<SettingsContent>();
settingsContent.SaveButton.Click();
```

### 4.2 Generic: IContentContainerControlObject&lt;T&gt;

Provides typed `Content` property directly. Use when content type is known at compile time.

```csharp
public class ProfileCard : ContentContainerControlBase<ProfileContent>
{
    public ProfileCard(ITestContext context, Locator locator, IPageObject? page)
        : base(context, locator, page) { }
    
    protected override ProfileContent CreateContent() 
        => new ProfileContent(Context, Locator, Page);
}

// Usage - typed Content property
page.ProfileCard.Content.Avatar.Click();        // Typed!
page.ProfileCard.Content.EditButton.Click();    // Typed!
```

**Key characteristics:**

| Aspect | Non-Generic | Generic |
|--------|-------------|----------|
| Content access | `GetControl<T>()` | `Content` property |
| Type safety | Runtime | Compile-time |
| Flexibility | High (any type) | Single known type |

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

The container interfaces have different inheritance depending on their role:

```
IPageObject
└── IContainerControlObject              (named child controls - IS a scoped page)

IControlObject
├── IListContainerControlObject          (index-based children)
│   └── IListContainerControlObject<T>   (typed index-based children)
├── IContentContainerControlObject       (single content region)
│   └── IContentContainerControlObject<T>(typed single content)
```

**Why IContainerControlObject inherits IPageObject:**

- It acts as a scoped page with named control properties
- Child controls reference it as their "page" for scoping
- Follows the same pattern as PageObject (properties for controls)

**All containers are also controls:**

- `container.IsExists()` — checks if container element exists
- `container.IsVisible()` — checks container visibility
- `container.AssertExists()` — asserts container presence

---

## 7. Combining Patterns

Containers can be combined for complex hierarchies:

```csharp
// Page has a list container of cards
public class ProductListPage : PageObjectBase
{
    public ProductGrid Products { get; }  // IListContainerControlObject
}

// Each card is a container with specific controls
public class ProductCard : ContainerControlBase
{
    public LabelControl Name { get; }
    public LabelControl Price { get; }
    public ButtonControl AddToCart { get; }
}

// Test navigates the hierarchy
var firstProduct = (ProductCard)page.Products[0];
firstProduct.AddToCart.Click();  // Scoped button

// Or using typed access
var widget = page.Products.GetChild<ProductCard>(0);
widget.Name.AssertTextEquals("Widget");
```

---

## 8. Comparison Summary

| Aspect       | Container               | List Container               | Content Container         |
| ------------ | ----------------------- | ---------------------------- | ------------------------- |
| Interface    | `IContainerControlObject` | `IListContainerControlObject[<T>]` | `IContentContainerControlObject[<T>]` |
| Inherits     | `IPageObject`           | `IControlObject`             | `IControlObject`          |
| Type safety  | Compile-time (properties) | Runtime or Compile-time (generic) | Runtime or Compile-time (generic) |
| Child access | Named properties        | `Children`, `this[i]`        | `GetControl<T>()` or `Content` |
| Child count  | Fixed (defined props)   | 0..n                         | 1                         |
| Use case     | Settings panel, Toolbar | List, Grid, Table            | Card, Frame, Modal        |

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
// ❌ Container wrapping one element unnecessarily
public class ButtonWrapper : ContainerControlBase
{
    public ButtonControl Submit { get; }
}

// ✅ Direct control on page
public ButtonControl Submit { get; }
```

**Don't use list container when children are different types:**

```csharp
// ❌ Mixed types in list container
public class MixedGrid : ListContainerControlBase { }  // Has buttons AND labels

// ✅ Use regular container with named properties
public class MixedPanel : ContainerControlBase
{
    public ButtonControl Action { get; }
    public LabelControl Status { get; }
}
```

---

## 10. Quick Reference

**Choose `IContainerControlObject` when:**

- Container has known child controls defined as properties
- Children are accessed by name (like a PageObject)
- Examples: Settings panel, Toolbar, Form section
- Note: Inherits `IPageObject` — child controls reference it as their page

**Choose `IListContainerControlObject` when:**

- Container has multiple children accessed by index
- Need count, iteration, or index access
- Use non-generic for flexibility, generic `<T>` for type safety
- Examples: ListView, Grid, Table, Repeater

**Choose `IContentContainerControlObject` when:**

- Container wraps a single content region
- Use non-generic + `GetControl<T>()` when content type varies
- Use generic `<T>` + `Content` property when type is fixed
- Examples: Card, Frame, Modal, ContentControl

---

## Related Documents

- [250_003 IContainerControlObject](../../250_specifications/250_000_Foundation/250_003_IContainerControlObject.spx.md)
- [250_003b IListContainerControlObject](../../250_specifications/250_000_Foundation/250_003b_IListContainerControlObject.spx.md)
- [250_003c IContentContainerControlObject](../../250_specifications/250_000_Foundation/250_003c_IContentContainerControlObject.spx.md)
- [231_004 Container Pattern (Original)](231_004_ContainerPattern.spx.md)
