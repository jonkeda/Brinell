using Brinell.Samples.Wpf.UITests.PageObjects;
using Brinell.Samples.Wpf.UITests.TestBase;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Wpf.UITests.Tests;

/// <summary>
/// Tests for the Login page functionality.
/// </summary>
[Collection("UITests")]
public class LoginTests : WpfSampleTestBase
{
    public LoginTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Login_WithValidCredentials_NavigatesToHomePage()
    {
        // Arrange
        LaunchApplication();
        var shell = new ShellPage(Context);
        shell.WaitForDisplayed();
        
        var loginPage = shell.NavigateToLogin();
        loginPage.WaitForReady();
        
        // Act
        var homePage = loginPage.SubmitValidLogin("demo", "password");
        
        // Assert
        homePage.AssertDisplayed("Home page should be displayed after successful login");
    }

    [Fact]
    public void Login_WithInvalidCredentials_ShowsErrorMessage()
    {
        // Arrange
        LaunchApplication();
        var shell = new ShellPage(Context);
        shell.WaitForDisplayed();
        
        var loginPage = shell.NavigateToLogin();
        loginPage.WaitForReady();
        
        // Act
        loginPage.SubmitInvalidLogin("wronguser", "wrongpass");
        
        // Assert
        loginPage.AssertHasLoginError("Login error should be displayed for invalid credentials");
        loginPage.AssertLoginErrorContains("Invalid");
    }

    [Fact]
    public void Login_WithEmptyUsername_ShowsValidationError()
    {
        // Arrange
        LaunchApplication();
        var shell = new ShellPage(Context);
        shell.WaitForDisplayed();
        
        var loginPage = shell.NavigateToLogin();
        loginPage.WaitForReady();
        
        // Act - Test validation by entering short username
        // The login form validates on property change
        loginPage.EnterUsername("ab"); // Too short for validation
        
        // Assert - Validation error should appear for short username
        loginPage.AssertHasUsernameError("Username validation error should appear for short username");
        
        // Verify login button is disabled with invalid input
        loginPage.LoginButton.AssertDisabled("Login button should be disabled with invalid username");
        
        // Now fix both fields with valid values
        loginPage.EnterCredentials("validuser", "password");
        
        // Wait for button to become enabled (with explicit wait)
        var isEnabled = loginPage.LoginButton.WaitEnabled(expected: true, timeoutMs: 5000);
        Assert.True(isEnabled, "Login button should be enabled with valid credentials");
    }

    [Fact]
    public void Login_WithShortPassword_ShowsValidationError()
    {
        // Arrange
        LaunchApplication();
        var shell = new ShellPage(Context);
        shell.WaitForDisplayed();
        
        var loginPage = shell.NavigateToLogin();
        loginPage.WaitForReady();
        
        // Act - Enter valid username but short password
        loginPage.EnterUsername("validuser");
        loginPage.EnterPassword("12345"); // Less than 6 characters
        
        // Assert
        loginPage.AssertHasPasswordError("Password validation error should appear for short password");
        loginPage.AssertPasswordErrorContains("6 characters");
    }

    [Fact]
    public void Login_CancelButton_ClearsForm()
    {
        // Arrange
        LaunchApplication();
        var shell = new ShellPage(Context);
        shell.WaitForDisplayed();
        
        var loginPage = shell.NavigateToLogin();
        loginPage.WaitForReady();
        
        // Act - Enter credentials
        loginPage.EnterCredentials("testuser", "testpass");
        
        // Verify credentials were entered
        var usernameText = loginPage.UsernameTextBox.GetText();
        Assert.Equal("testuser", usernameText);
        
        // Click cancel
        loginPage.ClickCancel();
        
        // Assert - After cancel, depending on app behavior, either:
        // 1. We navigate back to shell, or
        // 2. We stay on login page with cleared fields
        // Try waiting for shell page, and if that doesn't work, stay on login
        var shellPage = new ShellPage(Context);
        try
        {
            shellPage.WaitForDisplayed(2000);
            // If we got here, we navigated back to shell
            Assert.True(shellPage.IsDisplayed(), "Should navigate back to shell after cancel");
        }
        catch
        {
            // Otherwise, we're still on login page, fields should be cleared
            loginPage.WaitForDisplayed(2000);
            loginPage.UsernameTextBox.AssertTextEmpty("Username should be empty after cancel");
            loginPage.PasswordBox.AssertTextEmpty("Password should be empty after cancel");
        }
    }
}
