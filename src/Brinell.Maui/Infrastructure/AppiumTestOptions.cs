namespace Brinell.Maui.Infrastructure;

/// <summary>
/// Options for creating an Appium test context.
/// </summary>
public class AppiumTestOptions
{
    /// <summary>
    /// Appium server URL.
    /// </summary>
    public string ServerUrl { get; set; } = "http://localhost:4723";

    /// <summary>
    /// Platform name (Windows, Android, iOS).
    /// </summary>
    public string PlatformName { get; set; } = "Windows";

    /// <summary>
    /// Automation name (Windows, UiAutomator2, XCUITest).
    /// </summary>
    public string AutomationName { get; set; } = "Windows";

    /// <summary>
    /// Device name or UDID.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Platform/OS version.
    /// </summary>
    public string PlatformVersion { get; set; } = string.Empty;

    /// <summary>
    /// Path to the app package.
    /// </summary>
    public string AppPath { get; set; } = string.Empty;

    /// <summary>
    /// App bundle ID or package name.
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// Command timeout.
    /// </summary>
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Default wait timeout in milliseconds.
    /// </summary>
    public int DefaultTimeoutMs { get; set; } = 15000;

    /// <summary>
    /// Additional Appium capabilities.
    /// </summary>
    public Dictionary<string, object> AdditionalCapabilities { get; set; } = new();

    /// <summary>
    /// Create options for Windows MAUI testing.
    /// </summary>
    public static AppiumTestOptions Windows(string appPath) => new()
    {
        PlatformName = "Windows",
        AutomationName = "Windows",
        AppPath = appPath
    };

    /// <summary>
    /// Create options for Android testing.
    /// </summary>
    public static AppiumTestOptions Android(string appPath, string deviceName = "Pixel_7_API_34") => new()
    {
        PlatformName = "Android",
        AutomationName = "UiAutomator2",
        AppPath = appPath,
        DeviceName = deviceName
    };

    /// <summary>
    /// Create options for iOS testing.
    /// </summary>
    public static AppiumTestOptions iOS(string appPath, string deviceName = "iPhone 15", string platformVersion = "17.0") => new()
    {
        PlatformName = "iOS",
        AutomationName = "XCUITest",
        AppPath = appPath,
        DeviceName = deviceName,
        PlatformVersion = platformVersion
    };
}
