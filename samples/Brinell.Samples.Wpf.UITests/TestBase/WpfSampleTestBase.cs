using System.IO;
using Brinell.Wpf.Testing;
using Xunit.Abstractions;

namespace Brinell.Samples.Wpf.UITests.TestBase;

/// <summary>
/// Base class for Brinell WPF Sample application UI tests.
/// Configures the application path and provides common test infrastructure.
/// </summary>
public abstract class WpfSampleTestBase : WpfUITestBase
{
    protected WpfSampleTestBase(ITestOutputHelper output)
        : base(output.WriteLine)
    {
    }

    /// <summary>
    /// Gets the path to the Brinell.Samples.Wpf.App executable.
    /// </summary>
    protected override string ApplicationPath
    {
        get
        {
            // The app is built in the same output directory structure
            var testAssemblyDir = AppContext.BaseDirectory;
            
            // Navigate from test output to app output
            // From: samples/Brinell.Samples.Wpf.UITests/bin/Debug/net9.0-windows/
            // To:   samples/Brinell.Samples.Wpf.App/bin/Debug/net9.0-windows/
            var appPath = Path.GetFullPath(Path.Combine(
                testAssemblyDir,
                "..", "..", "..", "..",
                "Brinell.Samples.Wpf.App",
                "bin",
                GetBuildConfiguration(),
                "net9.0-windows",
                "Brinell.Samples.Wpf.App.exe"));
            
            if (!File.Exists(appPath))
            {
                throw new FileNotFoundException(
                    $"Application not found at '{appPath}'. " +
                    "Ensure Brinell.Samples.Wpf.App is built before running tests.");
            }
            
            return appPath;
        }
    }

    /// <summary>
    /// Gets the build configuration (Debug/Release) based on current assembly.
    /// </summary>
    private static string GetBuildConfiguration()
    {
#if DEBUG
        return "Debug";
#else
        return "Release";
#endif
    }
}
