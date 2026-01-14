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
    /// <summary>
    /// Creates a new entry control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the entry element.</param>
    public MauiEntryControl(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new entry control within the specified scope using a string locator value.
    /// Uses the scope's DefaultLocatorStrategy to create the locator.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public MauiEntryControl(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region ITextControlObject Implementation

    /// <inheritdoc />
    public bool WaitTextEquals(string? expected, int? timeoutMs = null)
    {

        if (expected == null)
            return true;

        return WaitText(expected, timeoutMs);
    }

    /// <inheritdoc />
    public bool WaitTextContains(string? expected, int? timeoutMs = null)
    {

        if (expected == null)
            return true;

        return Poll(
            () => GetText()?.Contains(expected) == true,
            timeoutMs ?? DefaultTimeoutMs);
    }

    /// <inheritdoc />
    public TScope AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null)
    {

        if (pattern == null)
            return ContainingScope;

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

        return ContainingScope;
    }

    #endregion

    #region IEditableTextControlObject<TScope> Implementation

    /// <inheritdoc />
    public TScope Enter(string? text, int? timeoutMs = null)
    {
        if (text == null)
            return ContainingScope;

        CheckEnabled(timeoutMs);
        var element = FindElement();
        element.SendKeys(text);
        return ContainingScope;
    }

    /// <inheritdoc />
    public TScope Clear(int? timeoutMs = null)
    {
        CheckEnabled(timeoutMs);
        var element = FindElement();
        element.Clear();
        return ContainingScope;
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
    public TScope SetText(string? text, int? timeoutMs = null)
    {

        if (text == null)
            return ContainingScope;

        Clear(timeoutMs);
        Enter(text, timeoutMs);
        return ContainingScope;
    }

    /// <inheritdoc />
    public string? GetPlaceholder()
    {
        var element = TryFindElement();
        if (element == null)
            return null;


        // Try common placeholder attribute names
        // Android uses "hint", iOS uses "placeholderValue" or "value" when empty
        var placeholder = element.GetAttribute("hint")
                       ?? element.GetAttribute("placeholderValue")
                       ?? element.GetAttribute("placeholder");

        return placeholder;

    }

    /// <inheritdoc />
    public bool WaitPlaceholder(string? expected, int? timeoutMs = null)
    {

        if (expected == null)
            return true;

        return Poll(
            () => GetPlaceholder() == expected,
            timeoutMs ?? DefaultTimeoutMs);
    }

    /// <inheritdoc />
    public TScope AssertPlaceholder(string? expected, string? message = null, int? timeoutMs = null)
    {

        if (expected == null)
            return ContainingScope;

        if (!WaitPlaceholder(expected, timeoutMs))
        {
            var actual = GetPlaceholder();
            throw new AssertionException(
                message ?? $"Expected placeholder '{expected}' but got '{actual ?? "(null)"}'. Locator: {Locator}");
        }

        return ContainingScope;
    }

    /// <inheritdoc />
    public bool? IsReadOnly()
    {
        var element = TryFindElement();
        if (element == null)
            return null;

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

    /// <inheritdoc />
    public bool WaitReadOnly(bool? expected, int? timeoutMs = null)
    {

        if (expected == null)
            return true;

        return Poll(
            () => IsReadOnly() == expected.Value,
            timeoutMs ?? DefaultTimeoutMs);
    }

    /// <inheritdoc />
    public TScope AssertReadOnly(bool? expected, string? message = null, int? timeoutMs = null)
    {

        if (expected == null)
            return ContainingScope;

        if (!WaitReadOnly(expected, timeoutMs))
        {
            var actual = IsReadOnly();
            throw new AssertionException(
                message ?? $"Expected element {(expected.Value ? "to be read-only" : "not to be read-only")} but read-only state is {actual?.ToString() ?? "unknown (element not found)"}. Locator: {Locator}");
        }

        return ContainingScope;
    }

    #endregion
}
