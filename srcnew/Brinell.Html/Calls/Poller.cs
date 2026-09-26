using Brinell.Core.Utilities;
using Brinell.Html.Exceptions;

namespace Brinell.Html.Calls;

/// <summary>
/// The one retry loop of an Html call.
/// </summary>
internal static class Poller
{
    /// <summary>
    /// Runs <paramref name="attempt"/> until it reports <see cref="ObservationKind.Done"/> or the
    /// deadline passes.
    /// </summary>
    /// <returns>Whether an attempt reported Done.</returns>
    public static bool Until(Func<AttemptContext, Observation> attempt, AttemptContext context, int pollingIntervalMs)
    {
        while (true)
        {
            Observation observation;
            try
            {
                observation = attempt(context);
            }
            catch (TimeoutException) when (context.Deadline.RemainingMs <= 0 && context.Log.Attempts > 0)
            {
                // The budget ran out in the middle of this attempt: a driver call below it was
                // given what was left (?A5) and used it up. That is not something the attempt saw,
                // so the call is reported by the last attempt that completed.
                return false;
            }
            catch (Exception error) when (!IsFatal(error))
            {
                observation = Observation.Failed(error);
            }

            context.Log.Add(observation, context.Deadline.ElapsedMs);

            if (observation.Kind == ObservationKind.Done)
            {
                return true;
            }

            var remaining = context.Deadline.RemainingMs;
            if (remaining <= 0)
            {
                return false;
            }

            WaitHelper.Pause(Math.Max(1, Math.Min(pollingIntervalMs, remaining)));
        }
    }

    /// <summary>Whether <paramref name="error"/> must end the call at once rather than be retried.</summary>
    public static bool IsFatal(Exception error) => error is AppUnavailableException or RouteUnavailableException;
}
