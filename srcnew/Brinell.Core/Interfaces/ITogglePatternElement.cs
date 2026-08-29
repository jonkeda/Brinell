namespace Brinell.Core.Interfaces;

/// <summary>
/// Optional platform capability for elements that expose a UI Automation Toggle pattern.
/// </summary>
public interface ITogglePatternElement
{
    /// <summary>
    /// Gets whether the element supports the Toggle UI Automation pattern.
    /// </summary>
    bool SupportsTogglePattern { get; }

    /// <summary>
    /// Gets the current Toggle pattern state when available.
    /// </summary>
    bool? IsTogglePatternChecked();

    /// <summary>
    /// Toggles the element through the platform Toggle pattern.
    /// </summary>
    bool TogglePattern();

    /// <summary>
    /// Sets the element to a requested state through the Toggle pattern when possible.
    /// </summary>
    bool SetToggleStatePattern(bool isChecked);
}
