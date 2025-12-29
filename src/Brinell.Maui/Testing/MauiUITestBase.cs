using Brinell.Core.Logging;
using Brinell.Core.Testing;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Testing;

/// <summary>
/// Base class for MAUI UI tests using Appium.
/// Provides app start and driver management for Windows, Android, and iOS.
/// </summary>
public abstract class MauiUITestBase : UITestBase<AppiumTestContext>
{
    private AppiumDriverAdapter? _driver;

    /// <summary>
    /// Create a new MAUI UI test base with optional output writer.
    /// </summary>
    /// <param name="outputWriter">Action to write test output (e.g., xUnit's ITestOutputHelper.WriteLine).</param>
    protected MauiUITestBase(Action<string>? outputWriter = null) : base(outputWriter)
    {
    }

    /// <summary>
    /// The driver adapter for direct element access.
    /// </summary>
    protected AppiumDriverAdapter Driver
    {
        get
        {
            if (_driver == null)
                throw new InvalidOperationException("Driver not initialized. Call StartApp first.");
            return _driver;
        }
    }

    /// <summary>
    /// Appium server URL. Override to change default.
    /// </summary>
    protected virtual string AppiumServerUrl => "http://127.0.0.1:4723";

    /// <summary>
    /// Gets the path to the application or app package.
    /// Override in derived classes.
    /// </summary>
    protected abstract string AppPath { get; }

    /// <summary>
    /// Gets the target platform. Override for Android/iOS.
    /// </summary>
    protected virtual string Platform => "Windows";

    /// <summary>
    /// Start the MAUI application.
    /// </summary>
    protected void StartApp(string? arguments = null)
    {
        Log($"Starting MAUI app: {AppPath} on {Platform}");
        
        // Create logger
        var logger = CsvTestLogger.CreateDefault(TestName);
        logger.LogInfo(TestName, "Start", $"Starting: {AppPath}");
        
        // Create driver and context
        var serverUri = new Uri(AppiumServerUrl);
        _driver = new AppiumDriverAdapter(AppPath, serverUri);
        var context = new AppiumTestContext(_driver, Log);
        InitializeContext(context, logger);
        
        Log("MAUI app started successfully");
        logger.LogAction(TestName, "", "Application", "Start", AppPath);
    }

    /// <summary>
    /// Dispose MAUI-specific resources.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                Log("Closing MAUI app...");
                Logger?.LogAction(TestName, "", "Application", "Close");
                _driver?.Dispose();
            }
            catch (Exception ex)
            {
                Log($"Error disposing driver: {ex.Message}");
            }
        }
        base.Dispose(disposing);
    }
}
