using Brinell.Maui.Controls.DateTimes;

namespace Brinell.Maui.UITests.Pages2;

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
    public Label<MainPage> TitleLabel => new(this,"TitleLabel");

    /// <summary>
    /// The subtitle label "UI Test Borderwork Demo".
    /// </summary>
    public Label<MainPage> SubtitleLabel => new(this,"SubtitleLabel");

    /// <summary>
    /// The counter display label showing "Counter: X".
    /// </summary>
    public Label<MainPage> CounterLabel => new(this,"CounterLabel");

    /// <summary>
    /// The greeting label that shows the greeting message.
    /// </summary>
    public Label<MainPage> GreetingLabel => new(this,"GreetingLabel");

    /// <summary>
    /// The volume percentage label.
    /// </summary>
    public Label<MainPage> VolumeLabel => new(this,"VolumeLabel");

    /// <summary>
    /// The notification status label.
    /// </summary>
    public Label<MainPage> NotificationLabel => new(this,"NotificationLabel");

    /// <summary>
    /// The selected color label.
    /// </summary>
    public Label<MainPage> SelectedColorLabel => new(this,"SelectedColorLabel");

    #endregion

    #region Buttons (from BasicsView.xaml)

    /// <summary>
    /// The increment (+) button for the counter.
    /// </summary>
    public Button<MainPage> IncrementButton => new(this,"IncrementButton");

    /// <summary>
    /// The decrement (-) button for the counter.
    /// </summary>
    public Button<MainPage> DecrementButton => new(this,"DecrementButton");

    /// <summary>
    /// The reset button for the counter.
    /// </summary>
    public Button<MainPage> ResetButton => new(this,"ResetButton");

    /// <summary>
    /// The greet button that generates a greeting from the name entry.
    /// </summary>
    public Button<MainPage> GreetButton => new(this,"GreetButton");

    /// <summary>
    /// The toggle loading button for the activity indicator.
    /// </summary>
    public Button<MainPage> ToggleLoadingButton => new(this,"ToggleLoadingButton");

    #endregion

    #region Text Input (from BasicsView.xaml)

    /// <summary>
    /// The name entry field.
    /// </summary>
    public Entry<MainPage> NameEntry => new(this,"NameEntry");

    /// <summary>
    /// The email entry field.
    /// </summary>
    public Entry<MainPage> EmailEntry => new(this,"EmailEntry");

    /// <summary>
    /// The message editor (multi-line text).
    /// </summary>
    public Editor<MainPage> MessageEditor => new(this,"MessageEditor");

    #endregion

    #region Toggle Controls (from BasicsView.xaml)

    /// <summary>
    /// The notification switch.
    /// </summary>
    public Switch<MainPage> NotificationSwitch => new(this,"NotificationSwitch");

    /// <summary>
    /// The agree to terms checkbox.
    /// </summary>
    public CheckBox<MainPage> AgreeCheckBox => new(this,"AgreeCheckBox");

    #endregion

    #region Slider and Progress (from BasicsView.xaml)

    /// <summary>
    /// The volume slider control.
    /// </summary>
    public Slider<MainPage> VolumeSlider => new(this,"VolumeSlider");

    /// <summary>
    /// The volume progress bar.
    /// </summary>
    public ProgressBar<MainPage> VolumeProgress => new(this,"VolumeProgress");

    #endregion

    #region Pickers (from BasicsView.xaml)

    /// <summary>
    /// The color picker.
    /// </summary>
    public Picker<MainPage> ColorPicker => new(this, "ColorPicker");

    /// <summary>
    /// The birth date picker.
    /// </summary>
    public DatePicker<MainPage> BirthDatePicker => new(this,"BirthDatePicker");

    /// <summary>
    /// The reminder time picker.
    /// </summary>
    public TimePicker<MainPage> ReminderTimePicker => new(this,"ReminderTimePicker");

    #endregion

    #region Activity Indicator (from BasicsView.xaml)

    /// <summary>
    /// The loading activity indicator.
    /// </summary>
    public ActivityIndicator<MainPage> LoadingIndicator => new(this,"LoadingIndicator");

    #endregion
}
