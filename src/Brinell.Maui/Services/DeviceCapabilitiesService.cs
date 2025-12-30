using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Services;

/// <summary>
/// Device capabilities service implementation for Appium.
/// Note: Many capability checks require platform-specific implementations.
/// </summary>
public class DeviceCapabilitiesService : IDeviceCapabilitiesService
{
    private readonly AppiumTestContext _context;

    public DeviceCapabilitiesService(AppiumTestContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public bool HasCamera => CheckCapability("camera", "hasCamera");

    /// <inheritdoc/>
    public bool HasFrontCamera => CheckCapability("frontCamera", "hasFrontCamera");

    /// <inheritdoc/>
    public bool HasGps => CheckCapability("gps", "hasGps", "locationServicesEnabled");

    /// <inheritdoc/>
    public bool HasBluetooth => CheckCapability("bluetooth", "hasBluetooth");

    /// <inheritdoc/>
    public bool HasNfc => CheckCapability("nfc", "hasNfc");

    /// <inheritdoc/>
    public bool HasBiometrics => CheckCapability("biometrics", "hasBiometrics", "hasTouchId", "hasFaceId");

    /// <inheritdoc/>
    public bool HasHapticFeedback => CheckCapability("haptics", "hasHaptics");

    /// <inheritdoc/>
    public bool HasNetworkConnectivity
    {
        get
        {
            var networkType = NetworkType;
            return !string.IsNullOrEmpty(networkType) && !networkType.Equals("none", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <inheritdoc/>
    public string NetworkType
    {
        get
        {
            var caps = _context.Driver.GetCapabilities();
            return caps.GetCapability("networkConnection")?.ToString()
                ?? caps.GetCapability("networkType")?.ToString()
                ?? "Unknown";
        }
    }

    /// <inheritdoc/>
    public int BatteryLevel
    {
        get
        {
            var caps = _context.Driver.GetCapabilities();
            var level = caps.GetCapability("batteryLevel")?.ToString();
            if (int.TryParse(level, out var result))
                return result;
            
            // Try getting from device state (platform-specific)
            return -1;
        }
    }

    /// <inheritdoc/>
    public bool IsCharging
    {
        get
        {
            var caps = _context.Driver.GetCapabilities();
            var charging = caps.GetCapability("batteryState")?.ToString();
            return charging?.Contains("charging", StringComparison.OrdinalIgnoreCase) ?? false;
        }
    }

    private bool CheckCapability(params string[] capabilityNames)
    {
        var caps = _context.Driver.GetCapabilities();
        foreach (var name in capabilityNames)
        {
            var value = caps.GetCapability(name)?.ToString();
            if (bool.TryParse(value, out var result) && result)
                return true;
            if (value?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
                return true;
        }
        return false;
    }
}
