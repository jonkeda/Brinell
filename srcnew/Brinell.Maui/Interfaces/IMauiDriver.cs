using Brinell.Core.Interfaces;
using Brinell.Maui.Enums;

namespace Brinell.Maui.Interfaces;

/// <summary>
/// The driver for a MAUI app under test: lookup from the app root, platform detection, context
/// switching for hybrid apps, window management, navigation and gestures.
/// </summary>
public interface IMauiDriver : IDiagnosticDriver, IDisposable
{
    #region Lookup

    /// <summary>
    /// Finds every element in the app matching <paramref name="locator"/>, in one attempt.
    /// </summary>
    /// <param name="locator">The locator strategy and value.</param>
    /// <returns>The matches; empty when none match now.</returns>
    IReadOnlyList<IMauiElement> FindElements(Locator locator);

    #endregion

    #region Session

    /// <summary>Closes the current window.</summary>
    void Close();

    /// <summary>Ends the session and closes the app.</summary>
    void Quit();

    /// <summary>Takes a screenshot of the app.</summary>
    byte[] GetScreenshot();

    #endregion

    #region Platform

    /// <summary>
    /// Gets the target platform (Windows, Android, iOS, macOS).
    /// </summary>
    MauiPlatform Platform { get; }

    #endregion

    #region The app

    /// <summary>
    /// The app itself, as an element: the application window on Windows, the hierarchy root on
    /// Android and iOS.
    /// </summary>
    IMauiElement AppElement { get; }

    /// <summary>Where this platform draws MAUI Shell's tabs and flyout.</summary>
    /// <exception cref="PlatformNotSupportedException">Shell has not been mapped on this platform.</exception>
    ShellChromeLocators ShellChrome
        => throw new PlatformNotSupportedException(
            $"Shell chrome has not been mapped for {GetType().Name}. Dump the tree and map it - "
            + "see .my/navigation/design-shell-sample-app.md.");

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
    /// <param name="automationId">The MAUI <c>AutomationId</c> of the target element.</param>
    /// <param name="gesture">The gesture to ask about.</param>
    /// <returns>Whether <see cref="PerformGesture(string, MauiGesture)"/> would work.</returns>
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
    /// <param name="automationId">The MAUI <c>AutomationId</c> of the target element.</param>
    /// <param name="gesture">The gesture to perform.</param>
    /// <param name="arg1">First argument, meaning defined per gesture.</param>
    /// <param name="arg2">Second argument, meaning defined per gesture.</param>
    /// <exception cref="NotSupportedException">
    /// This platform cannot perform the gesture on that element.
    /// </exception>
    void PerformGesture(string automationId, MauiGesture gesture, int arg1, int arg2)
        => throw new RouteUnavailableException(
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
    /// <exception cref="Brinell.Core.Exceptions.BrinellException">
    /// The app did not go back, with the specific reason.
    /// </exception>
    void NavigateBack();

    /// <summary>
    /// Whether the app is showing its first page, with nothing to go back to.
    /// </summary>
    /// <returns>Whether there is nothing to go back to.</returns>
    bool IsAtNavigationRoot() => NavigationDepth() <= 1;

    /// <summary>
    /// How many pages are on the app's navigation stack.
    /// </summary>
    /// <returns>The number of pages, where 1 means only the root.</returns>
    int NavigationDepth() => 1;

    /// <summary>
    /// Whether the app has finished the work it had queued.
    /// </summary>
    /// <param name="timeoutMs">How long to give the queue to drain.</param>
    /// <returns>Whether it drained within the budget.</returns>
    bool IsIdle(int timeoutMs = 2000) => true;

    /// <summary>
    /// Where the app currently is, in the terms its own navigation model uses.
    /// </summary>
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
    /// <param name="automationId">The menu item's <c>AutomationId</c>.</param>
    /// <exception cref="Brinell.Core.Exceptions.BrinellException">
    /// No such item, or the app offers no menu verbs.
    /// </exception>
    void InvokeMenuItem(string automationId)
        => throw new RouteUnavailableException(
            $"Menu items are not implemented for {GetType().Name}.");

    #endregion
}
