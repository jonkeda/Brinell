namespace Brinell.Core.Abstractions.Controls;

/// <summary>
/// Interface for controls that support form validation.
/// Provides methods to check validation state and retrieve validation errors.
/// </summary>
public interface IValidatableControl : IControlObject
{
    /// <summary>
    /// Immediate check if the control is in valid state.
    /// </summary>
    bool IsValid();

    /// <summary>
    /// Wait for the control to become valid or invalid.
    /// </summary>
    /// <param name="expected">Whether to wait for valid (true) or invalid (false) state.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if the expected state was reached within timeout.</returns>
    bool WaitValid(bool expected = true, int? timeoutMs = null);

    /// <summary>
    /// Get all validation error messages for this control.
    /// </summary>
    /// <returns>List of validation error messages, empty if valid.</returns>
    IReadOnlyList<string> GetValidationErrors();

    /// <summary>
    /// Check if the control has a specific validation error.
    /// </summary>
    /// <param name="errorText">The error text to search for (partial match).</param>
    /// <returns>True if the control has an error containing the specified text.</returns>
    bool HasValidationError(string errorText);

    /// <summary>
    /// Assert the control is in valid state.
    /// Captures screenshot on failure.
    /// </summary>
    /// <param name="message">Optional custom assertion message.</param>
    void AssertValid(string? message = null);

    /// <summary>
    /// Assert the control is in invalid state.
    /// Captures screenshot on failure.
    /// </summary>
    /// <param name="message">Optional custom assertion message.</param>
    void AssertInvalid(string? message = null);

    /// <summary>
    /// Assert the control has a specific validation error.
    /// Captures screenshot on failure.
    /// </summary>
    /// <param name="errorText">The expected error text (partial match).</param>
    /// <param name="message">Optional custom assertion message.</param>
    void AssertHasValidationError(string errorText, string? message = null);

    /// <summary>
    /// Assert the control does not have any validation errors.
    /// Captures screenshot on failure.
    /// </summary>
    /// <param name="message">Optional custom assertion message.</param>
    void AssertNoValidationErrors(string? message = null);
}
