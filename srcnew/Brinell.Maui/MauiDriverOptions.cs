using Brinell.Core.Logging;
using Brinell.Maui.Enums;

namespace Brinell.Maui;

/// <summary>
/// Configuration options for MAUI driver creation.
/// Works with both FlaUI (Windows) and Appium (Android/iOS) drivers.
/// </summary>
public class MauiDriverOptions
{
    /// <summary>
    /// Target platform. Determines driver type:
    /// - Windows: FlaUI (always)
    /// - Android/iOS: Appium
    /// </summary>
    public MauiPlatform Platform { get; set; } = MauiPlatform.Windows;
    
    /// <summary>
    /// Path to application executable or package.
    /// - Windows: Path to .exe
    /// - Android: Path to .apk
    /// - iOS: Path to .app or bundle ID
    /// </summary>
    public string? AppPath { get; set; }
    
    /// <summary>
    /// Process name to attach to (alternative to AppPath).
    /// Windows FlaUI only. Attaches to running process.
    /// </summary>
    public string? ProcessName { get; set; }
    
    /// <summary>
    /// Window handle to attach to (alternative to AppPath).
    /// Windows FlaUI only.
    /// </summary>
    public IntPtr? WindowHandle { get; set; }
    
    /// <summary>
    /// Appium server URI. Required for Appium driver (Android/iOS).
    /// Default: http://127.0.0.1:4723
    /// </summary>
    public Uri AppiumServerUri { get; set; } = new Uri("http://127.0.0.1:4723");
    
    /// <summary>
    /// Device name for Android/iOS.
    /// </summary>
    public string? DeviceName { get; set; }
    
    /// <summary>
    /// Platform version for iOS.
    /// </summary>
    public string? PlatformVersion { get; set; }
    
    /// <summary>
    /// Additional Appium capabilities.
    /// </summary>
    public Dictionary<string, object> AdditionalCapabilities { get; } = new();
    
    /// <summary>
    /// Timeout settings for waits and polling.
    /// </summary>
    public TimeoutSettings? Timeouts { get; set; }
    
    /// <summary>
    /// Logger for driver operations.
    /// </summary>
    public ITestLogger? Logger { get; set; }
    
    /// <summary>
    /// Creates options from environment variables.
    /// </summary>
    /// <remarks>
    /// Reads the following environment variables:
    /// - APPIUM_PLATFORM: "windows", "android", or "ios"
    /// - APPIUM_APP_PATH: Path to app executable/package
    /// - APPIUM_PROCESS_NAME: Process name for attach mode on Windows
    /// - APPIUM_WINDOW_HANDLE: Window handle (hex or decimal) for attach mode on Windows
    /// - APPIUM_DEVICE_NAME: Device name for Android/iOS
    /// - APPIUM_PLATFORM_VERSION: Platform version for iOS
    /// - APPIUM_SERVER_URI: Appium server URL
    /// </remarks>
    public static MauiDriverOptions FromEnvironment()
    {
        var platform = Environment.GetEnvironmentVariable("APPIUM_PLATFORM")?.ToLowerInvariant() switch
        {
            "android" => MauiPlatform.Android,
            "ios" => MauiPlatform.iOS,
            "windows" or _ => MauiPlatform.Windows
        };
        
        var windowHandle = ParseWindowHandle(Environment.GetEnvironmentVariable("APPIUM_WINDOW_HANDLE"));

        return new MauiDriverOptions
        {
            Platform = platform,
            AppPath = Environment.GetEnvironmentVariable("APPIUM_APP_PATH"),
            ProcessName = Environment.GetEnvironmentVariable("APPIUM_PROCESS_NAME"),
            WindowHandle = windowHandle,
            DeviceName = Environment.GetEnvironmentVariable("APPIUM_DEVICE_NAME"),
            PlatformVersion = Environment.GetEnvironmentVariable("APPIUM_PLATFORM_VERSION"),
            AppiumServerUri = new Uri(Environment.GetEnvironmentVariable("APPIUM_SERVER_URI") ?? "http://127.0.0.1:4723")
        };
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
}
