namespace Bruinet.Blazor.Tests.Controls;

public class DateInputControlTests
{
    private readonly Mock<IHtmlTestContext> _mockContext;
    private readonly Mock<IHtmlElement> _mockElement;
    private readonly TestPage _page;

    public DateInputControlTests()
    {
        _mockContext = MockHtmlFactory.CreateMockContext();
        _mockElement = MockHtmlFactory.CreateMockElement();
        MockHtmlFactory.SetupFindElement(_mockContext, _mockElement);
        _page = new TestPage(_mockContext.Object);
    }

    [Fact]
    public void GetDate_ReturnsCurrentDate()
    {
        _mockElement.Setup(e => e.InputValue).Returns("2025-01-15");

        Assert.Equal(new DateOnly(2025, 1, 15), _page.TestDateInput.GetDate());
    }

    [Fact]
    public void SetDate_FillsFormattedDate()
    {
        _page.TestDateInput.SetDate(new DateOnly(2025, 6, 20));

        _mockElement.Verify(e => e.Fill("2025-06-20"), Times.Once);
    }

    [Fact]
    public void GetMinimum_ReturnsMinAttribute()
    {
        _mockElement.Setup(e => e.GetAttribute("min")).Returns("2020-01-01");

        Assert.Equal(new DateOnly(2020, 1, 1), _page.TestDateInput.GetMinimum());
    }

    [Fact]
    public void GetMaximum_ReturnsMaxAttribute()
    {
        _mockElement.Setup(e => e.GetAttribute("max")).Returns("2030-12-31");

        Assert.Equal(new DateOnly(2030, 12, 31), _page.TestDateInput.GetMaximum());
    }

    [Fact]
    public void IsExists_WhenExists_ReturnsTrue()
    {
        Assert.True(_page.TestDateInput.IsExists());
    }

    [Fact]
    public void IsEnabled_WhenEnabled_ReturnsTrue()
    {
        Assert.Equal(true, _page.TestDateInput.IsEnabled());
    }

    private sealed class TestPage : BlazorPageObjectBase<TestPage>
    {
        public TestPage(IHtmlTestContext context) : base(context) { }

        public DateInputControl<TestPage> TestDateInput => new(this, "test-date");
    }
}
