using System.Diagnostics;
using Brinell.Core.Utilities;
using Brinell.Html.Interfaces;

namespace Brinell.Html;

/// <summary>
/// Base class for HTML page objects and controls.
/// </summary>
public abstract class ObjectBase
{
    public abstract IHtmlTestContext Context { get; }

    protected int DefaultTimeoutMs => Context.Timeouts.DefaultWait;

    protected int PollingIntervalMs => Context.Timeouts.PollingInterval;

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
                // Ignore exceptions while polling.
            }

            WaitHelper.Pause(PollingIntervalMs);
        }

        try
        {
            return condition();
        }
        catch
        {
            return false;
        }
    }

    protected async Task<bool> PollAsync(Func<Task<bool>> condition, int timeoutMs)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                if (await condition().ConfigureAwait(false))
                {
                    return true;
                }
            }
            catch
            {
                // Ignore exceptions while polling.
            }

            await Task.Delay(PollingIntervalMs).ConfigureAwait(false);
        }

        try
        {
            return await condition().ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }
}