using Brinell.Stride.Interfaces;

namespace Brinell.Stride.Controls;

/// <summary>
/// Image control for Stride UI.
/// </summary>
public class Image<TScope> : ClickableControlBase<TScope>
    where TScope : IStrideScope<TScope>
{
    public Image(IStrideScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    /// <summary>
    /// Get image source or name if available.
    /// </summary>
    public string GetSource() => GetState().Text ?? string.Empty;
}
