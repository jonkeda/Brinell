namespace Brinell.WinForms.UITests.Pages;

/// <summary>
/// Page object for the WinForms sample login form.
/// Exposes all control types available in a typical WinForms application.
/// </summary>
public class LoginPage : PageObjectBase<LoginPage>
{
    public TextBox<LoginPage> UsernameField => TextBox("txtUsername");
    public PasswordBox<LoginPage> PasswordField => PasswordBox("txtPassword");
    public CheckBox<LoginPage> RememberCheckBox => CheckBox("chkRemember");
    public ComboBox<LoginPage> RoleCombo => ComboBox("cmbRole");
    public NumericUpDown<LoginPage> PortNumeric => NumericUpDown("nudPort");
    public RichTextBox<LoginPage> NotesRichText => RichTextBox("rtbNotes");
    public DateTimePicker<LoginPage> StartDatePicker => DateTimePicker("dtpStartDate");
    public TrackBar<LoginPage> VolumeTrackBar => TrackBar("trbVolume");
    public ProgressBar<LoginPage> ProgressBarField => ProgressBar("prbProgress");
    public Button<LoginPage> LoginButton => Button("btnLogin");
    public Button<LoginPage> ClearButton => Button("btnClear");
    public Label<LoginPage> StatusLabel => Label("lblStatus");

    public LoginPage(IWinFormsTestContext context) : base(context) { }

    public override bool IsLoaded(int? timeoutMs = null)
    {
        return UsernameField.IsExists() && LoginButton.IsExists();
    }

    #region Form Actions

    public LoginPage EnterUsername(string username)
    {
        UsernameField.Enter(username);
        return this;
    }

    public LoginPage EnterPassword(string password)
    {
        PasswordField.Enter(password);
        return this;
    }

    public LoginPage SetRememberMe(bool value)
    {
        RememberCheckBox.SetChecked(value);
        return this;
    }

    public LoginPage SelectRole(string role)
    {
        RoleCombo.SelectByText(role);
        return this;
    }

    public LoginPage SetPort(double port)
    {
        PortNumeric.SetValue(port);
        return this;
    }

    public LoginPage SetNotes(string notes)
    {
        NotesRichText.SetText(notes);
        return this;
    }

    public LoginPage SetVolume(double level)
    {
        VolumeTrackBar.SetValue(level);
        return this;
    }

    public LoginPage SetStartDate(DateTime date)
    {
        StartDatePicker.SetDate(date);
        return this;
    }

    public LoginPage ClickLogin()
    {
        LoginButton.Click();
        return this;
    }

    public LoginPage ClickClear()
    {
        ClearButton.Click();
        return this;
    }

    #endregion

    #region Accessors

    public string GetUsername() => UsernameField.GetText() ?? string.Empty;
    public double? GetPort() => PortNumeric.GetValue();
    public string GetNotes() => NotesRichText.GetText() ?? string.Empty;
    public double? GetVolume() => VolumeTrackBar.GetValue();
    public double? GetProgress() => ProgressBarField.GetValue();
    public bool IsRememberMeChecked() => RememberCheckBox.IsChecked() == true;
    public string GetSelectedRole() => RoleCombo.GetSelectedText() ?? string.Empty;
    public string GetStatusMessage() => StatusLabel.GetText() ?? string.Empty;

    #endregion

    #region Wait Methods

    /// <summary>Wait for status to contain specific text.</summary>
    public bool WaitForStatusContains(string text, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        return Poll(() => GetStatusMessage().Contains(text, StringComparison.OrdinalIgnoreCase), timeout);
    }

    /// <summary>Wait for form to be cleared.</summary>
    public bool WaitForFormCleared(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        return Poll(() => string.IsNullOrEmpty(GetUsername()) && !IsRememberMeChecked(), timeout);
    }

    /// <summary>Wait for login to complete.</summary>
    public bool WaitForLoginComplete(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        return Poll(() => GetStatusMessage().Contains("Logged in", StringComparison.OrdinalIgnoreCase), timeout);
    }

    #endregion
}
