using System.Drawing;
using Brinell.Core.Recording;
using Brinell.Maui.Appium;
using Xunit;

namespace Brinell.Recording.Tests;

public sealed class AndroidRecorderTests
{
    [Theory]
    [InlineData("Physical size: 1080x2400\n", 1080, 2400)]
    [InlineData("Physical size: 1080x2400\nOverride size: 720x1600\n", 720, 1600)]
    [InlineData("error: no devices\n", 0, 0)]
    public void The_screen_size_is_what_wm_size_says_the_override_first(string output, int width, int height)
    {
        Assert.Equal(new Size(width, height), AndroidRecorder.ParseSize(output));
    }

    [Fact]
    public void The_segments_are_joined_in_order_quotes_escaped()
    {
        Assert.Equal(
            "file 'C:\\v\\a-0.mp4'\nfile 'C:\\v\\it'\\''s-1.mp4'\n",
            AndroidRecorder.ConcatList([@"C:\v\a-0.mp4", @"C:\v\it's-1.mp4"]));
    }

    [Fact]
    public void Only_a_device_is_for_the_Android_recorder()
    {
        Assert.False(new AndroidRecorder().CanRecord(new RecordingTarget.Window(1)));
        Assert.False(new AndroidRecorder().CanRecord(new RecordingTarget.IosSimulator("A")));
    }
}
