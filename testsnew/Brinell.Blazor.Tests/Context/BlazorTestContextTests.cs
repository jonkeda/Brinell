using Microsoft.Playwright;

namespace Brinell.Blazor.Tests.Context;

public class BlazorTestContextTests
{
    [Fact]
    public void ForPage_CreatesValidContext()
    {
        var mockPage = new Mock<IPage>();
        mockPage.Setup(p => p.Url).Returns("https://example.com");

        var context = BlazorTestContext.ForPage(mockPage.Object);

        Assert.NotNull(context);
    }

    [Fact]
    public void CurrentUrl_DelegatesToPage()
    {
        var mockPage = new Mock<IPage>();
        mockPage.Setup(p => p.Url).Returns("https://example.com/test");

        var context = BlazorTestContext.ForPage(mockPage.Object);

        Assert.Equal("https://example.com/test", context.CurrentUrl);
    }

    [Fact]
    public void PageTitle_DelegatesToPage()
    {
        var mockPage = new Mock<IPage>();
        mockPage.Setup(p => p.Url).Returns("https://example.com");
        mockPage.Setup(p => p.TitleAsync()).ReturnsAsync("Test Page");

        var context = BlazorTestContext.ForPage(mockPage.Object);

        Assert.Equal("Test Page", context.PageTitle);
    }

    [Fact]
    public void Timeouts_ReturnsDefaultSettings()
    {
        var mockPage = new Mock<IPage>();
        mockPage.Setup(p => p.Url).Returns("https://example.com");

        var context = BlazorTestContext.ForPage(mockPage.Object);

        Assert.NotNull(context.Timeouts);
    }

    [Fact]
    public void Context_ReturnsSelf()
    {
        var mockPage = new Mock<IPage>();
        mockPage.Setup(p => p.Url).Returns("https://example.com");

        var context = BlazorTestContext.ForPage(mockPage.Object);

        Assert.Same(context, context.Context);
    }

    [Fact]
    public void NavigateTo_DelegatesToPage()
    {
        var mockPage = new Mock<IPage>();
        mockPage.Setup(p => p.Url).Returns("https://example.com");
        mockPage.Setup(p => p.GotoAsync(It.IsAny<string>(), null))
            .ReturnsAsync((IResponse?)null);

        var context = BlazorTestContext.ForPage(mockPage.Object);
        context.NavigateTo("https://example.com/page2");

        mockPage.Verify(p => p.GotoAsync("https://example.com/page2", null), Times.Once);
    }

    [Fact]
    public void DefaultLocatorStrategy_ReturnsCss()
    {
        var mockPage = new Mock<IPage>();
        mockPage.Setup(p => p.Url).Returns("https://example.com");

        var context = BlazorTestContext.ForPage(mockPage.Object);

        Assert.Equal(LocatorStrategy.Css, context.DefaultLocatorStrategy);
    }
}
