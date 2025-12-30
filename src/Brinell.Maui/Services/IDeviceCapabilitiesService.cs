namespace Brinell.Maui.Services;

/// <summary>
/// Interface for device capability services.
/// Provides information about device features and capabilities.
/// </summary>
public interface IDeviceCapabilitiesService
{
    /// <summary>
    /// Check if the device has a camera.
    /// </summary>
    bool HasCamera { get; }

    /// <summary>
    /// Check if the device has a front camera.
    /// </summary>
    bool HasFrontCamera { get; }

    /// <summary>
    /// Check if the device supports GPS.
    /// </summary>
    bool HasGps { get; }

    /// <summary>
    /// Check if the device supports Bluetooth.
    /// </summary>
    bool HasBluetooth { get; }

    /// <summary>
    /// Check if the device supports NFC.
    /// </summary>
    bool HasNfc { get; }

    /// <summary>
    /// Check if the device supports biometric authentication.
    /// </summary>
    bool HasBiometrics { get; }

    /// <summary>
    /// Check if the device supports haptic feedback.
    /// </summary>
    bool HasHapticFeedback { get; }

    /// <summary>
    /// Check if network connectivity is available.
    /// </summary>
    bool HasNetworkConnectivity { get; }

    /// <summary>
    /// Get the current network type (WiFi, Cellular, Ethernet, None).
    /// </summary>
    string NetworkType { get; }

    /// <summary>
    /// Get the battery level percentage (0-100).
    /// </summary>
    int BatteryLevel { get; }

    /// <summary>
    /// Check if the device is charging.
    /// </summary>
    bool IsCharging { get; }
}
