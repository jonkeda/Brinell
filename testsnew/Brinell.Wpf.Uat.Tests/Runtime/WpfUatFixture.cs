namespace Brinell.Wpf.Uat.Tests.Runtime;

public sealed class WpfUatFixture : WpfSampleFixture
{
    public WpfUatFixture()
    {
        ShellPage = new ShellPage(Context);
        HomePage = new HomePage(Context);
        LoginPage = new LoginPage(Context);
    }

    public ShellPage ShellPage { get; }

    public HomePage HomePage { get; }

    public LoginPage LoginPage { get; }

    public void NavigateToHome()
    {
        ShellPage.NavigateToHome();
    }

    public void NavigateToLogin()
    {
        ShellPage.NavigateToLogin();
    }
}
