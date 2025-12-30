using Brinell.Maui.Infrastructure;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.Services;

/// <summary>
/// App lifecycle service implementation for Appium.
/// </summary>
public class AppLifecycleService : IAppLifecycleService
{
    private readonly AppiumTestContext _context;

    public AppLifecycleService(AppiumTestContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public void SendToBackground()
    {
        _context.Driver.SendToBackground();
    }

    /// <inheritdoc/>
    public void SendToBackground(int durationMs)
    {
        _context.Driver.SendToBackground(durationMs);
    }

    /// <inheritdoc/>
    public void BringToForeground()
    {
        _context.Driver.BringToForeground();
    }

    /// <inheritdoc/>
    public void ResetApp()
    {
        _context.Driver.ResetApp();
    }

    /// <inheritdoc/>
    public void TerminateAndRelaunch()
    {
        var appId = _context.Driver.AppId;
        if (string.IsNullOrEmpty(appId))
            throw new InvalidOperationException("App ID not available for terminate and relaunch.");
        
        _context.Driver.Terminate(appId);
        Thread.Sleep(500);
        _context.Driver.Activate(appId);
    }

    /// <inheritdoc/>
    public string GetAppState()
    {
        var appId = _context.Driver.AppId;
        if (string.IsNullOrEmpty(appId))
            return "Unknown";
        
        var state = _context.Driver.GetAppState(appId);
        return state.ToString();
    }

    /// <inheritdoc/>
    public bool IsInForeground
    {
        get
        {
            var state = GetAppState();
            return state.Contains("Running", StringComparison.OrdinalIgnoreCase) 
                || state.Contains("Foreground", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <inheritdoc/>
    public string GetCurrentPage()
    {
        // Platform-specific: try to get current activity (Android) or page title
        try
        {
            var caps = _context.Driver.GetCapabilities();
            return caps.GetCapability("currentActivity")?.ToString()
                ?? caps.GetCapability("currentPage")?.ToString()
                ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <inheritdoc/>
    public void InstallApp(string appPath)
    {
        if (string.IsNullOrEmpty(appPath))
            throw new ArgumentNullException(nameof(appPath));
        
        _context.Driver.InstallApp(appPath);
    }

    /// <inheritdoc/>
    public void UninstallApp()
    {
        var appId = _context.Driver.AppId;
        if (string.IsNullOrEmpty(appId))
            throw new InvalidOperationException("App ID not available for uninstall.");
        
        _context.Driver.Uninstall(appId);
    }

    /// <inheritdoc/>
    public void ClearAppData()
    {
        // This is typically done through ResetApp or platform-specific commands
        var appId = _context.Driver.AppId;
        if (string.IsNullOrEmpty(appId))
            throw new InvalidOperationException("App ID not available for clear data.");
        
        // For Android, you might use: adb shell pm clear <package>
        // For iOS, this typically requires reinstall
        ResetApp();
    }
}
