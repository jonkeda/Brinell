using Brinell.Core.Utilities;
using Brinell.Html.Exceptions;

namespace Brinell.Html.Calls;

/// <summary>
/// The wait for an action's effect, behind <c>Confirm</c> on controls.
/// </summary>
internal static class Confirmer
{
    public static Confirmation<T> Run<T>(Func<T?> read, Func<T?, bool> done, int budgetMs, int pollingIntervalMs)
    {
        var deadline = Deadline.In(budgetMs);
        T? last = default;
        Exception? lastError = null;

        while (true)
        {
            try
            {
                last = read();
                lastError = null;
                if (done(last))
                {
                    return new Confirmation<T>(ConfirmationResult.Confirmed, last, null);
                }
            }
            catch (StaleElementException error)
            {
                return new Confirmation<T>(ConfirmationResult.Replaced, last, error);
            }
            catch (Exception error) when (!Poller.IsFatal(error))
            {
                lastError = error;
            }

            var remaining = deadline.RemainingMs;
            if (remaining <= 0)
            {
                return new Confirmation<T>(ConfirmationResult.NotConfirmed, last, lastError);
            }

            WaitHelper.Pause(Math.Max(1, Math.Min(pollingIntervalMs, remaining)));
        }
    }
}
