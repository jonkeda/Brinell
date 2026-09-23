namespace Brinell.Stride.Uat.Tests.Runtime;

[TestModuleScan(typeof(MainPage), NamespacePrefix = "Brinell.Stride.UITests.PageObjects")]
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
            if (File.Exists(Path.Combine(dir, "Brinell.sln")))
            {
                return Path.Combine(
                    dir,
                    "samples",
                    "Brinell.Samples.Stride.App",
                    "bin",
                    "Debug",
                    "net10.0-windows",
                    "Brinell.Samples.Stride.App.exe");
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
            "Brinell.Samples.Stride.App",
            "bin",
            "Debug",
            "net10.0-windows",
            "Brinell.Samples.Stride.App.exe");
    }
}
