using FlaUI.Core;
using Brinell.Core.Logging;
using Brinell.Core.Testing;
using Brinell.Wpf.Infrastructure;

namespace Brinell.Wpf.Testing;

/// <summary>
/// Base class for WPF UI tests using FlaUI.
/// Provides application launch and driver management.
/// </summary>
public abstract class WpfUITestBase : UITestBase<FlaUITestContext>
{
    private FlaUIDriverAdapter? _driver;

    /// <summary>
    /// Create a new WPF UI test base with optional output writer.
    /// </summary>
    /// <param name="outputWriter">Action to write test output (e.g., xUnit's ITestOutputHelper.WriteLine).</param>
    protected WpfUITestBase(Action<string>? outputWriter = null) : base(outputWriter)
    {
    }

    /// <summary>
    /// The driver adapter for direct element access.
    /// </summary>
    protected FlaUIDriverAdapter Driver
    {
        get
        {
            if (_driver == null)
                throw new InvalidOperationException("Driver not initialized. Call LaunchApplication first.");
            return _driver;
        }
    }

    /// <summary>
    /// Gets the path to the application executable.
    /// Override in derived classes to specify the application.
    /// </summary>
    protected abstract string ApplicationPath { get; }

    /// <summary>
    /// Launch the application under test.
    /// </summary>
    protected void LaunchApplication(string? arguments = null)
    {
        Log($"Launching application: {ApplicationPath}");
        
        // Create logger
        var logger = CsvTestLogger.CreateDefault(TestName);
        logger.LogInfo(TestName, "Launch", $"Starting: {ApplicationPath}");
        
        // Create driver and context
        _driver = new FlaUIDriverAdapter(ApplicationPath, arguments);
        var context = new FlaUITestContext(_driver, Log);
        InitializeContext(context, logger);
        
        Log("Application launched successfully");
        logger.LogAction(TestName, "", "", "Launch", ApplicationPath);
    }

    /// <summary>
    /// Attach to an existing running application.
    /// </summary>
    protected void AttachToApplication(Application application)
    {
        Log("Attaching to existing application");
        
        // Create logger
        var logger = CsvTestLogger.CreateDefault(TestName);
        
        // Create driver and context
        _driver = new FlaUIDriverAdapter(application);
        var context = new FlaUITestContext(_driver, Log);
        InitializeContext(context, logger);
        
        Log("Attached to application successfully");
    }

    /// <summary>
    /// Dispose WPF-specific resources.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                Log("Closing application...");
                Logger?.LogAction(TestName, "", "", "Close");
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
