using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Stride.Infrastructure;

namespace Brinell.Stride.Controls.Base;

/// <summary>
/// Base class for clickable content controls (buttons, etc.).
/// </summary>
public abstract class StrideContentControlBase : StrideControlBase, IContentControl
{
    /// <summary>
    /// Create a new content control.
    /// </summary>
    protected StrideContentControlBase(StrideTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <inheritdoc />
    public virtual void Click()
    {
        CheckClickable();
        Context.ClickElement(_automationId);
        LogAction("Click");
    }

    /// <inheritdoc />
    public virtual void DoubleClick()
    {
        CheckClickable();
        var bounds = GetBounds();
        Context.Input.DoubleClick(bounds.CenterX, bounds.CenterY);
        LogAction("DoubleClick");
    }

    /// <inheritdoc />
    public virtual void RightClick()
    {
        CheckClickable();
        var bounds = GetBounds();
        Context.Input.RightClick(bounds.CenterX, bounds.CenterY);
        LogAction("RightClick");
    }

    /// <summary>
    /// Hover over the control without clicking.
    /// </summary>
    public virtual void Hover()
    {
        CheckVisible();
        var bounds = GetBounds();
        Context.MoveMouse(bounds.CenterX, bounds.CenterY);
        LogAction("Hover");
    }

    /// <summary>
    /// Click and wait for a condition.
    /// </summary>
    public void ClickAndWait(Func<bool> condition, int? timeoutMs = null)
    {
        Click();

        if (!Context.WaitFor(condition, timeoutMs, "post-click condition"))
        {
            throw new TimeoutException($"Condition not met after clicking '{AutomationId}'");
        }
    }

    /// <summary>
    /// Try to click if clickable, returns false if not clickable.
    /// </summary>
    public bool TryClick()
    {
        if (IsClickable())
        {
            Click();
            return true;
        }
        return false;
    }
}
