using Brinell.Html.Controls;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Samples.Blazor.UITests.PageObjects;

/// <summary>
/// Page object for the Blazor Home/Index page.
/// </summary>
public class HomePage : PageBase
{
    /// <summary>
    /// The page title element.
    /// </summary>
    public LabelControl PageTitle { get; }

    /// <summary>
    /// The welcome message element.
    /// </summary>
    public LabelControl WelcomeMessage { get; }

    /// <summary>
    /// Link to the Counter page.
    /// </summary>
    public LinkControl CounterLink { get; }

    /// <summary>
    /// Link to the Login page.
    /// </summary>
    public LinkControl LoginLink { get; }

    /// <summary>
    /// Link to the Dashboard page.
    /// </summary>
    public LinkControl DashboardLink { get; }

    public HomePage(SeleniumTestContext context)
        : base(context)
    {
        PageTitle = new LabelControl(context, this, "#page-title");
        WelcomeMessage = new LabelControl(context, this, "#welcome-message");
        CounterLink = new LinkControl(context, this, "#link-counter");
        LoginLink = new LinkControl(context, this, "#link-login");
        DashboardLink = new LinkControl(context, this, "#link-dashboard");
    }

    /// <summary>
    /// CSS selector that identifies this page.
    /// </summary>
    public override string AutomationId => "#page-title";

    /// <summary>
    /// Check if the home page is displayed.
    /// </summary>
    public override bool IsDisplayed()
    {
        return PageTitle.IsVisible() && PageTitle.GetText().Contains("Welcome");
    }

    /// <summary>
    /// Navigate to the Counter page.
    /// </summary>
    public CounterPage NavigateToCounter()
    {
        Log("NavigateToCounter()");
        CounterLink.Click();
        var counterPage = new CounterPage(_context);
        counterPage.WaitForDisplayed();
        return counterPage;
    }

    /// <summary>
    /// Navigate to the Login page.
    /// </summary>
    public LoginPage NavigateToLogin()
    {
        Log("NavigateToLogin()");
        LoginLink.Click();
        var loginPage = new LoginPage(_context);
        loginPage.WaitForDisplayed();
        return loginPage;
    }

    /// <summary>
    /// Navigate to the Dashboard page.
    /// </summary>
    public DashboardPage NavigateToDashboard()
    {
        Log("NavigateToDashboard()");
        DashboardLink.Click();
        var dashboardPage = new DashboardPage(_context);
        dashboardPage.WaitForDisplayed();
        return dashboardPage;
    }
}
