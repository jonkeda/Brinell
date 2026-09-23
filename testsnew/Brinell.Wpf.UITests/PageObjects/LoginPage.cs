namespace Brinell.Wpf.UITests.PageObjects;

/// <summary>
/// Page object for the Login page with form validation and busy state handling.
/// Demonstrates page-level wait patterns and form interaction helpers.
/// </summary>
public class LoginPage : PageObjectBase<LoginPage>
{
    public Label<LoginPage> LoginHeader => Label("LoginHeader");
    public TextBox<LoginPage> UsernameTextBox => TextBox("UsernameTextBox");
    public Label<LoginPage> UsernameErrorText => Label("UsernameErrorText");
    public PasswordBox<LoginPage> PasswordField => PasswordBox("PasswordBox");
    public Label<LoginPage> PasswordErrorText => Label("PasswordErrorText");
    public Label<LoginPage> LoginErrorText => Label("LoginErrorText");
    public Button<LoginPage> LoginButton => Button("LoginButton");
    public Button<LoginPage> CancelButton => Button("CancelButton");
    public Label<LoginPage> BusyIndicator => Label("BusyIndicator");
    public Label<LoginPage> SuccessText => Label("SuccessText");

    public LoginPage(IWpfTestContext context) : base(context) { }

    /// <summary>Checks whether the login header is visible.</summary>
    public override bool IsLoaded(int? timeoutMs = null)
    {
        return LoginHeader.IsVisible() == true;
    }

    #region Busy State

    /// <summary>Whether the busy indicator is currently visible.</summary>
    public bool IsBusy() => BusyIndicator.IsVisible() == true;

    /// <summary>Wait until the busy indicator hides.</summary>
    public bool WaitNotBusy(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        return Poll(() => !IsBusy(), timeout);
    }

    /// <summary>Wait until the page is loaded and not busy.</summary>
    public bool WaitPageReady(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        return Poll(() => IsLoaded() && !IsBusy(), timeout);
    }

    #endregion

    #region Form Actions

    /// <summary>Enter username and password.</summary>
    public LoginPage EnterCredentials(string username, string password)
    {
        UsernameTextBox.SetText(username);
        PasswordField.SetText(password);
        return this;
    }

    /// <summary>Click the Sign In button.</summary>
    public LoginPage ClickLogin()
    {
        LoginButton.Click();
        return this;
    }

    /// <summary>Click the Cancel button.</summary>
    public LoginPage ClickCancel()
    {
        CancelButton.Click();
        return this;
    }

    /// <summary>Submit valid credentials and wait for navigation to home.</summary>
    public HomePage SubmitValidLogin(string username, string password)
    {
        EnterCredentials(username, password);
        ClickLogin();
        WaitNotBusy();

        var homePage = new HomePage(Context);
        homePage.WaitLoaded(true);
        return homePage;
    }

    /// <summary>Submit invalid credentials and wait for busy to finish.</summary>
    public LoginPage SubmitInvalidLogin(string username, string password)
    {
        EnterCredentials(username, password);
        ClickLogin();
        WaitNotBusy();
        return this;
    }

    #endregion
}
