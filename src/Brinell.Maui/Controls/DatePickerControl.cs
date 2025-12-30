using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI DatePicker control wrapper.
/// Provides date selection functionality.
/// </summary>
public class DatePickerControl : ControlBase
{
    public DatePickerControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public DatePickerControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the currently selected date.
    /// </summary>
    public DateTime? GetDate()
    {
        var element = FindElement();
        if (element == null) return null;
        
        var dateStr = element.GetAttribute("date") ?? element.GetAttribute("value") ?? element.Text;
        if (DateTime.TryParse(dateStr, out var result))
            return result;
        
        return null;
    }

    /// <summary>
    /// Set the date.
    /// Note: Platform-specific date picker dialogs may require native automation.
    /// </summary>
    /// <param name="date">The date to set.</param>
    public void SetDate(DateTime date)
    {
        LogAction("SetDate", date.ToShortDateString());
        
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"DatePicker '{AutomationId}' not visible.");
        
        // Open the date picker dialog
        element.Click();
        Thread.Sleep(500); // Wait for picker dialog
        
        // Platform-specific date selection would go here
        Log($"SetDate: Native date picker opened. Date selection requires platform-specific implementation.");
    }

    /// <summary>
    /// Get the minimum allowed date.
    /// </summary>
    public DateTime? GetMinimumDate()
    {
        var element = FindElement();
        if (element == null) return null;
        
        var dateStr = element.GetAttribute("minimumDate") ?? element.GetAttribute("min");
        if (DateTime.TryParse(dateStr, out var result))
            return result;
        
        return null;
    }

    /// <summary>
    /// Get the maximum allowed date.
    /// </summary>
    public DateTime? GetMaximumDate()
    {
        var element = FindElement();
        if (element == null) return null;
        
        var dateStr = element.GetAttribute("maximumDate") ?? element.GetAttribute("max");
        if (DateTime.TryParse(dateStr, out var result))
            return result;
        
        return null;
    }

    #region Assert Methods

    /// <summary>
    /// Assert the selected date.
    /// </summary>
    public void AssertDate(DateTime expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetDate();
        if (actual?.Date != expected.Date)
        {
            ThrowAssertionFailed("Date", actual?.ToShortDateString() ?? "(null)", expected.ToShortDateString(),
                message ?? $"Expected date {expected:d} but got {actual:d}.");
        }
        LogAssertPass("Date", actual?.ToShortDateString() ?? "(null)", expected.ToShortDateString());
    }

    /// <summary>
    /// Assert the date is within a range.
    /// </summary>
    public void AssertDateInRange(DateTime min, DateTime max, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetDate();
        if (actual == null || actual < min || actual > max)
        {
            ThrowAssertionFailed("DateInRange", actual?.ToShortDateString() ?? "(null)", $"[{min:d}, {max:d}]",
                message ?? $"Expected date between {min:d} and {max:d} but got {actual:d}.");
        }
        LogAssertPass("DateInRange", actual?.ToShortDateString() ?? "(null)", $"[{min:d}, {max:d}]");
    }

    #endregion
}
