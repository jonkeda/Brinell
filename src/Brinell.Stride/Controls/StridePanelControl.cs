using Brinell.Core.Abstractions;
using Brinell.Stride.Controls.Base;
using Brinell.Stride.Infrastructure;

namespace Brinell.Stride.Controls;

/// <summary>
/// Control object for Stride UI panel/grid containers.
/// </summary>
public class StridePanelControl : StrideControlBase
{
    /// <summary>
    /// Create a new panel control.
    /// </summary>
    public StridePanelControl(StrideTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Click anywhere on the panel.
    /// </summary>
    public void Click()
    {
        CheckVisible();
        Context.ClickElement(_automationId);
        LogAction("Click");
    }

    /// <summary>
    /// Click at specific offset within panel.
    /// </summary>
    public void ClickAt(int offsetX, int offsetY)
    {
        CheckVisible();
        var bounds = GetBounds();
        Context.Input.Click(bounds.X + offsetX, bounds.Y + offsetY);
        LogAction("ClickAt", $"{offsetX},{offsetY}");
    }

    /// <summary>
    /// Get child element count if available.
    /// </summary>
    public int GetChildCount()
    {
        var state = GetState();
        return state.Items?.Count ?? 0;
    }
}
