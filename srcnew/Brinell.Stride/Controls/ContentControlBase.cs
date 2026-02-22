using Brinell.Stride.Interfaces;

namespace Brinell.Stride.Controls;

/// <summary>
/// Base class for clickable content controls (buttons with text content).
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent method chaining.</typeparam>
public abstract class ContentControlBase<TScope> : ClickableControlBase<TScope>
    where TScope : IStrideScope<TScope>
{
    protected ContentControlBase(IStrideScope<TScope> scope, string automationId)
        : base(scope, automationId)
    {
    }

    /// <summary>
    /// Get the content/text of this control.
    /// </summary>
    public string GetContent() => GetText() ?? string.Empty;
}
