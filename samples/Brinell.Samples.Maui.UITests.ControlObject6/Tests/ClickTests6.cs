using Brinell.Samples.Maui.UITests.ControlObject6.Pages;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Maui.UITests.ControlObject6.Tests;

/// <summary>
/// Click interaction tests using ControlObject6 API.
/// Tests Click, DoubleClick and other click-related functionality.
/// </summary>
public class ClickTests6 : MauiTestBase6
{
    private readonly MainPageObject6 _mainPage;

    public ClickTests6(ITestOutputHelper output) : base(output)
    {
        _mainPage = new MainPageObject6(Context);
    }

    [Fact]
    [Trait("Category", "Click")]
    [Trait("Priority", "P0")]
    public void Button_Click_TriggersAction()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        var initialCount = _mainPage.GetCounterValue();

        // Act
        _mainPage.IncrementButton.Click();

        // Assert
        Assert.Equal(initialCount + 1, _mainPage.GetCounterValue());
    }

    [Fact]
    [Trait("Category", "Click")]
    [Trait("Priority", "P0")]
    public void Button_Click_WorksOnVisibleButton()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.ResetButton.AssertVisible(true);

        // Act & Assert - should not throw
        _mainPage.ResetButton.Click();
    }

    [Fact]
    [Trait("Category", "Click")]
    [Trait("Priority", "P0")]
    public void Button_Click_WorksOnEnabledButton()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.GreetButton.AssertEnabled(true);

        // Act & Assert - should not throw
        _mainPage.GreetButton.Click();
    }

    [Fact]
    [Trait("Category", "Click")]
    [Trait("Priority", "P1")]
    public void Button_MultipleClicks_AllRegister()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.ClickReset();
        const int clickCount = 3;

        // Act
        for (int i = 0; i < clickCount; i++)
        {
            _mainPage.IncrementButton.Click();
        }

        // Assert
        Assert.Equal(clickCount, _mainPage.GetCounterValue());
    }

    [Fact]
    [Trait("Category", "Click")]
    [Trait("Priority", "P1")]
    public void Button_Click_WaitsForVisibility()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act - Click should wait for button to be visible
        _mainPage.IncrementButton.Click(5000);

        // Assert - if we get here without exception, click worked
        Assert.True(_mainPage.IncrementButton.IsVisible());
    }

    [Fact]
    [Trait("Category", "Click")]
    [Trait("Priority", "P2")]
    public void Button_Click_WithTimeout_UsesSpecifiedTimeout()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act & Assert - should work with custom timeout
        _mainPage.DecrementButton.Click(10000);
    }

    [Fact]
    [Trait("Category", "Click")]
    [Trait("Priority", "P2")]
    public void Entry_Click_FocusesControl()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act - Clicking entry should focus it
        _mainPage.NameEntry.Click();

        // Assert - After click, we should be able to type
        _mainPage.NameEntry.Enter("Focused!");
        _mainPage.NameEntry.AssertText("Focused!");
    }
}
