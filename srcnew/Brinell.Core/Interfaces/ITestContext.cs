using Brinell.Core.Configuration;
using Brinell.Core.Locators;
using Brinell.Core.Logging;

namespace Brinell.Core.Interfaces;

/// <summary>
/// Base test context interface (non-generic).
/// Manages test execution environment, navigation, and screenshots.
/// </summary>
public interface ITestContext : IDisposable
{
    /// <summary>
    /// Timeout configuration for this test context.
    /// </summary>
    TimeoutSettings Timeouts { get; }
    
    /// <summary>
    /// Logger for test actions and diagnostics.
    /// </summary>
    ITestLogger Logger { get; }
    
    /// <summary>
    /// Navigate to a destination (URL for web, route for mobile).
    /// </summary>
    void NavigateTo(string destination);
    
    /// <summary>
    /// Navigate back in history.
    /// </summary>
    void NavigateBack();
    
    /// <summary>
    /// Refresh the current page/screen.
    /// </summary>
    void Refresh();
    
    /// <summary>
    /// Capture a screenshot as byte array.
    /// </summary>
    byte[] TakeScreenshot();
    
    /// <summary>
    /// Save a screenshot to the specified path.
    /// </summary>
    void SaveScreenshot(string path);
    
    /// <summary>
    /// Reset application state (clear cache, cookies, etc.).
    /// </summary>
    void ResetAppState();
}

/// <summary>
/// Generic test context providing typed element finding from driver root.
/// TElement is the platform's native element type.
/// </summary>
public interface ITestContext<TElement> : ITestContext, IElementScope<TElement>
{
    // Inherits from ITestContext:
    // - Timeouts, Logger, Navigation, Screenshots, ResetAppState
    
    // Inherits from IElementScope<TElement>:
    // - TElement? TryFindElement(Locator locator);
    // - TElement FindElement(Locator locator);
    // - IReadOnlyList<TElement> FindElements(Locator locator);
}
