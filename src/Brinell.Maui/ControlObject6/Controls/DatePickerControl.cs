using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// DatePicker control implementation for MAUI.
/// Provides date selection through native MAUI DatePicker.
/// </summary>
public class DatePickerControl : DateControlBase
{
    /// <summary>
    /// Creates a new date picker control.
    /// </summary>
    public DatePickerControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new date picker control using AutomationId.
    /// </summary>
    public DatePickerControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Gets the date format used by this picker.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The date format string.</returns>
    public virtual string GetFormat(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var format = element.GetAttribute("Format") ?? "d";
        Log($"GetFormat: {format}");
        return format;
    }
}
