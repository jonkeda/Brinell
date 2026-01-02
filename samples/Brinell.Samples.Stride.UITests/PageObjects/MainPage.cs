using Brinell.Core.Abstractions;
using Brinell.Stride.Controls;
using Brinell.Stride.Infrastructure;
using Brinell.Stride.Pages;

namespace Brinell.Samples.Stride.UITests.PageObjects;

/// <summary>
/// Page object for the main sample app page (legacy, for backward compatibility).
/// Note: With the new game-focused UI, use GamePage for gameplay and SettingsPage for settings.
/// This page is kept for compatibility with existing tests.
/// </summary>
public class MainPage : StridePageBase
{
    /// <inheritdoc />
    public override string Name => "Main Page";

    public MainPage(StrideTestContext context) : base(context, "MainPanel")
    {
    }

    #region Legacy Counter & Greeting Controls

    /// <summary>
    /// Title text block.
    /// </summary>
    public StrideTextBlockControl Title => TextBlock("Title");

    /// <summary>
    /// Counter display text block.
    /// </summary>
    public StrideTextBlockControl CounterDisplay => TextBlock("CounterDisplay");

    /// <summary>
    /// Increment counter button.
    /// </summary>
    public StrideButtonControl IncrementButton => Button("IncrementButton");

    /// <summary>
    /// Decrement counter button.
    /// </summary>
    public StrideButtonControl DecrementButton => Button("DecrementButton");

    /// <summary>
    /// Reset counter button.
    /// </summary>
    public StrideButtonControl ResetButton => Button("ResetButton");

    /// <summary>
    /// Name input field.
    /// </summary>
    public StrideEditTextControl NameInput => EditText("NameInput");

    /// <summary>
    /// Greet button.
    /// </summary>
    public StrideButtonControl GreetButton => Button("GreetButton");

    /// <summary>
    /// Greeting display text block.
    /// </summary>
    public StrideTextBlockControl GreetingDisplay => TextBlock("GreetingDisplay");

    /// <summary>
    /// Dark mode toggle button.
    /// </summary>
    public StrideToggleButtonControl DarkModeToggle => ToggleButton("DarkModeToggle");

    /// <summary>
    /// Volume slider.
    /// </summary>
    public StrideSliderControl VolumeSlider => Slider("VolumeSlider");

    /// <summary>
    /// Volume display text block.
    /// </summary>
    public StrideTextBlockControl VolumeDisplay => TextBlock("VolumeDisplay");

    #endregion

    #region Actions

    /// <summary>
    /// Get the current counter value from the display.
    /// </summary>
    public int GetCounterValue()
    {
        var text = CounterDisplay.GetText();
        var valueStr = text.Replace("Count: ", "");
        return int.TryParse(valueStr, out var value) ? value : 0;
    }

    /// <summary>
    /// Increment the counter and wait for update.
    /// </summary>
    public void IncrementCounter()
    {
        var before = GetCounterValue();
        IncrementButton.Click();
        WaitFor(() => GetCounterValue() == before + 1);
    }

    /// <summary>
    /// Decrement the counter and wait for update.
    /// </summary>
    public void DecrementCounter()
    {
        var before = GetCounterValue();
        DecrementButton.Click();
        WaitFor(() => GetCounterValue() == before - 1);
    }

    /// <summary>
    /// Reset the counter and wait for update.
    /// </summary>
    public void ResetCounter()
    {
        ResetButton.Click();
        WaitFor(() => GetCounterValue() == 0);
    }

    /// <summary>
    /// Enter name and greet.
    /// </summary>
    public void Greet(string name)
    {
        NameInput.SetText(name);
        GreetButton.Click();
        WaitFor(() => !string.IsNullOrEmpty(GreetingDisplay.GetText()));
    }

    /// <summary>
    /// Set volume slider.
    /// </summary>
    public void SetVolume(double value)
    {
        VolumeSlider.SetValue(value);
    }

    /// <summary>
    /// Toggle dark mode.
    /// </summary>
    public void ToggleDarkMode()
    {
        DarkModeToggle.Toggle();
    }

    #endregion
}
