using Brinell.Html.Controls;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Samples.Blazor.UITests.PageObjects;

/// <summary>
/// Page object for the Validation page.
/// </summary>
public class ValidationPage : PageBase
{
    public override string AutomationId => "[data-automation-id='ValidationTitle']";

    // ═══════════════════════════════════════════════════════════════
    // HEADER
    // ═══════════════════════════════════════════════════════════════

    public LabelControl ValidationTitle { get; }
    public LabelControl ValidationSubtitle { get; }

    // ═══════════════════════════════════════════════════════════════
    // VALIDATION SUMMARY
    // ═══════════════════════════════════════════════════════════════

    public LabelControl ErrorCount { get; }
    public LabelControl ErrorList { get; }
    public LabelControl SuccessMessage { get; }

    // ═══════════════════════════════════════════════════════════════
    // REQUIRED FIELD
    // ═══════════════════════════════════════════════════════════════

    public TextInputControl RequiredInput { get; }
    public LabelControl RequiredError { get; }

    // ═══════════════════════════════════════════════════════════════
    // EMAIL FIELD
    // ═══════════════════════════════════════════════════════════════

    public TextInputControl EmailInput { get; }
    public LabelControl EmailError { get; }

    // ═══════════════════════════════════════════════════════════════
    // PHONE FIELD
    // ═══════════════════════════════════════════════════════════════

    public TextInputControl PhoneInput { get; }
    public LabelControl PhoneError { get; }

    // ═══════════════════════════════════════════════════════════════
    // MIN LENGTH FIELD
    // ═══════════════════════════════════════════════════════════════

    public TextInputControl MinLengthInput { get; }
    public LabelControl MinLengthCounter { get; }
    public LabelControl MinLengthError { get; }

    // ═══════════════════════════════════════════════════════════════
    // MAX LENGTH FIELD
    // ═══════════════════════════════════════════════════════════════

    public TextInputControl MaxLengthInput { get; }
    public LabelControl MaxLengthCounter { get; }
    public LabelControl MaxLengthError { get; }

    // ═══════════════════════════════════════════════════════════════
    // RANGE FIELD
    // ═══════════════════════════════════════════════════════════════

    public TextInputControl RangeInput { get; }
    public LabelControl RangeError { get; }

    // ═══════════════════════════════════════════════════════════════
    // REGEX FIELD
    // ═══════════════════════════════════════════════════════════════

    public TextInputControl RegexInput { get; }
    public LabelControl RegexError { get; }

    // ═══════════════════════════════════════════════════════════════
    // PASSWORD FIELDS
    // ═══════════════════════════════════════════════════════════════

    public TextInputControl PasswordInput { get; }
    public LabelControl PasswordError { get; }
    public TextInputControl ConfirmPasswordInput { get; }
    public LabelControl ConfirmPasswordError { get; }

    // ═══════════════════════════════════════════════════════════════
    // ACTION BUTTONS
    // ═══════════════════════════════════════════════════════════════

    public ButtonControl SubmitButton { get; }
    public ButtonControl ValidateButton { get; }
    public ButtonControl ClearButton { get; }

    public ValidationPage(SeleniumTestContext context) : base(context)
    {
        ValidationTitle = new LabelControl(context, this, "[data-automation-id='ValidationTitle']");
        ValidationSubtitle = new LabelControl(context, this, "[data-automation-id='ValidationSubtitle']");

        // Validation summary
        ErrorCount = new LabelControl(context, this, "[data-automation-id='ErrorCount']");
        ErrorList = new LabelControl(context, this, "[data-automation-id='ErrorList']");
        SuccessMessage = new LabelControl(context, this, "[data-automation-id='SuccessMessage']");

        // Required
        RequiredInput = new TextInputControl(context, this, "[data-automation-id='RequiredInput']");
        RequiredError = new LabelControl(context, this, "[data-automation-id='RequiredError']");

        // Email
        EmailInput = new TextInputControl(context, this, "[data-automation-id='EmailInput']");
        EmailError = new LabelControl(context, this, "[data-automation-id='EmailError']");

        // Phone
        PhoneInput = new TextInputControl(context, this, "[data-automation-id='PhoneInput']");
        PhoneError = new LabelControl(context, this, "[data-automation-id='PhoneError']");

        // Min length
        MinLengthInput = new TextInputControl(context, this, "[data-automation-id='MinLengthInput']");
        MinLengthCounter = new LabelControl(context, this, "[data-automation-id='MinLengthCounter']");
        MinLengthError = new LabelControl(context, this, "[data-automation-id='MinLengthError']");

        // Max length
        MaxLengthInput = new TextInputControl(context, this, "[data-automation-id='MaxLengthInput']");
        MaxLengthCounter = new LabelControl(context, this, "[data-automation-id='MaxLengthCounter']");
        MaxLengthError = new LabelControl(context, this, "[data-automation-id='MaxLengthError']");

        // Range
        RangeInput = new TextInputControl(context, this, "[data-automation-id='RangeInput']");
        RangeError = new LabelControl(context, this, "[data-automation-id='RangeError']");

        // Regex
        RegexInput = new TextInputControl(context, this, "[data-automation-id='RegexInput']");
        RegexError = new LabelControl(context, this, "[data-automation-id='RegexError']");

        // Password
        PasswordInput = new TextInputControl(context, this, "[data-automation-id='PasswordInput']");
        PasswordError = new LabelControl(context, this, "[data-automation-id='PasswordError']");
        ConfirmPasswordInput = new TextInputControl(context, this, "[data-automation-id='ConfirmPasswordInput']");
        ConfirmPasswordError = new LabelControl(context, this, "[data-automation-id='ConfirmPasswordError']");

        // Actions
        SubmitButton = new ButtonControl(context, this, "[data-automation-id='SubmitButton']");
        ValidateButton = new ButtonControl(context, this, "[data-automation-id='ValidateButton']");
        ClearButton = new ButtonControl(context, this, "[data-automation-id='ClearButton']");
    }

    public override bool IsDisplayed()
    {
        return ValidationTitle.IsVisible();
    }

    // ═══════════════════════════════════════════════════════════════
    // WORKFLOW METHODS
    // ═══════════════════════════════════════════════════════════════

    public ValidationPage FillRequiredField(string value)
    {
        Log($"FillRequiredField({value})");
        RequiredInput.ClearAndEnter(value);
        return this;
    }

    public ValidationPage FillEmailField(string value)
    {
        Log($"FillEmailField({value})");
        EmailInput.ClearAndEnter(value);
        return this;
    }

    public ValidationPage FillPhoneField(string value)
    {
        Log($"FillPhoneField({value})");
        PhoneInput.ClearAndEnter(value);
        return this;
    }

    public ValidationPage FillMinLengthField(string value)
    {
        Log($"FillMinLengthField({value})");
        MinLengthInput.ClearAndEnter(value);
        return this;
    }

    public ValidationPage FillPasswordField(string password)
    {
        Log("FillPasswordField(***)");
        PasswordInput.ClearAndEnter(password);
        return this;
    }

    public ValidationPage FillConfirmPasswordField(string password)
    {
        Log("FillConfirmPasswordField(***)");
        ConfirmPasswordInput.ClearAndEnter(password);
        return this;
    }

    public ValidationPage Submit()
    {
        Log("Submit()");
        SubmitButton.Click();
        return this;
    }

    public ValidationPage Validate()
    {
        Log("Validate()");
        ValidateButton.Click();
        return this;
    }

    public ValidationPage Clear()
    {
        Log("Clear()");
        ClearButton.Click();
        return this;
    }

    public bool HasValidationErrors()
    {
        return ErrorCount.IsVisible();
    }

    public bool HasSuccessMessage()
    {
        return SuccessMessage.IsVisible();
    }
}
