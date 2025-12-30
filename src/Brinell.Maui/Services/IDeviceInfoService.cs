namespace Brinell.Maui.Services;

/// <summary>
/// Interface for device information services.
/// Provides access to device-specific information during tests.
/// </summary>
public interface IDeviceInfoService
{
    /// <summary>
    /// Get the device platform (Android, iOS, Windows, MacCatalyst).
    /// </summary>
    string Platform { get; }

    /// <summary>
    /// Get the device model name.
    /// </summary>
    string Model { get; }

    /// <summary>
    /// Get the device manufacturer.
    /// </summary>
    string Manufacturer { get; }

    /// <summary>
    /// Get the OS version string.
    /// </summary>
    string OSVersion { get; }

    /// <summary>
    /// Get the device idiom (Phone, Tablet, Desktop, TV, Watch).
    /// </summary>
    string DeviceIdiom { get; }

    /// <summary>
    /// Get the screen width in pixels.
    /// </summary>
    int ScreenWidth { get; }

    /// <summary>
    /// Get the screen height in pixels.
    /// </summary>
    int ScreenHeight { get; }

    /// <summary>
    /// Get the screen density (DPI scale factor).
    /// </summary>
    double ScreenDensity { get; }

    /// <summary>
    /// Check if running on a physical device.
    /// </summary>
    bool IsPhysicalDevice { get; }

    /// <summary>
    /// Check if running on an emulator/simulator.
    /// </summary>
    bool IsEmulator { get; }

    /// <summary>
    /// Check if the device is in portrait orientation.
    /// </summary>
    bool IsPortrait { get; }

    /// <summary>
    /// Check if the device is in landscape orientation.
    /// </summary>
    bool IsLandscape { get; }
}
