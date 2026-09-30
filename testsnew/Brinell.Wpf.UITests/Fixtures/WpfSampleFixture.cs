namespace Brinell.Wpf.UITests.Fixtures;

/// <summary>
/// Shared fixture that launches the WPF sample app once per test collection.
/// </summary>
public class WpfSampleFixture : WpfTestFixtureBase
{
    protected override string GetDefaultAppPath()
    {
        // From this assembly's folder to the app's output, not the host's: Presenter runs the suite in its own process (AD-025).
        var testDir = Brinell.Core.Composition.AssemblyFile.FolderOf(typeof(WpfSampleFixture).Assembly);
        var appPath = Path.GetFullPath(Path.Combine(
            testDir, "..", "..", "..", "..", "..",
            "samples", "Brinell.Samples.Wpf.App", "bin",
            GetBuildConfiguration(), "net10.0-windows",
            "Brinell.Samples.Wpf.App.exe"));

        if (!File.Exists(appPath))
        {
            throw new FileNotFoundException(
                $"Sample app not found at '{appPath}'. Build Brinell.Samples.Wpf.App first.");
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
