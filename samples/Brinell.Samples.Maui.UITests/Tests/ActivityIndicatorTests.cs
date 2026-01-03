using Brinell.Samples.Maui.UITests.Pages;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Maui.UITests.Tests;

/// <summary>
/// Tests for activity indicator and page layout.
/// </summary>
public class ActivityIndicatorTests : MauiTestBase
{
    private readonly MainPageObject _mainPage;

    public ActivityIndicatorTests(ITestOutputHelper output) : base(output)
    {
        _mainPage = new MainPageObject(Context);
    }

    [Fact]
    public void LoadingIndicator_Initially_NotRunning()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToElement("ToggleLoadingButton");

        // Assert
        _mainPage.LoadingIndicator.AssertNotRunning();
    }

    [Fact]
    public void LoadingIndicator_ToggleButton_StartsIndicator()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToElement("ToggleLoadingButton");

        // Act
        _mainPage.ToggleLoadingButton.Tap();
        
        // Wait for indicator to start (give UI time to update)
        _mainPage.LoadingIndicator.WaitForStart(timeoutMs: 2000);

        // Assert
        _mainPage.LoadingIndicator.AssertRunning();
    }

    [Fact]
    public void LoadingIndicator_ToggleTwice_StopsIndicator()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToElement("ToggleLoadingButton");

        // Act
        _mainPage.ToggleLoadingButton.Tap();
        _mainPage.ToggleLoadingButton.Tap();

        // Assert
        _mainPage.LoadingIndicator.AssertNotRunning();
    }
}
