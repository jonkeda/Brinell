# 211.002 Base Classes Module

**Block Type:** MOD (Module)  
**ID:** 211.002  
**Title:** Base Classes Module Definition  
**Status:** Draft  
**Version:** 1.0

---

## 1. Overview

The Base Classes module contains abstract base class implementations for each platform package. Base classes implement Core interfaces and provide common functionality that concrete controls inherit.

> **Note:** Code snippets in this document are illustrative examples showing the intended patterns and API design. Final implementations may vary in details.

### Module Identity

- **Packages:** `Brinell.MAUI`, `Brinell.Blazor`, `Brinell.WPF`
- **Namespace:** `Brinell.<Platform>.Base`
- **Dependencies:** `Brinell.Core`
- **Consumers:** Concrete control classes

---

## 2. Purpose

The Base Classes module provides:

1. **Code Reuse** — Common implementation shared across controls
2. **Consistent Behavior** — Standard patterns for state, waiting, assertions
3. **Extension Points** — Virtual methods for platform-specific overrides
4. **Reduced Boilerplate** — Controls only implement what's unique

---

## 3. Base Class Hierarchy

Each platform implements a parallel base class hierarchy that mirrors the interface hierarchy. Base classes provide the common implementation while defining abstract methods for platform-specific operations.

```
ControlBase                         # Implements IControlObject
│
├── ClickableControlBase            # Implements IClickableControlObject
│
├── TextControlBase                 # Implements ITextControlObject
│   └── EditableTextControlBase     # Implements IEditableTextControlObject
│
├── ToggleControlBase               # Implements IToggleControlObject
│
├── SelectorControlBase             # Implements ISelectorControlObject
│
├── RangeControlBase                # Implements IRangeControlObject
│
├── ItemsControlBase                # Implements IItemsControlObject
│
├── ContainerControlBase            # Implements IContainerControlObject
│
└── ScrollableControlBase           # Implements IScrollableControlObject
```

---

## 4. ControlBase Implementation

The foundation for all controls. `ControlBase` implements `IControlObject` using the Template Method pattern — it defines the algorithm structure while delegating platform-specific operations to abstract methods.

```csharp
public abstract class ControlBase : IControlObject
{
    protected readonly ITestContext _context;
    protected readonly Locator _locator;
    protected readonly IPageObject? _page;
    
    protected ControlBase(ITestContext context, Locator locator, IPageObject? page = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
        _page = page;
    }
    
    // Properties - identity information
    public Locator Locator => _locator;
    public IPageObject? Page => _page;
    
    // State methods - use template pattern to call abstract FindElement
    public bool IsExists()
    {
        try { return FindElement() != null; }
        catch { return false; }
    }
    
    public bool IsVisible()
    {
        var element = TryFindElement();
        if (element == null) return false;
        return GetElementVisible(element);
    }
    
    public bool IsEnabled()
    {
        var element = TryFindElement();
        if (element == null) return false;
        return GetElementEnabled(element);
    }
    
    // Waiting - nullable expected = skip; uses context timeout
    public bool WaitExists(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;  // Skip-on-null pattern
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        return WaitHelper.WaitFor(() => IsExists() == expected.Value, timeout);
    }
    
    public bool WaitVisible(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;  // Skip-on-null pattern
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        return WaitHelper.WaitFor(() => IsVisible() == expected.Value, timeout);
    }
    
    public bool WaitEnabled(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;  // Skip-on-null pattern
        var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
        return WaitHelper.WaitFor(() => IsEnabled() == expected.Value, timeout);
    }
    
    // Assertions - nullable expected = skip; throws with descriptive message on failure
    public void AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;  // Skip-on-null pattern
        WaitExists(expected, timeoutMs);
        if (IsExists() != expected.Value)
            throw new AssertionException(
                message ?? $"Control '{_locator}' exists={IsExists()}, expected={expected.Value}");
    }
    
    public void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;  // Skip-on-null pattern
        WaitVisible(expected, timeoutMs);
        if (IsVisible() != expected.Value)
            throw new AssertionException(
                message ?? $"Control '{_locator}' visible={IsVisible()}, expected={expected.Value}");
    }
    
    public void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;  // Skip-on-null pattern
        WaitEnabled(expected, timeoutMs);
        if (IsEnabled() != expected.Value)
            throw new AssertionException(
                message ?? $"Control '{_locator}' enabled={IsEnabled()}, expected={expected.Value}");
    }
    
    // Text retrieval with nullable skip pattern for assertions
    public string GetText(int? timeoutMs = null)
    {
        var element = FindElement();
        return GetElementText(element) ?? string.Empty;
    }
    
    public void AssertText(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;  // Skip-on-null pattern
        var actual = GetText(timeoutMs);
        if (actual != expected)
            throw new AssertionException(
                message ?? $"Expected text '{expected}' but was '{actual}'");
    }
    
    public void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;  // Skip-on-null pattern
        var actual = GetText(timeoutMs);
        if (!actual.Contains(expected))
            throw new AssertionException(
                message ?? $"Text '{actual}' does not contain '{expected}'");
    }
    
    public string? GetAttribute(string name)
    {
        var element = TryFindElement();
        return element != null ? GetElementAttribute(element, name) : null;
    }
    
    // Abstract methods - must be implemented by platform-specific classes
    protected abstract object? TryFindElement();           // Find without throwing
    protected abstract object FindElement();               // Find or throw
    protected abstract bool GetElementVisible(object element);  // Check visibility
    protected abstract bool GetElementEnabled(object element);  // Check enabled
    protected abstract string? GetElementText(object element);  // Get text content
    protected abstract string? GetElementAttribute(object element, string name);  // Get attribute
}
```

---

## 5. Capability Base Classes

Each capability interface has a corresponding base class that provides the common implementation. These base classes extend `ControlBase` and implement their respective interfaces.

### 5.1 ClickableControlBase

Provides click actions with logging. Defines abstract methods for the actual click operations which vary by platform.

```csharp
public abstract class ClickableControlBase : ControlBase, IClickableControlObject
{
    protected ClickableControlBase(ITestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public virtual void Click(int? timeoutMs = null)
    {
        var element = FindElement();
        ClickElement(element);
        _context.Logger.LogAction("Click", _locator);
    }
    
    public virtual void DoubleClick(int? timeoutMs = null)
    {
        var element = FindElement();
        DoubleClickElement(element);
        _context.Logger.LogAction("DoubleClick", _locator);
    }
    
    public virtual void RightClick(int? timeoutMs = null)
    {
        var element = FindElement();
        RightClickElement(element);
        _context.Logger.LogAction("RightClick", _locator);
    }
    
    public bool WaitClickable(bool? clickable, int? timeoutMs = null)
    {
        if (clickable is null) return true;  // Skip-on-null pattern
        return WaitHelper.WaitFor(() => IsClickable() == clickable.Value, 
            timeoutMs ?? _context.Timeouts.DefaultWait);
    }
    
    protected virtual bool IsClickable() => IsVisible() && IsEnabled();
    
    // Abstract - platform-specific click
    protected abstract void ClickElement(object element);
    protected abstract void DoubleClickElement(object element);
    protected abstract void RightClickElement(object element);
}
```

### 5.2 TextControlBase

Provides text retrieval and text assertions. This is a read-only text control - text input is handled by `EditableTextControlBase`. Reuses the `GetElementText` abstract method from `ControlBase` for platform-specific text extraction.

```csharp
public abstract class TextControlBase : ControlBase, ITextControlObject
{
    protected TextControlBase(ITestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    // GetText, AssertText, and AssertTextContains are inherited from ControlBase
    
    public bool WaitTextEquals(string? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;  // Skip-on-null pattern
        return WaitHelper.WaitFor(() => GetText() == expected,
            timeoutMs ?? _context.Timeouts.DefaultWait);
    }
    
    public bool WaitTextContains(string? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;  // Skip-on-null pattern
        return WaitHelper.WaitFor(() => GetText().Contains(expected),
            timeoutMs ?? _context.Timeouts.DefaultWait);
    }
}
```

### 5.3 EditableTextControlBase

Extends `TextControlBase` to add text input capabilities. The `SetText` method combines `Clear` and `Enter` for convenience. Platform-specific text input is handled by abstract methods. Uses nullable skip pattern for input methods.

```csharp
public abstract class EditableTextControlBase : TextControlBase, IEditableTextControlObject
{
    protected EditableTextControlBase(ITestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public virtual void Enter(string? text, int? timeoutMs = null)
    {
        if (text is null) return;  // Skip-on-null pattern
        var element = FindElement();
        EnterText(element, text);
        _context.Logger.LogAction("Enter", _locator, text);
    }
    
    public virtual void Clear(int? timeoutMs = null)
    {
        var element = FindElement();
        ClearElement(element);
        _context.Logger.LogAction("Clear", _locator);
    }
    
    public virtual void SetText(string? text, int? timeoutMs = null)
    {
        if (text is null) return;  // Skip-on-null pattern
        Clear(timeoutMs);
        Enter(text, timeoutMs);
    }
    
    // Abstract - platform-specific text input
    protected abstract void EnterText(object element, string text);
    protected abstract void ClearElement(object element);
}
```

### 5.4 ToggleControlBase

Provides toggle state management. The `SetChecked` method uses `IsChecked` to check current state before toggling, avoiding unnecessary operations.

```csharp
public abstract class ToggleControlBase : ControlBase, IToggleControlObject
{
    protected ToggleControlBase(ITestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public abstract bool IsChecked();
    
    public virtual void Toggle(int? timeoutMs = null)
    {
        var element = FindElement();
        ToggleElement(element);
        _context.Logger.LogAction("Toggle", _locator);
    }
    
    public virtual void SetChecked(bool? @checked, int? timeoutMs = null)
    {
        if (@checked is null) return;  // Skip-on-null pattern
        if (IsChecked() != @checked.Value)
            Toggle(timeoutMs);
    }
    
    public virtual void Check(int? timeoutMs = null) => SetChecked(true, timeoutMs);
    public virtual void Uncheck(int? timeoutMs = null) => SetChecked(false, timeoutMs);
    
    public void AssertChecked(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;  // Skip-on-null pattern
        if (IsChecked() != expected.Value)
            throw new AssertionException(
                message ?? $"Control '{_locator}' checked={IsChecked()}, expected={expected.Value}");
    }
    
    protected abstract void ToggleElement(object element);
}
```

### 5.5 SelectorControlBase

Provides selection capabilities with explicit method names for clarity.

```csharp
public abstract class SelectorControlBase : ControlBase, ISelectorControlObject
{
    protected SelectorControlBase(ITestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page) { }
    
    public abstract void SelectByText(string? text, int? timeoutMs = null);
    public abstract void SelectByIndex(int? index, int? timeoutMs = null);
    public abstract string GetSelectedText(int? timeoutMs = null);
    public abstract int GetSelectedIndex(int? timeoutMs = null);
    public abstract IReadOnlyList<string> GetItemTexts(int? timeoutMs = null);
    
    public void AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;  // Skip-on-null pattern
        var actual = GetSelectedText(timeoutMs);
        if (actual != expected)
            throw new AssertionException(
                message ?? $"Expected selected text '{expected}' but was '{actual}'");
    }
    
    public void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;  // Skip-on-null pattern
        var actual = GetSelectedIndex(timeoutMs);
        if (actual != expected.Value)
            throw new AssertionException(
                message ?? $"Expected selected index {expected} but was {actual}");
    }
}
```

---

## 6. Platform-Specific Base Classes

Each platform provides concrete implementations of the abstract methods defined in the common base classes. These classes handle the technology-specific element finding and property access.

### 6.1 MAUI (Appium)

Uses `AppiumElement` and Appium-specific APIs for element interaction. Handles Android and iOS differences internally. The constructor takes `IMauiTestContext` to enable interface-based programming.

```csharp
// Brinell.Maui.Base
public abstract class MauiControlBase : ControlBase
{
    protected readonly IMauiTestContext _mauiContext;
    
    protected MauiControlBase(IMauiTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
        _mauiContext = context;
    }
    
    protected override object? TryFindElement()
    {
        return _mauiContext.TryFindElement(_locator);
    }
    
    protected override object FindElement()
    {
        return _mauiContext.FindElement(_locator);
    }
    
    protected override bool GetElementVisible(object element)
        => ((AppiumElement)element).Displayed;
    
    protected override bool GetElementEnabled(object element)
        => ((AppiumElement)element).Enabled;
    
    protected override string? GetElementText(object element)
        => ((AppiumElement)element).Text;
    
    protected override string? GetElementAttribute(object element, string name)
        => ((AppiumElement)element).GetDomAttribute(name);
}
```

### 6.2 Blazor (Selenium)

Uses `IWebElement` and Selenium WebDriver APIs. Works with any browser supported by Selenium. The constructor takes `IBlazorTestContext` to enable interface-based programming.

```csharp
// Brinell.Blazor.Base
public abstract class BlazorControlBase : ControlBase
{
    protected readonly IBlazorTestContext _blazorContext;
    
    protected BlazorControlBase(IBlazorTestContext context, Locator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
        _blazorContext = context;
    }
    
    protected override object? TryFindElement()
    {
        return _blazorContext.TryFindElement(_locator);
    }
    
    protected override object FindElement()
    {
        return _blazorContext.FindElement(_locator);
    }
    
    protected override bool GetElementVisible(object element)
        => ((IWebElement)element).Displayed;
    
    protected override bool GetElementEnabled(object element)
        => ((IWebElement)element).Enabled;
    
    protected override string? GetElementText(object element)
        => ((IWebElement)element).Text;
    
    protected override string? GetElementAttribute(object element, string name)
        => ((IWebElement)element).GetAttribute(name);
}
```

---

## 7. Extension Points

Base classes provide multiple extension points for customization. Derived classes can override behavior, access internal methods, or hook into action lifecycle.

| Extension Point | Purpose |
|-----------------|---------|
| Virtual methods | Override in derived classes for custom behavior |
| Protected methods | Access internal functionality in subclasses |
| Abstract methods | Force platform-specific implementation |
| Event hooks | BeforeAction, AfterAction for logging/retry |

---

## 8. Validation Rules

The Base Classes module is valid when:

- [ ] Each interface has a corresponding base class
- [ ] Base classes implement all interface methods
- [ ] Common logic is in base classes, not repeated in controls
- [ ] Platform-specific code is in abstract methods
- [ ] Logging is applied consistently to actions
- [ ] Timeouts use context configuration

---

## Related Documents

- [Interfaces Module](211_001_Interfaces.spx.md)
- [Controls Module](211_003_Controls.spx.md)
- [Platform Layer](../203_Layers/203_002_PlatformLayer.spx.md)
- [ADR-004 Control Hierarchy](../202_Decisions/202_004_ControlHierarchy.spx.md)
