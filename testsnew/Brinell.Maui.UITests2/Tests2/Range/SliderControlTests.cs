using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Tests.Range;

/// <summary>
/// UI tests for Slider verifying slider value operations.
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "Slider")]
public class SliderControlTests
{
    private readonly MauiFixture _fixture;
    private UserFormPage Page => _fixture.UserFormPage;

    public SliderControlTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToUserForm();
    }

    #region State Tests

    /// <summary>
    /// Verifies that slider exists on the page.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task Slider_IsExists_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.FontSizeSlider.IsExists());
        Assert.True(Page.VolumeSlider.IsExists());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that slider is visible.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task Slider_IsVisible_ReturnsTrue()
    {
        // Assert
        Assert.True(Page.FontSizeSlider.IsVisible() == true);
        return Task.CompletedTask;
    }

    #endregion

    #region Value Tests

    /// <summary>
    /// Verifies GetValue() returns current slider value.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetValue")]
    public Task Slider_GetValue_ReturnsCurrentValue()
    {
        // Act
        var value = Page.VolumeSlider.GetValue();

        // Assert - value should be within range 0-100
        Assert.True(value.HasValue, "Slider value should not be null");
        Assert.True(value!.Value >= 0 && value.Value <= 100);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies SetValue() changes slider value.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "SetValue")]
    public Task Slider_SetValue_ChangesValue()
    {
        // Act
        Page.VolumeSlider.SetValue(50);

        // Assert - value should be close to 50
        var value = Page.VolumeSlider.GetValue();
        Assert.True(value.HasValue && value.Value >= 45 && value.Value <= 55);
        return Task.CompletedTask;
    }

    #endregion

    #region Percentage Tests

    /// <summary>
    /// Verifies GetPercentage() returns value as percentage.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetPercentage")]
    public Task Slider_GetPercentage_ReturnsPercentage()
    {
        // Act
        var percentage = Page.VolumeSlider.GetPercentage();

        // Assert - percentage should be 0-100
        Assert.True(percentage.HasValue && percentage.Value >= 0 && percentage.Value <= 100);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies SlideToPercentage() sets slider by percentage.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "SlideToPercentage")]
    public Task Slider_SlideToPercentage_SetsPercentage()
    {
        // Act
        Page.VolumeSlider.SlideToPercentage(75);

        // Assert - percentage should be close to 75
        var percentage = Page.VolumeSlider.GetPercentage();
        Assert.True(percentage.HasValue && percentage.Value >= 70 && percentage.Value <= 80);
        return Task.CompletedTask;
    }

    #endregion

    #region Boundary Tests

    /// <summary>
    /// Verifies SlideToMinimum() sets slider to minimum value.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "SlideToMinimum")]
    public Task Slider_SlideToMinimum_SetsToMin()
    {
        // Act
        Page.VolumeSlider.SlideToMinimum();

        // Assert - value should be at or near minimum (0)
        var value = Page.VolumeSlider.GetValue();
        Assert.True(value.HasValue && value.Value >= 0 && value.Value <= 5);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Verifies SlideToMaximum() sets slider to maximum value.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "SlideToMaximum")]
    public Task Slider_SlideToMaximum_SetsToMax()
    {
        // Act
        Page.VolumeSlider.SlideToMaximum();

        // Assert - value should be at or near maximum (100)
        var value = Page.VolumeSlider.GetValue();
        Assert.True(value.HasValue && value.Value >= 95 && value.Value <= 100);
        return Task.CompletedTask;
    }

    #endregion

    #region Multiple Slider Tests

    /// <summary>
    /// Verifies multiple sliders operate independently.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Pattern", "MultipleControls")]
    public Task Slider_MultipleControls_OperateIndependently()
    {
        // Act
        Page.VolumeSlider.SetValue(25);
        Page.FontSizeSlider.SetValue(36); // FontSize: 8-72, so 36 is middle

        // Assert - each slider has its own value
        var volumeValue = Page.VolumeSlider.GetValue();
        var fontSizeValue = Page.FontSizeSlider.GetValue();
        
        Assert.True(volumeValue.HasValue && volumeValue.Value >= 20 && volumeValue.Value <= 30);
        Assert.True(fontSizeValue.HasValue && fontSizeValue.Value >= 32 && fontSizeValue.Value <= 40);
        return Task.CompletedTask;
    }

    #endregion
}
