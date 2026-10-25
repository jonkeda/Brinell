namespace Bruinell.WinForms.UITests.Fixtures;

/// <summary>
/// Shared fixture that launches the WinForms sample app once per test collection.
/// </summary>
public class WinFormsSampleFixture : WinFormsTestFixtureBase
{
    protected override string GetDefaultAppPath()
    {
        // From this assembly's folder, wherever the suite runs (AD-025).
        var testDir = Bruinell.Core.Composition.AssemblyFile.FolderOf(typeof(WinFormsSampleFixture).Assembly);
        var appPath = Path.GetFullPath(Path.Combine(
            testDir, "..", "..", "..", "..", "..",
            "samples", "Bruinell.Samples.WinForms.App", "bin",
            GetBuildConfiguration(), "net10.0-windows",
            "Bruinell.Samples.WinForms.App.exe"));

        if (!File.Exists(appPath))
        {
            throw new FileNotFoundException(
                $"Sample app not found at '{appPath}'. Build Bruinell.Samples.WinForms.App first.");
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
