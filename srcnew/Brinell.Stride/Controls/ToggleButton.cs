using Brinell.Stride.Interfaces;

namespace Brinell.Stride.Controls;

/// <summary>
/// Toggle button control for Stride UI.
/// </summary>
public class ToggleButton<TScope> : ToggleControlBase<TScope>
    where TScope : IStrideScope<TScope>
{
    public ToggleButton(IStrideScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }
}
