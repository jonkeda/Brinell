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
/// Makes a steady video from frames that come when the screen changes: from the first frame on, it
/// writes the latest frame at every tick of the frame rate, so a still screen is still in the video
/// and the video's time is the <see cref="RecordingClock"/>'s time since the first frame.
/// </summary>
public sealed class FrameVideo : IAsyncDisposable
{
    private readonly IFrameSink _sink;
    private readonly int _framesPerSecond;
    private readonly byte[] _latest;
    private readonly byte[] _writing;
    private readonly object _gate = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly TaskCompletionSource<long> _firstFrame = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<string> _warnings = [];
    private Task? _loop;
    private long _written;
    private Size _lastOffered;

    /// <summary>Creates a video of <paramref name="size"/> at <paramref name="framesPerSecond"/>.</summary>
    /// <param name="size">The video's size; a frame of another size is cropped or padded to it.</param>
    /// <param name="framesPerSecond">The video's frame rate.</param>
    /// <param name="sink">Where the frames go.</param>
    public FrameVideo(Size size, int framesPerSecond, IFrameSink sink)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(framesPerSecond, 1);
        Size = size;
        _framesPerSecond = framesPerSecond;
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _latest = new byte[size.Width * size.Height * 4];
        _writing = new byte[_latest.Length];
        _lastOffered = size;
    }

    /// <summary>The video's size.</summary>
    public Size Size { get; }

    /// <summary>When the first frame came, on the <see cref="RecordingClock"/>.</summary>
    public Task<long> FirstFrame => _firstFrame.Task;

    /// <summary>What went less well than asked.</summary>
    public IReadOnlyList<string> Warnings
    {
        get
        {
            lock (_gate)
            {
                return [.. _warnings];
            }
        }
    }

    /// <summary>A new frame; the first one starts the video at <paramref name="time"/>.</summary>
    /// <param name="time">When the frame was taken, on the <see cref="RecordingClock"/>.</param>
    /// <param name="bgra">Its pixels, top row first.</param>
    /// <param name="size">Its size.</param>
    /// <param name="stride">Bytes per row in <paramref name="bgra"/>.</param>
    public void Offer(long time, ReadOnlySpan<byte> bgra, Size size, int stride)
    {
        lock (_gate)
        {
            if (size != _lastOffered)
            {
                _warnings.Add($"The window changed size to {size.Width}x{size.Height}: the video stays {Size.Width}x{Size.Height}, cropped or padded.");
                _lastOffered = size;
                Array.Clear(_latest);
            }

            var rowBytes = Math.Min(size.Width, Size.Width) * 4;
            var rows = Math.Min(size.Height, Size.Height);
            for (var row = 0; row < rows; row++)
            {
                bgra.Slice(row * stride, rowBytes).CopyTo(_latest.AsSpan(row * Size.Width * 4, rowBytes));
            }
        }

        if (_firstFrame.TrySetResult(time))
        {
            _loop = Task.Run(() => WriteLoopAsync(time));
        }
    }

    /// <summary>Writes the frames up to now, finishes the file, and says what was made.</summary>
    /// <param name="path">The video file, for the result.</param>
    /// <param name="cancellationToken">Cancels waiting for the file.</param>
    public async Task<RecordingResult> StopAsync(string path, CancellationToken cancellationToken)
    {
        if (!_firstFrame.Task.IsCompleted)
        {
            throw new InvalidOperationException("Nothing was recorded: no frame came. A minimized window gives none.");
        }

        _stop.Cancel();
        await _loop!.ConfigureAwait(false);
        await _sink.CompleteAsync(cancellationToken).ConfigureAwait(false);
        var startedAt = await _firstFrame.Task.ConfigureAwait(false);
        return new RecordingResult(path, TimeSpan.FromSeconds((double)_written / _framesPerSecond), Size, startedAt, Warnings);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        if (_loop is not null)
        {
            await _loop.ConfigureAwait(false);
        }

        await _sink.DisposeAsync().ConfigureAwait(false);
        _stop.Dispose();
    }

    // Frame n is due n/fps after the first; a late tick writes the frames it missed, so the video's
    // time does not drift from the clock.
    private async Task WriteLoopAsync(long start)
    {
        var stopping = false;
        while (true)
        {
            var due = RecordingClock.Add(start, TimeSpan.FromSeconds((double)_written / _framesPerSecond));
            var wait = RecordingClock.Between(RecordingClock.Now, due);
            if (wait > TimeSpan.Zero && !stopping)
            {
                try
                {
                    await Task.Delay(wait, _stop.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    stopping = true;
                }
            }

            if (stopping && due > RecordingClock.Now)
            {
                return;
            }

            lock (_gate)
            {
                _latest.CopyTo(_writing, 0);
            }

            await _sink.WriteAsync(_writing, CancellationToken.None).ConfigureAwait(false);
            _written++;
            stopping |= _stop.IsCancellationRequested;
        }
    }
}
