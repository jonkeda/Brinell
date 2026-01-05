using Brinell.Html.Playwright.Controls;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Samples.Blazor.PlaywrightTests.PageObjects;

/// <summary>
/// Page object for the Validation page.
/// </summary>
public class ValidationPage : PageBase
{
    public override string AutomationId => "[data-automation-id='ValidationPage']";

    // Title and status elements
    public LabelControl ValidationTitle { get; }
    public LabelControl ValidationSummary { get; }
    public LabelControl SuccessMessage { get; }

    // Input fields
    public TextInputControl RequiredInput { get; }
    public TextInputControl EmailInput { get; }
    public TextInputControl PhoneInput { get; }
    public TextInputControl MinLengthInput { get; }
    public TextInputControl MaxLengthInput { get; }
    public TextInputControl RangeInput { get; }
    public TextInputControl RegexInput { get; }
    public TextInputControl PasswordInput { get; }
    public TextInputControl ConfirmPasswordInput { get; }

    // Error labels
    public LabelControl RequiredError { get; }
    public LabelControl EmailError { get; }
    public LabelControl PhoneError { get; }
    public LabelControl MinLengthError { get; }
    public LabelControl MaxLengthError { get; }
    public LabelControl RangeError { get; }
    public LabelControl RegexError { get; }
    public LabelControl PasswordError { get; }
    public LabelControl ConfirmPasswordError { get; }

    // Buttons
    public ButtonControl SubmitButton { get; }
    public ButtonControl ValidateButton { get; }
    public ButtonControl ClearButton { get; }

    public ValidationPage(PlaywrightTestContext context) : base(context)
    {
        // Title and status
        ValidationTitle = new LabelControl(context, this, "[data-automation-id='ValidationTitle']");
        ValidationSummary = new LabelControl(context, this, "[data-automation-id='ValidationSummary']");
        SuccessMessage = new LabelControl(context, this, "[data-automation-id='SuccessMessage']");

        // Inputs
        RequiredInput = new TextInputControl(context, this, "[data-automation-id='RequiredInput']");
        EmailInput = new TextInputControl(context, this, "[data-automation-id='EmailInput']");
        PhoneInput = new TextInputControl(context, this, "[data-automation-id='PhoneInput']");
        MinLengthInput = new TextInputControl(context, this, "[data-automation-id='MinLengthInput']");
        MaxLengthInput = new TextInputControl(context, this, "[data-automation-id='MaxLengthInput']");
        RangeInput = new TextInputControl(context, this, "[data-automation-id='RangeInput']");
        RegexInput = new TextInputControl(context, this, "[data-automation-id='RegexInput']");
        PasswordInput = new TextInputControl(context, this, "[data-automation-id='PasswordInput']");
        ConfirmPasswordInput = new TextInputControl(context, this, "[data-automation-id='ConfirmPasswordInput']");

        // Error labels
        RequiredError = new LabelControl(context, this, "[data-automation-id='RequiredError']");
        EmailError = new LabelControl(context, this, "[data-automation-id='EmailError']");
        PhoneError = new LabelControl(context, this, "[data-automation-id='PhoneError']");
        MinLengthError = new LabelControl(context, this, "[data-automation-id='MinLengthError']");
        MaxLengthError = new LabelControl(context, this, "[data-automation-id='MaxLengthError']");
        RangeError = new LabelControl(context, this, "[data-automation-id='RangeError']");
        RegexError = new LabelControl(context, this, "[data-automation-id='RegexError']");
        PasswordError = new LabelControl(context, this, "[data-automation-id='PasswordError']");
        ConfirmPasswordError = new LabelControl(context, this, "[data-automation-id='ConfirmPasswordError']");

        // Buttons
        SubmitButton = new ButtonControl(context, this, "[data-automation-id='SubmitButton']");
        ValidateButton = new ButtonControl(context, this, "[data-automation-id='ValidateButton']");
        ClearButton = new ButtonControl(context, this, "[data-automation-id='ClearButton']");
    }

    public override bool IsDisplayed()
    {
        return ValidationTitle.IsVisible();
    }

    /// <summary>
    /// Check if the page is displayed asynchronously.
    /// </summary>
    public override async Task<bool> IsDisplayedAsync()
    {
        return await ValidationTitle.IsVisibleAsync();
    }

    /// <summary>
    /// Fill all required fields with valid data.
    /// </summary>
    public ValidationPage FillAllFieldsValid()
    {
        Log("FillAllFieldsValid()");
        RequiredInput.ClearAndEnter("Test Name");
        EmailInput.ClearAndEnter("test@example.com");
        PhoneInput.ClearAndEnter("123-456-7890");
        MinLengthInput.ClearAndEnter("hello world");
        MaxLengthInput.ClearAndEnter("short");
        RangeInput.ClearAndEnter("50");
        RegexInput.ClearAndEnter("abc123");
        PasswordInput.ClearAndEnter("password123");
        ConfirmPasswordInput.ClearAndEnter("password123");
        return this;
    }

    /// <summary>
    /// Fill all required fields with valid data asynchronously.
    /// </summary>
    public async Task<ValidationPage> FillAllFieldsValidAsync()
    {
        Log("FillAllFieldsValidAsync()");
        await RequiredInput.ClearAndEnterAsync("Test Name");
        await EmailInput.ClearAndEnterAsync("test@example.com");
        await PhoneInput.ClearAndEnterAsync("123-456-7890");
        await MinLengthInput.ClearAndEnterAsync("hello world");
        await MaxLengthInput.ClearAndEnterAsync("short");
        await RangeInput.ClearAndEnterAsync("50");
        await RegexInput.ClearAndEnterAsync("abc123");
        await PasswordInput.ClearAndEnterAsync("password123");
        await ConfirmPasswordInput.ClearAndEnterAsync("password123");
        return this;
    }
}
