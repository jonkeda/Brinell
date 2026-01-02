using Brinell.Samples.Stride.UITests.PageObjects;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Stride.UITests;

/// <summary>
/// Tests for the counter functionality.
/// </summary>
public class CounterTests : StrideUITestBase
{
    public CounterTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void Counter_InitialValue_IsZero()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();

        // Act
        var value = mainPage.GetCounterValue();

        // Assert
        value.Should().Be(0);
        mainPage.CounterDisplay.AssertTextEquals("Count: 0");
    }

    [Fact]
    public void IncrementButton_Click_IncreasesCounter()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();

        // Act
        mainPage.IncrementCounter();

        // Assert
        mainPage.GetCounterValue().Should().Be(1);
    }

    [Fact]
    public void DecrementButton_Click_DecreasesCounter()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();
        mainPage.ResetCounter(); // Ensure we start at 0
        mainPage.IncrementCounter(); // Start at 1
        mainPage.IncrementCounter(); // Increment again to 2 for stability

        // Act
        mainPage.DecrementCounter();

        // Assert
        mainPage.GetCounterValue().Should().Be(1);
    }

    [Fact]
    public void ResetButton_Click_ResetsToZero()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();
        mainPage.IncrementCounter();
        mainPage.IncrementCounter();
        mainPage.IncrementCounter();

        // Act
        mainPage.ResetCounter();

        // Assert
        mainPage.GetCounterValue().Should().Be(0);
    }

    [Fact]
    public void Counter_MultipleIncrements_Accumulates()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();

        // Act
        for (int i = 0; i < 5; i++)
        {
            mainPage.IncrementCounter();
        }

        // Assert
        mainPage.GetCounterValue().Should().Be(5);
    }
}
