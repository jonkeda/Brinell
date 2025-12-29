using Brinell.Core.Abstractions;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls;

/// <summary>
/// HTML progress element control wrapper.
/// Works with &lt;progress&gt; elements.
/// </summary>
public class ProgressControl : RangeControlBase
{
    public ProgressControl(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ProgressControl(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the minimum value (always 0 for progress elements).
    /// </summary>
    public override double GetMinimum()
    {
        return 0;
    }

    /// <summary>
    /// Get the maximum value (default 1 for progress elements).
    /// </summary>
    public override double GetMaximum()
    {
        var max = GetAttribute("max");
        return double.TryParse(max, out var result) ? result : 1;
    }

    /// <summary>
    /// Progress is always "enabled" (visible = enabled).
    /// </summary>
    public override bool IsEnabled()
    {
        return IsVisible();
    }

    /// <summary>
    /// Get current value as percentage string (e.g., "75%").
    /// </summary>
    public override string GetText()
    {
        var percentage = GetPercentage();
        return $"{percentage:F0}%";
    }

    /// <summary>
    /// Check if this is an indeterminate progress (no value attribute).
    /// </summary>
    public bool IsIndeterminate()
    {
        var value = GetAttribute("value");
        return string.IsNullOrEmpty(value);
    }
}
