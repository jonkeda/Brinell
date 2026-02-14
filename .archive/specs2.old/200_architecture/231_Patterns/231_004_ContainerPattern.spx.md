# 231_004 Container Pattern

## pattern Container

- **title**: Container Pattern
- **type**: Structural
- **purpose**: Scope element searches to specific UI regions

---

## Description

The Container pattern scopes element searches to specific regions of the UI. Instead of searching the entire application for elements, containers search only within their bounds. This improves reliability and performance, especially in complex UIs with repeating elements.

**Key Design:** A container **IS** an `IElementScope<TElement>`. Child controls receive the container as their scope, and searches are automatically scoped to the container's element bounds.

> **Note:** Code snippets in this document are illustrative examples showing architectural patterns. Actual implementation may vary. See source code for current implementation details.

---

## 1. Intent

**Problem:** Global element searches cause:
- Ambiguous matches (multiple elements with same identifier)
- Slow searches through entire DOM/UI tree
- Brittle tests when UI structure changes
- Difficulty testing repeating UI patterns (lists, cards)

**Solution:** Create containers that:
- Implement `IElementScope<TElement>` to provide scoped element finding
- Act as scope for child controls (container IS the scope)
- Support nested containers for complex hierarchies
- Enable testing of repeated UI patterns

**Key Change:** The container IS a scope, not a locator modifier. Children receive the container as their `IElementScope`, eliminating `ScopedLocator()` chains.

---

## 2. Structure

### 2.1 Participants

| Participant | Role |
|-------------|------|
| `IContainerControl<TElement>` | Interface extending both `IControlObject` and `IElementScope<TElement>` |
| `ContainerBase<TElement, TScope>` | Generic base class for containers |
| `MauiContainerBase` | MAUI typed alias |
| `BlazorContainerBase` | Blazor typed alias |
| Page | Top-level scope (application root) |
| FormContainer | Container for a form region |
| CardContainer | Container for a card component |

### 2.2 Container Hierarchy

```
Page (root scope - implements IElementScope<TElement>)
├── HeaderContainer (scope for header children)
│   ├── LogoImage
│   └── MenuButton
├── ContentContainer (scope for content children)
│   ├── FormContainer (nested scope)
│   │   ├── UsernameEntry
│   │   ├── PasswordEntry
│   │   └── SubmitButton
│   └── CardContainer (repeated, each is a scope)
│       ├── TitleLabel
│       └── ActionButton
└── FooterContainer
    └── CopyrightLabel
```

### 2.3 Container Interface Hierarchy

```
                  IControlObject
                        │
                        ├─────────────────────────┐
                        │                         │
            IContainerControl<TElement>    IElementScope<TElement>
                        │                         │
                        └──────────┬──────────────┘
                                   │
                        Container IS both control AND scope
                                   │
               ┌───────────────────┼───────────────────┐
               │                   │                   │
        IMauiContainerControl  IBlazorContainerControl  IWpfContainerControl
```

---

## 3. Implementation

### 3.1 Container Interface

```csharp
/// <summary>
/// Generic container interface - is both a control AND a scope.
/// Container provides scoped element finding for its children.
/// </summary>
public interface IContainerControl<TElement> : IControlObject, IElementScope<TElement>
{
    // Inherits from IControlObject:
    // - Locator (how to find this container)
    // - IElementScope Scope (parent scope - page or parent container)
    
    // Inherits from IElementScope<TElement>:
    // - TElement? ScopeRoot (the container element itself - children search within)
    // - TElement? TryFindElement(Locator locator)  (scoped search)
    // - TElement FindElement(Locator locator)
    // - IReadOnlyList<TElement> FindElements(Locator locator)
}

/// <summary>
/// Generic list container interface for repeating elements (ListView, ItemsControl, etc.).
/// </summary>
/// <typeparam name="TElement">The driver element type.</typeparam>
/// <typeparam name="TItem">The type of item controls in the list.</typeparam>
public interface IListContainerControl<TElement, TItem> : IContainerControl<TElement> 
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
}
```

### 3.2 Generic Container Base Class

```csharp
/// <summary>
/// Generic container base - is both control and scope for children.
/// TElement: The driver element type (AppiumElement, IWebElement, etc.)
/// TScope: The parent scope type (for implementation convenience)
/// </summary>
public abstract class ContainerBase<TElement, TScope> : ControlBase<TElement, TScope>, IContainerControl<TElement>
    where TScope : IElementScope<TElement>
{
    protected ContainerBase(TScope parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }
    
    // IElementScope<TElement> - Container IS a scope for its children
    
    /// <summary>
    /// The container element serves as the scope root for children.
    /// </summary>
    public TElement? ScopeRoot => TryFindElement();
    object? IElementScope.ScopeRoot => ScopeRoot;
    
    ITestContext<TElement> IElementScope<TElement>.Context => _scope.Context;
    ITestContext IElementScope.Context => _scope.Context;
    
    /// <summary>
    /// Find element within this container.
    /// </summary>
    public TElement? TryFindElement(Locator locator)
    {
        var root = ScopeRoot;
        if (root == null) return default;
        return _scope.Context.TryFindElement(locator, root);
    }
    
    /// <summary>
    /// Find element within this container. Throws if not found.
    /// </summary>
    public TElement FindElement(Locator locator)
    {
        var root = base.FindElement();  // Throws if container not found
        return _scope.Context.FindElement(locator, root);
    }
    
    /// <summary>
    /// Find all matching elements within this container.
    /// </summary>
    public IReadOnlyList<TElement> FindElements(Locator locator)
    {
        var root = ScopeRoot;
        if (root == null) return Array.Empty<TElement>();
        return _scope.Context.FindElements(locator, root);
    }
}
```

### 3.3 Platform Container Base Classes

```csharp
namespace Brinell.Maui
{
    /// <summary>
    /// MAUI container base - typed alias for common use.
    /// </summary>
    public abstract class MauiContainerBase : ContainerBase<AppiumElement, IMauiElementScope>, IMauiContainerControl
    {
        protected MauiContainerBase(IMauiElementScope parentScope, Locator locator)
            : base(parentScope, locator) { }
        
        protected MauiContainerBase(IMauiElementScope parentScope, string automationId)
            : base(parentScope, Locator.ByAutomationId(automationId)) { }
        
        // IMauiElementScope
        IMauiTestContext IMauiElementScope.Context => _scope.Context;
        
        // Convenience for subclasses
        protected IMauiTestContext Context => _scope.Context;
    }
}

namespace Brinell.Blazor
{
    /// <summary>
    /// Blazor container base - typed alias for common use.
    /// </summary>
    public abstract class BlazorContainerBase : ContainerBase<IWebElement, IBlazorElementScope>, IBlazorContainerControl
    {
        protected BlazorContainerBase(IBlazorElementScope parentScope, Locator locator)
            : base(parentScope, locator) { }
        
        protected BlazorContainerBase(IBlazorElementScope parentScope, string testId)
            : base(parentScope, Locator.ByDataTestId(testId)) { }
        
        // IBlazorElementScope
        IBlazorTestContext IBlazorElementScope.Context => _scope.Context;
        
        protected IBlazorTestContext Context => _scope.Context;
    }
}
```

### 3.4 Scoped Control Creation (Container IS Scope)

```csharp
/// <summary>
/// Form container - children use 'this' as their scope.
/// </summary>
public class LoginFormContainer : MauiContainerBase
{
    public LoginFormContainer(IMauiElementScope parentScope, Locator locator)
        : base(parentScope, locator) { }
    
    public LoginFormContainer(IMauiElementScope parentScope, string automationId)
        : base(parentScope, automationId) { }
    
    // Controls use 'this' (container) as scope - NOT ScopedLocator()!
    public MauiEntryControl UsernameEntry => new(this, "UsernameEntry");
    public MauiEntryControl PasswordEntry => new(this, "PasswordEntry");
    public MauiButtonControl SubmitButton => new(this, "SubmitButton");
    //                                           ^^^^ 'this' IS IMauiElementScope
}

/// <summary>
/// Blazor form container example.
/// </summary>
public class LoginFormContainerBlazor : BlazorContainerBase
{
    public LoginFormContainerBlazor(IBlazorElementScope parentScope, string testId)
        : base(parentScope, testId) { }
    
    // Children use 'this' as scope
    public BlazorEntryControl UsernameEntry => new(this, "username");
    public BlazorEntryControl PasswordEntry => new(this, "password");
    public BlazorButtonControl SubmitButton => new(this, "submit");
}
```

---

## 4. Usage

### 4.1 Basic Container Usage

```csharp
public class LoginPage : MauiPageObjectBase
{
    // Container for the login form region
    public LoginFormContainer LoginForm => new(this, "LoginForm");
    //                                         ^^^^ page is container's parent scope
    
    // Or access controls through the container
    public MauiEntryControl Username => LoginForm.UsernameEntry;
    public MauiEntryControl Password => LoginForm.PasswordEntry;
    public MauiButtonControl Submit => LoginForm.SubmitButton;
    
    public LoginPage(IMauiTestContext context) : base(context, "LoginPage") { }
}

// In test
var loginPage = new LoginPage(_context);
loginPage.Username.Enter("testuser");
loginPage.Submit.Click();

// Or directly via container
loginPage.LoginForm.UsernameEntry.Enter("testuser");
```

### 4.2 Nested Containers

```csharp
public class DashboardPage : MauiPageObjectBase
{
    // First-level containers use 'this' (page) as scope
    public MauiContainerControl Sidebar => new(this, "Sidebar");
    public MauiContainerControl MainContent => new(this, "MainContent");
    
    // Nested container uses MainContent as its parent scope
    public SettingsFormContainer SettingsForm => new(MainContent, "SettingsForm");
    //                                               ^^^^^^^^^^^ parent is container, not page
    
    public DashboardPage(IMauiTestContext context) : base(context, "DashboardPage") { }
}

public class SettingsFormContainer : MauiContainerBase
{
    public SettingsFormContainer(IMauiElementScope parentScope, string automationId)
        : base(parentScope, automationId) { }
    
    // Children use 'this' (SettingsForm) as scope
    public MauiToggleControl DarkMode => new(this, "DarkModeToggle");
    public MauiPickerControl Language => new(this, "LanguagePicker");
    public MauiButtonControl Save => new(this, "SaveButton");
}
```

### 4.3 Repeating Elements (Lists)

```csharp
public class ProductListPage : MauiPageObjectBase
{
    public ProductListPage(IMauiTestContext context) : base(context, "ProductListPage") { }
    
    /// <summary>
    /// Get a product card by index.
    /// </summary>
    public ProductCard GetProductAt(int index)
    {
        // Create card with 'this' (page) as parent scope
        var cardLocator = Locator.ByXPath($"(.//*[@AutomationId='ProductCard'])[{index + 1}]");
        return new ProductCard(this, cardLocator);
    }
    
    /// <summary>
    /// Get a product card by name.
    /// </summary>
    public ProductCard? GetProductByName(string name)
    {
        var allCards = GetAllProducts();
        return allCards.FirstOrDefault(c => c.Title.GetText() == name);
    }
    
    /// <summary>
    /// Get all product cards.
    /// </summary>
    public IReadOnlyList<ProductCard> GetAllProducts()
    {
        var elements = FindElements(Locator.ByAutomationId("ProductCard"));
        return elements.Select((_, i) => GetProductAt(i)).ToList();
    }
    
    public int ProductCount => GetAllProducts().Count;
}

/// <summary>
/// Product card container - each card is a scope for its children.
/// </summary>
public class ProductCard : MauiContainerBase
{
    public ProductCard(IMauiElementScope parentScope, Locator locator)
        : base(parentScope, locator) { }
    
    // Children use 'this' (card) as scope
    public MauiLabelControl Title => new(this, "ProductTitle");
    public MauiLabelControl Price => new(this, "ProductPrice");
    public MauiButtonControl AddToCart => new(this, "AddToCartButton");
}
```

### 4.4 Test with Repeating Elements

```csharp
[Fact]
public void AddToCart_FirstProduct_UpdatesCartCount()
{
    var productPage = new ProductListPage(_context);
    
    // Get first product card - card is scope for its children
    var firstProduct = productPage.GetProductAt(0);
    firstProduct.AddToCart.Click();  // Button found within card
    
    // Get specific product by name
    var widgetPro = productPage.GetProductByName("Widget Pro");
    widgetPro?.AddToCart.Click();
    
    // Verify cart
    productPage.CartBadge.AssertTextEquals("2");
}

[Fact]
public void ProductCard_DisplaysCorrectInfo()
{
    var productPage = new ProductListPage(_context);
    
    var card = productPage.GetProductAt(0);
    
    // Each control searches within the card container
    card.Title.AssertExists();
    card.Price.AssertTextMatches(@"\$\d+\.\d{2}");  // e.g., "$19.99"
    card.AddToCart.AssertEnabled();
}
```

---

## 5. When to Use Containers

### 5.1 Use Container When

| Scenario | Example | Container Provides |
|----------|---------|-------------------|
| Region has multiple child controls | Form with inputs and button | Scope for children |
| Need to disambiguate elements | Multiple "Submit" buttons on page | Search within bounds |
| Repeating UI patterns | List items, cards, rows | Each item is a scope |
| Independent scroll region | Sidebar that scrolls separately | Scoped finding |
| Logical grouping | Header, footer, sidebar | Organization |

### 5.2 Use Control When

| Scenario | Example |
|----------|---------|
| Element is a leaf/action target | Button, label, input |
| No child elements needed | Simple text display |
| Single interaction point | Standalone button |
| Unique element on page | Only one instance exists |

---

## 6. Scoping Patterns

### 6.1 Scope Chain: Page → Container → Control

```
Page (root scope - ScopeRoot = null, searches from driver root)
│
└── Container (receives page as scope, ScopeRoot = container element)
    │
    └── Control (receives container as scope, searches within container)
```

### 6.2 Element Finding Flow

```csharp
// Control.TryFindElement() flow:
control._scope.TryFindElement(control._locator)
    ↓
// Container.TryFindElement(locator):
var root = ScopeRoot;  // Container's own element
return _scope.Context.TryFindElement(locator, root);  // Scoped search
```

### 6.3 Multiple Matches Resolution

```csharp
// ❌ OLD: Ambiguous - which "SubmitButton"?
var button = new MauiButtonControl(page, "SubmitButton");

// ❌ OLD: Explicit ScopedLocator() chain
var button = new MauiButtonControl(_context, form.ScopedLocator("SubmitButton"), page);

// ✅ NEW: Container IS scope - children automatically scoped
var form = new LoginFormContainer(page, "LoginForm");
var button = new MauiButtonControl(form, "SubmitButton");  // Automatically searches within form
//                                  ^^^^ container is the scope
```

---

## 7. Performance Considerations

### 7.1 Search Scope Reduction

```
Global search:    Entire app tree (1000+ elements)
Container search: Container subtree (10-50 elements)
```

### 7.2 Container Element Caching

The `ScopeRoot` property returns the container's element, which can be cached:

```csharp
public abstract class ContainerBase<TElement, TScope>
{
    private TElement? _cachedScopeRoot;
    
    /// <summary>
    /// Cached scope root for performance with many child accesses.
    /// </summary>
    public TElement? ScopeRoot => _cachedScopeRoot ??= TryFindSelf();
    
    /// <summary>
    /// Invalidate cache when container element may have changed.
    /// </summary>
    public void InvalidateCache() => _cachedScopeRoot = default;
    
    private TElement? TryFindSelf() => base.TryFindElement();
}
```

---

## 8. Anti-Patterns

### 8.1 Don't Scope Everything

```csharp
// ❌ BAD: Over-scoping unique elements
var header = new MauiContainerControl(page, "Header");
var logoRegion = new MauiContainerControl(header, "LogoRegion");
var logo = new MauiImageControl(logoRegion, "Logo");  // Too much nesting!

// ✅ GOOD: Direct access for unique elements
public MauiImageControl Logo => new(this, "Logo");  // If Logo is unique on page
```

### 8.2 Don't Create Containers for Single Controls

```csharp
// ❌ BAD: Container with one element
public MauiContainerControl ButtonWrapper => new(this, "ButtonWrapper");
public MauiButtonControl Submit => new(ButtonWrapper, "Submit");

// ✅ GOOD: Direct control if only one child
public MauiButtonControl Submit => new(this, "Submit");
```

### 8.3 Don't Use ScopedLocator() - Container IS Scope

```csharp
// ❌ OBSOLETE: Old ScopedLocator pattern
public MauiEntryControl Username => new(_context, ScopedLocator("Username"), _page);

// ✅ NEW: Container IS scope - pass 'this'
public MauiEntryControl Username => new(this, "Username");
```

### 8.4 Don't Pass Context Directly

```csharp
// ❌ BAD: Passing context + page - OLD pattern
public MauiEntryControl Username => new(_context, "Username", _page);

// ✅ GOOD: Pass scope (page or container) - NEW pattern
public MauiEntryControl Username => new(this, "Username");
```

---

## 9. Validation Rules

The Container pattern is valid when:

- [ ] Containers implement `IContainerControl<TElement>` (both control and scope)
- [ ] Container extends `ContainerBase<TElement, TScope>` or platform alias
- [ ] Children receive container (`this`) as their scope, not context
- [ ] No `ScopedLocator()` calls - container IS the scope
- [ ] `ScopeRoot` returns the container's own element
- [ ] `TryFindElement(locator)` searches within container bounds
- [ ] Nested containers receive parent container as scope
- [ ] List containers return properly scoped item containers
- [ ] Each item in a list is its own scope for its children

---

## Related Documents

- [231_001 Control Object Pattern](231_001_ControlObjectPattern.spx.md)
- [231_002 Page Object Pattern](231_002_PageObjectPattern.spx.md)
- [231_007 Scoped Element Finder](231_007_ScopedElementFinder.spx.md)
- [FR-102 Container Object](../../100_requirements/120_functional/120_102_ContainerObject.spx.md)
