namespace Bruinet.Stride.UITests.Uat;

[TestModuleScan(typeof(MainPage), NamespacePrefix = "Bruinet.Stride.UITests.PageObjects")]
public sealed class StrideUatFixture : StrideTestFixtureBase
{
    public StrideUatFixture()
    {
        InitializeAsync().GetAwaiter().GetResult();
        Composition = TestComposition.ForFixture(this, services =>
            services.AddSingleton<IStrideTestContext>(Context));
    }

    public TestComposition Composition { get; }

    public void NavigateToMain()
    {
    }

    protected override string GetDefaultAppPath()
    {
        var dir = Directory.GetCurrentDirectory();
        while (!string.IsNullOrWhiteSpace(dir))
        {
            if (File.Exists(Path.Combine(dir, "Bruinet.sln")))
            {
                return Path.Combine(
                    dir,
                    "samples",
                    "Bruinet.Samples.Stride.App",
                    "bin",
                    "Debug",
                    "net10.0-windows",
                    "Bruinet.Samples.Stride.App.exe");
            }

            var parent = Directory.GetParent(dir);
            if (parent is null)
            {
                break;
            }

            dir = parent.FullName;
        }

        return Path.Combine(
            Directory.GetCurrentDirectory(),
            "samples",
            "Bruinet.Samples.Stride.App",
            "bin",
            "Debug",
            "net10.0-windows",
            "Bruinet.Samples.Stride.App.exe");
    }
}
