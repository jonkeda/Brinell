using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// MAUI base class for text input controls that support validation.
/// Provides validation state checking via associated validation label controls.
/// </summary>
public abstract class ValidatableControlBase : TextControlBase, IValidatableControl
{
    /// <summary>
    /// The AutomationId of the validation error label for this control.
    /// Override this in derived classes to specify the error label.
    /// Return null if no validation label is available.
    /// </summary>
    protected virtual string? ValidationErrorLabelId => null;

    protected ValidatableControlBase(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected ValidatableControlBase(AppiumTestContext context, IPageObject? page, AppiumElement? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected ValidatableControlBase(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Check if the control is in valid state.
    /// Returns true if no validation error label is visible.
    /// </summary>
    public virtual bool IsValid()
    {
        if (string.IsNullOrEmpty(ValidationErrorLabelId))
            return true;

        return !_context.ElementIsVisible(ValidationErrorLabelId);
    }

    /// <summary>
    /// Wait for the control to become valid or invalid.
    /// </summary>
    /// <param name="expected">Whether to wait for valid (true) or invalid (false) state.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    /// <returns>True if the expected state was reached within timeout.</returns>
    public virtual bool WaitValid(bool expected = true, int? timeoutMs = null)
    {
        Log($"WaitValid(expected={expected})");
        return _context.WaitFor(
            () => IsValid() == expected,
            timeoutMs,
            $"control '{AutomationId}' {(expected ? "valid" : "invalid")}");
    }

    /// <summary>
    /// Get all validation error messages for this control.
    /// </summary>
    /// <returns>List of validation error messages, empty if valid.</returns>
    public virtual IReadOnlyList<string> GetValidationErrors()
    {
        if (string.IsNullOrEmpty(ValidationErrorLabelId))
            return Array.Empty<string>();

        var errorText = _context.GetElementText(ValidationErrorLabelId);
        if (string.IsNullOrEmpty(errorText))
            return Array.Empty<string>();

        return new[] { errorText };
    }

    /// <summary>
    /// Check if the control has a specific validation error.
    /// </summary>
    /// <param name="errorText">The error text to search for (partial match).</param>
    /// <returns>True if the control has an error containing the specified text.</returns>
    public virtual bool HasValidationError(string errorText)
    {
        var errors = GetValidationErrors();
        return errors.Any(e => e.Contains(errorText, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Assert the control is in valid state.
    /// Captures screenshot on failure.
    /// </summary>
    /// <param name="message">Optional custom assertion message.</param>
    public virtual void AssertValid(string? message = null)
    {
        if (!IsValid())
        {
            var errors = GetValidationErrors();
            var errorList = errors.Any() ? string.Join(", ", errors) : "(unknown errors)";
            ThrowAssertionFailed("Valid", "invalid", "valid",
                message ?? $"Expected control '{AutomationId}' to be valid but found errors: {errorList}");
        }
        LogAssertPass("Valid", "valid", "valid");
    }

    /// <summary>
    /// Assert the control is in invalid state.
    /// Captures screenshot on failure.
    /// </summary>
    /// <param name="message">Optional custom assertion message.</param>
    public virtual void AssertInvalid(string? message = null)
    {
        if (IsValid())
        {
            ThrowAssertionFailed("Invalid", "valid", "invalid",
                message ?? $"Expected control '{AutomationId}' to be invalid but it was valid.");
        }
        LogAssertPass("Invalid", "invalid", "invalid");
    }

    /// <summary>
    /// Assert the control has a specific validation error.
    /// Captures screenshot on failure.
    /// </summary>
    /// <param name="errorText">The expected error text (partial match).</param>
    /// <param name="message">Optional custom assertion message.</param>
    public virtual void AssertHasValidationError(string errorText, string? message = null)
    {
        if (!HasValidationError(errorText))
        {
            var errors = GetValidationErrors();
            var errorList = errors.Any() ? string.Join(", ", errors) : "(no errors)";
            ThrowAssertionFailed("HasValidationError", errorList, $"contains '{errorText}'",
                message ?? $"Expected control '{AutomationId}' to have validation error containing '{errorText}' but found: {errorList}");
        }
        LogAssertPass("HasValidationError", errorText, errorText);
    }

    /// <summary>
    /// Assert the control does not have any validation errors.
    /// Captures screenshot on failure.
    /// </summary>
    /// <param name="message">Optional custom assertion message.</param>
    public virtual void AssertNoValidationErrors(string? message = null)
    {
        AssertValid(message);
    }
}
