using Brinell.Core.Utilities;

namespace Brinell.Maui.Calls;

/// <summary>
/// The one retry loop of a MAUI call.
/// </summary>
internal static class Poller
{
    /// <summary>
    /// Runs <paramref name="attempt"/> until it reports <see cref="ObservationKind.Done"/> or the
    /// deadline passes.
    /// </summary>
    /// <param name="stop">
    /// Whether an observation ends the loop early, as a final answer that is not Done: the end of a
    /// list a search has scrolled through. Null never stops early.
    /// </param>
    /// <returns>Whether an attempt reported Done.</returns>
    public static bool Until(Func<AttemptContext, Observation> attempt, AttemptContext context, int pollingIntervalMs,
        Func<Observation, bool>? stop = null)
    {
        while (true)
        {
            Observation observation;
            try
            {
                observation = attempt(context);
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

            if (stop?.Invoke(observation) == true)
            {
                return false;
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
    public static bool IsFatal(Exception error) => error is AppUnavailableException
        or RouteUnavailableException
        or ScopeNotReadyException { Readiness.IsConfigurationError: true };
}
