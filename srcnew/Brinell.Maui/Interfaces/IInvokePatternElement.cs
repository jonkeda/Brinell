namespace Brinell.Maui.Interfaces;

/// <summary>
/// Optional platform capability for elements that expose a UI Automation Invoke pattern.
/// Controls use this before falling back to a generic click.
/// </summary>
public interface IInvokePatternElement
{
    /// <summary>
    /// Gets whether the element supports the invoke pattern.
    /// </summary>
    bool SupportsInvokePattern { get; }

    /// <summary>
    /// Invokes the element through the platform pattern.
    /// </summary>
    /// <returns>True when the invoke pattern was available and invoked.</returns>
    bool InvokePattern();
}
