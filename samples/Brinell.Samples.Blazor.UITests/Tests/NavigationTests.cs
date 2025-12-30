using Brinell.Samples.Blazor.UITests.PageObjects;
using Brinell.Samples.Blazor.UITests.TestBase;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.UITests.Tests;

/// <summary>
/// Tests for navigation between pages.
/// </summary>
[Collection("BlazorUITests")]
public class NavigationTests : BlazorSampleTestBase
{
    public NavigationTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Navigation_HomePageLoad_ShowsWelcomeContent()
    {
        // Arrange & Act
        LaunchBrowser();
        WaitForBlazorReady();

        var homePage = new HomePage(Context!);
        homePage.WaitForDisplayed();

        // Assert
        homePage.AssertDisplayed("Home page should be displayed");
        homePage.PageTitle.AssertVisible("Page title should be visible");
        homePage.WelcomeMessage.AssertVisible("Welcome message should be visible");
    }

    [Fact]
    public void Navigation_HomeToCounter_WorksCorrectly()
    {
        // Arrange
        LaunchBrowser();
        WaitForBlazorReady();

        var homePage = new HomePage(Context!);
        homePage.WaitForDisplayed();

        // Act
        var counterPage = homePage.NavigateToCounter();

        // Assert
        counterPage.AssertDisplayed("Counter page should be displayed after navigation");
        GetCurrentUrl().Should().Contain("/counter", "URL should contain /counter");
    }

    [Fact]
    public void Navigation_HomeToLogin_WorksCorrectly()
    {
        // Arrange
        LaunchBrowser();
        WaitForBlazorReady();

        var homePage = new HomePage(Context!);
        homePage.WaitForDisplayed();

        // Act
        var loginPage = homePage.NavigateToLogin();

        // Assert
        loginPage.AssertDisplayed("Login page should be displayed after navigation");
        GetCurrentUrl().Should().Contain("/login", "URL should contain /login");
    }

    [Fact]
    public void Navigation_HomeToDashboard_WorksCorrectly()
    {
        // Arrange
        LaunchBrowser();
        WaitForBlazorReady();

        var homePage = new HomePage(Context!);
        homePage.WaitForDisplayed();

        // Act
        var dashboardPage = homePage.NavigateToDashboard();

        // Assert
        dashboardPage.AssertDisplayed("Dashboard page should be displayed after navigation");
        GetCurrentUrl().Should().Contain("/dashboard", "URL should contain /dashboard");
    }

    [Fact]
    public void Navigation_DashboardToHome_WorksCorrectly()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/dashboard");

        var dashboardPage = new DashboardPage(Context!);
        dashboardPage.WaitForDisplayed();

        // Act
        var homePage = dashboardPage.NavigateToHome();

        // Assert
        homePage.AssertDisplayed("Home page should be displayed after navigation");
    }

    [Fact]
    public void Navigation_DirectUrlToCounter_WorksCorrectly()
    {
        // Arrange & Act
        LaunchBrowser();
        NavigateToPage("/counter");

        var counterPage = new CounterPage(Context!);
        counterPage.WaitForDisplayed();

        // Assert
        counterPage.AssertDisplayed("Counter page should be displayed when navigating directly");
    }

    [Fact]
    public void Navigation_DirectUrlToLogin_WorksCorrectly()
    {
        // Arrange & Act
        LaunchBrowser();
        NavigateToPage("/login");

        var loginPage = new LoginPage(Context!);
        loginPage.WaitForDisplayed();

        // Assert
        loginPage.AssertDisplayed("Login page should be displayed when navigating directly");
    }

    [Fact]
    public void Navigation_AllHomeLinksVisible()
    {
        // Arrange
        LaunchBrowser();
        WaitForBlazorReady();

        var homePage = new HomePage(Context!);
        homePage.WaitForDisplayed();

        // Assert
        homePage.CounterLink.AssertVisible("Counter link should be visible");
        homePage.LoginLink.AssertVisible("Login link should be visible");
        homePage.DashboardLink.AssertVisible("Dashboard link should be visible");
    }

    [Fact]
    public void Navigation_BackAndForth_MaintainsState()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/counter");

        var counterPage = new CounterPage(Context!);
        counterPage.WaitForDisplayed();

        // Increment the counter
        counterPage.IncrementMultiple(3);
        counterPage.GetCurrentCount().Should().Be(3, "Count should be 3");

        // Navigate away
        NavigateToPage("/");
        var homePage = new HomePage(Context!);
        homePage.WaitForDisplayed();

        // Navigate back
        NavigateToPage("/counter");
        counterPage = new CounterPage(Context!);
        counterPage.WaitForDisplayed();

        // Assert - Note: Blazor Server state is lost on full navigation
        // This test documents the expected behavior
        counterPage.GetCurrentCount().Should().Be(0, "Count resets on navigation (Blazor Server behavior)");
    }

    [Fact]
    public void Navigation_BrowserBack_WorksCorrectly()
    {
        // Arrange
        LaunchBrowser();
        WaitForBlazorReady();

        var homePage = new HomePage(Context!);
        homePage.WaitForDisplayed();

        // Navigate to counter
        var counterPage = homePage.NavigateToCounter();
        counterPage.WaitForDisplayed();

        // Act - Go back
        NavigateBack();

        // Assert
        homePage = new HomePage(Context!);
        homePage.WaitForDisplayed();
        homePage.AssertDisplayed("Home page should be displayed after browser back");
    }
}
