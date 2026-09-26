using System.Diagnostics;
using Brinell.Core.Exceptions;
using Brinell.Core.Utilities;
using Brinell.Html.Exceptions;
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
            catch (Exception error) when (IsTransient(error))
            {
                // Asked again on the next tick; anything else is not a signal and ends the wait (R5).
            }

            WaitHelper.Pause(PollingIntervalMs);
        }

        try
        {
            return condition();
        }
        catch (Exception error) when (IsTransient(error))
        {
            return false;
        }
    }

    /// <summary>
    /// The signals a wait may ask again after: the element went away, is not there yet, or is not
    /// ready yet. A closed browser, and every other error, are not among them.
    /// </summary>
    protected static bool IsTransient(Exception error)
        => error is StaleElementException or ElementNotFoundException or ElementNotReadyException;

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
            catch (Exception error) when (IsTransient(error))
            {
                // As in Poll.
            }

            await Task.Delay(PollingIntervalMs).ConfigureAwait(false);
        }

        try
        {
            return await condition().ConfigureAwait(false);
        }
        catch (Exception error) when (IsTransient(error))
        {
            return false;
        }
    }
}