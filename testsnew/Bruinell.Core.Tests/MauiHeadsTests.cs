using Bruinell.Core.Testing;

namespace Bruinell.Core.Tests;

public sealed class MauiHeadsTests
{
    [Theory]
    [InlineData("munit", MauiHead.Munit)]
    [InlineData(" MUnit ", MauiHead.Munit)]
    [InlineData("bunit", MauiHead.Device)]
    [InlineData("", MauiHead.Device)]
    [InlineData(null, MauiHead.Device)]
    [InlineData("device", MauiHead.Device)]
    public void Parse_NamesTheHead_TheDeviceUnlessMunit(string? value, MauiHead expected)
        => Assert.Equal(expected, MauiHeads.Parse(value));

    [Fact]
    public void The_MAUI_and_web_heads_are_picked_by_one_variable()
        => Assert.Equal(WebHeads.Variable, MauiHeads.Variable);

    // Run with BRUINELL_HEAD=munit, this test shows as skipped with its reason; without, it runs.
    [DeviceFact("it checks that the attribute skips on the MUnit head")]
    public void DeviceFact_RunsOnlyOnTheDeviceHead()
    {
        Assert.False(MauiHeads.IsMunit);
        Assert.Null(new DeviceFactAttribute().Skip);
    }
}
