using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Samples.Blazor.UITests.ControlObject6.PageObjects;
using Brinell.Samples.Blazor.UITests.ControlObject6.TestBase;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.UITests.ControlObject6.Tests;

/// <summary>
/// Click interaction tests using ControlObject6 async API.
/// </summary>
public class ClickTests6 : BlazorTestBase6
{
    public ClickTests6(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    [Trait("Category", "Click")]
    [Trait("Priority", "P0")]
    public async Task Button_ClickAsync_TriggersAction()
    {
        // Arrange
        await NavigateToAsync("counter");
        var counterPage = new CounterPage6(Context);
        await counterPage.WaitLoadedAsync(true);
        var initialCount = await counterPage.GetCurrentCountAsync();

        // Act
        await counterPage.IncrementButton.ClickAsync();

        // Assert
        var newCount = await counterPage.GetCurrentCountAsync();
        newCount.Should().Be(initialCount + 1);
    }

    [Fact]
    [Trait("Category", "Click")]
    [Trait("Priority", "P0")]
    public async Task Button_ClickAsync_WorksOnVisibleButton()
    {
        // Arrange
        await NavigateToAsync("counter");
        var resetButton = new ButtonControl(Context, "reset-btn", null);
        await resetButton.AssertVisibleAsync(true);

        // Act & Assert - should not throw
        await resetButton.ClickAsync();
    }

    [Fact]
    [Trait("Category", "Click")]
    [Trait("Priority", "P0")]
    public async Task Button_ClickAsync_WorksOnEnabledButton()
    {
        // Arrange
        await NavigateToAsync("counter");
        var incrementButton = new ButtonControl(Context, "increment-btn", null);
        await incrementButton.AssertEnabledAsync(true);

        // Act & Assert - should not throw
        await incrementButton.ClickAsync();
    }

    [Fact]
    [Trait("Category", "Click")]
    [Trait("Priority", "P1")]
    public async Task Button_MultipleClicksAsync_AllRegister()
    {
        // Arrange
        await NavigateToAsync("counter");
        var counterPage = new CounterPage6(Context);
        await counterPage.WaitLoadedAsync(true);
        await counterPage.ClickResetAsync();
        const int clickCount = 3;

        // Act
        for (int i = 0; i < clickCount; i++)
        {
            await counterPage.IncrementButton.ClickAsync();
        }

        // Assert
        var count = await counterPage.GetCurrentCountAsync();
        count.Should().Be(clickCount);
    }

    [Fact]
    [Trait("Category", "Click")]
    [Trait("Priority", "P1")]
    public async Task Button_ClickAsync_WaitsForVisibility()
    {
        // Arrange
        await NavigateToAsync("counter");
        var incrementButton = new ButtonControl(Context, "increment-btn", null);

        // Act - Click should wait for button to be visible
        await incrementButton.ClickAsync(5000);

        // Assert - if we get here without exception, click worked
        var isVisible = await incrementButton.IsVisibleAsync();
        isVisible.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Click")]
    [Trait("Priority", "P2")]
    public async Task Button_ClickAsync_WithTimeout_UsesSpecifiedTimeout()
    {
        // Arrange
        await NavigateToAsync("counter");
        var resetButton = new ButtonControl(Context, "reset-btn", null);

        // Act & Assert - should work with custom timeout
        await resetButton.ClickAsync(10000);
    }
}
