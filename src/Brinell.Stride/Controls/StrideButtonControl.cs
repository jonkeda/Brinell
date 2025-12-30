using Brinell.Core.Abstractions;
using Brinell.Stride.Controls.Base;
using Brinell.Stride.Infrastructure;

namespace Brinell.Stride.Controls;

/// <summary>
/// Control object for Stride UI button controls.
/// </summary>
public class StrideButtonControl : StrideContentControlBase
{
    /// <summary>
    /// Create a new button control.
    /// </summary>
    public StrideButtonControl(StrideTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }
}
