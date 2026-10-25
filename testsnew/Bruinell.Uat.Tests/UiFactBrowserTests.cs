using Bruinell.Core.Testing;
using Xunit;

namespace Bruinell.Uat.Tests;

/// <summary>A <c>[UiFact(Browser = "why")]</c> is skipped on the bUnit head only (AD-030).</summary>
public sealed class UiFactBrowserTests
{
    [Fact]
    public void Browser_KeepsItsReason()
        => Assert.Equal("it measures layout", new UiFactAttribute { Browser = "it measures layout" }.Browser);

    // Run with BRUINELL_HEAD=bunit, this test shows as skipped with its reason; without, it runs.
    [UiFact(Browser = "it checks that the attribute skips on the bUnit head")]
    public void Browser_RunsOnlyOnTheBrowserHead()
    {
        Assert.False(WebHeads.IsBunit);
        Assert.Null(new UiFactAttribute { Browser = "x" }.Skip);
    }

    [Fact]
    public void Browser_Unset_NeverSkips() => Assert.Null(new UiFactAttribute().Skip);
}
