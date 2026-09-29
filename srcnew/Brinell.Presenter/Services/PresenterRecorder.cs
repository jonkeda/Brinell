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
}

/// <inheritdoc />
public sealed class PresenterRecorder : IPresenterRecorder
{
    private static readonly Lazy<ScreenRecorders> Recorders = new(ScreenRecorders.Discover);
    private BddSceneRecording? _recording;
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
