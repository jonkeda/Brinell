using System.Security.Cryptography;
using System.Text;

namespace Bruinet.Narrator;

/// <summary>What a kept scene's recording is worth.</summary>
public enum BddSceneState
{
    /// <summary>No recording of the scene.</summary>
    NotRecorded,

    /// <summary>Recorded, but a step failed.</summary>
    Failed,

    /// <summary>Recorded from another text: the scene's lines, its scenario's steps, the voice or the beat changed since.</summary>
    OutOfDate,

    /// <summary>Recorded, passed, and current.</summary>
    Recorded,
}

/// <summary>
/// A script's scenes kept between recordings: each scene is recorded on its own into
/// <c>&lt;Recordings&gt;\&lt;script&gt;.scenes\scene-NN</c>, a scene recorded again replaces that scene
/// only, and the video is stitched from the latest recording of every scene.
/// </summary>
public static class BddKeptScenes
{
    /// <summary>The folder a script's scenes are kept in: its working set, with no time stamp.</summary>
    /// <param name="recordingsFolder">The Recordings folder.</param>
    /// <param name="scriptPath">The video script.</param>
    public static string Folder(string recordingsFolder, string scriptPath)
    {
        ArgumentNullException.ThrowIfNull(recordingsFolder);
        ArgumentNullException.ThrowIfNull(scriptPath);
        return Path.Combine(recordingsFolder, NameOf(scriptPath) + ".scenes");
    }

    /// <summary>A scene's folder in a scenes folder.</summary>
    /// <param name="scenesFolder">The scenes folder.</param>
    /// <param name="number">The scene's number, from 1.</param>
    public static string SceneFolder(string scenesFolder, int number) => Path.Combine(scenesFolder, $"scene-{number:00}");

    /// <summary>A script's name: its file name without <c>.video.md</c>.</summary>
    /// <param name="scriptPath">The video script.</param>
    public static string NameOf(string scriptPath)
    {
        var file = Path.GetFileName(scriptPath);
        return file.EndsWith(BddVideoScript.Extension, StringComparison.OrdinalIgnoreCase) ? file[..^BddVideoScript.Extension.Length] : Path.GetFileNameWithoutExtension(file);
    }

    /// <summary>
    /// What a scene's recording depends on, as one value: its name, its lines, its scenario's steps,
    /// the voice and the beat. A recording made from another value is out of date.
    /// </summary>
    /// <param name="plan">The scene.</param>
    /// <param name="voice">The voice it is spoken in; null for the default.</param>
    /// <param name="beat">The wait after a step.</param>
    /// <param name="instructions">How a cloud voice says the lines; null for none.</param>
    public static string Hash(BddVideoScenePlan plan, string? voice, TimeSpan beat, string? instructions = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var text = new StringBuilder();
        void Add(string? value) => text.Append(value ?? string.Empty).Append('\n');

        Add(plan.Scene.Name);
        Add(plan.Address);
        Add(plan.Scene.Intro);
        Add(plan.Scene.Outro);
        Add(voice);
        Add(((long)beat.TotalMilliseconds).ToString(System.Globalization.CultureInfo.InvariantCulture));

        // Only when there are any, so a scene kept before instructions existed is not out of date.
        if (instructions is not null)
        {
            Add("instructions: " + instructions);
        }

        foreach (var step in plan.Steps)
        {
            Add(BddVideoScriptCheck.Written(step));
            Add(plan.Says.TryGetValue(step.Source.LineNumber, out var line) ? line.Say : null);
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())))[..16];
    }

    /// <summary>A scene's kept recording; null when it has none, or none that can be read.</summary>
    /// <param name="scenesFolder">The scenes folder.</param>
    /// <param name="number">The scene's number, from 1.</param>
    public static BddSceneTimeline? Recorded(string scenesFolder, int number)
    {
        var path = Path.Combine(SceneFolder(scenesFolder, number), BddSceneTimeline.FileName);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return BddSceneTimeline.Load(path);
        }
        catch (Exception exception) when (exception is System.Text.Json.JsonException or InvalidDataException or IOException)
        {
            return null;
        }
    }

    /// <summary>What a scene's kept recording is worth against the scene as it is now.</summary>
    /// <param name="scenesFolder">The scenes folder.</param>
    /// <param name="number">The scene's number, from 1.</param>
    /// <param name="hash">The scene's <see cref="Hash"/> now.</param>
    public static BddSceneState State(string scenesFolder, int number, string hash)
    {
        if (Recorded(scenesFolder, number) is not { Video: { } video } timeline ||
            !File.Exists(Path.Combine(SceneFolder(scenesFolder, number), video)))
        {
            return BddSceneState.NotRecorded;
        }

        if (!string.Equals(timeline.Hash, hash, StringComparison.Ordinal))
        {
            return BddSceneState.OutOfDate;
        }

        return timeline.Status == BddStepResultStatus.Passed ? BddSceneState.Recorded : BddSceneState.Failed;
    }
}
