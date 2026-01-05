using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls.Base;

/// <summary>
/// Playwright base class for text input controls that support validation.
/// Provides validation state checking via CSS classes and validation message elements.
/// </summary>
public abstract class ValidatableControlBase : TextControlBase, IValidatableControl
{
    /// <summary>
    /// CSS selector for the validation error message element.
    /// Override this in derived classes if validation messages use different selectors.
    /// Default looks for .invalid-feedback sibling.
    /// </summary>
    protected virtual string? ValidationErrorSelector => null;

    /// <summary>
    /// CSS class that indicates invalid state.
    /// Default is "is-invalid" (Bootstrap).
    /// </summary>
    protected virtual string InvalidCssClass => "is-invalid";

    protected ValidatableControlBase(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected ValidatableControlBase(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected ValidatableControlBase(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Check if the control is in valid state.
    /// Returns true if the control does not have the invalid CSS class.
    /// </summary>
    public virtual bool IsValid()
    {
        var element = FindElement();
        if (element == null) return true;

        // Check for invalid CSS class
        if (HasClass(InvalidCssClass))
            return false;

        // Check if validation error message is visible
        if (!string.IsNullOrEmpty(ValidationErrorSelector))
        {
            var errorElement = _context.Page.Locator(ValidationErrorSelector);
            var isErrorVisible = errorElement.IsVisibleAsync().GetAwaiter().GetResult();
            if (isErrorVisible)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Check if the control is in valid state asynchronously.
    /// </summary>
    public virtual async Task<bool> IsValidAsync()
    {
        var element = await FindElementAsync();
        if (element == null) return true;

        var classAttr = await element.GetAttributeAsync("class");
        if (!string.IsNullOrEmpty(classAttr) && classAttr.Contains(InvalidCssClass))
            return false;

        if (!string.IsNullOrEmpty(ValidationErrorSelector))
        {
            var errorElement = _context.Page.Locator(ValidationErrorSelector);
            if (await errorElement.IsVisibleAsync())
                return false;
        }

        return true;
    }

    /// <summary>
    /// Wait for the control to become valid or invalid.
    /// </summary>
    public virtual bool WaitValid(bool expected = true, int? timeoutMs = null)
    {
        Log($"WaitValid(expected={expected})");
        return _context.WaitFor(
            () => IsValid() == expected,
            timeoutMs,
            $"control '{AutomationId}' {(expected ? "valid" : "invalid")}");
    }

    /// <summary>
    /// Wait for the control to become valid or invalid asynchronously.
    /// </summary>
    public virtual async Task<bool> WaitValidAsync(bool expected = true, int? timeoutMs = null)
    {
        Log($"WaitValidAsync(expected={expected})");
        return await _context.WaitForAsync(
            async () => await IsValidAsync() == expected,
            timeoutMs,
            $"control '{AutomationId}' {(expected ? "valid" : "invalid")}");
    }

    /// <summary>
    /// Get all validation error messages for this control.
    /// Looks for .invalid-feedback elements next to the control.
    /// </summary>
    public virtual IReadOnlyList<string> GetValidationErrors()
    {
        var errors = new List<string>();

        // Look for validation error messages
        var selector = ValidationErrorSelector ?? ".invalid-feedback";
        var locator = GetLocator();
        
        // Try to find error in parent context
        var parentSelector = BuildSelector();
        var errorLocator = _context.Page.Locator($"{parentSelector} ~ {selector}, {parentSelector} + {selector}");
        
        var count = errorLocator.CountAsync().GetAwaiter().GetResult();
        for (int i = 0; i < count; i++)
        {
            var text = errorLocator.Nth(i).TextContentAsync().GetAwaiter().GetResult()?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                errors.Add(text);
            }
        }

        // Also check for d-block invalid-feedback in card body context
        if (errors.Count == 0)
        {
            var altSelector = $".invalid-feedback.d-block";
            var altLocator = _context.Page.Locator(altSelector);
            var altCount = altLocator.CountAsync().GetAwaiter().GetResult();
            
            for (int i = 0; i < altCount; i++)
            {
                var text = altLocator.Nth(i).TextContentAsync().GetAwaiter().GetResult()?.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    errors.Add(text);
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Get all validation error messages asynchronously.
    /// </summary>
    public virtual async Task<IReadOnlyList<string>> GetValidationErrorsAsync()
    {
        var errors = new List<string>();
        var selector = ValidationErrorSelector ?? ".invalid-feedback";
        var parentSelector = BuildSelector();
        var errorLocator = _context.Page.Locator($"{parentSelector} ~ {selector}, {parentSelector} + {selector}");

        var count = await errorLocator.CountAsync();
        for (int i = 0; i < count; i++)
        {
            var text = (await errorLocator.Nth(i).TextContentAsync())?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                errors.Add(text);
            }
        }

        return errors;
    }

    /// <summary>
    /// Check if the control has a specific validation error.
    /// </summary>
    public virtual bool HasValidationError(string errorText)
    {
        var errors = GetValidationErrors();
        return errors.Any(e => e.Contains(errorText, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Assert the control is in valid state.
    /// </summary>
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
    /// </summary>
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
    /// </summary>
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
    /// </summary>
    public virtual void AssertNoValidationErrors(string? message = null)
    {
        AssertValid(message);
    }
}
