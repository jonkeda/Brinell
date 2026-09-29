using System.Diagnostics;
using Brinell.Core.Recording;
using Brinell.Recording;

namespace Brinell.Narrator;

/// <summary>A scene to record: where it goes, and what is said in it.</summary>
/// <param name="Number">The scene's number in the script, from 1.</param>
/// <param name="Name">The scene's name.</param>
/// <param name="Scenario">The scenario's address.</param>
/// <param name="Folder">The scene's folder: its video, its timeline and its speech.</param>
/// <param name="Clips">Its spoken lines.</param>
/// <param name="Beat">The wait after a step.</param>
public sealed record BddSceneRequest(int Number, string Name, string Scenario, string Folder, BddSceneClips Clips, TimeSpan Beat)
{
    /// <summary>The scene's video file.</summary>
    public string VideoPath => Path.Combine(Folder, "video.mp4");

    /// <summary>The scene's timeline file.</summary>
    public string TimelinePath => Path.Combine(Folder, BddSceneTimeline.FileName);
}

/// <summary>
/// Records one scene: the recorder starts at the first step, when the fixture has the app ready, and
/// the intro plays over its first frames; after the last step the outro plays, the recorder stops
/// and the timeline is written. Set it as the context's <see cref="BddExecutionContext.Pacer"/>.
/// </summary>
public sealed class BddSceneRecording : IBddStepPacer, IAsyncDisposable
{
    private readonly BddSceneRequest _request;
    private readonly Func<RecordingTarget?> _target;
    private readonly ScreenRecorders _recorders;
    private readonly BddNarrationPacer _pacer;
    private readonly List<string> _warnings = [];
    private IRecording? _recording;
    private bool _started;
    private long _startedAt;

    /// <summary>Creates the scene's recording; nothing starts until the first step.</summary>
    /// <param name="request">The scene.</param>
    /// <param name="target">What the app shows, asked at the first step.</param>
    /// <param name="recorders">The recorders to choose from.</param>
    /// <param name="play">Plays a line aloud while it is waited for; null to wait in silence.</param>
    public BddSceneRecording(BddSceneRequest request, Func<RecordingTarget?> target, ScreenRecorders recorders, Func<SpeechClip, CancellationToken, Task>? play = null)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _recorders = recorders ?? throw new ArgumentNullException(nameof(recorders));
        _pacer = new BddNarrationPacer(request.Clips, request.Beat, play);
    }

    /// <summary>The scene.</summary>
    public BddSceneRequest Request => _request;

    /// <summary>What went less well than asked, so far.</summary>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <inheritdoc />
    public async Task BeforeAsync(BddStepInvocation invocation, BddExecutionContext context, CancellationToken cancellationToken)
    {
        await StartAsync(cancellationToken).ConfigureAwait(false);
        await _pacer.BeforeAsync(invocation, context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task AfterAsync(BddStepResult result, BddExecutionContext context, CancellationToken cancellationToken)
        => _pacer.AfterAsync(result, context, cancellationToken);

    /// <summary>After the scenario: the outro, then the video and the timeline are finished.</summary>
    /// <param name="cancellationToken">Cancels the wait.</param>
    public async Task<BddSceneTimeline> FinishAsync(CancellationToken cancellationToken = default)
    {
        await StartAsync(cancellationToken).ConfigureAwait(false);
        await _pacer.FinishAsync(cancellationToken).ConfigureAwait(false);
        var end = Stopwatch.GetTimestamp();
        string? video = null;
        var size = System.Drawing.Size.Empty;
        if (_recording is { } recording)
        {
            _recording = null;
            var result = await recording.StopAsync(cancellationToken).ConfigureAwait(false);
            await recording.DisposeAsync().ConfigureAwait(false);
            _warnings.AddRange(result.Warnings);
            _startedAt = result.StartedAt;
            end = Math.Max(end, RecordingClock.Add(result.StartedAt, result.Duration));
            video = Path.GetFileName(result.Path);
            size = result.Size;
        }

        var timeline = _pacer.Timeline(_request.Number, _request.Name, _request.Scenario, video, _startedAt, end, _warnings) with { Width = size.Width, Height = size.Height };
        timeline.Save(_request.TimelinePath);
        return timeline;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_recording is { } recording)
        {
            _recording = null;
            await recording.DisposeAsync().ConfigureAwait(false);
        }
    }

    // A scene that cannot be recorded still runs and gets its timeline: the reason is a warning.
    private async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _startedAt = Stopwatch.GetTimestamp();
        Directory.CreateDirectory(_request.Folder);
        if (_target() is not { } target)
        {
            _warnings.Add("The test context does not say what it shows (IRecordable), so the scene was not recorded.");
            return;
        }

        if (_recorders.Find(target) is not { } recorder)
        {
            _warnings.Add($"No recorder here can record {target}, so the scene was not recorded. On Windows, set {RecordingPackages.Variable} to a folder with Brinell.Narrator.Windows.");
            return;
        }

        _recording = await recorder.StartAsync(target, new RecordingOptions(_request.VideoPath), cancellationToken).ConfigureAwait(false);
        _startedAt = _recording.StartedAt;
    }
}
