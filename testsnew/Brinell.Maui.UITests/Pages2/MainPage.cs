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
    public Label<MainPage> TitleLabel => Label("TitleLabel");

    /// <summary>
    /// The subtitle label "UI Test Borderwork Demo".
    /// </summary>
    public Label<MainPage> SubtitleLabel => Label("SubtitleLabel");

    /// <summary>
    /// The counter display label showing "Counter: X".
    /// </summary>
    public Label<MainPage> CounterLabel => Label("CounterLabel");

    /// <summary>
    /// The greeting label that shows the greeting message.
    /// </summary>
    public Label<MainPage> GreetingLabel => Label("GreetingLabel");

    /// <summary>
    /// The volume percentage label.
    /// </summary>
    public Label<MainPage> VolumeLabel => Label("VolumeLabel");

    /// <summary>
    /// The notification status label.
    /// </summary>
    public Label<MainPage> NotificationLabel => Label("NotificationLabel");

    /// <summary>
    /// The selected color label.
    /// </summary>
    public Label<MainPage> SelectedColorLabel => Label("SelectedColorLabel");

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
    public Editor<MainPage> MessageEditor => Editor("MessageEditor");

    #endregion

    #region Toggle Controls (from BasicsView.xaml)

    /// <summary>
    /// The notification switch.
    /// </summary>
    public Switch<MainPage> NotificationSwitch => Switch("NotificationSwitch");

    /// <summary>
    /// The agree to terms checkbox.
    /// </summary>
    public CheckBox<MainPage> AgreeCheckBox => CheckBox("AgreeCheckBox");

    #endregion

    #region Slider and Progress (from BasicsView.xaml)

    /// <summary>
    /// The volume slider control.
    /// </summary>
    public Slider<MainPage> VolumeSlider => Slider("VolumeSlider");

    /// <summary>
    /// The volume progress bar.
    /// </summary>
    public ProgressBar<MainPage> VolumeProgress => ProgressBar("VolumeProgress");

    #endregion

    #region Pickers (from BasicsView.xaml)

    /// <summary>
    /// The color picker.
    /// </summary>
    public Picker<MainPage> ColorPicker => new(this, "ColorPicker");

    /// <summary>
    /// The birth date picker.
    /// </summary>
    public DatePicker<MainPage> BirthDatePicker => DatePicker("BirthDatePicker");

    /// <summary>
    /// The reminder time picker.
    /// </summary>
    public TimePicker<MainPage> ReminderTimePicker => TimePicker("ReminderTimePicker");

    #endregion

    #region Activity Indicator (from BasicsView.xaml)

    /// <summary>
    /// The loading activity indicator.
    /// </summary>
    public ActivityIndicator<MainPage> LoadingIndicator => ActivityIndicator("LoadingIndicator");

    #endregion
}
