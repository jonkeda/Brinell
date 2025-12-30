using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI TimePicker control wrapper.
/// Provides time selection functionality.
/// </summary>
public class TimePickerControl : ControlBase
{
    public TimePickerControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public TimePickerControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the currently selected time.
    /// </summary>
    public TimeSpan? GetTime()
    {
        var element = FindElement();
        if (element == null) return null;
        
        var timeStr = element.GetAttribute("time") ?? element.GetAttribute("value") ?? element.Text;
        if (TimeSpan.TryParse(timeStr, out var result))
            return result;
        if (DateTime.TryParse(timeStr, out var dt))
            return dt.TimeOfDay;
        
        return null;
    }

    /// <summary>
    /// Set the time.
    /// Note: Platform-specific time picker dialogs may require native automation.
    /// </summary>
    /// <param name="time">The time to set.</param>
    public void SetTime(TimeSpan time)
    {
        LogAction("SetTime", time.ToString(@"hh\:mm"));
        
        var element = WaitForElementVisible();
        if (element == null)
            throw new InvalidOperationException($"TimePicker '{AutomationId}' not visible.");
        
        // Open the time picker dialog
        element.Click();
        Thread.Sleep(500); // Wait for picker dialog
        
        // Platform-specific time selection would go here
        Log($"SetTime: Native time picker opened. Time selection requires platform-specific implementation.");
    }

    /// <summary>
    /// Set time using hour and minute.
    /// </summary>
    public void SetTime(int hours, int minutes)
    {
        SetTime(new TimeSpan(hours, minutes, 0));
    }

    #region Assert Methods

    /// <summary>
    /// Assert the selected time.
    /// </summary>
    public void AssertTime(TimeSpan expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetTime();
        // Compare hours and minutes only
        if (actual?.Hours != expected.Hours || actual?.Minutes != expected.Minutes)
        {
            ThrowAssertionFailed("Time", actual?.ToString(@"hh\:mm") ?? "(null)", expected.ToString(@"hh\:mm"),
                message ?? $"Expected time {expected:hh\\:mm} but got {actual:hh\\:mm}.");
        }
        LogAssertPass("Time", actual?.ToString(@"hh\:mm") ?? "(null)", expected.ToString(@"hh\:mm"));
    }

    /// <summary>
    /// Assert the selected time using hours and minutes.
    /// </summary>
    public void AssertTime(int expectedHours, int expectedMinutes, string? message = null)
    {
        AssertTime(new TimeSpan(expectedHours, expectedMinutes, 0), message);
    }

    #endregion
}
