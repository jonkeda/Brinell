using Brinell.Core.Configuration;
using Brinell.Core.Interfaces;
using Brinell.Core.Services;
using Brinell.Core.Testing;
using Brinell.Maui.Context;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.Testing;

/// <summary>
/// Base fixture for MAUI UI tests that manages the Appium driver and test context lifecycle.
/// Inherit from this class in your test project and implement <see cref="GetDefaultAppPath"/>.
/// </summary>
/// <remarks>
/// Configuration via environment variables:
/// - APPIUM_SERVER_URI: Appium server URL (default: http://127.0.0.1:4723)
/// - APPIUM_PLATFORM: "windows", "android", or "ios" (default: windows)
/// - APPIUM_APP_PATH: Path to the app executable/package
/// - APPIUM_DEVICE_NAME: Device/emulator name (Android/iOS only)
/// - APPIUM_PLATFORM_VERSION: Platform version (iOS only)
/// </remarks>
public abstract class MauiTestFixtureBase : IDisposable
{
    private static int _instanceCount;
    private readonly int _instanceId;
    private readonly MauiTestContext _context;
    private readonly IScreenshotService _screenshotService;
    private bool _disposed;

    /// <summary>
    /// Initializes the fixture by creating the test context and screenshot service.
    /// </summary>
    protected MauiTestFixtureBase()
    {
        _instanceId = Interlocked.Increment(ref _instanceCount);
        Console.WriteLine($"[FIXTURE] {GetType().Name} #{_instanceId} CREATING at {DateTime.Now:HH:mm:ss.fff}");
        
        var options = CreateTestContextOptions();
        _context = new MauiTestContext(options);
        
        Console.WriteLine($"[FIXTURE] {GetType().Name} #{_instanceId} CREATED - Driver session started");
        
        // Initialize screenshot service
        var screenshotSettings = new ScreenshotSettings
        {
            OutputDirectory = GetScreenshotDirectory(),
            CaptureOnFailure = true,
            IncludeTimestamp = true,
            Format = ScreenshotFormat.Png
        };
        _screenshotService = new ScreenshotService(_context, _context.Logger, screenshotSettings);
        ScreenshotTestAttribute.SetService(_screenshotService);
    }

    /// <summary>
    /// Gets the MAUI test context.
    /// </summary>
    public MauiTestContext Context => _context;
    
    /// <summary>
    /// Gets the screenshot service.
    /// </summary>
    public IScreenshotService ScreenshotService => _screenshotService;

    /// <summary>
    /// Gets the current platform from environment variable or default.
    /// </summary>
    protected string Platform => Environment.GetEnvironmentVariable("APPIUM_PLATFORM") ?? "windows";

    #region Abstract Methods

    /// <summary>
    /// Gets the default app path based on platform. Must be implemented by derived classes.
    /// </summary>
    /// <param name="platform">The platform: "windows", "android", or "ios".</param>
    /// <returns>The path to the app executable or package.</returns>
    protected abstract string GetDefaultAppPath(string platform);

    #endregion

    #region Virtual Configuration Methods

    /// <summary>
    /// Creates test context options with platform-specific capabilities.
    /// Override to customize Appium configuration.
    /// </summary>
    protected virtual MauiTestContextOptions CreateTestContextOptions()
    {
        var serverUri = Environment.GetEnvironmentVariable("APPIUM_SERVER_URI")
            ?? "http://127.0.0.1:4723";

        var platform = Platform;

        var appPath = Environment.GetEnvironmentVariable("APPIUM_APP_PATH")
            ?? GetDefaultAppPath(platform);

        var appiumOptions = new AppiumOptions();
        
        switch (platform.ToLowerInvariant())
        {
            case "windows":
                ConfigureWindowsOptions(appiumOptions, appPath);
                break;
            case "android":
                ConfigureAndroidOptions(appiumOptions, appPath);
                break;
            case "ios":
                ConfigureiOSOptions(appiumOptions, appPath);
                break;
            default:
                throw new InvalidOperationException($"Unsupported platform: {platform}");
        }

        return new MauiTestContextOptions
        {
            AppiumServerUri = new Uri(serverUri),
            AppiumOptions = appiumOptions
        };
    }
    
    /// <summary>
    /// Configures AppiumOptions for Windows MAUI app testing.
    /// Override to add custom Windows capabilities.
    /// </summary>
    protected virtual void ConfigureWindowsOptions(AppiumOptions options, string appPath)
    {
        options.PlatformName = "Windows";
        options.AutomationName = "Windows";
        options.App = appPath;
    }
    
    /// <summary>
    /// Configures AppiumOptions for Android MAUI app testing.
    /// Override to customize Android capabilities like appPackage/appActivity.
    /// </summary>
    protected virtual void ConfigureAndroidOptions(AppiumOptions options, string appPath)
    {
        var deviceName = Environment.GetEnvironmentVariable("APPIUM_DEVICE_NAME")
            ?? "emulator-5554";
        
        options.PlatformName = "Android";
        options.AutomationName = "UiAutomator2";
        options.DeviceName = deviceName;
        options.App = appPath;
    }
    
    /// <summary>
    /// Configures AppiumOptions for iOS MAUI app testing.
    /// Override to customize iOS capabilities like bundleId.
    /// </summary>
    protected virtual void ConfigureiOSOptions(AppiumOptions options, string appPath)
    {
        var deviceName = Environment.GetEnvironmentVariable("APPIUM_DEVICE_NAME")
            ?? "iPhone 15";
        var platformVersion = Environment.GetEnvironmentVariable("APPIUM_PLATFORM_VERSION")
            ?? "17.0";
        
        options.PlatformName = "iOS";
        options.AutomationName = "XCUITest";
        options.DeviceName = deviceName;
        options.PlatformVersion = platformVersion;
        options.App = appPath;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Gets the screenshot output directory path.
    /// Override to customize the screenshot location.
    /// </summary>
    protected virtual string GetScreenshotDirectory()
    {
        var solutionDir = FindSolutionDirectory();
        var path = Path.Combine(solutionDir, "TestResults", "Screenshots");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }
    
    /// <summary>
    /// Finds the solution root directory by searching for *.sln files.
    /// </summary>
    protected static string FindSolutionDirectory()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0)
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return Directory.GetCurrentDirectory();
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed and unmanaged resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        Console.WriteLine($"[FIXTURE] {GetType().Name} #{_instanceId} DISPOSING at {DateTime.Now:HH:mm:ss.fff}");

        if (disposing)
        {
            _context?.Dispose();
        }

        Console.WriteLine($"[FIXTURE] {GetType().Name} #{_instanceId} DISPOSED");
        _disposed = true;
    }

    #endregion
}
