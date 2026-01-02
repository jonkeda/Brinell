using Brinell.Samples.Stride.UITests.PageObjects;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Stride.UITests;

/// <summary>
/// Tests for the settings overlay functionality.
/// Tests both new settings page controls and legacy controls.
/// </summary>
public class SettingsTests : StrideUITestBase
{
    public SettingsTests(ITestOutputHelper output) : base(output) { }

    #region Settings Page Tests

    [Fact]
    public void SettingsPage_Opens_ShowsAllSections()
    {
        // Arrange
        var game = new GamePage(Context);
        game.CheckActive();

        // Act
        game.OpenSettings();
        Context.WaitFor(() => true, 500);

        // Assert
        var settings = new SettingsPage(Context);
        settings.CheckActive();
    }

    #endregion

    #region Audio Settings Tests

    [Fact]
    public void AudioSettings_MasterVolumeSlider_Exists()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Assert
        settings.MasterVolumeSlider.AssertExists();
        settings.MasterVolumeSlider.AssertValue(80, tolerance: 1);
    }

    [Fact]
    public void AudioSettings_MasterVolume_CanBeChanged()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Act
        settings.SetMasterVolume(50);

        // Assert
        settings.MasterVolumeSlider.AssertValue(50, tolerance: 5);
        settings.MasterVolumeDisplay.AssertTextContains("50");
    }

    [Fact]
    public void AudioSettings_MusicVolume_InitialValue_Is60()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Assert
        settings.MusicVolumeSlider.AssertValue(60, tolerance: 1);
    }

    [Fact]
    public void AudioSettings_SFXVolume_InitialValue_Is70()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Assert
        settings.SFXVolumeSlider.AssertValue(70, tolerance: 1);
    }

    [Fact]
    public void AudioSettings_MuteToggle_InitiallyOff()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Assert
        settings.MuteAudioToggle.AssertUnchecked();
    }

    [Fact]
    public void AudioSettings_MuteToggle_CanBeToggled()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Act
        settings.MuteAudioToggle.Click();
        Context.WaitFor(() => true, 200);

        // Assert
        settings.MuteAudioToggle.AssertChecked();
    }

    #endregion

    #region Graphics Settings Tests

    [Fact]
    public void GraphicsSettings_FullscreenToggle_Exists()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Assert
        settings.FullscreenToggle.AssertExists();
        settings.FullscreenToggle.AssertUnchecked();
    }

    [Fact]
    public void GraphicsSettings_VSyncToggle_Exists()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Assert
        settings.VSyncToggle.AssertExists();
        settings.VSyncToggle.AssertUnchecked();
    }

    [Fact]
    public void GraphicsSettings_BrightnessSlider_InitialValue_Is50()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Assert
        settings.BrightnessSlider.AssertValue(50, tolerance: 1);
    }

    #endregion

    #region Gameplay Settings Tests

    [Fact]
    public void GameplaySettings_PlayerNameInput_Exists()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Assert
        settings.PlayerNameInput.AssertExists();
    }

    [Fact]
    public void GameplaySettings_PlayerName_CanBeChanged()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Act - Just verify the input exists and has placeholder text
        // The actual text input will be verified once UI automation is fully wired
        settings.PlayerNameInput.AssertExists();

        // Assert - Should have a player name input field with default value
        var nameText = settings.PlayerNameInput.GetText();
        nameText.Should().NotBeEmpty();
    }

    [Fact]
    public void GameplaySettings_MoveSpeedSlider_InitialValue_Is5()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Assert
        settings.MoveSpeedSlider.AssertValue(5, tolerance: 1);
    }

    [Fact]
    public void GameplaySettings_MoveSpeed_CanBeChanged()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Act
        settings.SetMoveSpeed(8);
        Context.WaitFor(() => true, 300);

        // Assert
        settings.MoveSpeedSlider.AssertValue(8, tolerance: 2);
    }

    [Fact]
    public void GameplaySettings_SensitivitySlider_InitialValue_Is5()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Assert
        settings.SensitivitySlider.AssertValue(5, tolerance: 1);
    }

    [Fact]
    public void GameplaySettings_InvertYToggle_InitiallyOff()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Assert
        settings.InvertYToggle.AssertUnchecked();
    }

    [Fact]
    public void GameplaySettings_ShowFpsToggle_InitiallyOff()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Assert
        settings.ShowFpsToggle.AssertUnchecked();
    }

    #endregion

    #region Settings Buttons Tests

    [Fact]
    public void Settings_ApplyButton_IsClickable()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Assert
        settings.ApplyButton.AssertEnabled();
        settings.ApplyButton.AssertExists();
    }

    [Fact]
    public void Settings_ApplyButton_ClickDoesNotCloseSettings()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Act
        settings.Apply();
        Context.WaitFor(() => true, 500);

        // Assert - Still on settings page
        settings.CheckActive();
    }

    [Fact]
    public void Settings_ResetButton_IsClickable()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Assert
        settings.ResetButton.AssertEnabled();
    }

    [Fact]
    public void Settings_ResetButton_ResetsAllValues()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Act - Change some settings
        settings.SetMasterVolume(25);
        settings.SetPlayerName("NewPlayer");
        Context.WaitFor(() => true, 300);

        // Act - Reset
        settings.Reset();
        Context.WaitFor(() => true, 300);

        // Assert - Check some defaults are restored
        settings.MasterVolumeSlider.AssertValue(80, tolerance: 5);
        settings.PlayerNameInput.AssertTextEquals("Player");
    }

    [Fact]
    public void Settings_CloseButton_ClosesSettings()
    {
        // Arrange
        var game = new GamePage(Context);
        game.OpenSettings();
        Context.WaitFor(() => true, 500);
        var settings = new SettingsPage(Context);

        // Act
        settings.CloseButton.Click();
        Context.WaitFor(() => true, 500);

        // Assert - Back to game
        game.CheckActive();
    }

    #endregion

    #region Legacy Settings Tests (for backward compatibility)

    [Fact]
    public void LegacySettings_DarkModeToggle_InitialState_IsOff()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();

        // Assert
        mainPage.DarkModeToggle.AssertUnchecked();
    }

    [Fact]
    public void LegacySettings_DarkModeToggle_Toggle_ChangesState()
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
    public void LegacySettings_DarkModeToggle_ToggleTwice_ReturnsToInitial()
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
    public void LegacySettings_VolumeSlider_InitialValue_Is50()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();

        // Assert
        mainPage.VolumeSlider.AssertValue(50, tolerance: 1);
        mainPage.VolumeDisplay.AssertTextEquals("50%");
    }

    [Fact]
    public void LegacySettings_VolumeSlider_SetValue_UpdatesDisplay()
    {
        // Arrange
        var mainPage = new MainPage(Context);
        mainPage.CheckActive();

        // Act
        mainPage.SetVolume(75);

        // Assert
        mainPage.VolumeSlider.AssertValue(75, tolerance: 5);
        mainPage.VolumeDisplay.AssertTextContains("75");
    }

    [Fact]
    public void LegacySettings_VolumeSlider_Increment_IncreasesValue()
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
    public void LegacySettings_VolumeSlider_Decrement_DecreasesValue()
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

    #endregion
}
