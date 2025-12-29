using Brinell.Core.Abstractions;
using Brinell.Core.Logging;

namespace Brinell.Core.Testing;

/// <summary>
/// Generic base class for UI tests. Platform-specific test bases derive from this.
/// TContext is the platform-specific context implementation (e.g., FlaUITestContext, AppiumTestContext).
/// </summary>
/// <typeparam name="TContext">The platform-specific test context type.</typeparam>
public abstract class UITestBase<TContext> : IDisposable 
    where TContext : class, ITestContext
{
    private readonly Action<string>? _outputWriter;
    private TContext? _context;
    private ITestLogger? _logger;
    private bool _disposed;

    /// <summary>
    /// Create a new UI test base with optional output writer.
    /// </summary>
    /// <param name="outputWriter">Action to write test output (e.g., xUnit's ITestOutputHelper.WriteLine).</param>
    protected UITestBase(Action<string>? outputWriter = null)
    {
        _outputWriter = outputWriter;
        TestName = GetType().Name;
    }

    /// <summary>
    /// Name of the current test.
    /// </summary>
    protected string TestName { get; set; }
    
    /// <summary>
    /// The CSV test logger. Prefer accessing via Context.Logger.
    /// </summary>
    protected ITestLogger? Logger => _context?.Logger ?? _logger;

    /// <summary>
    /// The test context for UI operations.
    /// </summary>
    protected TContext Context
    {
        get
        {
            if (_context == null)
                throw new InvalidOperationException("Context not initialized. Call InitializeContext first.");
            return _context;
        }
    }
    
    /// <summary>
    /// Check if context has been initialized.
    /// </summary>
    protected bool HasContext => _context != null;

    /// <summary>
    /// Initialize the context. Called by platform-specific initialization methods.
    /// </summary>
    protected void InitializeContext(TContext context, ITestLogger? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _context.TestName = TestName;
        
        if (logger != null)
        {
            _logger = logger;
            _context.SetLogger(logger);
        }
        else
        {
            // Create default logger if none provided
            _logger = CsvTestLogger.CreateDefault(TestName);
            _context.SetLogger(_logger);
        }
    }

    /// <summary>
    /// Log a message to test output (console/file).
    /// </summary>
    protected void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var formatted = $"[{timestamp}] {message}";
        _outputWriter?.Invoke(formatted);
    }

    /// <summary>
    /// Log a message to test output and CSV logger.
    /// Use this for important test events that should be tracked.
    /// </summary>
    protected void LogOutput(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var formattedMessage = $"[{timestamp}] {message}";
        
        // Write to output
        _outputWriter?.Invoke(formattedMessage);
        
        // Write to CSV logger
        Logger?.LogInfo(TestName, "Output", message);
    }

    /// <summary>
    /// Log with category prefix.
    /// </summary>
    protected void LogOutput(string category, string message)
    {
        LogOutput($"[{category}] {message}");
    }

    /// <summary>
    /// Take a screenshot for debugging.
    /// </summary>
    protected string? TakeScreenshot(string name)
    {
        var path = _context?.TakeScreenshot(name);
        if (path != null)
        {
            Logger?.LogAction(TestName, "", "", "Screenshot", path);
        }
        return path;
    }

    /// <summary>
    /// Wait for a specified time.
    /// </summary>
    protected void Wait(int milliseconds)
    {
        Thread.Sleep(milliseconds);
    }

    /// <summary>
    /// Dispose resources. Override in derived classes to clean up platform-specific resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose pattern implementation. Override to clean up platform-specific resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                try
                {
                    Log("Cleaning up test resources...");
                    Logger?.LogAction(TestName, "", "", "Cleanup");
                    _logger?.Dispose();
                }
                catch (Exception ex)
                {
                    Log($"Error disposing resources: {ex.Message}");
                }
            }
            _disposed = true;
        }
    }
}
