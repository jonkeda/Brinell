using Brinell.Samples.Maui.UITests.Pages;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Maui.UITests.Tests;

/// <summary>
/// Tests for counter functionality on MainPage.
/// </summary>
public class CounterTests : MauiTestBase
{
    private readonly MainPageObject _mainPage;

    public CounterTests(ITestOutputHelper output) : base(output)
    {
        _mainPage = new MainPageObject(Context);
    }

    [Fact]
    public void Counter_InitialValue_IsZero()
    {
        // Arrange
        _mainPage.WaitForPageLoad();

        // Act & Assert
        _mainPage.CounterLabel.AssertTextContains("Counter: 0");
    }

    [Fact]
    public void Counter_Increment_IncreasesValue()
    {
        // Arrange
        _mainPage.WaitForPageLoad();

        // Act
        _mainPage.IncrementButton.Tap();

        // Assert
        _mainPage.CounterLabel.AssertTextContains("Counter: 1");
    }

    [Fact]
    public void Counter_Decrement_DecreasesValue()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        
        // First reset and increment to have a known state
        _mainPage.ResetButton.Tap();
        _mainPage.IncrementButton.Tap();
        _mainPage.CounterLabel.AssertTextContains("Counter: 1", "Counter should be 1 after increment");

        // Act
        _mainPage.DecrementButton.Tap();

        // Assert - counter should go from 1 to 0
        _mainPage.CounterLabel.AssertTextContains("Counter: 0");
    }

    [Fact]
    public void Counter_MultipleIncrements_ShowsCorrectValue()
    {
        // Arrange
        _mainPage.WaitForPageLoad();

        // Act
        _mainPage.IncrementButton.Tap();
        _mainPage.IncrementButton.Tap();
        _mainPage.IncrementButton.Tap();

        // Assert
        _mainPage.CounterLabel.AssertTextContains("Counter: 3");
    }

    [Fact]
    public void Counter_Reset_SetsToZero()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.IncrementButton.Tap();
        _mainPage.IncrementButton.Tap();

        // Act
        _mainPage.ResetButton.Tap();

        // Assert
        _mainPage.CounterLabel.AssertTextContains("Counter: 0");
    }
}
