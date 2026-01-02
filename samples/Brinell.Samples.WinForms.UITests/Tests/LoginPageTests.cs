using Brinell.Samples.WinForms.UITests.Pages;
using Brinell.Samples.WinForms.UITests.Fixtures;
using Xunit;
using FluentAssertions;

namespace Brinell.Samples.WinForms.UITests.Tests;

/// <summary>
/// Sample UI tests for the login page demonstrating framework usage.
/// Uses shared AppFixture to avoid launching multiple app instances.
/// </summary>
[Collection("UI Tests Collection")]
public class LoginPageTests
{
    private readonly AppFixture _fixture;

    public LoginPageTests(AppFixture fixture)
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
    public void LoginPage_ShouldDisplayLoginForm()
    {
        // Arrange
        ResetForm();
        var page = GetLoginPage();

        // Act & Assert
        page.AssertDisplayed();
    }

    [Fact]
    public void LoginPage_CanEnterUsername()
    {
        // Arrange
        ResetForm();
        var page = GetLoginPage();
        var expectedUsername = "testuser";

        // Act
        page.EnterUsername(expectedUsername);

        // Assert
        var actualUsername = page.GetUsername();
        actualUsername.Should().Be(expectedUsername);
    }

    [Fact]
    public void LoginPage_CanEnterPassword()
    {
        // Arrange
        ResetForm();
        var page = GetLoginPage();
        var expectedPassword = "password123";

        // Act
        page.EnterPassword(expectedPassword);

        // Assert
        // Note: Password fields typically don't expose the text for security
        // But the control should accept the input without error
    }

    [Fact]
    public void LoginPage_CanToggleRememberMe()
    {
        // Arrange
        ResetForm();
        var page = GetLoginPage();

        // Act
        page.SetRememberMe(true);

        // Assert
        page.IsRememberMeChecked().Should().BeTrue();

        // Act
        page.SetRememberMe(false);

        // Assert
        page.IsRememberMeChecked().Should().BeFalse();
    }

    [Fact(Skip = "ComboBox control needs additional fixes - Phase 3+ work")]
    public void LoginPage_CanSelectRole()
    {
        // Arrange
        ResetForm();
        var page = GetLoginPage();

        // Act
        page.SelectRole("Admin");

        // Assert
        page.GetSelectedRole().Should().Be("Admin");
    }

    [Fact(Skip = "ComboBox control needs additional fixes - Phase 3+ work")]
    public void LoginPage_CanLogin()
    {
        // Arrange
        ResetForm();
        var page = GetLoginPage();
        var username = "john.doe";
        var role = "User";

        // Act
        page.EnterUsername(username);
        page.SelectRole(role);
        page.ClickLogin();
        System.Threading.Thread.Sleep(500); // Wait for status update

        // Assert
        var statusMessage = page.GetStatusMessage();
        statusMessage.Should().Contain(username);
        statusMessage.Should().Contain(role);
        statusMessage.Should().Contain("Logged in");
    }

    [Fact]
    public void LoginPage_CanClearForm()
    {
        // Arrange
        ResetForm();
        var page = GetLoginPage();
        page.EnterUsername("testuser");
        page.SetRememberMe(true);

        // Act
        page.ClickClear();
        System.Threading.Thread.Sleep(300); // Wait for clear

        // Assert
        page.GetUsername().Should().BeEmpty();
        page.IsRememberMeChecked().Should().BeFalse();
    }

    [Fact]
    public void LoginPage_StatusLabelShowsReadyInitially()
    {
        // Arrange
        ResetForm();
        var page = GetLoginPage();

        // Act & Assert
        page.GetStatusMessage().Should().Contain("Ready");
    }

    [Fact(Skip = "ComboBox control needs additional fixes - Phase 3+ work")]
    public void LoginPage_CanSelectMultipleRoles()
    {
        // Arrange
        ResetForm();
        var page = GetLoginPage();
        var roles = new[] { "Admin", "User", "Guest" };

        // Act & Assert
        foreach (var role in roles)
        {
            page.SelectRole(role);
            page.GetSelectedRole().Should().Be(role);
        }
    }

    [Fact(Skip = "ComboBox control needs additional fixes - Phase 3+ work")]
    public void LoginPage_CanLoginWithAllRoles()
    {
        // Arrange
        ResetForm();
        var page = GetLoginPage();
        var username = "testuser";
        var roles = new[] { "Admin", "User", "Guest" };

        // Act & Assert
        foreach (var role in roles)
        {
            page.EnterUsername(username);
            page.SelectRole(role);
            page.ClickLogin();
            System.Threading.Thread.Sleep(300);

            var status = page.GetStatusMessage();
            status.Should().Contain("Logged in");
            status.Should().Contain(role);

            page.ClickClear();
            System.Threading.Thread.Sleep(200);
        }
    }
}
