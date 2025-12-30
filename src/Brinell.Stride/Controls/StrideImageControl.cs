using Brinell.Core.Abstractions;
using Brinell.Stride.Controls.Base;
using Brinell.Stride.Infrastructure;

namespace Brinell.Stride.Controls;

/// <summary>
/// Control object for Stride UI image controls.
/// </summary>
public class StrideImageControl : StrideControlBase
{
    /// <summary>
    /// Create a new image control.
    /// </summary>
    public StrideImageControl(StrideTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Click on the image.
    /// </summary>
    public void Click()
    {
        CheckVisible();
        Context.ClickElement(_automationId);
        LogAction("Click");
    }

    /// <summary>
    /// Get image source or name if available.
    /// </summary>
    public string GetSource()
    {
        var state = GetState();
        return state.Text ?? string.Empty;
    }
}
