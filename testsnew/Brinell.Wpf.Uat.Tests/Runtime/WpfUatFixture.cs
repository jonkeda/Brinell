namespace Brinell.Wpf.Uat.Tests.Runtime;

[TestModuleScan(typeof(ShellPage), NamespacePrefix = "Brinell.Wpf.UITests.PageObjects")]
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
