using Brinell.Wpf.Controls;
using Brinell.Wpf.Controls.Base;
using Brinell.Wpf.Infrastructure;

namespace Brinell.Samples.Wpf.UITests.PageObjects;

/// <summary>
/// Page object for the Login page with form validation.
/// </summary>
public class LoginPage : BusyPageBase
{
    /// <summary>
    /// The login header text.
    /// </summary>
    public LabelControl LoginHeader { get; }
    
    /// <summary>
    /// Username input field.
    /// </summary>
    public TextBoxControl UsernameTextBox { get; }
    
    /// <summary>
    /// Username validation error label.
    /// </summary>
    public LabelControl UsernameErrorText { get; }
    
    /// <summary>
    /// Password input field (PasswordBox).
    /// </summary>
    public TextBoxControl PasswordBox { get; }
    
    /// <summary>
    /// Password validation error label.
    /// </summary>
    public LabelControl PasswordErrorText { get; }
    
    /// <summary>
    /// Login error message container.
    /// </summary>
    public LabelControl LoginErrorText { get; }
    
    /// <summary>
    /// The Sign In button.
    /// </summary>
    public ButtonControl LoginButton { get; }
    
    /// <summary>
    /// The Cancel button.
    /// </summary>
    public ButtonControl CancelButton { get; }
    
    /// <summary>
    /// The busy indicator shown during login.
    /// </summary>
    public LabelControl BusyIndicator { get; }
    
    /// <summary>
    /// Success message shown after successful login.
    /// </summary>
    public LabelControl SuccessText { get; }

    public LoginPage(FlaUITestContext context)
        : base(context, "LoginPage")
    {
        LoginHeader = new LabelControl(context, this, "LoginHeader");
        UsernameTextBox = new TextBoxControl(context, this, "UsernameTextBox");
        UsernameErrorText = new LabelControl(context, this, "UsernameErrorText");
        PasswordBox = new TextBoxControl(context, this, "PasswordBox");
        PasswordErrorText = new LabelControl(context, this, "PasswordErrorText");
        LoginErrorText = new LabelControl(context, this, "LoginErrorText");
        LoginButton = new ButtonControl(context, this, "LoginButton");
        CancelButton = new ButtonControl(context, this, "CancelButton");
        BusyIndicator = new LabelControl(context, this, "BusyIndicator");
        SuccessText = new LabelControl(context, this, "SuccessText");
    }

    /// <summary>
    /// Check if the login page is displayed.
    /// </summary>
    public override bool IsDisplayed()
    {
        return LoginHeader.IsVisible();
    }

    /// <summary>
    /// Check if the page is busy (showing busy indicator).
    /// </summary>
    public override bool IsBusy()
    {
        return BusyIndicator.IsVisible();
    }

    /// <summary>
    /// Enter username and password credentials.
    /// </summary>
    public LoginPage EnterCredentials(string username, string password)
    {
        Log($"EnterCredentials({username}, ***)");
        UsernameTextBox.SetText(username);
        PasswordBox.SetText(password);
        return this;
    }

    /// <summary>
    /// Enter only the username.
    /// </summary>
    public LoginPage EnterUsername(string username)
    {
        Log($"EnterUsername({username})");
        UsernameTextBox.SetText(username);
        return this;
    }

    /// <summary>
    /// Enter only the password.
    /// </summary>
    public LoginPage EnterPassword(string password)
    {
        Log("EnterPassword(***)");
        PasswordBox.SetText(password);
        return this;
    }

    /// <summary>
    /// Click the Sign In button.
    /// </summary>
    public LoginPage ClickLogin()
    {
        Log("ClickLogin()");
        LoginButton.Click();
        return this;
    }

    /// <summary>
    /// Click the Cancel button.
    /// </summary>
    public LoginPage ClickCancel()
    {
        Log("ClickCancel()");
        CancelButton.Click();
        return this;
    }

    /// <summary>
    /// Perform a complete login with valid credentials and wait for navigation.
    /// </summary>
    public HomePage SubmitValidLogin(string username, string password)
    {
        Log($"SubmitValidLogin({username}, ***)");
        EnterCredentials(username, password);
        ClickLogin();
        
        // Wait for login to complete and navigate to home
        WaitForNotBusy();
        
        var homePage = new HomePage(FlaContext);
        homePage.WaitForDisplayed();
        return homePage;
    }

    /// <summary>
    /// Submit login expecting it to fail with an error.
    /// </summary>
    public LoginPage SubmitInvalidLogin(string username, string password)
    {
        Log($"SubmitInvalidLogin({username}, ***)");
        EnterCredentials(username, password);
        ClickLogin();
        
        // Wait for the login operation to complete
        WaitForNotBusy();
        
        return this;
    }

    /// <summary>
    /// Check if username validation error is displayed.
    /// </summary>
    public bool HasUsernameError()
    {
        return UsernameErrorText.IsVisible();
    }

    /// <summary>
    /// Check if password validation error is displayed.
    /// </summary>
    public bool HasPasswordError()
    {
        return PasswordErrorText.IsVisible();
    }

    /// <summary>
    /// Check if login error is displayed.
    /// </summary>
    public bool HasLoginError()
    {
        return LoginErrorText.IsVisible();
    }

    /// <summary>
    /// Get the username validation error text.
    /// </summary>
    public string GetUsernameError()
    {
        return UsernameErrorText.GetText();
    }

    /// <summary>
    /// Get the password validation error text.
    /// </summary>
    public string GetPasswordError()
    {
        return PasswordErrorText.GetText();
    }

    /// <summary>
    /// Get the login error text.
    /// </summary>
    public string GetLoginError()
    {
        return LoginErrorText.GetText();
    }
}
