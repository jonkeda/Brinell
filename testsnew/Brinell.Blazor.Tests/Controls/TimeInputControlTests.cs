namespace Brinell.Blazor.Tests.Controls;

public class TimeInputControlTests
{
    private readonly Mock<IHtmlTestContext> _mockContext;
    private readonly Mock<IHtmlElement> _mockElement;
    private readonly TestPage _page;

    public TimeInputControlTests()
    {
        _mockContext = MockHtmlFactory.CreateMockContext();
        _mockElement = MockHtmlFactory.CreateMockElement();
        MockHtmlFactory.SetupFindElement(_mockContext, _mockElement);
        _page = new TestPage(_mockContext.Object);
    }

    [Fact]
    public void GetTime_ReturnsCurrentTime()
    {
        _mockElement.Setup(e => e.InputValue).Returns("14:30");

        Assert.Equal(new TimeOnly(14, 30), _page.TestTimeInput.GetTime());
    }

    [Fact]
    public void SetTime_FillsFormattedTime()
    {
        _page.TestTimeInput.SetTime(new TimeOnly(10, 15));

        _mockElement.Verify(e => e.Fill("10:15"), Times.Once);
    }

    [Fact]
    public void GetMin_ReturnsMinAttribute()
    {
        _mockElement.Setup(e => e.GetAttribute("min")).Returns("08:00");

        Assert.Equal("08:00", _page.TestTimeInput.GetMin());
    }

    [Fact]
    public void GetMax_ReturnsMaxAttribute()
    {
        _mockElement.Setup(e => e.GetAttribute("max")).Returns("17:00");

        Assert.Equal("17:00", _page.TestTimeInput.GetMax());
    }

    [Fact]
    public void IsExists_WhenExists_ReturnsTrue()
    {
        Assert.True(_page.TestTimeInput.IsExists());
    }

    [Fact]
    public void IsEnabled_WhenEnabled_ReturnsTrue()
    {
        Assert.Equal(true, _page.TestTimeInput.IsEnabled());
    }

    private sealed class TestPage : BlazorPageObjectBase<TestPage>
    {
        public TestPage(IHtmlTestContext context) : base(context) { }

        public TimeInputControl<TestPage> TestTimeInput => new(this, "test-time");
    }
}
