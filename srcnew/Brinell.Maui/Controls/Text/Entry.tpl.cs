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
    [SkipGeneration("Enter is hand-written below so a null text skips without resolving an element.")]
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

        element.Clear();
        // SetValue writes through the platform's own value mechanism, which needs no commit
        // keystroke: no trailing Tab.
        element.SendKeys(text, TextInputMethod.SetValue);
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

        // The hint is what a field shows before it is filled in, and every platform publishes it.
        // An empty MAUI Entry on Windows also reports its placeholder as the accessible name -
        // a coincidence of that platform rather than a binding, so it is consulted second.
        return element.Hint ?? element.Name;
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

        // Read-only is the Value pattern's business. Windows answers it; Android publishes no
        // editability at all, so it answers null - unknown - rather than "editable".
        return element is IValuePatternElement { SupportsValuePattern: true } value
            ? value.IsValuePatternReadOnly()
            : null;
    }

    #endregion

    #region Hand-written Convenience Members

    /// <summary>
    /// Enters text, doing nothing when the text is null.
    /// </summary>
    /// <remarks>
    /// Hand-written to follow the nullable skip pattern the generated setters use: a null value
    /// returns the scope without resolving an element. The generator classifies setters by the
    /// <c>Set</c> prefix, and <c>Enter</c> is the same operation under a different name.
    /// </remarks>
    /// <param name="text">The text to enter, or null to skip.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The containing scope, for chaining.</returns>
    public TScope Enter(string? text, int? timeoutMs = null)
    {
        return RunSetWithElement(text, element => EnterCore(element, text, timeoutMs), timeoutMs);
    }

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
