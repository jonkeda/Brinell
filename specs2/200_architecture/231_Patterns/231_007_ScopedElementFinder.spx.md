# 231_007 Scoped Element Finder Pattern

## pattern ScopedElementFinder

- **title**: Scoped Element Finder Pattern
- **type**: Structural
- **purpose**: Delegate element finding to typed scopes (pages and containers) using generic interfaces

---

## Description

The Scoped Element Finder pattern defines how controls locate their underlying elements. Instead of controls finding elements directly through the global `ITestContext`, elements are found within the scope of their owning page or container through the `IElementScope<TElement>` interface. This creates a hierarchical element finding mechanism where each scope is responsible for finding elements within its boundaries.

**Key Design:** Uses generic type parameter `TElement` on interfaces to provide typed element finding that flows through the hierarchy.

> **Note:** Code snippets in this document are illustrative examples showing architectural patterns. Actual implementation may vary. See source code for current implementation details.

---

## 1. Intent

**Problem:** Non-generic element finding requires casting and loses type information:

```csharp
// Problem: object-typed returns require casting
public interface IElementScope
{
    object? TryFindElement(Locator locator);  // Returns object, must cast to AppiumElement
}
```

**Solution:** Use generic `IElementScope<TElement>` that provides typed element finding:

```csharp
// Solution: Generic interface with typed returns
public interface IElementScope<TElement> : IElementScope
{
    TElement? TryFindElement(Locator locator);  // Returns typed element directly
    TElement FindElement(Locator locator);
    IReadOnlyList<TElement> FindElements(Locator locator);
}

// Platform interfaces narrow TElement
public interface IMauiElementScope : IElementScope<AppiumElement>
{
    // Inherited: TryFindElement() returns AppiumElement? - no casting needed!
}
```

---

## 2. Key Concepts

### 2.1 Generic IElementScope Interface

```csharp
/// <summary>
/// Non-generic base interface for basic scope operations.
/// </summary>
public interface IElementScope
{
    /// <summary>
    /// The test context for accessing driver, timeouts, logging.
    /// </summary>
    ITestContext Context { get; }
    
    /// <summary>
    /// The underlying element that defines this scope's boundaries.
    /// Null for pages (use driver root), populated for containers.
    /// </summary>
    object? ScopeRoot { get; }
}

/// <summary>
/// Generic element scope with typed element finding.
/// TElement is the driver element type (AppiumElement, IWebElement, etc.)
/// </summary>
public interface IElementScope<TElement> : IElementScope
{
    /// <summary>
    /// Typed test context for element finding.
    /// </summary>
    new ITestContext<TElement> Context { get; }
    
    /// <summary>
    /// Typed scope root element.
    /// </summary>
    new TElement? ScopeRoot { get; }
    
    /// <summary>
    /// Find element within this scope. Returns null if not found.
    /// </summary>
    TElement? TryFindElement(Locator locator);
    
    /// <summary>
    /// Find element within this scope. Throws if not found.
    /// </summary>
    TElement FindElement(Locator locator);
    
    /// <summary>
    /// Find all matching elements within this scope.
    /// </summary>
    IReadOnlyList<TElement> FindElements(Locator locator);
}
```

### 2.2 Scope Hierarchy

```
ITestContext<TElement> (driver root - provides TryFindElement from driver)
    │
    └── IPageObject<TElement> : IElementScope<TElement>
            │   (page scope - ScopeRoot = null, delegates to context)
            │
            ├── Control (uses page scope via TryFindElement)
            │
            └── IContainerControl<TElement> : IElementScope<TElement>
                    │   (container scope - ScopeRoot = container element)
                    │
                    └── Control (uses container scope via TryFindElement)
```

### 2.3 Platform Interface Narrowing

```csharp
// Platform interfaces narrow TElement through inheritance
public interface IMauiElementScope : IElementScope<AppiumElement>
{
    new IMauiTestContext Context { get; }
    // Inherited: TryFindElement() returns AppiumElement?
}

public interface IBlazorElementScope : IElementScope<IWebElement>
{
    new IBlazorTestContext Context { get; }
    // Inherited: TryFindElement() returns IWebElement?
}

public interface IWpfElementScope : IElementScope<AutomationElement>
{
    new IWpfTestContext Context { get; }
    // Inherited: TryFindElement() returns AutomationElement?
}
```

---

## 3. Implementation with Generics

### 3.1 ITestContext with Generic Element Finding

```csharp
/// <summary>
/// Generic test context with typed element finding from driver root.
/// </summary>
public interface ITestContext<TElement> : ITestContext
{
    TElement? TryFindElement(Locator locator);
    TElement FindElement(Locator locator);
    IReadOnlyList<TElement> FindElements(Locator locator);
    
    // Scoped finding within a root element
    TElement? TryFindElement(Locator locator, TElement scopeRoot);
    TElement FindElement(Locator locator, TElement scopeRoot);
    IReadOnlyList<TElement> FindElements(Locator locator, TElement scopeRoot);
}
```

### 3.2 Generic Page Base Class

```csharp
/// <summary>
/// Generic page base with typed element finding.
/// TElement: Driver element type
/// TContext: Context type (for implementation convenience)
/// </summary>
public abstract class PageObjectBase<TElement, TContext> : IPageObject<TElement>
    where TContext : ITestContext<TElement>
{
    protected readonly TContext _context;
    
    protected PageObjectBase(TContext context, string name)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
    
    // IElementScope<TElement> - page delegates to context (searches from driver root)
    public TElement? ScopeRoot => default;  // Page uses driver root
    object? IElementScope.ScopeRoot => null;
    
    ITestContext<TElement> IElementScope<TElement>.Context => _context;
    ITestContext IElementScope.Context => _context;
    
    public TElement? TryFindElement(Locator locator) => _context.TryFindElement(locator);
    public TElement FindElement(Locator locator) => _context.FindElement(locator);
    public IReadOnlyList<TElement> FindElements(Locator locator) => _context.FindElements(locator);
    
    // IPageObject
    public string Name { get; }
    public virtual LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
}
```

### 3.3 Generic Container Base Class

```csharp
/// <summary>
/// Generic container base - is both control and scope.
/// TElement: Driver element type
/// TScope: Parent scope type (for implementation convenience)
/// </summary>
public abstract class ContainerBase<TElement, TScope> : ControlBase<TElement, TScope>, IContainerControl<TElement>
    where TScope : IElementScope<TElement>
{
    protected ContainerBase(TScope parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }
    
    // IElementScope<TElement> - container searches within its bounds
    public TElement? ScopeRoot => TryFindSelf();
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
        return _scope.Context.TryFindElement(locator, root);  // Scoped search
    }
    
    public TElement FindElement(Locator locator)
    {
        var root = base.FindElement();  // Throws if container not found
        return _scope.Context.FindElement(locator, root);  // Scoped search
    }
    
    public IReadOnlyList<TElement> FindElements(Locator locator)
    {
        var root = ScopeRoot;
        if (root == null) return Array.Empty<TElement>();
        return _scope.Context.FindElements(locator, root);  // Scoped search
    }
    
    private TElement? TryFindSelf() => _scope.TryFindElement(_locator);
}
```

### 3.4 Generic Control Base Class

```csharp
/// <summary>
/// Generic control base that uses typed scope for element finding.
/// TElement: Driver element type
/// TScope: Scope type (for implementation convenience)
/// </summary>
public abstract class ControlBase<TElement, TScope> : IControlObject
    where TScope : IElementScope<TElement>
{
    protected readonly TScope _scope;
    protected readonly Locator _locator;
    
    protected ControlBase(TScope scope, Locator locator)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
    }
    
    protected ControlBase(TScope scope, string automationId)
        : this(scope, Locator.ByAutomationId(automationId))
    {
    }
    
    // IControlObject
    public Locator Locator => _locator;
    public IElementScope Scope => _scope;
    public IPageObject? Page => _scope as IPageObject;
    
    /// <summary>
    /// Find this control's element via scope - typed return!
    /// </summary>
    protected TElement? TryFindElement() => _scope.TryFindElement(_locator);
    protected TElement FindElement() => _scope.FindElement(_locator);
    
    // State methods
    public bool IsExists() => TryFindElement() != null;
}
```

---

## 4. Platform Type Aliases

### 4.1 MAUI Type Aliases

```csharp
namespace Brinell.Maui
{
    /// <summary>
    /// MAUI page base - typed alias for common use.
    /// </summary>
    public abstract class MauiPageObjectBase : PageObjectBase<AppiumElement, IMauiTestContext>, IMauiPageObject
    {
        protected MauiPageObjectBase(IMauiTestContext context, string name) 
            : base(context, name) { }
        
        // IMauiElementScope - narrow Context type
        IMauiTestContext IMauiElementScope.Context => _context;
    }
    
    /// <summary>
    /// MAUI control base - typed alias for common use.
    /// </summary>
    public abstract class MauiControlBase : ControlBase<AppiumElement, IMauiElementScope>
    {
        protected MauiControlBase(IMauiElementScope scope, Locator locator) 
            : base(scope, locator) { }
        
        protected MauiControlBase(IMauiElementScope scope, string automationId)
            : base(scope, automationId) { }
        
        // Convenience: typed context access
        protected IMauiTestContext Context => _scope.Context;
    }
    
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
        
        protected IMauiTestContext Context => _scope.Context;
    }
}
```

### 4.2 Blazor Type Aliases

```csharp
namespace Brinell.Blazor
{
    /// <summary>
    /// Blazor page base - typed alias for common use.
    /// </summary>
    public abstract class BlazorPageObjectBase : PageObjectBase<IWebElement, IBlazorTestContext>, IBlazorPageObject
    {
        protected BlazorPageObjectBase(IBlazorTestContext context, string name) 
            : base(context, name) { }
        
        public override LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.DataTestId;
        
        IBlazorTestContext IBlazorElementScope.Context => _context;
    }
    
    /// <summary>
    /// Blazor control base - typed alias for common use.
    /// </summary>
    public abstract class BlazorControlBase : ControlBase<IWebElement, IBlazorElementScope>
    {
        protected BlazorControlBase(IBlazorElementScope scope, Locator locator) 
            : base(scope, locator) { }
        
        protected BlazorControlBase(IBlazorElementScope scope, string testId)
            : base(scope, Locator.ByDataTestId(testId)) { }
        
        protected IBlazorTestContext Context => _scope.Context;
    }
    
    /// <summary>
    /// Blazor container base - typed alias for common use.
    /// </summary>
    public abstract class BlazorContainerBase : ContainerBase<IWebElement, IBlazorElementScope>, IBlazorContainerControl
    {
        protected BlazorContainerBase(IBlazorElementScope parentScope, Locator locator)
            : base(parentScope, locator) { }
        
        IBlazorTestContext IBlazorElementScope.Context => _scope.Context;
        
        protected IBlazorTestContext Context => _scope.Context;
    }
}
```

---

## 5. Usage Examples

### 5.1 Page with Controls

```csharp
// Page is the scope for its controls - typed throughout
public class LoginPage : MauiPageObjectBase
{
    // Controls use 'this' (page) as scope - returns typed controls
    public MauiEntryControl Username => new(this, "UsernameEntry");
    public MauiEntryControl Password => new(this, "PasswordEntry");
    public MauiButtonControl LoginButton => new(this, "LoginButton");
    //                                          ^^^^ 'this' is IMauiElementScope
    
    public LoginPage(IMauiTestContext context) : base(context, "LoginPage")
    {
    }
}

// Usage in test
var loginPage = new LoginPage(_context);
loginPage.Username.Enter("testuser");      // Username.TryFindElement() → page.TryFindElement() → context.TryFindElement()
loginPage.Password.Enter("password123");
loginPage.LoginButton.Click();
```

### 5.2 Container with Scoped Controls

```csharp
// Container is the scope for its controls
public class SettingsPanel : MauiContainerBase
{
    // Children use 'this' (container) as scope
    public MauiLabelControl Title => new(this, "PanelTitle");
    public MauiToggleControl DarkMode => new(this, "DarkModeToggle");
    public MauiButtonControl Save => new(this, "SaveButton");
    //                                   ^^^^ 'this' is IMauiElementScope
    
    public SettingsPanel(IMauiElementScope parentScope, Locator locator) 
        : base(parentScope, locator)
    {
    }
    
    public SettingsPanel(IMauiElementScope parentScope, string automationId) 
        : base(parentScope, automationId)
    {
    }
}

// Page with container
public class SettingsPage : MauiPageObjectBase
{
    // Containers use 'this' (page) as parent scope
    public SettingsPanel GeneralSettings => new(this, "GeneralPanel");
    public SettingsPanel AdvancedSettings => new(this, "AdvancedPanel");
    
    public SettingsPage(IMauiTestContext context) : base(context, "SettingsPage")
    {
    }
}

// Usage: scoped element finding in action
var settingsPage = new SettingsPage(_context);
settingsPage.GeneralSettings.DarkMode.Toggle();  // Searches within GeneralPanel
settingsPage.AdvancedSettings.Save.Click();      // Searches within AdvancedPanel
```

### 5.3 Nested Containers

```csharp
// Outer container
public class ProductCard : MauiContainerBase
{
    public MauiLabelControl Name => new(this, "ProductName");
    public PriceSection PriceInfo => new(this, "PriceSection");  // Nested container
    
    public ProductCard(IMauiElementScope parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }
}

// Inner container
public class PriceSection : MauiContainerBase
{
    public MauiLabelControl Price => new(this, "Price");
    public MauiLabelControl Discount => new(this, "Discount");
    
    public PriceSection(IMauiElementScope parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }
    
    public PriceSection(IMauiElementScope parentScope, string automationId)
        : base(parentScope, automationId)
    {
    }
}

// Usage: deeply nested scopes
var card = page.Products[0] as ProductCard;
card.Name.GetText();              // Searches within card
card.PriceInfo.Price.GetText();   // Searches within price section within card
card.PriceInfo.Discount.GetText(); // Searches within price section within card
```

### 5.4 Blazor Example

```csharp
public class LoginPageBlazor : BlazorPageObjectBase
{
    public BlazorEntryControl Username => new(this, "username-input");
    public BlazorEntryControl Password => new(this, "password-input");
    public BlazorButtonControl LoginButton => new(this, "login-button");
    
    public LoginPageBlazor(IBlazorTestContext context) : base(context, "LoginPage")
    {
    }
}

public class FormContainerBlazor : BlazorContainerBase
{
    public BlazorEntryControl Email => new(this, "email");
    public BlazorEntryControl Phone => new(this, "phone");
    
    public FormContainerBlazor(IBlazorElementScope parentScope, string testId)
        : base(parentScope, Locator.ByDataTestId(testId))
    {
    }
}
```

---

## 6. Element Finding Flow

### 6.1 Scope Hierarchy

```
Control.TryFindElement(locator)
    ↓
Scope.TryFindElement(locator)         // Page or Container
    ↓
[If Page] Context.TryFindElement()    // Search from driver root
[If Container] FindWithinRoot()       // Search within container bounds
```

### 6.2 Page as Scope (Delegates to Context)

```csharp
// PageObjectBase<TElement, TContext>
public TElement? TryFindElement(Locator locator)
{
    // Pages delegate to context (driver root)
    return _context.TryFindElement(locator);
}
```

### 6.3 Container as Scope (Searches Within Bounds)

```csharp
// ContainerBase<TElement, TScope>
public TElement? TryFindElement(Locator locator)
{
    // First, find container's own root element
    var root = _scope.TryFindElement(_locator);
    if (root == null) return default;
    
    // Then search within container bounds
    return FindWithinRoot(root, locator);
}

// Platform-specific implementation
protected abstract TElement? FindWithinRoot(TElement root, Locator locator);
```

### 6.4 MAUI Container FindWithinRoot

```csharp
// MauiContainerBase
protected override AppiumElement? FindWithinRoot(AppiumElement root, Locator locator)
{
    var by = ConvertLocatorToBy(locator);
    try
    {
        return root.FindElement(by);  // Search WITHIN container
    }
    catch (NoSuchElementException)
    {
        return null;
    }
}

private By ConvertLocatorToBy(Locator locator)
{
    return locator.Strategy switch
    {
        LocatorStrategy.AutomationId => MobileBy.AccessibilityId(locator.Value),
        LocatorStrategy.XPath => By.XPath($".//{locator.Value}"),  // Relative XPath!
        LocatorStrategy.ClassName => By.ClassName(locator.Value),
        _ => throw new NotSupportedException($"Strategy {locator.Strategy}")
    };
}
```

### 6.5 Blazor Container FindWithinRoot

```csharp
// BlazorContainerBase
protected override IWebElement? FindWithinRoot(IWebElement root, Locator locator)
{
    var by = ConvertLocatorToBy(locator);
    try
    {
        return root.FindElement(by);  // Search WITHIN container
    }
    catch (NoSuchElementException)
    {
        return null;
    }
}

private By ConvertLocatorToBy(Locator locator)
{
    return locator.Strategy switch
    {
        LocatorStrategy.DataTestId => By.CssSelector($"[data-testid='{locator.Value}']"),
        LocatorStrategy.Css => By.CssSelector(locator.Value),
        LocatorStrategy.XPath => By.XPath($".//{locator.Value}"),  // Relative XPath!
        LocatorStrategy.Id => By.Id(locator.Value),
        _ => throw new NotSupportedException($"Strategy {locator.Strategy}")
    };

---

## 7. Benefits

### 7.1 Type Safety Throughout

```csharp
// Type flows through the hierarchy
IMauiTestContext       → TryFindElement() returns AppiumElement?
IMauiPageObject        → TryFindElement() returns AppiumElement?  
IMauiContainerControl  → TryFindElement() returns AppiumElement?
MauiControlBase        → TryFindElement() returns AppiumElement?

// No casting required anywhere!
```

### 7.2 Cleaner Locators

```csharp
// Before: Complex scoped locators (old pattern)
new MauiButtonControl(_context, Locator.ByAutomationId("Save").ScopedTo(panelLocator), _page);

// After: Simple locators, scope is implicit
new MauiButtonControl(panel, "Save");
```

### 7.3 Automatic Scoping

- Child controls automatically search within parent container
- No need to manually compose locators
- Relative XPath works naturally (`./descendant::*[@id='X']`)

### 7.4 Better Performance

- Searches start from container element, not document root
- Reduces traversal time in complex UIs
- Eliminates duplicate full-page searches

### 7.5 Clearer Ownership

- Controls know their typed scope (page or container)

---

## 8. Migration Path

### 8.1 Phase 1: Add Generic Interfaces

1. Define `IElementScope<TElement>` interface
2. Add `IPageObject<TElement>` extending `IElementScope<TElement>`
3. Add `IContainerControl<TElement>` extending `IElementScope<TElement>`
4. Define platform interfaces (`IMauiElementScope`, `IBlazorElementScope`, etc.)

### 8.2 Phase 2: Update Base Classes

1. Create `PageObjectBase<TElement, TContext>` with typed methods
2. Create `ContainerBase<TElement, TScope>` with scoped finding
3. Create `ControlBase<TElement, TScope>` accepting scope
4. Add platform type aliases (`MauiPageObjectBase`, etc.)

### 8.3 Phase 3: Update Controls

1. Controls accept `IElementScope<TElement>` scope
2. Update constructors: `(scope, locator)` instead of `(context, locator, page)`
3. Internal finding uses `_scope.TryFindElement()`

### 8.4 Phase 4: Remove Old Pattern

1. Remove deprecated `(context, locator, page)` constructors
2. Remove `Locator.ScopedTo()` method
3. Update all sample apps and tests

---

## 9. Comparison: Before and After

| Aspect | Before | After |
|--------|--------|-------|
| Element Finding | Context globally | Scope locally (typed) |
| Control Constructor | `(context, locator, page)` | `(scope, locator)` |
| Container Children | `Locator.ScopedTo(parent)` | Simple locator |
| Search Root | Always driver root | Container element or driver |
| Return Types | `object?` (cast needed) | `TElement?` (typed) |
| IPageObject | Identity only | `IPageObject<TElement>` + scope |
| IContainer | IControlObject | `IContainerControl<TElement>` + scope |

### 9.1 Code Comparison

```csharp
// BEFORE: Untyped, explicit scoping
public class LoginPage : PageObjectBase
{
    public ButtonControl Login { get; }
    
    public LoginPage(ITestContext context) : base(context, "LoginPage")
    {
        Login = new ButtonControl(context, "LoginButton", this);
        //                        ^^^^^^^ context, not scope
    }
}

// AFTER: Typed, implicit scoping
public class LoginPage : MauiPageObjectBase
{
    public MauiButtonControl Login => new(this, "LoginButton");
    //                                    ^^^^ scope (page)
    
    public LoginPage(IMauiTestContext context) : base(context, "LoginPage") { }
}
```

---

## 10. Validation Rules

The Scoped Element Finder pattern is valid when:

- [ ] `IElementScope<TElement>` interface defines typed finding methods
- [ ] Platform interfaces narrow `TElement` via inheritance (e.g., `IMauiElementScope : IElementScope<AppiumElement>`)
- [ ] `IPageObject<TElement>` extends `IElementScope<TElement>`
- [ ] `IContainerControl<TElement>` extends `IElementScope<TElement>`
- [ ] Controls accept `IElementScope<TElement>` scope, not context directly
- [ ] Child controls receive their parent (page/container) as scope
- [ ] Containers search within their bounds using `FindWithinRoot()`
- [ ] Pages delegate to context (search from driver root)
- [ ] Locators are simple — no `ScopedTo()` chaining required
- [ ] XPath locators use relative paths inside containers (`./descendant::*`)
- [ ] No type casting required for element access
- [ ] Platform base classes provide typed convenience aliases

---

## Related Documents

- [250_001 IControlObject](../../250_specifications/250_000_Foundation/250_001_IControlObject.spx.md)
- [250_002 IPageObject](../../250_specifications/250_000_Foundation/250_002_IPageObject.spx.md)
- [250_003 IContainerControlObject](../../250_specifications/250_000_Foundation/250_003_IContainerControlObject.spx.md)
- [250_004 TestContext](../../250_specifications/250_000_Foundation/250_004_TestContext.spx.md)
- [231_001 Control Object Pattern](231_001_ControlObjectPattern.spx.md)
- [231_002 Page Object Pattern](231_002_PageObjectPattern.spx.md)
- [231_003b Platform-Specific Hierarchy](231_003b_PlatformSpecificHierarchy.spx.md)
- [231_004 Container Pattern](231_004_ContainerPattern.spx.md)
