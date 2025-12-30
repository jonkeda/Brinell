namespace Brinell.Maui.Services;

/// <summary>
/// Interface for app lifecycle management services.
/// Provides control over app state during tests.
/// </summary>
public interface IAppLifecycleService
{
    /// <summary>
    /// Send the app to the background.
    /// </summary>
    void SendToBackground();

    /// <summary>
    /// Send the app to the background for a specific duration.
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds to keep app in background.</param>
    void SendToBackground(int durationMs);

    /// <summary>
    /// Bring the app back to the foreground.
    /// </summary>
    void BringToForeground();

    /// <summary>
    /// Reset/restart the app.
    /// </summary>
    void ResetApp();

    /// <summary>
    /// Terminate and relaunch the app.
    /// </summary>
    void TerminateAndRelaunch();

    /// <summary>
    /// Get the current app state (Running, Background, Suspended).
    /// </summary>
    string GetAppState();

    /// <summary>
    /// Check if the app is in the foreground.
    /// </summary>
    bool IsInForeground { get; }

    /// <summary>
    /// Get the current activity/page name.
    /// </summary>
    string GetCurrentPage();

    /// <summary>
    /// Install the app on the device.
    /// </summary>
    /// <param name="appPath">Path to the app package.</param>
    void InstallApp(string appPath);

    /// <summary>
    /// Uninstall the app from the device.
    /// </summary>
    void UninstallApp();

    /// <summary>
    /// Clear app data and cache.
    /// </summary>
    void ClearAppData();
}
