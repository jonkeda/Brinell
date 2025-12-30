using Brinell.Core.Abstractions;
using Brinell.Stride.Controls.Base;
using Brinell.Stride.Infrastructure;

namespace Brinell.Stride.Controls;

/// <summary>
/// Control object for Stride UI toggle button controls.
/// </summary>
public class StrideToggleButtonControl : StrideToggleControlBase
{
    /// <summary>
    /// Create a new toggle button control.
    /// </summary>
    public StrideToggleButtonControl(StrideTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }
}
