# 231_004 Container Pattern

## pattern Container

- **title**: Container Pattern
- **type**: Structural
- **purpose**: Scope element searches to specific UI regions

---

## Description

The Container pattern scopes element searches to specific regions of the UI. Instead of searching the entire application for elements, containers search only within their bounds. This improves reliability and performance, especially in complex UIs with repeating elements.

> **Note:** Code snippets in this document are illustrative examples showing architectural patterns. Actual implementation may vary. See source code for current implementation details.

---

## 1. Intent

**Problem:** Global element searches cause:
- Ambiguous matches (multiple elements with same identifier)
- Slow searches through entire DOM/UI tree
- Brittle tests when UI structure changes
- Difficulty testing repeating UI patterns (lists, cards)

**Solution:** Create containers that:
- Scope searches to a specific UI region
- Find elements only within container bounds
- Support nested containers for complex hierarchies
- Enable testing of repeated UI patterns

---

## 2. Structure

### 2.1 Participants

| Participant | Role |
|-------------|------|
| IContainerControl | Interface for container behavior |
| ContainerBase | Abstract base for containers |
| Page | Top-level container (application root) |
| FormContainer | Container for a form region |
| CardContainer | Container for a card component |
| ListItemContainer | Container for list items |

### 2.2 Container Hierarchy

```
Page (root scope)
├── HeaderContainer
│   ├── LogoImage
│   └── MenuButton
├── ContentContainer
│   ├── FormContainer
│   │   ├── UsernameEntry
│   │   ├── PasswordEntry
│   │   └── SubmitButton
│   └── CardContainer (repeated)
│       ├── TitleLabel
│       └── ActionButton
└── FooterContainer
    └── CopyrightLabel
```

---

## 3. Implementation

### 3.1 Container Interface

```csharp
/// <summary>
/// Non-generic container interface for basic scoping operations.
/// </summary>
public interface IContainerControl : IControlObject
{
    /// <summary>
    /// Parent page or container.
    /// </summary>
    IPageObject? Page { get; }
    
    /// <summary>
    /// Create a scoped locator for finding elements within this container.
    /// </summary>
    Locator ScopedLocator(string automationId);
    Locator ScopedLocator(Locator locator);
    
    /// <summary>
    /// Find child element within this container.
    /// </summary>
    object? FindChild(Locator locator);
    
    /// <summary>
    /// Find all child elements within this container.
    /// </summary>
    IReadOnlyList<object> FindChildren(Locator locator);
}

/// <summary>
/// Generic container interface for single-content containers (ContentControl, Frame, etc.).
/// </summary>
/// <typeparam name="TContent">The type of content control contained.</typeparam>
public interface IContainerControlObject<TContent> : IContainerControl 
    where TContent : IControlObject
{
    /// <summary>
    /// Get the content of this container.
    /// </summary>
    TContent? GetContent(int? timeoutMs = null);
    
    /// <summary>
    /// Find a control of type T within this container.
    /// </summary>
    T FindControl<T>(Locator locator) where T : IControlObject;
}

/// <summary>
/// Generic list container interface for repeating elements (ListView, ItemsControl, etc.).
/// </summary>
/// <typeparam name="TItem">The type of item controls in the list.</typeparam>
public interface IListContainerControlObject<TItem> : IContainerControl 
    where TItem : IControlObject
{
    /// <summary>
    /// Get item at specified index.
    /// </summary>
    TItem GetItemAt(int index, int? timeoutMs = null);
    
    /// <summary>
    /// Get all items in the container.
    /// </summary>
    IReadOnlyList<TItem> GetAllItems(int? timeoutMs = null);
    
    /// <summary>
    /// Get the count of items.
    /// </summary>
    int GetItemCount(int? timeoutMs = null);
    
    /// <summary>
    /// Find a control of type T within this container.
    /// </summary>
    T FindControl<T>(Locator locator) where T : IControlObject;
}
```

### 3.2 Locator Scoping

```csharp
public class Locator
{
    public LocatorStrategy Strategy { get; }
    public string Value { get; }
    public Locator? Scope { get; }
    
    public Locator(LocatorStrategy strategy, string value, Locator? scope = null)
    {
        Strategy = strategy;
        Value = value;
        Scope = scope;
    }
    
    /// <summary>
    /// Create a new locator scoped to a container.
    /// </summary>
    public Locator ScopedTo(Locator containerLocator)
    {
        return new Locator(Strategy, Value, containerLocator);
    }
    
    /// <summary>
    /// Create a new locator scoped to a container.
    /// </summary>
    public static Locator ScopedTo(Locator elementLocator, Locator containerLocator)
    {
        return new Locator(elementLocator.Strategy, elementLocator.Value, containerLocator);
    }
}
```

### 3.3 Container Base Class

```csharp
public abstract class ContainerBase : ControlBase, IContainerControl
{
    protected ContainerBase(ITestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }
    
    protected ContainerBase(ITestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }
    
    /// <summary>
    /// Create a locator scoped to this container.
    /// </summary>
    public Locator ScopedLocator(string automationId)
    {
        var childLocator = new Locator(_page?.DefaultLocatorStrategy ?? LocatorStrategy.AutomationId, automationId);
        return childLocator.ScopedTo(_locator);
    }
    
    public Locator ScopedLocator(Locator locator)
    {
        return locator.ScopedTo(_locator);
    }
    
    /// <summary>
    /// Find element within this container.
    /// </summary>
    public object? FindChild(Locator locator)
    {
        var containerElement = FindElement();
        if (containerElement == null)
            throw new ElementNotFoundException($"Container '{Locator}' not found");
            
        return _context.FindElement(locator, containerElement);
    }
    
    public IReadOnlyList<object> FindChildren(Locator locator)
    {
        var containerElement = FindElement();
        if (containerElement == null)
            return Array.Empty<object>();
            
        return _context.FindElements(locator, containerElement);
    }
    
    /// <summary>
    /// Find a control of specified type within this container.
    /// </summary>
    public T FindControl<T>(Locator locator) where T : IControlObject
    {
        // Implementation creates control with scoped locator
        var scopedLocator = ScopedLocator(locator);
        return _context.CreateControl<T>(scopedLocator, _page);
    }
}
```

### 3.4 Scoped Control Creation

```csharp
public class FormContainer : ContainerBase
{
    public FormContainer(ITestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
    
    // Controls scoped to this container
    public EntryControl UsernameEntry => new(_context, ScopedLocator("UsernameEntry"), _page);
    public EntryControl PasswordEntry => new(_context, ScopedLocator("PasswordEntry"), _page);
    public ButtonControl SubmitButton => new(_context, ScopedLocator("SubmitButton"), _page);
    
    // Generic control finding
    public T GetControl<T>(string automationId) where T : IControlObject
        => FindControl<T>(new Locator(LocatorStrategy.AutomationId, automationId));
}
```

---

## 4. Usage

### 4.1 Basic Container Usage

```csharp
public class LoginPage : PageBase
{
    // Container for the login form region
    public FormContainer LoginForm => new(_context, "LoginForm", this);
    
    // Shortcut accessors through the container
    public EntryControl Username => LoginForm.UsernameEntry;
    public EntryControl Password => LoginForm.PasswordEntry;
    public ButtonControl Submit => LoginForm.SubmitButton;
}

// In test
var loginPage = new LoginPage(_context);
loginPage.Username.Enter("testuser");
loginPage.Submit.Click();
```

### 4.2 Nested Containers

```csharp
public class DashboardPage : PageBase
{
    public ContainerControl Sidebar => new(_context, "Sidebar", this);
    public ContainerControl MainContent => new(_context, "MainContent", this);
    
    // Nested container within MainContent
    public FormContainer SettingsForm => new(_context, 
        MainContent.ScopedLocator("SettingsForm"), this);
}
```

### 4.3 Repeating Elements (Lists)

```csharp
public class ProductListPage : PageBase
{
    public ContainerControl ProductList => new(_context, "ProductList", this);
    
    /// <summary>
    /// Get a product card by index.
    /// </summary>
    public ProductCard GetProductAt(int index)
    {
        var items = ProductList.FindChildren(new Locator(LocatorStrategy.ClassName, "product-card"));
        if (index >= items.Count)
            throw new ElementNotFoundException($"Product at index {index} not found");
            
        return new ProductCard(_context, items[index], this);
    }
    
    /// <summary>
    /// Get a product card by name.
    /// </summary>
    public ProductCard GetProductByName(string name)
    {
        var cards = GetAllProducts();
        return cards.FirstOrDefault(c => c.Title.GetText() == name)
            ?? throw new ElementNotFoundException($"Product '{name}' not found");
    }
    
    /// <summary>
    /// Get all product cards.
    /// </summary>
    public IReadOnlyList<ProductCard> GetAllProducts()
    {
        var items = ProductList.FindChildren(new Locator(LocatorStrategy.ClassName, "product-card"));
        return items.Select((e, i) => new ProductCard(_context, e, this)).ToList();
    }
}

public class ProductCard : ContainerBase
{
    public ProductCard(ITestContext context, object element, IPageObject? page)
        : base(context, element, page) { }
    
    public LabelControl Title => new(_context, ScopedLocator("ProductTitle"), _page);
    public LabelControl Price => new(_context, ScopedLocator("ProductPrice"), _page);
    public ButtonControl AddToCart => new(_context, ScopedLocator("AddToCartButton"), _page);
}
```

### 4.4 Test with Repeating Elements

```csharp
[Fact]
public void AddToCart_FirstProduct_UpdatesCartCount()
{
    var productPage = new ProductListPage(_context);
    
    // Get first product card
    var firstProduct = productPage.GetProductAt(0);
    firstProduct.AddToCart.Click();
    
    // Or by name
    var specificProduct = productPage.GetProductByName("Widget Pro");
    specificProduct.AddToCart.Click();
    
    // Verify cart
    productPage.CartCount.AssertTextEquals("2");
}
```

---

## 5. When to Use Containers

### 5.1 Use Container When

| Scenario | Example |
|----------|---------|
| Region has multiple child controls | Form with inputs and button |
| Need to disambiguate elements | Multiple "Submit" buttons on page |
| Repeating UI patterns | List items, cards, rows |
| Independent scroll region | Sidebar that scrolls separately |
| Logical grouping | Header, footer, sidebar |

### 5.2 Use Control When

| Scenario | Example |
|----------|---------|
| Element is a leaf/action target | Button, label, input |
| No child elements needed | Simple text display |
| Single interaction point | Standalone button |
| No scoping needed | Unique element on page |

---

## 6. Scoping Patterns

### 6.1 Page → Container → Control

```
Page (searches from app root)
└── Container (searches within container element)
    └── Control (element within container)
```

### 6.2 XPath Scoping

For XPath locators, scoping prepends the container path:

```csharp
// Container: //div[@id='login-form']
// Child: //input[@name='username']
// Scoped: //div[@id='login-form']//input[@name='username']
```

### 6.3 Multiple Matches

Without scoping:
```csharp
// ❌ Ambiguous: which "SubmitButton"?
var button = new ButtonControl(_context, "SubmitButton", page);
```

With scoping:
```csharp
// ✅ Specific: SubmitButton inside LoginForm
var form = new FormContainer(_context, "LoginForm", page);
var button = new ButtonControl(_context, form.ScopedLocator("SubmitButton"), page);
```

## 7. Performance Considerations

### 7.1 Search Scope Reduction

```
Global search:    Entire app tree (1000+ elements)
Container search: Container subtree (10-50 elements)
```

### 7.2 Caching Container Element

For performance with many child accesses:

```csharp
public class OptimizedContainer : ContainerBase
{
    private object? _cachedElement;
    
    protected object CachedElement => _cachedElement ??= FindElement() 
        ?? throw new ElementNotFoundException(AutomationId);
    
    public void InvalidateCache() => _cachedElement = null;
}
```

---

## 8. Anti-Patterns

### 8.1 Don't Scope Everything

```csharp
// ❌ BAD: Over-scoping unique elements
var header = new Container("Header");
var logo = new Container("LogoRegion");
var logoImage = logo.ScopedLocator("Logo");  // Too much nesting!

// ✅ GOOD: Direct access for unique elements
public ImageControl Logo => new(_context, "Logo", this);
```

### 8.2 Don't Create Containers for Single Controls

```csharp
// ❌ BAD: Container with one element
public ContainerControl ButtonWrapper => new(_context, "ButtonWrapper");
public ButtonControl Submit => new(_context, ButtonWrapper.ScopedLocator("Submit"));

// ✅ GOOD: Direct control if only one child
public ButtonControl Submit => new(_context, "Submit", this);
```

### 8.3 Don't Forget Page Reference

```csharp
// ❌ BAD: Lost page context
public EntryControl Username => new(_context, ScopedLocator("Username")); // Missing page!

// ✅ GOOD: Pass page reference
public EntryControl Username => new(_context, ScopedLocator("Username"), _page);
```

---

## 9. Validation Rules

The Container pattern is valid when:

- [ ] Containers implement IContainerControl interface
- [ ] Generic containers implement IContainerControlObject<T> or IListContainerControlObject<T>
- [ ] ScopedLocator creates properly scoped locators
- [ ] FindChild searches within container bounds
- [ ] FindControl<T> returns properly typed controls
- [ ] Nested containers scope to parent container
- [ ] Repeating elements use container for each item
- [ ] Page reference is passed to scoped controls
- [ ] Containers are used for grouping, not single elements

---

## Related Documents

- [231_001 Control Object Pattern](231_001_ControlObjectPattern.spx.md)
- [231_002 Page Object Pattern](231_002_PageObjectPattern.spx.md)
- [211_004 PageContext](../211_Modules/211_004_PageContext.spx.md)
- [FR-102 Container Object](../../100_requirements/120_functional/120_102_ContainerObject.spx.md)
