namespace Brinell.Stride.Uat.Tests.Runtime;

public sealed class StrideUatFixture : StrideTestFixtureBase
{
    public StrideUatFixture()
    {
        InitializeAsync().GetAwaiter().GetResult();
        MainPage = new MainPage(Context);
    }

    [UatName("Main")]
    public MainPage MainPage { get; }

    public void NavigateToMain()
    {
        MainPage.AssertLoaded(true, timeoutMs: 30000);
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
