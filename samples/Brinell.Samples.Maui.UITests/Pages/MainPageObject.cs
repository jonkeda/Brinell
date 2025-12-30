using Brinell.Core.Abstractions;
using Brinell.Maui.Controls;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Samples.Maui.UITests.Pages;

/// <summary>
/// Page object for the MainPage.
/// </summary>
public class MainPageObject : PageBase
{
    public override string AutomationId => "MainPage";

    public MainPageObject(AppiumTestContext context) : base(context)
    {
    }

    /// <summary>
    /// Override IsDisplayed to use a reliable visible element.
    /// ContentPage AutomationId may not be accessible on Windows MAUI.
    /// </summary>
    public override bool IsDisplayed()
    {
        // Check for TitleLabel which is always visible at top of page
        return _context.ElementIsVisible("TitleLabel");
    }

    // Counter controls
    public LabelControl TitleLabel => new(_context, this, "TitleLabel");
    public LabelControl SubtitleLabel => new(_context, this, "SubtitleLabel");
    public LabelControl CounterLabel => new(_context, this, "CounterLabel");
    public ButtonControl IncrementButton => new(_context, this, "IncrementButton");
    public ButtonControl DecrementButton => new(_context, this, "DecrementButton");
    public ButtonControl ResetButton => new(_context, this, "ResetButton");

    // Text input controls
    public EntryControl NameEntry => new(_context, this, "NameEntry");
    public EntryControl EmailEntry => new(_context, this, "EmailEntry");
    public EditorControl MessageEditor => new(_context, this, "MessageEditor");
    public LabelControl GreetingLabel => new(_context, this, "GreetingLabel");
    public ButtonControl GreetButton => new(_context, this, "GreetButton");

    // Toggle controls
    public SwitchControl NotificationSwitch => new(_context, this, "NotificationSwitch");
    public CheckBoxControl AgreeCheckBox => new(_context, this, "AgreeCheckBox");

    // Slider controls
    public SliderControl VolumeSlider => new(_context, this, "VolumeSlider");
    public LabelControl VolumeLabel => new(_context, this, "VolumeLabel");
    public ProgressBarControl VolumeProgress => new(_context, this, "VolumeProgress");

    // Picker controls
    public PickerControl ColorPicker => new(_context, this, "ColorPicker");
    public LabelControl SelectedColorLabel => new(_context, this, "SelectedColorLabel");
    public DatePickerControl BirthDatePicker => new(_context, this, "BirthDatePicker");
    public TimePickerControl ReminderTimePicker => new(_context, this, "ReminderTimePicker");

    // Activity indicator
    public ActivityIndicatorControl LoadingIndicator => new(_context, this, "LoadingIndicator");
    public ButtonControl ToggleLoadingButton => new(_context, this, "ToggleLoadingButton");

    // Scroll view
    public ScrollViewControl MainScrollView => new(_context, this, "MainScrollView");

    /// <summary>
    /// Wait for page to be fully loaded.
    /// </summary>
    public bool WaitForPageLoad(int? timeoutMs = null) => WaitForDisplayed(timeoutMs);
}
