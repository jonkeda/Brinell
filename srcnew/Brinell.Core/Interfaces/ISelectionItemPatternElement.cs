namespace Brinell.Core.Interfaces;

/// <summary>
/// Optional platform capability for elements that expose a UI Automation SelectionItem pattern.
/// List-oriented controls use this before falling back to invoke/click behavior.
/// </summary>
public interface ISelectionItemPatternElement
{
    /// <summary>
    /// Gets whether the element supports the selection item pattern.
    /// </summary>
    bool SupportsSelectionItemPattern { get; }

    /// <summary>
    /// Selects the item through the platform pattern.
    /// </summary>
    /// <returns>True when the selection item pattern was available and selected.</returns>
    bool SelectItemPattern();
}
