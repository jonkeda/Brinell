using Brinell.Core.ControlObject6.Locators;

namespace Brinell.Core.ControlObject6.Interfaces;

/// <summary>
/// Base interface for all control objects.
/// Provides existence, visibility, and text verification capabilities.
/// </summary>
public interface IControlObject
{
    /// <summary>
    /// The locator used to find this control.
    /// </summary>
    ControlLocator Locator { get; }

    /// <summary>
    /// The page that contains this control (may be null for standalone controls).
    /// </summary>
    IPageObject? Page { get; }

    #region Existence

    /// <summary>
    /// Immediately checks if the element exists in the DOM/visual tree.
    /// Does not wait or retry.
    /// </summary>
    bool IsExists();

    /// <summary>
    /// Waits for the element to exist or not exist.
    /// If expected is null, returns true immediately (skip operation).
    /// </summary>
    /// <param name="expected">Expected existence state, or null to skip.</param>
    /// <param name="timeoutMs">Timeout in milliseconds, or null for default.</param>
    /// <returns>True if expected state was reached, false if timed out.</returns>
    bool WaitExists(bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Checks that element exists/doesn't exist, throwing on failure.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    /// <param name="expected">Expected existence state, or null to skip.</param>
    /// <param name="timeoutMs">Timeout in milliseconds, or null for default.</param>
    void CheckExists(bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts that element exists/doesn't exist for test verification.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    /// <param name="expected">Expected existence state, or null to skip.</param>
    /// <param name="message">Custom assertion message.</param>
    /// <param name="timeoutMs">Timeout in milliseconds, or null for default.</param>
    void AssertExists(bool? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region Visibility

    /// <summary>
    /// Immediately checks if the element is visible.
    /// Does not wait or retry.
    /// </summary>
    bool IsVisible();

    /// <summary>
    /// Waits for the element to become visible or hidden.
    /// If expected is null, returns true immediately (skip operation).
    /// </summary>
    bool WaitVisible(bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Checks that element is visible/hidden, throwing on failure.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void CheckVisible(bool? expected, int? timeoutMs = null);

    /// <summary>
    /// Asserts that element is visible/hidden for test verification.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region Text

    /// <summary>
    /// Gets the text content of the element.
    /// </summary>
    /// <param name="timeoutMs">Timeout for element to exist, or null for default.</param>
    string GetText(int? timeoutMs = null);

    /// <summary>
    /// Asserts the element's text equals the expected value.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertText(string? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Asserts the element's text contains the expected substring.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Asserts the element's text starts with the expected prefix.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertTextStartsWith(string? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Asserts the element's text ends with the expected suffix.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertTextEndsWith(string? expected, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Asserts the element's text matches the regex pattern.
    /// If pattern is null, does nothing (skip operation).
    /// </summary>
    void AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null);

    /// <summary>
    /// Asserts the element's text is empty or not empty.
    /// If expected is null, does nothing (skip operation).
    /// </summary>
    void AssertTextEmpty(bool? expected, string? message = null, int? timeoutMs = null);

    #endregion
}
