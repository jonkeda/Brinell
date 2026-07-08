namespace Brinell.Maui.Controls.DateTimes;

/// <summary>
/// MAUI DatePicker control for date selection.
/// Provides GetDate, SetDate, and date assertion methods.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class DatePicker<TScope> : ControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new date picker control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the date picker element.</param>
    public DatePicker(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new date picker control within the specified scope using a string locator value.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public DatePicker(IMauiScope<TScope> scope, string locatorValue)
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

        // Try Date attribute first (MAUI mobile)
        var dateAttr = element.GetAttribute("Date")
            ?? element.GetAttribute("SelectedDate")
            ?? element.GetAttribute("Value")
            ?? element.GetAttribute("value.value");

        if (!string.IsNullOrEmpty(dateAttr) && System.DateTime.TryParse(dateAttr, out var dateValue))
        {
            return dateValue;
        }

        // Windows MAUI: DatePicker (CalendarDatePicker) has child Text with AutomationId="DateText"
        // whose Name contains the formatted date like "‎20‎-‎Jan‎-‎01" (with Unicode LTR marks)
        // Try finding the DateText child first (most reliable)
        var dateTextElements = element.FindElements(Locator.ByAutomationId("DateText"));
        if (dateTextElements.Count > 0)
        {
            var dateTextName = dateTextElements[0].GetAttribute("Name");
            if (!string.IsNullOrEmpty(dateTextName) && TryParseDateString(dateTextName, out var dateTextValue))
            {
                return dateTextValue;
            }
        }

        // Fallback: search all descendants for parseable date
        // XPath may not be supported by all drivers, so catch only WebDriverException
        try
        {
            var children = element.FindElements(Locator.ByXPath(".//*"));
            foreach (var child in children)
            {
                // Try child's Name attribute first (most reliable on Windows)
                var childName = child.GetAttribute("Name");
                if (!string.IsNullOrEmpty(childName) && TryParseDateString(childName, out var childNameDate))
                {
                    return childNameDate;
                }

                // Try child's Text property
                var childText = child.Text;
                if (!string.IsNullOrEmpty(childText) && TryParseDateString(childText, out var childTextDate))
                {
                    return childTextDate;
                }
            }
        }
        catch (WebDriverException)
        {
            // XPath not supported by this driver - fall through to Name/Text fallbacks
        }

        // Try element's own Name attribute (fallback)
        var nameAttr = element.GetAttribute("Name");
        if (!string.IsNullOrEmpty(nameAttr) && TryParseDateString(nameAttr, out var nameValue))
        {
            return nameValue;
        }

        // Try text content
        var text = element.Text;
        if (!string.IsNullOrEmpty(text) && TryParseDateString(text, out var textValue))
        {
            return textValue;
        }

        return null;
    }

    /// <summary>
    /// Attempts to parse a date string in various formats.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="result">The parsed DateTime if successful.</param>
    /// <returns>True if parsing succeeded.</returns>
    private static bool TryParseDateString(string text, out System.DateTime result)
    {
        result = default;
        if (string.IsNullOrEmpty(text)) return false;

        // Clean up text: strip Unicode control characters (like LTR marks U+200E)
        // Windows MAUI embeds these in date strings like "‎20‎-‎Jan‎-‎01"
        var cleaned = System.Text.RegularExpressions.Regex.Replace(text, @"\p{Cf}", "");
        cleaned = cleaned.Trim();
        
        if (string.IsNullOrEmpty(cleaned)) return false;

        // Try standard DateTime parsing
        if (System.DateTime.TryParse(cleaned, out result))
            return true;

        // Try common date formats
        var formats = new[]
        {
            "yyyy-MM-dd",           // ISO format
            "MM/dd/yyyy",           // US format
            "dd/MM/yyyy",           // UK/EU format  
            "dd-MMM-yy",            // 20-Jan-01 (Windows MAUI format)
            "d-MMM-yy",             // 1-Jan-01
            "dd-MMM-yyyy",          // 20-Jan-2001
            "d-MMM-yyyy",           // 1-Jan-2001
            "MMMM d, yyyy",         // March 20, 1985
            "MMMM dd, yyyy",        // March 20, 1985
            "d MMMM yyyy",          // 20 March 1985
            "dd MMMM yyyy",         // 20 March 1985
            "M/d/yyyy",             // 3/20/1985
            "d/M/yyyy"              // 20/3/1985
        };

        foreach (var format in formats)
        {
            if (System.DateTime.TryParseExact(cleaned, format, 
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsed))
            {
                result = parsed;
                return true;
            }
        }

        // Try current culture
        if (System.DateTime.TryParse(cleaned, System.Globalization.CultureInfo.CurrentCulture, out var cultureParsed))
        {
            result = cultureParsed;
            return true;
        }

        return false;
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
        return RunSetWithElement(date, element =>
        {
            SetDateCore(element, date!.Value);
        }, timeoutMs);
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
        // Clear can throw on WinUI CalendarDatePicker (non-text host), so treat as best effort.
        try
        {
            element.Clear();
        }
        catch
        {
            // Continue with direct input attempt.
        }

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

        return RunWaitWithElement(
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

        return RunAssert(expected, () =>
        {
            WaitDate(expected, timeoutMs);
            return GetDate();
        }, (actual, exp) => actual.HasValue && exp.HasValue && actual.Value.Date == exp.Value.Date,
            message ?? $"Expected date {expected:yyyy-MM-dd}. Locator: {Locator}", timeoutMs);
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
