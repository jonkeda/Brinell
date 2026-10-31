namespace Bruinet.Recording;

/// <summary>Records one kind of <see cref="RecordingTarget"/> to a video file.</summary>
public interface IScreenRecorder
{
    /// <summary>Whether this recorder can record the target, on this machine.</summary>
    /// <param name="target">What to record.</param>
    bool CanRecord(RecordingTarget target);

    /// <summary>Starts recording; the frames are timed on the <see cref="RecordingClock"/>.</summary>
    /// <param name="target">What to record.</param>
    /// <param name="options">Where the video goes, and how.</param>
    /// <param name="cancellationToken">Cancels the start.</param>
    Task<IRecording> StartAsync(RecordingTarget target, RecordingOptions options, CancellationToken cancellationToken = default);
}

/// <summary>A recording in progress.</summary>
public interface IRecording : IAsyncDisposable
{
    /// <summary>When the first frame was taken, on the <see cref="RecordingClock"/>.</summary>
    long StartedAt { get; }

    /// <summary>Stops, finishes the file, and says what was recorded.</summary>
    /// <param name="cancellationToken">Cancels waiting for the encoder.</param>
    Task<RecordingResult> StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>Where a recording goes, and how.</summary>
/// <param name="OutputPath">The video file (MP4).</param>
/// <param name="FramesPerSecond">Frames per second of the video.</param>
/// <param name="Cursor">Whether the mouse pointer is recorded.</param>
public sealed record RecordingOptions(string OutputPath, int FramesPerSecond = 30, bool Cursor = false);

/// <summary>What a recording made.</summary>
/// <param name="Path">The video file.</param>
/// <param name="Duration">How long the video is.</param>
/// <param name="Size">The video's size in pixels.</param>
/// <param name="StartedAt">When its first frame was taken, on the <see cref="RecordingClock"/>.</param>
/// <param name="Warnings">What went less well than asked: frames repeated, a size change, a clock estimate.</param>
public sealed record RecordingResult(string Path, TimeSpan Duration, Size Size, long StartedAt, IReadOnlyList<string> Warnings)
{
    /// <summary>
    /// The area of the driver's coordinates (screen, device or viewport pixels) the frames show, so
    /// an element's bounds can be found in the video; null when they are the frame's own pixels.
    /// </summary>
    public Rectangle? Source { get; init; }
}
