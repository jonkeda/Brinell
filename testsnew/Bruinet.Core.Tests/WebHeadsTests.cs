using Bruinet.Core.Testing;

namespace Bruinet.Core.Tests;

public sealed class WebHeadsTests
{
    [Theory]
    [InlineData("bunit", WebHead.Bunit)]
    [InlineData(" BUnit ", WebHead.Bunit)]
    [InlineData("browser", WebHead.Browser)]
    [InlineData("", WebHead.Browser)]
    [InlineData(null, WebHead.Browser)]
    [InlineData("chromium", WebHead.Browser)]
    public void Parse_NamesTheHead_TheBrowserUnlessBunit(string? value, WebHead expected)
        => Assert.Equal(expected, WebHeads.Parse(value));

    // Run with BRUINET_HEAD=bunit, this test shows as skipped with its reason; without, it runs.
    [BrowserFact("it checks that the attribute skips on the bUnit head")]
    public void BrowserFact_RunsOnlyOnTheBrowserHead()
    {
        Assert.False(WebHeads.IsBunit);
        Assert.Null(new BrowserFactAttribute().Skip);
    }
}
