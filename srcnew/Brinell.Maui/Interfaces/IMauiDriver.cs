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

    #region The app

    /// <summary>
    /// The app itself, as an element: the application window on Windows, the hierarchy root on
    /// Android and iOS.
    /// </summary>
    /// <remarks>
    /// Answers the app-level members of <see cref="IMauiElement"/> - the flyout, the alert, the
    /// active dialog, targets reached by id. Control objects reach it through
    /// <see cref="IMauiTestContext.AppElement"/>.
    /// </remarks>
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
    /// <remarks>
    /// Addressed by id rather than by element so it also reaches controls Windows automation
    /// cannot see, such as a <c>SwipeView</c>.
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
    /// Pan takes a distance in device-independent pixels; pinch takes a percentage where 100 is no
    /// change. Other gestures ignore the arguments.
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
    /// Ask <see cref="IsAtNavigationRoot"/> first if "nothing to pop" is an expected outcome
    /// rather than a failure.
    /// </remarks>
    /// <exception cref="Brinell.Core.Exceptions.BrinellException">
    /// The app did not go back, with the specific reason.
    /// </exception>
    void NavigateBack();

    /// <summary>
    /// Whether the app is showing its first page, with nothing to go back to.
    /// </summary>
    /// <remarks>
    /// Defaults to true on a platform with no navigation model.
    /// </remarks>
    /// <returns>Whether there is nothing to go back to.</returns>
    bool IsAtNavigationRoot() => NavigationDepth() <= 1;

    /// <summary>
    /// How many pages are on the app's navigation stack.
    /// </summary>
    /// <remarks>
    /// A pop is started rather than awaited, so a caller unwinding several pages can watch the
    /// depth fall to know each pop landed.
    /// </remarks>
    /// <returns>The number of pages, where 1 means only the root.</returns>
    int NavigationDepth() => 1;

    /// <summary>
    /// Whether the app has finished the work it had queued.
    /// </summary>
    /// <remarks>
    /// Use this to wait for the UI to settle instead of sleeping. It does not promise that nothing
    /// new will be queued: an app with a running animation or a live timer is never idle.
    /// </remarks>
    /// <param name="timeoutMs">How long to give the queue to drain.</param>
    /// <returns>Whether it drained within the budget.</returns>
    bool IsIdle(int timeoutMs = 2000) => true;

    /// <summary>
    /// Where the app currently is, in the terms its own navigation model uses.
    /// </summary>
    /// <remarks>
    /// A Shell app answers with its route. An app built on <c>NavigationPage</c> answers with the
    /// identity of the page on top.
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
    /// MAUI does not propagate <c>AutomationId</c> to menu chrome on Windows (dotnet/maui#3996),
    /// and a context flyout does not exist until it is opened, so the app is asked to raise the
    /// item by id.
    /// </para>
    /// <para>
    /// Nothing opens. To test that the menu opens and shows its items, use
    /// <see cref="IMauiElement.RightClick"/>.
    /// </para>
    /// </remarks>
    /// <param name="automationId">The menu item's <c>AutomationId</c>.</param>
    /// <exception cref="Brinell.Core.Exceptions.BrinellException">
    /// No such item, or the app offers no menu verbs.
    /// </exception>
    void InvokeMenuItem(string automationId)
        => throw new NotSupportedException(
            $"Menu items are not implemented for {GetType().Name}.");

    #endregion
}
