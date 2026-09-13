using Brinell.Maui.Controls.Container;

namespace Brinell.Maui.Tests.Semantic;

/// <summary>
/// Covers the lookup a control does when a plain lookup finds nothing: scroll once, and ask the
/// element that scrolls for its scope.
/// </summary>
/// <remarks>
/// <para>
/// Steps 100a and 100b. The sweep matters on Android, which leaves content outside the viewport
/// out of the accessibility tree, and it is expensive - a UiScrollable walk of the whole list.
/// Two things are pinned here because neither is visible in a Windows run, where the driver's
/// sweep does nothing: that a poll sweeps once rather than once per tick, and that a control
/// inside a scroll view asks the driver to scroll <i>that</i> view.
/// </para>
/// <para>
/// The second was simply not happening before: scopes each implemented the lookup, and the one
/// containers and pages inherited did not scroll at all.
/// </para>
/// </remarks>
public class ScrollLookupTests : SemanticControlTestsBase
{
    private const string MissingId = "PromptDialogView_OKButton";

    private readonly Mock<IMauiDriver> _driver = new();

    public ScrollLookupTests()
    {
        Context.Setup(c => c.Driver).Returns(_driver.Object);
        Context.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == MissingId)))
            .Returns((IMauiElement?)null);
    }

    [Fact]
    public void IsExists_OnAPage_SweepsOnce_LettingTheDriverPickTheScroller()
    {
        Assert.False(Page.PromptOk.IsExists());

        _driver.Verify(
            d => d.TryFindByScrollingWithin(null, It.Is<Locator>(l => l.Value == MissingId)),
            Times.Once);
    }

    [Fact]
    public void WaitExists_SweepsOnTheFirstTickOnly()
    {
        Assert.False(Page.PromptOk.WaitExists(true, timeoutMs: 60));

        _driver.Verify(
            d => d.TryFindByScrollingWithin(It.IsAny<IMauiElement?>(), It.IsAny<Locator>()),
            Times.Once);
    }

    [Fact]
    public void AssertVisibleAfterScroll_SweepsOnTheFirstTickOnly_AndDoesNotWaitOutAFindTimeout()
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();

        Assert.ThrowsAny<Exception>(() => Page.PromptOk.AssertVisibleAfterScroll(true, timeoutMs: 60));

        _driver.Verify(
            d => d.TryFindByScrollingWithin(It.IsAny<IMauiElement?>(), It.IsAny<Locator>()),
            Times.Once);
        Assert.True(clock.ElapsedMilliseconds < 2_000,
            $"An absent element should cost one sweep, not a find timeout per tick; took {clock.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void AControlInsideAScrollView_AsksTheDriverToScrollThatView()
    {
        var scrollRoot = CreateElement("Scroller", 0, 0, 400, 600);
        scrollRoot.Setup(e => e.FindElement(It.IsAny<Locator>(), 0))
            .Throws(new ElementNotFoundException("not realized"));
        Context.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "Scroller")))
            .Returns(scrollRoot.Object);

        var scroller = new ScrollView<TestPage>(Page, "Scroller");

        Assert.False(scroller.Label("BelowTheFold").IsExists());

        _driver.Verify(
            d => d.TryFindByScrollingWithin(
                scrollRoot.Object, It.Is<Locator>(l => l.Value == "BelowTheFold")),
            Times.Once);
    }

    [Fact]
    public void AnElementThePlainLookupFinds_IsNotSweptFor()
    {
        var present = CreateElement(MissingId, 0, 0, 80, 24);
        Context.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == MissingId)))
            .Returns(present.Object);

        Assert.True(Page.PromptOk.IsExists());

        _driver.Verify(
            d => d.TryFindByScrollingWithin(It.IsAny<IMauiElement?>(), It.IsAny<Locator>()),
            Times.Never);
    }
}
