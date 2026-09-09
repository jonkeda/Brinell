namespace Brinell.Maui.Tests;

public sealed class PageScopingTests
{
    [Fact]
    public void Page_DoesNotImplementChildContainerContract()
    {
        Assert.DoesNotContain(
            typeof(ScopedPage).GetInterfaces(),
            type => type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(IMauiContainerObject<,>));
    }

    [Fact]
    public void ChildLookup_IsScopedToPageRoot_WithoutGlobalFallback()
    {
        var context = new Mock<IMauiTestContext>();
        context.Setup(c => c.Timeouts).Returns(new TimeoutSettings
        {
            DefaultWait = 100,
            PageLoad = 100,
            ElementFind = 100,
            PollingInterval = 1
        });
        context.Setup(c => c.DefaultLocatorStrategy).Returns(LocatorStrategy.AutomationId);

        var pageRoot = new Mock<IMauiElement>();
        pageRoot.Setup(e => e.TagName).Returns("ContentView");
        pageRoot.Setup(e => e.Visible).Returns(true);
        pageRoot.Setup(e => e.Rect).Returns(new System.Drawing.Rectangle(0, 0, 400, 800));
        context.Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "ScopedPage")))
            .Returns([pageRoot.Object]);

        var globalDuplicate = new Mock<IMauiElement>();
        context.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "Save")))
            .Returns(globalDuplicate.Object);
        pageRoot.Setup(e => e.FindElement(It.Is<Locator>(l => l.Value == "Save"), 0))
            .Throws(new ElementNotFoundException("Not in page"));

        var page = new ScopedPage(context.Object);

        Assert.False(page.Save.IsExists());
        context.Verify(c => c.TryFindElement(It.Is<Locator>(l => l.Value == "Save")), Times.Never);
        pageRoot.Verify(e => e.FindElement(It.Is<Locator>(l => l.Value == "Save"), 0), Times.Once);
    }

    [Fact]
    public void FindElements_IsScopedToPageRoot()
    {
        var context = CreateContext();
        var pageRoot = CreateUsableElement();
        var pageChildren = new[] { CreateUsableElement().Object, CreateUsableElement().Object };
        var globalChild = CreateUsableElement();

        context.Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "ScopedPage")))
            .Returns([pageRoot.Object]);
        context.Setup(c => c.FindElements(It.Is<Locator>(l => l.Value == "Row")))
            .Returns([globalChild.Object]);
        pageRoot.Setup(e => e.FindElements(It.Is<Locator>(l => l.Value == "Row"), 0))
            .Returns(pageChildren);

        var actual = new ScopedPage(context.Object).FindElements(Locator.ByAutomationId("Row"));

        Assert.Equal(pageChildren, actual);
        context.Verify(c => c.FindElements(It.Is<Locator>(l => l.Value == "Row")), Times.Never);
    }

    [Fact]
    public void HiddenCachedPageRoot_IsReplacedByCurrentRoot()
    {
        var context = CreateContext();
        var firstRootVisible = true;
        var firstRoot = CreateUsableElement();
        firstRoot.Setup(e => e.Visible).Returns(() => firstRootVisible);
        var secondRoot = CreateUsableElement();

        context.SetupSequence(c => c.FindElements(It.Is<Locator>(l => l.Value == "ScopedPage")))
            .Returns([firstRoot.Object])
            .Returns([secondRoot.Object]);

        var page = new ScopedPage(context.Object);
        Assert.True(page.IsLoaded());

        firstRootVisible = false;

        Assert.True(page.IsLoaded());
        context.Verify(
            c => c.FindElements(It.Is<Locator>(l => l.Value == "ScopedPage")),
            Times.Exactly(2));
    }

    [Fact]
    public void BusySentinel_UsesExplicitDriverRootScope()
    {
        var context = CreateContext();
        var sentinel = CreateUsableElement();
        sentinel.Setup(e => e.Text).Returns("False");
        context.Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == "UITest_IsBusy")))
            .Returns(sentinel.Object);

        var actual = new ScopedPage(context.Object).BusySentinel.GetText();

        Assert.Equal("False", actual);
        context.Verify(c => c.FindElement(It.Is<Locator>(l => l.Value == "UITest_IsBusy")), Times.Once);
        context.Verify(c => c.FindElements(It.Is<Locator>(l => l.Value == "ScopedPage")), Times.Never);
    }

    [Fact]
    public void PopupPage_UsesDeclaredRootWithoutGlobalChildFallback()
    {
        var context = CreateContext();
        var popupRoot = CreateUsableElement();
        var popupChild = CreateUsableElement();
        context.Setup(c => c.FindElement(It.Is<Locator>(l => l.Value == "PopupRoot")))
            .Returns(popupRoot.Object);
        popupRoot.Setup(e => e.FindElement(It.Is<Locator>(l => l.Value == "Accept"), 0))
            .Returns(popupChild.Object);

        var actual = new PopupPage(context.Object).Accept.IsExists();

        Assert.True(actual);
        context.Verify(c => c.FindElement(It.Is<Locator>(l => l.Value == "Accept")), Times.Never);
    }

    private static Mock<IMauiTestContext> CreateContext()
    {
        var context = new Mock<IMauiTestContext>();
        context.Setup(c => c.Timeouts).Returns(new TimeoutSettings
        {
            DefaultWait = 100,
            PageLoad = 100,
            ElementFind = 100,
            PollingInterval = 1
        });
        context.Setup(c => c.DefaultLocatorStrategy).Returns(LocatorStrategy.AutomationId);
        return context;
    }

    private static Mock<IMauiElement> CreateUsableElement()
    {
        var element = new Mock<IMauiElement>();
        element.Setup(e => e.TagName).Returns("ContentView");
        element.Setup(e => e.Visible).Returns(true);
        element.Setup(e => e.Rect).Returns(new System.Drawing.Rectangle(0, 0, 400, 800));
        return element;
    }

    private sealed class ScopedPage(IMauiTestContext context) : PageObjectBase<ScopedPage>(context)
    {
        public override string Name => "ScopedPage";

        public Button<ScopedPage> Save => new(this, "Save");
    }

    private sealed class PopupPage(IMauiTestContext context) : PageObjectBase<PopupPage>(context)
    {
        public override string Name => "PopupPage";

        public Button<PopupPage> Accept => new(this, "Accept");

        protected override IMauiElement FindContainerRootElement()
            => Context.FindElement(Locator.ByAutomationId("PopupRoot"));
    }
}