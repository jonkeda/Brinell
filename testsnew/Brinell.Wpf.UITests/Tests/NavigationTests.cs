using Brinell.Wpf.UITests.Fixtures;
using Brinell.Wpf.UITests.PageObjects;

namespace Brinell.Wpf.UITests.Tests;

/// <summary>
/// Tests for navigation between pages using the shell sidebar.
/// </summary>
[Collection("WPF UITests")]
public class NavigationTests
{
    private readonly WpfSampleFixture _fixture;

    public NavigationTests(WpfSampleFixture fixture) => _fixture = fixture;

    [Fact]
    public void Application_Launch_ShowsShellWithHomeAsDefault()
    {
        var shell = new ShellPage(_fixture.Context);
        shell.WaitLoaded(true);

        shell.AppTitleText.AssertVisible(true, "App title should be visible in sidebar");

        var homePage = new HomePage(_fixture.Context);
        homePage.WaitLoaded(true);
        Assert.True(homePage.IsLoaded(), "Home page should be displayed by default");
    }

    [Fact]
    public void Navigation_FromHomeToLogin_ShowsLoginPage()
    {
        var shell = new ShellPage(_fixture.Context);
        shell.WaitLoaded(true);

        var loginPage = shell.NavigateToLogin();

        Assert.True(loginPage.IsLoaded(), "Login page should be displayed after navigation");
        loginPage.LoginHeader.AssertVisible(true, "Login header should be visible");
    }

    [Fact]
    public void Navigation_FromLoginToHome_ShowsHomePage()
    {
        var shell = new ShellPage(_fixture.Context);
        shell.WaitLoaded(true);

        shell.NavigateToLogin();
        var homePage = shell.NavigateToHome();

        Assert.True(homePage.IsLoaded(), "Home page should be displayed after navigation");
        homePage.WelcomeText.AssertVisible(true, "Welcome text should be visible");
    }

    [Fact]
    public void Navigation_SidebarButtons_AreAllVisible()
    {
        var shell = new ShellPage(_fixture.Context);
        shell.WaitLoaded(true);

        shell.NavHomeButton.AssertVisible(true, "Home button should be visible");
        shell.NavLoginButton.AssertVisible(true, "Login button should be visible");
        shell.NavFormsButton.AssertVisible(true, "Forms button should be visible");
        shell.NavDataGridButton.AssertVisible(true, "DataGrid button should be visible");
    }

    [Fact]
    public void Navigation_MultipleTimes_WorksCorrectly()
    {
        var shell = new ShellPage(_fixture.Context);
        shell.WaitLoaded(true);

        for (int i = 0; i < 3; i++)
        {
            var loginPage = shell.NavigateToLogin();
            Assert.True(loginPage.IsLoaded(), $"Login page should display (iteration {i + 1})");

            var homePage = shell.NavigateToHome();
            Assert.True(homePage.IsLoaded(), $"Home page should display (iteration {i + 1})");
        }
    }
}
