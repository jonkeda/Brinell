# specification MauiMinimalClasses

- **id**: SPC-202
- **version**: 1.0
- **created**: January 13, 2026
- **status**: Draft
- **level**: 0 - Foundation
- **requirement**: FR-100, FR-103

---

## Overview

This specification defines the minimal set of base classes and concrete controls needed to support MAUI Button and Entry controls. The class hierarchy is optimized for MAUI/Appium and implements the interfaces defined in [250_201_MauiMinimalInterfaces](250_201_MauiMinimalInterfaces.spx.md).

---

## Class Summary

| Class | Base | Implements | Purpose |
|-------|------|------------|---------|
| `MauiControlBase` | — | `IControlObject` | Base for all MAUI controls |
| `MauiClickableControlBase` | `MauiControlBase` | `IClickableControl` | Base for clickable controls |
| `MauiTextControlBase` | `MauiControlBase` | `ITextControl` | Base for text display controls |
| `MauiEditableTextControlBase` | `MauiTextControlBase` | `IEditableTextControl` | Base for text input controls |
| `MauiContainerBase` | `MauiControlBase` | `IMauiContainerControl` | Base for containers/views |
| `MauiButtonControl` | `MauiClickableControlBase` | `IClickableControl` | Button control |
| `MauiEntryControl` | `MauiEditableTextControlBase` | `IEditableTextControl` | Entry control |
| `MauiPageObjectBase` | — | `IMauiPageObject` | Base for page objects |

---

## 1. MauiControlBase

Base class for ALL MAUI controls. Implements `IControlObject` with AppiumElement-specific behavior.

```csharp
namespace Brinell.Maui.Controls
{
    /// <summary>
    /// Base class for all MAUI controls.
    /// TScope is constrained to IMauiElementScope for typed element access.
    /// </summary>
    public abstract class MauiControlBase : IControlObject
    {
        protected readonly IMauiElementScope _scope;
        protected readonly Locator _locator;
        
        // ─── Constructors ────────────────────────────────────────────────
        
        protected MauiControlBase(IMauiElementScope scope, Locator locator)
        {
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
        }
        
        protected MauiControlBase(IMauiElementScope scope, string automationId)
            : this(scope, Locator.ByAutomationId(automationId))
        {
        }
        
        // ─── Identity (IControlObject) ───────────────────────────────────
        
        public Locator Locator => _locator;
        public IElementScope Scope => _scope;
        
        public IPageObject? Page => _scope switch
        {
            IPageObject page => page,
            IControlObject control => control.Page,
            _ => null
        };
        
        // ─── Context Access ──────────────────────────────────────────────
        
        protected IMauiTestContext Context => _scope.Context;
        protected TimeoutSettings Timeouts => Context.Timeouts;
        protected ITestLogger Logger => Context.Logger;
        
        // ─── Element Finding ─────────────────────────────────────────────
        
        /// <summary>
        /// Try to find the underlying element via scope.
        /// </summary>
        protected AppiumElement? TryFindElement() => _scope.TryFindElement(_locator);
        
        /// <summary>
        /// Find the underlying element via scope. Throws if not found.
        /// </summary>
        protected AppiumElement FindElement() => _scope.FindElement(_locator);
        
        // ─── State Methods ───────────────────────────────────────────────
        
        public virtual bool IsExists()
        {
            return TryFindElement() != null;
        }
        
        public virtual bool? IsVisible()
        {
            var element = TryFindElement();
            return element?.Displayed;
        }
        
        public virtual bool? IsEnabled()
        {
            var element = TryFindElement();
            return element?.Enabled;
        }
        
        // ─── Wait Methods ────────────────────────────────────────────────
        
        public bool WaitExists(bool? expected, int? timeoutMs = null)
        {
            if (expected is null) return true; // Nullable skip pattern
            
            var timeout = timeoutMs ?? Timeouts.DefaultWait;
            return WaitHelper.WaitFor(() => IsExists() == expected.Value, timeout, Timeouts.PollingInterval);
        }
        
        public bool WaitVisible(bool? expected, int? timeoutMs = null)
        {
            if (expected is null) return true;
            
            var timeout = timeoutMs ?? Timeouts.DefaultWait;
            return WaitHelper.WaitFor(() => IsVisible() == expected.Value, timeout, Timeouts.PollingInterval);
        }
        
        public bool WaitEnabled(bool? expected, int? timeoutMs = null)
        {
            if (expected is null) return true;
            
            var timeout = timeoutMs ?? Timeouts.DefaultWait;
            return WaitHelper.WaitFor(() => IsEnabled() == expected.Value, timeout, Timeouts.PollingInterval);
        }
        
        // ─── Assert Methods ──────────────────────────────────────────────
        
        public void AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
        {
            if (expected is null) return; // Nullable skip pattern
            
            WaitExists(expected, timeoutMs);
            var actual = IsExists();
            if (actual != expected.Value)
            {
                throw new AssertionException(
                    message ?? $"Control {_locator} exists={actual}, expected={expected.Value}");
            }
        }
        
        public void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)
        {
            if (expected is null) return;
            
            WaitVisible(expected, timeoutMs);
            var actual = IsVisible();
            if (actual != expected.Value)
            {
                throw new AssertionException(
                    message ?? $"Control {_locator} visible={actual}, expected={expected.Value}");
            }
        }
        
        public void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null)
        {
            if (expected is null) return;
            
            WaitEnabled(expected, timeoutMs);
            var actual = IsEnabled();
            if (actual != expected.Value)
            {
                throw new AssertionException(
                    message ?? $"Control {_locator} enabled={actual}, expected={expected.Value}");
            }
        }
        
        // ─── Text Methods ────────────────────────────────────────────────
        
        public virtual string? GetText(int? timeoutMs = null)
        {
            if (timeoutMs.HasValue)
            {
                WaitExists(true, timeoutMs);
            }
            
            var element = TryFindElement();
            return element?.Text;
        }
        
        public bool WaitText(string? expected, int? timeoutMs = null)
        {
            if (expected is null) return true;
            
            var timeout = timeoutMs ?? Timeouts.DefaultWait;
            return WaitHelper.WaitFor(() => GetText() == expected, timeout, Timeouts.PollingInterval);
        }
        
        public void AssertText(string? expected, string? message = null, int? timeoutMs = null)
        {
            if (expected is null) return;
            
            WaitText(expected, timeoutMs);
            var actual = GetText();
            if (actual != expected)
            {
                throw new AssertionException(
                    message ?? $"Control {_locator} text='{actual}', expected='{expected}'");
            }
        }
        
        public void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)
        {
            if (expected is null) return;
            
            var timeout = timeoutMs ?? Timeouts.DefaultWait;
            WaitHelper.WaitFor(() => GetText()?.Contains(expected) == true, timeout, Timeouts.PollingInterval);
            
            var actual = GetText();
            if (actual?.Contains(expected) != true)
            {
                throw new AssertionException(
                    message ?? $"Control {_locator} text='{actual}' does not contain '{expected}'");
            }
        }
        
        // ─── Attribute Methods ───────────────────────────────────────────
        
        public string? GetAttribute(string name)
        {
            var element = TryFindElement();
            return element?.GetAttribute(name);
        }
        
        // ─── Logging Helpers ─────────────────────────────────────────────
        
        protected void LogAction(string action, string? value = null)
        {
            var testName = TestContext.CurrentContext?.Test?.Name ?? "Unknown";
            Logger.LogAction(testName, Page?.Name, _locator.ToString(), action, value);
        }
        
        protected void LogAssert(string assertion, object? expected, object? actual, bool passed)
        {
            var testName = TestContext.CurrentContext?.Test?.Name ?? "Unknown";
            Logger.LogAssert(testName, Page?.Name, _locator.ToString(), assertion, expected, actual, passed);
        }
    }
}
```

---

## 2. MauiClickableControlBase

Base class for controls that support click actions.

```csharp
namespace Brinell.Maui.Controls
{
    /// <summary>
    /// Base class for clickable MAUI controls.
    /// </summary>
    public abstract class MauiClickableControlBase : MauiControlBase, IClickableControl
    {
        protected MauiClickableControlBase(IMauiElementScope scope, Locator locator)
            : base(scope, locator) { }
        
        protected MauiClickableControlBase(IMauiElementScope scope, string automationId)
            : base(scope, automationId) { }
        
        /// <summary>
        /// Check if element is clickable (visible AND enabled).
        /// </summary>
        public bool? IsClickable()
        {
            var element = TryFindElement();
            if (element == null) return null;
            return element.Displayed && element.Enabled;
        }
        
        /// <summary>
        /// Wait for element to be clickable.
        /// </summary>
        protected bool WaitClickable(int? timeoutMs = null)
        {
            var timeout = timeoutMs ?? Timeouts.DefaultWait;
            return WaitHelper.WaitFor(() => IsClickable() == true, timeout, Timeouts.PollingInterval);
        }
        
        // ─── IClickableControl ───────────────────────────────────────────
        
        public virtual void Click()
        {
            WaitClickable();
            var element = FindElement();
            LogAction("Click");
            element.Click();
        }
        
        public virtual void DoubleClick()
        {
            WaitClickable();
            var element = FindElement();
            LogAction("DoubleClick");
            
            // Appium doesn't have native double-click, simulate with two clicks
            element.Click();
            element.Click();
        }
    }
}
```

---

## 3. MauiTextControlBase

Base class for controls that display text (read-only).

```csharp
namespace Brinell.Maui.Controls
{
    /// <summary>
    /// Base class for text display controls (Label, etc.).
    /// </summary>
    public abstract class MauiTextControlBase : MauiControlBase, ITextControl
    {
        protected MauiTextControlBase(IMauiElementScope scope, Locator locator)
            : base(scope, locator) { }
        
        protected MauiTextControlBase(IMauiElementScope scope, string automationId)
            : base(scope, automationId) { }
        
        // ─── ITextControl ────────────────────────────────────────────────
        
        public void AssertTextMatches(string pattern, string? message = null, int? timeoutMs = null)
        {
            var timeout = timeoutMs ?? Timeouts.DefaultWait;
            var regex = new Regex(pattern);
            
            WaitHelper.WaitFor(() => 
            {
                var text = GetText();
                return text != null && regex.IsMatch(text);
            }, timeout, Timeouts.PollingInterval);
            
            var actual = GetText();
            if (actual == null || !regex.IsMatch(actual))
            {
                throw new AssertionException(
                    message ?? $"Control {_locator} text='{actual}' does not match pattern '{pattern}'");
            }
        }
    }
}
```

---

## 4. MauiEditableTextControlBase

Base class for controls that accept text input.

```csharp
namespace Brinell.Maui.Controls
{
    /// <summary>
    /// Base class for text input controls (Entry, Editor, SearchBar).
    /// </summary>
    public abstract class MauiEditableTextControlBase : MauiTextControlBase, IEditableTextControl
    {
        protected MauiEditableTextControlBase(IMauiElementScope scope, Locator locator)
            : base(scope, locator) { }
        
        protected MauiEditableTextControlBase(IMauiElementScope scope, string automationId)
            : base(scope, automationId) { }
        
        /// <summary>
        /// Check if control accepts input (visible, enabled, not read-only).
        /// </summary>
        protected bool? IsEditable()
        {
            var element = TryFindElement();
            if (element == null) return null;
            return element.Displayed && element.Enabled;
        }
        
        /// <summary>
        /// Wait for control to be editable.
        /// </summary>
        protected bool WaitEditable(int? timeoutMs = null)
        {
            var timeout = timeoutMs ?? Timeouts.DefaultWait;
            return WaitHelper.WaitFor(() => IsEditable() == true, timeout, Timeouts.PollingInterval);
        }
        
        // ─── IEditableTextControl ────────────────────────────────────────
        
        public virtual void Enter(string? text)
        {
            if (text is null) return; // Nullable skip pattern
            
            WaitEditable();
            var element = FindElement();
            LogAction("Enter", text);
            element.SendKeys(text);
        }
        
        public virtual void Clear()
        {
            WaitEditable();
            var element = FindElement();
            LogAction("Clear");
            element.Clear();
        }
        
        public virtual void SetText(string? text)
        {
            if (text is null) return; // Nullable skip pattern
            
            LogAction("SetText", text);
            Clear();
            if (!string.IsNullOrEmpty(text))
            {
                Enter(text);
            }
        }
    }
}
```

---

## 5. MauiContainerBase

Base class for container controls (views, panels, forms).

```csharp
namespace Brinell.Maui.Controls
{
    /// <summary>
    /// Base class for container controls that scope child element searches.
    /// Use for: Cards, Panels, Forms, Sections, custom views.
    /// </summary>
    public abstract class MauiContainerBase : MauiControlBase, IMauiContainerControl
    {
        private AppiumElement? _containerRoot;
        
        protected MauiContainerBase(IMauiElementScope scope, Locator locator)
            : base(scope, locator) { }
        
        protected MauiContainerBase(IMauiElementScope scope, string automationId)
            : base(scope, automationId) { }
        
        // ─── IMauiContainerControl ───────────────────────────────────────
        
        /// <summary>
        /// Cached container root element.
        /// </summary>
        public AppiumElement ContainerRoot => _containerRoot ??= FindElement();
        
        object IContainerControl.ContainerRoot => ContainerRoot;
        
        // ─── IMauiElementScope ───────────────────────────────────────────
        
        IMauiTestContext IMauiElementScope.Context => Context;
        
        LocatorStrategy IElementScope.DefaultLocatorStrategy => LocatorStrategy.AutomationId;
        
        // ─── IElementScope<AppiumElement> ────────────────────────────────
        
        /// <summary>
        /// Find element within this container's bounds.
        /// </summary>
        public AppiumElement? TryFindElement(Locator locator)
        {
            try
            {
                var root = ContainerRoot;
                return root.FindElement(locator.ToAppiumBy());
            }
            catch (NoSuchElementException)
            {
                return null;
            }
        }
        
        public AppiumElement FindElement(Locator locator)
        {
            var element = TryFindElement(locator);
            if (element == null)
            {
                throw new ElementNotFoundException(
                    $"Element not found within container {_locator}: {locator}");
            }
            return element;
        }
        
        public IReadOnlyList<AppiumElement> FindElements(Locator locator)
        {
            try
            {
                var root = ContainerRoot;
                return root.FindElements(locator.ToAppiumBy()).ToList();
            }
            catch (NoSuchElementException)
            {
                return Array.Empty<AppiumElement>();
            }
        }
        
        /// <summary>
        /// Invalidate cached container root (use after UI refresh).
        /// </summary>
        public void InvalidateCache()
        {
            _containerRoot = null;
        }
    }
}
```

---

## 6. Concrete Controls

### 6.1 MauiButtonControl

```csharp
namespace Brinell.Maui.Controls
{
    /// <summary>
    /// MAUI Button control.
    /// Maps to: Button, ImageButton
    /// </summary>
    public class MauiButtonControl : MauiClickableControlBase
    {
        public MauiButtonControl(IMauiElementScope scope, Locator locator)
            : base(scope, locator) { }
        
        public MauiButtonControl(IMauiElementScope scope, string automationId)
            : base(scope, automationId) { }
    }
}
```

### 6.2 MauiEntryControl

```csharp
namespace Brinell.Maui.Controls
{
    /// <summary>
    /// MAUI Entry control (single-line text input).
    /// Maps to: Entry, SearchBar (text input part)
    /// </summary>
    public class MauiEntryControl : MauiEditableTextControlBase
    {
        public MauiEntryControl(IMauiElementScope scope, Locator locator)
            : base(scope, locator) { }
        
        public MauiEntryControl(IMauiElementScope scope, string automationId)
            : base(scope, automationId) { }
        
        /// <summary>
        /// Get placeholder text (if any).
        /// </summary>
        public string? GetPlaceholder()
        {
            return GetAttribute("Placeholder") ?? GetAttribute("hint");
        }
    }
}
```

---

## 7. MauiPageObjectBase

Base class for page objects.

```csharp
namespace Brinell.Maui.Pages
{
    /// <summary>
    /// Base class for MAUI page objects.
    /// </summary>
    public abstract class MauiPageObjectBase : IMauiPageObject
    {
        protected readonly IMauiTestContext _context;
        
        protected MauiPageObjectBase(IMauiTestContext context, string name)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
        
        // ─── IPageObject ─────────────────────────────────────────────────
        
        public string Name { get; }
        
        public virtual bool IsLoaded(int? timeoutMs = null) => true;
        
        public bool WaitLoaded(bool? expected, int? timeoutMs = null)
        {
            if (expected is null) return true;
            
            var timeout = timeoutMs ?? _context.Timeouts.PageLoad;
            return WaitHelper.WaitFor(() => IsLoaded() == expected.Value, timeout, _context.Timeouts.PollingInterval);
        }
        
        public void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null)
        {
            if (expected is null) return;
            
            WaitLoaded(expected, timeoutMs);
            var actual = IsLoaded();
            if (actual != expected.Value)
            {
                throw new AssertionException(
                    message ?? $"Page '{Name}' loaded={actual}, expected={expected.Value}");
            }
        }
        
        public virtual string? GetTitle(int? timeoutMs = null)
        {
            // Platform-specific: may need to find title bar element
            return null;
        }
        
        public void TakeScreenshot(string? filename = null, int? timeoutMs = null)
        {
            var name = filename ?? $"{Name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            _context.SaveScreenshot(name);
        }
        
        // ─── IMauiElementScope ───────────────────────────────────────────
        
        IMauiTestContext IMauiElementScope.Context => _context;
        
        public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
        
        // ─── IElementScope<AppiumElement> ────────────────────────────────
        
        /// <summary>
        /// Page delegates to context (driver root).
        /// </summary>
        public AppiumElement? TryFindElement(Locator locator) 
            => _context.TryFindElement(locator);
        
        public AppiumElement FindElement(Locator locator) 
            => _context.FindElement(locator);
        
        public IReadOnlyList<AppiumElement> FindElements(Locator locator) 
            => _context.FindElements(locator);
    }
}
```

---

## 8. Supporting Types

### 8.1 Locator

```csharp
namespace Brinell.Core
{
    public enum LocatorStrategy
    {
        AutomationId,
        Name,
        ClassName,
        XPath,
        AccessibilityId,
        DataTestId,  // For web/Blazor
        CssSelector  // For web/Blazor
    }
    
    public class Locator
    {
        public LocatorStrategy Strategy { get; }
        public string Value { get; }
        
        private Locator(LocatorStrategy strategy, string value)
        {
            Strategy = strategy;
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
        
        public static Locator ByAutomationId(string id) => new(LocatorStrategy.AutomationId, id);
        public static Locator ByName(string name) => new(LocatorStrategy.Name, name);
        public static Locator ByXPath(string xpath) => new(LocatorStrategy.XPath, xpath);
        public static Locator ByAccessibilityId(string id) => new(LocatorStrategy.AccessibilityId, id);
        public static Locator ByDataTestId(string id) => new(LocatorStrategy.DataTestId, id);
        
        public override string ToString() => $"{Strategy}:{Value}";
        
        // Extension method in MAUI package
        // public By ToAppiumBy() { ... }
    }
}
```

### 8.2 WaitHelper

```csharp
namespace Brinell.Core.Helpers
{
    public static class WaitHelper
    {
        public static bool WaitFor(Func<bool> condition, int timeoutMs, int pollingIntervalMs = 100)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                try
                {
                    if (condition())
                        return true;
                }
                catch
                {
                    // Ignore exceptions during polling
                }
                
                Thread.Sleep(pollingIntervalMs);
            }
            
            // Final check
            return condition();
        }
    }
}
```

---

## 9. Class Diagram

```
MauiControlBase (IControlObject)
├── MauiClickableControlBase (IClickableControl)
│   └── MauiButtonControl
│
├── MauiTextControlBase (ITextControl)
│   └── MauiEditableTextControlBase (IEditableTextControl)
│       └── MauiEntryControl
│
└── MauiContainerBase (IMauiContainerControl, IMauiElementScope)
        └── Custom views (AddressForm, ProductCard, etc.)

MauiPageObjectBase (IMauiPageObject, IMauiElementScope)
    └── Custom pages (LoginPage, HomePage, etc.)
```

---

## 10. Acceptance Criteria

### ACC-001: Button Click

```gherkin
Given a visible enabled button
When Click() is called
Then the button's click event is triggered
And the action is logged

Given a hidden button
When Click() is called
Then it waits for visibility
And clicks when visible
```

### ACC-002: Entry Text Input

```gherkin
Given an empty entry
When Enter("Hello") is called
Then GetText() returns "Hello"

Given an entry with existing text
When SetText("New") is called
Then GetText() returns "New"

Given a disabled entry
When Enter("Text") is called
Then no text is entered
And no exception is thrown
```

### ACC-003: Container Scoping

```gherkin
Given a MauiContainerBase with child controls
When a child control calls TryFindElement
Then the search is scoped to the container's root element
```

### ACC-004: Nullable Skip Pattern

```gherkin
Given any control
When Enter(null) is called
Then no action is performed

When AssertText(null, ...) is called
Then no assertion is performed
```

---

## Related Documents

- [250_201_MauiMinimalInterfaces](250_201_MauiMinimalInterfaces.spx.md) - Interface definitions
- [250_203_MauiMinimalScope](250_203_MauiMinimalScope.spx.md) - Scoping patterns
- [250_001_IControlObject](../250_000_Foundation/250_001_IControlObject.spx.md) - Full interface spec
