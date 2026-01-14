# specification MauiMinimalScope

- **id**: SPC-203
- **version**: 1.0
- **created**: January 13, 2026
- **status**: Draft
- **level**: 0 - Foundation
- **requirement**: FR-101, FR-102

---

## Overview

This specification details the scoping patterns for MAUI controls. Controls can be placed in pages, views (containers), or list items. Each scope type provides element finding within its boundaries.

**Key Principle:** The scope determines WHERE element searches start. Controls don't need to know if they're on a page or in a view—they just use their scope.

---

## 1. Scope Types

### 1.1 Page Scope

Pages search from the driver root (entire application window).

```csharp
// Page as scope - delegates to context (driver root)
public abstract class MauiPageObjectBase : IMauiPageObject
{
    protected readonly IMauiTestContext _context;
    
    // IElementScope<AppiumElement> - search from driver root
    public AppiumElement? TryFindElement(Locator locator) 
        => _context.TryFindElement(locator);
}
```

**Use When:**
- Control is directly on a page/screen
- Control is unique within the entire screen
- No need for scoped searching

**Example:**
```csharp
public class LoginPage : MauiPageObjectBase
{
    // These controls search from driver root
    public MauiEntryControl Username => new(this, "UsernameEntry");
    public MauiEntryControl Password => new(this, "PasswordEntry");
    public MauiButtonControl LoginButton => new(this, "LoginButton");
}
```

### 1.2 View Scope (Container)

Views/containers search from their root element (scoped search).

```csharp
// Container as scope - searches within container root
public abstract class MauiContainerBase : IMauiContainerControl
{
    // IElementScope<AppiumElement> - search within container
    public AppiumElement? TryFindElement(Locator locator)
    {
        var root = ContainerRoot;
        return root.FindElement(locator.ToAppiumBy());
    }
}
```

**Use When:**
- Multiple similar regions on one page (e.g., two address forms)
- Reusable UI components (cards, panels, forms)
- Need to scope searches to avoid ambiguity

**Example:**
```csharp
public class AddressForm : MauiContainerBase
{
    // These controls search within THIS form only
    public MauiEntryControl Street => new(this, "StreetEntry");
    public MauiEntryControl City => new(this, "CityEntry");
    public MauiEntryControl Zip => new(this, "ZipEntry");
    
    public AddressForm(IMauiElementScope scope, string automationId)
        : base(scope, automationId) { }
}

public class CheckoutPage : MauiPageObjectBase
{
    // Two forms with same control IDs - scoping prevents conflicts
    public AddressForm ShippingAddress => new(this, "ShippingForm");
    public AddressForm BillingAddress => new(this, "BillingForm");
    
    // shippingAddress.City finds CityEntry WITHIN ShippingForm
    // billingAddress.City finds CityEntry WITHIN BillingForm
}
```

### 1.3 List Item Scope

List items are containers that represent one item in a collection.

```csharp
// List item as scope - searches within item root
public abstract class MauiListItemBase : MauiContainerBase, IMauiListItemControl
{
    public int Index { get; }
    
    protected MauiListItemBase(IMauiElementScope scope, Locator locator, int index)
        : base(scope, locator)
    {
        Index = index;
    }
}
```

**Use When:**
- Working with CollectionView, ListView, CarouselView
- Need to interact with specific items in a list
- Items have internal controls (name, price, buttons)

**Example:**
```csharp
public class ProductListItem : MauiListItemBase
{
    // These controls search within THIS list item only
    public MauiLabelControl ProductName => new(this, "ProductName");
    public MauiLabelControl Price => new(this, "Price");
    public MauiButtonControl AddToCart => new(this, "AddToCart");
    
    public ProductListItem(IMauiElementScope scope, Locator locator, int index)
        : base(scope, locator, index) { }
}
```

---

## 2. Scope Hierarchy

Scopes can be nested to any depth:

```
IMauiTestContext (driver root)
│
└── Page (MauiPageObjectBase)
    │   search root: driver
    │
    ├── Control (MauiButtonControl)
    │       search root: driver (via page)
    │
    ├── View (MauiContainerBase)
    │   │   search root: view element
    │   │
    │   ├── Control (MauiEntryControl)
    │   │       search root: view element (via view)
    │   │
    │   └── Nested View (MauiContainerBase)
    │       │   search root: nested view element
    │       │
    │       └── Control
    │               search root: nested view element
    │
    └── List (future: MauiListControl)
        │
        └── List Item (MauiListItemBase)
            │   search root: item element
            │
            └── Control
                    search root: item element
```

---

## 3. Page Property Resolution

Every control has a `Page` property that resolves to the containing page:

```csharp
public IPageObject? Page => Scope switch
{
    IPageObject page => page,           // Direct page → return it
    IControlObject control => control.Page, // Container → ask container
    _ => null                           // TestContext → no page
};
```

**Resolution Chain:**
```
Control on Page:
    control.Scope = Page → control.Page = Page

Control in View on Page:
    control.Scope = View → View.Scope = Page → View.Page = Page → control.Page = Page

Control in Nested View:
    control.Scope = InnerView → InnerView.Scope = OuterView → 
    OuterView.Scope = Page → OuterView.Page = Page → 
    InnerView.Page = Page → control.Page = Page

Control in List Item on Page:
    control.Scope = ListItem → ListItem.Scope = Page → 
    ListItem.Page = Page → control.Page = Page
```

---

## 4. Complete Usage Examples

### 4.1 Simple Page with Controls

```csharp
// Test
var loginPage = new LoginPage(context);
loginPage.WaitLoaded(true);
loginPage.Username.Enter("user@example.com");
loginPage.Password.Enter("password123");
loginPage.LoginButton.Click();
```

### 4.2 Page with Views (Forms)

```csharp
public class CheckoutPage : MauiPageObjectBase
{
    public AddressForm ShippingAddress => new(this, "ShippingForm");
    public AddressForm BillingAddress => new(this, "BillingForm");
    public MauiButtonControl PlaceOrder => new(this, "PlaceOrderButton");
    
    public CheckoutPage(IMauiTestContext context) : base(context, "Checkout") { }
}

// Test
var checkout = new CheckoutPage(context);

// Fill shipping - controls scoped to ShippingForm
checkout.ShippingAddress.Street.Enter("123 Main St");
checkout.ShippingAddress.City.Enter("Seattle");
checkout.ShippingAddress.Zip.Enter("98101");

// Fill billing - controls scoped to BillingForm
checkout.BillingAddress.Street.Enter("456 Oak Ave");
checkout.BillingAddress.City.Enter("Portland");
checkout.BillingAddress.Zip.Enter("97201");

checkout.PlaceOrder.Click();
```

### 4.3 Nested Views

```csharp
public class ProductCard : MauiContainerBase
{
    public MauiLabelControl Name => new(this, "ProductName");
    public MauiLabelControl Price => new(this, "ProductPrice");
    public ReviewSection Reviews => new(this, "ReviewsSection");
    public MauiButtonControl AddToCart => new(this, "AddToCart");
    
    public ProductCard(IMauiElementScope scope, string automationId)
        : base(scope, automationId) { }
}

public class ReviewSection : MauiContainerBase
{
    public MauiLabelControl AverageRating => new(this, "AverageRating");
    public MauiLabelControl ReviewCount => new(this, "ReviewCount");
    public MauiButtonControl WriteReview => new(this, "WriteReview");
    
    public ReviewSection(IMauiElementScope scope, string automationId)
        : base(scope, automationId) { }
}

public class ProductDetailPage : MauiPageObjectBase
{
    public ProductCard Product => new(this, "ProductCard");
    
    public ProductDetailPage(IMauiTestContext context) 
        : base(context, "ProductDetail") { }
}

// Test
var page = new ProductDetailPage(context);

// Direct control access
page.Product.Name.AssertText("Widget Pro");
page.Product.Price.AssertTextContains("$99");

// Nested view access
page.Product.Reviews.AverageRating.AssertText("4.5 stars");
page.Product.Reviews.WriteReview.Click();
```

### 4.4 List with Items

```csharp
public class ProductListItem : MauiListItemBase
{
    public MauiLabelControl ProductName => new(this, "ProductName");
    public MauiLabelControl Price => new(this, "Price");
    public MauiButtonControl AddToCart => new(this, "AddToCart");
    
    public ProductListItem(IMauiElementScope scope, Locator locator, int index)
        : base(scope, locator, index) { }
}

public class ProductListPage : MauiPageObjectBase
{
    private MauiListControl<ProductListItem>? _products;
    
    public MauiListControl<ProductListItem> Products => 
        _products ??= new MauiListControl<ProductListItem>(
            this, 
            "ProductList",
            (scope, locator, idx) => new ProductListItem(scope, locator, idx)
        );
    
    public ProductListPage(IMauiTestContext context) 
        : base(context, "ProductList") { }
}

// Test
var page = new ProductListPage(context);

// Access by index
page.Products[0].ProductName.AssertTextContains("Widget");
page.Products[0].AddToCart.Click();

// Iterate
foreach (var item in page.Products.GetItems())
{
    var name = item.ProductName.GetText();
    var price = item.Price.GetText();
    Console.WriteLine($"{name}: {price}");
}

// Find by predicate
var expensiveItem = page.Products.FirstOrDefault(
    item => item.Price.GetText()?.Contains("$100") == true);
expensiveItem?.AddToCart.Click();
```

---

## 5. MauiListControl Implementation

For completeness, here's the list control that manages list items:

```csharp
namespace Brinell.Maui.Controls
{
    /// <summary>
    /// MAUI list control that provides access to list items.
    /// </summary>
    public class MauiListControl<TItem> : MauiControlBase 
        where TItem : IMauiListItemControl
    {
        private readonly Func<IMauiElementScope, Locator, int, TItem> _itemFactory;
        private readonly string _itemLocatorPattern;
        
        public MauiListControl(
            IMauiElementScope scope, 
            string listAutomationId,
            Func<IMauiElementScope, Locator, int, TItem> itemFactory,
            string itemLocatorPattern = "Item_{0}")
            : base(scope, listAutomationId)
        {
            _itemFactory = itemFactory;
            _itemLocatorPattern = itemLocatorPattern;
        }
        
        /// <summary>
        /// Get item by index (0-based).
        /// </summary>
        public TItem this[int index]
        {
            get
            {
                var itemLocator = Locator.ByAutomationId(
                    string.Format(_itemLocatorPattern, index));
                return _itemFactory(_scope, itemLocator, index);
            }
        }
        
        /// <summary>
        /// Get all visible items.
        /// </summary>
        public IReadOnlyList<TItem> GetItems()
        {
            var items = new List<TItem>();
            var index = 0;
            
            while (true)
            {
                var item = this[index];
                if (!item.IsExists())
                    break;
                    
                items.Add(item);
                index++;
            }
            
            return items;
        }
        
        /// <summary>
        /// Get item count.
        /// </summary>
        public int Count => GetItems().Count;
        
        /// <summary>
        /// Find first item matching predicate.
        /// </summary>
        public TItem? FirstOrDefault(Func<TItem, bool> predicate)
        {
            foreach (var item in GetItems())
            {
                if (predicate(item))
                    return item;
            }
            return default;
        }
    }
}
```

---

## 6. View vs. Page Decision Guide

| Scenario | Use Page | Use View/Container |
|----------|----------|-------------------|
| Entire screen/route | ✓ | |
| Unique region on screen | | ✓ |
| Multiple similar regions | | ✓ (one per region) |
| Reusable component | | ✓ |
| Navigation target | ✓ | |
| Card/panel/form | | ✓ |
| List item content | | ✓ (ListItem) |
| Modal/dialog | ✓ or ✓ | depends on app |

---

## 7. Boundary Conditions

### 7.1 View Not Found

```csharp
// When container root doesn't exist
var form = page.ShippingAddress;
form.Street.Enter("123 Main");  // Throws ElementNotFoundException

// Safe check first
if (form.IsExists())
{
    form.Street.Enter("123 Main");
}
```

### 7.2 Element Outside Scope

```csharp
// Element exists on page but NOT in container
public class Form : MauiContainerBase
{
    // This will NOT find GlobalMessage even if it exists on page
    public MauiLabelControl GlobalMessage => new(this, "GlobalMessage");
}

// GlobalMessage exists at page level, not inside form
form.GlobalMessage.IsExists();  // Returns false
```

### 7.3 Scope Invalidation

```csharp
// After UI refresh, container root may be stale
var card = page.ProductCard;
card.AddToCart.Click();  // Works

// Page refreshes/re-renders...
page.Refresh();

// Container root is now stale
card.Name.GetText();  // May throw StaleElementReferenceException

// Solution: Invalidate cache
card.InvalidateCache();
card.Name.GetText();  // Works - re-finds container root

// Or: Create new container reference
var newCard = page.ProductCard;
newCard.Name.GetText();  // Works
```

---

## 8. Acceptance Criteria

### ACC-001: Page Scope Searching

```gherkin
Given a LoginPage with Username entry
When Username.Enter("test") is called
Then the element is found from driver root
And text is entered successfully
```

### ACC-002: View Scope Searching

```gherkin
Given a CheckoutPage with ShippingAddress and BillingAddress forms
And both forms have a "CityEntry" element
When shippingAddress.City.Enter("Seattle") is called
Then only the CityEntry WITHIN ShippingForm is modified
And BillingForm's CityEntry remains unchanged
```

### ACC-003: Nested Scope Searching

```gherkin
Given a ProductCard containing a ReviewSection
And ReviewSection contains AverageRating
When card.Reviews.AverageRating.GetText() is called
Then the search finds AverageRating within Reviews within ProductCard
```

### ACC-004: Page Property Resolution

```gherkin
Given a control within a view within a page
When control.Page is accessed
Then it returns the containing page (not the view)
```

### ACC-005: List Item Scope

```gherkin
Given a ProductList with 3 items
And each item has ProductName and AddToCart
When products[1].ProductName.GetText() is called
Then it returns the name from item index 1 only
```

---

## Related Documents

- [250_201_MauiMinimalInterfaces](250_201_MauiMinimalInterfaces.spx.md) - Interface definitions
- [250_202_MauiMinimalClasses](250_202_MauiMinimalClasses.spx.md) - Class implementations
- [250_003_IContainerControlObject](../250_000_Foundation/250_003_IContainerControlObject.spx.md) - Full container spec
