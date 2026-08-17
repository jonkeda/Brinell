namespace Brinell.Core.Interfaces;

/// <summary>
/// Base interface for all controls in the Brinell framework.
/// Provides identity, state querying, waiting, and assertion capabilities.
/// Action methods return TScope for fluent method chaining.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public interface IControlObject<TScope> : IElementObject<TScope>
{
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
    /// <returns>The containing scope for fluent chaining.</returns>
    TScope AssertText(string? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Assert text content contains expected substring.
    /// If expected is null, returns immediately (skip).
    /// </summary>
    /// <returns>The containing scope for fluent chaining.</returns>
    TScope AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);
    
    // Attributes
    
    /// <summary>
    /// Get an attribute value from the element.
    /// Returns null if attribute or element doesn't exist.
    /// </summary>
    string? GetAttribute(string name, int? timeoutMs);
}
