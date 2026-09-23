namespace Brinell.Blazor.Tests.Controls;

public class TabContainerControlTests
{
    private readonly Mock<IHtmlTestContext> _mockContext;
    private readonly Mock<IHtmlElement> _mockElement;
    private readonly List<Mock<IHtmlElement>> _tabMocks;
    private readonly TestPage _page;

    public TabContainerControlTests()
    {
        _mockContext = MockHtmlFactory.CreateMockContext();
        _mockElement = MockHtmlFactory.CreateMockElement();
        MockHtmlFactory.SetupFindElement(_mockContext, _mockElement);

        _tabMocks = new List<Mock<IHtmlElement>>
        {
            CreateChildElement("Tab 1"),
            CreateChildElement("Tab 2"),
            CreateChildElement("Tab 3"),
        };

        var tabElements = _tabMocks.Select(m => m.Object).ToList().AsReadOnly();
        _mockElement.Setup(e => e.FindElements(It.IsAny<Locator>(), It.IsAny<int>()))
            .Returns(tabElements);

        _page = new TestPage(_mockContext.Object);
    }

    [Fact]
    public void TabCount_ReturnsCount()
    {
        Assert.Equal(3, _page.Tabs.TabCount);
    }

    [Fact]
    public void SelectTab_ByIndex_ClicksTab()
    {
        _page.Tabs.SelectTab(1);

        _tabMocks[1].Verify(e => e.Click(), Times.Once);
    }

    [Fact]
    public void SelectTab_ByIndex_FirstTab_ClicksTab()
    {
        _page.Tabs.SelectTab(0);

        _tabMocks[0].Verify(e => e.Click(), Times.Once);
    }

    [Fact]
    public void SelectTab_ByText_ClicksMatchingTab()
    {
        _page.Tabs.SelectTab("Tab 2");

        _tabMocks[1].Verify(e => e.Click(), Times.Once);
    }

    [Fact]
    public void SelectTab_ByText_NotFound_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => _page.Tabs.SelectTab("Nonexistent Tab"));
    }

    [Fact]
    public void SelectTab_OutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _page.Tabs.SelectTab(5));
    }

    [Fact]
    public void SelectTab_NegativeIndex_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _page.Tabs.SelectTab(-1));
    }

    private static Mock<IHtmlElement> CreateChildElement(string? text)
    {
        var mock = new Mock<IHtmlElement>();
        mock.Setup(e => e.Text).Returns(text);
        return mock;
    }

    private sealed class TestPage : BlazorPageObjectBase<TestPage>
    {
        public TestPage(IHtmlTestContext context) : base(context) { }

        public TestTabContainer Tabs => new(this, Locator.ByCss("#tabs"));
    }

    private sealed class TestTabContainer : TabContainerControl<TestPage, TestTabContainer>
    {
        public TestTabContainer(IHtmlScope<TestPage> parentScope, Locator locator)
            : base(parentScope, locator) { }
    }
}
