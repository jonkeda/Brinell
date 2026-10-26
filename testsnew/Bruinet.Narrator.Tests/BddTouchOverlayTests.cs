using Bruinet.Core.Recording;

namespace Bruinet.Narrator.Tests;

/// <summary>The touch marks a composed video gets, as the ASS file ffmpeg burns in.</summary>
public sealed class BddTouchOverlayTests
{
    [Fact]
    public void A_tap_is_a_ring_closing_before_it_then_a_dot_and_a_ripple_on_its_target_in_the_accent()
    {
        var ass = Ass(Scene(1000, 500, Tap(2.0, new BddBox(100, 100, 40, 20))));

        Assert.Contains("PlayResX: 1000\nPlayResY: 500", ass, StringComparison.Ordinal);
        Assert.Contains(@"\pos(120,110)", ass, StringComparison.Ordinal);
        Assert.Contains(@"Dialogue: 2,0:00:01.75,0:00:02.00,Mark,", ass, StringComparison.Ordinal);
        Assert.Contains(@"\fscx220\fscy220\t(0,250,\fscx100\fscy100)", ass, StringComparison.Ordinal);
        Assert.Contains(@"Dialogue: 2,0:00:02.00,0:00:02.40,Mark,", ass, StringComparison.Ordinal);
        Assert.Contains(@"\fscx250\fscy250\alpha&HFF&", ass, StringComparison.Ordinal);
        Assert.Contains(@"\1c&H1E11E8&", ass, StringComparison.Ordinal);
    }

    [Fact]
    public void A_target_is_found_in_the_frame_through_the_area_the_video_shows()
    {
        // The window at (100, 200) in logical units, recorded at twice the pixels.
        var scene = Scene(1600, 1200, Tap(1, new BddBox(480, 490, 40, 20))) with { Source = new BddBox(100, 200, 800, 600) };

        Assert.Contains(@"\pos(800,600)", Ass(scene), StringComparison.Ordinal);
    }

    [Fact]
    public void A_scene_smaller_than_the_video_is_padded_as_the_composer_pads_it()
    {
        var scene = Scene(1000, 500, Tap(1, new BddBox(480, 240, 40, 20)));
        var timeline = new BddVideoTimeline("T", [scene]);

        var ass = BddTouchOverlay.Ass(timeline, 1000, 1000, BddTouchMarks.Default)!;

        Assert.Contains(@"\pos(500,500)", ass, StringComparison.Ordinal);
    }

    [Fact]
    public void A_swipe_is_a_dot_travelling_from_its_start_to_its_end_and_an_arrow_pointing_its_way()
    {
        var swipe = new BddTimedInteraction(InteractionKind.Swipe, 3, new BddBox(0, 0, 400, 400)) { From = new BddSpot(200, 100), To = new BddSpot(200, 300) };

        var ass = Ass(Scene(400, 400, swipe));

        Assert.Contains(@"\move(200,100,200,300,0,400)", ass, StringComparison.Ordinal);
        Assert.Contains(@"Dialogue: 2,0:00:02.60,", ass, StringComparison.Ordinal);
        Assert.Contains(@"\pos(200,300)\org(200,300)\frz-90", ass, StringComparison.Ordinal);
    }

    [Fact]
    public void Typing_outlines_the_field_with_a_keyboard_chip_and_a_key_is_a_chip_in_the_corner()
    {
        var ass = Ass(Scene(
            1000,
            500,
            new BddTimedInteraction(InteractionKind.Type, 1, new BddBox(100, 100, 300, 40)),
            new BddTimedInteraction(InteractionKind.Key, 2, new BddBox(0, 0, 0, 0)) { Key = "Back" }));

        Assert.Contains(",Chip,,0,0,0,,{\\an3\\pos(400,100)\\fad(80,150)}⌨", ass, StringComparison.Ordinal);
        Assert.Contains("Dialogue: 3,0:00:02.00,0:00:02.60,Chip,,0,0,0,,{\\fad(80,150)}← Back", ass, StringComparison.Ordinal);
        Assert.DoesNotContain("text", ass, StringComparison.Ordinal);
    }

    [Fact]
    public void Setting_a_fields_text_which_clears_it_first_is_one_keyboard_chip()
    {
        var field = new BddBox(100, 100, 300, 40);

        var ass = Ass(Scene(1000, 500, new BddTimedInteraction(InteractionKind.Clear, 1, field), new BddTimedInteraction(InteractionKind.Type, 1.01, field)));

        Assert.Single(ass.Split('\n'), line => line.Contains(",Chip,", StringComparison.Ordinal));
        Assert.DoesNotContain("⌫", ass, StringComparison.Ordinal);
    }

    [Fact]
    public void A_tap_at_the_start_of_a_scene_starts_its_ring_with_the_scene_on_the_videos_clock()
    {
        var first = Scene(1000, 500) with { Duration = 5 };
        var second = Scene(1000, 500, Tap(5.1, new BddBox(100, 100, 40, 20)));

        var ass = BddTouchOverlay.Ass(new BddVideoTimeline("T", [first, second]), 1000, 500, BddTouchMarks.Default)!;

        Assert.Contains("Dialogue: 2,0:00:05.00,0:00:05.10,Mark,", ass, StringComparison.Ordinal);
        Assert.DoesNotContain("0:00:04.", ass, StringComparison.Ordinal);
    }

    [Fact]
    public void Off_draws_nothing_and_outline_only_draws_no_rings_or_dots()
    {
        var scene = Scene(1000, 500, Tap(2, new BddBox(100, 100, 40, 20)));
        var timeline = new BddVideoTimeline("T", [scene]);

        Assert.Null(BddTouchOverlay.Ass(timeline, 1000, 500, BddTouchMarks.Default with { Mode = BddTouchMode.Off }));
        var outline = BddTouchOverlay.Ass(timeline, 1000, 500, BddTouchMarks.Default with { Mode = BddTouchMode.OutlineOnly })!;
        Assert.Contains(@"\pos(120,110)\fad(80,150)", outline, StringComparison.Ordinal);
        Assert.DoesNotContain(@"\fscx220", outline, StringComparison.Ordinal);
        Assert.Null(BddTouchOverlay.Ass(new BddVideoTimeline("T", [Scene(1000, 500)]), 1000, 500, BddTouchMarks.Default));
    }

    [Fact]
    public void The_accent_is_the_scripts_then_the_environments_then_Exact_red()
    {
        Assert.Equal("&H1E11E8&", BddTouchOverlay.AssColor("#E8111E"));
        Assert.Equal("#E8111E", BddTouchMarks.For(null, _ => null).Color);
        Assert.Equal("#00A0FF", BddTouchMarks.For(null, name => name == BddTouchMarks.ColorVariable ? "#00A0FF" : null).Color);
        Assert.Equal("#E8111E", BddTouchMarks.For(null, _ => "red").Color);
        var script = new BddVideoScript("a.video.md", "A", null, null, []) { TouchColor = "#112233", Touches = BddTouchMode.OutlineOnly };
        Assert.Equal(new BddTouchMarks(BddTouchMode.OutlineOnly, "#112233"), BddTouchMarks.For(script, _ => "#00A0FF"));
        Assert.Equal("#445566", BddTouchMarks.For(null, _ => "#00A0FF", setting: "#445566").Color);
        Assert.Equal("#00A0FF", BddTouchMarks.For(null, _ => "#00A0FF", setting: "not a colour").Color);
        Assert.Equal(BddTouchMode.OutlineOnly, BddTouchMarks.ParseMode("Outline-only"));
        Assert.Null(BddTouchMarks.ParseMode("sometimes"));
    }

    private static string Ass(BddSceneTimeline scene)
        => BddTouchOverlay.Ass(new BddVideoTimeline("T", [scene]), scene.Width, scene.Height, BddTouchMarks.Default)!;

    private static BddTimedInteraction Tap(double at, BddBox bounds) => new(InteractionKind.Tap, at, bounds);

    private static BddSceneTimeline Scene(int width, int height, params BddTimedInteraction[] interactions)
        => new(1, "Scene", null, null, 10, BddStepResultStatus.Passed, [], [], [], width, height, Interactions: interactions);
}
