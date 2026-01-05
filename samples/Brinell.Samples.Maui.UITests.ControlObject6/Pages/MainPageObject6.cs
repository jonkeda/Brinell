using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;
using Brinell.Maui.ControlObject6.Controls;
using Brinell.Maui.ControlObject6.Pages;

namespace Brinell.Samples.Maui.UITests.ControlObject6.Pages;

/// <summary>
/// Page object for the MainPage using ControlObject6 API.
/// Uses the 'new' pattern for control creation.
/// </summary>
public class MainPageObject6 : PageObjectBase
{
    public override string Name => "MainPage";

    protected override ControlLocator PageLocator => By.AutomationId("TitleLabel");

    public MainPageObject6(MauiTestContext context) : base(context)
    {
    }

    #region Headers

    /// <summary>Title label at the top of the page.</summary>
    public LabelControl TitleLabel => new(Context, "TitleLabel", this);

    /// <summary>Subtitle label.</summary>
    public LabelControl SubtitleLabel => new(Context, "SubtitleLabel", this);

    #endregion

    #region Counter Controls

    /// <summary>The counter display label.</summary>
    public LabelControl CounterLabel => new(Context, "CounterLabel", this);

    /// <summary>Increment button for the counter.</summary>
    public ButtonControl IncrementButton => new(Context, "IncrementButton", this);

    /// <summary>Decrement button for the counter.</summary>
    public ButtonControl DecrementButton => new(Context, "DecrementButton", this);

    /// <summary>Reset button for the counter.</summary>
    public ButtonControl ResetButton => new(Context, "ResetButton", this);

    /// <summary>Counter frame container.</summary>
    public FrameControl CounterFrame => new(Context, "CounterFrame", this);

    #endregion

    #region Text Input Controls

    /// <summary>Name entry field.</summary>
    public EntryControl NameEntry => new(Context, "NameEntry", this);

    /// <summary>Email entry field.</summary>
    public EntryControl EmailEntry => new(Context, "EmailEntry", this);

    /// <summary>Message editor (multi-line text).</summary>
    public EditorControl MessageEditor => new(Context, "MessageEditor", this);

    /// <summary>Greeting label that shows the greeting message.</summary>
    public LabelControl GreetingLabel => new(Context, "GreetingLabel", this);

    /// <summary>Greet button to trigger greeting.</summary>
    public ButtonControl GreetButton => new(Context, "GreetButton", this);

    /// <summary>Text input frame container.</summary>
    public FrameControl TextInputFrame => new(Context, "TextInputFrame", this);

    #endregion

    #region Toggle Controls

    /// <summary>Notification switch.</summary>
    public SwitchControl NotificationSwitch => new(Context, "NotificationSwitch", this);

    /// <summary>Notification label.</summary>
    public LabelControl NotificationLabel => new(Context, "NotificationLabel", this);

    /// <summary>Agree checkbox.</summary>
    public CheckBoxControl AgreeCheckBox => new(Context, "AgreeCheckBox", this);

    /// <summary>Toggle frame container.</summary>
    public FrameControl ToggleFrame => new(Context, "ToggleFrame", this);

    #endregion

    #region Slider Controls

    /// <summary>Volume slider.</summary>
    public SliderControl VolumeSlider => new(Context, "VolumeSlider", this);

    /// <summary>Volume label.</summary>
    public LabelControl VolumeLabel => new(Context, "VolumeLabel", this);

    /// <summary>Volume progress bar.</summary>
    public ProgressBarControl VolumeProgress => new(Context, "VolumeProgress", this);

    /// <summary>Slider frame container.</summary>
    public FrameControl SliderFrame => new(Context, "SliderFrame", this);

    #endregion

    #region Picker Controls

    /// <summary>Color picker.</summary>
    public PickerControl ColorPicker => new(Context, "ColorPicker", this);

    /// <summary>Selected color label.</summary>
    public LabelControl SelectedColorLabel => new(Context, "SelectedColorLabel", this);

    /// <summary>Birth date picker.</summary>
    public DatePickerControl BirthDatePicker => new(Context, "BirthDatePicker", this);

    /// <summary>Reminder time picker.</summary>
    public TimePickerControl ReminderTimePicker => new(Context, "ReminderTimePicker", this);

    /// <summary>Picker frame container.</summary>
    public FrameControl PickerFrame => new(Context, "PickerFrame", this);

    #endregion

    #region Activity Controls

    /// <summary>Loading activity indicator.</summary>
    public ActivityIndicatorControl LoadingIndicator => new(Context, "LoadingIndicator", this);

    /// <summary>Toggle loading button.</summary>
    public ButtonControl ToggleLoadingButton => new(Context, "ToggleLoadingButton", this);

    /// <summary>Activity frame container.</summary>
    public FrameControl ActivityFrame => new(Context, "ActivityFrame", this);

    #endregion

    #region Scroll View

    /// <summary>Main scroll view.</summary>
    public ScrollViewControl MainScrollView => new(Context, "MainScrollView", this);

    #endregion

    #region Page Actions

    /// <summary>Click increment and return new count.</summary>
    public MainPageObject6 ClickIncrement()
    {
        IncrementButton.Click();
        return this;
    }

    /// <summary>Click decrement and return new count.</summary>
    public MainPageObject6 ClickDecrement()
    {
        DecrementButton.Click();
        return this;
    }

    /// <summary>Click reset button.</summary>
    public MainPageObject6 ClickReset()
    {
        ResetButton.Click();
        return this;
    }

    /// <summary>Enter name and click greet.</summary>
    public MainPageObject6 EnterNameAndGreet(string name)
    {
        NameEntry.Enter(name);
        GreetButton.Click();
        return this;
    }

    /// <summary>Get the current counter text.</summary>
    public string GetCounterText()
    {
        return CounterLabel.GetText();
    }

    /// <summary>Parse the counter value from the label text.</summary>
    public int GetCounterValue()
    {
        var text = GetCounterText();
        // Expected format: "Counter: X" or just "X"
        var parts = text.Split(':');
        var valuePart = parts.Length > 1 ? parts[1].Trim() : text.Trim();
        return int.TryParse(valuePart, out var value) ? value : 0;
    }

    /// <summary>Sets volume slider to a value.</summary>
    public MainPageObject6 SetVolume(double value)
    {
        VolumeSlider.SetValue(value);
        return this;
    }

    /// <summary>Gets the current volume value.</summary>
    public double GetVolume()
    {
        return VolumeSlider.GetValue();
    }

    /// <summary>Toggles the notification switch.</summary>
    public MainPageObject6 ToggleNotifications()
    {
        NotificationSwitch.Toggle();
        return this;
    }

    /// <summary>Selects a color from the picker.</summary>
    public MainPageObject6 SelectColor(string color)
    {
        ColorPicker.SelectByText(color);
        return this;
    }

    /// <summary>Toggles the loading indicator.</summary>
    public MainPageObject6 ToggleLoading()
    {
        ToggleLoadingButton.Click();
        return this;
    }

    /// <summary>Checks if the loading indicator is running.</summary>
    public bool IsLoading()
    {
        return LoadingIndicator.IsRunning();
    }

    #endregion
}
