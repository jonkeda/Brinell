using Brinell.Stride.Interfaces;

namespace Brinell.Stride.Controls;

/// <summary>
/// Text block (read-only label) control for Stride UI.
/// </summary>
public class TextBlock<TScope> : TextControlBase<TScope>
    where TScope : IStrideScope<TScope>
{
    public override bool IsEditable => false;

    public TextBlock(IStrideScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }
}
