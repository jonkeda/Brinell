using Brinell.Samples.Blazor.PlaywrightTests.PageObjects;
using Brinell.Samples.Blazor.PlaywrightTests.TestBase;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.PlaywrightTests.Tests;

/// <summary>
/// Tests for the Blazor Counter page using Playwright.
/// </summary>
public class CounterTests : BlazorPlaywrightTestBase
{
    public CounterTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Counter_InitialState_DisplaysZero()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/counter");
        await WaitForBlazorReadyAsync();

        // Act
        var counterPage = new CounterPage(Context);

        // Assert
        await counterPage.AssertDisplayedAsync("Counter page should be displayed.");
        var count = await counterPage.GetCurrentCountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Counter_ClickIncrement_IncrementsCount()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/counter");
        await WaitForBlazorReadyAsync();
        var counterPage = new CounterPage(Context);
        await counterPage.WaitForDisplayedAsync();

        // Act
        await counterPage.ClickIncrementAsync();

        // Assert
        var count = await counterPage.GetCurrentCountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Counter_ClickIncrementMultipleTimes_AccumulatesCount()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/counter");
        await WaitForBlazorReadyAsync();
        var counterPage = new CounterPage(Context);
        await counterPage.WaitForDisplayedAsync();

        // Act
        await counterPage.IncrementMultipleAsync(5);

        // Assert
        var count = await counterPage.GetCurrentCountAsync();
        Assert.Equal(5, count);
    }

    [Fact]
    public async Task Counter_ClickReset_ResetsToZero()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/counter");
        await WaitForBlazorReadyAsync();
        var counterPage = new CounterPage(Context);
        await counterPage.WaitForDisplayedAsync();
        await counterPage.IncrementMultipleAsync(3);

        // Act
        await counterPage.ClickResetAsync();

        // Assert
        var count = await counterPage.GetCurrentCountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Counter_WaitForCount_WaitsUntilExpectedValue()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/counter");
        await WaitForBlazorReadyAsync();
        var counterPage = new CounterPage(Context);
        await counterPage.WaitForDisplayedAsync();

        // Act
        await counterPage.ClickIncrementAsync();
        await counterPage.ClickIncrementAsync();
        await counterPage.ClickIncrementAsync();

        // Assert
        var result = await counterPage.WaitForCountAsync(3);
        Assert.True(result, "Counter should reach expected value.");
    }

    [Fact]
    public async Task Counter_IncrementButtonIsVisible_ReturnsTrue()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/counter");
        await WaitForBlazorReadyAsync();
        var counterPage = new CounterPage(Context);
        await counterPage.WaitForDisplayedAsync();

        // Act
        var isVisible = await counterPage.IncrementButton.IsVisibleAsync();

        // Assert
        Assert.True(isVisible, "Increment button should be visible.");
    }

    [Fact]
    public async Task Counter_IncrementButtonIsEnabled_ReturnsTrue()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/counter");
        await WaitForBlazorReadyAsync();
        var counterPage = new CounterPage(Context);
        await counterPage.WaitForDisplayedAsync();

        // Act
        var isEnabled = await counterPage.IncrementButton.IsEnabledAsync();

        // Assert
        Assert.True(isEnabled, "Increment button should be enabled.");
    }

    [Fact]
    public async Task Counter_CountDisplayHasCorrectFormat()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/counter");
        await WaitForBlazorReadyAsync();
        var counterPage = new CounterPage(Context);
        await counterPage.WaitForDisplayedAsync();

        // Act
        var text = await counterPage.CountDisplay.GetTextAsync();

        // Assert
        Assert.StartsWith("Current count:", text);
    }

    [Fact]
    public async Task Counter_TitleDisplaysCorrectly()
    {
        // Arrange
        await LaunchBrowserAsync();
        await NavigateToPageAsync("/counter");
        await WaitForBlazorReadyAsync();
        var counterPage = new CounterPage(Context);
        await counterPage.WaitForDisplayedAsync();

        // Act
        var title = await counterPage.CounterTitle.GetTextAsync();

        // Assert
        Assert.Equal("Counter", title);
    }
}
