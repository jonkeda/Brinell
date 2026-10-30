using System.Diagnostics;

namespace Bruinet.Core.Recording;

/// <summary>
/// The pause a recording asks of each action, so a video shows what the test did: a list that
/// opens is seen open, three actions in one step are three things. An action takes at least
/// <see cref="Pause"/>; one that took longer waits nothing more. An action that runs another is
/// paced once. With no recording, <see cref="Pause"/> is null and an action costs a null check
/// (AD-024: a run that does not record pays nothing).
/// </summary>
/// <remarks>
/// Scenes in two collections can record at once and end in either order, so each scene's pause
/// is kept until that scene ends, and the longest of those in force holds.
/// </remarks>
public static class ActionPacing
{
    private static readonly object Gate = new();

    // The pauses of the scenes recording now; the longest is copied to _pauseTicks for the reads.
    private static readonly List<Scope> Active = [];
    private static long _pauseTicks;

    [ThreadStatic]
    private static int _depth;

    /// <summary>The least time an action takes while a video records; null when none does.</summary>
    public static TimeSpan? Pause => Volatile.Read(ref _pauseTicks) is > 0 and var ticks ? TimeSpan.FromTicks(ticks) : null;

    /// <summary>Paces every action at <paramref name="pause"/> or more until the returned scope is disposed.</summary>
    /// <param name="pause">The least time an action takes; zero or less paces nothing.</param>
    public static IDisposable Begin(TimeSpan pause)
    {
        var scope = new Scope(Math.Max(0, pause.Ticks));
        lock (Gate)
        {
            Active.Add(scope);
            Update();
        }

        return scope;
    }

    /// <summary>An action begins: its start, or 0 when it is not paced (nothing records, or another action runs it).</summary>
    internal static long Start()
    {
        _depth++;
        return _depth == 1 && Volatile.Read(ref _pauseTicks) > 0 ? Stopwatch.GetTimestamp() : 0;
    }

    /// <summary>An action ended; when it succeeded and is paced, waits out the rest of the pause.</summary>
    /// <param name="started">What <see cref="Start"/> gave.</param>
    /// <param name="succeeded">Whether the action ran; a failed one is not waited for.</param>
    internal static void End(long started, bool succeeded)
    {
        _depth--;
        if (started == 0 || !succeeded)
        {
            return;
        }

        var left = TimeSpan.FromTicks(Volatile.Read(ref _pauseTicks)) - Stopwatch.GetElapsedTime(started);
        if (left > TimeSpan.Zero)
        {
            Thread.Sleep(left);
        }
    }

    // Under the gate.
    private static void Update()
        => Volatile.Write(ref _pauseTicks, Active.Count == 0 ? 0 : Active.Max(scope => scope.Ticks));

    private sealed class Scope(long ticks) : IDisposable
    {
        public long Ticks { get; } = ticks;

        public void Dispose()
        {
            lock (Gate)
            {
                if (Active.Remove(this))
                {
                    Update();
                }
            }
        }
    }
}
