using Brinell.Samples.Maui.UITests.Pages;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Maui.UITests.Tests;

/// <summary>
/// Tests for Shell navigation in the MAUI sample app.
/// Uses the flyout menu to navigate between pages.
/// </summary>
public class NavigationTests : MauiTestBase
{
    private readonly MainPageObject _mainPage;

    public NavigationTests(ITestOutputHelper output) : base(output)
    {
        _mainPage = new MainPageObject(Context);
    }

    // ═══════════════════════════════════════════════════════════════
    // APP STARTUP TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Navigation_AppLaunch_ShowsMainPage()
    {
        // Arrange & Act
        _mainPage.WaitForPageLoad();

        // Assert
        _mainPage.TitleLabel.AssertExists("Main page should be displayed on launch");
        _mainPage.TitleLabel.AssertTextContains("Brinell MAUI Sample");
    }

    [Fact]
    public void Navigation_MainPage_HasAllSections()
    {
        // Arrange
        _mainPage.WaitForPageLoad();

        // Assert - verify all major sections exist
        _mainPage.CounterLabel.AssertExists("Counter section should exist");
        _mainPage.NameEntry.AssertExists("Text input section should exist");
        _mainPage.NotificationSwitch.AssertExists("Toggle section should exist");
        _mainPage.VolumeSlider.AssertExists("Slider section should exist");
    }

    // ═══════════════════════════════════════════════════════════════
    // FLYOUT NAVIGATION TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Navigation_FlyoutHeader_IsVisible()
    {
        // Arrange
        _mainPage.WaitForPageLoad();

        // Assert - flyout should be visible (FlyoutBehavior="Locked")
        // Check flyout title is accessible
        Assert.True(Context.ElementIsVisible("FlyoutTitle"), 
            "Flyout header should be visible when FlyoutBehavior is Locked");
    }

    [Fact]
    public void Navigation_FlyoutItems_AreAccessible()
    {
        // Arrange
        _mainPage.WaitForPageLoad();

        // Assert - verify flyout items exist
        // On Windows MAUI, FlyoutItem's Title property becomes the Name attribute
        // The AutomationId may not propagate to the rendered control
        // Try both AutomationId and Name-based lookup for compatibility
        var mainExists = Context.ElementExists("FlyoutMain") 
                        || Context.ElementExistsByName("Main");
        var dashboardExists = Context.ElementExists("FlyoutDashboard") 
                             || Context.ElementExistsByName("Dashboard");
        
        Assert.True(mainExists, 
            "Main flyout item should exist (by AutomationId or Name)");
        Assert.True(dashboardExists, 
            "Dashboard flyout item should exist (by AutomationId or Name)");
    }

    // ═══════════════════════════════════════════════════════════════
    // SCROLL NAVIGATION TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void Navigation_ScrollToBottom_ShowsActivitySection()
    {
        // Arrange
        _mainPage.WaitForPageLoad();

        // Act
        _mainPage.MainScrollView.ScrollToBottom();

        // Assert - use WaitExists for more reliable detection after scroll
        // Also test ToggleLoadingButton as a fallback since ActivityIndicator
        // with IsRunning=false may not be visible in automation tree
        var indicatorVisible = _mainPage.LoadingIndicator.WaitExists(true, 3000);
        var buttonVisible = _mainPage.ToggleLoadingButton.WaitExists(true, 3000);
        
        Assert.True(indicatorVisible || buttonVisible, 
            "Activity section (indicator or toggle button) should be visible after scrolling");
    }

    [Fact]
    public void Navigation_ScrollToTop_ShowsTitle()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToBottom();

        // Act
        _mainPage.MainScrollView.ScrollToTop();

        // Assert
        _mainPage.TitleLabel.AssertVisible("Title should be visible after scrolling to top");
    }
}
