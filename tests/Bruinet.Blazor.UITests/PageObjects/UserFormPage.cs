// <scraped page="/userform" from="2026-09-30-sample-12/001-user-form-bruinet-blazor-sample.nodes.jsonl" hash="7683324a" />
using Bruinet.Html.Controls.Display;

namespace Bruinet.Blazor.UITests.PageObjects;

/// <summary>The user registration form (/userform): made with the Scraper from a recording of the sample.</summary>
public sealed class UserFormPage(IHtmlTestContext context) : BlazorPageObjectBase<UserFormPage>(context)
{
    public LabelControl<UserFormPage> Title => new(this, Locator.ByCss("[data-automation-id='UserFormTitle']"));

    public TextInputControl<UserFormPage> FirstName => new(this, Locator.ByCss("[data-automation-id='FirstNameInput']"));

    public TextInputControl<UserFormPage> LastName => new(this, Locator.ByCss("[data-automation-id='LastNameInput']"));

    public TextInputControl<UserFormPage> Email => new(this, Locator.ByCss("[data-automation-id='EmailInput']"));

    public TextInputControl<UserFormPage> Phone => new(this, Locator.ByCss("[data-automation-id='PhoneInput']"));

    public TextAreaControl<UserFormPage> Bio => new(this, Locator.ByCss("[data-automation-id='BioTextarea']"));

    public CheckBoxControl<UserFormPage> Newsletter => new(this, Locator.ByCss("[data-automation-id='NewsletterSwitch']"));

    public CheckBoxControl<UserFormPage> Terms => new(this, Locator.ByCss("[data-automation-id='TermsCheckbox']"));

    public CheckBoxControl<UserFormPage> Privacy => new(this, Locator.ByCss("[data-automation-id='PrivacyCheckbox']"));

    public RadioButtonControl<UserFormPage> Basic => new(this, Locator.ByCss("[data-automation-id='BasicRadio']"));

    public RadioButtonControl<UserFormPage> Professional => new(this, Locator.ByCss("[data-automation-id='ProfessionalRadio']"));

    public RadioButtonControl<UserFormPage> Enterprise => new(this, Locator.ByCss("[data-automation-id='EnterpriseRadio']"));

    public SelectControl<UserFormPage> Country => new(this, Locator.ByCss("[data-automation-id='CountrySelect']"));

    public SelectControl<UserFormPage> Department => new(this, Locator.ByCss("[data-automation-id='DepartmentSelect']"));

    public DateInputControl<UserFormPage> BirthDate => new(this, Locator.ByCss("[data-automation-id='BirthDateInput']"));

    public RangeInputControl<UserFormPage> Volume => new(this, Locator.ByCss("[data-automation-id='VolumeSlider']"));

    public ButtonControl<UserFormPage> Submit => new(this, Locator.ByCss("[data-automation-id='SubmitButton']"));

    public ButtonControl<UserFormPage> Clear => new(this, Locator.ByCss("[data-automation-id='ClearButton']"));
}
