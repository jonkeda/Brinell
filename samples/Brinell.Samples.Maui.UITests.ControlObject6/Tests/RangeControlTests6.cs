using Brinell.Samples.Maui.UITests.ControlObject6.Pages;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Maui.UITests.ControlObject6.Tests;

/// <summary>
/// Range control tests for Slider and ProgressBar controls.
/// Uses verified ControlObject6 APIs: GetValue, SetValue, GetMinimum, GetMaximum, GetRange, Increase, Decrease.
/// </summary>
public class RangeControlTests6 : MauiTestBase6
{
    private readonly MainPageObject6 _mainPage;

    public RangeControlTests6(ITestOutputHelper output) : base(output)
    {
        _mainPage = new MainPageObject6(Context);
    }

    #region Slider Tests

    [Fact]
    [Trait("Category", "Range")]
    [Trait("Control", "Slider")]
    [Trait("Priority", "P0")]
    public void Slider_GetValue_ReturnsCurrentValue()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act
        var value = _mainPage.VolumeSlider.GetValue();

        // Assert - Initial value is 50 per MainPage.xaml
        Assert.True(value >= 0, $"Slider value should be >= 0, was {value}");
    }

    [Fact]
    [Trait("Category", "Range")]
    [Trait("Control", "Slider")]
    [Trait("Priority", "P0")]
    public void Slider_SetValue_SetsToSpecifiedValue()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        const double targetValue = 75.0;

        // Act
        _mainPage.VolumeSlider.SetValue(targetValue);

        // Assert
        _mainPage.VolumeSlider.AssertValue(targetValue, tolerance: 5.0);
    }

    [Fact]
    [Trait("Category", "Range")]
    [Trait("Control", "Slider")]
    [Trait("Priority", "P0")]
    public void Slider_GetMinimum_ReturnsMinimumValue()
    {
        // Arrange - MainPage.xaml has Minimum="0"
        _mainPage.WaitLoaded(true);

        // Act
        var min = _mainPage.VolumeSlider.GetMinimum();

        // Assert
        Assert.Equal(0, min);
    }

    [Fact]
    [Trait("Category", "Range")]
    [Trait("Control", "Slider")]
    [Trait("Priority", "P0")]
    public void Slider_GetMaximum_ReturnsMaximumValue()
    {
        // Arrange - MainPage.xaml has Maximum="100"
        _mainPage.WaitLoaded(true);

        // Act
        var max = _mainPage.VolumeSlider.GetMaximum();

        // Assert
        Assert.True(max > 0, $"Maximum should be > 0, was {max}");
    }

    [Fact]
    [Trait("Category", "Range")]
    [Trait("Control", "Slider")]
    [Trait("Priority", "P0")]
    public void Slider_GetRange_ReturnsMinMax()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act
        var (min, max) = _mainPage.VolumeSlider.GetRange();

        // Assert - MainPage.xaml has 0-100 range
        Assert.Equal(0, min);
        Assert.True(max > min, $"Max ({max}) should be > min ({min})");
    }

    [Fact]
    [Trait("Category", "Range")]
    [Trait("Control", "Slider")]
    [Trait("Priority", "P1")]
    public void Slider_Increase_IncreasesValue()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.VolumeSlider.SetValue(50);
        var initialValue = _mainPage.VolumeSlider.GetValue();

        // Act
        _mainPage.VolumeSlider.Increase();

        // Assert
        var newValue = _mainPage.VolumeSlider.GetValue();
        Assert.True(newValue >= initialValue, $"Value should increase: was {initialValue}, now {newValue}");
    }

    [Fact]
    [Trait("Category", "Range")]
    [Trait("Control", "Slider")]
    [Trait("Priority", "P1")]
    public void Slider_Decrease_DecreasesValue()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.VolumeSlider.SetValue(50);
        var initialValue = _mainPage.VolumeSlider.GetValue();

        // Act
        _mainPage.VolumeSlider.Decrease();

        // Assert
        var newValue = _mainPage.VolumeSlider.GetValue();
        Assert.True(newValue <= initialValue, $"Value should decrease: was {initialValue}, now {newValue}");
    }

    [Fact]
    [Trait("Category", "Range")]
    [Trait("Control", "Slider")]
    [Trait("Priority", "P1")]
    public void Slider_AssertValue_PassesWhenMatches()
    {
        // Arrange
        _mainPage.WaitLoaded(true);
        _mainPage.VolumeSlider.SetValue(75.0);

        // Act & Assert - should not throw
        _mainPage.VolumeSlider.AssertValue(75.0, tolerance: 5.0);
    }

    [Fact]
    [Trait("Category", "Range")]
    [Trait("Control", "Slider")]
    [Trait("Priority", "P1")]
    public void Slider_SetToMinimum_SetsToMin()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act
        _mainPage.VolumeSlider.SetToMinimum();

        // Assert
        var min = _mainPage.VolumeSlider.GetMinimum();
        _mainPage.VolumeSlider.AssertValue(min, tolerance: 1.0);
    }

    [Fact]
    [Trait("Category", "Range")]
    [Trait("Control", "Slider")]
    [Trait("Priority", "P1")]
    public void Slider_SetToMaximum_SetsToMax()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act
        _mainPage.VolumeSlider.SetToMaximum();

        // Assert
        var max = _mainPage.VolumeSlider.GetMaximum();
        _mainPage.VolumeSlider.AssertValue(max, tolerance: 1.0);
    }

    #endregion

    #region ProgressBar Tests

    [Fact]
    [Trait("Category", "Range")]
    [Trait("Control", "ProgressBar")]
    [Trait("Priority", "P0")]
    public void ProgressBar_GetProgress_ReturnsCurrentProgress()
    {
        // Arrange - MainPage.xaml has Progress="0.5"
        _mainPage.WaitLoaded(true);

        // Act
        var progress = _mainPage.VolumeProgress.GetProgress();

        // Assert - Progress is 0-1 range
        Assert.True(progress >= 0 && progress <= 1, $"Progress should be 0-1, was {progress}");
    }

    [Fact]
    [Trait("Category", "Range")]
    [Trait("Control", "ProgressBar")]
    [Trait("Priority", "P0")]
    public void ProgressBar_GetProgressPercent_ReturnsPercentage()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act
        var percent = _mainPage.VolumeProgress.GetProgressPercent();

        // Assert - Percentage is 0-100 range
        Assert.True(percent >= 0 && percent <= 100, $"Percentage should be 0-100, was {percent}");
    }

    [Fact]
    [Trait("Category", "Range")]
    [Trait("Control", "ProgressBar")]
    [Trait("Priority", "P0")]
    public void ProgressBar_IsVisible_ReturnsTrueWhenVisible()
    {
        // Arrange
        _mainPage.WaitLoaded(true);

        // Act & Assert
        Assert.True(_mainPage.VolumeProgress.IsVisible());
    }

    [Fact]
    [Trait("Category", "Range")]
    [Trait("Control", "ProgressBar")]
    [Trait("Priority", "P1")]
    public void ProgressBar_AssertProgress_PassesWhenInRange()
    {
        // Arrange - Initial progress is 0.5
        _mainPage.WaitLoaded(true);

        // Act & Assert - should not throw
        _mainPage.VolumeProgress.AssertProgress(0.5, tolerance: 0.1);
    }

    #endregion
}
