using System.Diagnostics;
using System.Drawing;

namespace Brinell.Core.Recording;

/// <summary>What a test did to the app, as a recorded video shows it.</summary>
public enum InteractionKind
{
    /// <summary>A tap or click: a button, a toggle, a row, a choice.</summary>
    Tap,

    /// <summary>Two taps.</summary>
    DoubleTap,

    /// <summary>A press held for a while.</summary>
    LongPress,

    /// <summary>A secondary click (Html, desktop).</summary>
    RightClick,

    /// <summary>The pointer over an element, nothing pressed (Html, desktop).</summary>
    Hover,

    /// <summary>A swipe or drag from one point to another; pull to refresh is one.</summary>
    Swipe,

    /// <summary>Text typed into a field.</summary>
    Type,

    /// <summary>A field emptied.</summary>
    Clear,

    /// <summary>A key or a navigation without a target: Enter, Back, Menu.</summary>
    Key,
}

/// <summary>
/// One interaction, taken just before it ran: its target's bounds while the screen still showed it.
/// Coordinates are the driver's own (screen, device or viewport pixels); the recording maps them to
/// the video.
/// </summary>
/// <param name="Kind">What was done.</param>
/// <param name="At">When, on the <see cref="Stopwatch"/> clock the recorders use.</param>
/// <param name="Bounds">The target's rectangle; empty for a <see cref="InteractionKind.Key"/> without one.</param>
public sealed record Interaction(InteractionKind Kind, long At, Rectangle Bounds)
{
    /// <summary>Where a swipe starts; null for anything else.</summary>
    public Point? From { get; init; }

    /// <summary>Where a swipe ends; null for anything else.</summary>
    public Point? To { get; init; }

    /// <summary>How long it lasted (a long press, a real swipe); zero when instant.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>The target's AutomationId or name, for reading the timeline; never drawn.</summary>
    public string? Label { get; init; }

    /// <summary>The key's name for a <see cref="InteractionKind.Key"/>, e.g. <c>Enter</c> or <c>Back</c>.</summary>
    public string? Key { get; init; }
}

/// <summary>Where a recording keeps the interactions it is told about.</summary>
public interface IInteractionLog
{
    /// <summary>Keeps one interaction; called from the test's thread.</summary>
    /// <param name="interaction">The interaction.</param>
    void Add(Interaction interaction);
}

/// <summary>
/// The interactions of the scene being recorded. A driver's element reports each action here just
/// before it runs it; with no recording, <see cref="Log"/> is null and a report costs a null check
/// (AD-024: a run that does not record pays nothing).
/// </summary>
public static class Interactions
{
    private static IInteractionLog? _log;

    // An action that runs another (a double tap as a gesture, an Invoke as a Click) is one interaction.
    [ThreadStatic]
    private static int _depth;

    /// <summary>The log of the scene being recorded; null when nothing records.</summary>
    public static IInteractionLog? Log => Volatile.Read(ref _log);

    /// <summary>Sends the interactions to <paramref name="log"/> until the returned scope is disposed.</summary>
    /// <param name="log">The recording's log.</param>
    public static IDisposable Begin(IInteractionLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        var previous = Interlocked.Exchange(ref _log, log);
        return new Restore(log, previous);
    }

    /// <summary>
    /// Reports an action about to run, unless another action is already reporting it. Dispose the
    /// scope when the action has run. Nothing is read when nothing records.
    /// </summary>
    /// <param name="kind">What is done.</param>
    /// <param name="bounds">Reads the target's rectangle; only called while recording.</param>
    /// <param name="label">Reads the target's AutomationId or name; only called while recording.</param>
    /// <param name="from">Where a swipe starts.</param>
    /// <param name="to">Where a swipe ends.</param>
    /// <param name="duration">How long the action lasts.</param>
    /// <param name="key">The key's name, for <see cref="InteractionKind.Key"/>.</param>
    public static Scope Report(InteractionKind kind, Func<Rectangle> bounds, Func<string?>? label = null, Point? from = null, Point? to = null, TimeSpan duration = default, string? key = null)
        => Report(kind, bounds, label, _ => (from, to), duration, key);

    /// <summary>
    /// Reports a swipe that has a direction but no points (a bridge's <c>SwipeDown</c>): it is drawn
    /// across the middle half of its target, in that direction.
    /// </summary>
    /// <param name="bounds">Reads the target's rectangle; only called while recording.</param>
    /// <param name="dx">-1, 0 or 1: left or right.</param>
    /// <param name="dy">-1, 0 or 1: up or down.</param>
    /// <param name="label">Reads the target's AutomationId or name; only called while recording.</param>
    public static Scope ReportSwipe(Func<Rectangle> bounds, int dx, int dy, Func<string?>? label = null)
        => Report(InteractionKind.Swipe, bounds, label, rectangle => (Across(rectangle, -dx, -dy), Across(rectangle, dx, dy)), default, null);

    private static Scope Report(InteractionKind kind, Func<Rectangle> bounds, Func<string?>? label, Func<Rectangle, (Point? From, Point? To)> path, TimeSpan duration, string? key)
    {
        ArgumentNullException.ThrowIfNull(bounds);
        if (Log is not { } log)
        {
            return default;
        }

        if (_depth++ == 0 && TryRead(bounds, out var rectangle))
        {
            var (from, to) = path(rectangle);
            var name = label is not null && TryRead(label, out var read) ? read : null;
            log.Add(new Interaction(kind, Stopwatch.GetTimestamp(), rectangle) { From = from, To = to, Duration = duration, Label = name, Key = key });
        }

        return new Scope(counted: true);
    }

    // A point a quarter of the way out from the centre, in a direction.
    private static Point Across(Rectangle bounds, int dx, int dy)
        => new(bounds.X + (bounds.Width / 2) + (dx * bounds.Width / 4), bounds.Y + (bounds.Height / 2) + (dy * bounds.Height / 4));

    // A target that cannot be read (the element went stale) leaves no mark: a recording never fails a step.
    private static bool TryRead<T>(Func<T> read, out T value)
    {
        try
        {
            value = read();
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Debug.WriteLine($"Interactions: the target could not be read ({exception.Message}); no mark.");
            value = default!;
            return false;
        }
    }

    /// <summary>An action being reported; dispose it once the action has run.</summary>
    public readonly struct Scope : IDisposable
    {
        private readonly bool _counted;

        internal Scope(bool counted) => _counted = counted;

        /// <inheritdoc />
        public void Dispose()
        {
            if (_counted)
            {
                _depth--;
            }
        }
    }

    private sealed class Restore(IInteractionLog log, IInteractionLog? previous) : IDisposable
    {
        public void Dispose() => Interlocked.CompareExchange(ref _log, previous, log);
    }
}
