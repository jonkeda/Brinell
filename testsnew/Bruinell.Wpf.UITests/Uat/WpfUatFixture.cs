namespace Bruinell.Wpf.UITests.Uat;

[TestModuleScan(typeof(ShellPage), NamespacePrefix = "Bruinell.Wpf.UITests.PageObjects")]
public sealed class WpfUatFixture : WpfSampleFixture
{
    public WpfUatFixture()
    {
        Composition = TestComposition.ForFixture(this, services =>
            services.AddSingleton<IWpfTestContext>(Context));
    }

    public TestComposition Composition { get; }

    public void NavigateToHome()
    {
        using var scope = Composition.CreateScope();
        scope.ServiceProvider.GetRequiredService<ShellPage>().NavigateToHome();
    }

    public void NavigateToLogin()
    {
        using var scope = Composition.CreateScope();
        scope.ServiceProvider.GetRequiredService<ShellPage>().NavigateToLogin();
    }
}
