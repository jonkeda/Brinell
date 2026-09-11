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

    #region Activation

    // The control says what it is doing; the element says how this platform does that.
    //
    // These replaced a shared ladder that tried SelectionItem, then Invoke, then a pointer click,
    // and took the first that reported success. It was cross-platform only by accident of that
    // last rung - on Appium every pattern probe reports unsupported, so the fall-through to a tap
    // is what made mobile work. That answer never varies, so the ladder was rediscovering a
    // compile-time fact at run time, once per control, once per call.
    //
    // Worse, a ladder can only fall back from a rung that admits failure, and two have been
    // measured lying: LegacyIAccessible's DoDefaultAction reports success without changing a
    // Switch, and a MAUI ToolbarItem accepts Invoke and raises nothing. Neither is detectable
    // from inside a ladder; both are trivial when the control simply names its operation.
    //
    // See .my/fix/design-controls-know-how-to-click.md.

    /// <summary>
    /// Performs the control's primary action.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What a button does when pressed. On Windows this is the Invoke pattern; on a touch
    /// platform it is a tap, because that is how a touch platform invokes something - not because
    /// the pattern was tried first and declined.
    /// </para>
    /// <para>
    /// <b>Throws rather than clicking</b> when the platform cannot. A control that reaches here
    /// and finds no route has been declared wrong, and saying so is the point: the alternative is
    /// a suite that quietly changes how it drives the app and passes anyway.
    /// </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">This platform cannot invoke this element.</exception>
    void Invoke()
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement Invoke. A control asked to perform its primary "
            + "action and this platform offers no route to it.");

    /// <summary>
    /// Flips a two- or three-state control.
    /// </summary>
    /// <remarks>
    /// A <c>Switch</c> and a <c>CheckBox</c> are toggled, never invoked: MAUI maps both to WinUI
    /// controls that expose Toggle and neither Invoke nor SelectionItem.
    /// </remarks>
    /// <exception cref="NotSupportedException">This platform cannot toggle this element.</exception>
    void Toggle()
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement Toggle. A control asked to change its checked "
            + "state and this platform offers no route to it.");

    /// <summary>
    /// Chooses this element, as one of a group or as an item in a list.
    /// </summary>
    /// <remarks>
    /// Carries the "one of several" meaning that <see cref="Invoke"/> and <see cref="Toggle"/> do
    /// not, which is what separates a <c>RadioButton</c> from a <c>CheckBox</c>. Selecting one
    /// member deselects the rest; toggling one does not.
    /// </remarks>
    /// <exception cref="NotSupportedException">This platform cannot select this element.</exception>
    void Select()
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement Select. A control asked to be chosen and this "
            + "platform offers no route to it.");

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

    #region Scrolling

    /// <summary>
    /// Whether this element can be asked to scroll semantically.
    /// </summary>
    /// <remarks>
    /// A question, so a caller takes one route rather than trying one. See
    /// <see cref="SupportsSetDate"/> for why that distinction is the whole point.
    /// </remarks>
    bool SupportsScrollVerbs => false;

    /// <summary>
    /// Brings a named descendant into view.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What this replaces is a loop of mouse-wheel clicks.</b> A wheel click is an
    /// unquantified unit - it means whatever the OS and the control decide it means - and there
    /// is no signal saying the content arrived, so the old route scrolled a bit, polled a scroll
    /// percentage, and guessed when it had stopped moving. <c>ScrollToAsync</c> is a
    /// cross-platform MAUI API, so the same call does the same thing on Windows, Android and
    /// iOS.
    /// </para>
    /// <para>
    /// Never animated: an animated scroll finishes after the call returns, which is a sleep or a
    /// race in every caller.
    /// </para>
    /// </remarks>
    /// <param name="automationId">The descendant to reveal.</param>
    /// <exception cref="NotSupportedException">This platform offers no route.</exception>
    void ScrollTo(string automationId)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement ScrollTo.");

    /// <summary>
    /// Whether this element can be asked to scroll to an item index.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="SupportsScrollVerbs"/> because the two are declared separately
    /// and by different controls: a <c>ScrollView</c> answers <c>ScrollTo</c> and
    /// <c>ScrollPosition</c> and has no items; a <c>CollectionView</c> answers this and has no
    /// scroll offset of its own to report.
    /// </remarks>
    bool SupportsScrollToIndex => false;

    /// <summary>Brings the item at an index into view.</summary>
    /// <remarks>
    /// An index past the end is not an error: it is how a caller walking a virtualized list
    /// finds out it has reached the end. Nothing moves and nothing throws.
    /// </remarks>
    /// <param name="index">The item index.</param>
    /// <exception cref="NotSupportedException">This platform offers no route.</exception>
    void ScrollToIndex(int index)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement ScrollToIndex.");

    /// <summary>
    /// Where this scroller is, and how much there is to scroll.
    /// </summary>
    /// <remarks>
    /// Offset and extent together, because neither means anything alone: a percentage - which is
    /// what UI Automation offers - cannot distinguish a short page that cannot scroll from a long
    /// one already at the end, and that is exactly the assertion a test wants to make.
    /// </remarks>
    /// <returns>The scroller's position.</returns>
    /// <exception cref="NotSupportedException">This platform offers no route.</exception>
    ScrollPosition ReadScrollPosition()
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement ReadScrollPosition.");

    #endregion

    #region Dates and times

    /// <summary>
    /// Whether <see cref="SetDate"/> has a semantic route on this element.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A question, so that a control can choose a route without trying one.</b> A caller asks
    /// this once and then takes exactly one path: the verb, or whatever the platform does for an
    /// app that carries no bridge. That is the difference between this and the three-rung ladder
    /// it replaced, which performed each rung to discover whether it was the right one - and
    /// whose first rung, on WinUI, advertised a Value pattern and then refused the write.
    /// </para>
    /// <para>
    /// On Windows the answer is whether the app under test declared <c>SetDate</c> on this
    /// element, so a false is usually a missing line of markup rather than something impossible.
    /// </para>
    /// </remarks>
    bool SupportsSetDate => false;

    /// <summary>Whether <see cref="SetTime"/> has a semantic route. See <see cref="SupportsSetDate"/>.</summary>
    bool SupportsSetTime => false;

    /// <summary>
    /// Sets a date picker's date.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The control names the operation; the element decides how the platform performs it</b> -
    /// the same split as <see cref="Invoke"/> and <see cref="Toggle"/>. On Windows that is a verb
    /// the app answers by setting <c>DatePicker.Date</c>, or, for an app carrying no bridge, the
    /// calendar flyout walked by pattern. Neither is a rung of the other: the element asks the
    /// app once whether it declares the verb, and takes one route.
    /// </para>
    /// <para>
    /// Defaulted to a throw rather than to a no-op, for the reason the activation verbs are: a
    /// default that quietly did nothing would be a test reporting a date it never set.
    /// </para>
    /// </remarks>
    /// <param name="date">The date to set.</param>
    /// <exception cref="NotSupportedException">This platform offers no route.</exception>
    /// <seealso cref="SupportsSetDate"/>
    void SetDate(DateTime date)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement SetDate. A control asked for a date to be set "
            + "and this platform offers no route to it.");

    /// <summary>
    /// Sets a time picker's time.
    /// </summary>
    /// <remarks>See <see cref="SetDate"/>; the same split applies.</remarks>
    /// <param name="time">The time to set.</param>
    /// <exception cref="NotSupportedException">This platform offers no route.</exception>
    void SetTime(TimeSpan time)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement SetTime. A control asked for a time to be set "
            + "and this platform offers no route to it.");

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
