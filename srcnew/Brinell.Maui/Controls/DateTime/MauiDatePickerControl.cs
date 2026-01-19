namespace Brinell.Maui.Controls.DateTime;

/// <summary>
/// MAUI DatePicker control for date selection.
/// Provides GetDate, SetDate, and date assertion methods.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class MauiDatePickerControl<TScope> : MauiControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new date picker control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the date picker element.</param>
    public MauiDatePickerControl(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new date picker control within the specified scope using a string locator value.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public MauiDatePickerControl(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region GetDate

    /// <summary>
    /// Gets the date value from pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>The date value, or null if not found or unparseable.</returns>
    protected System.DateTime? GetDateCore(IMauiElement? element)
    {
        if (element == null) return null;

        // Try Date attribute first
        var dateAttr = element.GetAttribute("Date")
            ?? element.GetAttribute("SelectedDate")
            ?? element.GetAttribute("Value");

        if (!string.IsNullOrEmpty(dateAttr) && System.DateTime.TryParse(dateAttr, out var dateValue))
        {
            return dateValue;
        }

        // Try text content
        var text = element.Text;
        if (!string.IsNullOrEmpty(text) && System.DateTime.TryParse(text, out var textValue))
        {
            return textValue;
        }

        return null;
    }

    /// <summary>
    /// Gets the currently selected date.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout for finding the element.</param>
    /// <returns>The selected date, or null if element not found.</returns>
    public System.DateTime? GetDate(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }
        return GetDateCore(TryFindElement());
    }

    #endregion

    #region SetDate

    /// <summary>
    /// Sets the date value.
    /// </summary>
    /// <param name="date">The date to set. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope SetDate(System.DateTime? date, int? timeoutMs = null)
    {
        if (date == null)
            return ContainingScope;

        return RunWithElement(nameof(SetDate), date, timeoutMs, element =>
        {
            SetDateCore(element, date.Value);
        });
    }

    /// <summary>
    /// Sets the date on pre-found element.
    /// Platform-specific implementation may need adjustment.
    /// </summary>
    /// <param name="element">The date picker element.</param>
    /// <param name="date">The date to set.</param>
    protected virtual void SetDateCore(IMauiElement element, System.DateTime date)
    {
        // Click to open the picker
        element.Click();

        // Platform-specific date entry
        // For text-based input, clear and send formatted date
        element.Clear();
        element.SendKeys(date.ToString("yyyy-MM-dd"));

        // Close by pressing Enter or clicking elsewhere
        element.SendKeys(OpenQA.Selenium.Keys.Enter);
    }

    #endregion

    #region WaitDate

    /// <summary>
    /// Waits for the date to match the expected value.
    /// </summary>
    /// <param name="expected">Expected date. Null skips the wait.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>True if condition met, false if timeout.</returns>
    public bool WaitDate(System.DateTime? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;

        var element = TryFindElement();
        if (element == null) return false;

        return PollWithElement(
            element,
            e =>
            {
                var actual = GetDateCore(e);
                return actual.HasValue && actual.Value.Date == expected.Value.Date;
            },
            timeoutMs ?? DefaultTimeoutMs);
    }

    #endregion

    #region AssertDate

    /// <summary>
    /// Asserts the date matches the expected value.
    /// </summary>
    /// <param name="expected">Expected date. Null skips the assertion.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertDate(System.DateTime? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        return RunAssert(nameof(AssertDate), expected, () =>
        {
            WaitDate(expected, timeoutMs);
            return GetDate();
        }, (actual, exp) => actual.HasValue && exp.HasValue && actual.Value.Date == exp.Value.Date,
            message ?? $"Expected date {expected:yyyy-MM-dd}. Locator: {Locator}");
    }

    #endregion

    #region MinimumDate / MaximumDate

    /// <summary>
    /// Gets the minimum allowed date.
    /// </summary>
    /// <returns>The minimum date, or null if not available.</returns>
    public System.DateTime? GetMinimumDate()
    {
        var element = TryFindElement();
        if (element == null) return null;

        var minAttr = element.GetAttribute("MinimumDate") ?? element.GetAttribute("Minimum");
        if (!string.IsNullOrEmpty(minAttr) && System.DateTime.TryParse(minAttr, out var minValue))
        {
            return minValue;
        }

        return null;
    }

    /// <summary>
    /// Gets the maximum allowed date.
    /// </summary>
    /// <returns>The maximum date, or null if not available.</returns>
    public System.DateTime? GetMaximumDate()
    {
        var element = TryFindElement();
        if (element == null) return null;

        var maxAttr = element.GetAttribute("MaximumDate") ?? element.GetAttribute("Maximum");
        if (!string.IsNullOrEmpty(maxAttr) && System.DateTime.TryParse(maxAttr, out var maxValue))
        {
            return maxValue;
        }

        return null;
    }

    #endregion
}
