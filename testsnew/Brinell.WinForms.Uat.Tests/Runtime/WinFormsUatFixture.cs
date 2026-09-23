namespace Brinell.WinForms.Uat.Tests.Runtime;

[TestModuleScan(typeof(Pages.LoginUatPage), NamespacePrefix = "Brinell.WinForms.Uat.Tests.Pages")]
public sealed class WinFormsUatFixture : WinFormsSampleFixture
{
    public WinFormsUatFixture()
    {
        Composition = TestComposition.ForFixture(this, services =>
            services.AddSingleton<IWinFormsTestContext>(Context));
    }

    public TestComposition Composition { get; }

    public void NavigateToLogin()
    {
    }
}
