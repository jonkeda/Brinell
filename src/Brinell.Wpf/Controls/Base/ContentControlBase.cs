using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Wpf.Infrastructure;

namespace Brinell.Wpf.Controls.Base;

/// <summary>
/// WPF base class for controls that display content and can be clicked.
/// </summary>
public abstract class ContentControlBase : ControlBase, IContentControl
{
    protected ContentControlBase(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a control that searches within a container element.
    /// </summary>
    protected ContentControlBase(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected ContentControlBase(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Click the control.
    /// </summary>
    public virtual void Click()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("Click", $"Element '{AutomationId}' not visible for click.");
        }
        element!.Click();
        LogAction("Click");
    }

    /// <summary>
    /// Double-click the control.
    /// </summary>
    public virtual void DoubleClick()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("DoubleClick", $"Element '{AutomationId}' not visible for double-click.");
        }
        element!.DoubleClick();
        LogAction("DoubleClick");
    }

    /// <summary>
    /// Right-click the control.
    /// </summary>
    public virtual void RightClick()
    {
        var element = WaitForElementVisible();
        if (element == null)
        {
            ThrowCheckFailed("RightClick", $"Element '{AutomationId}' not visible for right-click.");
        }
        element!.RightClick();
        LogAction("RightClick");
    }
}
