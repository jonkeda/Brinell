using Brinell.Maui.Pages;

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the BasicsView content (first tab) of the Brinell sample MAUI app.
/// Exposes all controls from BasicsView.xaml with their AutomationIds.
/// Demonstrates the page object pattern with control factory methods.
/// </summary>
public class MainPage : PageObjectBase<MainPage>
{
    public MainPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "MainPage";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        // Page is loaded when the title label exists
        return TitleLabel.IsExists();
    }

    #region Labels (from BasicsView.xaml)

    /// <summary>
    /// The main title label "Brinell MAUI Sample".
    /// </summary>
    public ControlBase<MainPage> TitleLabel => Control("TitleLabel");

    /// <summary>
    /// The subtitle label "UI Test Framework Demo".
    /// </summary>
    public ControlBase<MainPage> SubtitleLabel => Control("SubtitleLabel");

    /// <summary>
    /// The counter display label showing "Counter: X".
    /// </summary>
    public ControlBase<MainPage> CounterLabel => Control("CounterLabel");

    /// <summary>
    /// The greeting label that shows the greeting message.
    /// </summary>
    public ControlBase<MainPage> GreetingLabel => Control("GreetingLabel");

    /// <summary>
    /// The volume percentage label.
    /// </summary>
    public ControlBase<MainPage> VolumeLabel => Control("VolumeLabel");

    /// <summary>
    /// The notification status label.
    /// </summary>
    public ControlBase<MainPage> NotificationLabel => Control("NotificationLabel");

    /// <summary>
    /// The selected color label.
    /// </summary>
    public ControlBase<MainPage> SelectedColorLabel => Control("SelectedColorLabel");

    #endregion

    #region Buttons (from BasicsView.xaml)

    /// <summary>
    /// The increment (+) button for the counter.
    /// </summary>
    public Button<MainPage> IncrementButton => Button("IncrementButton");

    /// <summary>
    /// The decrement (-) button for the counter.
    /// </summary>
    public Button<MainPage> DecrementButton => Button("DecrementButton");

    /// <summary>
    /// The reset button for the counter.
    /// </summary>
    public Button<MainPage> ResetButton => Button("ResetButton");

    /// <summary>
    /// The greet button that generates a greeting from the name entry.
    /// </summary>
    public Button<MainPage> GreetButton => Button("GreetButton");

    /// <summary>
    /// The toggle loading button for the activity indicator.
    /// </summary>
    public Button<MainPage> ToggleLoadingButton => Button("ToggleLoadingButton");

    #endregion

    #region Text Input (from BasicsView.xaml)

    /// <summary>
    /// The name entry field.
    /// </summary>
    public Entry<MainPage> NameEntry => Entry("NameEntry");

    /// <summary>
    /// The email entry field.
    /// </summary>
    public Entry<MainPage> EmailEntry => Entry("EmailEntry");

    /// <summary>
    /// The message editor (multi-line text).
    /// </summary>
    public ControlBase<MainPage> MessageEditor => Control("MessageEditor");

    #endregion

    #region Toggle Controls (from BasicsView.xaml)

    /// <summary>
    /// The notification switch.
    /// </summary>
    public ControlBase<MainPage> NotificationSwitch => Control("NotificationSwitch");

    /// <summary>
    /// The agree to terms checkbox.
    /// </summary>
    public ControlBase<MainPage> AgreeCheckBox => Control("AgreeCheckBox");

    #endregion

    #region Slider and Progress (from BasicsView.xaml)

    /// <summary>
    /// The volume slider control.
    /// </summary>
    public ControlBase<MainPage> VolumeSlider => Control("VolumeSlider");

    /// <summary>
    /// The volume progress bar.
    /// </summary>
    public ControlBase<MainPage> VolumeProgress => Control("VolumeProgress");

    #endregion

    #region Pickers (from BasicsView.xaml)

    /// <summary>
    /// The color picker.
    /// </summary>
    public ControlBase<MainPage> ColorPicker => Control("ColorPicker");

    /// <summary>
    /// The birth date picker.
    /// </summary>
    public ControlBase<MainPage> BirthDatePicker => Control("BirthDatePicker");

    /// <summary>
    /// The reminder time picker.
    /// </summary>
    public ControlBase<MainPage> ReminderTimePicker => Control("ReminderTimePicker");

    #endregion

    #region Activity Indicator (from BasicsView.xaml)

    /// <summary>
    /// The loading activity indicator.
    /// </summary>
    public ControlBase<MainPage> LoadingIndicator => Control("LoadingIndicator");

    #endregion
}
