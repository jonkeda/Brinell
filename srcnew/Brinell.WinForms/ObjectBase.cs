namespace Brinell.WinForms;

using Brinell.Core.Utilities;

/// <summary>
/// Base class for all WinForms objects providing shared utilities.
/// </summary>
public abstract class ObjectBase
{
    /// <summary>
    /// Gets the WinForms test context.
    /// </summary>
    public abstract IWinFormsTestContext Context { get; }

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
    protected bool Poll(Func<bool> condition, int timeoutMs)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                if (condition())
                    return true;
            }
            catch
            {
                // Polling expects transient failures
            }

            WaitHelper.Pause(PollingIntervalMs);
        }

        return condition();
    }
}
