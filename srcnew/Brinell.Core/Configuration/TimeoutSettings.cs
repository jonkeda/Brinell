namespace Brinell.Core.Configuration;

/// <summary>
/// Timeout configuration for test operations.
/// </summary>
public class TimeoutSettings
{
    /// <summary>
    /// Default timeout for wait operations (milliseconds).
    /// </summary>
    public int DefaultWait { get; set; } = 5000;
    
    /// <summary>
    /// Timeout for page load operations (milliseconds).
    /// </summary>
    public int PageLoad { get; set; } = 10000;
    
    /// <summary>
    /// Timeout for element finding (milliseconds).
    /// </summary>
    public int ElementFind { get; set; } = 3000;
    
    /// <summary>
    /// Timeout for element state changes (milliseconds).
    /// </summary>
    public int ElementState { get; set; } = 3000;
    
    /// <summary>
    /// Delay for animation settling (milliseconds).
    /// </summary>
    public int Animation { get; set; } = 300;
    
    /// <summary>
    /// Polling interval for wait operations (milliseconds).
    /// </summary>
    public int PollingInterval { get; set; } = 100;
    
    /// <summary>
    /// Default timeout settings.
    /// </summary>
    public static TimeoutSettings Default => new();
    
    /// <summary>
    /// Fast timeout settings for quick checks.
    /// </summary>
    public static TimeoutSettings Fast => new()
    {
        DefaultWait = 2000,
        PageLoad = 5000,
        ElementFind = 1000,
        ElementState = 1000,
        Animation = 150,
        PollingInterval = 50
    };
    
    /// <summary>
    /// Slow timeout settings for flaky environments.
    /// </summary>
    public static TimeoutSettings Slow => new()
    {
        DefaultWait = 15000,
        PageLoad = 30000,
        ElementFind = 10000,
        ElementState = 10000,
        Animation = 500,
        PollingInterval = 200
    };
    
    /// <summary>
    /// Create a copy with modified values.
    /// </summary>
    public TimeoutSettings With(
        int? defaultWait = null,
        int? pageLoad = null,
        int? elementFind = null,
        int? elementState = null,
        int? animation = null,
        int? pollingInterval = null)
    {
        return new TimeoutSettings
        {
            DefaultWait = defaultWait ?? DefaultWait,
            PageLoad = pageLoad ?? PageLoad,
            ElementFind = elementFind ?? ElementFind,
            ElementState = elementState ?? ElementState,
            Animation = animation ?? Animation,
            PollingInterval = pollingInterval ?? PollingInterval
        };
    }
}
