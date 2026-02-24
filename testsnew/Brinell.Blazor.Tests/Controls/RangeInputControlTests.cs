namespace Brinell.Blazor.Tests.Controls;

public class RangeInputControlTests
{
    private readonly Mock<IHtmlTestContext> _mockContext;
    private readonly Mock<IHtmlElement> _mockElement;
    private readonly TestPage _page;

    public RangeInputControlTests()
    {
        _mockContext = MockHtmlFactory.CreateMockContext();
        _mockElement = MockHtmlFactory.CreateMockElement();
        MockHtmlFactory.SetupFindElement(_mockContext, _mockElement);

        _mockElement.Setup(e => e.InputValue).Returns("75");
        _mockElement.Setup(e => e.GetAttribute("min")).Returns("0");
        _mockElement.Setup(e => e.GetAttribute("max")).Returns("100");
        _mockElement.Setup(e => e.GetAttribute("step")).Returns("1");

        _page = new TestPage(_mockContext.Object);
    }

    [Fact]
    public void GetNumericValue_ReturnsDoubleValue()
    {
        Assert.Equal(75, _page.TestRange.GetNumericValue());
    }

    [Fact]
    public void GetNumericValue_WhenNonNumeric_ReturnsZero()
    {
        _mockElement.Setup(e => e.InputValue).Returns("abc");

        Assert.Equal(0, _page.TestRange.GetNumericValue());
    }

    [Fact]
    public void SetNumericValue_FillsValue()
    {
        _page.TestRange.SetNumericValue(50.5);

        _mockElement.Verify(e => e.Fill("50.5"), Times.Once);
    }

    [Fact]
    public void GetMin_ReturnsAttribute()
    {
        Assert.Equal("0", _page.TestRange.GetMin());
    }

    [Fact]
    public void GetMax_ReturnsAttribute()
    {
        Assert.Equal("100", _page.TestRange.GetMax());
    }

    [Fact]
    public void GetStep_ReturnsAttribute()
    {
        Assert.Equal("1", _page.TestRange.GetStep());
    }

    [Fact]
    public void GetValue_ReturnsInputValue()
    {
        Assert.Equal("75", _page.TestRange.GetValue());
    }

    [Fact]
    public void SetValue_FillsStringValue()
    {
        _page.TestRange.SetValue("42");

        _mockElement.Verify(e => e.Fill("42"), Times.Once);
    }

    [Fact]
    public void IsExists_WhenExists_ReturnsTrue()
    {
        Assert.True(_page.TestRange.IsExists());
    }

    [Fact]
    public void IsExists_WhenNotFound_ReturnsFalse()
    {
        MockHtmlFactory.SetupElementNotFound(_mockContext);

        Assert.False(_page.TestRange.IsExists());
    }

    private sealed class TestPage : BlazorPageObjectBase<TestPage>
    {
        public TestPage(IHtmlTestContext context) : base(context) { }

        public RangeInputControl<TestPage> TestRange => new(this, "test-range");
    }
}
