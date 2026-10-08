using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using Brinell.Core.Recording;
using Brinell.Recording;

[assembly: ScreenRecorder(typeof(Brinell.Appium.AndroidRecorder), Order = 20)]

namespace Brinell.Appium;

/// <summary>
/// Records an Android device or emulator with <c>adb shell screenrecord</c>. screenrecord stops
/// after 3 minutes, so a long scene is recorded in segments, restarted as each ends, and joined
/// by ffmpeg; each join may miss a fraction of a second.
/// </summary>
public sealed partial class AndroidRecorder : IScreenRecorder
{
    /// <summary>The longest segment screenrecord makes.</summary>
    public static readonly TimeSpan SegmentLimit = TimeSpan.FromSeconds(180);

    /// <inheritdoc />
    public bool CanRecord(RecordingTarget target) => target is RecordingTarget.AndroidDevice && Adb() is not null;

    /// <inheritdoc />
    public async Task<IRecording> StartAsync(RecordingTarget target, RecordingOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (target is not RecordingTarget.AndroidDevice device || Adb() is not { } adb)
        {
            throw new NotSupportedException($"{nameof(AndroidRecorder)} records an Android device through adb, not {target}.");
        }

        var size = ParseSize(await RunAsync(adb, [.. Device(device.Serial), "shell", "wm", "size"], cancellationToken).ConfigureAwait(false));
        var recording = new Recording(adb, device.Serial, options.OutputPath, size);
        recording.StartSegment();
        return recording;
    }

    /// <summary>adb: <c>ANDROID_HOME</c>'s platform-tools, else on <c>PATH</c>; null when neither has it.</summary>
    public static string? Adb()
    {
        var exe = OperatingSystem.IsWindows() ? "adb.exe" : "adb";
        var home = Environment.GetEnvironmentVariable("ANDROID_HOME") ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
        if (home is { Length: > 0 } && Path.Combine(home, "platform-tools", exe) is var sdk && File.Exists(sdk))
        {
            return sdk;
        }

        return (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(folder => Path.Combine(folder.Trim('"'), exe))
            .FirstOrDefault(File.Exists);
    }

    /// <summary>The screen's size from <c>adb shell wm size</c>: the override when there is one.</summary>
    /// <param name="output">What <c>wm size</c> printed.</param>
    internal static Size ParseSize(string output)
    {
        var sizes = WmSize().Matches(output);
        return sizes.Count == 0
            ? Size.Empty
            : new Size(int.Parse(sizes[^1].Groups["w"].Value, CultureInfo.InvariantCulture), int.Parse(sizes[^1].Groups["h"].Value, CultureInfo.InvariantCulture));
    }

    /// <summary>ffmpeg's concat list for the segments, in order.</summary>
    /// <param name="segments">The segments' files.</param>
    internal static string ConcatList(IEnumerable<string> segments)
        => string.Concat(segments.Select(segment => $"file '{segment.Replace("'", "'\\''", StringComparison.Ordinal)}'\n"));

    private static string[] Device(string serial) => ["-s", serial];

    private static async Task<string> RunAsync(string adb, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var info = new ProcessStartInfo(adb) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        using var process = Process.Start(info) ?? throw new InvalidOperationException("adb did not start.");
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var errors = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode == 0
            ? output
            : throw new InvalidOperationException($"adb {string.Join(' ', arguments)} failed ({process.ExitCode}): {(output + errors).Trim()}");
    }

    [GeneratedRegex(@"size:\s*(?<w>\d+)x(?<h>\d+)")]
    private static partial Regex WmSize();

    private sealed class Recording(string adb, string serial, string outputPath, Size size) : IRecording
    {
        private readonly List<string> _remote = [];
        private readonly string _prefix = $"/sdcard/brinell-{Guid.NewGuid():N}";
        private readonly object _gate = new();
        private Process? _segment;
        private bool _stopping;

        public long StartedAt { get; private set; }

        // screenrecord ends a segment at its limit; the next starts at once, until Stop.
        public void StartSegment()
        {
            lock (_gate)
            {
                if (_stopping)
                {
                    return;
                }

                var remote = $"{_prefix}-{_remote.Count}.mp4";
                _remote.Add(remote);
                var info = new ProcessStartInfo(adb) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (var argument in (string[])[.. Device(serial), "shell", "screenrecord", "--time-limit", ((int)SegmentLimit.TotalSeconds).ToString(CultureInfo.InvariantCulture), remote])
                {
                    info.ArgumentList.Add(argument);
                }

                _segment = new Process { StartInfo = info, EnableRaisingEvents = true };
                _segment.Exited += (_, _) => StartSegment();
                _segment.Start();
                if (StartedAt == 0)
                {
                    StartedAt = RecordingClock.Now;
                }
            }
        }

        public async Task<RecordingResult> StopAsync(CancellationToken cancellationToken = default)
        {
            Process? segment;
            lock (_gate)
            {
                _stopping = true;
                segment = _segment;
            }

            var stoppedAt = RecordingClock.Now;

            // screenrecord finishes its file on SIGINT; killing adb would leave it unreadable. Only
            // this recording's screenrecord, by its file: "pkill screenrecord" also met another
            // one the shell may not signal and failed (measured on the emulator, 2026-10-07).
            await RunAsync(adb, [.. Device(serial), "shell", "pkill", "-2", "-f", _prefix], cancellationToken).ConfigureAwait(false);
            if (segment is not null)
            {
                await segment.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }

            var folder = Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(outputPath))!, Path.GetFileNameWithoutExtension(outputPath) + ".segments")).FullName;
            var local = new List<string>();
            foreach (var remote in _remote)
            {
                // adb cannot write a path past Windows' 260 characters (a long scenario title in a
                // deep results folder): it pulls into the short temp folder, and the file moves.
                var file = Path.Combine(folder, Path.GetFileName(remote));
                var pulled = Path.Combine(Path.GetTempPath(), Path.GetFileName(remote));
                await RunAsync(adb, [.. Device(serial), "pull", remote, pulled], cancellationToken).ConfigureAwait(false);
                File.Move(pulled, file, overwrite: true);
                await RunAsync(adb, [.. Device(serial), "shell", "rm", "-f", remote], cancellationToken).ConfigureAwait(false);
                local.Add(file);
            }

            var warnings = new List<string> { "The start is when adb began screenrecord, not its first frame: steps may be a frame or two early." };
            if (local.Count == 1)
            {
                File.Move(local[0], outputPath, overwrite: true);
            }
            else
            {
                var list = Path.Combine(folder, "segments.txt");
                await File.WriteAllTextAsync(list, ConcatList(local), cancellationToken).ConfigureAwait(false);
                await Ffmpeg.RunAsync(["-y", "-f", "concat", "-safe", "0", "-i", list, "-c", "copy", outputPath], cancellationToken).ConfigureAwait(false);
                warnings.Add($"Recorded in {local.Count} segments of up to {SegmentLimit.TotalMinutes:0} minutes, joined: each join may miss a fraction of a second.");
            }

            Directory.Delete(folder, recursive: true);
            // The screen in device pixels, which an element's bounds are in.
            return new RecordingResult(outputPath, RecordingClock.Between(StartedAt, stoppedAt), size, StartedAt, warnings)
            {
                Source = size.IsEmpty ? null : new Rectangle(Point.Empty, size),
            };
        }

        public ValueTask DisposeAsync()
        {
            lock (_gate)
            {
                _stopping = true;
                if (_segment is { HasExited: false } segment)
                {
                    segment.Kill();
                }

                _segment?.Dispose();
            }

            return ValueTask.CompletedTask;
        }
    }
}
