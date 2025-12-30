using Brinell.Core.Abstractions;
using Brinell.Stride.Controls.Base;
using Brinell.Stride.Infrastructure;

namespace Brinell.Stride.Controls;

/// <summary>
/// Control object for Stride UI text block (label) controls.
/// </summary>
public class StrideTextBlockControl : StrideTextControlBase
{
    /// <inheritdoc />
    public override bool IsEditable => false;

    /// <summary>
    /// Create a new text block control.
    /// </summary>
    public StrideTextBlockControl(StrideTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Get the displayed text.
    /// </summary>
    public new string GetText() => base.GetText();
}
