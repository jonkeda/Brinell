using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Image control implementation for MAUI.
/// Displays images with support for various sources.
/// </summary>
public class ImageControl : ImageControlBase
{
    /// <summary>
    /// Creates a new image control.
    /// </summary>
    public ImageControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new image control using AutomationId.
    /// </summary>
    public ImageControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// Gets the aspect ratio setting of the image.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>The aspect ratio value (e.g., "AspectFit", "AspectFill", "Fill").</returns>
    public virtual string? GetAspect(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var aspect = element.GetAttribute("Aspect");
        Log($"GetAspect: {aspect}");
        return aspect;
    }

    /// <summary>
    /// Checks if the image is opaque.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if opaque.</returns>
    public virtual bool IsOpaque(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var isOpaque = element.GetAttribute("IsOpaque");
        return isOpaque == "True" || isOpaque == "true";
    }
}
