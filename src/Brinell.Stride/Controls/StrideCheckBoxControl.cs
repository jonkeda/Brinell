using Brinell.Core.Abstractions;
using Brinell.Stride.Controls.Base;
using Brinell.Stride.Infrastructure;

namespace Brinell.Stride.Controls;

/// <summary>
/// Control object for Stride UI checkbox controls.
/// </summary>
public class StrideCheckBoxControl : StrideToggleControlBase
{
    /// <summary>
    /// Create a new checkbox control.
    /// </summary>
    public StrideCheckBoxControl(StrideTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }
}
