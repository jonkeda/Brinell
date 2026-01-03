using Brinell.FlaUI;
using Brinell.FlaUI.Controls.Base;
using Brinell.Wpf.Controls;

namespace Brinell.Samples.Wpf.UITests.PageObjects;

/// <summary>
/// Page object for the main shell window with navigation sidebar.
/// </summary>
public class ShellPage : PageBase
{
    /// <summary>
    /// The app title text in the sidebar.
    /// </summary>
    public LabelControl AppTitleText { get; }
    
    /// <summary>
    /// Navigation button to Home page.
    /// </summary>
    public ButtonControl NavHomeButton { get; }
    
    /// <summary>
    /// Navigation button to Login page.
    /// </summary>
    public ButtonControl NavLoginButton { get; }
    
    /// <summary>
    /// Navigation button to Forms page.
    /// </summary>
    public ButtonControl NavFormsButton { get; }
    
    /// <summary>
    /// Navigation button to DataGrid page.
    /// </summary>
    public ButtonControl NavDataGridButton { get; }

    public ShellPage(FlaUITestContext context) 
        : base(context, "ShellWindow")
    {
        AppTitleText = new LabelControl(context, this, "AppTitleText");
        NavHomeButton = new ButtonControl(context, this, "NavHomeButton");
        NavLoginButton = new ButtonControl(context, this, "NavLoginButton");
        NavFormsButton = new ButtonControl(context, this, "NavFormsButton");
        NavDataGridButton = new ButtonControl(context, this, "NavDataGridButton");
    }

    /// <summary>
    /// Check if the shell window is displayed.
    /// </summary>
    public override bool IsDisplayed()
    {
        // For the main shell window, check if the main window exists
        return FlaContext.MainWindow != null && !FlaContext.MainWindow.IsOffscreen;
    }

    /// <summary>
    /// Navigate to the Home page.
    /// </summary>
    public HomePage NavigateToHome()
    {
        Log("NavigateToHome()");
        NavHomeButton.Click();
        var homePage = new HomePage(FlaContext);
        homePage.WaitForDisplayed();
        return homePage;
    }

    /// <summary>
    /// Navigate to the Login page.
    /// </summary>
    public LoginPage NavigateToLogin()
    {
        Log("NavigateToLogin()");
        NavLoginButton.Click();
        var loginPage = new LoginPage(FlaContext);
        loginPage.WaitForDisplayed();
        return loginPage;
    }
}
