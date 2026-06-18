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
    public Label<UserFormPage> UserFormTitle => new(this,"UserFormTitle");

    /// <summary>
    /// The personal info section label.
    /// </summary>
    public Label<UserFormPage> PersonalInfoLabel => new(this,"PersonalInfoLabel");

    /// <summary>
    /// The preferences section label.
    /// </summary>
    public Label<UserFormPage> PreferencesLabel => new(this,"PreferencesLabel");

    /// <summary>
    /// The selection section label.
    /// </summary>
    public Label<UserFormPage> SelectionLabel => new(this,"SelectionLabel");

    /// <summary>
    /// The range section label.
    /// </summary>
    public Label<UserFormPage> RangeLabel => new(this,"RangeLabel");

    /// <summary>
    /// The result message label.
    /// </summary>
    public Label<UserFormPage> ResultMessage => new(this,"ResultMessage");

    /// <summary>
    /// The font size display label.
    /// </summary>
    public Label<UserFormPage> FontSizeLabel => new(this,"FontSizeLabel");

    /// <summary>
    /// The volume display label.
    /// </summary>
    public Label<UserFormPage> VolumeLabel => new(this,"VolumeLabel");

    /// <summary>
    /// The quantity display label.
    /// </summary>
    public Label<UserFormPage> QuantityLabel => new(this,"QuantityLabel");

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
    public Editor<UserFormPage> BioEditor => new(this,"BioEditor");

    #endregion

    #region SearchBar Controls

    /// <summary>
    /// The user search bar.
    /// </summary>
    public SearchBar<UserFormPage> UserSearchBar => new(this,"UserSearchBar");

    #endregion

    #region Toggle Controls

    /// <summary>
    /// The newsletter subscription switch.
    /// </summary>
    public Switch<UserFormPage> NewsletterSwitch => new(this,"NewsletterSwitch");

    /// <summary>
    /// The terms of service checkbox.
    /// </summary>
    public CheckBox<UserFormPage> TermsCheckBox => new(this,"TermsCheckBox");

    /// <summary>
    /// The privacy policy checkbox.
    /// </summary>
    public CheckBox<UserFormPage> PrivacyCheckBox => new(this,"PrivacyCheckBox");

    /// <summary>
    /// The indeterminate simulation checkbox.
    /// </summary>
    public CheckBox<UserFormPage> IndeterminateCheckBox => new(this,"IndeterminateCheckBox");

    #endregion

    #region RadioButton Controls

    /// <summary>
    /// The Basic subscription tier radio button.
    /// </summary>
    public RadioButton<UserFormPage> BasicRadio => new(this,"BasicRadio");

    /// <summary>
    /// The Professional subscription tier radio button.
    /// </summary>
    public RadioButton<UserFormPage> ProfessionalRadio => new(this,"ProfessionalRadio");

    /// <summary>
    /// The Enterprise subscription tier radio button.
    /// </summary>
    public RadioButton<UserFormPage> EnterpriseRadio => new(this,"EnterpriseRadio");

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
    public DatePicker<UserFormPage> BirthDatePicker => new(this,"BirthDatePicker");

    /// <summary>
    /// The preferred contact time picker.
    /// </summary>
    public TimePicker<UserFormPage> PreferredTimePicker => new(this,"PreferredTimePicker");

    #endregion

    #region Slider Controls

    /// <summary>
    /// The font size slider (8-72).
    /// </summary>
    public Slider<UserFormPage> FontSizeSlider => new(this,"FontSizeSlider");

    /// <summary>
    /// The volume slider (0-100).
    /// </summary>
    public Slider<UserFormPage> VolumeSlider => new(this,"VolumeSlider");

    #endregion

    #region Stepper Controls

    /// <summary>
    /// The quantity stepper (1-99).
    /// </summary>
    public Stepper<UserFormPage> QuantityStepper => new(this,"QuantityStepper");

    #endregion

    #region Button Controls

    /// <summary>
    /// The submit button.
    /// </summary>
    public Button<UserFormPage> SubmitButton => new(this,"SubmitButton");

    /// <summary>
    /// The save draft button.
    /// </summary>
    public Button<UserFormPage> SaveDraftButton => new(this,"SaveDraftButton");

    /// <summary>
    /// The clear button.
    /// </summary>
    public Button<UserFormPage> ClearButton => new(this,"ClearButton");

    #endregion
}
