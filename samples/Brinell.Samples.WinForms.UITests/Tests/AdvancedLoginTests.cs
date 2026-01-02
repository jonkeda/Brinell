using Brinell.Samples.WinForms.UITests.Pages;
using Brinell.Samples.WinForms.UITests.Fixtures;
using Xunit;
using FluentAssertions;

namespace Brinell.Samples.WinForms.UITests.Tests;

/// <summary>
/// Advanced UI tests demonstrating framework patterns and best practices.
/// </summary>
[Collection("UI Tests Collection")]
public class AdvancedLoginTests
{
    private readonly AppFixture _fixture;

    public AdvancedLoginTests(AppFixture fixture)
    {
        _fixture = fixture;
    }

    private LoginPage GetLoginPage() => _fixture.LoginPage;

    /// <summary>
    /// Reset form to clean state before each test.
    /// </summary>
    private void ResetForm()
    {
        var page = GetLoginPage();
        try
        {
            page.ClickClear();
            System.Threading.Thread.Sleep(100);
        }
        catch
        {
            // Form might already be clean
        }
    }

    [Fact]
    public void AdvancedLogin_DemonstatesWaitPattern()
    {
        // This test demonstrates the Wait pattern which is useful for:
        // - Waiting for elements to appear
        // - Waiting for elements to become visible
        // - Waiting for elements to be enabled
        
        // Arrange
        ResetForm();
        var page = GetLoginPage();

        // Act - Wait for form to be ready
        page.WaitForReady(timeoutMs: 5000);

        // Assert
        page.AssertDisplayed();
    }

    [Fact]
    public void AdvancedLogin_DemonstatesCheckPattern()
    {
        // Check pattern is similar to Wait but throws if condition not met
        // Useful for asserting state during test execution
        
        // Arrange
        ResetForm();
        var page = GetLoginPage();

        // Act & Assert
        page.EnterUsername("admin");
        
        // This will throw if username is not visible within timeout
        page.CheckDisplayed(timeoutMs: 2000);
    }

    [Fact(Skip = "ComboBox control needs additional fixes - Phase 3+ work")]
    public void AdvancedLogin_DemonstatesAssertPattern()
    {
        // Assert pattern provides explicit assertions for test verification
        
        // Arrange
        ResetForm();
        var page = GetLoginPage();

        // Act
        page.SelectRole("Admin");

        // Assert - These are explicit assertions
        page.GetSelectedRole().Should().Be("Admin", "because we selected Admin");
    }

    [Fact(Skip = "ComboBox control needs additional fixes - Phase 3+ work")]
    public void AdvancedLogin_TestCompleteWorkflow()
    {
        // This test demonstrates a complete user workflow
        // using the page object model
        
        // Arrange
        ResetForm();
        var page = GetLoginPage();
        var testUsername = "john.smith";
        var testPassword = "SecurePass123!";
        var testRole = "User";

        // Act - Complete login workflow
        page.WaitForDisplayed(); // Wait for page to be ready
        page.EnterUsername(testUsername);
        page.EnterPassword(testPassword);
        page.SetRememberMe(true);
        page.SelectRole(testRole);
        
        // Assert intermediate state
        page.GetUsername().Should().Be(testUsername);
        page.IsRememberMeChecked().Should().BeTrue();
        page.GetSelectedRole().Should().Be(testRole);

        // Act - Complete login
        page.ClickLogin();
        System.Threading.Thread.Sleep(500);

        // Assert final state
        var statusMessage = page.GetStatusMessage();
        statusMessage.Should().Contain("Logged in");
        statusMessage.Should().Contain(testUsername);
        statusMessage.Should().Contain(testRole);
    }

    [Fact(Skip = "ComboBox control needs additional fixes - Phase 3+ work")]
    public void AdvancedLogin_TestFormReset()
    {
        // This test verifies that the clear functionality works correctly
        
        // Arrange
        ResetForm();
        var page = GetLoginPage();
        var initialStatus = page.GetStatusMessage();

        // Act - Fill form
        page.EnterUsername("testuser");
        page.EnterPassword("password");
        page.SetRememberMe(true);
        page.SelectRole("Admin");

        // Verify form is filled
        page.GetUsername().Should().NotBeEmpty();
        page.IsRememberMeChecked().Should().BeTrue();

        // Act - Clear form
        page.ClickClear();
        System.Threading.Thread.Sleep(300);

        // Assert - Form should be reset
        page.GetUsername().Should().BeEmpty();
        page.IsRememberMeChecked().Should().BeFalse();
        page.GetSelectedRole().Should().Be("Admin"); // Role resets to first item
        page.GetStatusMessage().Should().Contain("Ready");
    }

    [Fact(Skip = "ComboBox control needs additional fixes - Phase 3+ work")]
    public void AdvancedLogin_TestMultipleLogins()
    {
        // This test verifies the application handles multiple login attempts
        
        // Arrange
        ResetForm();
        var page = GetLoginPage();
        var testCases = new[]
        {
            ("alice", "Admin"),
            ("bob", "User"),
            ("charlie", "Guest")
        };

        // Act & Assert
        foreach (var (username, role) in testCases)
        {
            // Login
            page.EnterUsername(username);
            page.SelectRole(role);
            page.ClickLogin();
            System.Threading.Thread.Sleep(300);

            // Verify login succeeded
            var status = page.GetStatusMessage();
            status.Should().Contain(username);
            status.Should().Contain(role);
            status.Should().Contain("Logged in");

            // Clear for next iteration
            page.ClickClear();
            System.Threading.Thread.Sleep(200);
        }
    }

    [Fact]
    public void AdvancedLogin_TestControlVisibility()
    {
        // This test demonstrates checking control visibility
        
        // Arrange
        ResetForm();
        var page = GetLoginPage();

        // Act & Assert
        // All controls should be visible
        page.AssertDisplayed(); // Page is displayed
        
        // Individual controls should exist
        // This is implicit in the page object but could be made explicit
    }
}
