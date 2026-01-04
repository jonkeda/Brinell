using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Entry/TextBox control implementation for MAUI.
/// Inherits virtual text input capabilities from TextControlBase.
/// </summary>
public class EntryControl : TextControlBase
{
    /// <summary>
    /// Creates a new entry control.
    /// </summary>
    public EntryControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new entry control using AutomationId.
    /// </summary>
    public EntryControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    // All text input methods are inherited from TextControlBase as virtual methods
    // Override if MAUI Entry-specific behavior is needed
}
