using Brinell.Html.Controls;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Samples.Blazor.UITests.PageObjects;

/// <summary>
/// Page object for the Blazor Dashboard page.
/// </summary>
public class DashboardPage : PageBase
{
    /// <summary>
    /// The dashboard title element.
    /// </summary>
    public LabelControl DashboardTitle { get; }

    /// <summary>
    /// Welcome alert message.
    /// </summary>
    public LabelControl WelcomeAlert { get; }

    /// <summary>
    /// Total users stat display.
    /// </summary>
    public LabelControl TotalUsers { get; }

    /// <summary>
    /// Active sessions stat display.
    /// </summary>
    public LabelControl ActiveSessions { get; }

    /// <summary>
    /// Tests passed stat display.
    /// </summary>
    public LabelControl TestsPassed { get; }

    /// <summary>
    /// Activity table element.
    /// </summary>
    public LabelControl ActivityTable { get; }

    /// <summary>
    /// Back to home button.
    /// </summary>
    public LinkControl BackHomeButton { get; }

    public DashboardPage(SeleniumTestContext context)
        : base(context)
    {
        DashboardTitle = new LabelControl(context, this, "#dashboard-title");
        WelcomeAlert = new LabelControl(context, this, "#welcome-alert");
        TotalUsers = new LabelControl(context, this, "#total-users");
        ActiveSessions = new LabelControl(context, this, "#active-sessions");
        TestsPassed = new LabelControl(context, this, "#tests-passed");
        ActivityTable = new LabelControl(context, this, "#activity-table");
        BackHomeButton = new LinkControl(context, this, "#back-home-btn");
    }

    /// <summary>
    /// CSS selector that identifies this page.
    /// </summary>
    public override string AutomationId => "#dashboard-title";

    /// <summary>
    /// Check if the dashboard page is displayed.
    /// </summary>
    public override bool IsDisplayed()
    {
        return DashboardTitle.IsVisible() && DashboardTitle.GetText() == "Dashboard";
    }

    /// <summary>
    /// Get the total users count.
    /// </summary>
    public string GetTotalUsers()
    {
        return TotalUsers.GetText();
    }

    /// <summary>
    /// Get the active sessions count.
    /// </summary>
    public string GetActiveSessions()
    {
        return ActiveSessions.GetText();
    }

    /// <summary>
    /// Get the tests passed percentage.
    /// </summary>
    public string GetTestsPassed()
    {
        return TestsPassed.GetText();
    }

    /// <summary>
    /// Navigate back to home page.
    /// </summary>
    public HomePage NavigateToHome()
    {
        Log("NavigateToHome()");
        BackHomeButton.Click();
        var homePage = new HomePage(_context);
        homePage.WaitForDisplayed();
        return homePage;
    }

    /// <summary>
    /// Check if welcome alert is displayed.
    /// </summary>
    public bool HasWelcomeAlert()
    {
        return WelcomeAlert.IsVisible();
    }

    /// <summary>
    /// Check if activity table is displayed.
    /// </summary>
    public bool HasActivityTable()
    {
        return ActivityTable.IsVisible();
    }
    
    /// <summary>
    /// Assert welcome alert is visible.
    /// </summary>
    public void AssertHasWelcomeAlert(string? message = null)
    {
        WelcomeAlert.AssertVisible(message ?? "Welcome alert should be visible");
    }
}
