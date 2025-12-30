using Brinell.Samples.Blazor.UITests.PageObjects;
using Brinell.Samples.Blazor.UITests.TestBase;
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
        counterPage.AssertCount(0);
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
        counterPage.AssertCount(1);
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
        counterPage.AssertCount(5);
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
        counterPage.AssertCount(3);

        // Act
        counterPage.ClickReset();

        // Assert - Wait for count to update
        counterPage.WaitForCount(0);
        counterPage.AssertCount(0);
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
        counterPage.AssertCount(2);
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
