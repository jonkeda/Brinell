using System.Text.RegularExpressions;
using Brinell.Samples.Shared.Commands;
using Brinell.Samples.Shared.ViewModels;

namespace Brinell.Samples.Maui.App.ViewModels2;

/// <summary>
/// ViewModel for the Validation page demonstrating form validation controls.
/// </summary>
public partial class ValidationViewModel : ParentViewModel
{
    private string _requiredValue = string.Empty;
    private string _emailValue = string.Empty;
    private string _phoneValue = string.Empty;
    private string _minLengthValue = string.Empty;
    private string _maxLengthValue = string.Empty;
    private string _rangeValue = string.Empty;
    private string _regexValue = string.Empty;

    private string _requiredError = string.Empty;
    private string _emailError = string.Empty;
    private string _phoneError = string.Empty;
    private string _minLengthError = string.Empty;
    private string _maxLengthError = string.Empty;
    private string _rangeError = string.Empty;
    private string _regexError = string.Empty;

    private string _validationSummary = string.Empty;
    private string _successMessage = string.Empty;
    private int _errorCount;

    // Properties - Values
    public string RequiredValue
    {
        get => _requiredValue;
        set { SetProperty(ref _requiredValue, value); ValidateRequired(); }
    }

    public string EmailValue
    {
        get => _emailValue;
        set { SetProperty(ref _emailValue, value); ValidateEmail(); }
    }

    public string PhoneValue
    {
        get => _phoneValue;
        set { SetProperty(ref _phoneValue, value); ValidatePhone(); }
    }

    public string MinLengthValue
    {
        get => _minLengthValue;
        set { SetProperty(ref _minLengthValue, value); ValidateMinLength(); }
    }

    public string MaxLengthValue
    {
        get => _maxLengthValue;
        set { SetProperty(ref _maxLengthValue, value); ValidateMaxLength(); }
    }

    public string RangeValue
    {
        get => _rangeValue;
        set { SetProperty(ref _rangeValue, value); ValidateRange(); }
    }

    public string RegexValue
    {
        get => _regexValue;
        set { SetProperty(ref _regexValue, value); ValidateRegex(); }
    }

    // Properties - Errors
    public string RequiredError
    {
        get => _requiredError;
        set => SetProperty(ref _requiredError, value);
    }

    public string EmailError
    {
        get => _emailError;
        set => SetProperty(ref _emailError, value);
    }

    public string PhoneError
    {
        get => _phoneError;
        set => SetProperty(ref _phoneError, value);
    }

    public string MinLengthError
    {
        get => _minLengthError;
        set => SetProperty(ref _minLengthError, value);
    }

    public string MaxLengthError
    {
        get => _maxLengthError;
        set => SetProperty(ref _maxLengthError, value);
    }

    public string RangeError
    {
        get => _rangeError;
        set => SetProperty(ref _rangeError, value);
    }

    public string RegexError
    {
        get => _regexError;
        set => SetProperty(ref _regexError, value);
    }

    public string ValidationSummary
    {
        get => _validationSummary;
        set => SetProperty(ref _validationSummary, value);
    }

    public string SuccessMessage
    {
        get => _successMessage;
        set => SetProperty(ref _successMessage, value);
    }

    public int ErrorCount
    {
        get => _errorCount;
        set => SetProperty(ref _errorCount, value);
    }

    public IAsyncRelayCommand SubmitCommand { get; }
    public IAsyncRelayCommand ClearCommand { get; }

    public ValidationViewModel()
    {
        SubmitCommand = new AsyncRelayCommand(this, SubmitAsync);
        ClearCommand = new AsyncRelayCommand(this, ClearAsync);
    }

    private void ValidateRequired()
    {
        RequiredError = string.IsNullOrWhiteSpace(RequiredValue) ? "This field is required" : string.Empty;
        UpdateSummary();
    }

    private void ValidateEmail()
    {
        if (string.IsNullOrEmpty(EmailValue))
        {
            EmailError = string.Empty;
        }
        else if (!EmailRegex().IsMatch(EmailValue))
        {
            EmailError = "Invalid email format";
        }
        else
        {
            EmailError = string.Empty;
        }
        UpdateSummary();
    }

    private void ValidatePhone()
    {
        if (string.IsNullOrEmpty(PhoneValue))
        {
            PhoneError = string.Empty;
        }
        else if (!PhoneRegex().IsMatch(PhoneValue))
        {
            PhoneError = "Invalid phone format (use: 123-456-7890)";
        }
        else
        {
            PhoneError = string.Empty;
        }
        UpdateSummary();
    }

    private void ValidateMinLength()
    {
        MinLengthError = MinLengthValue.Length > 0 && MinLengthValue.Length < 5
            ? "Minimum 5 characters required"
            : string.Empty;
        UpdateSummary();
    }

    private void ValidateMaxLength()
    {
        MaxLengthError = MaxLengthValue.Length > 20
            ? "Maximum 20 characters allowed"
            : string.Empty;
        UpdateSummary();
    }

    private void ValidateRange()
    {
        if (string.IsNullOrEmpty(RangeValue))
        {
            RangeError = string.Empty;
        }
        else if (!int.TryParse(RangeValue, out int value) || value < 1 || value > 100)
        {
            RangeError = "Value must be between 1 and 100";
        }
        else
        {
            RangeError = string.Empty;
        }
        UpdateSummary();
    }

    private void ValidateRegex()
    {
        if (string.IsNullOrEmpty(RegexValue))
        {
            RegexError = string.Empty;
        }
        else if (!AlphanumericRegex().IsMatch(RegexValue))
        {
            RegexError = "Only alphanumeric characters allowed";
        }
        else
        {
            RegexError = string.Empty;
        }
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var errors = new List<string>();
        if (!string.IsNullOrEmpty(RequiredError)) errors.Add(RequiredError);
        if (!string.IsNullOrEmpty(EmailError)) errors.Add(EmailError);
        if (!string.IsNullOrEmpty(PhoneError)) errors.Add(PhoneError);
        if (!string.IsNullOrEmpty(MinLengthError)) errors.Add(MinLengthError);
        if (!string.IsNullOrEmpty(MaxLengthError)) errors.Add(MaxLengthError);
        if (!string.IsNullOrEmpty(RangeError)) errors.Add(RangeError);
        if (!string.IsNullOrEmpty(RegexError)) errors.Add(RegexError);

        ErrorCount = errors.Count;
        ValidationSummary = errors.Count > 0
            ? string.Join("\n", errors)
            : string.Empty;
        SuccessMessage = string.Empty;
    }

    private async Task SubmitAsync()
    {
        // Validate all fields
        ValidateRequired();
        ValidateEmail();
        ValidatePhone();
        ValidateMinLength();
        ValidateMaxLength();
        ValidateRange();
        ValidateRegex();

        if (ErrorCount == 0)
        {
            SuccessMessage = "Form submitted successfully!";
        }
        await Task.CompletedTask;
    }

    private async Task ClearAsync()
    {
        RequiredValue = string.Empty;
        EmailValue = string.Empty;
        PhoneValue = string.Empty;
        MinLengthValue = string.Empty;
        MaxLengthValue = string.Empty;
        RangeValue = string.Empty;
        RegexValue = string.Empty;

        RequiredError = string.Empty;
        EmailError = string.Empty;
        PhoneError = string.Empty;
        MinLengthError = string.Empty;
        MaxLengthError = string.Empty;
        RangeError = string.Empty;
        RegexError = string.Empty;

        ValidationSummary = string.Empty;
        SuccessMessage = "Form cleared";
        ErrorCount = 0;

        await Task.CompletedTask;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\d{3}-\d{3}-\d{4}$")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9]+$")]
    private static partial Regex AlphanumericRegex();
}
