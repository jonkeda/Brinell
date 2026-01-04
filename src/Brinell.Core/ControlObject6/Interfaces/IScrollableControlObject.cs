namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for scrollable controls.
/// </summary>
public interface IScrollableControlObject : IControlObject
{
    /// <summary>
    /// Gets the current scroll position as (horizontal, vertical) percentages.
    /// </summary>
    (double horizontal, double vertical) GetScrollPosition(int? timeoutMs = null);

    /// <summary>
    /// Checks if horizontal scrolling is enabled.
    /// </summary>
    bool CanScrollHorizontally(int? timeoutMs = null);

    /// <summary>
    /// Checks if vertical scrolling is enabled.
    /// </summary>
    bool CanScrollVertically(int? timeoutMs = null);

    /// <summary>
    /// Scrolls to the specified position.
    /// </summary>
    void ScrollTo(double? horizontalPercent, double? verticalPercent, int? timeoutMs = null);

    /// <summary>
    /// Scrolls to the top.
    /// </summary>
    void ScrollToTop(int? timeoutMs = null);

    /// <summary>
    /// Scrolls to the bottom.
    /// </summary>
    void ScrollToBottom(int? timeoutMs = null);

    /// <summary>
    /// Scrolls to the left.
    /// </summary>
    void ScrollToLeft(int? timeoutMs = null);

    /// <summary>
    /// Scrolls to the right.
    /// </summary>
    void ScrollToRight(int? timeoutMs = null);

    /// <summary>
    /// Scrolls up by the specified amount.
    /// </summary>
    void ScrollUp(double? amount = null, int? timeoutMs = null);

    /// <summary>
    /// Scrolls down by the specified amount.
    /// </summary>
    void ScrollDown(double? amount = null, int? timeoutMs = null);

    /// <summary>
    /// Scrolls left by the specified amount.
    /// </summary>
    void ScrollLeft(double? amount = null, int? timeoutMs = null);

    /// <summary>
    /// Scrolls right by the specified amount.
    /// </summary>
    void ScrollRight(double? amount = null, int? timeoutMs = null);

    /// <summary>
    /// Scrolls until the specified control is visible.
    /// </summary>
    void ScrollToElement(IControlObject? control, int? timeoutMs = null);

    /// <summary>
    /// Waits for scrolling to complete.
    /// </summary>
    bool WaitScrollComplete(int? timeoutMs = null);
}
