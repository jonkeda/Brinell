using Brinell.Core.Configuration;
using Brinell.Core.Interfaces;
using Brinell.Core.Services;
using Brinell.Core.Testing;
using Brinell.Maui.UITests.Pages;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.UITests;

/// <summary>
/// Test fixture that manages the Appium driver and test context lifecycle.
/// Shared across test classes using xUnit's IClassFixture pattern.
/// </summary>
/// <remarks>
/// Configuration:
/// - Set APPIUM_SERVER_URI environment variable (default: http://127.0.0.1:4723)
/// - Set APPIUM_PLATFORM environment variable: "windows", "android", or "ios" (default: windows)
/// - Set APPIUM_APP_PATH for Windows: path to the .exe file
/// - For Android/iOS: configure device name and app package/bundle ID
/// </remarks>
public class AppiumFixture : IDisposable
{
    private readonly MauiTestContext _context;
    private readonly MainPage _mainPage;
    private readonly IScreenshotService _screenshotService;
    private bool _disposed;

    public AppiumFixture()
    {
        var options = CreateTestContextOptions();
        _context = new MauiTestContext(options);
        _mainPage = new MainPage(_context);
        
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
    /// Gets the MainPage page object.
    /// </summary>
    public MainPage MainPage => _mainPage;
    
    /// <summary>
    /// Gets the screenshot service.
    /// </summary>
    public IScreenshotService ScreenshotService => _screenshotService;
    
    /// <summary>
    /// Gets the screenshot output directory path.
    /// </summary>
    private static string GetScreenshotDirectory()
    {
        var solutionDir = FindSolutionDirectory();
        var path = Path.Combine(solutionDir, "TestResults", "Screenshots");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Creates test context options with platform-specific capabilities.
    /// </summary>
    /// <remarks>
    /// Override via environment variables:
    /// - APPIUM_SERVER_URI: Appium server URL (default: http://127.0.0.1:4723)
    /// - APPIUM_PLATFORM: "windows", "android", or "ios" (default: windows)
    /// - APPIUM_DEVICE_NAME: Device/emulator name (Android/iOS only)
    /// - APPIUM_APP_PATH: Path to the app executable/package
    /// </remarks>
    private static MauiTestContextOptions CreateTestContextOptions()
    {
        var serverUri = Environment.GetEnvironmentVariable("APPIUM_SERVER_URI")
            ?? "http://127.0.0.1:4723";

        var platform = Environment.GetEnvironmentVariable("APPIUM_PLATFORM")
            ?? "windows";

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
    /// </summary>
    private static void ConfigureWindowsOptions(AppiumOptions options, string appPath)
    {
        options.PlatformName = "Windows";
        options.AutomationName = "Windows";
        options.App = appPath;
    }
    
    /// <summary>
    /// Configures AppiumOptions for Android MAUI app testing.
    /// </summary>
    private static void ConfigureAndroidOptions(AppiumOptions options, string appPath)
    {
        var deviceName = Environment.GetEnvironmentVariable("APPIUM_DEVICE_NAME")
            ?? "emulator-5554";
        
        options.PlatformName = "Android";
        options.AutomationName = "UiAutomator2";
        options.DeviceName = deviceName;
        options.App = appPath;
        options.AddAdditionalAppiumOption("appPackage", "com.brinell.samples.maui");
        options.AddAdditionalAppiumOption("appActivity", "crc64hash.MainActivity");
    }
    
    /// <summary>
    /// Configures AppiumOptions for iOS MAUI app testing.
    /// </summary>
    private static void ConfigureiOSOptions(AppiumOptions options, string appPath)
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
        options.AddAdditionalAppiumOption("bundleId", "com.brinell.samples.maui");
    }

    /// <summary>
    /// Gets the default app path based on platform.
    /// </summary>
    private static string GetDefaultAppPath(string platform)
    {
        var solutionDir = FindSolutionDirectory();
        
        return platform.ToLowerInvariant() switch
        {
            "windows" => Path.Combine(solutionDir, 
                "samples", "Brinell.Samples.Maui.App", "bin", "Debug", 
                "net10.0-windows10.0.19041.0", "win-x64", "Brinell.Samples.Maui.App.exe"),
            "android" => Path.Combine(solutionDir,
                "samples", "Brinell.Samples.Maui.App", "bin", "Debug",
                "net10.0-android", "com.brinell.samples.maui-Signed.apk"),
            "ios" => Path.Combine(solutionDir,
                "samples", "Brinell.Samples.Maui.App", "bin", "Debug",
                "net10.0-ios", "iossimulator-x64", "Brinell.Samples.Maui.App.app"),
            _ => ""
        };
    }
    
    /// <summary>
    /// Finds the solution root directory by searching for Brinell.sln.
    /// </summary>
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
            _context?.Dispose();
        }

        _disposed = true;
    }
}
