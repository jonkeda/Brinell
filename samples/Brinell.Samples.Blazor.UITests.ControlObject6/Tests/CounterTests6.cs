using Brinell.Samples.Blazor.UITests.ControlObject6.PageObjects;
using Brinell.Samples.Blazor.UITests.ControlObject6.TestBase;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.UITests.ControlObject6.Tests;

/// <summary>
/// Counter functionality tests using ControlObject6 async API.
/// </summary>
public class CounterTests6 : BlazorTestBase6
{
    public CounterTests6(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    [Trait("Category", "Counter")]
    [Trait("Priority", "P0")]
    public async Task Counter_NavigateToPage_ShowsCounterTitle()
    {
        // Arrange
        await NavigateToAsync("counter");
        var counterPage = new CounterPage6(Context);

        // Act
        await counterPage.WaitLoadedAsync(true);

        // Assert
        await counterPage.CounterTitle.AssertVisibleAsync(true);
    }

    [Fact]
    [Trait("Category", "Counter")]
    [Trait("Priority", "P0")]
    public async Task Counter_InitialState_ShowsZero()
    {
        // Arrange
        await NavigateToAsync("counter");
        var counterPage = new CounterPage6(Context);
        await counterPage.WaitLoadedAsync(true);

        // Act
        var count = await counterPage.GetCurrentCountAsync();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Counter")]
    [Trait("Priority", "P0")]
    public async Task Counter_ClickIncrement_IncreasesCount()
    {
        // Arrange
        await NavigateToAsync("counter");
        var counterPage = new CounterPage6(Context);
        await counterPage.WaitLoadedAsync(true);
        var initialCount = await counterPage.GetCurrentCountAsync();

        // Act
        await counterPage.ClickIncrementAsync();

        // Assert
        var newCount = await counterPage.GetCurrentCountAsync();
        newCount.Should().Be(initialCount + 1);
    }

    [Fact]
    [Trait("Category", "Counter")]
    [Trait("Priority", "P0")]
    public async Task Counter_ClickReset_ResetsToZero()
    {
        // Arrange
        await NavigateToAsync("counter");
        var counterPage = new CounterPage6(Context);
        await counterPage.WaitLoadedAsync(true);
        await counterPage.IncrementMultipleAsync(5);

        // Act
        await counterPage.ClickResetAsync();

        // Assert
        var count = await counterPage.GetCurrentCountAsync();
        count.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Counter")]
    [Trait("Priority", "P1")]
    public async Task Counter_MultipleIncrements_AccumulatesCorrectly()
    {
        // Arrange
        await NavigateToAsync("counter");
        var counterPage = new CounterPage6(Context);
        await counterPage.WaitLoadedAsync(true);
        await counterPage.ClickResetAsync();

        // Act
        await counterPage.IncrementMultipleAsync(5);

        // Assert
        var count = await counterPage.GetCurrentCountAsync();
        count.Should().Be(5);
    }

    [Fact]
    [Trait("Category", "Counter")]
    [Trait("Priority", "P1")]
    public async Task Counter_IncrementButton_IsVisibleAndEnabled()
    {
        // Arrange
        await NavigateToAsync("counter");
        var counterPage = new CounterPage6(Context);
        await counterPage.WaitLoadedAsync(true);

        // Assert
        await counterPage.IncrementButton.AssertVisibleAsync(true);
        await counterPage.IncrementButton.AssertEnabledAsync(true);
    }

    [Fact]
    [Trait("Category", "Counter")]
    [Trait("Priority", "P1")]
    public async Task Counter_ResetButton_IsVisibleAndEnabled()
    {
        // Arrange
        await NavigateToAsync("counter");
        var counterPage = new CounterPage6(Context);
        await counterPage.WaitLoadedAsync(true);

        // Assert
        await counterPage.ResetButton.AssertVisibleAsync(true);
        await counterPage.ResetButton.AssertEnabledAsync(true);
    }

    [Fact]
    [Trait("Category", "Counter")]
    [Trait("Priority", "P1")]
    public async Task Counter_CountDisplay_UpdatesAfterClick()
    {
        // Arrange
        await NavigateToAsync("counter");
        var counterPage = new CounterPage6(Context);
        await counterPage.WaitLoadedAsync(true);
        await counterPage.ClickResetAsync();

        // Act
        await counterPage.ClickIncrementAsync();

        // Assert
        await counterPage.CountDisplay.AssertTextContainsAsync("1");
    }
}
