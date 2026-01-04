using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// ProgressBar control implementation for MAUI.
/// Displays a horizontal progress bar.
/// </summary>
public class ProgressBarControl : ProgressControlBase
{
    /// <summary>
    /// Creates a new progress bar control.
    /// </summary>
    public ProgressBarControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new progress bar control using AutomationId.
    /// </summary>
    public ProgressBarControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Gets the progress color.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The progress color value.</returns>
    public virtual string? GetProgressColor(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var color = element.GetAttribute("ProgressColor");
        Log($"GetProgressColor: {color}");
        return color;
    }
}
