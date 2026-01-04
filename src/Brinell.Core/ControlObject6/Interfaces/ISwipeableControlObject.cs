namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Interface for swipeable controls.
/// </summary>
public interface ISwipeableControlObject : IControlObject
{
    /// <summary>
    /// Performs a swipe left gesture.
    /// </summary>
    void SwipeLeft(int? timeoutMs = null);

    /// <summary>
    /// Performs a swipe right gesture.
    /// </summary>
    void SwipeRight(int? timeoutMs = null);

    /// <summary>
    /// Performs a swipe up gesture.
    /// </summary>
    void SwipeUp(int? timeoutMs = null);

    /// <summary>
    /// Performs a swipe down gesture.
    /// </summary>
    void SwipeDown(int? timeoutMs = null);

    /// <summary>
    /// Gets whether left swipe items are revealed.
    /// </summary>
    bool IsLeftSwipeRevealed(int? timeoutMs = null);

    /// <summary>
    /// Gets whether right swipe items are revealed.
    /// </summary>
    bool IsRightSwipeRevealed(int? timeoutMs = null);

    /// <summary>
    /// Closes the swipe, hiding revealed items.
    /// </summary>
    void CloseSwipe(int? timeoutMs = null);
}
