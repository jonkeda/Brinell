using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// MAUI base class for clickable content controls (buttons, labels).
/// Note: Click, DoubleTap, and LongPress are inherited from ControlBase.
/// </summary>
public abstract class ContentControlBase : ControlBase, IContentControl
{
    protected ContentControlBase(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected ContentControlBase(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Double-click (tap twice on mobile). Alias for DoubleTap.
    /// </summary>
    public virtual void DoubleClick()
    {
        DoubleTap();
    }

    /// <summary>
    /// Right-click. On mobile, this performs a long press.
    /// </summary>
    public virtual void RightClick()
    {
        LongPress();
    }
}
