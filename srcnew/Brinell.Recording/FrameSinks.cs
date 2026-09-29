using System.Globalization;

namespace Brinell.Recording;

/// <summary>Where a <see cref="FrameVideo"/> writes its frames: 32-bit BGRA, top row first, no padding.</summary>
public interface IFrameSink : IAsyncDisposable
{
    /// <summary>Writes one frame.</summary>
    /// <param name="bgra">The frame's pixels.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    ValueTask WriteAsync(ReadOnlyMemory<byte> bgra, CancellationToken cancellationToken);

    /// <summary>The last frame was written: finish the file.</summary>
    /// <param name="cancellationToken">Cancels waiting for the file.</param>
    Task CompleteAsync(CancellationToken cancellationToken);
}

/// <summary>Raw frames to an H.264 MP4, through ffmpeg's input pipe.</summary>
public sealed class FfmpegFrameSink : IFrameSink
{
    private readonly FfmpegProcess _ffmpeg;

    /// <summary>Starts ffmpeg, waiting for frames of <paramref name="size"/> at <paramref name="framesPerSecond"/>.</summary>
    /// <param name="path">The video file.</param>
    /// <param name="size">The frames' size; even width and height.</param>
    /// <param name="framesPerSecond">The video's frame rate.</param>
    public FfmpegFrameSink(string path, Size size, int framesPerSecond)
    {
        _ffmpeg = FfmpegProcess.Start(
        [
            "-f", "rawvideo",
            "-pix_fmt", "bgra",
            "-video_size", $"{size.Width}x{size.Height}",
            "-framerate", framesPerSecond.ToString(CultureInfo.InvariantCulture),
            "-i", "-",
            .. FfmpegProcess.Mp4Output(path),
        ], input: true);
    }

    /// <inheritdoc />
    public ValueTask WriteAsync(ReadOnlyMemory<byte> bgra, CancellationToken cancellationToken)
        => _ffmpeg.Input.WriteAsync(bgra, cancellationToken);

    /// <inheritdoc />
    public Task CompleteAsync(CancellationToken cancellationToken) => _ffmpeg.EndInputAsync(cancellationToken);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _ffmpeg.DisposeAsync();
}

/// <summary>
/// Encoded images (JPEG or PNG) to an H.264 MP4, through ffmpeg's image pipe; each is scaled and
/// padded to the video's size, so a page that changes size still makes one video.
/// </summary>
public sealed class FfmpegImageSink : IFrameSink
{
    private readonly FfmpegProcess _ffmpeg;

    /// <summary>Starts ffmpeg, waiting for images at <paramref name="framesPerSecond"/>.</summary>
    /// <param name="path">The video file.</param>
    /// <param name="size">The video's size; even width and height.</param>
    /// <param name="framesPerSecond">The video's frame rate.</param>
    public FfmpegImageSink(string path, Size size, int framesPerSecond)
    {
        _ffmpeg = FfmpegProcess.Start(
        [
            "-f", "image2pipe",
            "-framerate", framesPerSecond.ToString(CultureInfo.InvariantCulture),
            "-i", "-",
            "-vf", $"scale={size.Width}:{size.Height}:force_original_aspect_ratio=decrease,pad={size.Width}:{size.Height}:(ow-iw)/2:(oh-ih)/2",
            .. FfmpegProcess.Mp4Output(path),
        ], input: true);
    }

    /// <inheritdoc />
    public ValueTask WriteAsync(ReadOnlyMemory<byte> bgra, CancellationToken cancellationToken)
        => _ffmpeg.Input.WriteAsync(bgra, cancellationToken);

    /// <inheritdoc />
    public Task CompleteAsync(CancellationToken cancellationToken) => _ffmpeg.EndInputAsync(cancellationToken);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _ffmpeg.DisposeAsync();
}
