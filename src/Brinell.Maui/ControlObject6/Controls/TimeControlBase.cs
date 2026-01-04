using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for time picker controls in MAUI.
/// Provides common functionality for time selection and picker interaction.
/// </summary>
public abstract class TimeControlBase : ControlObjectBase, ITimeControlObject
{
    /// <summary>
    /// Creates a new time control.
    /// </summary>
    protected TimeControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new time control using AutomationId.
    /// </summary>
    protected TimeControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    #region Get/Set Time

    /// <inheritdoc/>
    public virtual TimeSpan GetTime(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);

        // Try various attributes for time value
        var value = element.GetAttribute("Value.Value")
                   ?? element.GetAttribute("Time")
                   ?? element.Text;

        Log($"GetTime: raw value = {value}");
        return TimeSpan.TryParse(value, out var result) ? result : TimeSpan.Zero;
    }

    /// <inheritdoc/>
    public virtual void SetTime(TimeSpan? time, int? timeoutMs = null)
    {
        if (time is null) return;

        Log($"SetTime({time})");
        PerformSetTime(time.Value, timeoutMs);
    }

    /// <summary>
    /// Override to provide platform-specific time setting logic.
    /// Default implementation opens picker and selects hour/minute.
    /// </summary>
    protected virtual void PerformSetTime(TimeSpan time, int? timeoutMs)
    {
        OpenPicker(timeoutMs);
        SelectHour(time.Hours, timeoutMs);
        SelectMinute(time.Minutes, timeoutMs);
        if (time.Seconds > 0)
        {
            SelectSecond(time.Seconds, timeoutMs);
        }
        ClosePicker(timeoutMs);
    }

    /// <inheritdoc/>
    public virtual bool WaitTime(TimeSpan? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.Now.AddMilliseconds(timeout);

        while (DateTime.Now < deadline)
        {
            try
            {
                var current = GetTime(timeoutMs);
                // Compare hours and minutes (ignore seconds for most cases)
                if (current.Hours == expected.Value.Hours && current.Minutes == expected.Value.Minutes)
                    return true;
            }
            catch
            {
                // Element not found yet, keep trying
            }

            Thread.Sleep(DefaultPollingIntervalMs);
        }

        return false;
    }

    /// <inheritdoc/>
    public virtual void AssertTime(TimeSpan? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetTime(timeoutMs);
        if (actual.Hours != expected.Value.Hours || actual.Minutes != expected.Value.Minutes)
        {
            var msg = message ?? $"Expected time {expected:hh\\:mm} but was {actual:hh\\:mm}";
            throw new AssertionException(msg, Locator.Value, "AssertTime");
        }
    }

    /// <inheritdoc/>
    public virtual void AssertTimeInRange(TimeSpan? min, TimeSpan? max, string? message = null, int? timeoutMs = null)
    {
        var actual = GetTime(timeoutMs);

        if (min.HasValue && actual < min.Value)
        {
            var msg = message ?? $"Time {actual:hh\\:mm} is less than minimum {min:hh\\:mm}";
            throw new AssertionException(msg, Locator.Value, "AssertTimeInRange");
        }

        if (max.HasValue && actual > max.Value)
        {
            var msg = message ?? $"Time {actual:hh\\:mm} is greater than maximum {max:hh\\:mm}";
            throw new AssertionException(msg, Locator.Value, "AssertTimeInRange");
        }
    }

    #endregion

    #region Time Components

    /// <summary>
    /// Selects a specific hour in the picker.
    /// </summary>
    /// <param name="hour">The hour to select (0-23).</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    public virtual void SelectHour(int? hour, int? timeoutMs = null)
    {
        if (hour is null) return;
        Log($"SelectHour({hour})");
        // Platform-specific implementation needed - override in derived classes
    }

    /// <summary>
    /// Selects a specific minute in the picker.
    /// </summary>
    /// <param name="minute">The minute to select (0-59).</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    public virtual void SelectMinute(int? minute, int? timeoutMs = null)
    {
        if (minute is null) return;
        Log($"SelectMinute({minute})");
        // Platform-specific implementation needed - override in derived classes
    }

    /// <summary>
    /// Selects a specific second in the picker.
    /// </summary>
    /// <param name="second">The second to select (0-59).</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    public virtual void SelectSecond(int? second, int? timeoutMs = null)
    {
        if (second is null) return;
        Log($"SelectSecond({second})");
        // Platform-specific implementation needed - override in derived classes
    }

    #endregion

    #region Time Range

    /// <inheritdoc/>
    public virtual TimeSpan GetMinTime(int? timeoutMs = null)
    {
        var element = FindElement();
        if (element == null)
            return TimeSpan.Zero;

        var value = element.GetAttribute("MinimumTime")
                   ?? element.GetAttribute("Minimum");

        return TimeSpan.TryParse(value, out var result) ? result : TimeSpan.Zero;
    }

    /// <inheritdoc/>
    public virtual TimeSpan GetMaxTime(int? timeoutMs = null)
    {
        var element = FindElement();
        if (element == null)
            return new TimeSpan(23, 59, 59);

        var value = element.GetAttribute("MaximumTime")
                   ?? element.GetAttribute("Maximum");

        return TimeSpan.TryParse(value, out var result) ? result : new TimeSpan(23, 59, 59);
    }

    #endregion

    #region Picker

    /// <inheritdoc/>
    public virtual bool IsPickerOpen(int? timeoutMs = null)
    {
        try
        {
            // Look for common MAUI time picker popup elements
            var popup = Driver.FindElement(MobileBy.ClassName("TimePickerFlyout"))
                     ?? Driver.FindElement(MobileBy.ClassName("TimePicker"));
            return popup?.Displayed ?? false;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public virtual void OpenPicker(int? timeoutMs = null)
    {
        if (IsPickerOpen(timeoutMs))
            return;

        Log("OpenPicker");
        var element = FindElement();
        element?.Click();

        // Wait for picker to open
        var deadline = DateTime.Now.AddMilliseconds(timeoutMs ?? DefaultTimeoutMs);
        while (DateTime.Now < deadline && !IsPickerOpen())
        {
            Thread.Sleep(DefaultPollingIntervalMs);
        }
    }

    /// <inheritdoc/>
    public virtual void ClosePicker(int? timeoutMs = null)
    {
        if (!IsPickerOpen(timeoutMs))
            return;

        Log("ClosePicker");
        // Try clicking outside or pressing escape
        try
        {
            var actions = new OpenQA.Selenium.Interactions.Actions(Driver);
            actions.SendKeys(OpenQA.Selenium.Keys.Escape).Perform();
        }
        catch
        {
            // Fallback: click on the control itself
            FindElement()?.Click();
        }
    }

    #endregion
}
