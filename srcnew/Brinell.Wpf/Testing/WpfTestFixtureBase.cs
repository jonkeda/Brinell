using Brinell.Wpf.Context;

namespace Brinell.Wpf.Testing;

/// <summary>
/// Base fixture for WPF UI tests that manages the FlaUI driver and test context lifecycle.
/// Inherit from this class and implement <see cref="GetDefaultAppPath"/>.
/// </summary>
/// <remarks>
/// Configuration via environment variables:
/// - WPF_APP_PATH: Path to the WPF app executable
/// - WPF_PROCESS_NAME: Process name to attach to (when WPF_ATTACH_TO_RUNNING is true)
/// - WPF_ATTACH_TO_RUNNING: "true" to attach to an already running instance
/// - WPF_WINDOW_HANDLE: Window handle to attach to (hex or decimal)
/// </remarks>
public abstract class WpfTestFixtureBase : IDisposable
{
    private static int _instanceCount;
    private readonly int _instanceId;
    private readonly WpfTestContext _context;
    private bool _disposed;

    /// <summary>
    /// Initializes the fixture by creating the test context.
    /// </summary>
    protected WpfTestFixtureBase()
    {
        _instanceId = Interlocked.Increment(ref _instanceCount);
        Console.WriteLine($"[FIXTURE] {GetType().Name} #{_instanceId} CREATING at {DateTime.Now:HH:mm:ss.fff}");

        var options = CreateTestContextOptions();
        _context = new WpfTestContext(options);

        Console.WriteLine($"[FIXTURE] {GetType().Name} #{_instanceId} CREATED - Driver session started");
    }

    /// <summary>
    /// Gets the WPF test context.
    /// </summary>
    public WpfTestContext Context => _context;

    #region Abstract Methods

    /// <summary>
    /// Gets the default path to the WPF application executable.
    /// Must be implemented by derived classes.
    /// </summary>
    protected abstract string GetDefaultAppPath();

    #endregion

    #region Virtual Configuration Methods

    /// <summary>
    /// Creates test context options from environment variables or defaults.
    /// Override to customize driver configuration.
    /// </summary>
    protected virtual WpfTestContextOptions CreateTestContextOptions()
    {
        var attachToRunning = ParseBool(Environment.GetEnvironmentVariable("WPF_ATTACH_TO_RUNNING"));
        var processName = Environment.GetEnvironmentVariable("WPF_PROCESS_NAME");
        var windowHandle = ParseWindowHandle(Environment.GetEnvironmentVariable("WPF_WINDOW_HANDLE"));

        var appPath = Environment.GetEnvironmentVariable("WPF_APP_PATH");
        if (string.IsNullOrWhiteSpace(appPath) && !attachToRunning)
        {
            appPath = GetDefaultAppPath();
        }

        var timeouts = new TimeoutSettings
        {
            DefaultWait = 5000,
            PageLoad = 10000,
            ElementFind = 3000,
            ElementState = 3000,
            Animation = 300,
            PollingInterval = 100
        };

        if (attachToRunning && windowHandle != IntPtr.Zero)
        {
            return new WpfTestContextOptions
            {
                WindowHandle = windowHandle,
                Timeouts = timeouts
            };
        }

        if (attachToRunning && !string.IsNullOrWhiteSpace(processName))
        {
            var process = Process.GetProcessesByName(processName).FirstOrDefault()
                ?? throw new InvalidOperationException($"Process '{processName}' not found");
            return new WpfTestContextOptions
            {
                ProcessId = process.Id,
                Timeouts = timeouts
            };
        }

        return new WpfTestContextOptions
        {
            ExecutablePath = appPath,
            Timeouts = timeouts
        };
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Gets the screenshot output directory path.
    /// Override to customize the screenshot location.
    /// </summary>
    protected virtual string GetScreenshotDirectory()
    {
        var solutionDir = FindSolutionDirectory();
        var path = Path.Combine(solutionDir, "TestResults", "Screenshots");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Finds the solution root directory by searching for *.sln files.
    /// </summary>
    protected static string FindSolutionDirectory()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0)
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return Directory.GetCurrentDirectory();
    }

    private static bool ParseBool(string? value)
    {
        return bool.TryParse(value, out var result) && result;
    }

    private static IntPtr ParseWindowHandle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return IntPtr.Zero;
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (long.TryParse(value[2..], System.Globalization.NumberStyles.HexNumber, null, out var hex))
                return new IntPtr(hex);
        }
        if (long.TryParse(value, out var dec))
            return new IntPtr(dec);
        return IntPtr.Zero;
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed and unmanaged resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        Console.WriteLine($"[FIXTURE] {GetType().Name} #{_instanceId} DISPOSING at {DateTime.Now:HH:mm:ss.fff}");

        if (disposing)
        {
            _context?.Dispose();
        }

        _disposed = true;

        Console.WriteLine($"[FIXTURE] {GetType().Name} #{_instanceId} DISPOSED");
    }

    #endregion
}
