using Brinell.Core;

namespace Brinell.Maui.Controls.Text;

/// <summary>
/// MAUI Entry control with text input capability and fluent method chaining.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class Entry<TScope> : Base.FocusableControlBase<TScope>, IEditableTextControlObject<TScope>
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

    #region Text - Core Methods

    /// <summary>
    /// Gets the text of the element using pre-found element.
    /// Override in derived classes for platform-specific text retrieval.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>The element text, or null if element is null.</returns>
    [GenerateComparisons(Comparison.Equals | Comparison.Contains | Comparison.StartsWith
        | Comparison.EndsWith | Comparison.Empty)]
    protected virtual string? GetTextCore(IMauiElement? element)
    {
        return element?.Text;
    }

    #endregion

    #region Editable Text - Core Methods

    /// <summary>
    /// Core implementation of Enter using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="text">The text to enter.</param>
    /// <param name="timeoutMs">Optional timeout for enabled check.</param>
    protected virtual void EnterCore(IMauiElement element, string? text, int? timeoutMs = null)
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
    protected virtual void SetTextCore(IMauiElement element, string? text, int? timeoutMs = null)
    {
        if (text == null) return;

        if (element is INestedTextElement<IMauiElement> textElement
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
    protected virtual void AppendCore(IMauiElement element, string? text, int? timeoutMs = null)
    {
        if (text == null) return;

        element.SendKeys(text);
    }

    /// <summary>
    /// Core implementation of Submit using pre-found element.
    /// Sends Enter to the edit element, driving MAUI Entry.Completed command paths
    /// such as search boxes.
    /// </summary>
    /// <param name="element">The pre-found element.</param>
    /// <param name="timeoutMs">Optional timeout for enabled check.</param>
    protected virtual void SubmitCore(IMauiElement element, int? timeoutMs = null)
    {
        element.SendKeys(Keys.Enter, TextInputMethod.Keys);
    }

    #endregion

    #region Placeholder - Core Methods

    /// <summary>
    /// Gets the placeholder text using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>The placeholder text, or null if not found.</returns>
    protected virtual string? GetPlaceholderCore(IMauiElement? element)
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

    #endregion

    #region ReadOnly - Core Methods

    /// <summary>
    /// Checks if element is read-only using pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>True if read-only, false if editable, null if element not found.</returns>
    protected virtual bool? IsReadOnlyCore(IMauiElement? element)
    {
        if (element == null) return null;

        var readOnly = element.GetAttribute("readonly") ?? element.GetAttribute("isReadOnly");
        if (readOnly != null) return readOnly.Equals("true", StringComparison.OrdinalIgnoreCase);

        var editable = element.GetAttribute("editable");
        if (editable != null) return !editable.Equals("true", StringComparison.OrdinalIgnoreCase);

        return false;
    }

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Waits until the text equals the expected value.
    /// If expected is null, returns true immediately (skip).
    /// </summary>
    /// <param name="expected">The expected text.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if the condition was met, false if the timeout was reached.</returns>
    public bool WaitTextEquals(string? expected, int? timeoutMs = null)
    {
        return WaitText(expected, timeoutMs) == true;
    }

    #endregion
}
