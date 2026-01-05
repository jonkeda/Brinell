namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for controls that support focus management.
/// </summary>
public interface IFocusableControl : IControlObject
{
    /// <summary>
    /// Check if the control currently has focus.
    /// </summary>
    /// <returns>True if the control has focus.</returns>
    bool IsFocused();

    /// <summary>
    /// Wait for the control to have or not have focus.
    /// </summary>
    /// <param name="expected">Whether to wait for focused (true) or unfocused (false).</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if expected state reached within timeout.</returns>
    bool WaitFocused(bool expected = true, int? timeoutMs = null);

    /// <summary>
    /// Set focus to this control.
    /// </summary>
    void Focus();

    /// <summary>
    /// Remove focus from this control.
    /// </summary>
    void Blur();

    /// <summary>
    /// Assert the control has focus.
    /// Captures screenshot on failure.
    /// </summary>
    /// <param name="message">Optional custom assertion message.</param>
    void AssertFocused(string? message = null);

    /// <summary>
    /// Assert the control does not have focus.
    /// Captures screenshot on failure.
    /// </summary>
    /// <param name="message">Optional custom assertion message.</param>
    void AssertNotFocused(string? message = null);
}
