namespace Brinell.Maui.Controls.DateTime;

/// <summary>
/// MAUI TimePicker control for time selection.
/// Provides GetTime, SetTime, and time assertion methods.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class MauiTimePickerControl<TScope> : MauiControlBase<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>
    /// Creates a new time picker control within the specified scope.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locator">The locator for the time picker element.</param>
    public MauiTimePickerControl(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>
    /// Creates a new time picker control within the specified scope using a string locator value.
    /// </summary>
    /// <param name="scope">The scope (page or container) providing element finding.</param>
    /// <param name="locatorValue">The locator value (e.g., automation ID, name).</param>
    public MauiTimePickerControl(IMauiScope<TScope> scope, string locatorValue)
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

        // Try Time attribute first
        var timeAttr = element.GetAttribute("Time")
            ?? element.GetAttribute("SelectedTime")
            ?? element.GetAttribute("Value");

        if (!string.IsNullOrEmpty(timeAttr) && TimeSpan.TryParse(timeAttr, out var timeValue))
        {
            return timeValue;
        }

        // Try parsing from DateTime attribute
        if (!string.IsNullOrEmpty(timeAttr) && System.DateTime.TryParse(timeAttr, out var dateTimeValue))
        {
            return dateTimeValue.TimeOfDay;
        }

        // Try text content
        var text = element.Text;
        if (!string.IsNullOrEmpty(text))
        {
            if (TimeSpan.TryParse(text, out var textTimeValue))
            {
                return textTimeValue;
            }
            if (System.DateTime.TryParse(text, out var textDateTimeValue))
            {
                return textDateTimeValue.TimeOfDay;
            }
        }

        return null;
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
        if (time == null)
            return ContainingScope;

        return RunWithElement(nameof(SetTime), time, timeoutMs, element =>
        {
            SetTimeCore(element, time.Value);
        });
    }

    /// <summary>
    /// Sets the time on pre-found element.
    /// Platform-specific implementation may need adjustment.
    /// </summary>
    /// <param name="element">The time picker element.</param>
    /// <param name="time">The time to set.</param>
    protected virtual void SetTimeCore(IMauiElement element, TimeSpan time)
    {
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

        return PollWithElement(
            element,
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
