using Brinell.Samples.Maui.UITests.ControlObject6.Pages;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Maui.UITests.ControlObject6.Tests;

/// <summary>
/// Counter functionality tests using ControlObject6 API.
/// </summary>
public class CounterTests6 : MauiTestBase6
{
    private readonly MainPageObject6 _mainPage;

    public CounterTests6(ITestOutputHelper output) : base(output)
    {
        _mainPage = new MainPageObject6(Context);
    }

    [Fact]
    [Trait("Category", "Counter")]
    [Trait("Priority", "P0")]
    public void Counter_InitialState_ShowsZero()
    {
        // Arrange & Act
        _mainPage.WaitLoaded(true);

        // Assert
        _mainPage.CounterLabel.AssertTextContains("0");
    }

    [Fact]
    [Trait("Category", "Counter")]
    [Trait("Priority", "P0")]
    public void Counter_ClickIncrement_IncreasesCount()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        var initialValue = _mainPage.GetCounterValue();

        // Act
        _mainPage.ClickIncrement();

        // Assert
        var newValue = _mainPage.GetCounterValue();
        Assert.Equal(initialValue + 1, newValue);
    }

    [Fact]
    [Trait("Category", "Counter")]
    [Trait("Priority", "P0")]
    public void Counter_ClickDecrement_DecreasesCount()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.ClickIncrement(); // Start at 1
        var initialValue = _mainPage.GetCounterValue();

        // Act
        _mainPage.ClickDecrement();

        // Assert
        var newValue = _mainPage.GetCounterValue();
        Assert.Equal(initialValue - 1, newValue);
    }

    [Fact]
    [Trait("Category", "Counter")]
    [Trait("Priority", "P0")]
    public void Counter_ClickReset_ResetsToZero()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.ClickIncrement();
        _mainPage.ClickIncrement();
        _mainPage.ClickIncrement();

        // Act
        _mainPage.ClickReset();

        // Assert
        Assert.Equal(0, _mainPage.GetCounterValue());
    }

    [Fact]
    [Trait("Category", "Counter")]
    [Trait("Priority", "P1")]
    public void Counter_MultipleIncrements_AccumulatesCorrectly()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.ClickReset(); // Ensure we start at 0

        // Act
        for (int i = 0; i < 5; i++)
        {
            _mainPage.ClickIncrement();
        }

        // Assert
        Assert.Equal(5, _mainPage.GetCounterValue());
    }

    [Fact]
    [Trait("Category", "Counter")]
    [Trait("Priority", "P1")]
    public void Counter_IncrementButton_IsVisibleAndEnabled()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Assert
        _mainPage.IncrementButton.AssertVisible(true);
        _mainPage.IncrementButton.AssertEnabled(true);
    }

    [Fact]
    [Trait("Category", "Counter")]
    [Trait("Priority", "P1")]
    public void Counter_DecrementButton_IsVisibleAndEnabled()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Assert
        _mainPage.DecrementButton.AssertVisible(true);
        _mainPage.DecrementButton.AssertEnabled(true);
    }

    [Fact]
    [Trait("Category", "Counter")]
    [Trait("Priority", "P1")]
    public void Counter_ResetButton_IsVisibleAndEnabled()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Assert
        _mainPage.ResetButton.AssertVisible(true);
        _mainPage.ResetButton.AssertEnabled(true);
    }
}
