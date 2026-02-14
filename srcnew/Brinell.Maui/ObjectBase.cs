namespace Brinell.Maui;

using Brinell.Core.Utilities;

/// <summary>
/// Base class for all MAUI objects providing shared utilities.
/// Both page objects and controls inherit from this class.
/// </summary>
public abstract class ObjectBase
{
    /// <summary>
    /// Gets the MAUI test context.
    /// </summary>
    public abstract IMauiTestContext Context { get; }
    
    /// <summary>
    /// Gets the default timeout in milliseconds.
    /// </summary>
    protected int DefaultTimeoutMs => Context.Timeouts.DefaultWait;
    
    /// <summary>
    /// Gets the polling interval in milliseconds.
    /// </summary>
    protected int PollingIntervalMs => Context.Timeouts.PollingInterval;
    
    /// <summary>
    /// Polls a condition until it returns true or timeout is reached.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    protected bool Poll(Func<bool> condition, int timeoutMs)
    {
        var stopwatch = Stopwatch.StartNew();
        
        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                if (condition())
                {
                    return true;
                }
            }
            catch
            {
                // Ignore exceptions during polling, continue trying
            }
            
            WaitHelper.Pause(PollingIntervalMs);
        }
        
        // Final check after timeout
        try
        {
            return condition();
        }
        catch
        {
            return false;
        }
    }
}
