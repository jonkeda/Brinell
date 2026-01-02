using System.Globalization;
using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.WinForms.Controls;

/// <summary>
/// WinForms DateTimePicker control wrapper.
/// Inherits from InputControlBase which provides Clear, AppendText, IsReadOnly, GetTextLength.
/// Provides date/time-specific operations for setting and retrieving date/time values.
/// </summary>
public class DateTimePickerControl : InputControlBase
{
    public DateTimePickerControl(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public DateTimePickerControl(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public DateTimePickerControl(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Set the date/time value.
    /// Format: MM/dd/yyyy or MM/dd/yyyy HH:mm:ss depending on control configuration.
    /// </summary>
    public void SetDateTime(DateTime dateTime)
    {
        SetText(dateTime.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture));
        LogAction("SetDateTime", dateTime.ToString("g"));
    }

    /// <summary>
    /// Set the date only (time portion set to 00:00:00).
    /// </summary>
    public void SetDate(DateTime date)
    {
        SetText(date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture));
        LogAction("SetDate", date.ToString("d"));
    }

    /// <summary>
    /// Get the current date/time value.
    /// </summary>
    public DateTime GetDateTime()
    {
        var text = GetText();
        
        // Try various date formats
        var formats = new[]
        {
            "MM/dd/yyyy HH:mm:ss",
            "MM/dd/yyyy h:mm:ss tt",
            "MM/dd/yyyy",
            "MM/dd/yyyy HH:mm",
            "MM/dd/yyyy h:mm tt"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                LogAction("GetDateTime", result.ToString("g"));
                return result;
            }
        }

        ThrowCheckFailed("GetDateTime", $"Could not parse '{text}' as a date/time value.");
        return DateTime.MinValue;
    }

    /// <summary>
    /// Get the date portion only.
    /// </summary>
    public DateTime GetDate()
    {
        var dateTime = GetDateTime();
        LogAction("GetDate", dateTime.ToString("d"));
        return dateTime.Date;
    }

    /// <summary>
    /// Get the time portion only.
    /// </summary>
    public TimeSpan GetTime()
    {
        var dateTime = GetDateTime();
        LogAction("GetTime", dateTime.ToString("T"));
        return dateTime.TimeOfDay;
    }

    /// <summary>
    /// Assert that the date equals expected.
    /// </summary>
    public void AssertDateEquals(DateTime expected)
    {
        var actual = GetDate();
        if (actual != expected.Date)
        {
            ThrowAssertionFailed("DateEquals", actual.ToString("d"), expected.Date.ToString("d"),
                $"DateTimePicker '{AutomationId}' date is {actual:d}, expected {expected:d}.");
        }
        LogAssertPass("DateEquals", actual.ToString("d"), expected.Date.ToString("d"));
    }

    /// <summary>
    /// Assert that the date equals expected, with optional timeout.
    /// </summary>
    public void AssertDateEqualsWait(DateTime expected, int? timeoutMs = null)
    {
        WaitForElement(timeoutMs);
        AssertDateEquals(expected);
    }

    /// <summary>
    /// Assert that the date/time equals expected.
    /// </summary>
    public void AssertDateTimeEquals(DateTime expected)
    {
        var actual = GetDateTime();
        // Compare with 1 second tolerance for potential timing differences
        if (Math.Abs((actual - expected).TotalSeconds) > 1)
        {
            ThrowAssertionFailed("DateTimeEquals", actual.ToString("g"), expected.ToString("g"),
                $"DateTimePicker '{AutomationId}' value is {actual:g}, expected {expected:g}.");
        }
        LogAssertPass("DateTimeEquals", actual.ToString("g"), expected.ToString("g"));
    }

    /// <summary>
    /// Assert that the date/time equals expected, with optional timeout.
    /// </summary>
    public void AssertDateTimeEqualsWait(DateTime expected, int? timeoutMs = null)
    {
        WaitForElement(timeoutMs);
        AssertDateTimeEquals(expected);
    }

    /// <summary>
    /// Assert that the date is after the specified date.
    /// </summary>
    public void AssertDateIsAfter(DateTime date)
    {
        var actual = GetDate();
        if (actual <= date.Date)
        {
            ThrowAssertionFailed("DateIsAfter", actual.ToString("d"), date.Date.ToString("d"),
                $"DateTimePicker '{AutomationId}' date {actual:d} is not after {date:d}.");
        }
        LogAssertPass("DateIsAfter", actual.ToString("d"), $"after {date.Date:d}");
    }

    /// <summary>
    /// Assert that the date is before the specified date.
    /// </summary>
    public void AssertDateIsBefore(DateTime date)
    {
        var actual = GetDate();
        if (actual >= date.Date)
        {
            ThrowAssertionFailed("DateIsBefore", actual.ToString("d"), date.Date.ToString("d"),
                $"DateTimePicker '{AutomationId}' date {actual:d} is not before {date:d}.");
        }
        LogAssertPass("DateIsBefore", actual.ToString("d"), $"before {date.Date:d}");
    }

    /// <summary>
    /// Assert that the date is within range (inclusive).
    /// </summary>
    public void AssertDateInRange(DateTime minDate, DateTime maxDate)
    {
        var actual = GetDate();
        if (actual < minDate.Date || actual > maxDate.Date)
        {
            ThrowAssertionFailed("DateInRange", actual.ToString("d"), $"{minDate:d} to {maxDate:d}",
                $"DateTimePicker '{AutomationId}' date {actual:d} is not in range {minDate:d} to {maxDate:d}.");
        }
        LogAssertPass("DateInRange", actual.ToString("d"), $"{minDate:d} to {maxDate:d}");
    }
}
