using System.Drawing;
using System.Text.Json;
using Bruinet.Core.Recording;
using Bruinet.Recording;
using Microsoft.Playwright;

[assembly: ScreenRecorder(typeof(Bruinet.Html.Playwright.PlaywrightRecorder), Order = 10)]

namespace Bruinet.Html.Playwright;

/// <summary>
/// Records a browser page through Chromium's screencast (<c>Page.startScreencast</c>): the page's
/// own frames, headless or not, started when the scene starts. Other browsers are not offered: a
/// headed one under Presenter is a window, which the window recorder records.
/// </summary>
public sealed class PlaywrightRecorder : IScreenRecorder
{
    private readonly Func<string, Size, int, IFrameSink> _sink;

    /// <summary>Creates the recorder; the frames are encoded by ffmpeg over a pipe.</summary>
    public PlaywrightRecorder()
        : this((path, size, fps) => new FfmpegImageSink(path, size, fps))
    {
    }

    internal PlaywrightRecorder(Func<string, Size, int, IFrameSink> sink) => _sink = sink;

    /// <inheritdoc />
    public bool CanRecord(RecordingTarget target)
        => target is RecordingTarget.BrowserPage { Page: IPage page }
           && !page.IsClosed
           && (page.Context.Browser is not { } browser || browser.BrowserType.Name == "chromium");

    /// <inheritdoc />
    public async Task<IRecording> StartAsync(RecordingTarget target, RecordingOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (target is not RecordingTarget.BrowserPage { Page: IPage page } || !CanRecord(target))
        {
            throw new NotSupportedException($"{nameof(PlaywrightRecorder)} records a Chromium page, not {target}.");
        }

        var session = await page.Context.NewCDPSessionAsync(page).ConfigureAwait(false);
        var viewport = page.ViewportSize is { } size
            ? new Size(size.Width, size.Height)
            : new Size(await page.EvaluateAsync<int>("() => window.innerWidth").ConfigureAwait(false), await page.EvaluateAsync<int>("() => window.innerHeight").ConfigureAwait(false));
        var recording = new Recording(session, options, FfmpegSize(viewport), _sink) { Viewport = viewport };
        try
        {
            await recording.StartAsync(viewport).ConfigureAwait(false);
            await recording.FirstFrame.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
            return recording;
        }
        catch
        {
            await recording.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    // libx264's 4:2:0 wants even sides.
    private static Size FfmpegSize(Size size) => new(Math.Max(2, size.Width & ~1), Math.Max(2, size.Height & ~1));

    private sealed class Recording : IRecording
    {
        private readonly ICDPSession _session;
        private readonly ImageVideo _video;
        private readonly string _path;
        private bool _stopped;

        public Recording(ICDPSession session, RecordingOptions options, Size size, Func<string, Size, int, IFrameSink> sink)
        {
            _session = session;
            _path = options.OutputPath;
            _video = new ImageVideo(size, options.FramesPerSecond, sink(options.OutputPath, size, options.FramesPerSecond));
            _session.Event("Page.screencastFrame").OnEvent += OnFrame;
        }

        public Task<long> FirstFrame => _video.FirstFrame;

        // The viewport in CSS pixels, which an element's bounds are in; the frames may be larger.
        public Size Viewport { get; init; }

        public long StartedAt => _video.FirstFrame.IsCompletedSuccessfully ? _video.FirstFrame.Result : 0;

        public Task StartAsync(Size viewport) => _session.SendAsync("Page.startScreencast", new Dictionary<string, object>
        {
            ["format"] = "jpeg",
            ["quality"] = 85,
            ["maxWidth"] = viewport.Width,
            ["maxHeight"] = viewport.Height,
            ["everyNthFrame"] = 1,
        });

        public async Task<RecordingResult> StopAsync(CancellationToken cancellationToken = default)
        {
            await StopScreencastAsync().ConfigureAwait(false);
            var result = await _video.StopAsync(_path, cancellationToken).ConfigureAwait(false);
            return result with
            {
                Warnings = [.. result.Warnings, "The start is when the first frame arrived from the browser, not when it was painted: steps may be a frame early."],
                Source = new Rectangle(Point.Empty, Viewport),
            };
        }

        public async ValueTask DisposeAsync()
        {
            await StopScreencastAsync().ConfigureAwait(false);
            await _video.DisposeAsync().ConfigureAwait(false);
        }

        // A closed page has no screencast to stop; it is stopped already.
        private async Task StopScreencastAsync()
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;
            _session.Event("Page.screencastFrame").OnEvent -= OnFrame;
            try
            {
                await _session.SendAsync("Page.stopScreencast").ConfigureAwait(false);
                await _session.DetachAsync().ConfigureAwait(false);
            }
            catch (PlaywrightException exception)
            {
                System.Diagnostics.Debug.WriteLine($"Recording: the screencast had ended ({exception.Message}).");
            }
        }

        // Every frame is acknowledged, or Chromium stops sending them.
        private void OnFrame(object? sender, JsonElement? frame)
        {
            if (frame is not { } value || _stopped)
            {
                return;
            }

            var arrived = RecordingClock.Now;
            _ = _session.SendAsync("Page.screencastFrameAck", new Dictionary<string, object> { ["sessionId"] = value.GetProperty("sessionId").GetInt32() });
            _video.Offer(arrived, Convert.FromBase64String(value.GetProperty("data").GetString()!));
        }
    }
}
