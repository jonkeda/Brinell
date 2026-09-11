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
    /// Navigates back in the navigation history.
    /// </summary>
    void NavigateBack();

    /// <summary>
    /// Navigates back without using real input, if the platform can.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Separate from <see cref="NavigateBack"/> because the answer matters. On Windows
    /// <c>NavigateBack</c> ends in a desktop-wide Alt+Left, which lands wherever the foreground
    /// happens to be and which several drivers swallow when <c>SendInput</c> is denied; a caller
    /// that needs to know whether it actually navigated cannot find out. This one says.
    /// </para>
    /// <para>
    /// <b>It never falls back to real input.</b> A false means the app published no semantic
    /// route to going back, which is a fact the caller may want to act on - by clicking the
    /// affordance a user would click, or by failing with something more useful than a test that
    /// silently stayed on the same page.
    /// </para>
    /// <para>
    /// Defaulted to false so a platform without a semantic route compiles and answers honestly.
    /// </para>
    /// </remarks>
    /// <returns>Whether the app navigated back.</returns>
    bool TryNavigateBack() => false;
    
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
