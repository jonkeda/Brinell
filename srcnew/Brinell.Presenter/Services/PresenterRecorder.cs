using System.Globalization;
using Brinell.Core.Recording;
using Brinell.Narrator;
using Brinell.Narrator.Windows;
using Brinell.Recording;

namespace Brinell.Presenter.Services;

/// <summary>What a Presenter recording uses: the settings, where the Settings page keeps them.</summary>
/// <param name="Folder">The Recordings folder; null for the suite's <c>TestResults\recordings</c>.</param>
/// <param name="Voice">The voice, by (part of) its name, when the video script names none; null for the default.</param>
/// <param name="Beat">The wait after a step, when the video script does not say.</param>
/// <param name="SpeakAloud">Whether each line is also played while the run waits for it.</param>
public sealed record PresenterRecordingSettings(string? Folder, string? Voice, TimeSpan Beat, bool SpeakAloud);

/// <summary>A recording Presenter made.</summary>
/// <param name="Video">The video file.</param>
/// <param name="Narrated">Whether a video script's scene narrated it.</param>
/// <param name="Warnings">What went less well than asked.</param>
public sealed record PresenterRecorded(string Video, bool Narrated, IReadOnlyList<string> Warnings);

/// <summary>A scene of a video script to record into its kept folder (<see cref="BddKeptScenes"/>).</summary>
/// <param name="Plan">The scene.</param>
/// <param name="Number">Its number in the script, from 1.</param>
/// <param name="Folder">Its folder; an earlier recording there is replaced.</param>
/// <param name="Hash">What it is recorded from, written to its timeline.</param>
/// <param name="Voice">The voice, by (part of) its name; null for the default.</param>
/// <param name="Beat">The wait after a step.</param>
/// <param name="SpeakAloud">Whether each line is also played while the run waits for it.</param>
public sealed record PresenterSceneRequest(BddVideoScenePlan Plan, int Number, string Folder, string Hash, string? Voice, TimeSpan Beat, bool SpeakAloud);

/// <summary>Records Presenter's runs, the way a gate run with <c>BRINELL_RECORD</c> does.</summary>
public interface IPresenterRecorder
{
    /// <summary>Whether recording can work here, and why not (no ffmpeg).</summary>
    string? Unavailable { get; }

    /// <summary>
    /// Starts recording a session's scenario: its pacer is set, and the recorder starts at the
    /// first step. The scene a video script has for the scenario narrates it; without one it runs
    /// silently, with its beats.
    /// </summary>
    /// <param name="session">The session, before its first step.</param>
    /// <param name="workspacePath">The suite's folder.</param>
    /// <param name="featureFile">The scenario's feature file.</param>
    /// <param name="scenarioName">The scenario's name.</param>
    /// <param name="settings">The recording settings.</param>
    /// <param name="cancellationToken">Cancels the speech.</param>
    Task StartAsync(PresenterUatExecutionSession session, string workspacePath, string featureFile, string scenarioName, PresenterRecordingSettings settings, CancellationToken cancellationToken);

    /// <summary>After the run: the outro, then the video is composed. Null when nothing was being recorded.</summary>
    /// <param name="cancellationToken">Cancels the outro and the compose.</param>
    Task<PresenterRecorded?> FinishAsync(CancellationToken cancellationToken);

    /// <summary>
    /// A rehearsal's pacer for a scene: each line is spoken and played before its step, nothing is
    /// recorded. Null when this machine has no voice.
    /// </summary>
    /// <param name="scene">The scene.</param>
    /// <param name="voice">The voice, by (part of) its name; null for the default.</param>
    /// <param name="beat">The wait after a step.</param>
    /// <param name="cancellationToken">Cancels the speech.</param>
    Task<BddNarrationPacer?> RehearseAsync(BddVideoScenePlan scene, string? voice, TimeSpan beat, CancellationToken cancellationToken);

    /// <summary>
    /// Starts recording one scene of a script into its kept folder: its lines are spoken, the
    /// session's pacer is set, and the recorder starts at the first step. Nothing is composed.
    /// </summary>
    /// <param name="session">The session, before its first step.</param>
    /// <param name="scene">The scene.</param>
    /// <param name="cancellationToken">Cancels the speech.</param>
    Task StartSceneAsync(PresenterUatExecutionSession session, PresenterSceneRequest scene, CancellationToken cancellationToken);

    /// <summary>After the scene's last step: the outro, then its video and timeline are finished. Null when no scene was being recorded.</summary>
    /// <param name="cancellationToken">Cancels the outro.</param>
    Task<BddSceneTimeline?> FinishSceneAsync(CancellationToken cancellationToken);

    /// <summary>Gives up the scene being recorded (the run was stopped): nothing of it is kept.</summary>
    Task DropSceneAsync();

    /// <summary>Stitches a script's kept scenes into one video; the scenes stay.</summary>
    /// <param name="title">The video's title.</param>
    /// <param name="scenesFolder">The script's scenes folder.</param>
    /// <param name="video">The video to write.</param>
    /// <param name="sceneCount">The script's scene count.</param>
    /// <param name="cancellationToken">Cancels ffmpeg.</param>
    Task<PresenterRecorded> StitchAsync(string title, string scenesFolder, string video, int sceneCount, CancellationToken cancellationToken);

    /// <summary>Speaks a line aloud in a voice, to hear it before choosing it. False when this machine has no voice.</summary>
    /// <param name="text">What to say.</param>
    /// <param name="voice">The voice, by (part of) its name; null for the default.</param>
    /// <param name="cancellationToken">Stops it.</param>
    Task<bool> TryVoiceAsync(string text, string? voice, CancellationToken cancellationToken);
}

/// <inheritdoc />
public sealed class PresenterRecorder : IPresenterRecorder
{
    private static readonly Lazy<ScreenRecorders> Recorders = new(ScreenRecorders.Discover);
    private BddSceneRecording? _recording;
    private BddSceneRecording? _scene;
    private string? _title;
    private bool _narrated;

    /// <inheritdoc />
    public string? Unavailable => Ffmpeg.Find() is null ? Ffmpeg.HowToGet : null;

    /// <inheritdoc />
    public async Task StartAsync(PresenterUatExecutionSession session, string workspacePath, string featureFile, string scenarioName, PresenterRecordingSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(settings);
        if (Unavailable is not null)
        {
            throw new FfmpegNotFoundException();
        }

        var folder = settings.Folder is { Length: > 0 } named ? named : BddRecordings.DefaultFolder(workspacePath);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var sceneFolder = Path.Combine(folder, $"{BddScenarioAddress.Slug(scenarioName)}-{stamp}.scenes", "scene-01");
        var address = BddScenarioAddress.Of(Path.GetRelativePath(workspacePath, featureFile), scenarioName);

        var clips = BddSceneClips.Silent;
        var name = scenarioName;
        var beat = settings.Beat;
        if (BddRecordings.FindScene(workspacePath, featureFile, scenarioName) is { } found)
        {
            var speech = new WindowsSpeech();
            clips = await BddSceneClips.SpeakAsync(found.Scene, speech.IsAvailable ? speech : null, found.Script.Voice ?? settings.Voice, sceneFolder, cancellationToken).ConfigureAwait(false);
            name = found.Scene.Scene.Name;
            beat = found.Script.Beat ?? beat;
        }

        _narrated = !ReferenceEquals(clips, BddSceneClips.Silent);
        _title = name;
        var request = new BddSceneRequest(1, name, address, sceneFolder, clips, beat);
        _recording = new BddSceneRecording(request, session.RecordingTarget, Recorders.Value, settings.SpeakAloud ? WindowsSpeech.PlayAsync : null);
        session.Runner.Context.Pacer = _recording;
    }

    /// <inheritdoc />
    public async Task<BddNarrationPacer?> RehearseAsync(BddVideoScenePlan scene, string? voice, TimeSpan beat, CancellationToken cancellationToken)
    {
        var speech = new WindowsSpeech();
        if (!speech.IsAvailable)
        {
            return null;
        }

        var folder = Path.Combine(Path.GetTempPath(), "BrinellPresenter", "rehearsal");
        var clips = await BddSceneClips.SpeakAsync(scene, speech, voice, folder, cancellationToken).ConfigureAwait(false);
        return new BddNarrationPacer(clips, beat, WindowsSpeech.PlayAsync);
    }

    /// <inheritdoc />
    public async Task StartSceneAsync(PresenterUatExecutionSession session, PresenterSceneRequest scene, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(scene);
        if (Unavailable is not null)
        {
            throw new FfmpegNotFoundException();
        }

        // A scene recorded again replaces its earlier recording, whatever this one comes to.
        TryDelete(scene.Folder);
        var speech = new WindowsSpeech();
        var clips = await BddSceneClips.SpeakAsync(scene.Plan, speech.IsAvailable ? speech : null, scene.Voice, scene.Folder, cancellationToken).ConfigureAwait(false);
        var request = new BddSceneRequest(scene.Number, scene.Plan.Scene.Name, scene.Plan.Address, scene.Folder, clips, scene.Beat) { Hash = scene.Hash };
        _scene = new BddSceneRecording(request, session.RecordingTarget, Recorders.Value, scene.SpeakAloud ? WindowsSpeech.PlayAsync : null);
        session.Runner.Context.Pacer = _scene;
    }

    /// <inheritdoc />
    public async Task<BddSceneTimeline?> FinishSceneAsync(CancellationToken cancellationToken)
    {
        if (_scene is not { } recording)
        {
            return null;
        }

        _scene = null;
        await using (recording.ConfigureAwait(false))
        {
            return await recording.FinishAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task DropSceneAsync()
    {
        if (_scene is not { } recording)
        {
            return;
        }

        _scene = null;
        await recording.DisposeAsync().ConfigureAwait(false);
        TryDelete(recording.Request.Folder);
    }

    /// <inheritdoc />
    public async Task<PresenterRecorded> StitchAsync(string title, string scenesFolder, string video, int sceneCount, CancellationToken cancellationToken)
    {
        var stitched = await BddVideoComposer.StitchAsync(title, scenesFolder, video, sceneCount, cancellationToken).ConfigureAwait(false);
        return new PresenterRecorded(stitched.Video, true, stitched.Warnings);
    }

    /// <inheritdoc />
    public async Task<bool> TryVoiceAsync(string text, string? voice, CancellationToken cancellationToken)
    {
        var speech = new WindowsSpeech();
        if (!speech.IsAvailable)
        {
            return false;
        }

        var folder = Path.Combine(Path.GetTempPath(), "BrinellPresenter", "rehearsal");
        Directory.CreateDirectory(folder);
        var clip = await Speech.ClipAsync(speech, text, voice, Path.Combine(folder, "try.wav"), cancellationToken).ConfigureAwait(false);
        await WindowsSpeech.PlayAsync(clip, cancellationToken).ConfigureAwait(false);
        return true;
    }

    // False when a file of the folder is still held: the scene then reads as it did, until it is recorded again.
    private static bool TryDelete(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<PresenterRecorded?> FinishAsync(CancellationToken cancellationToken)
    {
        if (_recording is not { } recording)
        {
            return null;
        }

        _recording = null;
        await using (recording.ConfigureAwait(false))
        {
            var scene = await recording.FinishAsync(cancellationToken).ConfigureAwait(false);
            var video = await BddVideoComposer.ComposeAsync(_title ?? scene.Name, Path.GetDirectoryName(recording.Request.Folder)!, cancellationToken).ConfigureAwait(false);
            return new PresenterRecorded(video.Video, _narrated, [.. scene.Warnings, .. video.Warnings]);
        }
    }
}
