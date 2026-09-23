using Xunit;

namespace Brinell.Scraper.Tests.Services;

using Brinell.Scraper.Models;
using Brinell.Scraper.Services;
using NSubstitute;

public sealed class CorpusToolsTests
{
    [Fact]
    public void FormatSnapshot_ReturnsFormattedDom()
    {
        var snapshot = new DomSnapshot
        {
            PageUrl = "https://example.com/login",
            PageTitle = "Login Page",
            RootElement = new DomElement
            {
                Tag = "form",
                Id = "loginForm",
                Children =
                [
                    new DomElement { Tag = "input", Type = "text", Name = "username" },
                    new DomElement { Tag = "input", Type = "password", Name = "password" },
                    new DomElement { Tag = "button", TextContent = "Sign In" }
                ]
            }
        };

        var result = CorpusTools.FormatSnapshot(snapshot);

        Assert.Contains("https://example.com/login", result);
        Assert.Contains("Login Page", result);
        Assert.Contains("<form", result);
        Assert.Contains("id=\"loginForm\"", result);
        Assert.Contains("<input", result);
        Assert.Contains("name=\"username\"", result);
        Assert.Contains("Sign In", result);
    }

    [Fact]
    public void FormatElement_OmitsNullAttributes()
    {
        var element = new DomElement
        {
            Tag = "button",
            TextContent = "Click me"
        };

        var sb = new System.Text.StringBuilder();
        CorpusTools.FormatElement(sb, element, indent: 0);
        var result = sb.ToString();

        Assert.Contains("<button", result);
        Assert.Contains("Click me", result);
        Assert.DoesNotContain("id=", result);
        Assert.DoesNotContain("class=", result);
    }
}
