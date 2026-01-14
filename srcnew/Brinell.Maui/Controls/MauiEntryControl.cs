using System.Text.RegularExpressions;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Maui.Context;
using Brinell.Maui.Interfaces;
using OpenQA.Selenium;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Entry control with text input capability and fluent method chaining.
/// </summary>
/// <typeparam name="TPage">The parent page type for fluent chaining.</typeparam>
public class MauiEntryControl<TPage> : MauiControlBase<TPage>, IEditableTextControlObject<TPage>
    where TPage : IPageObject
{
    /// <summary>
    /// Creates a new entry control within the specified scope.
    /// </summary>
    /// <param name="scope">The paged scope (page or container) providing element finding and page reference.</param>
    /// <param name="locator">The locator for the entry element.</param>
    public MauiEntryControl(IMauiPagedScope<TPage> scope, Locator locator)
        : base(scope, locator)
    {
    }
    
    #region ITextControlObject Implementation
    
    /// <inheritdoc />
    public bool WaitTextEquals(string? expected, int? timeoutMs = null)
    {
        
        if (expected == null) return true;
        
        return WaitText(expected, timeoutMs);
    }
    
    /// <inheritdoc />
    public bool WaitTextContains(string? expected, int? timeoutMs = null)
    {
        
        if (expected == null) return true;
        
        return Poll(
            () => GetText()?.Contains(expected) == true,
            timeoutMs ?? DefaultTimeoutMs);
    }
    
    /// <inheritdoc />
    public void AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null)
    {
        
        if (pattern == null) return;
        
        var regex = new Regex(pattern);
        var passed = Poll(
            () =>
            {
                var text = GetText();
                return text != null && regex.IsMatch(text);
            },
            timeoutMs ?? DefaultTimeoutMs);
        
        if (!passed)
        {
            var actual = GetText();
            throw new AssertionException(
                message ?? $"Expected text to match pattern '{pattern}' but got '{actual ?? "(null)"}'. Locator: {Locator}");
        }
    }
    
    #endregion
    
    #region IEditableTextControlObject<TPage> Implementation
    
    /// <inheritdoc />
    public TPage Enter(string? text, int? timeoutMs = null)
    {
        if (text == null) return Page;
        
        CheckEnabled(timeoutMs);
        var element = FindElement();
        element.SendKeys(text);
        return Page;
    }
    
    /// <inheritdoc />
    public TPage Clear(int? timeoutMs = null)
    {
        CheckEnabled(timeoutMs);
        var element = FindElement();
        element.Clear();
        return Page;
    }
    
    /// <summary>
    /// Checks that the element exists and is enabled, throwing if not.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <exception cref="ElementNotFoundException">Thrown when element not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when element is disabled.</exception>
    public void CheckEnabled(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        
        if (!WaitExists(true, timeout))
        {
            throw new ElementNotFoundException(
                $"Element not found within {timeout}ms. Locator: {Locator}");
        }
        
        if (IsEnabled() == false)
        {
            throw new InvalidOperationException(
                $"Element is disabled and cannot be interacted with. Locator: {Locator}");
        }
    }
    
    /// <inheritdoc />
    public TPage SetText(string? text, int? timeoutMs = null)
    {
        
        if (text == null) return Page;
        
        Clear(timeoutMs);
        Enter(text, timeoutMs);
        return Page;
    }
    
    /// <inheritdoc />
    public string? GetPlaceholder()
    {
        var element = TryFindElement();
        if (element == null) return null;
        
        try
        {
            // Try common placeholder attribute names
            // Android uses "hint", iOS uses "placeholderValue" or "value" when empty
            var placeholder = element.GetAttribute("hint") 
                           ?? element.GetAttribute("placeholderValue")
                           ?? element.GetAttribute("placeholder");
            
            return placeholder;
        }
        catch (StaleElementReferenceException)
        {
            return null;
        }
        catch (WebDriverException)
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public bool WaitPlaceholder(string? expected, int? timeoutMs = null)
    {
        
        if (expected == null) return true;
        
        return Poll(
            () => GetPlaceholder() == expected,
            timeoutMs ?? DefaultTimeoutMs);
    }
    
    /// <inheritdoc />
    public void AssertPlaceholder(string? expected, string? message = null, int? timeoutMs = null)
    {
        
        if (expected == null) return;
        
        if (!WaitPlaceholder(expected, timeoutMs))
        {
            var actual = GetPlaceholder();
            throw new AssertionException(
                message ?? $"Expected placeholder '{expected}' but got '{actual ?? "(null)"}'. Locator: {Locator}");
        }
    }
    
    /// <inheritdoc />
    public bool? IsReadOnly()
    {
        var element = TryFindElement();
        if (element == null) return null;
        
        try
        {
            // Check for read-only attribute
            var readOnly = element.GetAttribute("readonly") 
                        ?? element.GetAttribute("isReadOnly");
            
            if (readOnly != null)
            {
                return readOnly.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            
            // Also check if element is editable
            var editable = element.GetAttribute("editable");
            if (editable != null)
            {
                return !editable.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            
            // Default to not read-only if we can't determine
            return false;
        }
        catch (StaleElementReferenceException)
        {
            return null;
        }
        catch (WebDriverException)
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public bool WaitReadOnly(bool? expected, int? timeoutMs = null)
    {
        
        if (expected == null) return true;
        
        return Poll(
            () => IsReadOnly() == expected.Value,
            timeoutMs ?? DefaultTimeoutMs);
    }
    
    /// <inheritdoc />
    public void AssertReadOnly(bool? expected, string? message = null, int? timeoutMs = null)
    {
        
        if (expected == null) return;
        
        if (!WaitReadOnly(expected, timeoutMs))
        {
            var actual = IsReadOnly();
            throw new AssertionException(
                message ?? $"Expected element {(expected.Value ? "to be read-only" : "not to be read-only")} but read-only state is {actual?.ToString() ?? "unknown (element not found)"}. Locator: {Locator}");
        }
    }
    
    #endregion
}
