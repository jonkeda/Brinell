using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Control object for MAUI Label elements.
/// Read-only text display control.
/// </summary>
public class LabelControl : ControlObjectBase
{
    /// <summary>
    /// Creates a new LabelControl.
    /// </summary>
    public LabelControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new LabelControl using AutomationId.
    /// </summary>
    public LabelControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc />
    public override string GetText(int? timeoutMs = null)
    {
        Log("GetText()");
        var element = FindElementRequired(timeoutMs);
        
        // For Label, try Text attribute first
        var text = element.GetAttribute("Text");
        if (text is not null)
            return text;

        // Fall back to element.Text
        return element.Text ?? string.Empty;
    }
}
