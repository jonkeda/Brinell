namespace Brinell.Maui.Interfaces;

/// <summary>
/// A gesture a test can ask an element to perform, in the test author's terms.
/// </summary>
/// <remarks>
/// Independent of how each platform carries the gesture: a verb through the UI Automation bridge
/// on Windows, real touch input on Android and iOS.
/// </remarks>
public enum MauiGesture
{
    /// <summary>A single tap or click.</summary>
    Tap,

    /// <summary>Two taps in quick succession.</summary>
    DoubleTap,

    /// <summary>A press held long enough to be a distinct gesture.</summary>
    LongPress,

    /// <summary>A swipe towards the left edge.</summary>
    SwipeLeft,

    /// <summary>A swipe towards the right edge.</summary>
    SwipeRight,

    /// <summary>A swipe towards the top edge.</summary>
    SwipeUp,

    /// <summary>A swipe towards the bottom edge.</summary>
    SwipeDown,

    /// <summary>A drag by a given offset.</summary>
    Pan,

    /// <summary>A two-finger scale.</summary>
    Pinch,
}
