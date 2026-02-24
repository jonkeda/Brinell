namespace Brinell.Blazor.Tests.Controls;

public class TableControlTests
{
    private readonly Mock<IHtmlTestContext> _mockContext;
    private readonly Mock<IHtmlElement> _mockElement;
    private readonly TestPage _page;

    public TableControlTests()
    {
        _mockContext = MockHtmlFactory.CreateMockContext();
        _mockElement = MockHtmlFactory.CreateMockElement();
        MockHtmlFactory.SetupFindElement(_mockContext, _mockElement);

        var headerElements = new List<IHtmlElement>
        {
            CreateChildElement("Header1").Object,
            CreateChildElement("Header2").Object,
            CreateChildElement("Header3").Object,
        };

        var rowElements = new List<IHtmlElement>
        {
            CreateChildElement("Row1").Object,
            CreateChildElement("Row2").Object,
        };

        var cellElements = new List<IHtmlElement>
        {
            CreateChildElement("Cell1").Object,
            CreateChildElement("Cell2").Object,
        };

        _mockElement.Setup(e => e.FindElements(
                It.Is<Locator>(l => l.Value == "thead th"), It.IsAny<int>()))
            .Returns(headerElements.AsReadOnly());

        _mockElement.Setup(e => e.FindElements(
                It.Is<Locator>(l => l.Value == "tbody tr"), It.IsAny<int>()))
            .Returns(rowElements.AsReadOnly());

        _mockElement.Setup(e => e.FindElements(
                It.Is<Locator>(l => l.Value.StartsWith("tbody tr:nth-child(")), It.IsAny<int>()))
            .Returns(cellElements.AsReadOnly());

        _page = new TestPage(_mockContext.Object);
    }

    [Fact]
    public void RowCount_ReturnsCorrectCount()
    {
        Assert.Equal(2, _page.TestTable.RowCount);
    }

    [Fact]
    public void ColumnCount_ReturnsHeaderCount()
    {
        Assert.Equal(3, _page.TestTable.ColumnCount);
    }

    [Fact]
    public void GetCellText_ReturnsText()
    {
        Assert.Equal("Cell1", _page.TestTable.GetCellText(0, 0));
    }

    [Fact]
    public void GetCellText_SecondColumn_ReturnsText()
    {
        Assert.Equal("Cell2", _page.TestTable.GetCellText(0, 1));
    }

    [Fact]
    public void GetHeaderText_ReturnsText()
    {
        Assert.Equal("Header1", _page.TestTable.GetHeaderText(0));
    }

    [Fact]
    public void GetHeaderText_SecondColumn_ReturnsText()
    {
        Assert.Equal("Header2", _page.TestTable.GetHeaderText(1));
    }

    [Fact]
    public void GetRowTexts_ReturnsRow()
    {
        var texts = _page.TestTable.GetRowTexts(0);

        Assert.Equal(2, texts.Count);
        Assert.Equal("Cell1", texts[0]);
        Assert.Equal("Cell2", texts[1]);
    }

    [Fact]
    public void IsExists_WhenExists_ReturnsTrue()
    {
        Assert.True(_page.TestTable.IsExists());
    }

    [Fact]
    public void IsExists_WhenNotFound_ReturnsFalse()
    {
        MockHtmlFactory.SetupElementNotFound(_mockContext);

        Assert.False(_page.TestTable.IsExists());
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

        public TableControl<TestPage> TestTable => new(this, "test-table");
    }
}
