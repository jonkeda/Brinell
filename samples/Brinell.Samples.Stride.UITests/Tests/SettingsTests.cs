using Brinell.Samples.Stride.UITests.PageObjects;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Stride.UITests;

/// <summary>
/// Tests for the settings functionality.
/// </summary>
public class SettingsTests : StrideUITestBase
{
    public SettingsTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void DarkModeToggle_InitialState_IsOff()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();

        // Assert
        mainPage.DarkModeToggle.AssertUnchecked();
    }

    [Fact]
    public void DarkModeToggle_Toggle_ChangesState()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();

        // Act
        mainPage.ToggleDarkMode();

        // Assert
        mainPage.DarkModeToggle.AssertChecked();
    }

    [Fact]
    public void DarkModeToggle_ToggleTwice_ReturnsToInitial()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();

        // Act
        mainPage.ToggleDarkMode();
        mainPage.ToggleDarkMode();

        // Assert
        mainPage.DarkModeToggle.AssertUnchecked();
    }

    [Fact]
    public void VolumeSlider_InitialValue_Is50()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();

        // Assert
        mainPage.VolumeSlider.AssertValue(50, tolerance: 1);
        mainPage.VolumeDisplay.AssertTextEquals("50%");
    }

    [Fact]
    public void VolumeSlider_SetValue_UpdatesDisplay()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();

        // Act
        mainPage.SetVolume(75);

        // Assert
        mainPage.VolumeSlider.WaitValue(75, tolerance: 5);
        mainPage.VolumeDisplay.WaitTextContains("75");
    }

    [Fact]
    public void VolumeSlider_Increment_IncreasesValue()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();
        var initialValue = mainPage.VolumeSlider.GetValue();

        // Act
        mainPage.VolumeSlider.Increment();

        // Assert
        mainPage.VolumeSlider.GetValue().Should().BeGreaterThan(initialValue);
    }

    [Fact]
    public void VolumeSlider_Decrement_DecreasesValue()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();
        var initialValue = mainPage.VolumeSlider.GetValue();

        // Act
        mainPage.VolumeSlider.Decrement();

        // Assert
        mainPage.VolumeSlider.GetValue().Should().BeLessThan(initialValue);
    }
}
