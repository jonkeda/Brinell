using Brinell.Html.Controls;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Samples.Blazor.UITests.PageObjects;

/// <summary>
/// Page object for the UserForm page.
/// </summary>
public class UserFormPage : PageBase
{
    public override string AutomationId => "[data-automation-id='UserFormTitle']";

    // ═══════════════════════════════════════════════════════════════
    // HEADER
    // ═══════════════════════════════════════════════════════════════

    public LabelControl UserFormTitle { get; }
    public LabelControl UserFormSubtitle { get; }
    public LabelControl ResultMessage { get; }

    // ═══════════════════════════════════════════════════════════════
    // TEXT INPUT SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl PersonalInfoLabel { get; }
    public TextInputControl FirstNameInput { get; }
    public TextInputControl LastNameInput { get; }
    public TextInputControl EmailInput { get; }
    public TextInputControl PhoneInput { get; }
    public TextAreaControl BioTextarea { get; }
    public TextInputControl SearchInput { get; }
    public ButtonControl SearchButton { get; }

    // ═══════════════════════════════════════════════════════════════
    // TOGGLE SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl PreferencesLabel { get; }
    public CheckBoxControl NewsletterSwitch { get; }
    public CheckBoxControl TermsCheckbox { get; }
    public CheckBoxControl PrivacyCheckbox { get; }

    // ═══════════════════════════════════════════════════════════════
    // SELECTION SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl SelectionLabel { get; }
    public SelectControl CountrySelect { get; }
    public SelectControl DepartmentSelect { get; }
    public TextInputControl BirthDateInput { get; }

    // ═══════════════════════════════════════════════════════════════
    // RANGE SECTION
    // ═══════════════════════════════════════════════════════════════

    public LabelControl RangeLabel { get; }
    public RangeInputControl FontSizeSlider { get; }
    public RangeInputControl VolumeSlider { get; }
    public TextInputControl QuantityInput { get; }
    public ButtonControl IncrementButton { get; }
    public ButtonControl DecrementButton { get; }

    // ═══════════════════════════════════════════════════════════════
    // ACTION BUTTONS
    // ═══════════════════════════════════════════════════════════════

    public ButtonControl SubmitButton { get; }
    public ButtonControl SaveDraftButton { get; }
    public ButtonControl ClearButton { get; }

    public UserFormPage(SeleniumTestContext context) : base(context)
    {
        UserFormTitle = new LabelControl(context, this, "[data-automation-id='UserFormTitle']");
        UserFormSubtitle = new LabelControl(context, this, "[data-automation-id='UserFormSubtitle']");
        ResultMessage = new LabelControl(context, this, "[data-automation-id='ResultMessage']");

        // Text input section
        PersonalInfoLabel = new LabelControl(context, this, "[data-automation-id='PersonalInfoLabel']");
        FirstNameInput = new TextInputControl(context, this, "[data-automation-id='FirstNameInput']");
        LastNameInput = new TextInputControl(context, this, "[data-automation-id='LastNameInput']");
        EmailInput = new TextInputControl(context, this, "[data-automation-id='EmailInput']");
        PhoneInput = new TextInputControl(context, this, "[data-automation-id='PhoneInput']");
        BioTextarea = new TextAreaControl(context, this, "[data-automation-id='BioTextarea']");
        SearchInput = new TextInputControl(context, this, "[data-automation-id='SearchInput']");
        SearchButton = new ButtonControl(context, this, "[data-automation-id='SearchButton']");

        // Toggle section
        PreferencesLabel = new LabelControl(context, this, "[data-automation-id='PreferencesLabel']");
        NewsletterSwitch = new CheckBoxControl(context, this, "[data-automation-id='NewsletterSwitch']");
        TermsCheckbox = new CheckBoxControl(context, this, "[data-automation-id='TermsCheckbox']");
        PrivacyCheckbox = new CheckBoxControl(context, this, "[data-automation-id='PrivacyCheckbox']");

        // Selection section
        SelectionLabel = new LabelControl(context, this, "[data-automation-id='SelectionLabel']");
        CountrySelect = new SelectControl(context, this, "[data-automation-id='CountrySelect']");
        DepartmentSelect = new SelectControl(context, this, "[data-automation-id='DepartmentSelect']");
        BirthDateInput = new TextInputControl(context, this, "[data-automation-id='BirthDateInput']");

        // Range section
        RangeLabel = new LabelControl(context, this, "[data-automation-id='RangeLabel']");
        FontSizeSlider = new RangeInputControl(context, this, "[data-automation-id='FontSizeSlider']");
        VolumeSlider = new RangeInputControl(context, this, "[data-automation-id='VolumeSlider']");
        QuantityInput = new TextInputControl(context, this, "[data-automation-id='QuantityInput']");
        IncrementButton = new ButtonControl(context, this, "[data-automation-id='IncrementButton']");
        DecrementButton = new ButtonControl(context, this, "[data-automation-id='DecrementButton']");

        // Action buttons
        SubmitButton = new ButtonControl(context, this, "[data-automation-id='SubmitButton']");
        SaveDraftButton = new ButtonControl(context, this, "[data-automation-id='SaveDraftButton']");
        ClearButton = new ButtonControl(context, this, "[data-automation-id='ClearButton']");
    }

    public override bool IsDisplayed()
    {
        return UserFormTitle.IsVisible();
    }

    // ═══════════════════════════════════════════════════════════════
    // WORKFLOW METHODS
    // ═══════════════════════════════════════════════════════════════

    public UserFormPage FillPersonalInfo(string firstName, string lastName, string email)
    {
        Log($"FillPersonalInfo({firstName}, {lastName}, {email})");
        FirstNameInput.ClearAndEnter(firstName);
        LastNameInput.ClearAndEnter(lastName);
        EmailInput.ClearAndEnter(email);
        return this;
    }

    public UserFormPage AcceptTerms()
    {
        Log("AcceptTerms()");
        TermsCheckbox.Check();
        PrivacyCheckbox.Check();
        return this;
    }

    public UserFormPage SelectCountry(string country)
    {
        Log($"SelectCountry({country})");
        CountrySelect.SelectByText(country);
        return this;
    }

    public UserFormPage SelectDepartment(string department)
    {
        Log($"SelectDepartment({department})");
        DepartmentSelect.SelectByText(department);
        return this;
    }

    public UserFormPage Submit()
    {
        Log("Submit()");
        SubmitButton.Click();
        return this;
    }

    public UserFormPage SaveDraft()
    {
        Log("SaveDraft()");
        SaveDraftButton.Click();
        return this;
    }

    public UserFormPage Clear()
    {
        Log("Clear()");
        ClearButton.Click();
        return this;
    }

    public UserFormPage IncrementQuantity()
    {
        Log("IncrementQuantity()");
        IncrementButton.Click();
        return this;
    }

    public UserFormPage DecrementQuantity()
    {
        Log("DecrementQuantity()");
        DecrementButton.Click();
        return this;
    }
}
