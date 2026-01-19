using Brinell.Maui.Pages;

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the BasicsView content (first tab) of the Brinell sample MAUI app.
/// Exposes all controls from BasicsView.xaml with their AutomationIds.
/// Demonstrates the page object pattern with control factory methods.
/// </summary>
public class MainPage : MauiPageObjectBase<MainPage>
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
    public MauiControlBase<MainPage> TitleLabel => Control("TitleLabel");

    /// <summary>
    /// The subtitle label "UI Test Framework Demo".
    /// </summary>
    public MauiControlBase<MainPage> SubtitleLabel => Control("SubtitleLabel");

    /// <summary>
    /// The counter display label showing "Counter: X".
    /// </summary>
    public MauiControlBase<MainPage> CounterLabel => Control("CounterLabel");

    /// <summary>
    /// The greeting label that shows the greeting message.
    /// </summary>
    public MauiControlBase<MainPage> GreetingLabel => Control("GreetingLabel");

    /// <summary>
    /// The volume percentage label.
    /// </summary>
    public MauiControlBase<MainPage> VolumeLabel => Control("VolumeLabel");

    /// <summary>
    /// The notification status label.
    /// </summary>
    public MauiControlBase<MainPage> NotificationLabel => Control("NotificationLabel");

    /// <summary>
    /// The selected color label.
    /// </summary>
    public MauiControlBase<MainPage> SelectedColorLabel => Control("SelectedColorLabel");

    #endregion

    #region Buttons (from BasicsView.xaml)

    /// <summary>
    /// The increment (+) button for the counter.
    /// </summary>
    public MauiButtonControl<MainPage> IncrementButton => Button("IncrementButton");

    /// <summary>
    /// The decrement (-) button for the counter.
    /// </summary>
    public MauiButtonControl<MainPage> DecrementButton => Button("DecrementButton");

    /// <summary>
    /// The reset button for the counter.
    /// </summary>
    public MauiButtonControl<MainPage> ResetButton => Button("ResetButton");

    /// <summary>
    /// The greet button that generates a greeting from the name entry.
    /// </summary>
    public MauiButtonControl<MainPage> GreetButton => Button("GreetButton");

    /// <summary>
    /// The toggle loading button for the activity indicator.
    /// </summary>
    public MauiButtonControl<MainPage> ToggleLoadingButton => Button("ToggleLoadingButton");

    #endregion

    #region Text Input (from BasicsView.xaml)

    /// <summary>
    /// The name entry field.
    /// </summary>
    public MauiEntryControl<MainPage> NameEntry => Entry("NameEntry");

    /// <summary>
    /// The email entry field.
    /// </summary>
    public MauiEntryControl<MainPage> EmailEntry => Entry("EmailEntry");

    /// <summary>
    /// The message editor (multi-line text).
    /// </summary>
    public MauiControlBase<MainPage> MessageEditor => Control("MessageEditor");

    #endregion

    #region Toggle Controls (from BasicsView.xaml)

    /// <summary>
    /// The notification switch.
    /// </summary>
    public MauiControlBase<MainPage> NotificationSwitch => Control("NotificationSwitch");

    /// <summary>
    /// The agree to terms checkbox.
    /// </summary>
    public MauiControlBase<MainPage> AgreeCheckBox => Control("AgreeCheckBox");

    #endregion

    #region Slider and Progress (from BasicsView.xaml)

    /// <summary>
    /// The volume slider control.
    /// </summary>
    public MauiControlBase<MainPage> VolumeSlider => Control("VolumeSlider");

    /// <summary>
    /// The volume progress bar.
    /// </summary>
    public MauiControlBase<MainPage> VolumeProgress => Control("VolumeProgress");

    #endregion

    #region Pickers (from BasicsView.xaml)

    /// <summary>
    /// The color picker.
    /// </summary>
    public MauiControlBase<MainPage> ColorPicker => Control("ColorPicker");

    /// <summary>
    /// The birth date picker.
    /// </summary>
    public MauiControlBase<MainPage> BirthDatePicker => Control("BirthDatePicker");

    /// <summary>
    /// The reminder time picker.
    /// </summary>
    public MauiControlBase<MainPage> ReminderTimePicker => Control("ReminderTimePicker");

    #endregion

    #region Activity Indicator (from BasicsView.xaml)

    /// <summary>
    /// The loading activity indicator.
    /// </summary>
    public MauiControlBase<MainPage> LoadingIndicator => Control("LoadingIndicator");

    #endregion
}
