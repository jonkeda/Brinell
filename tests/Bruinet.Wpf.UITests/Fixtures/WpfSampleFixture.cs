namespace Bruinet.Wpf.UITests.Fixtures;

/// <summary>
/// Shared fixture that launches the WPF sample app once per test collection.
/// </summary>
public class WpfSampleFixture : WpfTestFixtureBase
{
    protected override string GetDefaultAppPath()
    {
        // From this assembly's folder to the app's output, wherever the suite runs (AD-025).
        var testDir = Bruinet.Core.Composition.AssemblyFile.FolderOf(typeof(WpfSampleFixture).Assembly);
        var appPath = Path.GetFullPath(Path.Combine(
            testDir, "..", "..", "..", "..", "..",
            "samples", "Bruinet.Samples.Wpf.App", "bin",
            GetBuildConfiguration(), "net10.0-windows",
            "Bruinet.Samples.Wpf.App.exe"));

        if (!File.Exists(appPath))
        {
            throw new FileNotFoundException(
                $"Sample app not found at '{appPath}'. Build Bruinet.Samples.Wpf.App first.");
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
/// xUnit collection definition — all UI tests share one WPF app instance.
/// </summary>
[CollectionDefinition("WPF UITests", DisableParallelization = true)]
public class WpfUITestCollection : ICollectionFixture<WpfSampleFixture>
{
}
