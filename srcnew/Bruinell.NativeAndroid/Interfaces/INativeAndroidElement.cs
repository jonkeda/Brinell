namespace Bruinell.NativeAndroid.Interfaces;

// R9: NativeAndroid owns this contract and does not derive from Core's IElement<T>; lookups make
// one attempt and never wait. See AD-010.

/// <summary>
/// One view of the app under test, as UiAutomator2 reports it. Every member reads or acts once; a
/// view that was removed or recreated throws <see cref="Bruinell.Core.Exceptions.StaleElementException"/>,
/// a session that ended throws <see cref="Bruinell.Core.Exceptions.AppUnavailableException"/>.
/// </summary>
public interface INativeAndroidElement
{
    #region Identity

    /// <summary>Which view this is: changes when the view is recreated or a list row is rebound.</summary>
    string InstanceKey { get; }

    /// <summary>The view's resource id, with its package prefix (<c>com.app:id/save</c>); null when it has none.</summary>
    string? ResourceId { get; }

    /// <summary>The view's content description; null when it has none.</summary>
    string? ContentDescription { get; }

    /// <summary>The view's class as accessibility reports it (<c>android.widget.Button</c>).</summary>
    string? ClassName { get; }

    #endregion

    #region State

    /// <summary>Whether the view is shown on the screen.</summary>
    bool Visible { get; }

    /// <summary>Whether the view is enabled.</summary>
    bool Enabled { get; }

    /// <summary>Whether the view is selected.</summary>
    bool Selected { get; }

    /// <summary>Whether the view has focus.</summary>
    bool Focused { get; }

    /// <summary>Whether the view is checked; null when it cannot be checked.</summary>
    bool? Checked { get; }

    /// <summary>The view's text. An empty text field answers empty, not its hint.</summary>
    string? Text { get; }

    /// <summary>The text field's hint; null when it has none.</summary>
    string? Hint { get; }

    /// <summary>The view's bounds on the screen, in device pixels.</summary>
    Rectangle Rect { get; }

    /// <summary>A UiAutomator2 attribute (<c>checkable</c>, <c>scrollable</c>, <c>package</c>, ...).</summary>
    string? GetAttribute(string name);

    #endregion

    #region Actions

    /// <summary>Taps the view.</summary>
    void Click();

    /// <summary>
    /// Taps a point of the view, as fractions of its width and height (0.5, 0.5 is its centre):
    /// for a control whose tap means where it lands, such as a rating bar's star.
    /// </summary>
    void TapAt(double x, double y);

    /// <summary>Taps the view twice.</summary>
    void DoubleTap();

    /// <summary>Presses the view and holds it.</summary>
    void LongPress(int durationMs = 1000);

    /// <summary>Types or sets text into the view.</summary>
    void SendKeys(string text, TextInputMethod method = TextInputMethod.Keys);

    /// <summary>Empties the text field.</summary>
    void Clear();

    /// <summary>Swipes across the view.</summary>
    /// <param name="direction">The way the finger moves.</param>
    /// <param name="percent">How much of the view the swipe covers, 0 to 1.</param>
    void Swipe(GestureDirection direction, double percent = 0.75);

    /// <summary>Scrolls the view's content once.</summary>
    /// <param name="direction">The way the finger moves.</param>
    /// <param name="percent">How much of the view one scroll covers, 0 to 1.</param>
    /// <returns>Whether it can scroll further that way.</returns>
    bool Scroll(GestureDirection direction, double percent = 0.75);

    /// <summary>Scrolls the screen until the view is shown, within <paramref name="timeoutMs"/>.</summary>
    void ScrollIntoView(int timeoutMs);

    #endregion

    #region Child lookup

    /// <summary>The first matching descendant now, or null. One attempt.</summary>
    INativeAndroidElement? TryFindElement(Locator locator);

    /// <summary>The matching descendants now. One attempt.</summary>
    IReadOnlyList<INativeAndroidElement> FindElements(Locator locator);

    #endregion
}
