using Brinell.Core.Utilities;
using Brinell.Maui.Configuration;

namespace Brinell.Maui.Context;

/// <summary>
/// MAUI test context implementation supporting multiple drivers (Appium, FlaUI).
/// Uses MauiDriverFactory to create the appropriate driver for the platform.
/// </summary>
public class MauiTestContext : IMauiTestContext
{
    private readonly Interfaces.IMauiDriver _driver;
    private readonly TimeoutSettings _timeouts;
    private readonly ITestLogger _logger;

    /// <summary>The call log this context opened itself (<c>BRINELL_CALL_LOG</c>), disposed with it.</summary>
    private readonly CsvTestLogger? _callLog;
    private readonly MauiPlatform _platform;
    private readonly bool _ownsDriver;
    private bool _disposed;
    
    /// <summary>
    /// Creates a new MAUI test context with the specified options.
    /// </summary>
    /// <param name="options">Configuration options for the context.</param>
    public MauiTestContext(MauiTestContextOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        
        _timeouts = options.Timeouts ?? TimeoutSettings.Default;
        _callLog = options.Logger == null ? OpenCallLog() : null;
        _logger = options.Logger ?? _callLog ?? (ITestLogger)NullTestLogger.Instance;
        
        // Use injected driver if provided, otherwise use factory
        if (options.Driver != null)
        {
            _driver = options.Driver;
            _platform = _driver.Platform;
            _ownsDriver = false; // Caller owns injected driver
        }
        else
        {
            ArgumentNullException.ThrowIfNull(options.DriverOptions, nameof(options.DriverOptions));
            
            var driverOptions = options.DriverOptions;
            // Apply overrides from context options
            driverOptions.Timeouts ??= options.Timeouts;
            driverOptions.Logger ??= options.Logger;
            
            _driver = MauiDriverFactory.Create(driverOptions);
            _platform = driverOptions.Platform;
            _ownsDriver = true; // We own factory-created driver
        }
    }
    
    /// <inheritdoc />
    public Interfaces.IMauiDriver Driver => _driver;
    
    /// <inheritdoc />
    public IMauiTestContext Context => this;
    
    /// <inheritdoc />
    public MauiPlatform Platform => _platform;

    /// <inheritdoc />
    public IMauiElement AppElement => _driver.AppElement;

    /// <inheritdoc />
    public ShellChromeLocators ShellChrome => _driver.ShellChrome;

    /// <inheritdoc />
    public TimeoutSettings Timeouts => _timeouts;
    
    /// <inheritdoc />
    public ITestLogger Logger => _logger;
    
    /// <inheritdoc />
    public LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.AutomationId;
    
    /// <inheritdoc />
    /// <remarks>
    /// Test context root scope has no associated page.
    /// </remarks>
    public IMauiPage? Page => null;
    
    /// <inheritdoc />
    /// <remarks>
    /// Ready while the session is open: the context is a lookup service, not a scope with content.
    /// </remarks>
    public ScopeReadiness ProbeReadiness()
        => _disposed
            ? new ScopeReadiness(nameof(MauiTestContext), ScopeReadinessState.MissingRoot, "the test context is disposed")
            : ScopeReadiness.Ready(nameof(MauiTestContext));
    
    /// <inheritdoc />
    /// <remarks>
    /// Test context root is always ready (driver is connected).
    /// </remarks>
    public bool WaitReady(int? timeoutMs = null) => !_disposed;
    
    /// <inheritdoc />
    /// <remarks>
    /// Null means "nothing matches now" and nothing else. A driver error propagates: reading it as
    /// "absent" would let <c>WaitExists(false)</c> pass on a broken driver (F4).
    /// </remarks>
    public IMauiElement? TryFindElement(Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);

        var elements = _driver.FindElements(locator);
        return elements.Count > 0 ? elements[0] : null;
    }
    
    /// <inheritdoc />
    /// <remarks>
    /// One attempt, like every lookup (R2): it used to wait <c>ElementFind</c> (3 s) and then sweep
    /// the app, inside every poll tick of every control on <c>AppRoot</c> (F1, F2). Waiting is the
    /// call's poll's job, and so is the sweep (throttled, in the control's lookup).
    /// </remarks>
    public IMauiElement FindElement(Locator locator)
        => TryFindElement(locator)
           ?? throw new ElementNotFoundException($"Element not found with locator: {locator}");
    
    /// <inheritdoc />
    public IReadOnlyList<IMauiElement> FindElements(Locator locator)
    {        
        ArgumentNullException.ThrowIfNull(locator);
        
        return _driver.FindElements(locator);
    }
    
    /// <inheritdoc />
    public void NavigateTo(string destination)
    {        
        ArgumentNullException.ThrowIfNull(destination);
        
        _logger.LogNavigation("", "", destination);
        _driver.NavigateTo(destination);
    }
    
    /// <inheritdoc />
    public void NavigateBack()
    {        
        _driver.NavigateBack();
    }
    
    /// <inheritdoc />
    public void Refresh()
    {        
        _driver.Refresh();
    }
    
    /// <inheritdoc />
    public byte[] TakeScreenshot()
    {        
        return _driver.TakeScreenshot();
    }
    
    /// <inheritdoc />
    public void SaveScreenshot(string path)
    {        
        ArgumentNullException.ThrowIfNull(path);
        
        var screenshot = _driver.TakeScreenshot();
        File.WriteAllBytes(path, screenshot);
    }
    
    /// <inheritdoc />
    public void ResetAppState()
    {      
        _driver.ResetAppState();
    }
    
    /// <summary>
    /// A call log in the folder <c>BRINELL_CALL_LOG</c> names, when it is set: one CSV file per
    /// context, with every call's entry and exit, near-miss warnings included
    /// (<c>.my/stale-readiness/design.md</c>, 4.4). Off unless asked for, and only when the
    /// options give no logger of their own.
    /// </summary>
    private static CsvTestLogger? OpenCallLog()
    {
        var folder = Environment.GetEnvironmentVariable("BRINELL_CALL_LOG");
        if (string.IsNullOrWhiteSpace(folder))
        {
            return null;
        }

        var log = new CsvTestLogger(Path.Combine(folder,
            $"calls-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}-{Guid.NewGuid().ToString("N")[..8]}.csv"));
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                log.Flush();
            }
            catch (ObjectDisposedException)
            {
                // Already closed with its context.
            }
        };
        return log;
    }

    /// <summary>
    /// Disposes the test context and quits the driver.
    /// </summary>
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
        
        if (disposing)
        {
            _callLog?.Dispose();
        }

        if (disposing && _ownsDriver)
        {
            try
            {
                _driver?.Dispose();
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }
        
        _disposed = true;
    }
}
