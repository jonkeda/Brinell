namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Base interface for all UI control objects.
/// Defines the Is/Wait/Check/Assert pattern for control state verification.
/// </summary>
public interface IControlObject
{
    /// <summary>
    /// AutomationId used to locate this control.
    /// </summary>
    string AutomationId { get; }
    
    /// <summary>
    /// The parent page object (may be null for global controls).
    /// </summary>
    IPageObject? Page { get; }

    #region Exists
    
    /// <summary>
    /// Immediate check if element exists (no wait).
    /// </summary>
    bool IsExists();
    
    /// <summary>
    /// Wait for element to exist or not exist.
    /// </summary>
    bool WaitExists(bool expected = true, int? timeoutMs = null);
    
    /// <summary>
    /// Check element exists - waits and throws if not met.
    /// </summary>
    void CheckExists(bool expected = true, int? timeoutMs = null);
    
    /// <summary>
    /// Assert element exists.
    /// </summary>
    void AssertExists(string? message = null);
    
    /// <summary>
    /// Assert element does not exist.
    /// </summary>
    void AssertNotExists(string? message = null);
    
    #endregion

    #region Visible
    
    /// <summary>
    /// Immediate check if element is visible (no wait).
    /// </summary>
    bool IsVisible();
    
    /// <summary>
    /// Wait for element to be visible or not visible.
    /// </summary>
    bool WaitVisible(bool expected = true, int? timeoutMs = null);
    
    /// <summary>
    /// Check element visibility - waits and throws if not met.
    /// </summary>
    void CheckVisible(bool expected = true, int? timeoutMs = null);
    
    /// <summary>
    /// Assert element is visible.
    /// </summary>
    void AssertVisible(string? message = null);
    
    /// <summary>
    /// Assert element is not visible.
    /// </summary>
    void AssertNotVisible(string? message = null);
    
    #endregion

    #region Enabled
    
    /// <summary>
    /// Immediate check if element is enabled (no wait).
    /// </summary>
    bool IsEnabled();
    
    /// <summary>
    /// Wait for element to be enabled or disabled.
    /// </summary>
    bool WaitEnabled(bool expected = true, int? timeoutMs = null);
    
    /// <summary>
    /// Check element enabled state - waits and throws if not met.
    /// </summary>
    void CheckEnabled(bool expected = true, int? timeoutMs = null);
    
    /// <summary>
    /// Assert element is enabled.
    /// </summary>
    void AssertEnabled(string? message = null);
    
    /// <summary>
    /// Assert element is disabled.
    /// </summary>
    void AssertDisabled(string? message = null);
    
    #endregion

    #region Text
    
    /// <summary>
    /// Get element text/value.
    /// </summary>
    string GetText();
    
    /// <summary>
    /// Assert text equals expected value.
    /// </summary>
    void AssertTextEquals(string expected, string? message = null);
    
    /// <summary>
    /// Assert text contains expected value.
    /// </summary>
    void AssertTextContains(string expected, string? message = null);
    
    #endregion
}
