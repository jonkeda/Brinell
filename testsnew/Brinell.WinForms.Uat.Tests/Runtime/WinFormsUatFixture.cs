namespace Brinell.WinForms.Uat.Tests.Runtime;

public sealed class WinFormsUatFixture : WinFormsSampleFixture
{
    public WinFormsUatFixture()
    {
        LoginPage = new Pages.LoginUatPage(Context);
    }

    public Pages.LoginUatPage LoginPage { get; }

    public void NavigateToLogin()
    {
        LoginPage.WaitLoaded(true);
    }
}
