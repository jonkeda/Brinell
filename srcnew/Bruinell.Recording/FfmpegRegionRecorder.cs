using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Bruinell.Recording;

/// <summary>
/// The last resort: an area of the screen through ffmpeg's <c>gdigrab</c>. It records whatever covers
/// the area, so the app has to be in front; a window recorder does not need that.
/// </summary>
public sealed class FfmpegRegionRecorder : IScreenRecorder
{
    /// <inheritdoc />
    public bool CanRecord(RecordingTarget target) => OperatingSystem.IsWindows() && target is RecordingTarget.Monitor;

    /// <inheritdoc />
    public async Task<IRecording> StartAsync(RecordingTarget target, RecordingOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!OperatingSystem.IsWindows() || target is not RecordingTarget.Monitor monitor)
        {
            throw new NotSupportedException($"{nameof(FfmpegRegionRecorder)} records an area of a Windows screen, not {target}.");
        }

        var area = monitor.Crop ?? MonitorArea(monitor.Handle);
        var size = FfmpegProcess.Even(area.Size);
        var ffmpeg = FfmpegProcess.Start(
        [
            "-f", "gdigrab",
            "-framerate", options.FramesPerSecond.ToString(CultureInfo.InvariantCulture),
            "-draw_mouse", options.Cursor ? "1" : "0",
            "-offset_x", area.X.ToString(CultureInfo.InvariantCulture),
            "-offset_y", area.Y.ToString(CultureInfo.InvariantCulture),
            "-video_size", $"{size.Width}x{size.Height}",
            "-i", "desktop",
            .. FfmpegProcess.Mp4Output(options.OutputPath),
        ], input: true);

        try
        {
            var startedAt = await ffmpeg.InputOpened.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
            return new Recording(ffmpeg, options.OutputPath, size, startedAt);
        }
        catch
        {
            await ffmpeg.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    [SupportedOSPlatform("windows")]
    private static Rectangle MonitorArea(nint monitor)
    {
        if (monitor == 0)
        {
            return new Rectangle(
                GetSystemMetrics(VirtualScreenX),
                GetSystemMetrics(VirtualScreenY),
                GetSystemMetrics(VirtualScreenWidth),
                GetSystemMetrics(VirtualScreenHeight));
        }

        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        return GetMonitorInfo(monitor, ref info)
            ? Rectangle.FromLTRB(info.Monitor.Left, info.Monitor.Top, info.Monitor.Right, info.Monitor.Bottom)
            : throw new ArgumentException($"No monitor has the handle {monitor}.", nameof(monitor));
    }

    private const int VirtualScreenX = 76;
    private const int VirtualScreenY = 77;
    private const int VirtualScreenWidth = 78;
    private const int VirtualScreenHeight = 79;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }

    private sealed class Recording(FfmpegProcess ffmpeg, string path, Size size, long startedAt) : IRecording
    {
        private RecordingResult? _result;

        public long StartedAt { get; } = startedAt;

        public async Task<RecordingResult> StopAsync(CancellationToken cancellationToken = default)
        {
            if (_result is null)
            {
                var stoppedAt = RecordingClock.Now;
                await ffmpeg.QuitAsync(cancellationToken).ConfigureAwait(false);
                _result = new RecordingResult(path, RecordingClock.Between(StartedAt, stoppedAt), size, StartedAt,
                    ["The start is when ffmpeg opened the screen, not its first frame: steps may be a frame or two early."]);
            }

            return _result;
        }

        public ValueTask DisposeAsync() => ffmpeg.DisposeAsync();
    }
}
