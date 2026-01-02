using Brinell.Core.Abstractions;
using Brinell.WinForms.Controls;
using Brinell.WinForms.Controls.Base;
using Brinell.WinForms.Infrastructure;

namespace Brinell.Samples.WinForms.UITests.Pages;

/// <summary>
/// Login page object for the sample application.
/// </summary>
public class LoginPage : PageBase
{
    private readonly TextBoxControl _usernameField;
    private readonly TextBoxControl _passwordField;
    private readonly CheckBoxControl _rememberCheckBox;
    private readonly ComboBoxControl _roleCombo;
    private readonly NumericUpDownControl _portNumeric;
    private readonly RichTextBoxControl _notesRichText;
    private readonly DateTimePickerControl _startDatePicker;
    private readonly TrackBarControl _volumeTrackBar;
    private readonly ProgressBarControl _progressBar;
    private readonly ButtonControl _loginButton;
    private readonly ButtonControl _clearButton;
    private readonly LabelControl _statusLabel;

    public LoginPage(FlaUITestContext context)
        : base(context, "LoginPage")
    {
        _usernameField = new TextBoxControl(context, this, "txtUsername");
        _passwordField = new TextBoxControl(context, this, "txtPassword");
        _rememberCheckBox = new CheckBoxControl(context, this, "chkRemember");
        _roleCombo = new ComboBoxControl(context, this, "cmbRole");
        _portNumeric = new NumericUpDownControl(context, this, "nudPort");
        _notesRichText = new RichTextBoxControl(context, this, "rtbNotes");
        _startDatePicker = new DateTimePickerControl(context, this, "dtpStartDate");
        _volumeTrackBar = new TrackBarControl(context, this, "trbVolume");
        _progressBar = new ProgressBarControl(context, this, "prbProgress");
        _loginButton = new ButtonControl(context, this, "btnLogin");
        _clearButton = new ButtonControl(context, this, "btnClear");
        _statusLabel = new LabelControl(context, this, "lblStatus");
    }

    /// <summary>
    /// Get the page name for logging.
    /// </summary>
    public override string Name => "LoginPage";

    /// <summary>
    /// Check if the login page is displayed.
    /// </summary>
    public override bool IsDisplayed()
    {
        return _usernameField.IsExists() && _loginButton.IsExists();
    }

    /// <summary>
    /// Enter username.
    /// </summary>
    public void EnterUsername(string username)
    {
        _usernameField.Enter(username);
    }

    /// <summary>
    /// Enter password.
    /// </summary>
    public void EnterPassword(string password)
    {
        _passwordField.Enter(password);
    }

    /// <summary>
    /// Set remember me checkbox.
    /// </summary>
    public void SetRememberMe(bool value)
    {
        if (value)
        {
            _rememberCheckBox.Check();
        }
        else
        {
            _rememberCheckBox.Uncheck();
        }
    }

    /// <summary>
    /// Select role from dropdown.
    /// </summary>
    public void SelectRole(string role)
    {
        _roleCombo.SelectByText(role);
    }

    /// <summary>
    /// Set the port number.
    /// </summary>
    public void SetPort(decimal port)
    {
        _portNumeric.SetValue(port);
    }

    /// <summary>
    /// Get the current port number.
    /// </summary>
    public decimal GetPort()
    {
        return _portNumeric.GetValue();
    }

    /// <summary>
    /// Set notes/comments.
    /// </summary>
    public void SetNotes(string notes)
    {
        _notesRichText.SetContent(notes);
    }

    /// <summary>
    /// Get notes/comments.
    /// </summary>
    public string GetNotes()
    {
        return _notesRichText.GetContent();
    }

    /// <summary>
    /// Set the start date.
    /// </summary>
    public void SetStartDate(DateTime date)
    {
        _startDatePicker.SetDate(date);
    }

    /// <summary>
    /// Get the start date.
    /// </summary>
    public DateTime GetStartDate()
    {
        return _startDatePicker.GetDate();
    }

    /// <summary>
    /// Set the volume level.
    /// </summary>
    public void SetVolume(int level)
    {
        _volumeTrackBar.SetValue(level);
    }

    /// <summary>
    /// Get the current volume level.
    /// </summary>
    public int GetVolume()
    {
        return _volumeTrackBar.GetValue();
    }

    /// <summary>
    /// Get the current progress.
    /// </summary>
    public int GetProgress()
    {
        return _progressBar.GetValue();
    }

    /// <summary>
    /// Wait for progress to complete.
    /// </summary>
    public void WaitForProgressComplete(int timeoutMs = 30000)
    {
        _progressBar.WaitForComplete(timeoutMs);
    }

    /// <summary>
    /// Click the login button.
    /// </summary>
    public void ClickLogin()
    {
        _loginButton.Click();
    }

    /// <summary>
    /// Click the clear button.
    /// </summary>
    public void ClickClear()
    {
        _clearButton.Click();
    }

    /// <summary>
    /// Get the status message.
    /// </summary>
    public string GetStatusMessage()
    {
        return _statusLabel.GetText();
    }

    /// <summary>
    /// Get the username field text.
    /// </summary>
    public string GetUsername()
    {
        return _usernameField.GetText();
    }

    /// <summary>
    /// Check if remember me is checked.
    /// </summary>
    public bool IsRememberMeChecked()
    {
        return _rememberCheckBox.IsChecked();
    }

    /// <summary>
    /// Get selected role.
    /// </summary>
    public string GetSelectedRole()
    {
        return _roleCombo.GetSelectedItem();
    }

    #region Wait Methods

    /// <summary>
    /// Wait for status message to contain specific text.
    /// </summary>
    public void WaitForStatusContains(string text, int? timeoutMs = null)
    {
        _context.WaitFor(
            () => GetStatusMessage().Contains(text, StringComparison.OrdinalIgnoreCase),
            timeoutMs,
            $"status contains '{text}'");
    }

    /// <summary>
    /// Wait for form to be cleared (username empty and remember unchecked).
    /// </summary>
    public void WaitForFormCleared(int? timeoutMs = null)
    {
        _context.WaitFor(
            () => string.IsNullOrEmpty(GetUsername()) && !IsRememberMeChecked(),
            timeoutMs,
            "form cleared");
    }

    /// <summary>
    /// Wait for form to be in ready state.
    /// Overrides base implementation with status-specific check.
    /// </summary>
    public override bool WaitForReady(int? timeoutMs = null)
    {
        return _context.WaitFor(
            () => GetStatusMessage().Contains("Ready", StringComparison.OrdinalIgnoreCase),
            timeoutMs,
            "form ready");
    }

    /// <summary>
    /// Wait for login to complete (status contains "Logged in").
    /// </summary>
    public void WaitForLoginComplete(int? timeoutMs = null)
    {
        _context.WaitFor(
            () => GetStatusMessage().Contains("Logged in", StringComparison.OrdinalIgnoreCase),
            timeoutMs,
            "login complete");
    }

    #endregion
}
