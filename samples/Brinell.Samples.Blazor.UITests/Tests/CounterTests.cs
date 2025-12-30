using Brinell.Samples.Blazor.UITests.PageObjects;
using Brinell.Samples.Blazor.UITests.TestBase;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.UITests.Tests;

/// <summary>
/// Tests for the Counter page functionality.
/// </summary>
[Collection("BlazorUITests")]
public class CounterTests : BlazorSampleTestBase
{
    public CounterTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Counter_InitialLoad_ShowsZeroCount()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/counter");

        var counterPage = new CounterPage(Context!);
        counterPage.WaitForDisplayed();

        // Assert
        counterPage.AssertDisplayed("Counter page should be displayed");
        counterPage.GetCurrentCount().Should().Be(0, "Initial count should be zero");
    }

    [Fact]
    public void Counter_ClickIncrement_IncreasesCount()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/counter");

        var counterPage = new CounterPage(Context!);
        counterPage.WaitForDisplayed();

        // Act
        counterPage.ClickIncrement();

        // Assert - Wait for count to update (Blazor async rendering)
        counterPage.WaitForCount(1);
        counterPage.GetCurrentCount().Should().Be(1, "Count should be 1 after one click");
    }

    [Fact]
    public void Counter_MultipleIncrements_CountsCorrectly()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/counter");

        var counterPage = new CounterPage(Context!);
        counterPage.WaitForDisplayed();

        // Act
        counterPage.IncrementMultiple(5);

        // Assert - Wait for count to update (Blazor async rendering)
        counterPage.WaitForCount(5);
        counterPage.GetCurrentCount().Should().Be(5, "Count should be 5 after five clicks");
    }

    [Fact]
    public void Counter_Reset_SetsCountToZero()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/counter");

        var counterPage = new CounterPage(Context!);
        counterPage.WaitForDisplayed();

        // Increment first
        counterPage.IncrementMultiple(3);
        counterPage.WaitForCount(3);
        counterPage.GetCurrentCount().Should().Be(3, "Count should be 3 before reset");

        // Act
        counterPage.ClickReset();

        // Assert - Wait for count to update
        counterPage.WaitForCount(0);
        counterPage.GetCurrentCount().Should().Be(0, "Count should be 0 after reset");
    }

    [Fact]
    public void Counter_IncrementAfterReset_CountsFromZero()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/counter");

        var counterPage = new CounterPage(Context!);
        counterPage.WaitForDisplayed();

        // Increment, reset, then increment again
        counterPage.IncrementMultiple(5);
        counterPage.WaitForCount(5);
        counterPage.ClickReset();
        counterPage.WaitForCount(0);

        // Act
        counterPage.IncrementMultiple(2);

        // Assert - Wait for count to update
        counterPage.WaitForCount(2);
        counterPage.GetCurrentCount().Should().Be(2, "Count should be 2 after reset and two increments");
    }

    [Fact]
    public void Counter_ButtonsAreVisible_OnLoad()
    {
        // Arrange
        LaunchBrowser();
        NavigateToPage("/counter");

        var counterPage = new CounterPage(Context!);
        counterPage.WaitForDisplayed();

        // Assert
        counterPage.IncrementButton.AssertVisible("Increment button should be visible");
        counterPage.ResetButton.AssertVisible("Reset button should be visible");
        counterPage.CountDisplay.AssertVisible("Count display should be visible");
    }
}
