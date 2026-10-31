using Bruinet.Core.Diagnostics;

namespace Bruinet.Core.Tests;

public sealed class AutWindowTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("background", false)]
    [InlineData("shown", true)]
    [InlineData(" Shown ", true)]
    [InlineData("typo", false)]
    public void IsShown_ReadsTheProcessVariable(string? value, bool shown)
        => Assert.Equal(shown, AutWindow.IsShown(read: name => name == AutWindow.Variable ? value : null));

    [Fact]
    public void IsShown_TheLaunchEnvironmentWins()
    {
        var launch = new Dictionary<string, string> { [AutWindow.Variable] = "background" };

        Assert.False(AutWindow.IsShown(launch, _ => "shown"));
    }

    [Theory]
    [InlineData("0,0,1432,1040", 0, 0, 1432, 1040)]
    [InlineData("-1920, 10, 800, 600", -1920, 10, 800, 600)]
    public void Bounds_ReadsFourNumbers(string value, int x, int y, int width, int height)
        => Assert.Equal((x, y, width, height), AutWindow.Bounds(_ => value));

    [Theory]
    [InlineData(null)]
    [InlineData("1,2,3")]
    [InlineData("a,b,c,d")]
    [InlineData("0,0,0,600")]
    public void Bounds_IsNullWhenNotFourSizes(string? value)
        => Assert.Null(AutWindow.Bounds(_ => value));
}
