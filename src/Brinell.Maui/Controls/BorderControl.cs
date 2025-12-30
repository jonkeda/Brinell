using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Border control wrapper.
/// Provides a bordered container with customizable stroke.
/// </summary>
public class BorderControl : ContentControlBase
{
    public BorderControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public BorderControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the stroke thickness.
    /// </summary>
    public double GetStrokeThickness()
    {
        var element = FindElement();
        if (element != null)
        {
            var thickness = element.GetAttribute("strokeThickness");
            if (double.TryParse(thickness, out var result))
                return result;
        }
        return 0;
    }

    /// <summary>
    /// Get the stroke color.
    /// </summary>
    public string? GetStroke()
    {
        var element = FindElement();
        return element?.GetAttribute("stroke");
    }

    /// <summary>
    /// Get the stroke shape.
    /// </summary>
    public string? GetStrokeShape()
    {
        var element = FindElement();
        return element?.GetAttribute("strokeShape");
    }
}
