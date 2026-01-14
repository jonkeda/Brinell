# specification MauiMinimalInterfaces

- **id**: SPC-201
- **version**: 1.0
- **created**: January 13, 2026
- **status**: Draft
- **level**: 0 - Foundation
- **requirement**: FR-100, FR-101, FR-102, FR-103

---

## Overview

This specification defines the minimal set of interfaces needed to support MAUI Button and Entry controls. The design supports controls being placed in pages, views (containers), or list items.

**Core Principle:** Scope is the element finder. Controls receive a scope (page, view, or list item) and use it to find their underlying element.

---

## Interface Summary

| Interface | Purpose | Package |
|-----------|---------|---------|
| `IElementScope<TElement>` | Generic element finding contract | Core |
| `IMauiElementScope` | MAUI-specific scope (typed to AppiumElement) | MAUI |
| `IControlObject` | Base for all controls | Core |
| `IClickableControl` | Click capability | Core |
| `ITextControl` | Read text capability | Core |
| `IEditableTextControl` | Edit text capability | Core |
| `IContainerControl` | Container/view capability | Core |
| `IPageObject` | Page representation | Core |
| `IMauiPageObject` | MAUI page (AppiumElement scope) | MAUI |
| `IMauiContainerControl` | MAUI container/view | MAUI |

---

## 1. Element Scope Interfaces

### 1.1 IElementScope (Core)

The fundamental scope contract. Pages, views, and list items all implement this.

```csharp
namespace Brinell.Core.Interfaces
{
    /// <summary>
    /// Non-generic element scope for polymorphic access.
    /// </summary>
    public interface IElementScope
    {
        /// <summary>
        /// Default locator strategy for controls in this scope.
        /// </summary>
        LocatorStrategy DefaultLocatorStrategy { get; }
    }

    /// <summary>
    /// Generic element scope with typed element finding.
    /// TElement is the platform's native element type.
    /// </summary>
    public interface IElementScope<TElement> : IElementScope
    {
        /// <summary>
        /// Try to find a single element within this scope.
        /// </summary>
        /// <returns>Element if found, null otherwise.</returns>
        TElement? TryFindElement(Locator locator);
        
        /// <summary>
        /// Find a single element within this scope.
        /// </summary>
        /// <exception cref="ElementNotFoundException">If not found.</exception>
        TElement FindElement(Locator locator);
        
        /// <summary>
        /// Find all matching elements within this scope.
        /// </summary>
        IReadOnlyList<TElement> FindElements(Locator locator);
    }
}
```

### 1.2 IMauiElementScope (MAUI)

MAUI-specific scope typed to AppiumElement.

```csharp
namespace Brinell.Maui.Interfaces
{
    /// <summary>
    /// MAUI element scope - typed to AppiumElement.
    /// Implemented by: IMauiTestContext, IMauiPageObject, IMauiContainerControl
    /// </summary>
    public interface IMauiElementScope : IElementScope<AppiumElement>
    {
        /// <summary>
        /// Access to the test context for timeouts, logging, screenshots.
        /// </summary>
        IMauiTestContext Context { get; }
    }
}
```

---

## 2. Control Interfaces (Core)

### 2.1 IControlObject

Base interface for ALL controls.

```csharp
namespace Brinell.Core.Interfaces
{
    /// <summary>
    /// Base interface for all controls. Provides state queries, waits, and assertions.
    /// </summary>
    public interface IControlObject
    {
        // ─── Identity ────────────────────────────────────────────────────
        
        /// <summary>
        /// Locator used to find this control.
        /// </summary>
        Locator Locator { get; }
        
        /// <summary>
        /// The scope (page, view, or list item) containing this control.
        /// </summary>
        IElementScope Scope { get; }
        
        /// <summary>
        /// The page containing this control (may be null for orphaned controls).
        /// </summary>
        IPageObject? Page { get; }
        
        // ─── State (immediate, no waiting) ───────────────────────────────
        
        /// <summary>Check if element exists in UI tree.</summary>
        /// <returns>True if exists, false otherwise.</returns>
        bool IsExists();
        
        /// <summary>Check if element is visible.</summary>
        /// <returns>True if visible, false if hidden, null if not exists.</returns>
        bool? IsVisible();
        
        /// <summary>Check if element is enabled.</summary>
        /// <returns>True if enabled, false if disabled, null if not exists.</returns>
        bool? IsEnabled();
        
        // ─── Waiting (poll until condition or timeout) ───────────────────
        
        /// <summary>Wait for existence state.</summary>
        /// <param name="expected">Expected state. Null = skip.</param>
        /// <param name="timeoutMs">Timeout in ms. Null = use default.</param>
        /// <returns>True if condition met, false if timeout.</returns>
        bool WaitExists(bool? expected, int? timeoutMs = null);
        
        bool WaitVisible(bool? expected, int? timeoutMs = null);
        bool WaitEnabled(bool? expected, int? timeoutMs = null);
        
        // ─── Assertions (throw on failure) ───────────────────────────────
        
        /// <summary>Assert existence state.</summary>
        /// <param name="expected">Expected state. Null = skip.</param>
        /// <param name="message">Custom message. Null = default.</param>
        /// <param name="timeoutMs">Wait timeout before assert. Null = default.</param>
        void AssertExists(bool? expected, string? message = null, int? timeoutMs = null);
        
        void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);
        void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null);
        
        // ─── Text (inherited by all controls) ────────────────────────────
        
        /// <summary>Get element text content.</summary>
        /// <returns>Text if exists, null if element not found.</returns>
        string? GetText(int? timeoutMs = null);
        
        bool WaitText(string? expected, int? timeoutMs = null);
        void AssertText(string? expected, string? message = null, int? timeoutMs = null);
        void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);
        
        // ─── Attributes ──────────────────────────────────────────────────
        
        string? GetAttribute(string name);
    }
}
```

### 2.2 IClickableControl

Click capability - extends IControlObject.

```csharp
namespace Brinell.Core.Interfaces
{
    /// <summary>
    /// Interface for controls that support click/tap actions.
    /// Implemented by: Button, ImageButton, Link, etc.
    /// </summary>
    public interface IClickableControl : IControlObject
    {
        /// <summary>
        /// Perform a single click/tap on the control.
        /// Waits for element to be clickable (visible + enabled) first.
        /// </summary>
        void Click();
        
        /// <summary>
        /// Perform a double click/tap on the control.
        /// </summary>
        void DoubleClick();
    }
}
```

### 2.3 ITextControl

Read-only text capability.

```csharp
namespace Brinell.Core.Interfaces
{
    /// <summary>
    /// Interface for controls that display text.
    /// Note: GetText, WaitText, AssertText are in IControlObject base.
    /// This interface adds text-specific assertions.
    /// </summary>
    public interface ITextControl : IControlObject
    {
        /// <summary>Assert text matches regex pattern.</summary>
        void AssertTextMatches(string pattern, string? message = null, int? timeoutMs = null);
    }
}
```

### 2.4 IEditableTextControl

Editable text capability - extends ITextControl.

```csharp
namespace Brinell.Core.Interfaces
{
    /// <summary>
    /// Interface for controls that accept text input.
    /// Implemented by: Entry, Editor, SearchBar
    /// </summary>
    public interface IEditableTextControl : ITextControl
    {
        /// <summary>
        /// Append text to current value.
        /// </summary>
        /// <param name="text">Text to enter. Null = no action.</param>
        void Enter(string? text);
        
        /// <summary>
        /// Clear all text from the control.
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Replace current text (Clear + Enter).
        /// </summary>
        /// <param name="text">Text to set. Null = no action.</param>
        void SetText(string? text);
    }
}
```

### 2.5 IContainerControl

Container/View capability - control that is also a scope.

```csharp
namespace Brinell.Core.Interfaces
{
    /// <summary>
    /// Non-generic container interface.
    /// A container is both a control AND an element scope.
    /// </summary>
    public interface IContainerControl : IControlObject, IElementScope
    {
        /// <summary>
        /// Root element for scoped element finding.
        /// Child elements are found within this root.
        /// </summary>
        object ContainerRoot { get; }
    }
    
    /// <summary>
    /// Generic container with typed element finding.
    /// </summary>
    public interface IContainerControl<TElement> : IContainerControl, IElementScope<TElement>
    {
        /// <summary>Typed root element.</summary>
        new TElement ContainerRoot { get; }
    }
}
```

---

## 3. Page Interface (Core)

### 3.1 IPageObject

```csharp
namespace Brinell.Core.Interfaces
{
    /// <summary>
    /// Represents a page/screen/view in the application.
    /// Pages are element scopes for their controls.
    /// </summary>
    public interface IPageObject : IElementScope
    {
        /// <summary>Page name for logging.</summary>
        string Name { get; }
        
        /// <summary>Check if page is loaded.</summary>
        bool IsLoaded(int? timeoutMs = null);
        
        /// <summary>Wait for loaded state.</summary>
        bool WaitLoaded(bool? expected, int? timeoutMs = null);
        
        /// <summary>Assert loaded state.</summary>
        void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null);
        
        /// <summary>Get page title.</summary>
        string? GetTitle(int? timeoutMs = null);
        
        /// <summary>Take screenshot.</summary>
        void TakeScreenshot(string? filename = null, int? timeoutMs = null);
    }
    
    /// <summary>
    /// Generic page with typed element finding.
    /// </summary>
    public interface IPageObject<TElement> : IPageObject, IElementScope<TElement>
    {
    }
}
```

---

## 4. MAUI-Specific Interfaces

### 4.1 IMauiPageObject

```csharp
namespace Brinell.Maui.Interfaces
{
    /// <summary>
    /// MAUI page interface - typed to AppiumElement.
    /// </summary>
    public interface IMauiPageObject : IPageObject<AppiumElement>, IMauiElementScope
    {
        // Inherits from IMauiElementScope:
        // - IMauiTestContext Context { get; }
        // - AppiumElement? TryFindElement(Locator locator);
        // - AppiumElement FindElement(Locator locator);
        // - IReadOnlyList<AppiumElement> FindElements(Locator locator);
    }
}
```

### 4.2 IMauiContainerControl (View)

```csharp
namespace Brinell.Maui.Interfaces
{
    /// <summary>
    /// MAUI container/view interface - both a control and a scope.
    /// Use for: Cards, Panels, Forms, Sections, custom views.
    /// </summary>
    public interface IMauiContainerControl : IContainerControl<AppiumElement>, IMauiElementScope
    {
        /// <summary>Typed container root.</summary>
        new AppiumElement ContainerRoot { get; }
    }
}
```

### 4.3 IMauiListItemControl

```csharp
namespace Brinell.Maui.Interfaces
{
    /// <summary>
    /// MAUI list item interface - an item within a CollectionView/ListView.
    /// A list item is both a control (can be clicked) and a scope (contains children).
    /// </summary>
    public interface IMauiListItemControl : IMauiContainerControl, IClickableControl
    {
        /// <summary>Index of this item in the list (0-based).</summary>
        int Index { get; }
    }
}
```

---

## 5. Scope Hierarchy

The scope hierarchy determines where element finding starts:

```
IMauiTestContext (driver root)
    │
    ├── IMauiPageObject (page scope - searches from driver root)
    │   │
    │   ├── Controls (ButtonControl, EntryControl)
    │   │       └── search within driver root
    │   │
    │   └── IMauiContainerControl (view scope)
    │       │
    │       ├── Controls (search within view root)
    │       │
    │       └── Nested IMauiContainerControl
    │           │
    │           └── Controls (search within nested view root)
    │
    └── IMauiListControl<TItem>
            │
            └── IMauiListItemControl (item scope)
                    │
                    └── Controls (search within item root)
```

### 5.1 Scope Resolution Rules

| Control Location | Scope Type | Element Search Root |
|------------------|------------|---------------------|
| Directly on Page | IMauiPageObject | Driver root (whole screen) |
| Within View | IMauiContainerControl | View's container element |
| Within List Item | IMauiListItemControl | Item's container element |
| Nested View | IMauiContainerControl | Parent view's container |

### 5.2 Page Property Resolution

```csharp
// IControlObject.Page resolution:
// - If Scope is IPageObject → Page = Scope
// - If Scope is IContainerControl → Page = Scope.Page (recursive)
// - If Scope is ITestContext → Page = null

public IPageObject? Page => Scope switch
{
    IPageObject page => page,
    IControlObject control => control.Page,
    _ => null
};
```

---

## 6. Usage Patterns

### 6.1 Control on Page

```csharp
public class LoginPage : MauiPageObjectBase
{
    // Controls directly on page - page is scope
    public MauiEntryControl Username => new(this, "UsernameEntry");
    public MauiEntryControl Password => new(this, "PasswordEntry");
    public MauiButtonControl LoginButton => new(this, "LoginButton");
    
    public LoginPage(IMauiTestContext context) : base(context, "LoginPage") { }
}
```

### 6.2 Control in View (Container)

```csharp
public class AddressForm : MauiContainerBase
{
    // Controls within this form - form is scope
    public MauiEntryControl Street => new(this, "StreetEntry");
    public MauiEntryControl City => new(this, "CityEntry");
    public MauiEntryControl Zip => new(this, "ZipEntry");
    
    public AddressForm(IMauiElementScope scope, string automationId)
        : base(scope, automationId) { }
}

public class CheckoutPage : MauiPageObjectBase
{
    // View on page - page is scope for view
    public AddressForm ShippingAddress => new(this, "ShippingAddressForm");
    public AddressForm BillingAddress => new(this, "BillingAddressForm");
    
    public CheckoutPage(IMauiTestContext context) : base(context, "CheckoutPage") { }
}
```

### 6.3 View in View (Nested Containers)

```csharp
public class ProductCard : MauiContainerBase
{
    public MauiLabelControl Name => new(this, "ProductName");
    public MauiLabelControl Price => new(this, "ProductPrice");
    public MauiButtonControl AddToCart => new(this, "AddToCart");
    
    // Nested view - card contains review section
    public ReviewSection Reviews => new(this, "ReviewsSection");
    
    public ProductCard(IMauiElementScope scope, Locator locator)
        : base(scope, locator) { }
}

public class ReviewSection : MauiContainerBase
{
    public MauiLabelControl AverageRating => new(this, "AverageRating");
    public MauiLabelControl ReviewCount => new(this, "ReviewCount");
    
    public ReviewSection(IMauiElementScope scope, string automationId)
        : base(scope, automationId) { }
}
```

### 6.4 View in List

```csharp
public class ProductListItem : MauiListItemBase
{
    // Controls within this list item
    public MauiLabelControl ProductName => new(this, "ProductName");
    public MauiLabelControl Price => new(this, "Price");
    public MauiButtonControl AddToCart => new(this, "AddToCart");
    
    public ProductListItem(IMauiElementScope scope, Locator locator, int index)
        : base(scope, locator, index) { }
}

public class ProductListPage : MauiPageObjectBase
{
    // List of product items
    public MauiListControl<ProductListItem> Products => 
        new(this, "ProductList", (scope, locator, idx) => new ProductListItem(scope, locator, idx));
    
    public ProductListPage(IMauiTestContext context) : base(context, "ProductList") { }
}
```

---

## 7. Boundary Conditions

### 7.1 Scope Not Found

| Scenario | Behavior |
|----------|----------|
| View scope not found | ElementNotFoundException when accessing ContainerRoot |
| List item scope not found | ElementNotFoundException when accessing item |
| Page not loaded | Control searches may fail or timeout |

### 7.2 Control Not Found in Scope

| Scenario | Behavior |
|----------|----------|
| Control not in view | `TryFindElement` returns null |
| Control exists but outside scope | NOT found (scope limits search) |
| `IsExists()` on missing control | Returns `false` |

### 7.3 Null Parameter Handling

| Parameter | Null Behavior |
|-----------|--------------|
| `Enter(null)` | No-op, returns immediately |
| `SetText(null)` | No-op, returns immediately |
| `AssertText(null, ...)` | Skips assertion |
| `WaitExists(null, ...)` | Returns true immediately |

---

## 8. Acceptance Criteria

### ACC-001: Scope-Based Element Finding

```gherkin
Given a page with two AddressForm views
And each form has an Entry with AutomationId "CityEntry"
When shippingAddress.City.GetText() is called
Then it returns the city from the shipping form only
And NOT from the billing form
```

### ACC-002: Nested View Scoping

```gherkin
Given a ProductCard containing a ReviewSection
And ReviewSection contains AverageRating label
When card.Reviews.AverageRating.GetText() is called
Then it finds AverageRating within Reviews within ProductCard
```

### ACC-003: List Item Scoping

```gherkin
Given a ProductList with multiple items
When products[0].ProductName.GetText() is called
Then it returns the name from the first item only
```

### ACC-004: Page Property Resolution

```gherkin
Given a control within a view within a page
When control.Page is accessed
Then it returns the containing page
```

---

## Related Documents

- [250_202_MauiMinimalClasses](250_202_MauiMinimalClasses.spx.md) - Base classes and concrete controls
- [250_203_MauiMinimalScope](250_203_MauiMinimalScope.spx.md) - Detailed scoping patterns
- [250_001_IControlObject](../250_000_Foundation/250_001_IControlObject.spx.md) - Full IControlObject spec
- [250_002_IPageObject](../250_000_Foundation/250_002_IPageObject.spx.md) - Full IPageObject spec
- [250_003_IContainerControlObject](../250_000_Foundation/250_003_IContainerControlObject.spx.md) - Full container spec
