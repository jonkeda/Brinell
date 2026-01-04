# SPEC-006-002a: Locator Classes

**Version:** 1.0  
**Status:** Final  
**Date:** January 2026

---

## 1. LocatorStrategy Enum

```csharp
namespace Brinell.Core;

/// <summary>
/// Defines the strategy used to locate a control element.
/// </summary>
public enum LocatorStrategy
{
    /// <summary>AutomationId / data-testid attribute.</summary>
    AutomationId,
    
    /// <summary>Element name attribute.</summary>
    Name,
    
    /// <summary>CSS class name.</summary>
    ClassName,
    
    /// <summary>Element tag name (HTML) or control type (native).</summary>
    TagName,
    
    /// <summary>XPath expression.</summary>
    XPath,
    
    /// <summary>CSS selector (Blazor/web only).</summary>
    CssSelector,
    
    /// <summary>Accessible name / aria-label.</summary>
    AccessibleName,
    
    /// <summary>Text content of the element.</summary>
    Text,
    
    /// <summary>Partial text content match.</summary>
    PartialText,
    
    /// <summary>Index within parent container.</summary>
    Index,
    
    /// <summary>Platform-specific native locator.</summary>
    Native
}
```

---

## 2. ControlLocator Class

```csharp
namespace Brinell.Core;

/// <summary>
/// Represents a control locator with strategy and value.
/// Immutable value type for element identification.
/// </summary>
public sealed class ControlLocator : IEquatable<ControlLocator>
{
    public LocatorStrategy Strategy { get; }
    public string Value { get; }
    public ControlLocator? Parent { get; }

    public ControlLocator(LocatorStrategy strategy, string value, ControlLocator? parent = null)
    {
        Strategy = strategy;
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Parent = parent;
    }

    /// <summary>
    /// Creates a child locator scoped to this locator as parent.
    /// </summary>
    public ControlLocator Child(LocatorStrategy strategy, string value)
    {
        return new ControlLocator(strategy, value, this);
    }

    /// <summary>
    /// Creates a child locator by AutomationId scoped to this locator.
    /// </summary>
    public ControlLocator Child(string automationId)
    {
        return new ControlLocator(LocatorStrategy.AutomationId, automationId, this);
    }

    #region Equality

    public bool Equals(ControlLocator? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Strategy == other.Strategy 
            && Value == other.Value 
            && Equals(Parent, other.Parent);
    }

    public override bool Equals(object? obj) => Equals(obj as ControlLocator);

    public override int GetHashCode() => HashCode.Combine(Strategy, Value, Parent);

    public static bool operator ==(ControlLocator? left, ControlLocator? right)
        => Equals(left, right);

    public static bool operator !=(ControlLocator? left, ControlLocator? right)
        => !Equals(left, right);

    #endregion

    public override string ToString()
    {
        var locatorStr = $"{Strategy}='{Value}'";
        return Parent != null ? $"{Parent} > {locatorStr}" : locatorStr;
    }
}
```

---

## 3. By Static Factory

```csharp
namespace Brinell.Core;

/// <summary>
/// Static factory for creating ControlLocator instances.
/// Provides a fluent API for element location.
/// </summary>
public static class By
{
    /// <summary>
    /// Locate by AutomationId (recommended for test automation).
    /// </summary>
    public static ControlLocator AutomationId(string id)
        => new(LocatorStrategy.AutomationId, id);

    /// <summary>
    /// Locate by element name attribute.
    /// </summary>
    public static ControlLocator Name(string name)
        => new(LocatorStrategy.Name, name);

    /// <summary>
    /// Locate by CSS class name.
    /// </summary>
    public static ControlLocator ClassName(string className)
        => new(LocatorStrategy.ClassName, className);

    /// <summary>
    /// Locate by tag name (HTML) or control type (native).
    /// </summary>
    public static ControlLocator TagName(string tagName)
        => new(LocatorStrategy.TagName, tagName);

    /// <summary>
    /// Locate by XPath expression.
    /// </summary>
    public static ControlLocator XPath(string xpath)
        => new(LocatorStrategy.XPath, xpath);

    /// <summary>
    /// Locate by CSS selector (Blazor/web only).
    /// </summary>
    public static ControlLocator Css(string selector)
        => new(LocatorStrategy.CssSelector, selector);

    /// <summary>
    /// Locate by accessible name / aria-label.
    /// </summary>
    public static ControlLocator AccessibleName(string name)
        => new(LocatorStrategy.AccessibleName, name);

    /// <summary>
    /// Locate by exact text content.
    /// </summary>
    public static ControlLocator Text(string text)
        => new(LocatorStrategy.Text, text);

    /// <summary>
    /// Locate by partial text content.
    /// </summary>
    public static ControlLocator PartialText(string text)
        => new(LocatorStrategy.PartialText, text);

    /// <summary>
    /// Locate by index within parent container.
    /// </summary>
    public static ControlLocator Index(int index)
        => new(LocatorStrategy.Index, index.ToString());

    /// <summary>
    /// Locate using platform-specific native locator.
    /// </summary>
    public static ControlLocator Native(string locator)
        => new(LocatorStrategy.Native, locator);
}
```

---

## 4. IPlatformLocatorResolver Interface

```csharp
namespace Brinell.Core;

/// <summary>
/// Resolves ControlLocator to platform-specific locator format.
/// </summary>
public interface IPlatformLocatorResolver
{
    /// <summary>
    /// Resolves a ControlLocator to the platform-specific locator type.
    /// </summary>
    object Resolve(ControlLocator locator);

    /// <summary>
    /// Gets the platform name this resolver supports.
    /// </summary>
    string PlatformName { get; }
}
```

---

## 5. MAUI Locator Resolver

```csharp
namespace Brinell.Maui;

using OpenQA.Selenium;

/// <summary>
/// Resolves ControlLocator to Appium/Selenium By locators for MAUI.
/// </summary>
public class MauiLocatorResolver : IPlatformLocatorResolver
{
    public string PlatformName => "MAUI";

    // Full implementation for Resolve
    public object Resolve(ControlLocator locator)
    {
        // Handle parent chain first
        if (locator.Parent != null)
        {
            var parentBy = Resolve(locator.Parent) as OpenQA.Selenium.By;
            var childBy = ResolveCore(locator);
            // Return composite for chained locators
            return new ChainedLocator(parentBy!, childBy);
        }
        
        return ResolveCore(locator);
    }

    private OpenQA.Selenium.By ResolveCore(ControlLocator locator)
    {
        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId => MobileBy.AccessibilityId(locator.Value),
            LocatorStrategy.Name => OpenQA.Selenium.By.Name(locator.Value),
            LocatorStrategy.ClassName => OpenQA.Selenium.By.ClassName(locator.Value),
            LocatorStrategy.TagName => OpenQA.Selenium.By.TagName(locator.Value),
            LocatorStrategy.XPath => OpenQA.Selenium.By.XPath(locator.Value),
            LocatorStrategy.AccessibleName => MobileBy.AccessibilityId(locator.Value),
            LocatorStrategy.Text => OpenQA.Selenium.By.XPath($"//*[@text='{locator.Value}']"),
            LocatorStrategy.PartialText => OpenQA.Selenium.By.XPath($"//*[contains(@text, '{locator.Value}')]"),
            LocatorStrategy.Index => OpenQA.Selenium.By.XPath($"(./*)[{int.Parse(locator.Value) + 1}]"),
            LocatorStrategy.Native => OpenQA.Selenium.By.XPath(locator.Value),
            LocatorStrategy.CssSelector => throw new NotSupportedException("CSS selectors not supported on MAUI/native."),
            _ => throw new ArgumentOutOfRangeException(nameof(locator.Strategy))
        };
    }
}

/// <summary>
/// Represents a chained parent-child locator for MAUI.
/// </summary>
public class ChainedLocator
{
    public OpenQA.Selenium.By Parent { get; }
    public OpenQA.Selenium.By Child { get; }

    public ChainedLocator(OpenQA.Selenium.By parent, OpenQA.Selenium.By child)
    {
        Parent = parent;
        Child = child;
    }
}
```

---

## 6. Blazor Locator Resolver

```csharp
namespace Brinell.Blazor;

using Microsoft.Playwright;

/// <summary>
/// Resolves ControlLocator to Playwright locator strings for Blazor.
/// </summary>
public class BlazorLocatorResolver : IPlatformLocatorResolver
{
    public string PlatformName => "Blazor";

    // Full implementation for Resolve
    public object Resolve(ControlLocator locator)
    {
        // Build full selector including parent chain
        var selector = BuildSelector(locator);
        return selector;
    }

    private string BuildSelector(ControlLocator locator)
    {
        var current = ResolveCore(locator);
        
        if (locator.Parent != null)
        {
            var parentSelector = BuildSelector(locator.Parent);
            return $"{parentSelector} {current}";
        }
        
        return current;
    }

    private string ResolveCore(ControlLocator locator)
    {
        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId => $"[data-testid='{locator.Value}']",
            LocatorStrategy.Name => $"[name='{locator.Value}']",
            LocatorStrategy.ClassName => $".{locator.Value}",
            LocatorStrategy.TagName => locator.Value,
            LocatorStrategy.XPath => $"xpath={locator.Value}",
            LocatorStrategy.CssSelector => locator.Value,
            LocatorStrategy.AccessibleName => $"[aria-label='{locator.Value}']",
            LocatorStrategy.Text => $"text={locator.Value}",
            LocatorStrategy.PartialText => $"text=/{locator.Value}/",
            LocatorStrategy.Index => $":nth-child({int.Parse(locator.Value) + 1})",
            LocatorStrategy.Native => locator.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(locator.Strategy))
        };
    }
}
```

---

## 7. Usage Examples

```csharp
// Simple locators
var loginButton = By.AutomationId("LoginButton");
var userName = By.Name("username");
var submitBtn = By.Css("button[type='submit']");

// Chained/scoped locators
var dialogOkButton = By.AutomationId("ConfirmDialog").Child("OkButton");
var firstListItem = By.AutomationId("ItemList").Child(By.Index(0));

// XPath for complex scenarios
var errorLabel = By.XPath("//Label[@text='Error' and @visible='true']");

// Using in controls
var button = page.FindControl<IButtonControlObject>(By.AutomationId("Submit"));
var input = page.FindControl<IEditableTextControlObject>(By.Name("email"));
```

---

**Next:** [SPEC-006-002b: Foundation Classes](SPEC-006-002-CLASSES-FOUNDATION.md)
