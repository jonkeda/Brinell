using Brinell.Maui.UITests.Pages;
using Xunit;

namespace Brinell.Maui.UITests.Tests;

/// <summary>
/// Tests for MauiFlyoutItemControl.
/// Verifies flyout navigation using XPath @Name strategy.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "FlyoutItem")]
public class FlyoutItemControlTests
{
    private readonly AppiumFixture _fixture;
    private AppShellPage Shell => _fixture.AppShell;

    public FlyoutItemControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Method", "IsExists")]
    public void ContainerDemoFlyout_IsExists_ReturnsTrue()
    {
        // Arrange - scroll to make item visible
        Shell.ScrollFlyoutToBottom();
        
        // Act & Assert
        Assert.True(Shell.ContainerDemoFlyout.IsExists(), "Container Demo flyout item should exist");
    }

    [Fact]
    [Trait("Method", "IsClickable")]
    public void ContainerDemoFlyout_IsClickable_ReturnsTrue()
    {
        // Arrange
        Shell.ScrollFlyoutToBottom();
        
        // Act & Assert
        var result = Shell.ContainerDemoFlyout.IsClickable();
        Assert.True(result == true, "Container Demo flyout item should be clickable");
    }

    [Fact]
    [Trait("Method", "Click")]
    public void ContainerDemoFlyout_Click_NavigatesToContainerDemoPage()
    {
        // Arrange - scroll to find the Container Demo flyout item
        Shell.ScrollFlyoutToBottom();
        
        // Act - click the flyout item
        Shell.ContainerDemoFlyout.Click();
        
        // Assert - use ContainerDemoPage and wait for it to be loaded
        Assert.True(_fixture.ContainerDemoPage.WaitReady(5000), "Should navigate to ContainerDemoPage");
    }

    [Fact]
    [Trait("Method", "IsExists")]
    public void MainFlyout_IsExists_ReturnsTrue()
    {
        // Act & Assert - Main is at top, no scroll needed
        Assert.True(Shell.MainFlyout.IsExists(), "Main flyout item should exist");
    }
}
