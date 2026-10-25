using Bruinell.Stride.Interfaces;

namespace Bruinell.Stride.Controls;

/// <summary>
/// Button control for Stride UI.
/// </summary>
public class Button<TScope> : ContentControlBase<TScope>
    where TScope : IStrideScope<TScope>
{
    public Button(IStrideScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }
}
