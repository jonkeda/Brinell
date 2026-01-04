using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Samples.Blazor.UITests.ControlObject6.TestBase;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Blazor.UITests.ControlObject6.Tests;

/// <summary>
/// Control state tests using ControlObject6 async API.
/// Tests IsExistsAsync, IsVisibleAsync, IsEnabledAsync, Wait, and Assert methods.
/// </summary>
public class ControlStateTests6 : BlazorTestBase6
{
    public ControlStateTests6(ITestOutputHelper output) : base(output)
    {
    }

    #region Existence Tests

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public async Task Control_IsExistsAsync_ReturnsTrueForExistingControl()
    {
        // Arrange
        await NavigateToAsync("counter");
        var incrementButton = new ButtonControl(Context, "increment-btn", null);

        // Act
        var exists = await incrementButton.IsExistsAsync();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public async Task Control_WaitExistsAsync_WaitsForControlToExist()
    {
        // Arrange
        await NavigateToAsync("counter");
        var incrementButton = new ButtonControl(Context, "increment-btn", null);

        // Act
        var result = await incrementButton.WaitExistsAsync(true, 5000);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public async Task Control_AssertExistsAsync_PassesForExistingControl()
    {
        // Arrange
        await NavigateToAsync("counter");
        var incrementButton = new ButtonControl(Context, "increment-btn", null);

        // Act & Assert - should not throw
        await incrementButton.AssertExistsAsync(true);
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public async Task Control_CheckExistsAsync_PassesForExistingControl()
    {
        // Arrange
        await NavigateToAsync("counter");
        var incrementButton = new ButtonControl(Context, "increment-btn", null);

        // Act & Assert - should not throw
        await incrementButton.CheckExistsAsync(true);
    }

    #endregion

    #region Visibility Tests

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public async Task Control_IsVisibleAsync_ReturnsTrueForVisibleControl()
    {
        // Arrange
        await NavigateToAsync("counter");
        var incrementButton = new ButtonControl(Context, "increment-btn", null);

        // Act
        var isVisible = await incrementButton.IsVisibleAsync();

        // Assert
        isVisible.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public async Task Control_WaitVisibleAsync_WaitsForControlToBeVisible()
    {
        // Arrange
        await NavigateToAsync("counter");
        var counterTitle = new ButtonControl(Context, "counter-title", null);

        // Act
        var result = await counterTitle.WaitVisibleAsync(true, 5000);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public async Task Control_AssertVisibleAsync_PassesForVisibleControl()
    {
        // Arrange
        await NavigateToAsync("counter");
        var counterTitle = new ButtonControl(Context, "counter-title", null);

        // Act & Assert - should not throw
        await counterTitle.AssertVisibleAsync(true);
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public async Task Control_CheckVisibleAsync_PassesForVisibleControl()
    {
        // Arrange
        await NavigateToAsync("counter");
        var countDisplay = new ButtonControl(Context, "count-display", null);

        // Act & Assert - should not throw
        await countDisplay.CheckVisibleAsync(true);
    }

    #endregion

    #region Enabled Tests

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public async Task Control_IsEnabledAsync_ReturnsTrueForEnabledControl()
    {
        // Arrange
        await NavigateToAsync("counter");
        var incrementButton = new ButtonControl(Context, "increment-btn", null);

        // Act
        var isEnabled = await incrementButton.IsEnabledAsync();

        // Assert
        isEnabled.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public async Task Control_WaitEnabledAsync_WaitsForControlToBeEnabled()
    {
        // Arrange
        await NavigateToAsync("counter");
        var resetButton = new ButtonControl(Context, "reset-btn", null);

        // Act
        var result = await resetButton.WaitEnabledAsync(true, 5000);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public async Task Control_AssertEnabledAsync_PassesForEnabledControl()
    {
        // Arrange
        await NavigateToAsync("counter");
        var resetButton = new ButtonControl(Context, "reset-btn", null);

        // Act & Assert - should not throw
        await resetButton.AssertEnabledAsync(true);
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P0")]
    public async Task Control_CheckEnabledAsync_PassesForEnabledControl()
    {
        // Arrange
        await NavigateToAsync("counter");
        var incrementButton = new ButtonControl(Context, "increment-btn", null);

        // Act & Assert - should not throw
        await incrementButton.CheckEnabledAsync(true);
    }

    #endregion

    #region Nullable Expected Tests

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P1")]
    public async Task WaitExistsAsync_NullExpected_ReturnsImmediately()
    {
        // Arrange
        await NavigateToAsync("counter");
        var incrementButton = new ButtonControl(Context, "increment-btn", null);

        // Act - null expected should return immediately (true)
        var result = await incrementButton.WaitExistsAsync(null, 100);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P1")]
    public async Task WaitVisibleAsync_NullExpected_ReturnsImmediately()
    {
        // Arrange
        await NavigateToAsync("counter");
        var incrementButton = new ButtonControl(Context, "increment-btn", null);

        // Act - null expected should return immediately (true)
        var result = await incrementButton.WaitVisibleAsync(null, 100);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "ControlState")]
    [Trait("Priority", "P1")]
    public async Task WaitEnabledAsync_NullExpected_ReturnsImmediately()
    {
        // Arrange
        await NavigateToAsync("counter");
        var incrementButton = new ButtonControl(Context, "increment-btn", null);

        // Act - null expected should return immediately (true)
        var result = await incrementButton.WaitEnabledAsync(null, 100);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
}
