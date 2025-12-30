using Brinell.Samples.Wpf.UITests.PageObjects;
using Brinell.Samples.Wpf.UITests.TestBase;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Wpf.UITests.Tests;

/// <summary>
/// Tests for navigation between pages.
/// </summary>
[Collection("UITests")]
public class NavigationTests : WpfSampleTestBase
{
    public NavigationTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Application_Launch_ShowsShellWithHomeAsDefault()
    {
        // Arrange & Act
        LaunchApplication();
        var shell = new ShellPage(Context);
        shell.WaitForDisplayed();
        
        // Assert
        shell.AssertDisplayed("Shell window should be displayed on launch");
        shell.AppTitleText.AssertVisible("App title should be visible in sidebar");
        
        // Home page should be the default content
        var homePage = new HomePage(Context);
        homePage.WaitForDisplayed();
        homePage.AssertDisplayed("Home page should be displayed by default");
    }

    [Fact]
    public void Navigation_FromHomeToLogin_ShowsLoginPage()
    {
        // Arrange
        LaunchApplication();
        var shell = new ShellPage(Context);
        shell.WaitForDisplayed();
        
        // Verify starting on home page
        var homePage = new HomePage(Context);
        homePage.WaitForDisplayed();
        
        // Act
        var loginPage = shell.NavigateToLogin();
        
        // Assert
        loginPage.AssertDisplayed("Login page should be displayed after navigation");
        loginPage.LoginHeader.AssertVisible("Login header should be visible");
    }

    [Fact]
    public void Navigation_FromLoginToHome_ShowsHomePage()
    {
        // Arrange
        LaunchApplication();
        var shell = new ShellPage(Context);
        shell.WaitForDisplayed();
        
        // Navigate to login first
        var loginPage = shell.NavigateToLogin();
        loginPage.WaitForDisplayed();
        
        // Act
        var homePage = shell.NavigateToHome();
        
        // Assert
        homePage.AssertDisplayed("Home page should be displayed after navigation");
        homePage.WelcomeText.AssertVisible("Welcome text should be visible");
    }

    [Fact]
    public void Navigation_SidebarButtons_AreAllVisible()
    {
        // Arrange
        LaunchApplication();
        var shell = new ShellPage(Context);
        shell.WaitForDisplayed();
        
        // Assert
        shell.NavHomeButton.AssertVisible("Home navigation button should be visible");
        shell.NavLoginButton.AssertVisible("Login navigation button should be visible");
        shell.NavFormsButton.AssertVisible("Forms navigation button should be visible");
        shell.NavDataGridButton.AssertVisible("DataGrid navigation button should be visible");
    }

    [Fact]
    public void Navigation_MultipleTimes_WorksCorrectly()
    {
        // Arrange
        LaunchApplication();
        var shell = new ShellPage(Context);
        shell.WaitForDisplayed();
        
        // Act & Assert - Navigate back and forth multiple times
        for (int i = 0; i < 3; i++)
        {
            var loginPage = shell.NavigateToLogin();
            loginPage.AssertDisplayed($"Login page should be displayed (iteration {i + 1})");
            
            var homePage = shell.NavigateToHome();
            homePage.AssertDisplayed($"Home page should be displayed (iteration {i + 1})");
        }
    }
}
