using Brinell.Maui.Infrastructure;
using Brinell.Maui.Services;

namespace Brinell.Maui.Testing;

/// <summary>
/// Base class for platform-specific MAUI tests.
/// Provides convenient access to platform detection and device services.
/// </summary>
public abstract class PlatformSpecificTestBase : IDisposable
{
    protected readonly AppiumTestContext Context;
    private readonly Lazy<IDeviceInfoService> _deviceInfo;
    private readonly Lazy<IDeviceCapabilitiesService> _deviceCapabilities;
    private readonly Lazy<IAppLifecycleService> _appLifecycle;
    private readonly Lazy<IAlertService> _alertService;

    protected PlatformSpecificTestBase(AppiumTestContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        
        _deviceInfo = new Lazy<IDeviceInfoService>(() => new DeviceInfoService(context));
        _deviceCapabilities = new Lazy<IDeviceCapabilitiesService>(() => new DeviceCapabilitiesService(context));
        _appLifecycle = new Lazy<IAppLifecycleService>(() => new AppLifecycleService(context));
        _alertService = new Lazy<IAlertService>(() => new AlertService(context));
    }

    #region Device Services

    /// <summary>
    /// Device information service.
    /// </summary>
    protected IDeviceInfoService DeviceInfo => _deviceInfo.Value;

    /// <summary>
    /// Device capabilities service.
    /// </summary>
    protected IDeviceCapabilitiesService DeviceCapabilities => _deviceCapabilities.Value;

    /// <summary>
    /// App lifecycle service.
    /// </summary>
    protected IAppLifecycleService AppLifecycle => _appLifecycle.Value;

    /// <summary>
    /// Alert/dialog service.
    /// </summary>
    protected IAlertService Alerts => _alertService.Value;

    #endregion

    #region Platform Detection

    /// <summary>
    /// Check if running on Android.
    /// </summary>
    protected bool IsAndroid => Context.Driver.IsAndroid;

    /// <summary>
    /// Check if running on iOS.
    /// </summary>
    protected bool IsIOS => Context.Driver.IsIOS;

    /// <summary>
    /// Check if running on Windows.
    /// </summary>
    protected bool IsWindows => Context.Driver.IsWindows;

    /// <summary>
    /// Check if running on macOS.
    /// </summary>
    protected bool IsMac => DeviceInfo.Platform.Contains("Mac", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Check if running on a phone-sized device.
    /// </summary>
    protected bool IsPhone => DeviceInfo.DeviceIdiom == "Phone";

    /// <summary>
    /// Check if running on a tablet-sized device.
    /// </summary>
    protected bool IsTablet => DeviceInfo.DeviceIdiom == "Tablet";

    /// <summary>
    /// Check if running on a desktop platform.
    /// </summary>
    protected bool IsDesktop => DeviceInfo.DeviceIdiom == "Desktop" || IsWindows || IsMac;

    /// <summary>
    /// Check if running on a physical device.
    /// </summary>
    protected bool IsRealDevice => DeviceInfo.IsPhysicalDevice;

    /// <summary>
    /// Check if running on an emulator/simulator.
    /// </summary>
    protected bool IsEmulator => DeviceInfo.IsEmulator;

    #endregion

    #region Orientation

    /// <summary>
    /// Check if device is in portrait orientation.
    /// </summary>
    protected bool IsPortrait => DeviceInfo.IsPortrait;

    /// <summary>
    /// Check if device is in landscape orientation.
    /// </summary>
    protected bool IsLandscape => DeviceInfo.IsLandscape;

    /// <summary>
    /// Rotate device to portrait.
    /// </summary>
    protected void RotateToPortrait()
    {
        Context.Driver.RotateToPortrait();
    }

    /// <summary>
    /// Rotate device to landscape.
    /// </summary>
    protected void RotateToLandscape()
    {
        Context.Driver.RotateToLandscape();
    }

    #endregion

    #region Screen Info

    /// <summary>
    /// Get screen width in pixels.
    /// </summary>
    protected int ScreenWidth => DeviceInfo.ScreenWidth;

    /// <summary>
    /// Get screen height in pixels.
    /// </summary>
    protected int ScreenHeight => DeviceInfo.ScreenHeight;

    /// <summary>
    /// Get screen density.
    /// </summary>
    protected double ScreenDensity => DeviceInfo.ScreenDensity;

    #endregion

    #region Platform-Specific Helpers

    /// <summary>
    /// Execute action only on Android.
    /// </summary>
    protected void OnAndroid(Action action)
    {
        if (IsAndroid)
            action();
    }

    /// <summary>
    /// Execute action only on iOS.
    /// </summary>
    protected void OnIOS(Action action)
    {
        if (IsIOS)
            action();
    }

    /// <summary>
    /// Execute action only on Windows.
    /// </summary>
    protected void OnWindows(Action action)
    {
        if (IsWindows)
            action();
    }

    /// <summary>
    /// Execute action only on Mac.
    /// </summary>
    protected void OnMac(Action action)
    {
        if (IsMac)
            action();
    }

    /// <summary>
    /// Execute action only on mobile platforms (Android/iOS).
    /// </summary>
    protected void OnMobile(Action action)
    {
        if (IsAndroid || IsIOS)
            action();
    }

    /// <summary>
    /// Execute action only on desktop platforms (Windows/Mac).
    /// </summary>
    protected void OnDesktop(Action action)
    {
        if (IsDesktop)
            action();
    }

    /// <summary>
    /// Execute platform-specific actions.
    /// </summary>
    protected void OnPlatform(Action? android = null, Action? ios = null, Action? windows = null, Action? mac = null)
    {
        if (IsAndroid && android != null)
            android();
        else if (IsIOS && ios != null)
            ios();
        else if (IsWindows && windows != null)
            windows();
        else if (IsMac && mac != null)
            mac();
    }

    /// <summary>
    /// Get platform-specific value.
    /// </summary>
    protected T OnPlatform<T>(T android, T ios, T windows, T mac, T defaultValue)
    {
        if (IsAndroid) return android;
        if (IsIOS) return ios;
        if (IsWindows) return windows;
        if (IsMac) return mac;
        return defaultValue;
    }

    /// <summary>
    /// Skip test if not on specified platform.
    /// </summary>
    protected void RequireAndroid()
    {
        if (!IsAndroid)
            throw new SkipTestException("Test requires Android platform.");
    }

    /// <summary>
    /// Skip test if not on specified platform.
    /// </summary>
    protected void RequireIOS()
    {
        if (!IsIOS)
            throw new SkipTestException("Test requires iOS platform.");
    }

    /// <summary>
    /// Skip test if not on specified platform.
    /// </summary>
    protected void RequireWindows()
    {
        if (!IsWindows)
            throw new SkipTestException("Test requires Windows platform.");
    }

    /// <summary>
    /// Skip test if not on mobile platform.
    /// </summary>
    protected void RequireMobile()
    {
        if (!IsAndroid && !IsIOS)
            throw new SkipTestException("Test requires mobile platform (Android or iOS).");
    }

    /// <summary>
    /// Skip test if not on desktop platform.
    /// </summary>
    protected void RequireDesktop()
    {
        if (!IsDesktop)
            throw new SkipTestException("Test requires desktop platform (Windows or Mac).");
    }

    /// <summary>
    /// Skip test if not on real device.
    /// </summary>
    protected void RequireRealDevice()
    {
        if (!IsRealDevice)
            throw new SkipTestException("Test requires a physical device.");
    }

    #endregion

    #region Cleanup

    public virtual void Dispose()
    {
        // Base cleanup - derived classes can override
    }

    #endregion
}

/// <summary>
/// Exception to indicate test should be skipped.
/// </summary>
public class SkipTestException : Exception
{
    public SkipTestException(string message) : base(message) { }
}
