using Brinell.Samples.Maui.UITests.Pages;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Maui.UITests.Tests;

/// <summary>
/// Tests for slider and progress controls on MainPage.
/// </summary>
public class SliderTests : MauiTestBase
{
    private readonly MainPageObject _mainPage;

    public SliderTests(ITestOutputHelper output) : base(output)
    {
        _mainPage = new MainPageObject(Context);
    }

    [Fact]
    public void VolumeSlider_InitialValue_Is50()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToElement("VolumeSlider");

        // Assert
        _mainPage.VolumeLabel.AssertTextContains("Volume: 50%");
    }

    [Fact]
    public void VolumeSlider_SetValue_UpdatesLabel()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToElement("VolumeSlider");

        // Act
        _mainPage.VolumeSlider.SetValue(75);

        // Assert
        _mainPage.VolumeLabel.AssertTextContains("75");
    }

    [Fact]
    public void VolumeProgress_InitialValue_IsHalf()
    {
        // Arrange
        _mainPage.WaitForPageLoad();
        _mainPage.MainScrollView.ScrollToElement("VolumeProgress");

        // Assert - Progress should be 50%
        _mainPage.VolumeProgress.AssertPercentage(50, tolerance: 5);
    }
}
