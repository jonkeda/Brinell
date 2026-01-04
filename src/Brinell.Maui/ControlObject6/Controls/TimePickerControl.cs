using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// TimePicker control implementation for MAUI.
/// Provides time selection through native MAUI TimePicker.
/// </summary>
public class TimePickerControl : TimeControlBase
{
    /// <summary>
    /// Creates a new time picker control.
    /// </summary>
    public TimePickerControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new time picker control using AutomationId.
    /// </summary>
    public TimePickerControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Gets the time format used by this picker.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The time format string.</returns>
    public virtual string GetFormat(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var format = element.GetAttribute("Format") ?? "t";
        Log($"GetFormat: {format}");
        return format;
    }
}
