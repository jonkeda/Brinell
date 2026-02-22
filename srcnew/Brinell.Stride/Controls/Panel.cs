using Brinell.Stride.Communication;
using Brinell.Stride.Interfaces;

namespace Brinell.Stride.Controls;

/// <summary>
/// Panel/grid container control for Stride UI.
/// </summary>
public class Panel<TScope> : ClickableControlBase<TScope>
    where TScope : IStrideScope<TScope>
{
    public Panel(IStrideScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    /// <summary>
    /// Click at specific offset within panel. Not supported — use named child elements instead.
    /// </summary>
    public TScope ClickAt(int offsetX, int offsetY)
    {
        throw new NotSupportedException("Coordinate-based clicking is not supported in Stride UI automation. Use named child elements instead.");
    }

    /// <summary>
    /// Get child element count if available.
    /// </summary>
    public int GetChildCount() => GetState().Items?.Count ?? 0;
}
