namespace Bruinell.Recording;

/// <summary>
/// Makes a steady video from frames that come when the screen changes: from the first frame on, it
/// writes the latest frame at every tick of the frame rate, so a still screen is still in the video
/// and the video's time is the <see cref="RecordingClock"/>'s time since the first frame.
/// </summary>
public abstract class PacedVideo : IAsyncDisposable
{
    private readonly IFrameSink _sink;
    private readonly int _framesPerSecond;
    private readonly CancellationTokenSource _stop = new();
    private readonly TaskCompletionSource<long> _firstFrame = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<string> _warnings = [];
    private Task? _loop;
    private long _written;

    /// <summary>Creates a video at <paramref name="framesPerSecond"/> into <paramref name="sink"/>.</summary>
    /// <param name="framesPerSecond">The video's frame rate.</param>
    /// <param name="sink">Where the frames go.</param>
    protected PacedVideo(int framesPerSecond, IFrameSink sink)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(framesPerSecond, 1);
        _framesPerSecond = framesPerSecond;
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    /// <summary>The video's size.</summary>
    public abstract Size Size { get; }

    /// <summary>When the first frame came, on the <see cref="RecordingClock"/>.</summary>
    public Task<long> FirstFrame => _firstFrame.Task;

    /// <summary>What went less well than asked.</summary>
    public IReadOnlyList<string> Warnings
    {
        get
        {
            lock (Gate)
            {
                return [.. _warnings];
            }
        }
    }

    /// <summary>Held while the latest frame changes or is taken.</summary>
    protected object Gate { get; } = new();

    /// <summary>Why nothing was recorded, when no frame came.</summary>
    protected virtual string NoFrame => "Nothing was recorded: no frame came.";

    /// <summary>Writes the frames up to now, finishes the file, and says what was made.</summary>
    /// <param name="path">The video file, for the result.</param>
    /// <param name="cancellationToken">Cancels waiting for the file.</param>
    public async Task<RecordingResult> StopAsync(string path, CancellationToken cancellationToken)
    {
        if (!_firstFrame.Task.IsCompleted)
        {
            throw new InvalidOperationException(NoFrame);
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
        GC.SuppressFinalize(this);
    }

    /// <summary>The latest frame, as the sink takes it; called while <see cref="Gate"/> is held.</summary>
    protected abstract ReadOnlyMemory<byte> TakeLatest();

    /// <summary>Notes what went less well; called while <see cref="Gate"/> is held.</summary>
    /// <param name="warning">What happened.</param>
    protected void Warn(string warning) => _warnings.Add(warning);

    /// <summary>A frame came: the first one starts the video at <paramref name="time"/>.</summary>
    /// <param name="time">When the frame was taken, on the <see cref="RecordingClock"/>.</param>
    protected void Arrived(long time)
    {
        if (_firstFrame.TrySetResult(time))
        {
            _loop = Task.Run(() => WriteLoopAsync(time));
        }
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

            ReadOnlyMemory<byte> frame;
            lock (Gate)
            {
                frame = TakeLatest();
            }

            await _sink.WriteAsync(frame, CancellationToken.None).ConfigureAwait(false);
            _written++;
            stopping |= _stop.IsCancellationRequested;
        }
    }
}

/// <summary>A <see cref="PacedVideo"/> of raw frames (32-bit BGRA) of one size.</summary>
public sealed class FrameVideo : PacedVideo
{
    private readonly byte[] _latest;
    private readonly byte[] _writing;
    private Size _lastOffered;

    /// <summary>Creates a video of <paramref name="size"/> at <paramref name="framesPerSecond"/>.</summary>
    /// <param name="size">The video's size; a frame of another size is cropped or padded to it.</param>
    /// <param name="framesPerSecond">The video's frame rate.</param>
    /// <param name="sink">Where the frames go.</param>
    public FrameVideo(Size size, int framesPerSecond, IFrameSink sink)
        : base(framesPerSecond, sink)
    {
        Size = size;
        _latest = new byte[size.Width * size.Height * 4];
        _writing = new byte[_latest.Length];
        _lastOffered = size;
    }

    /// <inheritdoc />
    public override Size Size { get; }

    /// <inheritdoc />
    protected override string NoFrame => "Nothing was recorded: no frame came. A minimized window gives none.";

    /// <summary>A new frame; the first one starts the video at <paramref name="time"/>.</summary>
    /// <param name="time">When the frame was taken, on the <see cref="RecordingClock"/>.</param>
    /// <param name="bgra">Its pixels, top row first.</param>
    /// <param name="size">Its size.</param>
    /// <param name="stride">Bytes per row in <paramref name="bgra"/>.</param>
    public void Offer(long time, ReadOnlySpan<byte> bgra, Size size, int stride)
    {
        lock (Gate)
        {
            if (size != _lastOffered)
            {
                Warn($"The window changed size to {size.Width}x{size.Height}: the video stays {Size.Width}x{Size.Height}, cropped or padded.");
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

        Arrived(time);
    }

    /// <inheritdoc />
    protected override ReadOnlyMemory<byte> TakeLatest()
    {
        _latest.CopyTo(_writing, 0);
        return _writing;
    }
}

/// <summary>
/// A <see cref="PacedVideo"/> of whole encoded images (JPEG or PNG), such as a browser's
/// screencast; ffmpeg decodes and scales them (<see cref="FfmpegImageSink"/>).
/// </summary>
public sealed class ImageVideo : PacedVideo
{
    private byte[] _latest = [];

    /// <summary>Creates a video of <paramref name="size"/> at <paramref name="framesPerSecond"/>.</summary>
    /// <param name="size">The video's size; images are scaled to it.</param>
    /// <param name="framesPerSecond">The video's frame rate.</param>
    /// <param name="sink">Where the images go.</param>
    public ImageVideo(Size size, int framesPerSecond, IFrameSink sink)
        : base(framesPerSecond, sink)
    {
        Size = size;
    }

    /// <inheritdoc />
    public override Size Size { get; }

    /// <summary>A new image; the first one starts the video at <paramref name="time"/>.</summary>
    /// <param name="time">When the image was taken, on the <see cref="RecordingClock"/>.</param>
    /// <param name="image">The encoded image; kept, not copied.</param>
    public void Offer(long time, byte[] image)
    {
        ArgumentNullException.ThrowIfNull(image);
        lock (Gate)
        {
            _latest = image;
        }

        Arrived(time);
    }

    /// <inheritdoc />
    protected override ReadOnlyMemory<byte> TakeLatest() => _latest;
}
