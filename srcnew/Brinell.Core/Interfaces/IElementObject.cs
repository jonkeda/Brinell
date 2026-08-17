namespace Brinell.Core.Interfaces;

/// <summary>
/// Base interface for all controls in the Brinell framework.
/// Provides identity, state querying, waiting, and assertion capabilities.
/// Action methods return TScope for fluent method chaining.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public interface IElementObject<TScope>
{
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
    /// <returns>The containing scope for fluent chaining.</returns>
    TScope AssertExists(bool? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Assert element visibility matches expected value.
    /// If expected is null, returns immediately (skip).
    /// </summary>
    /// <returns>The containing scope for fluent chaining.</returns>
    TScope AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);
    
    /// <summary>
    /// Assert element enabled state matches expected value.
    /// If expected is null, returns immediately (skip).
    /// </summary>
    /// <returns>The containing scope for fluent chaining.</returns>
    TScope AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null);
    
}
