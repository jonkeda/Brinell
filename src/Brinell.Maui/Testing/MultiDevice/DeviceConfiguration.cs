namespace Brinell.Maui.Testing.MultiDevice;

/// <summary>
/// Configuration for a device in multi-device testing.
/// </summary>
public class DeviceConfiguration
{
    /// <summary>
    /// Unique identifier for this device configuration.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the device.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Platform type (Android, iOS, Windows, Mac).
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Device name or UDID for real devices.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Platform/OS version.
    /// </summary>
    public string PlatformVersion { get; set; } = string.Empty;

    /// <summary>
    /// Appium server URL for this device.
    /// </summary>
    public string AppiumServerUrl { get; set; } = "http://localhost:4723";

    /// <summary>
    /// Path to the app package/IPA/MSIX.
    /// </summary>
    public string AppPath { get; set; } = string.Empty;

    /// <summary>
    /// App bundle ID or package name.
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is an emulator/simulator.
    /// </summary>
    public bool IsEmulator { get; set; } = true;

    /// <summary>
    /// Whether to run tests in parallel on this device.
    /// </summary>
    public bool RunInParallel { get; set; } = true;

    /// <summary>
    /// Additional Appium capabilities.
    /// </summary>
    public Dictionary<string, object> AdditionalCapabilities { get; set; } = new();

    /// <summary>
    /// Tags for filtering devices.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Create a default Android configuration.
    /// </summary>
    public static DeviceConfiguration Android(string name, string deviceName, string platformVersion = "14") => new()
    {
        Id = $"android-{name.ToLowerInvariant().Replace(" ", "-")}",
        Name = name,
        Platform = "Android",
        DeviceName = deviceName,
        PlatformVersion = platformVersion,
        Tags = ["android", "mobile"]
    };

    /// <summary>
    /// Create a default iOS configuration.
    /// </summary>
    public static DeviceConfiguration IOS(string name, string deviceName, string platformVersion = "17.0") => new()
    {
        Id = $"ios-{name.ToLowerInvariant().Replace(" ", "-")}",
        Name = name,
        Platform = "iOS",
        DeviceName = deviceName,
        PlatformVersion = platformVersion,
        Tags = ["ios", "mobile"]
    };

    /// <summary>
    /// Create a default Windows configuration.
    /// </summary>
    public static DeviceConfiguration Windows(string name = "Windows Desktop") => new()
    {
        Id = "windows-desktop",
        Name = name,
        Platform = "Windows",
        DeviceName = "WindowsPC",
        PlatformVersion = "10",
        Tags = ["windows", "desktop"]
    };

    /// <summary>
    /// Create a default Mac configuration.
    /// </summary>
    public static DeviceConfiguration Mac(string name = "Mac Desktop") => new()
    {
        Id = "mac-desktop",
        Name = name,
        Platform = "Mac",
        DeviceName = "Mac",
        PlatformVersion = "14",
        Tags = ["mac", "desktop"]
    };
}
