using Bruinet.Core.Testing;

namespace Bruinet.Core.Tests;

public sealed class MauiHeadsTests
{
    [Theory]
    [InlineData("headless", MauiHead.Headless)]
    [InlineData(" MauiHeadless ", MauiHead.Headless)]
    [InlineData("bunit", MauiHead.Device)]
    [InlineData("", MauiHead.Device)]
    [InlineData(null, MauiHead.Device)]
    [InlineData("device", MauiHead.Device)]
    public void Parse_NamesTheHead_TheDeviceUnlessMauiHeadless(string? value, MauiHead expected)
        => Assert.Equal(expected, MauiHeads.Parse(value));

    [Fact]
    public void The_MAUI_and_web_heads_are_picked_by_one_variable()
        => Assert.Equal(WebHeads.Variable, MauiHeads.Variable);

    // Run with BRUINET_HEAD=headless, this test shows as skipped with its reason; without, it runs.
    [DeviceFact("it checks that the attribute skips on the MauiHeadless head")]
    public void DeviceFact_RunsOnlyOnTheDeviceHead()
    {
        Assert.False(MauiHeads.IsHeadless);
        Assert.Null(new DeviceFactAttribute().Skip);
    }
}
