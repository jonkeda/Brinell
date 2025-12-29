using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI Entry (text input) control wrapper.
/// Corresponds to MAUI Entry, Editor, and SearchBar controls.
/// Inherits from TextControlBase for standard text input behavior.
/// </summary>
public class EntryControl : TextControlBase
{
    public EntryControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public EntryControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }
}
