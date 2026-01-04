namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for controls that can be clicked.
/// Provides click, double-click, right-click, and hover actions.
/// </summary>
public interface IClickableControlObject : IInteractiveControlObject
{
    /// <summary>
    /// Clicks the element.
    /// Waits for element to be visible and enabled before clicking.
    /// </summary>
    /// <param name="timeoutMs">Timeout for element to be clickable.</param>
    void Click(int? timeoutMs = null);

    /// <summary>
    /// Double-clicks the element.
    /// Waits for element to be visible and enabled before clicking.
    /// </summary>
    /// <param name="timeoutMs">Timeout for element to be clickable.</param>
    void DoubleClick(int? timeoutMs = null);

    /// <summary>
    /// Right-clicks the element (context menu click).
    /// Waits for element to be visible and enabled before clicking.
    /// </summary>
    /// <param name="timeoutMs">Timeout for element to be clickable.</param>
    void RightClick(int? timeoutMs = null);

    /// <summary>
    /// Hovers the mouse over the element.
    /// Waits for element to be visible before hovering.
    /// </summary>
    /// <param name="timeoutMs">Timeout for element to be visible.</param>
    void Hover(int? timeoutMs = null);

    /// <summary>
    /// Performs a long press on the element (mobile gesture).
    /// Waits for element to be visible and enabled before pressing.
    /// </summary>
    /// <param name="durationMs">Duration of the press in milliseconds.</param>
    /// <param name="timeoutMs">Timeout for element to be ready.</param>
    void LongPress(int? durationMs = null, int? timeoutMs = null);
}
