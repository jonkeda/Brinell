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
    bool IsAtNavigationRoot() => true;

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
