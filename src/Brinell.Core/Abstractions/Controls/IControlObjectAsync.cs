namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Async variant of IControlObject for platforms supporting async operations.
/// Enables non-blocking UI test automation for better performance and resource utilization.
/// </summary>
/// <remarks>
/// Use async methods when:
/// - Testing performance-critical paths
/// - Running multiple parallel test operations
/// - Targeting platforms with native async support (HTML, Playwright)
/// 
/// Sync methods (IControlObject) are still preferred for:
/// - Desktop UI tests (WPF, WinForms)
/// - Game engine tests (Stride)
/// - Simple sequential tests
/// - Rapid test development
/// </remarks>
public interface IControlObjectAsync
{
    /// <summary>
    /// The automation ID of this control.
    /// </summary>
    string AutomationId { get; }

    /// <summary>
    /// The parent page object, if any.
    /// </summary>
    IPageObject? Page { get; }

    #region Existence Checks - Is/Wait/Check/Assert

    /// <summary>
    /// Check if element exists (non-blocking, immediate state).
    /// </summary>
    ValueTask<bool> IsExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Wait for element to exist or be gone.
    /// </summary>
    /// <param name="expected">True to wait for element to exist, false to wait for element to be gone.</param>
    /// <param name="timeoutMs">Timeout in milliseconds, or null for default timeout.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    ValueTask<bool> WaitExistsAsync(bool expected = true, int? timeoutMs = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check that element exists or is gone, throw if not.
    /// </summary>
    ValueTask CheckExistsAsync(bool expected = true, int? timeoutMs = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assert element exists.
    /// </summary>
    ValueTask AssertExistsAsync(string? message = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assert element does not exist.
    /// </summary>
    ValueTask AssertNotExistsAsync(string? message = null, CancellationToken cancellationToken = default);

    #endregion

    #region Visibility Checks - Is/Wait/Check/Assert

    /// <summary>
    /// Check if element is visible (non-blocking, immediate state).
    /// </summary>
    ValueTask<bool> IsVisibleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Wait for element to be visible or hidden.
    /// </summary>
    /// <param name="expected">True to wait for visible, false to wait for hidden.</param>
    /// <param name="timeoutMs">Timeout in milliseconds, or null for default timeout.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    ValueTask<bool> WaitVisibleAsync(bool expected = true, int? timeoutMs = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check that element is visible or hidden, throw if not.
    /// </summary>
    ValueTask CheckVisibleAsync(bool expected = true, int? timeoutMs = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assert element is visible.
    /// </summary>
    ValueTask AssertVisibleAsync(string? message = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assert element is not visible.
    /// </summary>
    ValueTask AssertNotVisibleAsync(string? message = null, CancellationToken cancellationToken = default);

    #endregion

    #region Enabled State Checks - Is/Wait/Check/Assert

    /// <summary>
    /// Check if element is enabled (non-blocking, immediate state).
    /// </summary>
    ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Wait for element to be enabled or disabled.
    /// </summary>
    /// <param name="expected">True to wait for enabled, false to wait for disabled.</param>
    /// <param name="timeoutMs">Timeout in milliseconds, or null for default timeout.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    ValueTask<bool> WaitEnabledAsync(bool expected = true, int? timeoutMs = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check that element is enabled or disabled, throw if not.
    /// </summary>
    ValueTask CheckEnabledAsync(bool expected = true, int? timeoutMs = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assert element is enabled.
    /// </summary>
    ValueTask AssertEnabledAsync(string? message = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assert element is disabled.
    /// </summary>
    ValueTask AssertDisabledAsync(string? message = null, CancellationToken cancellationToken = default);

    #endregion

    #region Text Access - Get/Wait/Check/Assert

    /// <summary>
    /// Get element text (non-blocking, immediate state).
    /// </summary>
    ValueTask<string> GetTextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Wait for element text to match expected value.
    /// </summary>
    ValueTask<bool> WaitTextAsync(string expected, int? timeoutMs = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check that element text matches expected, throw if not.
    /// </summary>
    ValueTask CheckTextAsync(string expected, int? timeoutMs = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assert element text equals expected.
    /// </summary>
    ValueTask AssertTextEqualsAsync(string expected, string? message = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assert element text contains substring.
    /// </summary>
    ValueTask AssertTextContainsAsync(string substring, string? message = null, CancellationToken cancellationToken = default);

    #endregion
}
