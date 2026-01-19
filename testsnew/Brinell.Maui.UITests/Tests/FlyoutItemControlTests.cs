using Brinell.Maui.UITests.Pages;
using Xunit;

namespace Brinell.Maui.UITests.Tests;

/// <summary>
/// Tests for MauiFlyoutItemControl.
/// NOTE: These tests are skipped as the app has been converted from Flyout to TabBar navigation (SPEC-016).
/// Flyout navigation tests are now obsolete. See TabControlTests for tab navigation tests.
/// </summary>
[Collection("Appium")]
[Trait("Category", "UITest")]
[Trait("Control", "FlyoutItem")]
public class FlyoutItemControlTests
{
    private readonly AppiumFixture _fixture;

    public FlyoutItemControlTests(AppiumFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Flyout navigation replaced with TabBar (SPEC-016)")]
    [Trait("Method", "IsExists")]
    public void ContainerDemoFlyout_IsExists_ReturnsTrue()
    {
        // Test obsolete - app now uses TabBar instead of Flyout
        // See AppShellPage.ContainersTab for tab-based navigation
    }

    [Fact(Skip = "Flyout navigation replaced with TabBar (SPEC-016)")]
    [Trait("Method", "IsClickable")]
    public void ContainerDemoFlyout_IsClickable_ReturnsTrue()
    {
        // Test obsolete - app now uses TabBar instead of Flyout
        // See AppShellPage.ContainersTab for tab-based navigation
    }

    [Fact(Skip = "Flyout navigation replaced with TabBar (SPEC-016)")]
    [Trait("Method", "Click")]
    public void ContainerDemoFlyout_Click_NavigatesToContainerDemoPage()
    {
        // Test obsolete - app now uses TabBar instead of Flyout
        // See AppShellPage.ContainersTab for tab-based navigation
    }

    [Fact(Skip = "Flyout navigation replaced with TabBar (SPEC-016)")]
    [Trait("Method", "IsExists")]
    public void MainFlyout_IsExists_ReturnsTrue()
    {
        // Test obsolete - app now uses TabBar instead of Flyout
        // See AppShellPage.MainTab for tab-based navigation
    }
}
