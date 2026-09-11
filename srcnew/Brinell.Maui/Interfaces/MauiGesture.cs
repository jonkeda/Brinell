namespace Brinell.Maui.Interfaces;

/// <summary>
/// A gesture a test can ask an element to perform, in the test author's terms.
/// </summary>
/// <remarks>
/// <para>
/// <b>Deliberately not the wire enum.</b> On Windows a gesture travels as a numbered verb
/// through a custom UI Automation pattern, and those numbers are a frozen contract shared with
/// the app under test. On Android and iOS the same gesture is real touch input and no such
/// number exists. Tying the test-facing API to one platform's wire format would leak that
/// platform into every shared test and make the numbers impossible to change independently.
/// </para>
/// <para>
/// The mapping to Windows verb numbers lives in the FlaUI driver, which is the only place that
/// knows about either.
/// </para>
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
