using Bruinell.Core.Exceptions;
using Bruinell.Html.Playwright;
using Microsoft.Playwright;

namespace Bruinell.Html.Tests;

/// <summary>
/// Playwright's failures arrive as Bruinell's signals (plan step 3, R5): a detached element as
/// <see cref="StaleElementException"/>, a closed browser as <see cref="AppUnavailableException"/>,
/// and anything else unchanged.
/// </summary>
public class PlaywrightErrorMappingTests
{
    private const string Closed = "Target page, context or browser has been closed";
    private const string Detached = "Element is not attached to the DOM";

    private static Mock<ILocator> Locator()
    {
        var locator = new Mock<ILocator>();
        locator.Setup(l => l.First).Returns(() => locator.Object);
        locator.Setup(l => l.Nth(It.IsAny<int>())).Returns(() => locator.Object);
        return locator;
    }

    [Fact]
    public void ADetachedElement_IsStale()
    {
        var locator = Locator();
        locator.Setup(l => l.IsVisibleAsync(It.IsAny<LocatorIsVisibleOptions?>())).ThrowsAsync(new PlaywrightException(Detached));

        var error = Assert.Throws<StaleElementException>(() => new PlaywrightHtmlElement(locator.Object).Visible);

        Assert.IsType<PlaywrightException>(error.InnerException);
    }

    /// <summary>
    /// A read waits for its element no longer than <see cref="PlaywrightHtmlElement.ReadAttachMs"/>,
    /// and a node that is gone by then is stale: the poll finds the element again, instead of one
    /// read waiting out the call (R2, R5).
    /// </summary>
    [Fact]
    public void ARead_OfANodeThatIsGone_IsStale_AfterAShortWait()
    {
        var locator = Locator();
        locator.Setup(l => l.InnerTextAsync(It.IsAny<LocatorInnerTextOptions?>()))
            .ThrowsAsync(new TimeoutException("Timeout 250ms exceeded."));

        Assert.Throws<StaleElementException>(() => new PlaywrightHtmlElement(locator.Object).Text);

        locator.Verify(l => l.InnerTextAsync(
            It.Is<LocatorInnerTextOptions?>(o => o != null && o.Timeout == PlaywrightHtmlElement.ReadAttachMs)), Times.Once);
    }

    [Fact]
    public void AClosedBrowser_IsAppUnavailable_OnAnElement()
    {
        var locator = Locator();
        locator.Setup(l => l.ClickAsync(It.IsAny<LocatorClickOptions?>())).ThrowsAsync(new PlaywrightException(Closed));

        Assert.Throws<AppUnavailableException>(() => new PlaywrightHtmlElement(locator.Object).Click());
    }

    [Fact]
    public void AClosedBrowser_IsAppUnavailable_OnTheDriver()
    {
        var child = Locator();
        child.Setup(l => l.CountAsync()).ThrowsAsync(new PlaywrightException(Closed));
        var root = Locator();
        root.Setup(l => l.Locator(It.IsAny<string>(), It.IsAny<LocatorLocatorOptions?>())).Returns(child.Object);
        var page = new Mock<IPage>();
        page.Setup(p => p.Locator(It.IsAny<string>(), It.IsAny<PageLocatorOptions?>())).Returns(root.Object);

        Assert.Throws<AppUnavailableException>(() => new PlaywrightHtmlDriver(page.Object).FindElements(Bruinell.Core.Locators.Locator.ByCss("button")));
    }

    [Fact]
    public void AnyOtherPlaywrightError_StaysAsItIs()
    {
        var locator = Locator();
        locator.Setup(l => l.InnerTextAsync(It.IsAny<LocatorInnerTextOptions?>()))
            .ThrowsAsync(new PlaywrightException("strict mode violation: locator resolved to 2 elements"));

        Assert.Throws<PlaywrightException>(() => new PlaywrightHtmlElement(locator.Object).Text);
    }

    [Fact]
    public void InstanceKey_IsReadFromTheNode_AndReportsADetachedOneAsStale()
    {
        var locator = Locator();
        locator.SetupSequence(l => l.EvaluateAsync<string>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<LocatorEvaluateOptions?>()))
            .ReturnsAsync("key-1")
            .ThrowsAsync(new PlaywrightException(Detached));
        var element = new PlaywrightHtmlElement(locator.Object);

        Assert.Equal("key-1", element.InstanceKey);
        Assert.Throws<StaleElementException>(() => element.InstanceKey);
    }
}
