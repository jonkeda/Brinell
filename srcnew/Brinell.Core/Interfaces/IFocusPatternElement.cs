namespace Brinell.Core.Interfaces;

/// <summary>
/// Optional platform capability for elements that can be given keyboard focus directly.
/// Controls use this before falling back to a click.
/// </summary>
/// <remarks>
/// Focus is not a UI Automation pattern; it is a direct operation on the element. It earns a
/// capability interface anyway because only some platforms can do it: UIA can focus a control
/// without touching it, while a WebDriver-backed element generally cannot, and the difference
/// decides whether a control has to fall back to the pointer.
/// </remarks>
public interface IFocusPatternElement
{
    /// <summary>
    /// Gets whether the element can be focused without the pointer.
    /// </summary>
    bool SupportsSetFocus { get; }

    /// <summary>
    /// Gives the element keyboard focus.
    /// </summary>
    /// <returns>True when focus was available and applied.</returns>
    bool SetFocus();
}
