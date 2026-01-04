using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// ActivityIndicator control implementation for MAUI.
/// Displays a loading spinner/indicator.
/// </summary>
public class ActivityIndicatorControl : ActivityIndicatorControlBase
{
    /// <summary>
    /// Creates a new activity indicator control.
    /// </summary>
    public ActivityIndicatorControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new activity indicator control using AutomationId.
    /// </summary>
    public ActivityIndicatorControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Gets the indicator color.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The indicator color value.</returns>
    public virtual string? GetColor(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var color = element.GetAttribute("Color");
        Log($"GetColor: {color}");
        return color;
    }
}
