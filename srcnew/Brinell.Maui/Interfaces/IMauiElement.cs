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
    /// The element's automation id - the identifier the app author set.
    /// </summary>
    /// <remarks>
    /// Windows reports the UIA AutomationId, Android the view's resource id, iOS the accessibility
    /// identifier. Null when the element carries no id.
    /// </remarks>
    string? AutomationId { get; }

    /// <summary>
    /// The element's accessible name - what a screen reader would announce.
    /// </summary>
    /// <remarks>
    /// Platform-drawn chrome is often named only this way: an Android tab has no text and no id.
    /// Windows reports the UIA Name, Android the content description (falling back to its text),
    /// iOS the accessibility label. Null when the element has none.
    /// </remarks>
    string? Name { get; }

    /// <summary>
    /// Whether the element currently has keyboard focus.
    /// </summary>
    bool Focused { get; }

    /// <summary>
    /// One-based logical position in a set, inherited from the nearest ancestor that publishes it.
    /// </summary>
    int? PositionInSet => null;

    /// <summary>
    /// Logical size of the set, inherited from the nearest ancestor that publishes it.
    /// </summary>
    int? SizeOfSet => null;

    /// <summary>
    /// The element's hint text - what a field shows before it is filled in.
    /// </summary>
    /// <remarks>
    /// Windows reports UIA <c>HelpText</c>, Android <c>hint</c>, iOS <c>placeholderValue</c>.
    /// Null when the element has none.
    /// </remarks>
    string? Hint { get; }

    #endregion

    #region Activation

    /// <summary>
    /// Performs the control's primary action.
    /// </summary>
    /// <remarks>
    /// On Windows this is the Invoke pattern; on Android and iOS it is a tap.
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
    /// Used for a <c>Switch</c> and a <c>CheckBox</c>, which are toggled rather than invoked.
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
    /// Selecting one member deselects the rest; toggling one does not. This is what separates a
    /// <c>RadioButton</c> from a <c>CheckBox</c>.
    /// </remarks>
    /// <exception cref="NotSupportedException">This platform cannot select this element.</exception>
    void Select()
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement Select. A control asked to be chosen and this "
            + "platform offers no route to it.");

    /// <summary>
    /// Raises this element as a MAUI <c>ToolbarItem</c>. Performs or throws.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this rather than <see cref="Invoke"/> for toolbar items: on Windows a toolbar item
    /// accepts Invoke and raises nothing. Windows asks the app to raise the item through the
    /// bridge's <c>InvokeToolbarItem</c> verb; Android and iOS tap this element.
    /// </para>
    /// <para>
    /// Called on <see cref="IMauiTestContext.AppElement"/>, it raises the item with that id on the
    /// page on screen, without finding it first.
    /// </para>
    /// </remarks>
    /// <param name="automationId">The item's <c>AutomationId</c>, as the app's markup gives it.</param>
    /// <exception cref="NotSupportedException">This platform cannot raise a toolbar item.</exception>
    void InvokeToolbarItem(string automationId)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement InvokeToolbarItem.");

    #endregion

    #region Focus

    /// <summary>Gives this element focus. Performs or throws.</summary>
    /// <remarks>
    /// Windows asks the app through the <c>Focus</c> verb, or taps an element that declares
    /// <c>Tap</c>, and otherwise throws naming the verb. Android and iOS tap.
    /// </remarks>
    /// <exception cref="NotSupportedException">This platform cannot focus this element.</exception>
    void Focus()
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement Focus.");

    #endregion

    #region Checked state

    /// <summary>
    /// Whether a two-state control is on: a <c>Switch</c> or a <c>CheckBox</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Distinct from <see cref="IElement{TSelf}.Selected"/>, which means "chosen, one of a group" -
    /// a tab, a list row, a radio button.
    /// </para>
    /// <para>
    /// Null when the element publishes no checked state at all, which is different from false.
    /// </para>
    /// </remarks>
    bool? Checked => null;

    /// <summary>Sets the checked state. Performs or throws; a no-op when already there.</summary>
    /// <remarks>
    /// Reads <see cref="Checked"/> and toggles only when it differs: the Toggle pattern on Windows,
    /// a tap on Android and iOS.
    /// </remarks>
    /// <exception cref="NotSupportedException">This platform offers no route.</exception>
    void SetChecked(bool isChecked)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement SetChecked.");

    #endregion

    #region Text field state

    /// <summary>
    /// The value a field publishes, where the platform keeps it apart from what the field shows.
    /// Null when it does not.
    /// </summary>
    /// <remarks>
    /// Windows' Value pattern: a <c>CalendarDatePicker</c> answers '07-Sep-26' here while its
    /// name and text say something else. Android and iOS publish no such thing, and their
    /// <see cref="IElement{TSelf}.Text"/> is already the value.
    /// </remarks>
    string? Value => null;

    /// <summary>Whether a text field refuses edits. Null when the platform does not say.</summary>
    /// <remarks>
    /// Windows publishes it on the Value pattern. Android publishes no editability, so it answers
    /// null.
    /// </remarks>
    bool? IsReadOnly => null;

    #endregion

    #region Range

    /// <summary>A range control's current value, in the platform's own units. Null when not published.</summary>
    /// <remarks>
    /// WinUI reports a progress bar as 0-100 where MAUI's <c>Progress</c> is 0-1. Normalise against
    /// <see cref="RangeMinimum"/> and <see cref="RangeMaximum"/>.
    /// </remarks>
    double? RangeValue => null;

    /// <summary>The smallest value the range allows. Null when not published.</summary>
    double? RangeMinimum => null;

    /// <summary>The largest value the range allows. Null when not published.</summary>
    double? RangeMaximum => null;

    /// <summary>The range's small step. Null when not published.</summary>
    double? RangeSmallChange => null;

    /// <summary>Sets a range control's value. Performs or throws.</summary>
    /// <remarks>
    /// Windows sets it through the RangeValue pattern. Android and iOS step it with the keyboard.
    /// </remarks>
    /// <exception cref="NotSupportedException">This platform offers no route.</exception>
    void SetRangeValue(double value)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement SetRangeValue.");

    #endregion

    #region Dropdown

    /// <summary>Whether the dropdown is open. Null where this element has no dropdown.</summary>
    /// <remarks>
    /// Null means "this element has no dropdown", which is different from "there is one and it is
    /// closed".
    /// </remarks>
    bool? IsDropdownOpen => null;

    /// <summary>Opens the dropdown and waits for it to report open. Performs or throws.</summary>
    /// <exception cref="NotSupportedException">This element has no dropdown.</exception>
    void OpenDropdown()
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement OpenDropdown.");

    /// <summary>Closes the dropdown. A no-op when it is already closed, or when there is none.</summary>
    void CloseDropdown() { }

    /// <summary>
    /// The texts of the items the dropdown shows, opening it for the read and restoring it.
    /// Null where this element has no dropdown.
    /// </summary>
    /// <remarks>
    /// Realized items only: a long list publishes the visible handful. Empty where the dropdown
    /// opened and showed nothing.
    /// </remarks>
    IReadOnlyList<string>? ReadDropdownItemTexts() => null;

    /// <summary>The text of the selector's selected item, read without opening anything. Null when none.</summary>
    string? SelectedItemText => null;

    /// <summary>
    /// The texts of a selector's items, in order. Null where this platform cannot read them.
    /// </summary>
    /// <remarks>
    /// Windows opens the dropdown, reads the items and leaves it as it was found. Android and iOS
    /// answer null.
    /// </remarks>
    IReadOnlyList<string>? ReadItemTexts() => null;

    #endregion

    #region Gestures

    /// <summary>
    /// Performs the gesture, or throws saying why it could not.
    /// </summary>
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

    /// <summary>Appends text to a field. Performs or throws.</summary>
    /// <remarks>
    /// Windows asks the app through the <c>AppendText</c> verb, which reads and writes in one pass
    /// of the app's UI thread. Android and iOS type the text. A refusal from the app throws with
    /// its reason.
    /// </remarks>
    /// <param name="text">The text to append.</param>
    void AppendText(string text)
        => throw new NotSupportedException("This platform has no semantic route for appending text.");

    /// <summary>Removes focus from the element. Performs or throws.</summary>
    /// <remarks>
    /// Windows drops focus through the app's <c>Unfocus</c> verb. Android and iOS send Tab, which
    /// moves focus on to whatever is next in the tab order.
    /// </remarks>
    void ClearFocus()
        => throw new NotSupportedException("This platform has no semantic route for clearing focus.");

    #endregion

    #region State the platform cannot be asked for

    /// <summary>
    /// Reads a named piece of app state. Null where the app does not answer state reads here.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reads what a user could perceive - a source, a progress, an enabled flag - through named
    /// cases in the app's own provider, not the view model behind it.
    /// </para>
    /// <para>
    /// Null means the app does not answer state reads on this element, not that the value is
    /// empty. A name the app has no case for throws.
    /// </para>
    /// </remarks>
    /// <param name="property">The property name, as the provider spells it.</param>
    /// <returns>The value as the app formatted it, or null where the app does not answer.</returns>
    /// <exception cref="NotSupportedException">
    /// The app answers state reads here but has no case for that name.
    /// </exception>
    string? ReadState(string property) => null;

    #endregion

    #region Scrolling

    /// <summary>
    /// Scrolls the content one viewport step and reports what is known about whether it moved.
    /// Performs or throws.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Windows uses the Scroll pattern on the element or its nearest scrolling ancestor and reports
    /// <see cref="ScrollStep.Moved"/> or <see cref="ScrollStep.NotMoved"/>; without the pattern it
    /// swipes. Android and iOS swipe across the element and report
    /// <see cref="ScrollStep.Unconfirmed"/>.
    /// </para>
    /// <para>
    /// Not to be confused with <see cref="ScrollTo"/>, which reveals a named descendant.
    /// </para>
    /// </remarks>
    /// <param name="verticalSteps">Positive towards the end, negative towards the start.</param>
    /// <param name="horizontalSteps">Positive towards the end, negative towards the start.</param>
    /// <returns>What is known about the movement.</returns>
    /// <exception cref="NotSupportedException">This element has no scroll route.</exception>
    ScrollStep ScrollContent(int verticalSteps, int horizontalSteps = 0)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement ScrollContent.");

    /// <summary>
    /// Finds a descendant by scrolling this element until the descendant enters the tree.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For platforms that leave off-screen content out of the accessibility tree, such as Android.
    /// Called on the app element, the platform picks the scrolling container on screen.
    /// </para>
    /// <para>
    /// Windows keeps off-screen elements in the tree, so it answers null.
    /// </para>
    /// </remarks>
    /// <param name="locator">The locator for the descendant.</param>
    /// <returns>The element once it is on screen and still, or null when scrolling does not reach it.</returns>
    IMauiElement? TryFindByScrolling(Locator locator) => null;

    /// <summary>
    /// Brings a named descendant into view, without animation.
    /// </summary>
    /// <param name="automationId">The descendant to reveal.</param>
    /// <exception cref="NotSupportedException">This platform offers no route.</exception>
    void ScrollTo(string automationId)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement ScrollTo.");

    /// <summary>
    /// Moves the content towards the item at an index, and reports how it moved.
    /// Performs or throws.
    /// </summary>
    /// <remarks>
    /// Where the platform can jump to an index it jumps and reports <see cref="ScrollStep.Jumped"/>;
    /// otherwise it scrolls one step, as <see cref="ScrollContent"/> does, and reports what that
    /// step reported.
    /// </remarks>
    /// <param name="index">The item index to move towards.</param>
    /// <returns>How the content moved.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The platform can jump and the index is past the end of the items. A platform that can only
    /// step cannot tell, and scrolls.
    /// </exception>
    ScrollStep ScrollTowards(int index) => ScrollContent(1);

    /// <summary>Brings the item at an index into view.</summary>
    /// <remarks>
    /// An index past the end throws <see cref="ArgumentOutOfRangeException"/>, naming the item count
    /// where the app publishes it, and nothing moves.
    /// </remarks>
    /// <param name="index">The item index.</param>
    /// <exception cref="NotSupportedException">This platform offers no route.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is past the end of the items.</exception>
    void ScrollToIndex(int index)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement ScrollToIndex.");

    /// <summary>
    /// Where this scroller is, and how much there is to scroll.
    /// </summary>
    /// <returns>The scroller's position.</returns>
    /// <exception cref="NotSupportedException">This platform offers no route.</exception>
    ScrollPosition ReadScrollPosition()
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement ReadScrollPosition.");

    #endregion

    #region Selection

    /// <summary>
    /// Selects the item at a position, or throws saying why it could not.
    /// </summary>
    /// <remarks>
    /// Windows asks the app through the <c>SelectIndex</c> verb; where the app declares none it
    /// opens the dropdown, selects the item and closes it again. Android and iOS tap the picker
    /// open and tap the item.
    /// </remarks>
    /// <param name="index">The zero-based item position.</param>
    /// <exception cref="NotSupportedException">This platform offers no route.</exception>
    /// <exception cref="ArgumentOutOfRangeException">No item at that position.</exception>
    void SelectIndex(int index)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement SelectIndex.");

    /// <summary>Selects the item showing this text, or throws saying why it could not.</summary>
    /// <remarks>
    /// Where two items read alike this takes the first; use <see cref="SelectIndex"/> to choose
    /// between them.
    /// </remarks>
    /// <param name="text">The item text.</param>
    /// <exception cref="NotSupportedException">This platform offers no route.</exception>
    void SelectByText(string text)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement SelectByText.");

    #endregion

    #region Dates and times

    /// <summary>
    /// Sets a date picker's date.
    /// </summary>
    /// <remarks>
    /// On Windows this uses the app's <c>SetDate</c> verb and throws when the app does not declare
    /// it. Android and iOS have no route yet and throw.
    /// </remarks>
    /// <param name="date">The date to set.</param>
    /// <exception cref="NotSupportedException">This platform offers no route.</exception>
    void SetDate(DateTime date)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement SetDate. A control asked for a date to be set "
            + "and this platform offers no route to it.");

    /// <summary>
    /// Sets a time picker's time.
    /// </summary>
    /// <remarks>Routes as <see cref="SetDate"/> does, through the <c>SetTime</c> verb.</remarks>
    /// <param name="time">The time to set.</param>
    /// <exception cref="NotSupportedException">This platform offers no route.</exception>
    void SetTime(TimeSpan time)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement SetTime. A control asked for a time to be set "
            + "and this platform offers no route to it.");

    #endregion

    #region The app (IMauiTestContext.AppElement only)

    // App-level operations: the Shell's flyout, the alert on screen, the dialog, and targets the
    // platform's tree cannot show. Only IMauiTestContext.AppElement implements these; any other
    // element throws.

    /// <summary>Opens a Shell's flyout. Performs or throws; already open is success.</summary>
    /// <remarks>
    /// Windows uses the app's <c>OpenFlyout</c> verb where declared, and otherwise invokes the
    /// Shell's opener in the title bar. Android taps the navigation drawer's opener.
    /// </remarks>
    /// <exception cref="NotSupportedException">Not the app element, or no route on this platform.</exception>
    void OpenFlyout()
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement OpenFlyout. Ask IMauiTestContext.AppElement.");

    /// <summary>Closes a Shell's flyout without choosing anything. Performs or throws.</summary>
    /// <remarks>
    /// Windows uses the app's <c>CloseFlyout</c> verb where declared, and otherwise invokes the
    /// light-dismiss layer. Android goes back.
    /// </remarks>
    /// <exception cref="NotSupportedException">Not the app element, or no route on this platform.</exception>
    void CloseFlyout()
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement CloseFlyout. Ask IMauiTestContext.AppElement.");

    /// <summary>
    /// Whether a Shell's flyout is showing, as the app reports it. Null when the platform cannot
    /// say without counting the flyout's items.
    /// </summary>
    /// <remarks>
    /// Null where the app does not declare the read; count the flyout's items instead.
    /// </remarks>
    /// <exception cref="NotSupportedException">Not the app element.</exception>
    bool? IsFlyoutOpen
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement IsFlyoutOpen. Ask IMauiTestContext.AppElement.");

    /// <summary>
    /// What the alert on screen is asking, or null when none is open or the app does not report it.
    /// </summary>
    /// <remarks>
    /// Requires the app to raise its alerts through <c>BrinellAlerts</c>. Android and iOS answer
    /// null.
    /// </remarks>
    /// <exception cref="NotSupportedException">Not the app element.</exception>
    AlertContents? ReadAlert()
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement ReadAlert. Ask IMauiTestContext.AppElement.");

    /// <summary>The root of the dialog on screen, or null when none is open.</summary>
    /// <exception cref="NotSupportedException">Not the app element.</exception>
    IMauiElement? TryFindActiveDialog()
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement TryFindActiveDialog. Ask IMauiTestContext.AppElement.");

    /// <summary>
    /// An element the app declared by <c>AutomationId</c>, whether or not the platform's tree
    /// shows it. Null when there is none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For controls Windows automation cannot see, such as a <c>Stepper</c> or a
    /// <c>SwipeView</c>. On Windows the returned element answers <see cref="ReadState"/> and
    /// <see cref="PerformGesture"/> and refuses everything else.
    /// </para>
    /// <para>
    /// Android and iOS return the real node found by that id, where it is in the tree.
    /// </para>
    /// </remarks>
    /// <param name="automationId">The <c>AutomationId</c> in the app's markup.</param>
    /// <exception cref="NotSupportedException">Not the app element.</exception>
    IMauiElement? TryFindDeclared(string automationId)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement TryFindDeclared. Ask IMauiTestContext.AppElement.");

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
