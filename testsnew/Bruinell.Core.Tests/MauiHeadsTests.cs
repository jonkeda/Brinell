using Bruinell.Core.Testing;

namespace Bruinell.Core.Tests;

public sealed class MauiHeadsTests
{
    [Theory]
    [InlineData("mauiunit", MauiHead.MauiUnit)]
    [InlineData(" MauiUnit ", MauiHead.MauiUnit)]
    [InlineData("bunit", MauiHead.Device)]
    [InlineData("", MauiHead.Device)]
    [InlineData(null, MauiHead.Device)]
    [InlineData("device", MauiHead.Device)]
    public void Parse_NamesTheHead_TheDeviceUnlessMauiUnit(string? value, MauiHead expected)
        => Assert.Equal(expected, MauiHeads.Parse(value));

    [Fact]
    public void The_MAUI_and_web_heads_are_picked_by_one_variable()
        => Assert.Equal(WebHeads.Variable, MauiHeads.Variable);

    // Run with BRUINELL_HEAD=mauiunit, this test shows as skipped with its reason; without, it runs.
    [DeviceFact("it checks that the attribute skips on the MauiUnit head")]
    public void DeviceFact_RunsOnlyOnTheDeviceHead()
    {
        Assert.False(MauiHeads.IsMauiUnit);
        Assert.Null(new DeviceFactAttribute().Skip);
    }
}
