using Xunit;

namespace Brinell.Maui.UITests;

/// <summary>Step 35: the timing report flags what step 33 would have needed flagging, and nothing else.</summary>
[Trait("Category", "Unit")]
public class TestTimingReportTests
{
    /// <summary>
    /// Step 33's regression, replayed: a two-second wait added to every test in a class.
    /// </summary>
    [Fact]
    public void AClassThatStartedWaiting_IsFlagged()
    {
        var rows = new[]
        {
            new TestTimings.Row("Navigation", "NavigationVerbTests", "A", 2600),
            new TestTimings.Row("Navigation", "NavigationVerbTests", "B", 2500),
        };
        var baseline = new Dictionary<string, double> { ["Navigation/NavigationVerbTests"] = 550 };

        var report = TestTimings.Render(rows, baseline);

        Assert.Contains("**slower**", report);
        Assert.Contains("1 class(es) are over", report);
    }

    /// <summary>
    /// A tiny test that doubles is noise, not a regression - the absolute floor keeps it off.
    /// </summary>
    [Fact]
    public void AFastClassThatDoubles_IsNotFlagged()
    {
        var rows = new[] { new TestTimings.Row("Buttons", "ButtonTests", "A", 90) };
        var baseline = new Dictionary<string, double> { ["Buttons/ButtonTests"] = 40 };

        var report = TestTimings.Render(rows, baseline);

        Assert.DoesNotContain("**slower**", report);
        Assert.Contains("No class is markedly slower", report);
    }
}
