using Brinell.Core.Abstractions;
using Brinell.Stride.Controls;
using Brinell.Stride.Infrastructure;
using Brinell.Stride.Pages;

namespace Brinell.Samples.Stride.UITests.PageObjects;

/// <summary>
/// Page object for the settings overlay.
/// </summary>
public class SettingsPage : StridePageBase
{
    /// <inheritdoc />
    public override string Name => "Settings Page";

    public SettingsPage(StrideTestContext context) : base(context, "SettingsPanel")
    {
    }

    #region Audio Controls

    /// <summary>
    /// Master volume slider.
    /// </summary>
    public StrideSliderControl MasterVolumeSlider => Slider("MasterVolumeSlider");

    /// <summary>
    /// Master volume display text block.
    /// </summary>
    public StrideTextBlockControl MasterVolumeDisplay => TextBlock("MasterVolumeDisplay");

    /// <summary>
    /// Music volume slider.
    /// </summary>
    public StrideSliderControl MusicVolumeSlider => Slider("MusicVolumeSlider");

    /// <summary>
    /// Music volume display text block.
    /// </summary>
    public StrideTextBlockControl MusicVolumeDisplay => TextBlock("MusicVolumeDisplay");

    /// <summary>
    /// SFX volume slider.
    /// </summary>
    public StrideSliderControl SFXVolumeSlider => Slider("SFXVolumeSlider");

    /// <summary>
    /// SFX volume display text block.
    /// </summary>
    public StrideTextBlockControl SFXVolumeDisplay => TextBlock("SFXVolumeDisplay");

    /// <summary>
    /// Mute audio toggle button.
    /// </summary>
    public StrideToggleButtonControl MuteAudioToggle => ToggleButton("MuteAudioToggle");

    #endregion

    #region Graphics Controls

    /// <summary>
    /// Fullscreen toggle button.
    /// </summary>
    public StrideToggleButtonControl FullscreenToggle => ToggleButton("FullscreenToggle");

    /// <summary>
    /// VSync toggle button.
    /// </summary>
    public StrideToggleButtonControl VSyncToggle => ToggleButton("VSyncToggle");

    /// <summary>
    /// Brightness slider.
    /// </summary>
    public StrideSliderControl BrightnessSlider => Slider("BrightnessSlider");

    /// <summary>
    /// Brightness display text block.
    /// </summary>
    public StrideTextBlockControl BrightnessDisplay => TextBlock("BrightnessDisplay");

    #endregion

    #region Gameplay Controls

    /// <summary>
    /// Player name input field.
    /// </summary>
    public StrideEditTextControl PlayerNameInput => EditText("PlayerNameInput");

    /// <summary>
    /// Move speed slider.
    /// </summary>
    public StrideSliderControl MoveSpeedSlider => Slider("MoveSpeedSlider");

    /// <summary>
    /// Move speed display text block.
    /// </summary>
    public StrideTextBlockControl MoveSpeedDisplay => TextBlock("MoveSpeedDisplay");

    /// <summary>
    /// Camera sensitivity slider.
    /// </summary>
    public StrideSliderControl SensitivitySlider => Slider("SensitivitySlider");

    /// <summary>
    /// Sensitivity display text block.
    /// </summary>
    public StrideTextBlockControl SensitivityDisplay => TextBlock("SensitivityDisplay");

    /// <summary>
    /// Invert Y axis toggle button.
    /// </summary>
    public StrideToggleButtonControl InvertYToggle => ToggleButton("InvertYToggle");

    /// <summary>
    /// Show FPS counter toggle button.
    /// </summary>
    public StrideToggleButtonControl ShowFpsToggle => ToggleButton("ShowFPSToggle");

    #endregion

    #region Buttons

    /// <summary>
    /// Apply settings button.
    /// </summary>
    public StrideButtonControl ApplyButton => Button("ApplyButton");

    /// <summary>
    /// Reset to defaults button.
    /// </summary>
    public StrideButtonControl ResetButton => Button("ResetButton");

    /// <summary>
    /// Close settings button.
    /// </summary>
    public StrideButtonControl CloseButton => Button("CloseButton");

    #endregion

    #region Actions

    /// <summary>
    /// Close the settings page using ESC key.
    /// </summary>
    public void Close()
    {
        PressKey(VirtualKey.Escape);
    }

    /// <summary>
    /// Set master volume to a specific value.
    /// </summary>
    public void SetMasterVolume(double value)
    {
        MasterVolumeSlider.SetValue(value);
    }

    /// <summary>
    /// Set music volume to a specific value.
    /// </summary>
    public void SetMusicVolume(double value)
    {
        MusicVolumeSlider.SetValue(value);
    }

    /// <summary>
    /// Set SFX volume to a specific value.
    /// </summary>
    public void SetSFXVolume(double value)
    {
        SFXVolumeSlider.SetValue(value);
    }

    /// <summary>
    /// Set brightness to a specific value.
    /// </summary>
    public void SetBrightness(double value)
    {
        BrightnessSlider.SetValue(value);
    }

    /// <summary>
    /// Set player name.
    /// </summary>
    public void SetPlayerName(string name)
    {
        PlayerNameInput.SetText(name);
    }

    /// <summary>
    /// Set move speed to a specific value.
    /// </summary>
    public void SetMoveSpeed(double value)
    {
        MoveSpeedSlider.SetValue(value);
    }

    /// <summary>
    /// Set camera sensitivity to a specific value.
    /// </summary>
    public void SetSensitivity(double value)
    {
        SensitivitySlider.SetValue(value);
    }

    /// <summary>
    /// Click the Apply button.
    /// </summary>
    public void Apply()
    {
        ApplyButton.Click();
    }

    /// <summary>
    /// Click the Reset button.
    /// </summary>
    public void Reset()
    {
        ResetButton.Click();
    }

    #endregion
}
