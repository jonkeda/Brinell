using Brinell.Maui.Infrastructure;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.Services;

/// <summary>
/// Device information service implementation for Appium.
/// </summary>
public class DeviceInfoService : IDeviceInfoService
{
    private readonly AppiumTestContext _context;

    public DeviceInfoService(AppiumTestContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public string Platform => _context.Driver.PlatformName;

    /// <inheritdoc/>
    public string Model
    {
        get
        {
            var caps = _context.Driver.GetCapabilities();
            return caps.GetCapability("deviceModel")?.ToString() 
                ?? caps.GetCapability("device")?.ToString() 
                ?? "Unknown";
        }
    }

    /// <inheritdoc/>
    public string Manufacturer
    {
        get
        {
            var caps = _context.Driver.GetCapabilities();
            return caps.GetCapability("deviceManufacturer")?.ToString() ?? "Unknown";
        }
    }

    /// <inheritdoc/>
    public string OSVersion
    {
        get
        {
            var caps = _context.Driver.GetCapabilities();
            return caps.GetCapability("platformVersion")?.ToString() 
                ?? caps.GetCapability("os_version")?.ToString() 
                ?? "Unknown";
        }
    }

    /// <inheritdoc/>
    public string DeviceIdiom
    {
        get
        {
            // Try to infer from device name or capabilities
            var deviceName = _context.Driver.DeviceName.ToLowerInvariant();
            if (deviceName.Contains("tablet") || deviceName.Contains("ipad"))
                return "Tablet";
            if (deviceName.Contains("tv") || deviceName.Contains("firetv"))
                return "TV";
            if (deviceName.Contains("watch"))
                return "Watch";
            if (deviceName.Contains("desktop") || deviceName.Contains("pc"))
                return "Desktop";
            return "Phone";
        }
    }

    /// <inheritdoc/>
    public int ScreenWidth => _context.Driver.ScreenSize.Width;

    /// <inheritdoc/>
    public int ScreenHeight => _context.Driver.ScreenSize.Height;

    /// <inheritdoc/>
    public double ScreenDensity
    {
        get
        {
            var caps = _context.Driver.GetCapabilities();
            var density = caps.GetCapability("deviceScreenDensity")?.ToString();
            if (double.TryParse(density, out var result))
                return result;
            return 1.0;
        }
    }

    /// <inheritdoc/>
    public bool IsPhysicalDevice
    {
        get
        {
            var caps = _context.Driver.GetCapabilities();
            var isReal = caps.GetCapability("realDevice")?.ToString();
            if (bool.TryParse(isReal, out var result))
                return result;
            
            // Check for known emulator indicators
            return !IsEmulator;
        }
    }

    /// <inheritdoc/>
    public bool IsEmulator
    {
        get
        {
            var deviceName = _context.Driver.DeviceName.ToLowerInvariant();
            return deviceName.Contains("emulator") 
                || deviceName.Contains("simulator")
                || deviceName.Contains("sdk")
                || deviceName.Contains("genymotion");
        }
    }

    /// <inheritdoc/>
    public bool IsPortrait => ScreenHeight > ScreenWidth;

    /// <inheritdoc/>
    public bool IsLandscape => ScreenWidth > ScreenHeight;
}
