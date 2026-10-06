using System.Globalization;
using System.Text;
using Brinell.Recording;

namespace Brinell.Narrator;

/// <summary>What a composed video is made of, before ffmpeg runs: the scenes, the combined timeline, the captions and ffmpeg's arguments.</summary>
/// <param name="Timeline">The video's timeline: every scene on the one clock.</param>
/// <param name="Captions">The <c>.srt</c>: a cue per spoken line, and one per failure.</param>
/// <param name="Chapters">ffmpeg's metadata file: the title, and a chapter per scene.</param>
/// <param name="Arguments">ffmpeg's arguments; the chapters file is <c>{chapters}</c> in them.</param>
/// <param name="Warnings">Scenes left out, and why.</param>
public sealed record BddVideoComposition(BddVideoTimeline Timeline, string Captions, string Chapters, IReadOnlyList<string> Arguments, IReadOnlyList<string> Warnings);

/// <summary>What a compose made.</summary>
/// <param name="Video">The video (<c>.mp4</c>), with the voice and a chapter per scene.</param>
/// <param name="Captions">The captions (<c>.srt</c>) beside it.</param>
/// <param name="Timeline">The timeline (<c>.timeline.json</c>) beside it.</param>
/// <param name="Warnings">Scenes left out, and why.</param>
public sealed record BddComposedVideo(string Video, string Captions, string Timeline, IReadOnlyList<string> Warnings);

/// <summary>
/// Makes the video from a run's scenes: the scene videos joined in order, each spoken line laid in
/// at its time, a chapter per scene, and the captions as a separate <c>.srt</c>.
/// </summary>
public static class BddVideoComposer
{
    /// <summary>How long a failure's caption shows.</summary>
    public static readonly TimeSpan FailureCaption = TimeSpan.FromSeconds(4);

    /// <summary>Whether every scene of a script has finished in <paramref name="scenesFolder"/>.</summary>
    /// <param name="scenesFolder">The run's scenes folder.</param>
    /// <param name="sceneCount">How many scenes the script has.</param>
    public static bool IsComplete(string scenesFolder, int sceneCount)
        => Enumerable.Range(1, sceneCount).All(number => File.Exists(Path.Combine(scenesFolder, $"scene-{number:00}", BddSceneTimeline.FileName)));

    /// <summary>Reads the scenes' timelines, in scene order.</summary>
    /// <param name="scenesFolder">The run's scenes folder.</param>
    public static IReadOnlyList<(string Folder, BddSceneTimeline Scene)> ReadScenes(string scenesFolder)
        => Directory.EnumerateDirectories(scenesFolder, "scene-*")
            .Select(folder => (Folder: folder, Path: Path.Combine(folder, BddSceneTimeline.FileName)))
            .Where(scene => File.Exists(scene.Path))
            .Select(scene => (scene.Folder, BddSceneTimeline.Load(scene.Path)))
            .OrderBy(scene => scene.Item2.Number)
            .ToList();

    /// <summary>Plans the video; nothing is run.</summary>
    /// <param name="title">The video's title.</param>
    /// <param name="scenes">The scenes, in order, with their folders.</param>
    /// <param name="outputVideo">The video to write.</param>
    public static BddVideoComposition Plan(string title, IReadOnlyList<(string Folder, BddSceneTimeline Scene)> scenes, string outputVideo)
    {
        ArgumentNullException.ThrowIfNull(scenes);
        var warnings = new List<string>();
        var recorded = new List<(string Folder, BddSceneTimeline Scene)>();
        foreach (var (folder, scene) in scenes)
        {
            if (scene.Video is null || !File.Exists(Path.Combine(folder, scene.Video)))
            {
                warnings.Add($"Scene {scene.Number} ({scene.Name}) has no video and is left out{(scene.Warnings.Count > 0 ? $": {scene.Warnings[0]}" : ".")}");
                continue;
            }

            recorded.Add((folder, scene));
        }

        if (recorded.Count == 0)
        {
            throw new InvalidOperationException($"No scene has a video, so there is nothing to compose.{Environment.NewLine}{string.Join(Environment.NewLine, warnings)}");
        }

        var timeline = new List<BddSceneTimeline>();
        var offset = 0d;
        foreach (var (_, scene) in recorded)
        {
            timeline.Add(scene with
            {
                Video = null,
                Steps = [.. scene.Steps.Select(step => step with { Start = step.Start + offset, End = step.End + offset })],
                // The scenes' WAVs go with their folder once the voice is in the video.
                Clips = [.. scene.Clips.Select(clip => clip with { Wav = null, Start = clip.Start + offset, End = clip.End + offset })],
                Interactions = scene.Interactions?.Select(interaction => interaction with { At = interaction.At + offset }).ToList(),
            });
            offset += scene.Duration;
        }

        var combined = new BddVideoTimeline(title, timeline);
        return new BddVideoComposition(combined, Captions(combined), Chapters(combined, offsets: Offsets(recorded)), Arguments(recorded, outputVideo), warnings);
    }

    /// <summary>The extension of a run's scenes folder, removed once its video is made.</summary>
    public const string RunFolderExtension = ".run";

    /// <summary>Composes the video from a run's scenes folder, then removes the folder.</summary>
    /// <param name="title">The video's title.</param>
    /// <param name="scenesFolder">The run's scenes folder (<c>&lt;name&gt;.run</c>).</param>
    /// <param name="cancellationToken">Stops ffmpeg.</param>
    /// <returns>The files beside the folder, replacing an earlier run's: <c>&lt;name&gt;.mp4</c>, <c>.srt</c> and <c>.timeline.json</c>.</returns>
    public static async Task<BddComposedVideo> ComposeAsync(string title, string scenesFolder, CancellationToken cancellationToken = default)
    {
        var folder = Path.GetFullPath(scenesFolder).TrimEnd(Path.DirectorySeparatorChar);
        var basePath = folder.EndsWith(RunFolderExtension, StringComparison.OrdinalIgnoreCase) ? folder[..^RunFolderExtension.Length] : folder;
        var composed = await StitchAsync(title, folder, basePath + ".mp4", null, cancellationToken).ConfigureAwait(false);
        Directory.Delete(folder, recursive: true);
        return composed;
    }

    /// <summary>
    /// Stitches the scenes of a kept folder into a video, with its captions and timeline beside it;
    /// the folder stays, so a scene can be recorded again and the video stitched again.
    /// </summary>
    /// <param name="title">The video's title.</param>
    /// <param name="scenesFolder">The scenes folder.</param>
    /// <param name="video">The video to write.</param>
    /// <param name="sceneCount">The script's scene count: a folder of a scene the script no longer has is left out. Null for every scene there.</param>
    /// <param name="cancellationToken">Cancels ffmpeg.</param>
    public static async Task<BddComposedVideo> StitchAsync(string title, string scenesFolder, string video, int? sceneCount, CancellationToken cancellationToken = default)
    {
        var folder = Path.GetFullPath(scenesFolder).TrimEnd(Path.DirectorySeparatorChar);
        var basePath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(video))!, Path.GetFileNameWithoutExtension(video));
        var scenes = ReadScenes(folder);
        var plan = Plan(title, sceneCount is { } count ? [.. scenes.Where(scene => scene.Scene.Number <= count)] : scenes, video);

        var chapters = Path.Combine(folder, "chapters.txt");
        await File.WriteAllTextAsync(chapters, plan.Chapters, cancellationToken).ConfigureAwait(false);
        await Ffmpeg.RunAsync(plan.Arguments.Select(argument => argument == "{chapters}" ? chapters : argument), cancellationToken).ConfigureAwait(false);

        var captions = basePath + ".srt";
        var timeline = basePath + ".timeline.json";
        await File.WriteAllTextAsync(captions, plan.Captions, cancellationToken).ConfigureAwait(false);
        plan.Timeline.Save(timeline);
        return new BddComposedVideo(video, captions, timeline, plan.Warnings);
    }

    /// <summary>The captions: a cue per spoken line, and a failure's message after the step that failed.</summary>
    /// <param name="timeline">The video's timeline.</param>
    public static string Captions(BddVideoTimeline timeline)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        var cues = timeline.Scenes
            .SelectMany(scene => scene.Clips.Select(clip => (clip.Start, clip.End, clip.Text))
                .Concat(scene.Steps
                    .Where(step => step.Status == BddStepResultStatus.Failed)
                    .Select(step => (Start: step.End, End: step.End + FailureCaption.TotalSeconds, Text: $"Failed: {step.Text}{(step.Message is { } message ? $" ({message})" : string.Empty)}"))))
            .OrderBy(cue => cue.Start)
            .ToList();
        var srt = new StringBuilder();
        for (var index = 0; index < cues.Count; index++)
        {
            srt.Append(CultureInfo.InvariantCulture, $"{index + 1}\n{Time(cues[index].Start)} --> {Time(cues[index].End)}\n{cues[index].Text}\n\n");
        }

        return srt.ToString();
    }

    private static List<double> Offsets(List<(string Folder, BddSceneTimeline Scene)> scenes)
    {
        var offsets = new List<double>();
        var offset = 0d;
        foreach (var (_, scene) in scenes)
        {
            offsets.Add(offset);
            offset += scene.Duration;
        }

        offsets.Add(offset);
        return offsets;
    }

    // ffmpeg's metadata format: the title, and a chapter per scene in milliseconds.
    private static string Chapters(BddVideoTimeline timeline, List<double> offsets)
    {
        var text = new StringBuilder(";FFMETADATA1\n").Append("title=").Append(Escape(timeline.Title)).Append('\n');
        for (var index = 0; index < timeline.Scenes.Count; index++)
        {
            text.Append(CultureInfo.InvariantCulture, $"\n[CHAPTER]\nTIMEBASE=1/1000\nSTART={(long)(offsets[index] * 1000)}\nEND={(long)(offsets[index + 1] * 1000)}\ntitle={Escape(timeline.Scenes[index].Name)}\n");
        }

        return text.ToString();
    }

    // The scenes' videos, scaled and padded to the first one's size, joined; each line's WAV
    // delayed to its time and mixed; the chapters file last.
    private static List<string> Arguments(List<(string Folder, BddSceneTimeline Scene)> scenes, string outputVideo)
    {
        var arguments = new List<string> { "-y" };
        foreach (var (folder, scene) in scenes)
        {
            arguments.AddRange(["-i", Path.Combine(folder, scene.Video!)]);
        }

        var clips = new List<(string Wav, double Start)>();
        var offset = 0d;
        foreach (var (folder, scene) in scenes)
        {
            clips.AddRange(scene.Clips
                .Where(clip => clip.Wav is not null)
                .Select(clip => (Path.IsPathRooted(clip.Wav) ? clip.Wav! : Path.Combine(folder, clip.Wav!), clip.Start + offset)));
            offset += scene.Duration;
        }

        foreach (var clip in clips)
        {
            arguments.AddRange(["-i", clip.Wav]);
        }

        var chaptersInput = scenes.Count + clips.Count;
        arguments.AddRange(["-f", "ffmetadata", "-i", "{chapters}"]);

        // Every scene at the first one's size: a window that was resized is scaled and padded, not stretched.
        var (width, height) = (scenes[0].Scene.Width, scenes[0].Scene.Height);
        var graph = new StringBuilder();
        for (var index = 0; index < scenes.Count; index++)
        {
            graph.Append(width > 0 && height > 0
                ? $"[{index}:v]scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2,setsar=1,fps=30[v{index}];"
                : $"[{index}:v]setsar=1,fps=30[v{index}];");
        }

        for (var index = 0; index < scenes.Count; index++)
        {
            graph.Append(CultureInfo.InvariantCulture, $"[v{index}]");
        }

        graph.Append(CultureInfo.InvariantCulture, $"concat=n={scenes.Count}:v=1:a=0[video]");
        arguments.Add("-filter_complex");
        if (clips.Count > 0)
        {
            graph.Append(';');
            for (var index = 0; index < clips.Count; index++)
            {
                var delay = (long)Math.Round(clips[index].Start * 1000);
                graph.Append(CultureInfo.InvariantCulture, $"[{scenes.Count + index}:a]adelay={delay}:all=1[a{index}];");
            }

            for (var index = 0; index < clips.Count; index++)
            {
                graph.Append(CultureInfo.InvariantCulture, $"[a{index}]");
            }

            graph.Append(CultureInfo.InvariantCulture, $"amix=inputs={clips.Count}:normalize=0:dropout_transition=0[audio]");
        }

        arguments.Add(graph.ToString());
        arguments.AddRange(["-map", "[video]"]);
        if (clips.Count > 0)
        {
            arguments.AddRange(["-map", "[audio]", "-c:a", "aac", "-b:a", "128k"]);
        }

        arguments.AddRange(["-map_metadata", chaptersInput.ToString(CultureInfo.InvariantCulture), "-map_chapters", chaptersInput.ToString(CultureInfo.InvariantCulture)]);
        arguments.AddRange(FfmpegProcess.Mp4Output(outputVideo));
        return arguments;
    }

    private static string Time(double seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00},{time.Milliseconds:000}";
    }

    private static string Escape(string value)
    {
        var escaped = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (character is '=' or ';' or '#' or '\\' or '\n')
            {
                escaped.Append('\\');
            }

            escaped.Append(character);
        }

        return escaped.ToString();
    }
}
