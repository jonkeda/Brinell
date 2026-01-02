using Brinell.WinForms.Infrastructure;
using Brinell.Core.Logging;
using Xunit;

namespace Brinell.Samples.WinForms.UITests.Infrastructure;

/// <summary>
/// Base class for WinForms UI tests using xUnit.
/// Provides initialization and cleanup of FlaUI test context.
/// </summary>
public abstract class UITestBase : IAsyncLifetime
{
    private FlaUITestContext? _context;
    private ITestLogger? _logger;
    private FlaUIDriverAdapter? _driver;
    private string _testName;

    /// <summary>
    /// The FlaUI test context for UI automation operations.
    /// </summary>
    protected FlaUITestContext Context
    {
        get
        {
            if (_context == null)
                throw new InvalidOperationException("Context not initialized. Initialize xUnit test framework.");
            return _context;
        }
    }

    /// <summary>
    /// Check if context is initialized.
    /// </summary>
    protected bool HasContext => _context != null;

    /// <summary>
    /// The test name, from xUnit.
    /// </summary>
    protected string TestName => _testName;

    /// <summary>
    /// Constructor - initialize test name from xUnit context.
    /// </summary>
    protected UITestBase()
    {
        _testName = GetType().Name;
    }

    /// <summary>
    /// xUnit InitializeAsync - called before each test.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Create logger with test output file
            var logDir = Path.Combine(Path.GetTempPath(), "BrinellTests");
            Directory.CreateDirectory(logDir);
            var logFile = Path.Combine(logDir, $"{_testName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            _logger = new CsvTestLogger(logFile);
            
            // Find the sample app executable
            var appPath = FindSampleApp();
            
            // Create FlaUI driver with the app
            _driver = new FlaUIDriverAdapter(appPath);
            
            // Create FlaUI context
            _context = new FlaUITestContext(_driver, _logger);
            _context.TestName = _testName;
            
            // Give the window time to initialize
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing test: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// xUnit DisposeAsync - called after each test.
    /// </summary>
    public async Task DisposeAsync()
    {
        try
        {
            // Cleanup FlaUI context (doesn't dispose)
            _context = null;

            // Cleanup driver
            if (_driver != null)
            {
                try
                {
                    // Close the application
                    _driver.MainWindow.Close();
                }
                catch { }
                _driver = null;
            }

            // Cleanup logger
            _logger?.Dispose();
            _logger = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cleaning up test: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Find the sample application executable.
    /// </summary>
    private static string FindSampleApp()
    {
        // Try to find the app in common locations
        var possiblePaths = new[]
        {
            "Brinell.Samples.WinForms.App.exe",
            Path.Combine(AppContext.BaseDirectory, "Brinell.Samples.WinForms.App.exe"),
            Path.Combine(AppContext.BaseDirectory, "../../../Brinell.Samples.WinForms.App/bin/Debug/net10.0-windows/Brinell.Samples.WinForms.App.exe"),
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
                return Path.GetFullPath(path);
        }

        throw new FileNotFoundException("Could not find Brinell.Samples.WinForms.App.exe");
    }

    /// <summary>
    /// Log a message to output.
    /// </summary>
    protected void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var formatted = $"[{timestamp}] {message}";
        System.Diagnostics.Debug.WriteLine(formatted);
    }

    /// <summary>
    /// Wait for specified milliseconds.
    /// </summary>
    protected void Wait(int milliseconds)
    {
        System.Threading.Thread.Sleep(milliseconds);
    }
}
