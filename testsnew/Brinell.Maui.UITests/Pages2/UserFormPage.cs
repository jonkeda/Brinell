using Brinell.Maui.Controls.DateTimes;

namespace Brinell.Maui.UITests.Pages2;

/// <summary>
/// Page object for the UserFormPage of the Brinell sample MAUI app.
/// Exposes controls from UserFormPage.xaml with their AutomationIds.
/// </summary>
public class UserFormPage : PageObjectBase<UserFormPage>
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
    public Label<UserFormPage> UserFormTitle => Label("UserFormTitle");

    /// <summary>
    /// The personal info section label.
    /// </summary>
    public Label<UserFormPage> PersonalInfoLabel => Label("PersonalInfoLabel");

    /// <summary>
    /// The preferences section label.
    /// </summary>
    public Label<UserFormPage> PreferencesLabel => Label("PreferencesLabel");

    /// <summary>
    /// The selection section label.
    /// </summary>
    public Label<UserFormPage> SelectionLabel => Label("SelectionLabel");

    /// <summary>
    /// The range section label.
    /// </summary>
    public Label<UserFormPage> RangeLabel => Label("RangeLabel");

    /// <summary>
    /// The result message label.
    /// </summary>
    public Label<UserFormPage> ResultMessage => Label("ResultMessage");

    /// <summary>
    /// The font size display label.
    /// </summary>
    public Label<UserFormPage> FontSizeLabel => Label("FontSizeLabel");

    /// <summary>
    /// The volume display label.
    /// </summary>
    public Label<UserFormPage> VolumeLabel => Label("VolumeLabel");

    /// <summary>
    /// The quantity display label.
    /// </summary>
    public Label<UserFormPage> QuantityLabel => Label("QuantityLabel");

    #endregion

    #region Entry Controls

    /// <summary>
    /// The first name entry field.
    /// </summary>
    public Entry<UserFormPage> FirstNameEntry => new (this, "FirstNameEntry");

    /// <summary>
    /// The last name entry field.
    /// </summary>
    public Entry<UserFormPage> LastNameEntry => new(this, "LastNameEntry");

    /// <summary>
    /// The email entry field.
    /// </summary>
    public Entry<UserFormPage> EmailEntry => new(this, "EmailEntry");

    /// <summary>
    /// The phone entry field.
    /// </summary>
    public Entry<UserFormPage> PhoneEntry => new(this, "PhoneEntry");

    #endregion

    #region Editor Controls

    /// <summary>
    /// The bio editor (multi-line text).
    /// </summary>
    public Editor<UserFormPage> BioEditor => Editor("BioEditor");

    #endregion

    #region SearchBar Controls

    /// <summary>
    /// The user search bar.
    /// </summary>
    public SearchBar<UserFormPage> UserSearchBar => SearchBar("UserSearchBar");

    #endregion

    #region Toggle Controls

    /// <summary>
    /// The newsletter subscription switch.
    /// </summary>
    public Switch<UserFormPage> NewsletterSwitch => Switch("NewsletterSwitch");

    /// <summary>
    /// The terms of service checkbox.
    /// </summary>
    public CheckBox<UserFormPage> TermsCheckBox => CheckBox("TermsCheckBox");

    /// <summary>
    /// The privacy policy checkbox.
    /// </summary>
    public CheckBox<UserFormPage> PrivacyCheckBox => CheckBox("PrivacyCheckBox");

    /// <summary>
    /// The indeterminate simulation checkbox.
    /// </summary>
    public CheckBox<UserFormPage> IndeterminateCheckBox => CheckBox("IndeterminateCheckBox");

    #endregion

    #region RadioButton Controls

    /// <summary>
    /// The Basic subscription tier radio button.
    /// </summary>
    public RadioButton<UserFormPage> BasicRadio => RadioButton("BasicRadio");

    /// <summary>
    /// The Professional subscription tier radio button.
    /// </summary>
    public RadioButton<UserFormPage> ProfessionalRadio => RadioButton("ProfessionalRadio");

    /// <summary>
    /// The Enterprise subscription tier radio button.
    /// </summary>
    public RadioButton<UserFormPage> EnterpriseRadio => RadioButton("EnterpriseRadio");

    #endregion

    #region Picker Controls

    /// <summary>
    /// The country picker.
    /// </summary>
    public Picker<UserFormPage> CountryPicker => new(this, "CountryPicker");

    /// <summary>
    /// The department picker.
    /// </summary>
    public Picker<UserFormPage> DepartmentPicker => new(this, "DepartmentPicker");

    #endregion

    #region DatePicker/TimePicker Controls

    /// <summary>
    /// The birth date picker.
    /// </summary>
    public DatePicker<UserFormPage> BirthDatePicker => DatePicker("BirthDatePicker");

    /// <summary>
    /// The preferred contact time picker.
    /// </summary>
    public TimePicker<UserFormPage> PreferredTimePicker => TimePicker("PreferredTimePicker");

    #endregion

    #region Slider Controls

    /// <summary>
    /// The font size slider (8-72).
    /// </summary>
    public Slider<UserFormPage> FontSizeSlider => Slider("FontSizeSlider");

    /// <summary>
    /// The volume slider (0-100).
    /// </summary>
    public Slider<UserFormPage> VolumeSlider => Slider("VolumeSlider");

    #endregion

    #region Stepper Controls

    /// <summary>
    /// The quantity stepper (1-99).
    /// </summary>
    public Stepper<UserFormPage> QuantityStepper => Stepper("QuantityStepper");

    #endregion

    #region Button Controls

    /// <summary>
    /// The submit button.
    /// </summary>
    public Button<UserFormPage> SubmitButton => Button("SubmitButton");

    /// <summary>
    /// The save draft button.
    /// </summary>
    public Button<UserFormPage> SaveDraftButton => Button("SaveDraftButton");

    /// <summary>
    /// The clear button.
    /// </summary>
    public Button<UserFormPage> ClearButton => Button("ClearButton");

    #endregion
}
