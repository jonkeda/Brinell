namespace Brinell.Maui.UITests;

/// <summary>
/// Step 16: which apps under test are running right now, and since when.
/// </summary>
/// <remarks>
/// <para>
/// <b>Because "the collections ran in parallel" is not observable from inside one of them.</b>
/// A test can see its own fixture and nothing else. The runner's summary is no help either: a
/// suite that serialised perfectly and a suite that overlapped perfectly report the same passes,
/// and the wall clock only says something if you already know what serial would have cost. So
/// each fixture announces itself here while its app is up, and a test on the other side can ask
/// whether it is there.
/// </para>
/// <para>
/// <b>Intervals, not a flag and not a snapshot.</b> An event that is set and never cleared answers
/// "did the other collection ever run", which a serialised run answers yes to: the first
/// collection finishes, leaves its flag set, and the second reads it as company. Asking instead
/// whether the other app is up <i>at this instant</i> fixes that and breaks something else - the
/// two collections are wildly different lengths, the Shell one is a single test and is gone in two
/// seconds, and a hub test scheduled a minute in would find an empty room and call it
/// serialisation. So each side records when it arrived and when it left, and the question is
/// whether the two stays overlapped. That is the claim step 16 makes, it is true whenever it
/// happened, and it is false in a serialised run for the right reason.
/// </para>
/// <para>
/// <b>Announced by the fixture, not by the test.</b> A fixture is built once, at the start of its
/// collection; a test runs at whatever position it was scheduled. Signalling from a test body
/// would mean the other side had to wait out however many tests came before it - minutes on a
/// full run, and a timeout that would have to be long enough to cover the whole suite to avoid
/// being a source of flakes itself.
/// </para>
/// <para>
/// Lives at the project root rather than under <c>Tests/</c> because the fixtures reference it,
/// and the fixtures are shared with the mobile head. There it simply records and is never asked:
/// that head serialises its collections unconditionally, because an emulator is not a desktop.
/// </para>
/// </remarks>
internal static class ParallelismProbe
{
    /// <summary>The hub sample app, driven by <see cref="MauiFixture"/>.</summary>
    internal const string Hub = "Hub";

    /// <summary>The Shell sample app, driven by <see cref="ShellFixture"/>.</summary>
    internal const string Shell = "Shell";

    private static readonly object Gate = new();

    /// <summary>Side name to its app's stay. Entries are kept after the app closes.</summary>
    private static readonly Dictionary<string, Appearance> Appearances = new();

    /// <summary>
    /// Records that <paramref name="side"/>'s app is up.
    /// </summary>
    /// <param name="side">One of <see cref="Hub"/> or <see cref="Shell"/>.</param>
    /// <param name="token">
    /// Something that distinguishes this app from the other one - the window handle, in practice.
    /// It is what turns "two fixtures" into "two apps" for anyone reading the assertion.
    /// </param>
    internal static void Enter(string side, string token)
    {
        lock (Gate)
        {
            Appearances[side] = new Appearance(token, Environment.TickCount64);
            Monitor.PulseAll(Gate);
        }
    }

    /// <summary>
    /// Records that <paramref name="side"/>'s app is gone.
    /// </summary>
    /// <remarks>
    /// Called after the app is closed rather than before, so the window named by the token is
    /// still on screen for as long as this says it was.
    /// </remarks>
    /// <param name="side">One of <see cref="Hub"/> or <see cref="Shell"/>.</param>
    internal static void Leave(string side)
    {
        lock (Gate)
        {
            if (Appearances.TryGetValue(side, out var mine))
            {
                mine.LeftAt = Environment.TickCount64;
            }

            Monitor.PulseAll(Gate);
        }
    }

    /// <summary>
    /// Waits until the other side's app has been up at the same time as this one, and reports it.
    /// </summary>
    /// <remarks>
    /// The caller's own app is still running - a test is what is asking - so its stay has no end
    /// yet. The other side therefore overlapped it if it is still up, or if it left after this
    /// side arrived. Waiting is for the case where it has not arrived yet.
    /// </remarks>
    /// <param name="side">The caller's own side.</param>
    /// <param name="timeoutMs">How long to wait before concluding it is not coming.</param>
    /// <returns>
    /// The other side's token, or <see langword="null"/> if the two stays never overlapped -
    /// which is what a serialised run looks like from here.
    /// </returns>
    internal static string? WaitForOverlap(string side, int timeoutMs)
    {
        var otherSide = side == Hub ? Shell : Hub;
        var deadline = Environment.TickCount64 + timeoutMs;

        lock (Gate)
        {
            if (!Appearances.TryGetValue(side, out var mine))
            {
                throw new InvalidOperationException(
                    $"{side} asked about the other app before its own fixture announced itself. "
                    + "Enter is called from the fixture constructor, so this means the question "
                    + "was asked from somewhere a fixture does not reach.");
            }

            while (true)
            {
                if (Appearances.TryGetValue(otherSide, out var other)
                    && (other.LeftAt is null || other.LeftAt > mine.EnteredAt))
                {
                    return other.Token;
                }

                var remaining = (int)(deadline - Environment.TickCount64);
                if (remaining <= 0 || !Monitor.Wait(Gate, remaining))
                {
                    return null;
                }
            }
        }
    }

    /// <summary>One app's stay: what it was, when it arrived, and when it left.</summary>
    /// <param name="Token">Distinguishes this app from the other one.</param>
    /// <param name="EnteredAt">Tick count when the app came up.</param>
    private sealed record Appearance(string Token, long EnteredAt)
    {
        /// <summary>Tick count when the app closed, or null while it is still running.</summary>
        internal long? LeftAt { get; set; }
    }
}
