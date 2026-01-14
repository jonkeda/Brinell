using System.Text.RegularExpressions;
using Brinell.Core.Exceptions;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Maui.Interfaces;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Entry control with text input capability and fluent method chaining.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class MauiEntryControl<TScope> : MauiControlBase<TScope>, IEditableTextControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    public MauiEntryControl(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public MauiEntryControl(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region ITextControlObject Implementation

    public bool WaitTextEquals(string? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        return WaitText(expected, timeoutMs);
    }

    public bool WaitTextContains(string? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        return Poll(() => GetText()?.Contains(expected) == true, timeoutMs ?? DefaultTimeoutMs);
    }

    public TScope AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null)
    {
        if (pattern == null) return ContainingScope;

        var regex = new Regex(pattern);
        return RunAssert("AssertTextMatches", pattern, () =>
        {
            Poll(() =>
            {
                var text = GetText();
                return text != null && regex.IsMatch(text);
            }, timeoutMs ?? DefaultTimeoutMs);
            return GetText();
        }, (actual, exp) => actual != null && regex.IsMatch(actual),
            message ?? $"Expected text to match pattern '{pattern}'. Locator: {Locator}");
    }

    #endregion

    #region IEditableTextControlObject<TScope> Implementation

    public TScope Enter(string? text, int? timeoutMs = null)
    {
        if (text == null) return ContainingScope;

        Run<string>("Enter", text, () =>
        {
            CheckEnabled(timeoutMs);
            var element = FindElement();
            element.SendKeys(text);
        });
        return ContainingScope;
    }

    public TScope Clear(int? timeoutMs = null)
    {
        Run("Clear", () =>
        {
            CheckEnabled(timeoutMs);
            var element = FindElement();
            element.Clear();
        });
        return ContainingScope;
    }

    public void CheckEnabled(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        if (!WaitExists(true, timeout))
            throw new ElementNotFoundException($"Element not found within {timeout}ms. Locator: {Locator}");
        if (IsEnabled() == false)
            throw new InvalidOperationException($"Element is disabled and cannot be interacted with. Locator: {Locator}");
    }

    public TScope SetText(string? text, int? timeoutMs = null)
    {
        if (text == null) return ContainingScope;

        Run<string>("SetText", text, () =>
        {
            CheckEnabled(timeoutMs);
            var element = FindElement();
            element.Clear();
            element.SendKeys(text);
        });
        return ContainingScope;
    }

    public string? GetPlaceholder()
    {
        var element = TryFindElement();
        if (element == null) return null;
        var placeholder = element.GetAttribute("hint")
                       ?? element.GetAttribute("placeholderValue")
                       ?? element.GetAttribute("placeholder");
        return placeholder;
    }

    public bool WaitPlaceholder(string? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        return Poll(() => GetPlaceholder() == expected, timeoutMs ?? DefaultTimeoutMs);
    }

    public TScope AssertPlaceholder(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;
        
        return RunAssert("AssertPlaceholder", expected, () =>
        {
            WaitPlaceholder(expected, timeoutMs);
            return GetPlaceholder();
        }, message ?? $"Expected placeholder '{expected}'. Locator: {Locator}");
    }

    public bool? IsReadOnly()
    {
        var element = TryFindElement();
        if (element == null) return null;
        var readOnly = element.GetAttribute("readonly") ?? element.GetAttribute("isReadOnly");
        if (readOnly != null) return readOnly.Equals("true", StringComparison.OrdinalIgnoreCase);
        var editable = element.GetAttribute("editable");
        if (editable != null) return !editable.Equals("true", StringComparison.OrdinalIgnoreCase);
        return false;
    }

    public bool WaitReadOnly(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        return Poll(() => IsReadOnly() == expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    public TScope AssertReadOnly(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;
        
        return RunAssert("AssertReadOnly", expected, () =>
        {
            WaitReadOnly(expected, timeoutMs);
            return IsReadOnly();
        }, message ?? $"Expected element {(expected.Value ? "to be read-only" : "not to be read-only")}. Locator: {Locator}");
    }

    #endregion
}