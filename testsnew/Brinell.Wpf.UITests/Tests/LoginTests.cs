using Brinell.Wpf.UITests.Fixtures;
using Brinell.Wpf.UITests.PageObjects;

namespace Brinell.Wpf.UITests.Tests;

/// <summary>
/// Tests for the Login page form and validation.
/// </summary>
[Collection("WPF UITests")]
public class LoginTests
{
    private readonly WpfSampleFixture _fixture;

    public LoginTests(WpfSampleFixture fixture) => _fixture = fixture;

    [Fact]
    public void Login_WithValidCredentials_NavigatesToHomePage()
    {
        var shell = new ShellPage(_fixture.Context);
        shell.WaitLoaded(true);

        var loginPage = shell.NavigateToLogin();
        loginPage.WaitPageReady();

        var homePage = loginPage.SubmitValidLogin("demo", "password");

        Assert.True(homePage.IsLoaded(), "Home page should be displayed after successful login");
    }

    [Fact]
    public void Login_WithInvalidCredentials_ShowsErrorMessage()
    {
        var shell = new ShellPage(_fixture.Context);
        shell.WaitLoaded(true);

        var loginPage = shell.NavigateToLogin();
        loginPage.WaitPageReady();

        loginPage.SubmitInvalidLogin("wronguser", "wrongpass");

        loginPage.LoginErrorText.AssertVisible(true, "Login error should be displayed for invalid credentials");
        loginPage.LoginErrorText.AssertTextContains("Invalid");
    }

    [Fact]
    public void Login_WithShortUsername_ShowsValidationError()
    {
        var shell = new ShellPage(_fixture.Context);
        shell.WaitLoaded(true);

        var loginPage = shell.NavigateToLogin();
        loginPage.WaitPageReady();

        loginPage.UsernameTextBox.SetText("ab");

        loginPage.UsernameErrorText.AssertVisible(true, "Username error should appear for short input");
        loginPage.LoginButton.AssertEnabled(false, "Login button should be disabled with invalid username");

        loginPage.EnterCredentials("validuser", "password");
        Assert.True(loginPage.LoginButton.WaitEnabled(true, timeoutMs: 5000),
            "Login button should be enabled with valid credentials");
    }

    [Fact]
    public void Login_WithShortPassword_ShowsValidationError()
    {
        var shell = new ShellPage(_fixture.Context);
        shell.WaitLoaded(true);

        var loginPage = shell.NavigateToLogin();
        loginPage.WaitPageReady();

        loginPage.UsernameTextBox.SetText("validuser");
        loginPage.PasswordField.SetText("12345");

        loginPage.PasswordErrorText.AssertVisible(true, "Password error should appear for short password");
        loginPage.PasswordErrorText.AssertTextContains("6 characters");
    }

    [Fact]
    public void Login_CancelButton_ClearsFormOrNavigatesBack()
    {
        var shell = new ShellPage(_fixture.Context);
        shell.WaitLoaded(true);

        var loginPage = shell.NavigateToLogin();
        loginPage.WaitPageReady();

        loginPage.EnterCredentials("testuser", "testpass");
        loginPage.UsernameTextBox.AssertText("testuser");

        loginPage.ClickCancel();

        // Cancel may navigate back to shell or clear the form
        var shellPage = new ShellPage(_fixture.Context);
        if (shellPage.WaitLoaded(true, timeoutMs: 2000))
        {
            Assert.True(shellPage.IsLoaded(), "Should navigate back to shell after cancel");
        }
        else
        {
            loginPage.UsernameTextBox.AssertText("", "Username should be empty after cancel");
        }
    }
}
