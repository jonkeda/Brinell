namespace Brinell.Wpf.UITests.PageObjects;

/// <summary>
/// Page object for the main shell window with navigation sidebar.
/// Demonstrates CRTP page object pattern with factory methods.
/// </summary>
public class ShellPage : PageObjectBase<ShellPage>
{
    public Label<ShellPage> AppTitleText => Label("AppTitleText");
    public Button<ShellPage> NavHomeButton => Button("NavHomeButton");
    public Button<ShellPage> NavLoginButton => Button("NavLoginButton");
    public Button<ShellPage> NavFormsButton => Button("NavFormsButton");
    public Button<ShellPage> NavDataGridButton => Button("NavDataGridButton");

    public ShellPage(IWpfTestContext context) : base(context) { }

    /// <summary>Navigate to the Home page via sidebar button.</summary>
    public HomePage NavigateToHome()
    {
        NavHomeButton.Click();
        var page = new HomePage(Context);
        page.WaitLoaded(true);
        return page;
    }

    /// <summary>Navigate to the Login page via sidebar button.</summary>
    public LoginPage NavigateToLogin()
    {
        NavLoginButton.Click();
        var page = new LoginPage(Context);
        page.WaitLoaded(true);
        return page;
    }
}
