using System.Text.RegularExpressions;

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
        return RunAssert(nameof(AssertTextMatches), pattern, () =>
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

    #region IEditableTextControlObject<TScope> Implementation - Public API

    /// <summary>
    /// Enters text into the control. Uses optimized element-passing pattern.
    /// </summary>
    /// <param name="text">The text to enter.</param>
    /// <param name="timeoutMs">Optional timeout for waiting.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope Enter(string? text, int? timeoutMs = null)
    {
        if (text == null) return ContainingScope;

        return RunWithElement(nameof(Enter), text, timeoutMs, element =>
        {
            EnterCore(element, text, timeoutMs);
        });
    }

    /// <summary>
    /// Clears the text from the control. Uses optimized element-passing pattern.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout for waiting.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope Clear(int? timeoutMs = null)
    {
        return RunWithElement(nameof(Clear), timeoutMs, element =>
        {
            ClearCore(element, timeoutMs);
        });
    }

    /// <summary>
    /// Checks if the element is enabled and can be interacted with.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout for waiting.</param>
    /// <exception cref="ElementNotFoundException">Thrown when element is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when element is disabled.</exception>
    public void CheckEnabled(int? timeoutMs = null)
    {
        var element = FindElementWithWait(timeoutMs ?? DefaultTimeoutMs);
        CheckEnabledCore(element, timeoutMs);
    }

    /// <summary>
    /// Sets the text of the control (clears then enters). Uses optimized element-passing pattern.
    /// </summary>
    /// <param name="text">The text to set.</param>
    /// <param name="timeoutMs">Optional timeout for waiting.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope SetText(string? text, int? timeoutMs = null)
    {
        if (text == null) return ContainingScope;

        return RunWithElement(nameof(SetText), text, timeoutMs, element =>
        {
            SetTextCore(element, text, timeoutMs);
        });
    }

    #endregion

    #region Core Methods (Element-Aware) - Internal Implementation

    /// <summary>
    /// Core implementation of Enter using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="text">The text to enter.</param>
    /// <param name="timeoutMs">Optional timeout for enabled check.</param>
    protected void EnterCore(IMauiElement element, string text, int? timeoutMs = null)
    {
        CheckEnabledCore(element, timeoutMs);
        element.SendKeys(text);
    }

    /// <summary>
    /// Core implementation of Clear using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for enabled check.</param>
    protected void ClearCore(IMauiElement element, int? timeoutMs = null)
    {
        CheckEnabledCore(element, timeoutMs);
        element.Clear();
    }

    /// <summary>
    /// Core implementation of SetText using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="text">The text to set.</param>
    /// <param name="timeoutMs">Optional timeout for enabled check.</param>
    protected void SetTextCore(IMauiElement element, string text, int? timeoutMs = null)
    {
        CheckEnabledCore(element, timeoutMs);
        element.Clear();
        element.SendKeys(text);
    }

    /// <summary>
    /// Core implementation of CheckEnabled using pre-found element.
    /// Element already exists (was found by RunWithElement), just check if it's enabled.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for waiting for enabled state.</param>
    /// <exception cref="InvalidOperationException">Thrown when element is disabled.</exception>
    protected void CheckEnabledCore(IMauiElement element, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;

        // Element already exists - just check enabled state
        if (IsEnabledCore(element) != true)
        {
            if (!WaitEnabledCore(element, true, timeout))
            {
                throw new InvalidOperationException(
                    $"Element is disabled and cannot be interacted with. Locator: {Locator}");
            }
        }
    }

    #endregion

    #region Placeholder - Core Methods

    /// <summary>
    /// Gets the placeholder text using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>The placeholder text, or null if not found.</returns>
    protected string? GetPlaceholderCore(IMauiElement? element)
    {
        if (element == null) return null;
        // Windows MAUI uses "Name" for placeholder when entry is empty
        // Android uses hint, iOS uses placeholder
        return element.GetAttribute("Name")
            ?? element.GetAttribute("HelpText")
            ?? element.GetAttribute("hint")
            ?? element.GetAttribute("placeholderValue")
            ?? element.GetAttribute("placeholder");
    }

    public string? GetPlaceholder()
    {
        return GetPlaceholderCore(TryFindElement());
    }

    /// <summary>
    /// Polls placeholder text using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="expected">The expected placeholder text.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    protected bool WaitPlaceholderCore(IMauiElement element, string expected, int timeoutMs)
    {
        return PollWithElement(element, e => GetPlaceholderCore(e) == expected, timeoutMs);
    }

    public bool WaitPlaceholder(string? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        var element = TryFindElement();
        if (element == null) return false;

        return WaitPlaceholderCore(element, expected, timeoutMs ?? DefaultTimeoutMs);
    }

    public TScope AssertPlaceholder(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;
        
        return RunAssert(nameof(AssertPlaceholder), expected, () =>
        {
            WaitPlaceholder(expected, timeoutMs);
            return GetPlaceholder();
        }, message ?? $"Expected placeholder '{expected}'. Locator: {Locator}");
    }

    #endregion

    #region ReadOnly - Core Methods

    /// <summary>
    /// Checks if element is read-only using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True if read-only, false if editable, null if element not found.</returns>
    protected bool? IsReadOnlyCore(IMauiElement? element)
    {
        if (element == null) return null;

        var readOnly = element.GetAttribute("readonly") ?? element.GetAttribute("isReadOnly");
        if (readOnly != null) return readOnly.Equals("true", StringComparison.OrdinalIgnoreCase);

        var editable = element.GetAttribute("editable");
        if (editable != null) return !editable.Equals("true", StringComparison.OrdinalIgnoreCase);

        return false;
    }

    public bool? IsReadOnly()
    {
        return IsReadOnlyCore(TryFindElement());
    }

    /// <summary>
    /// Polls read-only state using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="expected">The expected read-only state.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    protected bool WaitReadOnlyCore(IMauiElement element, bool expected, int timeoutMs)
    {
        return PollWithElement(element, e => IsReadOnlyCore(e) == expected, timeoutMs);
    }

    public bool WaitReadOnly(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        var element = TryFindElement();
        if (element == null) return expected.Value == false; // Not found = not readonly

        return WaitReadOnlyCore(element, expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    public TScope AssertReadOnly(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;
        
        return RunAssert(nameof(AssertReadOnly), expected, () =>
        {
            WaitReadOnly(expected, timeoutMs);
            return IsReadOnly();
        }, message ?? $"Expected element {(expected.Value ? "to be read-only" : "not to be read-only")}. Locator: {Locator}");
    }

    #endregion
}