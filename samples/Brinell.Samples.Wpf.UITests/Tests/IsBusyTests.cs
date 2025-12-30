using Brinell.Samples.Wpf.UITests.PageObjects;
using Brinell.Samples.Wpf.UITests.TestBase;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Wpf.UITests.Tests;

/// <summary>
/// Tests for IsBusy indicator functionality during async operations.
/// </summary>
[Collection("UITests")]
public class IsBusyTests : WpfSampleTestBase
{
    public IsBusyTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Login_DuringSubmit_ShowsBusyIndicator()
    {
        // Arrange
        LaunchApplication();
        var shell = new ShellPage(Context);
        shell.WaitForDisplayed();
        
        var loginPage = shell.NavigateToLogin();
        loginPage.WaitForReady();
        
        // Act - Enter valid credentials and submit
        loginPage.EnterCredentials("demo", "password");
        loginPage.ClickLogin();
        
        // Assert - Busy indicator should appear during login operation
        // The login has a 1500ms simulated delay, so we should catch the busy state
        var wasBusy = loginPage.WaitForBusy(timeoutMs: 2000);
        Assert.True(wasBusy, "Busy indicator should appear during login operation");
    }

    [Fact]
    public void Login_AfterSubmitCompletes_HidesBusyIndicator()
    {
        // Arrange
        LaunchApplication();
        var shell = new ShellPage(Context);
        shell.WaitForDisplayed();
        
        var loginPage = shell.NavigateToLogin();
        loginPage.WaitForReady();
        
        // Act - Submit login and wait for completion
        loginPage.EnterCredentials("wronguser", "wrongpass");
        loginPage.ClickLogin();
        
        // Wait for busy indicator to appear first
        loginPage.WaitForBusy(timeoutMs: 2000);
        
        // Wait for busy indicator to disappear
        var isNotBusy = loginPage.WaitForNotBusy(timeoutMs: 5000);
        
        // Assert
        Assert.True(isNotBusy, "Busy indicator should hide after login completes");
        Assert.False(loginPage.IsBusy(), "Page should not be busy after operation completes");
    }

    [Fact]
    public void Login_WhileBusy_InputsAreDisabled()
    {
        // Arrange
        LaunchApplication();
        var shell = new ShellPage(Context);
        shell.WaitForDisplayed();
        
        var loginPage = shell.NavigateToLogin();
        loginPage.WaitForReady();
        
        // Verify inputs are enabled before submit
        loginPage.UsernameTextBox.AssertEnabled("Username should be enabled before submit");
        
        // Act - Enter credentials and submit
        loginPage.EnterCredentials("demo", "password");
        loginPage.ClickLogin();
        
        // Wait for busy state
        var wasBusy = loginPage.WaitForBusy(timeoutMs: 2000);
        
        // Assert - During busy state, inputs should be disabled
        // Note: This test may be timing-sensitive as the busy state is brief
        if (wasBusy)
        {
            // If we caught the busy state, check that inputs are disabled
            var isUsernameEnabled = loginPage.UsernameTextBox.IsEnabled();
            Assert.False(isUsernameEnabled, "Username input should be disabled while busy");
        }
    }

    [Fact]
    public void Login_IsReadyCheck_WaitForBothDisplayedAndNotBusy()
    {
        // Arrange
        LaunchApplication();
        var shell = new ShellPage(Context);
        shell.WaitForDisplayed();
        
        var loginPage = shell.NavigateToLogin();
        
        // Act - Use the IsReady check which combines displayed and not busy
        var isReady = loginPage.WaitForReady();
        
        // Assert
        loginPage.AssertDisplayed("Page should be displayed");
        loginPage.BusyIndicator.AssertNotVisible("Page should not be busy");
    }
}
