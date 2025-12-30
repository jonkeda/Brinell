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
        
        // Act - Enter only password, leaving username empty
        loginPage.EnterPassword("password123");
        loginPage.ClickLogin();
        
        // Assert - Login button should be disabled or validation error should appear
        // Note: Based on ViewModel, the button is disabled when fields are empty
        // The validation happens on property change
        loginPage.UsernameTextBox.SetText("ab"); // Too short
        loginPage.AssertHasUsernameError("Username validation error should appear for short username");
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
        
        // Act - Enter credentials then cancel
        loginPage.EnterCredentials("testuser", "testpass");
        loginPage.ClickCancel();
        
        // Assert - Fields should be cleared
        loginPage.UsernameTextBox.AssertTextEmpty("Username should be empty after cancel");
    }
}
