namespace Brinell.Core.Abstractions;

/// <summary>
/// Interface for page objects. Each platform provides its own PageBase implementation.
/// </summary>
public interface IPageObject
{
    /// <summary>
    /// Name of the page for logging.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// The AutomationId of the page root element.
    /// </summary>
    string AutomationId { get; }
    
    /// <summary>
    /// The test context.
    /// </summary>
    ITestContext Context { get; }
    
    /// <summary>
    /// Check if the page is currently displayed.
    /// </summary>
    bool IsDisplayed();
    
    /// <summary>
    /// Check if the page is ready for interaction.
    /// </summary>
    bool IsReady();
    
    /// <summary>
    /// Wait for the page to be displayed.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if page became displayed within timeout.</returns>
    bool WaitForDisplayed(int? timeoutMs = null);
    
    /// <summary>
    /// Wait for the page to be ready.
    /// </summary>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if page became ready within timeout.</returns>
    bool WaitForReady(int? timeoutMs = null);
    
    /// <summary>
    /// Check page is displayed - throws if not.
    /// </summary>
    void CheckDisplayed(int? timeoutMs = null);
    
    /// <summary>
    /// Check page is ready - throws if not.
    /// </summary>
    void CheckReady(int? timeoutMs = null);
    
    /// <summary>
    /// Take a screenshot of the current page.
    /// </summary>
    string? TakeScreenshot(string suffix = "");
}
