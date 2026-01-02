using Brinell.Core.Abstractions;
using Brinell.Html.Playwright.Controls;
using Brinell.Html.Playwright.UITests;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Html.Playwright.Samples;

/// <summary>
/// Sample async UI tests demonstrating IControlObjectAsync and async test patterns.
/// </summary>
/// <remarks>
/// This sample shows how to write async UI tests with Playwright.
/// Key differences from sync tests:
/// - Test methods are async Task instead of void
/// - All control operations use async/await
/// - No blocking waits - async/await handles coordination
/// - Better performance in parallel test scenarios
/// 
/// When to use async tests:
/// - Multi-step workflows with many async operations
/// - Tests that benefit from parallelization
/// - Performance-critical test scenarios
/// - Web testing where async is natural
/// 
/// When to use sync tests:
/// - Simple sequential tests
/// - Desktop UI testing (WPF, WinForms)
/// - Game engine testing (Stride)
/// - Rapid test development
/// </remarks>
public class LoginFormAsyncTests : PlaywrightUITestBaseAsync
{
    public LoginFormAsyncTests(ITestOutputHelper output) : base(output) { }

    /// <summary>
    /// Sample async test: navigate to login page and verify form is ready.
    /// </summary>
    [Fact(Skip = "Sample only - requires test server")]
    public async Task LoginPage_Load_DisplaysForm()
    {
        // Arrange
        await GotoAsync("http://localhost:5180/login");
        
        var usernameInput = new TextControlAsync(Context, null, "UsernameInput", "#username");
        var passwordInput = new TextControlAsync(Context, null, "PasswordInput", "#password");
        var loginButton = new ButtonControlAsync(Context, null, "LoginButton", "#login-btn");

        // Act & Assert
        await usernameInput.AssertVisibleAsync("Username input should be visible");
        await passwordInput.AssertVisibleAsync("Password input should be visible");
        await loginButton.AssertVisibleAsync("Login button should be visible");
        
        Log("✓ Login form loaded successfully");
    }

    /// <summary>
    /// Sample async test: login with valid credentials.
    /// Demonstrates async/await flow and multi-step operations.
    /// </summary>
    [Fact(Skip = "Sample only - requires test server")]
    public async Task Login_ValidCredentials_NavigatesToDashboard()
    {
        // Arrange
        await GotoAsync("http://localhost:5180/login");
        
        var usernameInput = new TextControlAsync(Context, null, "UsernameInput", "#username");
        var passwordInput = new TextControlAsync(Context, null, "PasswordInput", "#password");
        var loginButton = new ButtonControlAsync(Context, null, "LoginButton", "#login-btn");
        var dashboardHeader = new TextControlAsync(Context, null, "DashboardHeader", "h1");

        // Act
        await usernameInput.SetTextAsync("testuser@example.com");
        Log("Username entered");
        
        await passwordInput.SetTextAsync("password123");
        Log("Password entered");
        
        await loginButton.WaitAndClickAsync();
        Log("Login button clicked");
        
        // Wait a bit for navigation
        await DelayAsync(1000);

        // Assert
        await dashboardHeader.AssertVisibleAsync("Dashboard should load after login");
        await dashboardHeader.AssertTextEqualsAsync("Welcome, Test User");
        
        Log("✓ Login successful and dashboard displayed");
    }

    /// <summary>
    /// Sample async test: form validation on empty fields.
    /// Demonstrates error handling and validation testing.
    /// </summary>
    [Fact(Skip = "Sample only - requires test server")]
    public async Task Login_EmptyUsername_ShowsValidationError()
    {
        // Arrange
        await GotoAsync("http://localhost:5180/login");
        
        var passwordInput = new TextControlAsync(Context, null, "PasswordInput", "#password");
        var loginButton = new ButtonControlAsync(Context, null, "LoginButton", "#login-btn");
        var errorMessage = new TextControlAsync(Context, null, "ErrorMessage", ".error-message");

        // Act
        await passwordInput.SetTextAsync("password123");
        await loginButton.ClickAsync();

        // Assert
        await errorMessage.AssertVisibleAsync("Validation error should appear");
        await errorMessage.AssertTextContainsAsync("username", "Error should mention username");
        
        Log("✓ Validation error displayed correctly");
    }

    /// <summary>
    /// Sample async test: parallel async operations.
    /// Demonstrates that async/await enables better resource utilization.
    /// </summary>
    [Fact(Skip = "Sample only - requires test server")]
    public async Task Login_MultipleFieldsAsync_PerformsEfficiently()
    {
        // Arrange
        await GotoAsync("http://localhost:5180/login");
        
        var usernameInput = new TextControlAsync(Context, null, "UsernameInput", "#username");
        var passwordInput = new TextControlAsync(Context, null, "PasswordInput", "#password");

        // Act - Fill both fields asynchronously (no blocking waits)
        // In real async code, these would run more concurrently
        await Task.WhenAll(
            usernameInput.SetTextAsync("testuser@example.com").AsTask(),
            passwordInput.SetTextAsync("password123").AsTask()
        );

        // Assert
        await usernameInput.AssertTextEqualsAsync("testuser@example.com");
        await passwordInput.AssertTextEqualsAsync("password123");
        
        Log("✓ Async field operations completed efficiently");
    }

    /// <summary>
    /// Sample async test: interactive button behavior.
    /// Demonstrates hover, click, and state testing.
    /// </summary>
    [Fact(Skip = "Sample only - requires test server")]
    public async Task LoginButton_Interactions_WorkCorrectly()
    {
        // Arrange
        await GotoAsync("http://localhost:5180/login");
        
        var loginButton = new ButtonControlAsync(Context, null, "LoginButton", "#login-btn");

        // Act & Assert
        await loginButton.AssertEnabledAsync("Button should be enabled initially");
        
        // Hover over button
        await loginButton.HoverAsync();
        Log("Hovered over button");
        
        // Check clickable
        var isClickable = await loginButton.IsClickableAsync();
        Assert.True(isClickable, "Button should be clickable");
        
        // Verify text
        var buttonText = await loginButton.GetTextAsync();
        Assert.Equal("Sign In", buttonText);
        
        Log("✓ Button interactions work correctly");
    }
}

/// <summary>
/// Another sample showing form reset functionality.
/// </summary>
public class FormResetAsyncTests : PlaywrightUITestBaseAsync
{
    public FormResetAsyncTests(ITestOutputHelper output) : base(output) { }

    /// <summary>
    /// Sample async test: reset form clears all fields.
    /// </summary>
    [Fact(Skip = "Sample only - requires test server")]
    public async Task LoginForm_Reset_ClearsAllFields()
    {
        // Arrange
        await GotoAsync("http://localhost:5180/login");
        
        var usernameInput = new TextControlAsync(Context, null, "UsernameInput", "#username");
        var passwordInput = new TextControlAsync(Context, null, "PasswordInput", "#password");
        var resetButton = new ButtonControlAsync(Context, null, "ResetButton", "#reset-btn");

        // Act - Fill form
        await usernameInput.SetTextAsync("testuser@example.com");
        await passwordInput.SetTextAsync("password123");
        Log("Form filled with data");

        // Verify filled
        await usernameInput.AssertTextEqualsAsync("testuser@example.com");
        await passwordInput.AssertTextEqualsAsync("password123");
        Log("Form data verified");

        // Reset form
        await resetButton.ClickAsync();
        Log("Reset button clicked");
        await DelayAsync(500);

        // Assert - Verify cleared
        await usernameInput.AssertTextEqualsAsync("");
        await passwordInput.AssertTextEqualsAsync("");
        
        Log("✓ Form reset successful - all fields cleared");
    }
}
