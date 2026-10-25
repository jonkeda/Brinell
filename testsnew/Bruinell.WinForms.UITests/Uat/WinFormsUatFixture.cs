namespace Bruinell.WinForms.UITests.Uat;

[TestModuleScan(typeof(Pages.LoginUatPage), NamespacePrefix = "Bruinell.WinForms.UITests.Uat.Pages")]
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
