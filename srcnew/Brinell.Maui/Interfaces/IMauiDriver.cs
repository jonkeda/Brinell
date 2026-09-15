using Brinell.Core.Interfaces;
using Brinell.Maui.Enums;

namespace Brinell.Maui.Interfaces;

/// <summary>
/// MAUI-specific driver interface extending <see cref="IDriver{TElement}"/> and <see cref="IDiagnosticDriver"/>.
/// Adds platform detection, context switching for hybrid apps, and window management.
/// This interface can be mocked for unit testing without requiring an Appium connection.
/// </summary>
public interface IMauiDriver : IDriver<IMauiElement>, IDiagnosticDriver
{
    #region Platform
    
    /// <summary>
    /// Gets the target platform (Windows, Android, iOS, macOS).
    /// </summary>
    MauiPlatform Platform { get; }
    
    #endregion
    
    #region Context Switching (Hybrid Apps)
    
    /// <summary>
    /// Gets or sets the current context (NATIVE_APP, WEBVIEW_*, etc.).
    /// </summary>
    string Context { get; set; }
    
    /// <summary>
    /// Gets all available contexts.
    /// </summary>
    IReadOnlyCollection<string> Contexts { get; }
    
    #endregion
    
    #region Window Management
    
    /// <summary>
    /// Gets the current window handle.
    /// </summary>
    string CurrentWindowHandle { get; }
    
    /// <summary>
    /// Gets all window handles.
    /// </summary>
    IReadOnlyCollection<string> WindowHandles { get; }

    #endregion

    #region Gestures

    /// <summary>
    /// Whether a gesture can be performed semantically on the element with this
    /// <c>AutomationId</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Addressed by id, not by element, deliberately.</b> The controls that most need a
    /// gesture are the ones Windows automation cannot see: a MAUI <c>SwipeView</c> publishes no
    /// <c>AutomationId</c> at all on Windows, because its WinUI peer must not be overridden -
    /// doing so collapses the app's entire automation tree. There is therefore no element to
    /// hang the call on, and requiring one would exclude exactly the cases the bridge exists
    /// for.
    /// </para>
    /// <para>
    /// The element-level <see cref="IMauiElement.SupportsGesture"/> remains, for the elements
    /// that are addressable.
    /// </para>
    /// </remarks>
    /// <param name="automationId">The MAUI <c>AutomationId</c> of the target element.</param>
    /// <param name="gesture">The gesture to ask about.</param>
    /// <returns>Whether <see cref="PerformGesture"/> would work.</returns>
    bool SupportsGesture(string automationId, MauiGesture gesture) => false;

    /// <summary>Performs a gesture on the element with this <c>AutomationId</c>, or throws.</summary>
    /// <param name="automationId">The MAUI <c>AutomationId</c> of the target element.</param>
    /// <param name="gesture">The gesture to perform.</param>
    /// <exception cref="NotSupportedException">
    /// This platform cannot perform the gesture on that element.
    /// </exception>
    void PerformGesture(string automationId, MauiGesture gesture)
        => PerformGesture(automationId, gesture, 0, 0);

    /// <summary>
    /// Performs a gesture that carries a magnitude, or throws.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two gestures are meaningless without one.</b> A pan with no distance is not a pan and
    /// a pinch with no scale is not a pinch, so an overload that could not express either would
    /// leave the two verbs describable but not usable. The rest ignore the arguments, which is
    /// why the no-argument overload above simply passes zeroes rather than being a separate
    /// path.
    /// </para>
    /// <para>
    /// The meaning of each argument is defined per verb, on <c>BrinellVerb</c>: pan takes device
    /// independent pixels, pinch takes a percentage where 100 is no change.
    /// </para>
    /// </remarks>
    /// <param name="automationId">The MAUI <c>AutomationId</c> of the target element.</param>
    /// <param name="gesture">The gesture to perform.</param>
    /// <param name="arg1">First argument, meaning defined per gesture.</param>
    /// <param name="arg2">Second argument, meaning defined per gesture.</param>
    /// <exception cref="NotSupportedException">
    /// This platform cannot perform the gesture on that element.
    /// </exception>
    void PerformGesture(string automationId, MauiGesture gesture, int arg1, int arg2)
        => throw new NotSupportedException(
            $"Gestures are not implemented for {GetType().Name}. On Windows they are carried by "
            + "the Brinell UI Automation bridge, which the app under test must opt into; on "
            + "Android and iOS they are synthetic touch input.");

    #endregion

    #region State the platform cannot expose as an element

    /// <summary>Whether the element identified by <paramref name="automationId"/> declares GetState.</summary>
    bool SupportsStateReads(string automationId) => false;

    /// <summary>Reads app-published state for an element that may not exist in the native tree.</summary>
    string ReadState(string automationId, string property)
        => throw new NotSupportedException(
            $"State reads are not implemented for {GetType().Name}.");

    #endregion

    #region Navigation
    
    /// <summary>
    /// Navigates to the specified URL or destination.
    /// </summary>
    void NavigateTo(string destination);
    
    /// <summary>
    /// Goes back one page, or throws saying why it could not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This used to be a bool, and the bool caused a defect rather than merely permitting
    /// one.</b> It answered <c>false</c> for four different situations - the app has no bridge,
    /// the page has not published itself yet, this target is a page that was popped long ago, and
    /// we are already at the root with nothing to pop. The caller could not tell them apart, so
    /// it guessed, and the guess written into the driver was a two-second wait that fired on the
    /// commonest of the four. Every fixture reset starting at the hub paid it. See
    /// <c>.my/fix/rca-navigation-tests-stall.md</c>.
    /// </para>
    /// <para>
    /// Ask <see cref="IsAtNavigationRoot"/> first if "nothing to pop" is an expected outcome
    /// rather than a failure. That is the question; this is the command.
    /// </para>
    /// </remarks>
    /// <exception cref="Brinell.Core.Exceptions.BrinellException">
    /// The app did not go back, with the specific reason.
    /// </exception>
    void NavigateBack();

    /// <summary>
    /// Whether the app is showing its first page, with nothing to go back to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The question that <see cref="NavigateBack"/>'s old bool was being made to answer as a side
    /// effect. Asking it costs one round trip and is answered by any live element on the app's
    /// bridge - a page's navigation stack is the app's, not that page's, so unlike going back
    /// there is no wrong element to reach.
    /// </para>
    /// <para>
    /// Defaulted to true so a platform with no navigation model compiles and does not send a
    /// caller looking for a page to pop that does not exist.
    /// </para>
    /// </remarks>
    /// <returns>Whether there is nothing to go back to.</returns>
    bool IsAtNavigationRoot() => NavigationDepth() <= 1;

    /// <summary>
    /// How many pages are on the app's navigation stack.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A number rather than a flag, because callers pop in a loop.</b> "Are we at the root" is
    /// enough to decide whether to pop at all, and not enough to wait for a pop to finish: the
    /// pop is started rather than awaited, so a caller unwinding a stack three deep has to be
    /// able to see the depth fall to know the first one landed. With only the flag it would wait
    /// out a timeout per pop, which is the shape of the defect this whole change removes.
    /// </para>
    /// <para>
    /// Answered by any live element on the app's bridge: a page's navigation stack is the app's,
    /// not that page's.
    /// </para>
    /// </remarks>
    /// <returns>The number of pages, where 1 means only the root.</returns>
    int NavigationDepth() => 1;

    /// <summary>
    /// Whether the app has finished the work it had queued.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What <c>AD-004</c> needs in order to be a followable rule.</b> "No arbitrary sleeps" is
    /// only obeyable if there is something to wait <i>on</i>; without one, a test that needs the
    /// UI to settle either sleeps or invents a sentinel element whose appearance approximates
    /// settling.
    /// </para>
    /// <para>
    /// <b>On the driver, not on an element</b>, because it is a question about the app: a
    /// dispatcher belongs to the app and every element would give the same answer. Putting it on
    /// an element would also mean every control that wanted to be waited on had to declare a verb
    /// that has nothing to do with that control.
    /// </para>
    /// <para>
    /// It does not promise that nothing new will be queued. An app with a running animation or a
    /// live timer is never idle by any definition, and reporting that is better than a number
    /// that hides it.
    /// </para>
    /// </remarks>
    /// <param name="timeoutMs">How long to give the queue to drain.</param>
    /// <returns>Whether it drained within the budget.</returns>
    bool IsIdle(int timeoutMs = 2000) => true;

    /// <summary>
    /// Where the app currently is, in the terms its own navigation model uses.
    /// </summary>
    /// <remarks>
    /// A Shell app answers with its route. An app built on <c>NavigationPage</c> has no route, so
    /// it answers with the identity of the page on top - which is what a test asserting "we are
    /// on the hub" actually means.
    /// </remarks>
    /// <returns>The route, or the top page's identity.</returns>
    string CurrentRoute() => string.Empty;
    
    /// <summary>
    /// Refreshes the current page/view.
    /// </summary>
    void Refresh();
    
    /// <summary>
    /// Takes a screenshot of the current state.
    /// </summary>
    byte[] TakeScreenshot();
    
    /// <summary>
    /// Resets the application state (terminates and relaunches).
    /// </summary>
    void ResetAppState();
    
    #endregion
    
    #region Script Execution
    
    /// <summary>
    /// Executes a script command (e.g., mobile gestures, platform-specific actions).
    /// </summary>
    /// <param name="script">The script name (e.g., "mobile: longClickGesture", "windows: click").</param>
    /// <param name="args">Arguments to pass to the script.</param>
    /// <returns>The script result, or null.</returns>
    object? ExecuteScript(string script, params object[] args);
    
    #endregion
    
    #region Dialogs

    /// <summary>
    /// Raises a menu item by its <c>AutomationId</c>, without opening any menu.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The item is not in the tree this client can search.</b> MAUI does not propagate
    /// <c>AutomationId</c> to menu chrome on Windows (dotnet/maui#3996), which the navigation
    /// probe measures rather than assumes: <c>PageMenuFile</c> and <c>PageMenuFileNew</c> are
    /// findable by neither id nor name. A context flyout is further out of reach still - it does
    /// not exist until someone right-clicks. So the app is asked by id, because the app is the
    /// only party that has the id.
    /// </para>
    /// <para>
    /// <b>What this replaces is the suite's remaining positional input.</b> Reaching a context
    /// menu item meant a right-click at one coordinate followed by a click at another, with the
    /// app holding the foreground throughout. Both coordinates are guesses about where the
    /// platform drew something.
    /// </para>
    /// <para>
    /// Nothing opens. A test that means "the menu opens and shows these items" is a test about
    /// the menu and still needs the pointer - see <see cref="IMauiElement.RightClick"/>.
    /// </para>
    /// </remarks>
    /// <param name="automationId">The menu item's <c>AutomationId</c>.</param>
    /// <exception cref="Brinell.Core.Exceptions.BrinellException">
    /// No such item, or the app offers no menu verbs.
    /// </exception>
    void InvokeMenuItem(string automationId)
        => throw new NotSupportedException(
            $"Menu items are not implemented for {GetType().Name}.");

    /// <summary>
    /// Raises a toolbar item on the page on screen by its <c>AutomationId</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The only route to a toolbar item on Windows that does not take the mouse.</b> A MAUI
    /// <c>ToolbarItem</c>'s automation peer accepts the Invoke pattern, reports success and raises
    /// nothing - measured four ways. The app raises the item itself, through the same entry point
    /// its toolbar uses, so the same command runs.
    /// </para>
    /// <para>
    /// Android and iOS find the item by its accessibility id and tap it, which is the ordinary
    /// route there. <c>ToolbarButton</c> calls this on every platform; there used to be a
    /// <c>SupportsToolbarVerb</c> question in front of it, whose false branch was that tap.
    /// </para>
    /// </remarks>
    /// <param name="automationId">The toolbar item's <c>AutomationId</c>.</param>
    /// <exception cref="Brinell.Core.Exceptions.BrinellException">
    /// The item is disabled, or no page on screen has an item with that id.
    /// </exception>
    void InvokeToolbarItem(string automationId)
        => throw new NotSupportedException(
            $"Toolbar items are not implemented for {GetType().Name}.");

    /// <summary>Opens a Shell's flyout.</summary>
    /// <remarks>
    /// <b>One property on the app, replacing a hamburger button nothing can find.</b> Shell
    /// draws that button as chrome with no <c>AutomationId</c>, so every previous route to it was
    /// a guess: by name, by control type, or by clicking where it usually is.
    /// </remarks>
    /// <exception cref="Brinell.Core.Exceptions.BrinellException">
    /// The app has no Shell, or does not declare the verb.
    /// </exception>
    void OpenFlyout()
        => throw new NotSupportedException(
            $"Flyouts are not implemented for {GetType().Name}.");

    /// <summary>
    /// Whether the app offers the flyout verbs, so a caller can take that route rather than the
    /// chrome.
    /// </summary>
    /// <remarks>
    /// A question, asked before commanding - not a command that returns false. Stage G step 32:
    /// the Shell control object dismissed its flyout by tapping a light-dismiss layer that does not
    /// support Invoke, and two tests failed on it; where the app declares the verbs, there is no
    /// chrome to guess at.
    /// </remarks>
    bool SupportsFlyoutVerbs => false;

    /// <summary>Closes a Shell's flyout. See <see cref="OpenFlyout"/>.</summary>
    /// <exception cref="Brinell.Core.Exceptions.BrinellException">
    /// The app has no Shell, or does not declare the verb.
    /// </exception>
    void CloseFlyout()
        => throw new NotSupportedException(
            $"Flyouts are not implemented for {GetType().Name}.");

    /// <summary>
    /// Whether a Shell's flyout is showing.
    /// </summary>
    /// <remarks>
    /// The question, so a caller does not have to establish it by opening the flyout and seeing
    /// what changes. Windows keeps the pane's items in the tree once it has been opened, hidden
    /// rather than removed, so counting them answers differently on a fresh launch than on the
    /// second test in a run - which is what asking the app avoids.
    /// </remarks>
    /// <returns>Whether it is presented.</returns>
    /// <exception cref="NotSupportedException">The app does not declare the read.</exception>
    bool IsFlyoutOpen()
        => throw new NotSupportedException(
            $"Flyouts are not implemented for {GetType().Name}.");

    /// <summary>
    /// What the alert on screen is asking, or null when none is open.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Only the app can answer this, and only if it says so.</b> On Windows the popup
    /// publishes its title and its buttons, and those are read straight off the platform through
    /// <c>ContentDialog</c>. The message is the exception: WinUI puts it in the dialog's content
    /// area beside a second copy of the title, so from outside it can only be identified as "the
    /// text that is not the title" - which is wrong for an alert whose message and title read
    /// alike, and wrong quietly.
    /// </para>
    /// <para>
    /// There is no supported way to observe <c>DisplayAlert</c> either: MAUI signals its own
    /// platform layer through <c>MessagingCenter</c>, which is internal in MAUI 10. So the app
    /// under test raises its alerts through <c>BrinellAlerts</c> and this reports what they were
    /// given. An app that does not answers null, and its title and buttons are still readable.
    /// </para>
    /// </remarks>
    /// <returns>The alert's four strings, or null when nothing is open or nothing declares it.</returns>
    AlertContents? CurrentAlert() => null;

    /// <summary>
    /// Gets the active native dialog root, or null when no dialog is open.
    /// </summary>
    /// <remarks>
    /// On Windows, WinUI3 dialogs live in a sibling top-level window. Other platforms expose
    /// dialogs in the normal element tree. The returned element is the root used to scope all
    /// dialog content lookups.
    /// </remarks>
    IMauiElement? TryFindActiveDialogRoot();

    #endregion
    
    #region Scrolling

    /// <summary>
    /// Finds an element by scrolling a container until it enters the accessibility tree.
    /// </summary>
    /// <remarks>
    /// The neutral form of "the tree omits what is not rendered", which every backend has some
    /// version of: UiAutomator2 drops scrolled-off-screen elements, while UIA and the DOM keep
    /// them but drop virtualised ones. A driver whose backend hides nothing relevant answers
    /// null, which is an answer rather than a gap.
    /// </remarks>
    /// <param name="container">
    /// The container to scroll, or null to let the platform pick the scrolling container on
    /// screen.
    /// </param>
    /// <param name="locator">The locator for the element.</param>
    /// <returns>
    /// The element once it is on screen and still, or null when scrolling does not reach it.
    /// </returns>
    IMauiElement? TryFindByScrollingWithin(IMauiElement? container, Locator locator);

    #endregion
}
