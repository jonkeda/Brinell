using Brinell.Stride.Interfaces;

namespace Brinell.Stride.Controls;

/// <summary>
/// Checkbox control for Stride UI.
/// </summary>
public class CheckBox<TScope> : ToggleControlBase<TScope>
    where TScope : IStrideScope<TScope>
{
    public CheckBox(IStrideScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }
}
