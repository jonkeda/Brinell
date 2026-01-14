namespace Brinell.Core.Interfaces;

/// <summary>
/// Base page interface (non-generic).
/// Represents a page, screen, or view in the application under test.
/// </summary>
public interface IPageObject : IElementScope
{
    /// <summary>
    /// The name of this page for logging and identification.
    /// </summary>
    string Name { get; }
    
    // Page state
    
    /// <summary>
    /// Check if the page is currently loaded and ready.
    /// </summary>
    bool IsLoaded(int? timeoutMs = null);
    
    /// <summary>
    /// Wait until page loaded state matches expected value.
    /// If expected is null, returns true immediately (skip).
    /// </summary>
    bool WaitLoaded(bool? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert page loaded state matches expected value.
    /// If expected is null, returns immediately (skip).
    /// </summary>
    void AssertLoaded(bool? expected, string? message = null, int? timeoutMs = null);
    
    // Title
    
    /// <summary>
    /// Get the page title.
    /// </summary>
    string? GetTitle(int? timeoutMs = null);
    
    /// <summary>
    /// Wait until page title matches expected value.
    /// If expected is null, returns true immediately (skip).
    /// </summary>
    bool WaitTitle(string? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert page title matches expected value.
    /// If expected is null, returns immediately (skip).
    /// </summary>
    void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);
    
    // Page operations
    
    /// <summary>
    /// Take a screenshot of the current page.
    /// </summary>
    void TakeScreenshot(string? filename = null, int? timeoutMs = null);
}

/// <summary>
/// Generic page interface with typed element finding.
/// TElement is the platform's native element type.
/// </summary>
public interface IPageObject<TElement> : IPageObject, IElementScope<TElement>
{
    // Inherits from IElementScope<TElement>:
    // TElement? TryFindElement(Locator locator);
    // TElement FindElement(Locator locator);
    // IReadOnlyList<TElement> FindElements(Locator locator);
}
