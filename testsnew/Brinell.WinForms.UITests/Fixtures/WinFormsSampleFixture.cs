namespace Brinell.WinForms.UITests.Fixtures;

/// <summary>
/// Shared fixture that launches the WinForms sample app once per test collection.
/// </summary>
public class WinFormsSampleFixture : WinFormsTestFixtureBase
{
    protected override string GetDefaultAppPath()
    {
        var testDir = AppContext.BaseDirectory;
        var appPath = Path.GetFullPath(Path.Combine(
            testDir, "..", "..", "..", "..", "..",
            "samples", "Brinell.Samples.WinForms.App", "bin",
            GetBuildConfiguration(), "net10.0-windows",
            "Brinell.Samples.WinForms.App.exe"));

        if (!File.Exists(appPath))
        {
            throw new FileNotFoundException(
                $"Sample app not found at '{appPath}'. Build Brinell.Samples.WinForms.App first.");
        }

        return appPath;
    }

    private static string GetBuildConfiguration()
    {
#if DEBUG
        return "Debug";
#else
        return "Release";
#endif
    }
}

/// <summary>
/// xUnit collection definition — all UI tests share one WinForms app instance.
/// </summary>
[CollectionDefinition("WinForms UITests", DisableParallelization = true)]
public class WinFormsUITestCollection : ICollectionFixture<WinFormsSampleFixture>
{
}
