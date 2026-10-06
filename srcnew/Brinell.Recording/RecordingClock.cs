namespace Brinell.Recording;

/// <summary>
/// The one clock of a recording: <see cref="Stopwatch"/> ticks. A step's time on it is its time in
/// the video, because every recorder converts its frame times to it.
/// </summary>
public static class RecordingClock
{
    /// <summary>Now, in <see cref="Stopwatch"/> ticks.</summary>
    public static long Now => Stopwatch.GetTimestamp();

    /// <summary>The time from <paramref name="start"/> to <paramref name="time"/>.</summary>
    /// <param name="start">The earlier time, in <see cref="Stopwatch"/> ticks.</param>
    /// <param name="time">The later time, in <see cref="Stopwatch"/> ticks.</param>
    public static TimeSpan Between(long start, long time) => Stopwatch.GetElapsedTime(start, time);

    /// <summary>A time <paramref name="offset"/> after <paramref name="start"/>, in <see cref="Stopwatch"/> ticks.</summary>
    /// <param name="start">The time, in <see cref="Stopwatch"/> ticks.</param>
    /// <param name="offset">How much later.</param>
    public static long Add(long start, TimeSpan offset)
        => start + (long)((decimal)offset.Ticks * Stopwatch.Frequency / TimeSpan.TicksPerSecond);

    /// <summary>
    /// A time from the system's performance counter as a <see cref="TimeSpan"/> (Windows'
    /// <c>SystemRelativeTime</c>), in <see cref="Stopwatch"/> ticks. On Windows the stopwatch reads
    /// the same counter, so the two agree.
    /// </summary>
    /// <param name="systemRelativeTime">The performance counter's time.</param>
    public static long FromSystemRelativeTime(TimeSpan systemRelativeTime) => Add(0, systemRelativeTime);
}
