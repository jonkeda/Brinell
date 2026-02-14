# 231_003b Platform-Specific Hierarchy Pattern

## pattern PlatformSpecificHierarchy

- **title**: Platform-Specific Hierarchy Pattern
- **type**: Structural
- **purpose**: Eliminate casting by using generic type parameters to narrow element types through inheritance

---

## Description

The Platform-Specific Hierarchy pattern extends the Adapter pattern (231_003) by using **generic type parameters** on interfaces (`ITestContext<TElement>`, `IElementScope<TElement>`, `IPageObject<TElement>`) that narrow through inheritance to provide platform-specific typed access.

This pattern replaces the previous approach of parallel typed methods (e.g., `TryFindMauiElement()`, `TryFindWebElement()`) with a single generic method (`TryFindElement()`) where the element type is determined by the interface hierarchy.

> **Key Design Decision:** Generic type parameters appear on **interfaces only** (`TElement`). Base classes add `TContext`/`TScope` parameters for implementation convenience, but these are not part of the interface contract.

> **Note:** Code snippets in this document are illustrative examples showing architectural patterns. Actual implementation may vary. See source code for current implementation details.

---

## 1. Intent

**Problem:** Original approach used parallel typed methods:

```csharp
// ❌ OLD APPROACH: Parallel typed methods
public interface IMauiElementScope : IElementScope
{
    AppiumElement? TryFindMauiElement(Locator locator);  // MAUI-specific method name
    AppiumElement FindMauiElement(Locator locator);
}

public interface IBlazorElementScope : IElementScope
{
    IWebElement? TryFindWebElement(Locator locator);  // Blazor-specific method name
    IWebElement FindWebElement(Locator locator);
}

// Problem: Method names differ by platform, no shared contract
```

**Solution:** Use generic type parameters that narrow through inheritance:

```csharp
// ✅ NEW APPROACH: Generic interfaces with TElement
public interface IElementScope<TElement> : IElementScope
{
    TElement? TryFindElement(Locator locator);  // Same method name
    TElement FindElement(Locator locator);
}

// Platform interfaces narrow TElement via inheritance
public interface IMauiElementScope : IElementScope<AppiumElement>
{
    // TryFindElement() returns AppiumElement? - inherited from IElementScope<AppiumElement>
    new IMauiTestContext Context { get; }  // Narrow context type
}

public interface IBlazorElementScope : IElementScope<IWebElement>
{
    // TryFindElement() returns IWebElement? - inherited from IElementScope<IWebElement>
    new IBlazorTestContext Context { get; }
}
```

---

## 2. Generic Interface Hierarchy

### 2.1 Core Generic Interfaces

```csharp
/// <summary>
/// Base test context with typed element finding.
/// Platform interfaces narrow TElement to driver-specific types.
/// </summary>
public interface ITestContext<TElement> : ITestContext
{
    TElement? TryFindElement(Locator locator);
    TElement FindElement(Locator locator);
    IReadOnlyList<TElement> FindElements(Locator locator);
    
    TElement? TryFindElement(Locator locator, TElement scopeRoot);
    TElement FindElement(Locator locator, TElement scopeRoot);
    IReadOnlyList<TElement> FindElements(Locator locator, TElement scopeRoot);
}

/// <summary>
/// Element scope with typed element finding.
/// Pages and containers implement this to provide search scope.
/// </summary>
public interface IElementScope<TElement> : IElementScope
{
    new ITestContext<TElement> Context { get; }
    new TElement? ScopeRoot { get; }
    
    TElement? TryFindElement(Locator locator);
    TElement FindElement(Locator locator);
    IReadOnlyList<TElement> FindElements(Locator locator);
}

/// <summary>
/// Page object with typed element scope.
/// </summary>
public interface IPageObject<TElement> : IPageObject, IElementScope<TElement>
{
}

/// <summary>
/// Container with typed element scope.
/// Container IS both a control and a scope for its children.
/// </summary>
public interface IContainerControl<TElement> : IControlObject, IElementScope<TElement>
{
}
```

### 2.2 Platform Interface Narrowing

```csharp
// MAUI Platform - narrows TElement to AppiumElement
public interface IMauiTestContext : ITestContext<AppiumElement>
{
    AppiumDriver Driver { get; }
    void HideKeyboard();
    bool IsKeyboardShown();
    // Inherited: TryFindElement() returns AppiumElement?
}

public interface IMauiElementScope : IElementScope<AppiumElement>
{
    new IMauiTestContext Context { get; }
    // Inherited: TryFindElement() returns AppiumElement?
}

public interface IMauiPageObject : IPageObject<AppiumElement>, IMauiElementScope
{
}

public interface IMauiContainerControl : IContainerControl<AppiumElement>, IMauiElementScope
{
}

// Blazor Platform - narrows TElement to IWebElement
public interface IBlazorTestContext : ITestContext<IWebElement>
{
    IWebDriver Driver { get; }
    void WaitForBlazorReady(int? timeoutMs = null);
    void NavigateTo(string path);
    // Inherited: TryFindElement() returns IWebElement?
}

public interface IBlazorElementScope : IElementScope<IWebElement>
{
    new IBlazorTestContext Context { get; }
    // Inherited: TryFindElement() returns IWebElement?
}

public interface IBlazorPageObject : IPageObject<IWebElement>, IBlazorElementScope
{
}

public interface IBlazorContainerControl : IContainerControl<IWebElement>, IBlazorElementScope
{
}

// WPF Platform - narrows TElement to AutomationElement
public interface IWpfTestContext : ITestContext<AutomationElement>
{
    Application Application { get; }
    Window MainWindow { get; }
    // Inherited: TryFindElement() returns AutomationElement?
}

public interface IWpfElementScope : IElementScope<AutomationElement>
{
    new IWpfTestContext Context { get; }
}

public interface IWpfPageObject : IPageObject<AutomationElement>, IWpfElementScope
{
}

public interface IWpfContainerControl : IContainerControl<AutomationElement>, IWpfElementScope
{
}
```

---

## 3. Interface Hierarchy Diagram

```
                          ITestContext
                               │
                     ITestContext<TElement>
                               │
         ┌─────────────────────┼─────────────────────┐
         │                     │                     │
    IMauiTestContext     IBlazorTestContext    IWpfTestContext
    <AppiumElement>       <IWebElement>        <AutomationElement>


                          IElementScope
                               │
                    IElementScope<TElement>
                               │
         ┌─────────────────────┼─────────────────────┐
         │                     │                     │
    IMauiElementScope    IBlazorElementScope    IWpfElementScope
    <AppiumElement>       <IWebElement>         <AutomationElement>


                           IPageObject
                               │
                     IPageObject<TElement>
                               │
         ┌─────────────────────┼─────────────────────┐
         │                     │                     │
     IMauiPageObject     IBlazorPageObject      IWpfPageObject
     <AppiumElement>      <IWebElement>         <AutomationElement>


                         IControlObject
                               │
    (Controls receive IElementScope<TElement> as their scope)
```

---

## 4. Base Class Implementations

> **Key Design:** Base classes use **two type parameters** (`TElement` + `TContext`/`TScope`) for implementation convenience. Interfaces use only `TElement`.

### 4.1 Generic Base Classes

```csharp
/// <summary>
/// Generic page base class with two type parameters.
/// TElement: The element type (narrowed by interface)
/// TContext: The context type (for implementation convenience)
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
    
    // IPageObject
    public string Name { get; }
    public virtual LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
    
    // Typed context access for subclasses
    protected TContext Context => _context;
    
    // IElementScope<TElement> - page delegates to context (driver root)
    public TElement? ScopeRoot => default;  // Page uses driver root
    object? IElementScope.ScopeRoot => null;
    
    ITestContext<TElement> IElementScope<TElement>.Context => _context;
    ITestContext IElementScope.Context => _context;
    
    public TElement? TryFindElement(Locator locator) => _context.TryFindElement(locator);
    public TElement FindElement(Locator locator) => _context.FindElement(locator);
    public IReadOnlyList<TElement> FindElements(Locator locator) => _context.FindElements(locator);
    
    public virtual bool IsLoaded(int? timeoutMs = null) => true;
}

/// <summary>
/// Generic control base class with two type parameters.
/// TElement: The element type
/// TScope: The scope type (for implementation convenience)
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
    
    // Typed scope access for subclasses
    protected TScope TypedScope => _scope;
    
    // Element finding via scope
    protected TElement? TryFindElement() => _scope.TryFindElement(_locator);
    protected TElement FindElement() => _scope.FindElement(_locator);
    
    // State methods
    public bool IsExists() => TryFindElement() != null;
}

/// <summary>
/// Generic container base - is both control and scope.
/// </summary>
public abstract class ContainerBase<TElement, TScope> : ControlBase<TElement, TScope>, IContainerControl<TElement>
    where TScope : IElementScope<TElement>
{
    protected ContainerBase(TScope parentScope, Locator locator)
        : base(parentScope, locator)
    {
    }
    
    // IElementScope<TElement> - container provides scoped searching
    public TElement? ScopeRoot => TryFindElement();
    object? IElementScope.ScopeRoot => ScopeRoot;
    
    ITestContext<TElement> IElementScope<TElement>.Context => _scope.Context;
    ITestContext IElementScope.Context => _scope.Context;
    
    public TElement? TryFindElement(Locator locator)
    {
        var root = ScopeRoot;
        if (root == null) return default;
        return _scope.Context.TryFindElement(locator, root);
    }
    
    public TElement FindElement(Locator locator)
    {
        var root = FindElement();  // Throws if container not found
        return _scope.Context.FindElement(locator, root);
    }
    
    public IReadOnlyList<TElement> FindElements(Locator locator)
    {
        var root = ScopeRoot;
        if (root == null) return Array.Empty<TElement>();
        return _scope.Context.FindElements(locator, root);
    }
}
```

### 4.2 Platform Type Aliases

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
        
        // IMauiElementScope
        IMauiTestContext IMauiElementScope.Context => _scope.Context;
        
        // Convenience
        protected IMauiTestContext Context => _scope.Context;
    }
}

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

namespace Brinell.Wpf
{
    /// <summary>
    /// WPF page base - typed alias for common use.
    /// </summary>
    public abstract class WpfPageObjectBase : PageObjectBase<AutomationElement, IWpfTestContext>, IWpfPageObject
    {
        protected WpfPageObjectBase(IWpfTestContext context, string name) 
            : base(context, name) { }
        
        IWpfTestContext IWpfElementScope.Context => _context;
    }
    
    /// <summary>
    /// WPF control base - typed alias for common use.
    /// </summary>
    public abstract class WpfControlBase : ControlBase<AutomationElement, IWpfElementScope>
    {
        protected WpfControlBase(IWpfElementScope scope, Locator locator) 
            : base(scope, locator) { }
        
        protected IWpfTestContext Context => _scope.Context;
    }
    
    /// <summary>
    /// WPF container base - typed alias for common use.
    /// </summary>
    public abstract class WpfContainerBase : ContainerBase<AutomationElement, IWpfElementScope>, IWpfContainerControl
    {
        protected WpfContainerBase(IWpfElementScope parentScope, Locator locator)
            : base(parentScope, locator) { }
        
        IWpfTestContext IWpfElementScope.Context => _scope.Context;
        
        protected IWpfTestContext Context => _scope.Context;
    }
}
```

---

## 5. Concrete Control Examples

### 5.1 MAUI Button Control

```csharp
namespace Brinell.Maui.Controls
{
    public class MauiButtonControl : MauiControlBase, IClickableControl
    {
        public MauiButtonControl(IMauiElementScope scope, Locator locator)
            : base(scope, locator) { }
        
        public MauiButtonControl(IMauiElementScope scope, string automationId)
            : base(scope, automationId) { }
        
        public void Click(int? timeoutMs = null)
        {
            var element = FindElement();  // Returns AppiumElement - typed!
            WaitClickable(timeoutMs);
            element.Click();
            
            // Platform-specific: Hide keyboard if shown
            if (Context.IsKeyboardShown())
            {
                Context.HideKeyboard();  // ✅ No casting needed!
            }
        }
        
        public void DoubleClick(int? timeoutMs = null)
        {
            var element = FindElement();  // AppiumElement
            var actions = new Actions(Context.Driver);  // ✅ Typed driver access
            actions.DoubleClick(element).Perform();
        }
        
        public void LongPress(int durationMs = 1000)
        {
            var element = FindElement();
            var actions = new Actions(Context.Driver);
            actions.ClickAndHold(element)
                .Pause(TimeSpan.FromMilliseconds(durationMs))
                .Release()
                .Perform();
        }
    }
}
```

### 5.2 Blazor Button Control

```csharp
namespace Brinell.Blazor.Controls
{
    public class BlazorButtonControl : BlazorControlBase, IClickableControl
    {
        public BlazorButtonControl(IBlazorElementScope scope, Locator locator)
            : base(scope, locator) { }
        
        public BlazorButtonControl(IBlazorElementScope scope, string testId)
            : base(scope, testId) { }
        
        public void Click(int? timeoutMs = null)
        {
            var element = FindElement();  // Returns IWebElement - typed!
            WaitClickable(timeoutMs);
            
            // Scroll into view before click (web-specific)
            ((IJavaScriptExecutor)Context.Driver).ExecuteScript(
                "arguments[0].scrollIntoView(true);", element);  // ✅ No casting for Context!
            element.Click();
            
            // Wait for Blazor to process
            Context.WaitForBlazorReady();  // ✅ Platform-specific method!
        }
        
        public void ClickViaJavaScript(int? timeoutMs = null)
        {
            var element = FindElement();
            ((IJavaScriptExecutor)Context.Driver).ExecuteScript(
                "arguments[0].click();", element);
            Context.WaitForBlazorReady();
        }
        
        public void Hover(int? timeoutMs = null)
        {
            var element = FindElement();
            new Actions(Context.Driver).MoveToElement(element).Perform();
        }
    }
}
```

---

## 6. Page Examples

### 6.1 MAUI Login Page

```csharp
namespace MyApp.Maui.UITests.Pages
{
    public class LoginPage : MauiPageObjectBase
    {
        // Controls receive 'this' (page) as scope - typed as IMauiElementScope
        public MauiEntryControl Username => new(this, "UsernameEntry");
        public MauiEntryControl Password => new(this, "PasswordEntry");
        public MauiButtonControl LoginButton => new(this, "LoginButton");
        public MauiLabelControl ErrorLabel => new(this, "ErrorLabel");
        
        public LoginPage(IMauiTestContext context) : base(context, "LoginPage")
        {
        }
        
        public override bool IsLoaded(int? timeoutMs = null)
        {
            return LoginButton.WaitExists(true, timeoutMs);
        }
        
        public void Login(string username, string password)
        {
            Username.Enter(username);
            Password.Enter(password);
            
            // Platform-specific: hide keyboard before clicking
            Context.HideKeyboard();  // ✅ Direct access, no casting!
            
            LoginButton.Click();
        }
    }
}
```

### 6.2 Blazor Login Page

```csharp
namespace MyApp.Blazor.UITests.Pages
{
    public class LoginPage : BlazorPageObjectBase
    {
        // Controls receive 'this' (page) as scope - typed as IBlazorElementScope
        public BlazorEntryControl Username => new(this, "username-input");
        public BlazorEntryControl Password => new(this, "password-input");
        public BlazorButtonControl LoginButton => new(this, "login-button");
        public BlazorLabelControl ErrorLabel => new(this, "error-label");
        
        public LoginPage(IBlazorTestContext context) : base(context, "LoginPage")
        {
        }
        
        public override bool IsLoaded(int? timeoutMs = null)
        {
            // Wait for Blazor to be ready first
            Context.WaitForBlazorReady(timeoutMs);  // ✅ Direct access!
            return LoginButton.WaitExists(true, timeoutMs);
        }
        
        public void Login(string username, string password)
        {
            Username.Enter(username);
            Password.Enter(password);
            LoginButton.Click();
            
            // Wait for Blazor SignalR to process
            Context.WaitForBlazorReady();  // ✅ Platform-specific!
        }
    }
}
```

---

## 7. Container Examples

### 7.1 MAUI Container with Typed Children

```csharp
namespace MyApp.Maui.UITests.Containers
{
    public class ProductCard : MauiContainerBase
    {
        // Child controls use 'this' (container) as scope
        public MauiLabelControl Name => new(this, "ProductName");
        public MauiLabelControl Price => new(this, "ProductPrice");
        public MauiButtonControl AddToCart => new(this, "AddToCart");
        
        public ProductCard(IMauiElementScope parentScope, Locator locator)
            : base(parentScope, locator)
        {
        }
    }
    
    public class ProductGrid : MauiContainerBase
    {
        public ProductGrid(IMauiElementScope parentScope, Locator locator)
            : base(parentScope, locator) { }
        
        public ProductCard GetCard(int index)
        {
            // Child container uses 'this' as parent scope
            var cardLocator = Locator.ByXPath($"(.//*[@AutomationId='ProductCard'])[{index + 1}]");
            return new ProductCard(this, cardLocator);
        }
        
        public int Count
        {
            get
            {
                // TryFindElement() returns AppiumElement - typed!
                var cards = FindElements(Locator.ByAutomationId("ProductCard"));
                return cards.Count;
            }
        }
    }
}
```

### 7.2 Blazor Container with Typed Children

```csharp
namespace MyApp.Blazor.UITests.Containers
{
    public class ProductCard : BlazorContainerBase
    {
        public BlazorLabelControl Name => new(this, "product-name");
        public BlazorLabelControl Price => new(this, "product-price");
        public BlazorButtonControl AddToCart => new(this, "add-to-cart");
        
        public ProductCard(IBlazorElementScope parentScope, Locator locator)
            : base(parentScope, locator)
        {
        }
        
        public void HighlightCard()
        {
            // Blazor-specific: use JavaScript for visual feedback
            var root = FindElement();  // Returns IWebElement - typed!
            ((IJavaScriptExecutor)Context.Driver).ExecuteScript(
                "arguments[0].style.border = '2px solid red';", root);  // ✅ No casting for Context!
        }
    }
    
    public class ProductGrid : BlazorContainerBase
    {
        public ProductGrid(IBlazorElementScope parentScope, Locator locator)
            : base(parentScope, locator) { }
        
        public ProductCard GetCard(int index)
        {
            var cardLocator = Locator.ByCss($".product-card:nth-child({index + 1})");
            return new ProductCard(this, cardLocator);
        }
    }
}
```

---

## 8. Benefits

### 8.1 Zero Casting

```csharp
// ❌ Before: Casting everywhere
public class MauiButtonControl : ControlBase
{
    public void Click()
    {
        var context = (IMauiTestContext)_context;  // Cast!
        context.HideKeyboard();
    }
}

// ✅ After: Typed throughout via generic inheritance
public class MauiButtonControl : MauiControlBase  // MauiControlBase has typed scope
{
    public void Click()
    {
        Context.HideKeyboard();  // Direct access - Context is IMauiTestContext!
    }
}
```

### 8.2 Single Method Name, Type Varies by Interface

```csharp
// ❌ OLD: Different method names per platform
IMauiElementScope.TryFindMauiElement(locator);  // Returns AppiumElement
IBlazorElementScope.TryFindWebElement(locator);  // Returns IWebElement

// ✅ NEW: Same method name, type narrowed by interface
IMauiElementScope.TryFindElement(locator);   // Returns AppiumElement? (from IElementScope<AppiumElement>)
IBlazorElementScope.TryFindElement(locator); // Returns IWebElement? (from IElementScope<IWebElement>)
```

### 8.3 Compile-Time Safety

```csharp
// ❌ Before: Runtime errors if wrong context
var page = new MauiLoginPage(blazorContext);  // Compiles but fails at runtime

// ✅ After: Compile-time error
var page = new MauiLoginPage(blazorContext);  // Error: Cannot convert IBlazorTestContext to IMauiTestContext
```

### 8.4 IntelliSense/Autocomplete

```csharp
// Platform-specific methods show up in autocomplete
Context.  // Shows: HideKeyboard(), Driver, IsKeyboardShown(), etc. (MAUI)
Context.  // Shows: WaitForBlazorReady(), NavigateTo(), Driver, etc. (Blazor)
```

### 8.5 Consistent API Surface

The generic approach provides consistent method names across platforms:
- `TryFindElement()` - same name on all platforms, return type varies
- `FindElement()` - same name on all platforms
- `FindElements()` - same name on all platforms

---

## 9. Type Flow Summary

```
Test creates context:     IMauiTestContext context = new MauiTestContext(...)
                                │
                          (implements ITestContext<AppiumElement>)
                                │
Test creates page:        var page = new LoginPage(context)
                                │
                          (LoginPage : MauiPageObjectBase : PageObjectBase<AppiumElement, IMauiTestContext>)
                                │
Page stores context:      this._context = context (IMauiTestContext)
                                │
Page IS scope:            page implements IMauiElementScope (via IMauiPageObject)
                                │
Page creates controls:    public MauiButtonControl Submit => new(this, "Submit")
                                │
                          'this' is IMauiElementScope
                                │
Control stores scope:     MauiControlBase._scope = scope (IMauiElementScope)
                                │
Control accesses:         this._scope.Context → IMauiTestContext
                          this.TryFindElement() → AppiumElement?
                                │
                          No casting anywhere! Types flow through generics.
```

---

## 10. Comparison: Old vs New Approach

| Aspect | Old (Parallel Methods) | New (Generic TElement) |
|--------|----------------------|------------------------|
| Method names | `TryFindMauiElement()`, `TryFindWebElement()` | `TryFindElement()` on all |
| Return type | Explicit per method | Via `TElement` parameter |
| Interface definition | Repeat methods per platform | Inherit from `IElementScope<TElement>` |
| Base interface | `object` return types | Typed via generics |
| Code duplication | High (each platform defines methods) | Low (inherit from generic) |
| Adding new platform | Define new typed methods | Just narrow `TElement` |

---

## 11. Migration Path

### Phase 1: Add Generic Interfaces
1. Create `ITestContext<TElement>` extending `ITestContext`
2. Create `IElementScope<TElement>` extending `IElementScope`
3. Create `IPageObject<TElement>` extending `IPageObject` and `IElementScope<TElement>`
4. Create `IContainerControl<TElement>` extending `IControlObject` and `IElementScope<TElement>`

### Phase 2: Update Platform Interfaces
1. `IMauiTestContext : ITestContext<AppiumElement>` 
2. `IMauiElementScope : IElementScope<AppiumElement>`
3. `IMauiPageObject : IPageObject<AppiumElement>, IMauiElementScope`
4. Same for Blazor (IWebElement) and WPF (AutomationElement)

### Phase 3: Create Generic Base Classes
1. `PageObjectBase<TElement, TContext>` with both type parameters
2. `ControlBase<TElement, TScope>` with both type parameters
3. `ContainerBase<TElement, TScope>` with both type parameters

### Phase 4: Create Platform Type Aliases
1. `MauiPageObjectBase : PageObjectBase<AppiumElement, IMauiTestContext>`
2. `MauiControlBase : ControlBase<AppiumElement, IMauiElementScope>`
3. Same for Blazor and WPF

### Phase 5: Update Existing Controls
1. Change from `TryFindMauiElement()` to `TryFindElement()`
2. Remove parallel method implementations
3. Verify all casting is eliminated

---

## 12. Validation Rules

The Platform-Specific Hierarchy pattern is valid when:

- [ ] Generic interfaces use `TElement` parameter only (not `TContext`)
- [ ] `ITestContext<TElement>` provides typed `TryFindElement()` methods
- [ ] `IElementScope<TElement>` provides typed scoped element finding
- [ ] Platform interfaces narrow `TElement` via inheritance (not parallel methods)
- [ ] `IMauiElementScope : IElementScope<AppiumElement>` - inherits typed methods
- [ ] Base classes use two type parameters (`TElement` + `TContext`/`TScope`)
- [ ] Platform type aliases provide simple single-parameter inheritance
- [ ] No `TryFindMauiElement()` or `TryFindWebElement()` methods exist
- [ ] No casting to platform types exists in control or page code
- [ ] Tests use platform-specific context and page types
- [ ] Compile-time errors occur when mixing platform types

---

## Related Documents

- [231_003 Adapter Pattern](231_003_AdapterPattern.spx.md) - Base adapter pattern
- [231_007 Scoped Element Finder](231_007_ScopedElementFinder.spx.md) - Element finding pattern
- [250_009 Platform Contexts](../../250_specifications/250_000_Foundation/250_009_PlatformContexts.spx.md)
- [231_002 Page Object Pattern](231_002_PageObjectPattern.spx.md)
