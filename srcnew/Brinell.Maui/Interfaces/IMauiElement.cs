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

    // There is no SupportsInvoke, SupportsSelect or SupportsToggle. Every control object names
    // the one operation it means - Invoke, Toggle, Select or InvokeToolbarItem - and the element
    // performs it or throws naming what was missing. The two flags that used to sit here existed
    // for controls that walked candidates of mixed kind and could not name an operation; those
    // controls are gone, and the one candidate-walker left, CollectionObjectBase, names Select
    // because it knows it is choosing a row. A flag here would be an invitation to guess.

    /// <summary>
    /// Raises this element as a MAUI <c>ToolbarItem</c>. Performs or throws.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Not <see cref="Invoke"/>, because on Windows Invoke lies here.</b> A toolbar item's
    /// automation peer accepts the Invoke pattern, reports success and raises nothing - measured
    /// four ways. Windows asks the app to raise the item through the bridge's
    /// <c>InvokeToolbarItem</c> verb; Android and iOS tap this element, which is the ordinary route.
    /// </para>
    /// <para>
    /// The id is passed in rather than read from the element: the element Windows resolves is
    /// native chrome, and MAUI does not carry the item's <c>AutomationId</c> onto it reliably.
    /// Called on <see cref="IMauiTestContext.AppElement"/>, it raises the item on the page on
    /// screen with that id, without anything having been found first.
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
    /// <para>
    /// The element picks the route. Windows asks the app through the <c>Focus</c> verb, or taps an
    /// element that declares <c>Tap</c>, and otherwise throws naming the verb. Android and iOS
    /// tap, which is how a touch platform focuses a field.
    /// </para>
    /// <para>
    /// There used to be a <c>SupportsFocus</c> question in front of this. Its false branch clicked,
    /// which since the Windows driver stopped using the mouse only ever meant "tap on mobile", so
    /// that choice now lives in the mobile element instead.
    /// </para>
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
    /// <b>Not <see cref="IElement{TSelf}.Selected"/>, and step 105a is why they are two.</b>
    /// Selected is "chosen, one of a group" - a tab, a list row, a radio button. Checked is "on".
    /// Windows' <c>Selected</c> used to fall back to the Toggle pattern, so a checked CheckBox
    /// reported itself selected, and a control reading both could not tell which one the platform
    /// had actually published.
    /// </para>
    /// <para>
    /// Null when the element publishes no checked state at all, which is different from false.
    /// </para>
    /// </remarks>
    bool? Checked => null;

    /// <summary>Sets the checked state. Performs or throws; a no-op when already there.</summary>
    /// <remarks>
    /// Neither platform has a set-state command. Each reads <see cref="Checked"/> and toggles only
    /// when it differs: the Toggle pattern on Windows, a tap on Android and iOS. The control then
    /// verifies the state it asked for.
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
    /// Windows publishes it on the Value pattern. Android publishes no editability at all, so it
    /// answers null - unknown - rather than "editable".
    /// </remarks>
    bool? IsReadOnly => null;

    #endregion

    #region Range

    /// <summary>A range control's current value, in the platform's own units. Null when not published.</summary>
    /// <remarks>
    /// The platform's units, deliberately: WinUI reports a progress bar as 0-100 where MAUI's
    /// <c>Progress</c> is 0-1. Normalising is the control's job, against
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
    /// Windows sets it through the RangeValue pattern. Android and iOS publish no settable value,
    /// so their element steps it with the keyboard.
    /// </remarks>
    /// <exception cref="NotSupportedException">This platform offers no route.</exception>
    void SetRangeValue(double value)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement SetRangeValue.");

    #endregion

    #region Dropdown

    /// <summary>Whether this element is a dropdown that can be opened and read.</summary>
    /// <remarks>
    /// <para>
    /// The Windows combo box, through ExpandCollapse. Replaces casts to
    /// <c>IExpandCollapsePatternElement</c> in the selector controls (step 107).
    /// </para>
    /// <para>
    /// Opening a dropdown is a visible journey, so a selector asks for its app-side verbs first
    /// and uses this only where the app declares none.
    /// </para>
    /// </remarks>
    /// <summary>Whether the dropdown is open. Null where this element has no dropdown.</summary>
    /// <remarks>
    /// <b>Nullable, so the question and the read are one call.</b> Null means "this element
    /// publishes no dropdown", which is a different thing from "there is one and it is shut" -
    /// and the caller that needed to tell them apart used to ask <c>SupportsDropdown</c> first,
    /// at the cost of a second round trip. The same convention covers <see cref="Checked"/>,
    /// <see cref="RangeValue"/> and <see cref="IsFlyoutOpen"/>.
    /// </remarks>
    bool? IsDropdownOpen => null;

    /// <summary>Opens the dropdown and waits for it to report open. Performs or throws.</summary>
    /// <exception cref="NotSupportedException">This element has no dropdown.</exception>
    void OpenDropdown()
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement OpenDropdown.");

    /// <summary>Closes the dropdown. A no-op when it is already closed, or when there is none.</summary>
    /// <remarks>
    /// <b>Lenient, unlike <see cref="OpenDropdown"/>.</b> An element that cannot be open already
    /// has the state the caller asked for, which is the reasoning <c>CloseFlyout</c> uses for
    /// "already shut is success". Opening has no such reading: asking for a dropdown that does
    /// not exist is asking for something that cannot happen.
    /// </remarks>
    void CloseDropdown() { }

    /// <summary>
    /// The texts of the items the dropdown shows, opening it for the read and restoring it.
    /// Null where this element has no dropdown.
    /// </summary>
    /// <remarks>
    /// Realized items, not every item the control holds: a long list publishes the visible
    /// handful. Empty where the dropdown opened and showed nothing.
    /// <para>
    /// Texts rather than elements. The elements went stale as soon as the dropdown closed, so
    /// the only safe caller was one holding it open - which is the platform's own selection
    /// route, and is now private to it.
    /// </para>
    /// </remarks>
    IReadOnlyList<string>? ReadDropdownItemTexts() => null;

    /// <summary>The text of the selector's selected item, read without opening anything. Null when none.</summary>
    /// <remarks>
    /// Windows reads a dropdown's Selection pattern, which names the chosen item rather than the
    /// combo box's header, and a selector without a dropdown's text. Android and iOS read the text
    /// the picker shows.
    /// </remarks>
    string? SelectedItemText => null;

    /// <summary>
    /// The texts of a selector's items, in order. Null where this platform cannot read them.
    /// </summary>
    /// <remarks>
    /// Windows opens the dropdown, reads the items while they are live, and leaves it as it was
    /// found. Android and iOS answer null: nothing reads a picker's items there yet, and null says
    /// so rather than an empty list claiming the picker holds nothing.
    /// </remarks>
    IReadOnlyList<string>? ReadItemTexts() => null;

    #endregion

    #region Gestures

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

    /// <summary>Appends text to a field. Performs or throws.</summary>
    /// <remarks>
    /// <para>
    /// Appending is the one text operation that cannot be assembled from a read and a write out
    /// here. Between the two calls the app is still running, and anything it does to the field
    /// in between is silently overwritten. Windows asks the app through the <c>AppendText</c> verb,
    /// which does both in one pass of the app's UI thread. Android and iOS type the text.
    /// </para>
    /// <para>
    /// A refusal from the app is an exception carrying its reason, never a quiet fallback. This
    /// was once <c>TryAppendText</c>, whose <c>false</c> meant both "no route" and "the app
    /// refused", so a caller that typed on <c>false</c> typed into a read-only field (stage G step
    /// 34). It later became a <c>SupportsAppendText</c> question whose false branch typed, which
    /// on Windows only ever threw; the route choice now lives in each element.
    /// </para>
    /// </remarks>
    /// <param name="text">The text to append.</param>
    void AppendText(string text)
        => throw new NotSupportedException("This platform has no semantic route for appending text.");

    /// <summary>Removes focus from the element. Performs or throws.</summary>
    /// <remarks>
    /// Windows drops focus through the app's <c>Unfocus</c> verb. Android and iOS send Tab, which is
    /// a stand-in rather than the operation: it moves focus on to whatever is next in the tab
    /// order, with whatever side effects that has.
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
    /// <b>Named properties, not reflection.</b> The boundary is deliberate and is stated in step
    /// 23: this reads what a user could perceive - a source, a progress, an enabled flag - and
    /// not the view model behind it. An assertion that needs the view model is a unit test, and
    /// arbitrary property access would turn a UI test framework into a slower one.
    /// </para>
    /// <para>
    /// Each name is a case in the app's own provider, so an unknown one is refused rather than
    /// silently answered with an empty string.
    /// </para>
    /// <para>
    /// <b>Null is "the app does not answer state reads on this element", not "the value is
    /// empty".</b> That is the case a control falls back from, to whatever the accessibility tree
    /// can see - which is what every one of these reads used to do exclusively, and is why
    /// several of them were true for the wrong reason. A name the app <i>does</i> answer reads
    /// for but has no case for still throws: that is a defect in the test, not an absence in the
    /// platform, and the two used to be indistinguishable because asking cost a separate walk.
    /// </para>
    /// <para>
    /// One call, so a caller branches on the result rather than asking and then reading. Calling
    /// it twice - once to decide, once to use - is the double walk this replaced, under a new
    /// name.
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
    /// Windows uses the Scroll pattern on the element or its nearest scrolling ancestor - a MAUI
    /// <c>CollectionView</c> is often wrapped, so the addressable element and the scrolling one
    /// differ - and the pattern reports <see cref="ScrollStep.Moved"/> or
    /// <see cref="ScrollStep.NotMoved"/>. Without it, the bridge's swipe verb, which cannot say.
    /// Android and iOS swipe across the element, which cannot say either:
    /// <see cref="ScrollStep.Unconfirmed"/>.
    /// </para>
    /// <para>
    /// There used to be a <c>SupportsScrollContent</c> question in front of this, whose false
    /// branch swiped. On Windows that swipe was the bridge verb, on Android a drag computed in the
    /// control layer - so the question only chose the platform. The answer that mattered, whether
    /// a caller can loop until the content stops, is the return value now.
    /// </para>
    /// <para>
    /// Not to be confused with the bridge's <see cref="ScrollTo"/>, which reveals a named descendant.
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
    /// For platforms that leave off-screen content out of the accessibility tree: Android
    /// publishes nodes only for what is inside the viewport. Called on the app element, the
    /// platform picks the scrolling container on screen.
    /// </para>
    /// <para>
    /// Windows keeps off-screen elements in the tree, so scrolling reveals nothing a plain lookup
    /// missed, and it answers null - an answer, not a gap. That is the default.
    /// </para>
    /// </remarks>
    /// <param name="locator">The locator for the descendant.</param>
    /// <returns>The element once it is on screen and still, or null when scrolling does not reach it.</returns>
    IMauiElement? TryFindByScrolling(Locator locator) => null;

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
    /// Moves the content towards the item at an index, and reports how it moved.
    /// Performs or throws.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The route choice lives here, not in the caller.</b> Where the platform can jump to an
    /// index it jumps and reports <see cref="ScrollStep.Jumped"/>; otherwise it scrolls one step,
    /// exactly as <see cref="ScrollContent"/> does, and reports what that step reported. A
    /// collection without the jump route still scrolls by the Scroll pattern or by swiping,
    /// which is why this performs rather than refusing.
    /// </para>
    /// <para>
    /// The caller still needs to know which happened, because the two need different waits, so
    /// the answer is a result rather than a question asked beforehand. This replaced a
    /// <c>SupportsScrollToIndex</c> flag that cost a round trip and then told the caller
    /// something the call itself establishes.
    /// </para>
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
    /// An index past the end is refused with <see cref="ArgumentOutOfRangeException"/>, naming the
    /// item count where the app publishes it. That is how a caller walking a virtualized list
    /// finds out it has reached the end, and nothing moves.
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

    #region Selection

    /// <summary>
    /// Selects the item at a position, or throws saying why it could not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The element picks the route. Windows asks the app through the <c>SelectIndex</c> verb, where
    /// nothing opens and the app range-checks against its own items; where the app declares no
    /// verb it opens the dropdown, selects the item and closes it again; with neither it throws.
    /// Android and iOS tap the picker open and tap the item.
    /// </para>
    /// <para>
    /// There used to be <c>SupportsSelectIndex</c> and <c>SupportsSelectByText</c> questions in
    /// front of these, and a third branch that tapped. That branch ran only on Android and iOS, and
    /// only there was it broken, which nothing on Windows could show.
    /// </para>
    /// </remarks>
    /// <param name="index">The zero-based item position.</param>
    /// <exception cref="NotSupportedException">This platform offers no route.</exception>
    /// <exception cref="ArgumentOutOfRangeException">No item at that position.</exception>
    void SelectIndex(int index)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement SelectIndex.");

    /// <summary>Selects the item showing this text, or throws saying why it could not.</summary>
    /// <remarks>
    /// Where two items read alike this takes the first, and cannot do otherwise - that is what
    /// <see cref="SelectIndex"/> is for. Routes as <see cref="SelectIndex"/> does.
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
    /// <para>
    /// <b>The control names the operation; the element decides how the platform performs it</b> -
    /// the same split as <see cref="Invoke"/> and <see cref="Toggle"/>. On Windows that is the
    /// <c>SetDate</c> verb, which the app answers by setting <c>DatePicker.Date</c>, and a missing
    /// declaration throws naming it. Android and iOS have no route yet, so they throw too.
    /// </para>
    /// <para>
    /// There used to be a <c>SupportsSetDate</c> question in front of this. Both of its branches
    /// ended in the same place on every platform - the verb, or a throw - so it was removed.
    /// </para>
    /// <para>
    /// Defaulted to a throw rather than to a no-op, for the reason the activation verbs are: a
    /// default that quietly did nothing would be a test reporting a date it never set.
    /// </para>
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
    /// <remarks>See <see cref="SetDate"/>; the same split applies.</remarks>
    /// <param name="time">The time to set.</param>
    /// <exception cref="NotSupportedException">This platform offers no route.</exception>
    void SetTime(TimeSpan time)
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement SetTime. A control asked for a time to be set "
            + "and this platform offers no route to it.");

    #endregion

    #region The app (IMauiTestContext.AppElement only)

    // What a control needs from the app rather than from one of its elements: the Shell's
    // flyout, the alert on screen, the dialog, and targets the platform's tree cannot show.
    //
    // These used to be IMauiDriver members that control objects called directly, which put a
    // second route beside the element one - and every such call site had to know which of the
    // two it was on. Now there is one: a control asks an element, and for app-level questions the
    // element is the app. Called on any other element they throw, which is the default.
    //
    // See .my/ControlFlow/design-every-call-through-the-element.md.

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
    /// light-dismiss layer - through its pattern, since a click aimed at it could land on whatever
    /// it covers. Android goes back, which is how its drawer is dismissed.
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
    /// Null is the ordinary answer where the app does not declare the read, and the caller counts
    /// the items instead - which works on every platform, so this is not a route choice in
    /// disguise. Windows keeps a flyout's items in the tree once opened, so counting answers
    /// differently on a fresh launch than later in a run; asking the app does not.
    /// </remarks>
    /// <exception cref="NotSupportedException">Not the app element.</exception>
    bool? IsFlyoutOpen
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement IsFlyoutOpen. Ask IMauiTestContext.AppElement.");

    /// <summary>
    /// What the alert on screen is asking, or null when none is open or the app does not report it.
    /// </summary>
    /// <remarks>
    /// Only the app can answer, and only if it raises its alerts through <c>BrinellAlerts</c>:
    /// WinUI puts the message beside a second copy of the title, so from outside it cannot be told
    /// apart. Android and iOS answer null until something reads their dialogs.
    /// </remarks>
    /// <exception cref="NotSupportedException">Not the app element.</exception>
    AlertContents? ReadAlert()
        => throw new NotSupportedException(
            $"{GetType().Name} does not implement ReadAlert. Ask IMauiTestContext.AppElement.");

    /// <summary>The root of the dialog on screen, or null when none is open.</summary>
    /// <remarks>
    /// A find with no locator, because each platform draws its dialog somewhere different: WinUI
    /// as a popup inside the app's window, Android as an alert panel, iOS as an alert element.
    /// </remarks>
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
    /// <b>For the controls Windows automation cannot see.</b> A MAUI <c>Stepper</c> has no tree node
    /// of its own on Windows, only its two buttons, and a <c>SwipeView</c> publishes no
    /// <c>AutomationId</c>. Their bridge elements are still addressable by id, so Windows returns
    /// an element standing for the bridge target: it answers <see cref="ReadState"/> and
    /// <see cref="PerformGesture"/>, and refuses everything else rather than inventing a
    /// visibility or a text.
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
