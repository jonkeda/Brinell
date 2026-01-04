using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for date picker controls in MAUI.
/// Provides common functionality for date selection and picker interaction.
/// </summary>
public abstract class DateControlBase : ControlObjectBase, IDateControlObject
{
    /// <summary>
    /// Creates a new date control.
    /// </summary>
    protected DateControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new date control using AutomationId.
    /// </summary>
    protected DateControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    #region Get/Set Date

    /// <inheritdoc/>
    public virtual DateTime GetDate(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);

        // Try various attributes for date value
        var value = element.GetAttribute("Value.Value")
                   ?? element.GetAttribute("DateTime")
                   ?? element.Text;

        Log($"GetDate: raw value = {value}");
        return DateTime.TryParse(value, out var result) ? result.Date : DateTime.MinValue;
    }

    /// <inheritdoc/>
    public virtual void SetDate(DateTime? date, int? timeoutMs = null)
    {
        if (date is null) return;

        Log($"SetDate({date:yyyy-MM-dd})");
        PerformSetDate(date.Value, timeoutMs);
    }

    /// <summary>
    /// Override to provide platform-specific date setting logic.
    /// Default implementation opens picker and selects year/month/day.
    /// </summary>
    protected virtual void PerformSetDate(DateTime date, int? timeoutMs)
    {
        OpenPicker(timeoutMs);
        SelectYear(date.Year, timeoutMs);
        SelectMonth(date.Month, timeoutMs);
        SelectDay(date.Day, timeoutMs);
        ClosePicker(timeoutMs);
    }

    /// <inheritdoc/>
    public virtual bool WaitDate(DateTime? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.Now.AddMilliseconds(timeout);

        while (DateTime.Now < deadline)
        {
            try
            {
                var current = GetDate(timeoutMs);
                if (current.Date == expected.Value.Date)
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
    public virtual void AssertDate(DateTime? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetDate(timeoutMs);
        if (actual.Date != expected.Value.Date)
        {
            var msg = message ?? $"Expected date {expected:yyyy-MM-dd} but was {actual:yyyy-MM-dd}";
            throw new AssertionException(msg, Locator.Value, "AssertDate");
        }
    }

    /// <inheritdoc/>
    public virtual void AssertDateInRange(DateTime? min, DateTime? max, string? message = null, int? timeoutMs = null)
    {
        var actual = GetDate(timeoutMs);

        if (min.HasValue && actual.Date < min.Value.Date)
        {
            var msg = message ?? $"Date {actual:yyyy-MM-dd} is less than minimum {min:yyyy-MM-dd}";
            throw new AssertionException(msg, Locator.Value, "AssertDateInRange");
        }

        if (max.HasValue && actual.Date > max.Value.Date)
        {
            var msg = message ?? $"Date {actual:yyyy-MM-dd} is greater than maximum {max:yyyy-MM-dd}";
            throw new AssertionException(msg, Locator.Value, "AssertDateInRange");
        }
    }

    #endregion

    #region Date Components

    /// <summary>
    /// Selects a specific year in the picker.
    /// </summary>
    /// <param name="year">The year to select.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    public virtual void SelectYear(int? year, int? timeoutMs = null)
    {
        if (year is null) return;
        Log($"SelectYear({year})");
        // Platform-specific implementation needed - override in derived classes
    }

    /// <summary>
    /// Selects a specific month in the picker.
    /// </summary>
    /// <param name="month">The month to select (1-12).</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    public virtual void SelectMonth(int? month, int? timeoutMs = null)
    {
        if (month is null) return;
        Log($"SelectMonth({month})");
        // Platform-specific implementation needed - override in derived classes
    }

    /// <summary>
    /// Selects a specific day in the picker.
    /// </summary>
    /// <param name="day">The day to select (1-31).</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    public virtual void SelectDay(int? day, int? timeoutMs = null)
    {
        if (day is null) return;
        Log($"SelectDay({day})");
        // Platform-specific implementation needed - override in derived classes
    }

    #endregion

    #region Date Range

    /// <inheritdoc/>
    public virtual DateTime GetMinDate(int? timeoutMs = null)
    {
        var element = FindElement();
        if (element == null)
            return DateTime.MinValue;

        var value = element.GetAttribute("MinimumDate")
                   ?? element.GetAttribute("Minimum");

        return DateTime.TryParse(value, out var result) ? result.Date : DateTime.MinValue;
    }

    /// <inheritdoc/>
    public virtual DateTime GetMaxDate(int? timeoutMs = null)
    {
        var element = FindElement();
        if (element == null)
            return DateTime.MaxValue;

        var value = element.GetAttribute("MaximumDate")
                   ?? element.GetAttribute("Maximum");

        return DateTime.TryParse(value, out var result) ? result.Date : DateTime.MaxValue;
    }

    #endregion

    #region Picker

    /// <inheritdoc/>
    public virtual bool IsPickerOpen(int? timeoutMs = null)
    {
        try
        {
            // Look for common MAUI date picker popup elements
            var popup = Driver.FindElement(MobileBy.ClassName("CalendarDatePicker"))
                     ?? Driver.FindElement(MobileBy.ClassName("DatePickerFlyout"));
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
