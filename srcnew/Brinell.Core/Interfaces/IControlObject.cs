using Brinell.Core.Locators;

namespace Brinell.Core.Interfaces;

/// <summary>
/// Base interface for all controls in the Brinell framework.
/// Provides identity, state querying, waiting, and assertion capabilities.
/// </summary>
public interface IControlObject
{
    // Identity
    
    /// <summary>
    /// The locator used to find this control in the UI tree.
    /// </summary>
    Locator Locator { get; }
    
    /// <summary>
    /// The element scope (page or container) for this control.
    /// </summary>
    IElementScope Scope { get; }
    
    /// <summary>
    /// The page containing this control.
    /// Returns null if Scope is not a page and doesn't have a page ancestor.
    /// </summary>
    IPageObject? Page { get; }
    
    // State (immediate, no waiting)
    
    /// <summary>
    /// Check if the element exists in the UI tree.
    /// </summary>
    bool IsExists();
    
    /// <summary>
    /// Check if the element is visible.
    /// Returns null if element doesn't exist.
    /// </summary>
    bool? IsVisible();
    
    /// <summary>
    /// Check if the element is enabled.
    /// Returns null if element doesn't exist.
    /// </summary>
    bool? IsEnabled();
    
    // Waiting (poll until condition or timeout)
    
    /// <summary>
    /// Wait until element existence matches expected value.
    /// If expected is null, returns true immediately (skip).
    /// </summary>
    bool WaitExists(bool? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Wait until element visibility matches expected value.
    /// If expected is null, returns true immediately (skip).
    /// </summary>
    bool WaitVisible(bool? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Wait until element enabled state matches expected value.
    /// If expected is null, returns true immediately (skip).
    /// </summary>
    bool WaitEnabled(bool? expected, int? timeoutMs = null);
    
    // Assertions (throw on failure)
    
    /// <summary>
    /// Assert element existence matches expected value.
    /// If expected is null, returns immediately (skip).
    /// </summary>
    void AssertExists(bool? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Assert element visibility matches expected value.
    /// If expected is null, returns immediately (skip).
    /// </summary>
    void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Assert element enabled state matches expected value.
    /// If expected is null, returns immediately (skip).
    /// </summary>
    void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null);
    
    // Text
    
    /// <summary>
    /// Get the text content of the control.
    /// Returns null if element not found or has no text.
    /// </summary>
    string? GetText(int? timeoutMs = null);
    
    /// <summary>
    /// Wait until text content matches expected value.
    /// If expected is null, returns true immediately (skip).
    /// </summary>
    bool WaitText(string? expected, int? timeoutMs = null);
    
    /// <summary>
    /// Assert text content matches expected value exactly.
    /// If expected is null, returns immediately (skip).
    /// </summary>
    void AssertText(string? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Assert text content contains expected substring.
    /// If expected is null, returns immediately (skip).
    /// </summary>
    void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);
    
    // Attributes
    
    /// <summary>
    /// Get an attribute value from the element.
    /// Returns null if attribute or element doesn't exist.
    /// </summary>
    string? GetAttribute(string name);
}
