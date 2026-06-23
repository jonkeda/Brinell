using Brinell.Maui.Configuration;
using Brinell.Maui.Enums;
using Brinell.Maui.Interfaces;

namespace Brinell.Maui.Context;

/// <summary>
/// Configuration options for creating a MAUI test context.
/// </summary>
public class MauiTestContextOptions
{
    /// <summary>
    /// Pre-created driver instance (for testing or custom drivers).
    /// If set, factory is not used and DriverOptions are ignored.
    /// </summary>
    public IMauiDriver? Driver { get; init; }
    
    /// <summary>
    /// Gets or sets the driver options for the factory.
    /// Required when Driver is not set.
    /// </summary>
    public MauiDriverOptions? DriverOptions { get; init; }
    
    /// <summary>
    /// Gets or sets the timeout configuration.
    /// If null, default timeouts will be used.
    /// </summary>
    public TimeoutSettings? Timeouts { get; init; }
    
    /// <summary>
    /// Gets or sets the test logger.
    /// If null, a no-op logger will be used.
    /// </summary>
    public ITestLogger? Logger { get; init; }
}
