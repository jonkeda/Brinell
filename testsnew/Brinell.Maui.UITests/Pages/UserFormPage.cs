using Brinell.Maui.Controls;
using Brinell.Maui.Controls.DateTime;
using Brinell.Maui.Controls.Range;
using Brinell.Maui.Controls.Selection;
using Brinell.Maui.Controls.Text;
using Brinell.Maui.Controls.Toggle;
using Brinell.Maui.Pages;

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the UserFormPage of the Brinell sample MAUI app.
/// Exposes controls from UserFormPage.xaml with their AutomationIds.
/// </summary>
public class UserFormPage : MauiPageObjectBase<UserFormPage>
{
    public UserFormPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "UserFormPage";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        // Page is loaded when the title label exists
        return UserFormTitle.IsExists();
    }

    #region Labels

    /// <summary>
    /// The main title label "User Registration".
    /// </summary>
    public MauiControlBase<UserFormPage> UserFormTitle => Control("UserFormTitle");

    /// <summary>
    /// The personal info section label.
    /// </summary>
    public MauiControlBase<UserFormPage> PersonalInfoLabel => Control("PersonalInfoLabel");

    /// <summary>
    /// The preferences section label.
    /// </summary>
    public MauiControlBase<UserFormPage> PreferencesLabel => Control("PreferencesLabel");

    /// <summary>
    /// The selection section label.
    /// </summary>
    public MauiControlBase<UserFormPage> SelectionLabel => Control("SelectionLabel");

    /// <summary>
    /// The range section label.
    /// </summary>
    public MauiControlBase<UserFormPage> RangeLabel => Control("RangeLabel");

    /// <summary>
    /// The result message label.
    /// </summary>
    public MauiControlBase<UserFormPage> ResultMessage => Control("ResultMessage");

    /// <summary>
    /// The font size display label.
    /// </summary>
    public MauiControlBase<UserFormPage> FontSizeLabel => Control("FontSizeLabel");

    /// <summary>
    /// The volume display label.
    /// </summary>
    public MauiControlBase<UserFormPage> VolumeLabel => Control("VolumeLabel");

    /// <summary>
    /// The quantity display label.
    /// </summary>
    public MauiControlBase<UserFormPage> QuantityLabel => Control("QuantityLabel");

    #endregion

    #region Entry Controls

    /// <summary>
    /// The first name entry field.
    /// </summary>
    public MauiEntryControl<UserFormPage> FirstNameEntry => Entry("FirstNameEntry");

    /// <summary>
    /// The last name entry field.
    /// </summary>
    public MauiEntryControl<UserFormPage> LastNameEntry => Entry("LastNameEntry");

    /// <summary>
    /// The email entry field.
    /// </summary>
    public MauiEntryControl<UserFormPage> EmailEntry => Entry("EmailEntry");

    /// <summary>
    /// The phone entry field.
    /// </summary>
    public MauiEntryControl<UserFormPage> PhoneEntry => Entry("PhoneEntry");

    #endregion

    #region Editor Controls

    /// <summary>
    /// The bio editor (multi-line text).
    /// </summary>
    public MauiEditorControl<UserFormPage> BioEditor => Editor("BioEditor");

    #endregion

    #region SearchBar Controls

    /// <summary>
    /// The user search bar.
    /// </summary>
    public MauiSearchBarControl<UserFormPage> UserSearchBar => SearchBar("UserSearchBar");

    #endregion

    #region Toggle Controls

    /// <summary>
    /// The newsletter subscription switch.
    /// </summary>
    public MauiSwitchControl<UserFormPage> NewsletterSwitch => Switch("NewsletterSwitch");

    /// <summary>
    /// The terms of service checkbox.
    /// </summary>
    public MauiCheckBoxControl<UserFormPage> TermsCheckBox => CheckBox("TermsCheckBox");

    /// <summary>
    /// The privacy policy checkbox.
    /// </summary>
    public MauiCheckBoxControl<UserFormPage> PrivacyCheckBox => CheckBox("PrivacyCheckBox");

    /// <summary>
    /// The indeterminate simulation checkbox.
    /// </summary>
    public MauiCheckBoxControl<UserFormPage> IndeterminateCheckBox => CheckBox("IndeterminateCheckBox");

    #endregion

    #region RadioButton Controls

    /// <summary>
    /// The Basic subscription tier radio button.
    /// </summary>
    public MauiRadioButtonControl<UserFormPage> BasicRadio => RadioButton("BasicRadio");

    /// <summary>
    /// The Professional subscription tier radio button.
    /// </summary>
    public MauiRadioButtonControl<UserFormPage> ProfessionalRadio => RadioButton("ProfessionalRadio");

    /// <summary>
    /// The Enterprise subscription tier radio button.
    /// </summary>
    public MauiRadioButtonControl<UserFormPage> EnterpriseRadio => RadioButton("EnterpriseRadio");

    #endregion

    #region Picker Controls

    /// <summary>
    /// The country picker.
    /// </summary>
    public MauiPickerControl<UserFormPage> CountryPicker => Picker("CountryPicker");

    /// <summary>
    /// The department picker.
    /// </summary>
    public MauiPickerControl<UserFormPage> DepartmentPicker => Picker("DepartmentPicker");

    #endregion

    #region DatePicker/TimePicker Controls

    /// <summary>
    /// The birth date picker.
    /// </summary>
    public MauiDatePickerControl<UserFormPage> BirthDatePicker => DatePicker("BirthDatePicker");

    /// <summary>
    /// The preferred contact time picker.
    /// </summary>
    public MauiTimePickerControl<UserFormPage> PreferredTimePicker => TimePicker("PreferredTimePicker");

    #endregion

    #region Slider Controls

    /// <summary>
    /// The font size slider (8-72).
    /// </summary>
    public MauiSliderControl<UserFormPage> FontSizeSlider => Slider("FontSizeSlider");

    /// <summary>
    /// The volume slider (0-100).
    /// </summary>
    public MauiSliderControl<UserFormPage> VolumeSlider => Slider("VolumeSlider");

    #endregion

    #region Stepper Controls

    /// <summary>
    /// The quantity stepper (1-99).
    /// </summary>
    public MauiStepperControl<UserFormPage> QuantityStepper => Stepper("QuantityStepper");

    #endregion

    #region Button Controls

    /// <summary>
    /// The submit button.
    /// </summary>
    public MauiButtonControl<UserFormPage> SubmitButton => Button("SubmitButton");

    /// <summary>
    /// The save draft button.
    /// </summary>
    public MauiButtonControl<UserFormPage> SaveDraftButton => Button("SaveDraftButton");

    /// <summary>
    /// The clear button.
    /// </summary>
    public MauiButtonControl<UserFormPage> ClearButton => Button("ClearButton");

    #endregion
}
