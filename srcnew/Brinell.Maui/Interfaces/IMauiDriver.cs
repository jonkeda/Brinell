namespace Brinell.Maui.Interfaces;

/// <summary>
/// Abstraction over AppiumDriver to enable unit testing with Moq.
/// This interface can be mocked because it doesn't require an Appium connection.
/// </summary>
public interface IMauiDriver
{
    #region Element Finding
    
    /// <summary>
    /// Finds an element at the driver level.
    /// </summary>
    /// <param name="by">The locator to use.</param>
    /// <returns>The matching element.</returns>
    IMauiElement FindElement(By by);
    
    /// <summary>
    /// Finds all elements matching the locator.
    /// </summary>
    /// <param name="by">The locator to use.</param>
    /// <returns>A list of matching elements.</returns>
    IReadOnlyList<IMauiElement> FindElements(By by);
    
    #endregion
    
    #region Driver State
    
    /// <summary>
    /// Gets the page source of the current page.
    /// </summary>
    string PageSource { get; }
    
    /// <summary>
    /// Gets the current window handle.
    /// </summary>
    string CurrentWindowHandle { get; }
    
    /// <summary>
    /// Gets all window handles.
    /// </summary>
    IReadOnlyCollection<string> WindowHandles { get; }
    
    #endregion
    
    #region Session Management
    
    /// <summary>
    /// Quits the driver and closes all associated windows.
    /// </summary>
    void Quit();
    
    /// <summary>
    /// Closes the current window.
    /// </summary>
    void Close();
    
    #endregion
    
    #region Screenshots
    
    /// <summary>
    /// Takes a screenshot of the current screen.
    /// </summary>
    /// <returns>The screenshot.</returns>
    Screenshot GetScreenshot();
    
    #endregion
    
    #region Context Switching
    
    /// <summary>
    /// Gets or sets the current context (native/webview).
    /// </summary>
    string Context { get; set; }
    
    /// <summary>
    /// Gets all available contexts.
    /// </summary>
    IReadOnlyCollection<string> Contexts { get; }
    
    #endregion
    
    #region Escape Hatch
    
    /// <summary>
    /// Gets the underlying AppiumDriver for advanced scenarios.
    /// Use sparingly - prefer interface methods for testability.
    /// </summary>
    /// <returns>The wrapped AppiumDriver.</returns>
    AppiumDriver UnwrapDriver();
    
    #endregion
}
