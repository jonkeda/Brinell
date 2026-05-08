using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using Xunit;

namespace Brinell.Scraper.Tests.Services;

public sealed class PromptTruncatorTests
{
    private static DomSnapshot DummySnapshot() => new()
    {
        SiteName = "test",
        PageName = "p",
        CapturedAt = DateTimeOffset.UtcNow,
        RootElement = new DomElement { Tag = "html" },
    };

    [Fact]
    public void Truncate_ShortPrompt_ReturnsUnchanged()
    {
        const string prompt = "short prompt with <input id='a'>";
        var result = PromptTruncator.TruncatePageObjectPrompt(prompt, DummySnapshot(), 1000);
        Assert.Equal(prompt, result);
    }

    [Fact]
    public void Truncate_StripsScriptAndStyleAndComments()
    {
        var prompt =
            "<input id='a'>" +
            new string('x', 200) +
            "<script>var z = " + new string('y', 500) + ";</script>" +
            "<style>." + new string('s', 400) + "{ color: red; }</style>" +
            "<!-- " + new string('c', 300) + " -->" +
            "<button>Go</button>";

        var result = PromptTruncator.TruncatePageObjectPrompt(prompt, DummySnapshot(), 600);

        Assert.NotNull(result);
        Assert.True(result!.Length <= 600,
            $"Expected truncated length <= 600 but was {result.Length}");
        Assert.DoesNotContain("<script", result);
        Assert.DoesNotContain("<style", result);
        Assert.DoesNotContain("<!--", result);
        // Actionable content preserved.
        Assert.Contains("input", result);
        Assert.Contains("button", result);
    }

    [Fact]
    public void Truncate_ReturnsNull_WhenStillTooLargeAfterAllStrategies()
    {
        // Prompt of pure actionable elements that cannot be reduced below budget.
        var prompt = string.Concat(Enumerable.Repeat("<input id='a'>actionable text here ", 200));
        var result = PromptTruncator.TruncatePageObjectPrompt(prompt, DummySnapshot(), 100);

        Assert.Null(result);
    }

    [Fact]
    public void EstimateTokens_UsesCharsPerFour()
    {
        Assert.Equal(0, PromptTruncator.EstimateTokens(""));
        Assert.Equal(2, PromptTruncator.EstimateTokens("12345678"));
    }
}
