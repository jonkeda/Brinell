namespace Brinell.Maui.Context;

/// <summary>
/// Configuration options for creating a MAUI test context.
/// </summary>
public class MauiTestContextOptions
{
    /// <summary>
    /// Gets or sets the Appium server URI.
    /// Default: http://localhost:4723
    /// </summary>
    public Uri AppiumServerUri { get; init; } = new("http://localhost:4723");
    
    /// <summary>
    /// Gets or sets the Appium driver options.
    /// Must be configured with appropriate capabilities for the target platform.
    /// </summary>
    public required AppiumOptions AppiumOptions { get; init; }
    
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
