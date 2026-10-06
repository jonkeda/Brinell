using System.Globalization;
using Brinell.Recording;

namespace Brinell.Narrator.Tests;

/// <summary>Runs only where ffmpeg is installed; skipped, saying how to get it, elsewhere.</summary>
public sealed class FfmpegFactAttribute : FactAttribute
{
    public FfmpegFactAttribute()
    {
        if (Ffmpeg.Find() is null)
        {
            Skip = Ffmpeg.HowToGet;
        }
    }
}

public sealed class BddVideoComposerTests : IDisposable
{
    private readonly string _scenes = Directory.CreateTempSubdirectory("brinell-compose-").FullName + ".scenes";

    public BddVideoComposerTests() => Directory.CreateDirectory(_scenes);

    public void Dispose()
    {
        if (Directory.Exists(_scenes))
        {
            Directory.Delete(_scenes, recursive: true);
        }
    }

    [Fact]
    public void Scenes_are_put_on_one_clock_with_a_chapter_each()
    {
        var plan = BddVideoComposer.Plan("How it works", Scenes(), Path.Combine(_scenes, "out.mp4"));

        var (first, second) = (plan.Timeline.Scenes[0], plan.Timeline.Scenes[1]);
        Assert.Equal(2.0, first.Clips[0].Start);
        Assert.Equal(5.0 + 1.0, second.Clips[0].Start);
        Assert.Equal(5.0 + 3.0, second.Steps[0].End);
        Assert.All(plan.Timeline.Scenes.SelectMany(scene => scene.Clips), clip => Assert.Null(clip.Wav));
        Assert.Contains("title=How it works", plan.Chapters, StringComparison.Ordinal);
        Assert.Contains("[CHAPTER]\nTIMEBASE=1/1000\nSTART=5000\nEND=9000\ntitle=Adding", plan.Chapters, StringComparison.Ordinal);
    }

    [Fact]
    public void Captions_have_a_cue_per_line_and_one_after_a_failure()
    {
        var plan = BddVideoComposer.Plan("How it works", Scenes(), Path.Combine(_scenes, "out.mp4"));

        Assert.Equal(
            "1\n00:00:02,000 --> 00:00:03,500\nThe app opens.\n\n" +
            "2\n00:00:06,000 --> 00:00:07,000\nTap Add.\n\n" +
            "3\n00:00:08,000 --> 00:00:12,000\nFailed: When I tap Add (It is gone.)\n\n",
            plan.Captions);
    }

    [Fact]
    public void Each_line_is_laid_in_at_its_time_and_the_scenes_are_joined_at_the_first_ones_size()
    {
        var plan = BddVideoComposer.Plan("How it works", Scenes(), Path.Combine(_scenes, "out.mp4"));
        var graph = plan.Arguments[plan.Arguments.ToList().IndexOf("-filter_complex") + 1];

        Assert.Contains("[1:v]scale=640:480:force_original_aspect_ratio=decrease,pad=640:480", graph, StringComparison.Ordinal);
        Assert.Contains("[v0][v1]concat=n=2:v=1:a=0[video]", graph, StringComparison.Ordinal);
        Assert.Contains("[2:a]adelay=2000:all=1[a0]", graph, StringComparison.Ordinal);
        Assert.Contains("[3:a]adelay=6000:all=1[a1]", graph, StringComparison.Ordinal);
        Assert.Contains("amix=inputs=2", graph, StringComparison.Ordinal);
        Assert.Equal(["-map_metadata", "4", "-map_chapters", "4"], plan.Arguments.SkipWhile(argument => argument != "-map_metadata").Take(4));
    }

    [Fact]
    public void A_scene_without_a_video_is_left_out_and_said()
    {
        var scenes = Scenes();
        scenes.Insert(1, (Path.Combine(_scenes, "scene-02"), scenes[0].Scene with { Number = 2, Name = "Lost", Video = null, Warnings = ["No recorder here."] }));

        var plan = BddVideoComposer.Plan("How it works", scenes, Path.Combine(_scenes, "out.mp4"));

        Assert.Equal(["Opening", "Adding"], plan.Timeline.Scenes.Select(scene => scene.Name));
        Assert.Equal("Scene 2 (Lost) has no video and is left out: No recorder here.", Assert.Single(plan.Warnings));
    }

    [Fact]
    public void A_script_is_whole_when_every_scene_has_its_timeline()
    {
        Scenes();

        Assert.True(BddVideoComposer.IsComplete(_scenes, 2));
        Assert.False(BddVideoComposer.IsComplete(_scenes, 3));
        Assert.Equal([1, 2], BddVideoComposer.ReadScenes(_scenes).Select(scene => scene.Scene.Number));
    }

    [FfmpegFact]
    public async Task The_scenes_become_one_video_with_its_captions_and_timeline()
    {
        foreach (var (folder, scene) in Scenes())
        {
            await Ffmpeg.RunAsync(["-y", "-f", "lavfi", "-i", $"testsrc=size={scene.Width}x{scene.Height}:rate=30", "-t", scene.Duration.ToString(CultureInfo.InvariantCulture), "-pix_fmt", "yuv420p", Path.Combine(folder, "video.mp4")]);
            foreach (var clip in scene.Clips)
            {
                await Ffmpeg.RunAsync(["-y", "-f", "lavfi", "-i", "sine=frequency=440", "-t", (clip.End - clip.Start).ToString(CultureInfo.InvariantCulture), Path.Combine(folder, clip.Wav!)]);
            }
        }

        var video = await BddVideoComposer.ComposeAsync("How it works", _scenes);

        Assert.True(new FileInfo(video.Video).Length > 0);
        Assert.Contains("Tap Add.", await File.ReadAllTextAsync(video.Captions), StringComparison.Ordinal);
        Assert.Equal(2, BddVideoTimeline.Load(video.Timeline).Scenes.Count);
        Assert.False(Directory.Exists(_scenes));
        File.Delete(video.Video);
        File.Delete(video.Captions);
        File.Delete(video.Timeline);
    }

    // Two scenes: 5 s at 640x480 with a line at 2 s; 4 s at 800x600 with a line at 1 s and a failed step.
    private List<(string Folder, BddSceneTimeline Scene)> Scenes()
    {
        var one = new BddSceneTimeline(1, "Opening", "Features/A.feature.md#A.1", "video.mp4", 5, BddStepResultStatus.Passed,
            [new BddTimedStep(6, "Given I am on the list", 3.5, 4, BddStepResultStatus.Passed)],
            [new BddTimedClip("The app opens.", "line-6.wav", 2, 3.5)], [], 640, 480);
        var two = new BddSceneTimeline(2, "Adding", "Features/A.feature.md#A.2", "video.mp4", 4, BddStepResultStatus.Failed,
            [new BddTimedStep(9, "When I tap Add", 2, 3, BddStepResultStatus.Failed, "It is gone.")],
            [new BddTimedClip("Tap Add.", "line-9.wav", 1, 2)], [], 800, 600);
        var scenes = new List<(string, BddSceneTimeline)>();
        foreach (var scene in new[] { one, two })
        {
            var folder = Path.Combine(_scenes, $"scene-{scene.Number:00}");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "video.mp4"), []);
            scene.Save(Path.Combine(folder, BddSceneTimeline.FileName));
            scenes.Add((folder, scene));
        }

        return scenes;
    }
}
