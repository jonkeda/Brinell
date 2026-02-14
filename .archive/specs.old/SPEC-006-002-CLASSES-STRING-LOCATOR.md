# SPEC-006-002g: String Locator Support

**Version:** 1.0  
**Status:** Final  
**Date:** January 2026

---

## 1. Overview

Controls can accept a simple `string` as a locator instead of requiring a `ControlLocator`. The default locator strategy is configured at the `IPageObject` level, allowing pages to define their preferred identification approach (e.g., AutomationId, TestId, CSS selector).

---

## 2. IPageObject Default Strategy

```csharp
namespace Brinell.Core;

/// <summary>
/// Page object interface with default locator strategy configuration.
/// </summary>
public interface IPageObject
{
    /// <summary>
    /// The test context for this page.
    /// </summary>
    ITestContext Context { get; }

    /// <summary>
    /// Default locator strategy used when controls are created with string locators.
    /// </summary>
    LocatorStrategy DefaultLocatorStrategy { get; }

    /// <summary>
    /// Creates a ControlLocator from a string using the page's default strategy.
    /// </summary>
    ControlLocator CreateLocator(string locator);

    // Navigation and lifecycle methods...
    void NavigateTo();
    bool IsLoaded(int? timeoutMs = null);
    void WaitForLoad(int? timeoutMs = null);
}
```

---

## 3. PageObjectBase Implementation

```csharp
namespace Brinell.Core;

/// <summary>
/// Base implementation for page objects with configurable default locator strategy.
/// </summary>
public abstract class PageObjectBase : IPageObject
{
    protected readonly ITestContext _context;

    protected PageObjectBase(ITestContext context)
    {
        _context = context;
    }

    public ITestContext Context => _context;

    /// <summary>
    /// Default locator strategy. Override in derived pages to change.
    /// Defaults to AutomationId for MAUI, TestId for Blazor.
    /// </summary>
    public virtual LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;

    /// <summary>
    /// Creates a ControlLocator from a string using the page's default strategy.
    /// </summary>
    public ControlLocator CreateLocator(string locator)
    {
        return new ControlLocator(DefaultLocatorStrategy, locator);
    }

    // Abstract methods for page-specific implementation
    public abstract void NavigateTo();
    public abstract bool IsLoaded(int? timeoutMs = null);
    public abstract void WaitForLoad(int? timeoutMs = null);
}

/// <summary>
/// MAUI page base - defaults to AutomationId strategy.
/// </summary>
public abstract class MauiPageBase : PageObjectBase
{
    protected MauiPageBase(MauiTestContext context) : base(context) { }

    public override LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
}

/// <summary>
/// Blazor page base - defaults to TestId strategy.
/// </summary>
public abstract class BlazorPageBase : PageObjectBase
{
    protected BlazorPageBase(BlazorTestContext context) : base(context) { }

    public override LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.TestId;
}
```

---

## 4. ControlBase String Constructor Support

```csharp
namespace Brinell.Core;

public abstract partial class ControlBase : IControlObject
{
    protected readonly ControlLocator _locator;
    protected readonly IPageObject? _page;
    protected readonly ITestContext _context;

    /// <summary>
    /// Creates control with explicit ControlLocator.
    /// </summary>
    protected ControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
    {
        _locator = locator;
        _page = page;
        _context = context;
    }

    /// <summary>
    /// Creates control with string locator, using page's default strategy.
    /// </summary>
    protected ControlBase(string locator, IPageObject page)
        : this(page.CreateLocator(locator), page, page.Context)
    {
    }

    /// <summary>
    /// Creates control with string locator and explicit strategy.
    /// </summary>
    protected ControlBase(string locator, LocatorStrategy strategy, IPageObject? page, ITestContext context)
        : this(new ControlLocator(strategy, locator), page, context)
    {
    }
}
```

---

## 5. Example Control Classes

### ButtonControl

```csharp
namespace Brinell.Core;

public abstract class ButtonControlBase : ClickableControlBase, IButtonControlObject
{
    // ControlLocator constructor
    protected ButtonControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // String constructor using page default
    protected ButtonControlBase(string locator, IPageObject page)
        : base(locator, page) { }

    // String with explicit strategy
    protected ButtonControlBase(string locator, LocatorStrategy strategy, IPageObject? page, ITestContext context)
        : base(locator, strategy, page, context) { }

    public abstract string? GetLabel(int? timeoutMs = null);
}
```

### MAUI Button

```csharp
namespace Brinell.Maui;

public class MauiButton : MauiClickableControlBase, IButtonControlObject
{
    // ControlLocator constructor
    public MauiButton(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // String constructor using page default (most common usage)
    public MauiButton(string locator, IPageObject page)
        : base(locator, page) { }

    // String with explicit strategy
    public MauiButton(string locator, LocatorStrategy strategy, IPageObject? page, MauiTestContext context)
        : base(locator, strategy, page, context) { }

    public string? GetLabel(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs) as AppiumElement;
        return element?.Text;
    }
}
```

### Blazor Button

```csharp
namespace Brinell.Blazor;

public class BlazorButton : BlazorClickableControlBase, IButtonControlObject
{
    // ControlLocator constructor
    public BlazorButton(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // String constructor using page default (most common usage)
    public BlazorButton(string locator, IPageObject page)
        : base(locator, page) { }

    // String with explicit strategy
    public BlazorButton(string locator, LocatorStrategy strategy, IPageObject? page, BlazorTestContext context)
        : base(locator, strategy, page, context) { }

    public string? GetLabel(int? timeoutMs = null)
    {
        var pwLocator = GetPlaywrightLocator(timeoutMs);
        return pwLocator.TextContentAsync().GetAwaiter().GetResult();
    }
}
```

### TextControl Example

```csharp
namespace Brinell.Maui;

public class MauiEntry : MauiTextControlBase, IEditableTextControlObject
{
    public MauiEntry(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    public MauiEntry(string locator, IPageObject page)
        : base(locator, page) { }

    public MauiEntry(string locator, LocatorStrategy strategy, IPageObject? page, MauiTestContext context)
        : base(locator, strategy, page, context) { }

    // ... implementation
}
```

---

## 6. Usage Examples

### Clean Page Object with String Locators

```csharp
public class LoginPage : MauiPageBase
{
    // Controls use simple strings - page default is AutomationId
    public MauiEntry Username => new("UsernameEntry", this);
    public MauiEntry Password => new("PasswordEntry", this);
    public MauiButton LoginButton => new("LoginButton", this);
    public MauiLabel ErrorMessage => new("ErrorLabel", this);

    public LoginPage(MauiTestContext context) : base(context) { }

    public override void NavigateTo() => _context.LaunchApp();
    public override bool IsLoaded(int? timeoutMs = null) => LoginButton.IsVisible(timeoutMs);
    public override void WaitForLoad(int? timeoutMs = null) => LoginButton.WaitVisible(true, timeoutMs);
}
```

### Override Default Strategy Per Page

```csharp
public class LegacyPage : MauiPageBase
{
    // This page uses XPath instead of AutomationId
    public override LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.XPath;

    public MauiButton Submit => new("//button[@name='submit']", this);
    public MauiEntry Input => new("//input[@type='text']", this);

    public LegacyPage(MauiTestContext context) : base(context) { }
}
```

### Mix String and Explicit Locators

```csharp
public class MixedPage : MauiPageBase
{
    // Uses page default (AutomationId)
    public MauiButton SaveButton => new("SaveBtn", this);

    // Explicit CSS selector for complex case
    public MauiLabel Status => new(By.Css(".status-indicator > span"), this, _context);

    // Explicit XPath for accessibility
    public MauiButton Cancel => new(By.AccessibilityId("CancelAction"), this, _context);

    public MixedPage(MauiTestContext context) : base(context) { }
}
```

### Blazor Page with TestId Default

```csharp
public class DashboardPage : BlazorPageBase
{
    // Controls use simple strings - page default is TestId
    public BlazorButton RefreshButton => new("refresh-btn", this);
    public BlazorLabel UserName => new("user-name", this);
    public BlazorSelect FilterDropdown => new("filter-select", this);

    public DashboardPage(BlazorTestContext context) : base(context) { }

    public override void NavigateTo() => _context.Page.GotoAsync("/dashboard").Wait();
    public override bool IsLoaded(int? timeoutMs = null) => RefreshButton.IsVisible(timeoutMs);
    public override void WaitForLoad(int? timeoutMs = null) => RefreshButton.WaitVisible(true, timeoutMs);
}
```

---

## 7. Constructor Priority

When creating controls, use this priority:

| Constructor | When to Use |
|-------------|-------------|
| `new Control("locator", page)` | **Most common** - uses page default strategy |
| `new Control(By.Strategy("locator"), page, context)` | Explicit strategy needed |
| `new Control("locator", LocatorStrategy.XPath, page, context)` | Alternative explicit syntax |

---

**End of Document**
