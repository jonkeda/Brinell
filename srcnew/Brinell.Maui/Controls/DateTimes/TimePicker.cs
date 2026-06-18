namespace Brinell.Maui.Controls.DateTimes;

/// <summary>
/// MAUI TimePicker control for time selection.
/// Provides GetTime, SetTime, and time assertion methods.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class TimePicker<TScope> : ControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new time picker control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the time picker element.</param>
    public TimePicker(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new time picker control within the specified scope using a string locator value.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public TimePicker(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    #region GetTime

    /// <summary>
    /// Gets the time value from pre-found element.
    /// </summary>
    /// <param name="element">The pre-found element (may be null).</param>
    /// <returns>The time value, or null if not found or unparseable.</returns>
    protected TimeSpan? GetTimeCore(IMauiElement? element)
    {
        if (element == null) return null;

        // Try Time attribute first (MAUI mobile)
        var timeAttr = element.GetAttribute("Time")
            ?? element.GetAttribute("SelectedTime")
            ?? element.GetAttribute("Value")
            ?? element.GetAttribute("value.value");

        if (!string.IsNullOrEmpty(timeAttr) && TimeSpan.TryParse(timeAttr, out var timeValue))
        {
            return timeValue;
        }

        // Try parsing from DateTime attribute
        if (!string.IsNullOrEmpty(timeAttr) && System.DateTime.TryParse(timeAttr, out var dateTimeValue))
        {
            return dateTimeValue.TimeOfDay;
        }

        // Windows MAUI: TimePicker has child Button with AutomationId="FlyoutButton"
        // whose Name contains the formatted time like " 9:00 AM time picker"
        // Try finding the FlyoutButton child
        var flyoutButton = element.FindElements(Locator.ByAutomationId("FlyoutButton"));
        if (flyoutButton.Count > 0)
        {
            var buttonName = flyoutButton[0].GetAttribute("Name");
            if (!string.IsNullOrEmpty(buttonName) && TryParseTimeString(buttonName, out var buttonTime))
            {
                return buttonTime;
            }
        }

        // Fallback: search all descendants for parseable time
        // XPath may not be supported by all drivers, so catch only WebDriverException
        try
        {
            var children = element.FindElements(Locator.ByXPath(".//*"));
            foreach (var child in children)
            {
                var childName = child.GetAttribute("Name");
                if (!string.IsNullOrEmpty(childName) && TryParseTimeString(childName, out var childNameTime))
                {
                    return childNameTime;
                }

                var childText = child.Text;
                if (!string.IsNullOrEmpty(childText) && TryParseTimeString(childText, out var childTextTime))
                {
                    return childTextTime;
                }
            }
        }
        catch (WebDriverException)
        {
            // XPath not supported by this driver - fall through to Name/Text fallbacks
        }

        // Try element's own Name attribute (fallback)
        var nameAttr = element.GetAttribute("Name");
        if (!string.IsNullOrEmpty(nameAttr) && TryParseTimeString(nameAttr, out var nameTimeValue))
        {
            return nameTimeValue;
        }

        // Try text content
        var text = element.Text;
        if (!string.IsNullOrEmpty(text) && TryParseTimeString(text, out var textTimeValue))
        {
            return textTimeValue;
        }

        return null;
    }

    /// <summary>
    /// Attempts to parse a time string in various formats.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="result">The parsed TimeSpan if successful.</param>
    /// <returns>True if parsing succeeded.</returns>
    private static bool TryParseTimeString(string text, out TimeSpan result)
    {
        result = default;
        if (string.IsNullOrEmpty(text)) return false;

        // Clean up text: strip Unicode control characters (like LTR marks U+200E)
        // Windows MAUI embeds these in time strings like " ‎9‎:‎00‎ ‎AM time picker"
        var cleaned = System.Text.RegularExpressions.Regex.Replace(text, @"\p{Cf}", "");
        
        // Remove "time picker" suffix if present (Windows MAUI adds this)
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s*time\s*picker\s*$", "", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = cleaned.Trim();
        
        if (string.IsNullOrEmpty(cleaned)) return false;

        // Try standard TimeSpan parsing
        if (TimeSpan.TryParse(cleaned, out result))
            return true;

        // Try DateTime parsing and extract TimeOfDay
        if (System.DateTime.TryParse(cleaned, out var dateTime))
        {
            result = dateTime.TimeOfDay;
            return true;
        }

        // Try common Windows time formats (e.g., "10:30 AM", "2:45 PM")
        var formats = new[]
        {
            "h:mm tt",      // 2:30 PM
            "hh:mm tt",     // 02:30 PM
            "H:mm",         // 14:30
            "HH:mm",        // 14:30
            "h:mm:ss tt",   // 2:30:00 PM
            "HH:mm:ss"      // 14:30:00
        };

        foreach (var format in formats)
        {
            if (System.DateTime.TryParseExact(cleaned, format, 
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsed))
            {
                result = parsed.TimeOfDay;
                return true;
            }
        }

        // Try current culture
        if (System.DateTime.TryParse(cleaned, System.Globalization.CultureInfo.CurrentCulture, out var cultureParsed))
        {
            result = cultureParsed.TimeOfDay;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the currently selected time.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout for finding the element.</param>
    /// <returns>The selected time, or null if element not found.</returns>
    public TimeSpan? GetTime(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs);
        }
        return GetTimeCore(TryFindElement());
    }

    #endregion

    #region SetTime

    /// <summary>
    /// Sets the time value.
    /// </summary>
    /// <param name="time">The time to set. Null skips the operation.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope SetTime(TimeSpan? time, int? timeoutMs = null)
    {
        return RunSetWithElement(time, element =>
        {
            SetTimeCore(element, time!.Value, timeoutMs);
        }, timeoutMs);
    }

    /// <summary>
    /// Sets the time on pre-found element.
    /// Platform-specific implementation may need adjustment.
    /// </summary>
    /// <param name="element">The time picker element.</param>
    /// <param name="time">The time to set.</param>
    /// <param name="timeoutMs"></param>
    protected virtual void SetTimeCore(IMauiElement element, TimeSpan time, int? timeoutMs)
    {
        Scope.WaitReady(timeoutMs ?? DefaultTimeoutMs);

        // Click to open the picker
        element.Click();

        // Platform-specific time entry
        // For text-based input, clear and send formatted time
        element.Clear();
        element.SendKeys(time.ToString(@"hh\:mm"));

        // Close by pressing Enter or clicking elsewhere
        element.SendKeys(OpenQA.Selenium.Keys.Enter);
    }

    #endregion

    #region WaitTime

    /// <summary>
    /// Waits for the time to match the expected value.
    /// </summary>
    /// <param name="expected">Expected time. Null skips the wait.</param>
    /// <param name="toleranceSeconds">Tolerance in seconds for comparison.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>True if condition met, false if timeout.</returns>
    public bool WaitTime(TimeSpan? expected, int toleranceSeconds = 60, int? timeoutMs = null)
    {
        if (expected == null) return true;

        var element = TryFindElement();
        if (element == null) return false;

        var tolerance = TimeSpan.FromSeconds(toleranceSeconds);

        return RunWaitWithElement(
            e =>
            {
                var actual = GetTimeCore(e);
                if (!actual.HasValue) return false;
                var diff = (actual.Value - expected.Value).Duration();
                return diff <= tolerance;
            },
            timeoutMs ?? DefaultTimeoutMs);
    }

    #endregion

    #region AssertTime

    /// <summary>
    /// Asserts the time matches the expected value.
    /// </summary>
    /// <param name="expected">Expected time. Null skips the assertion.</param>
    /// <param name="toleranceSeconds">Tolerance in seconds for comparison.</param>
    /// <param name="message">Optional assertion message.</param>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The containing scope for fluent chaining.</returns>
    public TScope AssertTime(TimeSpan? expected, int toleranceSeconds = 60, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return ContainingScope;

        var tolerance = TimeSpan.FromSeconds(toleranceSeconds);

        return RunAssert(nameof(AssertTime), expected, () =>
        {
            WaitTime(expected, toleranceSeconds, timeoutMs);
            return GetTime();
        }, (actual, exp) =>
        {
            if (!actual.HasValue || !exp.HasValue) return false;
            var diff = (actual.Value - exp.Value).Duration();
            return diff <= tolerance;
        },
            message ?? $"Expected time {expected:hh\\:mm}. Locator: {Locator}");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the hours component of the selected time.
    /// </summary>
    /// <returns>The hours (0-23), or null if not available.</returns>
    public int? GetHours()
    {
        var time = GetTime();
        return time?.Hours;
    }

    /// <summary>
    /// Gets the minutes component of the selected time.
    /// </summary>
    /// <returns>The minutes (0-59), or null if not available.</returns>
    public int? GetMinutes()
    {
        var time = GetTime();
        return time?.Minutes;
    }

    #endregion
}
