namespace Brinell.Samples.Maui.App.Models;

/// <summary>
/// Represents a validation result for form validation demonstrations.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    public static ValidationResult Success() => new() { IsValid = true };

    public static ValidationResult Error(string fieldName, string message) => new()
    {
        IsValid = false,
        FieldName = fieldName,
        ErrorMessage = message
    };
}

/// <summary>
/// Represents a form field with validation state.
/// </summary>
public class ValidatableField
{
    public string Value { get; set; } = string.Empty;
    public bool HasError { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string ValidationPattern { get; set; } = string.Empty;

    public void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }

    public void SetError(string message)
    {
        HasError = true;
        ErrorMessage = message;
    }
}
