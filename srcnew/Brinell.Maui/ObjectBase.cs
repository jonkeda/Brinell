namespace Brinell.Maui;

using Brinell.Maui.Calls;

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
    /// <remarks>
    /// Runs on the calls layer's one loop (<c>Poller</c>), so it keeps its rules: at least one
    /// attempt, a closed app or misconfigured page ends it at once, and other exceptions are
    /// retried. When the last attempt threw, that exception is rethrown, so the caller sees the
    /// real failure rather than a plain false.
    /// </remarks>
    /// <param name="condition">The condition to check.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds.</param>
    /// <returns>True if condition was met, false if timeout reached.</returns>
    protected bool Poll(Func<bool> condition, int? timeoutMs)
    {
        var context = new AttemptContext(Deadline.In(timeoutMs ?? 0), Context.Timeouts.Animation);
        if (Poller.Until(_ => condition() ? Observation.Done() : Observation.Pending(), context, PollingIntervalMs))
        {
            return true;
        }

        if (context.Log.Last is { Kind: ObservationKind.Failed, Error: { } error })
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        }

        return false;
    }
}
