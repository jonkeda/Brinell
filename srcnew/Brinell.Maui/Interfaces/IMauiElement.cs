using System.Drawing;
using Brinell.Core;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;

namespace Brinell.Maui.Interfaces;

/// <summary>
/// MAUI-specific element interface extending <see cref="IElement{TSelf}"/>.
/// Adds DOM access methods for hybrid WebView apps.
/// This interface can be mocked for unit testing without requiring an Appium connection.
/// </summary>
public interface IMauiElement : IElement<IMauiElement>
{
    #region Identity

    /// <summary>
    /// The element's automation id - the identifier the app author set, in the app author's
    /// terms.
    /// </summary>
    /// <remarks>
    /// Each platform publishes a MAUI <c>AutomationId</c> differently: Windows as the UIA
    /// AutomationId, Android as the view's resource id, iOS as the accessibility identifier.
    /// This property is where that difference is answered, so code that compares ids does not
    /// have to know which platform it is on. Null when the element carries no id.
    /// </remarks>
    string? AutomationId { get; }

    /// <summary>
    /// The element's accessible name - what a screen reader would announce.
    /// </summary>
    /// <remarks>
    /// The other way an element is named, and the only one platform-drawn chrome usually
    /// carries: an Android tab has no text and no id, and answers only to this. Windows reports
    /// the UIA Name, Android the content description (falling back to its text), iOS the
    /// accessibility label. Null when the element has none.
    /// </remarks>
    string? Name { get; }

    /// <summary>
    /// Whether the element currently has keyboard focus.
    /// </summary>
    /// <remarks>
    /// A property rather than an attribute lookup because focus is one of the things every
    /// accessibility tree publishes first-class: UIA as <c>HasKeyboardFocus</c>, Android as
    /// <c>focused</c>, iOS as <c>hasFocus</c>. Asking for it by string worked on Android by
    /// coincidence of naming and returned null on Windows, where the control was focused all
    /// along - see .my/GetAttribute/audit-what-getattribute-can-answer.md.
    /// </remarks>
    bool Focused { get; }

    /// <summary>
    /// The element's hint text - what a field shows before it is filled in.
    /// </summary>
    /// <remarks>
    /// Published by every platform under its own name: UIA <c>HelpText</c>, Android <c>hint</c>,
    /// iOS <c>placeholderValue</c>. Null when the element has none.
    /// </remarks>
    string? Hint { get; }

    #endregion

    #region Gestures

    /// <summary>
    /// Whether this element can perform the gesture semantically, without synthetic input.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A question, not a demand: a test may reasonably ask and then take another route. On
    /// Windows the answer is whether the app under test declared this gesture on this element
    /// for the automation bridge, which means a false is usually a missing declaration in the
    /// app's markup rather than something impossible.
    /// </para>
    /// <para>
    /// Defaulted to false so a platform that has not implemented gestures yet compiles and
    /// answers honestly. A platform that synthesises real touch input should return true.
    /// </para>
    /// </remarks>
    /// <param name="gesture">The gesture to ask about.</param>
    /// <returns>Whether <see cref="PerformGesture"/> would work.</returns>
    bool SupportsGesture(MauiGesture gesture) => false;

    /// <summary>
    /// Performs the gesture, or throws saying why it could not.
    /// </summary>
    /// <remarks>
    /// The form to use when the gesture is the point of the test, so a failure names the
    /// element and the gesture rather than surfacing as a later assertion about state that
    /// never changed.
    /// </remarks>
    /// <param name="gesture">The gesture to perform.</param>
    /// <exception cref="NotSupportedException">
    /// This platform cannot perform the gesture on this element.
    /// </exception>
    void PerformGesture(MauiGesture gesture)
        => throw new NotSupportedException(
            $"Gestures are not implemented for {GetType().Name}. On Windows they are carried by "
            + "the Brinell UI Automation bridge, which the app under test must opt into; on "
            + "Android and iOS they are synthetic touch input.");

    /// <summary>
    /// Performs the gesture if it can, and says whether it did.
    /// </summary>
    /// <remarks>
    /// The form to use when the gesture is arrangement rather than the subject - setting a
    /// swipe view open so the test can get at what is behind it - and a fallback exists.
    /// </remarks>
    /// <param name="gesture">The gesture to perform.</param>
    /// <returns>Whether the gesture was performed.</returns>
    bool TryPerformGesture(MauiGesture gesture)
    {
        if (!SupportsGesture(gesture))
        {
            return false;
        }

        try
        {
            PerformGesture(gesture);
            return true;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    #endregion

    #region Semantic text

    /// <summary>
    /// Appends text without typing it, if the platform can.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Appending is the one text operation that cannot be assembled from a read and a write out
    /// here. Between the two calls the app is still running, and anything it does to the field
    /// in between is silently overwritten; a platform that can do both in one pass of the app's
    /// own UI thread should, and this is where it says so.
    /// </para>
    /// <para>
    /// Defaulted to false so a platform without a semantic route compiles and answers honestly.
    /// The caller then types, which is what it always did.
    /// </para>
    /// </remarks>
    /// <param name="text">The text to append.</param>
    /// <returns>Whether the text was appended.</returns>
    bool TryAppendText(string text) => false;

    /// <summary>
    /// Removes focus from the element without moving it elsewhere, if the platform can.
    /// </summary>
    /// <remarks>
    /// The usual route is a Tab keystroke, which is a different operation wearing this one's
    /// name: it moves focus to whatever is next in the tab order, with whatever side effects
    /// that has, and it needs the app in front to receive the key at all. A platform that can
    /// simply drop focus should say so here.
    /// </remarks>
    /// <returns>Whether focus was cleared.</returns>
    bool TryClearFocus() => false;

    #endregion

    #region DOM Access (Hybrid Apps)
    
    /// <summary>
    /// Gets a DOM attribute value (for WebView content).
    /// </summary>
    /// <param name="attributeName">The name of the DOM attribute.</param>
    /// <returns>The attribute value, or null if not present or not applicable.</returns>
    string? GetDomAttribute(string attributeName);
    
    /// <summary>
    /// Gets a DOM property value (for WebView content).
    /// </summary>
    /// <param name="propertyName">The name of the DOM property.</param>
    /// <returns>The property value, or null if not present or not applicable.</returns>
    string? GetDomProperty(string propertyName);
    
    /// <summary>
    /// Gets a computed CSS value (for WebView content).
    /// </summary>
    /// <param name="propertyName">The name of the CSS property.</param>
    /// <returns>The CSS value, or null if not applicable.</returns>
    string? GetCssValue(string propertyName);
    
    #endregion
    
    #region Form Actions
    
    /// <summary>
    /// Submits a form (if the element is within a form).
    /// </summary>
    void Submit();
    
    #endregion
}
