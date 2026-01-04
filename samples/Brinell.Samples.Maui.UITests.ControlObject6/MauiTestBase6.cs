using Brinell.Maui.ControlObject6.Context;
using Brinell.Maui.Infrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using Xunit.Abstractions;

namespace Brinell.Samples.Maui.UITests.ControlObject6;

/// <summary>
/// Base class for MAUI UI tests using ControlObject6 API.
/// Manages Appium driver lifecycle and provides MauiTestContext.
/// </summary>
public abstract class MauiTestBase6 : IDisposable
{
    protected readonly MauiTestContext Context;
    protected readonly ITestOutputHelper Output;

    private readonly AppiumDriver _driver;
    private bool _disposed;

    // Update this path to match your built MAUI app location
    private static readonly string AppPath = GetAppPath();

    protected MauiTestBase6(ITestOutputHelper output)
    {
        Output = output;

        _driver = CreateDriver();
        
        // Poll-wait for app to be ready (instead of fixed 10s wait)
        WaitForAppReady(_driver, timeoutMs: 10000, pollingMs: 100);
        
        Context = new MauiTestContext(_driver);
        Context.DefaultTimeoutMs = 10000;
        
        Log($"MauiTestBase6 initialized for {GetType().Name}");
    }

    /// <summary>
    /// Poll-waits for the app to be ready by checking if we can find any element.
    /// Returns as soon as the app is responsive - no fixed delays.
    /// </summary>
    private void WaitForAppReady(AppiumDriver driver, int timeoutMs, int pollingMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        Exception? lastException = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                // Try to get window handles - this works when app is ready
                var handles = driver.WindowHandles;
                if (handles.Count > 0)
                {
                    Log($"App ready - found {handles.Count} window handle(s)");
                    return;
                }
            }
            catch (WebDriverException ex)
            {
                lastException = ex;
                // App not ready yet, keep polling
            }

            Thread.Sleep(pollingMs);
        }

        throw new TimeoutException(
            $"App did not become ready within {timeoutMs}ms", lastException);
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

    private AppiumDriver CreateDriver()
    {
        Log($"Creating Windows driver for: {AppPath}");

        if (!File.Exists(AppPath))
        {
            throw new FileNotFoundException(
                $"MAUI app not found at: {AppPath}. Build the app first.");
        }

        var options = new AppiumOptions
        {
            AutomationName = "Windows",
            PlatformName = "Windows",
            App = AppPath
        };

        // Use minimal wait - we poll-wait ourselves after driver creation
        // This avoids the 10-second fixed delay of ms:waitForAppLaunch
        options.AddAdditionalAppiumOption("ms:waitForAppLaunch", "1");
        options.AddAdditionalAppiumOption("ms:experimental-webdriver", true);
        
        // Force quit app when session ends - prevents orphaned processes
        options.AddAdditionalAppiumOption("ms:forcequit", true);

        var serverUrl = Environment.GetEnvironmentVariable("APPIUM_SERVER_URL")
            ?? "http://127.0.0.1:4723";

        return new WindowsDriver(new Uri(serverUrl), options);
    }

    protected void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        Output.WriteLine($"[{timestamp}] {message}");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            try
            {
                _driver?.Quit();
            }
            catch (Exception ex)
            {
                Log($"Error disposing driver: {ex.Message}");
            }
        }

        _disposed = true;
    }
}
