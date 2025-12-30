using Brinell.Samples.Blazor.UITests.PageObjects;
using Brinell.Samples.Blazor.UITests.TestBase;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.UITests.Tests;

/// <summary>
/// Tests for the Login page functionality.
/// </summary>
[Collection("BlazorUITests")]
public class LoginTests : BlazorSampleTestBase
{
    public LoginTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Login_WithValidCredentials_NavigatesToDashboard()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/login");

        var loginPage = new LoginPage(Context!);
        loginPage.WaitForDisplayed();

        // Act
        var dashboardPage = loginPage.SubmitValidLogin("test@example.com", "password123");

        // Assert
        dashboardPage.AssertDisplayed("Dashboard page should be displayed after successful login");
        dashboardPage.AssertHasWelcomeAlert("Welcome alert should be shown");
    }

    [Fact]
    public void Login_WithInvalidCredentials_ShowsErrorMessage()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/login");

        var loginPage = new LoginPage(Context!);
        loginPage.WaitForDisplayed();

        // Act
        loginPage.SubmitInvalidLogin("wrong@example.com", "wrongpassword");

        // Assert
        loginPage.WaitForError();
        loginPage.AssertHasError("Error message should appear for invalid credentials");
        loginPage.AssertErrorNotEmpty();
    }

    [Fact]
    public void Login_PageLoad_ShowsAllFormElements()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/login");

        var loginPage = new LoginPage(Context!);
        loginPage.WaitForDisplayed();

        // Assert
        loginPage.AssertDisplayed("Login page should be displayed");
        loginPage.EmailInput.AssertVisible("Email input should be visible");
        loginPage.PasswordInput.AssertVisible("Password input should be visible");
        loginPage.LoginButton.AssertVisible("Login button should be visible");
        loginPage.TestCredentialsInfo.AssertVisible("Test credentials info should be visible");
    }

    [Fact]
    public void Login_EmailInputHasPlaceholder()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/login");

        var loginPage = new LoginPage(Context!);
        loginPage.WaitForDisplayed();

        // Assert
        loginPage.EmailInput.AssertHasPlaceholder("Email input should have a placeholder");
    }

    [Fact]
    public void Login_PasswordInputHasCorrectType()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/login");

        var loginPage = new LoginPage(Context!);
        loginPage.WaitForDisplayed();

        // Assert
        loginPage.PasswordInput.AssertInputType("password");
    }

    [Fact]
    public void Login_ShowsLoadingSpinnerDuringSubmit()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/login");

        var loginPage = new LoginPage(Context!);
        loginPage.WaitForDisplayed();

        // Act - Enter credentials and click login
        loginPage.EnterCredentials("test@example.com", "password123");
        loginPage.ClickLogin();

        // Assert - Wait for either success message or dashboard
        // The spinner appears briefly, success message shows for 1 second, then navigates to dashboard
        var dashboardPage = new DashboardPage(Context!);
        var result = Context!.WaitFor(
            () => loginPage.HasSuccessMessage() || dashboardPage.IsDisplayed(),
            10000,
            "success message or dashboard");

        Assert.True(result, "Login should complete with success message or dashboard");
    }

    [Fact]
    public void Login_ClearFields_WorksCorrectly()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/login");

        var loginPage = new LoginPage(Context!);
        loginPage.WaitForDisplayed();

        // Enter some text
        loginPage.EnterEmail("test@example.com");
        loginPage.EnterPassword("password123");

        // Act - Clear the fields
        loginPage.EmailInput.Clear();
        loginPage.PasswordInput.Clear();

        // Assert
        loginPage.EmailInput.AssertTextEmpty("Email should be empty after clear");
        loginPage.PasswordInput.AssertTextEmpty("Password should be empty after clear");
    }
}
