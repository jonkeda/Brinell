using Brinell.Maui.Infrastructure;
using Xunit.Abstractions;

namespace Brinell.Samples.Maui.UITests;

/// <summary>
/// Base class for MAUI UI tests.
/// Manages Appium driver lifecycle.
/// </summary>
public abstract class MauiTestBase : IDisposable
{
    protected readonly AppiumTestContext Context;
    protected readonly ITestOutputHelper Output;

    // Update this path to match your built MAUI app location
    private static readonly string AppPath = GetAppPath();

    protected MauiTestBase(ITestOutputHelper output)
    {
        Output = output;

        var options = AppiumTestOptions.Windows(AppPath);
        options.ServerUrl = "http://127.0.0.1:4723";
        options.DefaultTimeoutMs = 10000;

        Context = AppiumTestContext.Create(options, Log);
        Context.TestName = GetType().Name;
    }

    private static string GetAppPath()
    {
        // Look for the built MAUI app
        var solutionDir = FindSolutionDirectory();
        var appPath = Path.Combine(solutionDir, 
            "samples", "Brinell.Samples.Maui.App", "bin", "Debug", 
            "net10.0-windows10.0.19041.0", "win-x64", "Brinell.Samples.Maui.App.exe");
        
        if (!File.Exists(appPath))
        {
            // Try alternate path without win-x64
            appPath = Path.Combine(solutionDir,
                "samples", "Brinell.Samples.Maui.App", "bin", "Debug",
                "net10.0-windows10.0.19041.0", "Brinell.Samples.Maui.App.exe");
        }

        return appPath;
    }

    private static string FindSolutionDirectory()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "Brinell.sln")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return Directory.GetCurrentDirectory();
    }

    protected void Log(string message)
    {
        Output.WriteLine(message);
    }

    public void Dispose()
    {
        Context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
