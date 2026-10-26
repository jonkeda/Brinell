using System.Text;

namespace Bruinet.Recording;

/// <summary>
/// Where ffmpeg is, and running it. Every recording needs it: to encode frames, lay in the voice and
/// join the scenes. <c>BRUINET_FFMPEG</c> names the executable; otherwise it is looked for on <c>PATH</c>.
/// </summary>
public static class Ffmpeg
{
    /// <summary>The variable that names ffmpeg's executable.</summary>
    public const string Variable = "BRUINET_FFMPEG";

    /// <summary>What to tell someone who has no ffmpeg.</summary>
    public const string HowToGet =
        "Recording needs ffmpeg. Install it (Windows: winget install Gyan.FFmpeg.Essentials; macOS: brew install ffmpeg; Linux: your package manager), " +
        "then open a new terminal so it is on PATH, or set " + Variable + " to ffmpeg's executable.";

    /// <summary>ffmpeg's executable; null when it cannot be found.</summary>
    /// <param name="read">Reads a process variable; <see cref="Environment.GetEnvironmentVariable(string)"/> by default.</param>
    public static string? Find(Func<string, string?>? read = null)
    {
        read ??= Environment.GetEnvironmentVariable;
        if (read(Variable) is { Length: > 0 } named)
        {
            return File.Exists(named) ? named : null;
        }

        var exe = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        return (read("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(folder => Path.Combine(folder.Trim('"'), exe))
            .FirstOrDefault(File.Exists);
    }

    /// <summary>ffmpeg's executable, or <see cref="FfmpegNotFoundException"/> saying how to get it.</summary>
    public static string Require() => Find() ?? throw new FfmpegNotFoundException();

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> Filters = new(StringComparer.Ordinal);

    /// <summary>
    /// Whether this ffmpeg has a filter, e.g. <c>subtitles</c>, which needs a build with libass. Asked
    /// once per filter and process.
    /// </summary>
    /// <param name="name">The filter's name.</param>
    public static bool HasFilter(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return Filters.GetOrAdd(name, filter =>
        {
            var info = new ProcessStartInfo(Require(), "-hide_banner -filters")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = Process.Start(info) ?? throw new FfmpegException("ffmpeg did not start.");
            var errors = process.StandardError.ReadToEndAsync();
            var listed = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            _ = errors.GetAwaiter().GetResult();

            // A line per filter: " TSC name   V->V   description".
            return listed.Split('\n').Any(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries) is [_, var listedName, ..] && listedName == filter);
        });
    }

    /// <summary>Runs ffmpeg to the end; throws with the end of its output when it fails.</summary>
    /// <param name="arguments">Its arguments, one per item.</param>
    /// <param name="cancellationToken">Stops it.</param>
    public static async Task RunAsync(IEnumerable<string> arguments, CancellationToken cancellationToken = default)
    {
        await using var process = FfmpegProcess.Start(arguments, input: false);
        await process.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>ffmpeg is not installed, or not where <c>BRUINET_FFMPEG</c> says.</summary>
public sealed class FfmpegNotFoundException() : InvalidOperationException(Ffmpeg.HowToGet);

/// <summary>ffmpeg failed; the message ends with what it said last.</summary>
/// <param name="message">What failed, and ffmpeg's last lines.</param>
public sealed class FfmpegException(string message) : InvalidOperationException(message);

/// <summary>One ffmpeg process: its input pipe, and its output kept for when it fails.</summary>
internal sealed class FfmpegProcess : IAsyncDisposable
{
    private const int KeptLines = 30;

    private readonly Process _process;
    private readonly Queue<string> _lines = new();
    private readonly Task _reading;
    private readonly TaskCompletionSource<long> _inputOpened = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private FfmpegProcess(Process process)
    {
        _process = process;
        _reading = Task.Run(ReadErrorsAsync);
    }

    /// <summary>ffmpeg's input, for raw frames; only when started with <c>input</c>.</summary>
    public Stream Input => _process.StandardInput.BaseStream;

    /// <summary>When ffmpeg said it had opened its input (<c>Input #0</c>), on the <see cref="RecordingClock"/>.</summary>
    public Task<long> InputOpened => _inputOpened.Task;

    public static FfmpegProcess Start(IEnumerable<string> arguments, bool input)
    {
        var info = new ProcessStartInfo(Ffmpeg.Require())
        {
            RedirectStandardInput = true,
            RedirectStandardError = true,
            RedirectStandardOutput = false,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in new[] { "-hide_banner", "-nostats", "-loglevel", "info" }.Concat(arguments))
        {
            info.ArgumentList.Add(argument);
        }

        var process = Process.Start(info) ?? throw new FfmpegException("ffmpeg did not start.");
        if (!input)
        {
            process.StandardInput.Close();
        }

        return new FfmpegProcess(process);
    }

    /// <summary>Asks a grabbing ffmpeg to stop (<c>q</c>) and waits for it to finish the file.</summary>
    public async Task QuitAsync(CancellationToken cancellationToken)
    {
        if (!_process.HasExited)
        {
            await _process.StandardInput.WriteAsync("q").ConfigureAwait(false);
            await _process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
            _process.StandardInput.Close();
        }

        await WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Ends the input (the last frame was written) and waits for ffmpeg to finish the file.</summary>
    public async Task EndInputAsync(CancellationToken cancellationToken)
    {
        await Input.FlushAsync(cancellationToken).ConfigureAwait(false);
        _process.StandardInput.Close();
        await WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        await _process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        await _reading.ConfigureAwait(false);
        if (_process.ExitCode != 0)
        {
            throw new FfmpegException($"ffmpeg exited with {_process.ExitCode}:{Environment.NewLine}{Output()}");
        }
    }

    /// <summary>ffmpeg's last lines of output.</summary>
    public string Output()
    {
        lock (_lines)
        {
            return string.Join(Environment.NewLine, _lines);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync().ConfigureAwait(false);
        }

        _process.Dispose();
    }

    private async Task ReadErrorsAsync()
    {
        var reader = _process.StandardError;
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            if (line.StartsWith("Input #0", StringComparison.Ordinal))
            {
                _inputOpened.TrySetResult(RecordingClock.Now);
            }

            lock (_lines)
            {
                _lines.Enqueue(line);
                if (_lines.Count > KeptLines)
                {
                    _lines.Dequeue();
                }
            }
        }

        _inputOpened.TrySetException(new FfmpegException($"ffmpeg ended before it opened its input:{Environment.NewLine}{Output()}"));
    }

    /// <summary>The H.264 MP4 output arguments every recording uses.</summary>
    public static IEnumerable<string> Mp4Output(string path)
        => ["-c:v", "libx264", "-preset", "veryfast", "-pix_fmt", "yuv420p", "-movflags", "+faststart", "-y", path];

    /// <summary>A size libx264's 4:2:0 accepts: even width and height.</summary>
    public static Size Even(Size size) => new(Math.Max(2, size.Width & ~1), Math.Max(2, size.Height & ~1));

    public override string ToString() => new StringBuilder("ffmpeg ").AppendJoin(' ', _process.StartInfo.ArgumentList).ToString();
}
