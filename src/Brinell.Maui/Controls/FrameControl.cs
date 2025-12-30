using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Frame control wrapper.
/// Provides a bordered container with shadow.
/// </summary>
public class FrameControl : ContentControlBase
{
    public FrameControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public FrameControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Check if shadow is enabled.
    /// </summary>
    public bool HasShadow()
    {
        var element = FindElement();
        var hasShadow = element?.GetAttribute("hasShadow");
        return hasShadow?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// Get the corner radius.
    /// </summary>
    public double GetCornerRadius()
    {
        var element = FindElement();
        if (element != null)
        {
            var radius = element.GetAttribute("cornerRadius");
            if (double.TryParse(radius, out var result))
                return result;
        }
        return 0;
    }

    /// <summary>
    /// Get the border color.
    /// </summary>
    public string? GetBorderColor()
    {
        var element = FindElement();
        return element?.GetAttribute("borderColor");
    }
}
