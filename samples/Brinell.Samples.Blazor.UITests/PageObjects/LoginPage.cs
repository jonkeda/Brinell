using Brinell.Html.Controls;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Samples.Blazor.UITests.PageObjects;

/// <summary>
/// Page object for the Blazor Login page with form validation.
/// </summary>
public class LoginPage : LoadingPageBase
{
    /// <summary>
    /// The login page title.
    /// </summary>
    public LabelControl LoginTitle { get; }

    /// <summary>
    /// Email input field.
    /// </summary>
    public TextInputControl EmailInput { get; }

    /// <summary>
    /// Password input field.
    /// </summary>
    public TextInputControl PasswordInput { get; }

    /// <summary>
    /// Login button.
    /// </summary>
    public ButtonControl LoginButton { get; }

    /// <summary>
    /// Error message container.
    /// </summary>
    public LabelControl ErrorMessage { get; }

    /// <summary>
    /// Success message container.
    /// </summary>
    public LabelControl SuccessMessage { get; }

    /// <summary>
    /// Loading spinner shown during login.
    /// </summary>
    public LabelControl LoadingSpinner { get; }

    /// <summary>
    /// Test credentials info text.
    /// </summary>
    public LabelControl TestCredentialsInfo { get; }

    public LoginPage(SeleniumTestContext context)
        : base(context)
    {
        LoginTitle = new LabelControl(context, this, "#login-title");
        EmailInput = new TextInputControl(context, this, "#email-input");
        PasswordInput = new TextInputControl(context, this, "#password-input");
        LoginButton = new ButtonControl(context, this, "#login-btn");
        ErrorMessage = new LabelControl(context, this, "#error-message");
        SuccessMessage = new LabelControl(context, this, "#success-message");
        LoadingSpinner = new LabelControl(context, this, "#loading-spinner");
        TestCredentialsInfo = new LabelControl(context, this, "#test-credentials-info");
    }

    /// <summary>
    /// CSS selector that identifies this page.
    /// </summary>
    public override string AutomationId => "#login-title";

    /// <summary>
    /// CSS selector for the loading indicator.
    /// </summary>
    protected override string? LoadingIndicatorSelector => "#loading-spinner";

    /// <summary>
    /// Check if the login page is displayed.
    /// </summary>
    public override bool IsDisplayed()
    {
        return LoginTitle.IsVisible() && LoginTitle.GetText() == "Login";
    }

    /// <summary>
    /// Enter email address.
    /// </summary>
    public LoginPage EnterEmail(string email)
    {
        Log($"EnterEmail({email})");
        EmailInput.SetText(email);
        return this;
    }

    /// <summary>
    /// Enter password.
    /// </summary>
    public LoginPage EnterPassword(string password)
    {
        Log("EnterPassword(***)");
        PasswordInput.SetText(password);
        return this;
    }

    /// <summary>
    /// Enter credentials (email and password).
    /// </summary>
    public LoginPage EnterCredentials(string email, string password)
    {
        Log($"EnterCredentials({email}, ***)");
        EnterEmail(email);
        EnterPassword(password);
        return this;
    }

    /// <summary>
    /// Click the login button.
    /// </summary>
    public LoginPage ClickLogin()
    {
        Log("ClickLogin()");
        LoginButton.Click();
        return this;
    }

    /// <summary>
    /// Submit login with valid credentials and wait for navigation to dashboard.
    /// </summary>
    public DashboardPage SubmitValidLogin(string email, string password)
    {
        Log($"SubmitValidLogin({email}, ***)");
        EnterCredentials(email, password);
        ClickLogin();

        // Wait for loading to complete
        WaitForLoaded();

        // Wait for success message then navigation
        _context.WaitFor(() => SuccessMessage.IsVisible(), 5000, "success message");

        var dashboardPage = new DashboardPage(_context);
        dashboardPage.WaitForDisplayed();
        return dashboardPage;
    }

    /// <summary>
    /// Submit login expecting it to fail with an error.
    /// </summary>
    public LoginPage SubmitInvalidLogin(string email, string password)
    {
        Log($"SubmitInvalidLogin({email}, ***)");
        EnterCredentials(email, password);
        ClickLogin();

        // Wait for loading to complete
        WaitForLoaded();

        return this;
    }

    /// <summary>
    /// Check if error message is displayed.
    /// </summary>
    public bool HasErrorMessage()
    {
        return ErrorMessage.IsVisible();
    }

    /// <summary>
    /// Get the error message text.
    /// </summary>
    public string GetErrorMessage()
    {
        return ErrorMessage.GetText();
    }

    /// <summary>
    /// Check if success message is displayed.
    /// </summary>
    public bool HasSuccessMessage()
    {
        return SuccessMessage.IsVisible();
    }

    /// <summary>
    /// Wait for error message to appear.
    /// </summary>
    public bool WaitForError(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        return _context.WaitFor(() => HasErrorMessage(), timeout, "error message");
    }
}
