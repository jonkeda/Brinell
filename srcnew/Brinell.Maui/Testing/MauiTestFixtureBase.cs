using Brinell.Core.Configuration;
using Brinell.Core.Interfaces;
using Brinell.Core.Services;
using Brinell.Core.Testing;
using Brinell.Core.Artifacts;
using Brinell.Maui.Context;
using Brinell.Maui.Enums;

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
    /// Override to customize driver configuration.
    /// </summary>
    protected virtual MauiTestContextOptions CreateTestContextOptions()
    {
        var platform = Platform;

        var attachToRunning = ParseBool(Environment.GetEnvironmentVariable("APPIUM_ATTACH_TO_RUNNING"));
        var processName = Environment.GetEnvironmentVariable("APPIUM_PROCESS_NAME");
        var windowHandle = ParseWindowHandle(Environment.GetEnvironmentVariable("APPIUM_WINDOW_HANDLE"));

        var appPath = Environment.GetEnvironmentVariable("APPIUM_APP_PATH");
        if (string.IsNullOrWhiteSpace(appPath) && !attachToRunning)
        {
            appPath = GetDefaultAppPath(platform);
        }

        var mauiPlatform = platform.ToLowerInvariant() switch
        {
            "android" => MauiPlatform.Android,
            "ios" => MauiPlatform.iOS,
            "windows" => MauiPlatform.Windows,
            _ => throw new InvalidOperationException($"Unsupported platform: {platform}")
        };

        var driverOptions = new MauiDriverOptions
        {
            Platform = mauiPlatform,
            AppPath = appPath,
            ProcessName = attachToRunning ? processName : null,
            WindowHandle = attachToRunning ? windowHandle : null,
            Timeouts = new TimeoutSettings
            {
                DefaultWait = 5000,
                PageLoad = 10000,
                ElementFind = 3000,
                ElementState = 3000,
                Animation = 300,
                PollingInterval = 100
            }
        };
        
        // Configure platform-specific options
        switch (mauiPlatform)
        {
            case MauiPlatform.Android:
                ConfigureAndroidOptions(driverOptions);
                break;
            case MauiPlatform.iOS:
                ConfigureiOSOptions(driverOptions);
                break;
            // Windows uses FlaUI - no additional options needed
        }

        return new MauiTestContextOptions
        {
            DriverOptions = driverOptions,
            Timeouts = driverOptions.Timeouts
        };
    }
    
    /// <summary>
    /// Configures driver options for Android MAUI app testing.
    /// Override to customize Android capabilities.
    /// </summary>
    protected virtual void ConfigureAndroidOptions(MauiDriverOptions options)
    {
        var serverUri = Environment.GetEnvironmentVariable("APPIUM_SERVER_URI")
            ?? "http://127.0.0.1:4723";
        var deviceName = Environment.GetEnvironmentVariable("APPIUM_DEVICE_NAME")
            ?? "emulator-5554";
        
        options.AppiumServerUri = new Uri(serverUri);
        options.DeviceName = deviceName;
    }
    
    /// <summary>
    /// Configures driver options for iOS MAUI app testing.
    /// Override to customize iOS capabilities.
    /// </summary>
    protected virtual void ConfigureiOSOptions(MauiDriverOptions options)
    {
        var serverUri = Environment.GetEnvironmentVariable("APPIUM_SERVER_URI")
            ?? "http://127.0.0.1:4723";
        var deviceName = Environment.GetEnvironmentVariable("APPIUM_DEVICE_NAME")
            ?? "iPhone 15";
        var platformVersion = Environment.GetEnvironmentVariable("APPIUM_PLATFORM_VERSION")
            ?? "17.0";
        
        options.AppiumServerUri = new Uri(serverUri);
        options.DeviceName = deviceName;
        options.PlatformVersion = platformVersion;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Gets the screenshot output directory path.
    /// Override to customize the screenshot location.
    /// </summary>
    protected virtual string GetScreenshotDirectory()
    {
        var path = GetArtifactPathProvider().ScreenshotsDirectory;
        Directory.CreateDirectory(path);
        return path;
    }

    protected virtual ITestArtifactPathProvider GetArtifactPathProvider()
    {
        return DefaultTestArtifactPathProvider.Create(GetType().Assembly.GetName().Name);
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

    private static bool ParseBool(string? value)
    {
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static IntPtr? ParseWindowHandle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[2..];
        }

        if (long.TryParse(normalized, System.Globalization.NumberStyles.HexNumber, null, out var hexResult))
        {
            return new IntPtr(hexResult);
        }

        if (long.TryParse(normalized, out var decimalResult))
        {
            return new IntPtr(decimalResult);
        }

        return null;
    }

    #endregion
}
