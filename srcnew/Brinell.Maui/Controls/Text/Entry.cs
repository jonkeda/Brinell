using Brinell.Core;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace Brinell.Maui.Controls.Text;

/// <summary>
/// MAUI Entry control with text input capability and fluent method chaining.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class Entry<TScope> : ControlBase<TScope>, IEditableTextControlObject<TScope>
    where TScope : IMauiScope<TScope>
{
    public Entry(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public Entry(IMauiScope<TScope> scope, string locatorValue)
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
        return RunWait(() => GetText()?.Contains(expected) == true, timeoutMs);
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
        return RunSetWithElement(text, element =>
        {
            EnterCore(element, text!, timeoutMs);
        }, timeoutMs);
    }

    /// <summary>
    /// Clears the text from the control. Uses optimized element-passing pattern.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout for waiting.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope Clear(int? timeoutMs = null)
    {
        return RunDoWithElement(element =>
        {
            ClearCore(element, timeoutMs);
        }, timeoutMs);
    }

    /// <summary>
    /// Sets the text of the control (clears then enters). Uses optimized element-passing pattern.
    /// </summary>
    /// <param name="text">The text to set.</param>
    /// <param name="timeoutMs">Optional timeout for waiting.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope SetText(string? text, int? timeoutMs = null)
    {
        return RunSetWithElement(text, element =>
        {
            SetTextCore(element, text!, timeoutMs);
        }, timeoutMs);
    }

    /// <summary>
    /// Submits the entry by sending Enter to the existing edit element.
    /// This is useful for MAUI Entry.Completed command paths, such as search boxes.
    /// </summary>
    public TScope Submit(int? timeoutMs = null)
    {
        return RunSetWithElement(Keys.Enter, element =>
        {
            element.SendKeys(Keys.Enter, TextInputMethod.Keys);
        }, timeoutMs);
    }
    
    /// <summary>
    /// Appends text to existing content without clearing.
    /// If text is null, returns immediately (skip).
    /// </summary>
    /// <param name="text">The text to append.</param>
    /// <param name="timeoutMs">Optional timeout for waiting.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope Append(string? text, int? timeoutMs = null)
    {
        return RunSetWithElement(text, element =>
        {
            AppendCore(element, text!, timeoutMs);
        }, timeoutMs);
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
        SetTextCore(element, text, timeoutMs);
    }

    /// <summary>
    /// Core implementation of Clear using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for enabled check.</param>
    protected virtual void ClearCore(IMauiElement element, int? timeoutMs = null)
    {
        element.Clear();
    }

    /// <summary>
    /// Core implementation of SetText using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="text">The text to set.</param>
    /// <param name="timeoutMs">Optional timeout for enabled check.</param>
    protected virtual void SetTextCore(IMauiElement element, string text, int? timeoutMs = null)
    {
        if (element is Interfaces.INestedTextElement textElement
            && textElement.SetTextWithFallback(text))
        {
            return;
        }

        element.Clear();
        element.SendKeys(text, TextInputMethod.SetValue);
        element.SendKeys("\t");
    }
    
    /// <summary>
    /// Core implementation of Append using pre-found element.
    /// Appends text without clearing existing content.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="text">The text to append.</param>
    /// <param name="timeoutMs">Optional timeout for enabled check.</param>
    protected void AppendCore(IMauiElement element, string text, int? timeoutMs = null)
    {
        element.SendKeys(text);
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
    /// <param name="expected">The expected placeholder text.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    public bool WaitPlaceholder(string? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        var element = TryFindElement();
        if (element == null) return false;

        return RunWaitWithElement(e => GetPlaceholderCore(e) == expected, timeoutMs);
    }

    public TScope AssertPlaceholder(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        return RunAssert(expected, () =>
        {
            WaitPlaceholder(expected, timeoutMs);
            return GetPlaceholder();
        }, (actual, exp) => Equals(actual, exp), message ?? $"Expected placeholder '{expected}'. Locator: {Locator}", timeoutMs);
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
    /// <param name="expected">The expected read-only state.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    public bool WaitReadOnly(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        var element = TryFindElement();
        if (element == null) return expected.Value == false; // Not found = not readonly

        return RunWaitWithElement(e => IsReadOnlyCore(e) == expected, timeoutMs);
    }

    public TScope AssertReadOnly(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        return RunAssert(expected, () =>
        {
            WaitReadOnly(expected, timeoutMs);
            return IsReadOnly();
        }, (actual, exp) => Equals(actual, exp), message ?? $"Expected element {(expected.Value ? "to be read-only" : "not to be read-only")}. Locator: {Locator}", timeoutMs);
    }

    #endregion
}
