using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// TabBar control for MAUI (Shell TabBar).
/// </summary>
public class TabBarControl : TabControlBase
{
    /// <summary>
    /// Creates a new TabBar control.
    /// </summary>
    public TabBarControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new TabBar control using AutomationId.
    /// </summary>
    public TabBarControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc/>
    protected override string TabXPath => ".//*[@ClassName='ShellTab' or @ClassName='TabBarItem' or contains(@ClassName,'TabBar')]";
}
