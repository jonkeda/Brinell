using Brinell.Maui.Pages;

namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the MainPage of the Brinell sample MAUI app.
/// Demonstrates the page object pattern with control factory methods.
/// </summary>
public class MainPage : MauiPageObjectBase<MainPage>
{
    public MainPage(IMauiTestContext context)
        : base(context)
    {
        // Initialize labels
        TitleLabel = Control("TitleLabel");
        SubtitleLabel = Control("SubtitleLabel");
        CounterLabel = Control("CounterLabel");
        GreetingLabel = Control("GreetingLabel");
        VolumeLabel = Control("VolumeLabel");

        // Initialize buttons
        IncrementButton = Button("IncrementButton");
        DecrementButton = Button("DecrementButton");
        ResetButton = Button("ResetButton");
        GreetButton = Button("GreetButton");
        ToggleLoadingButton = Button("ToggleLoadingButton");

        // Initialize entry controls
        NameEntry = Entry("NameEntry");
        EmailEntry = Entry("EmailEntry");
    }

    /// <inheritdoc />
    public override string Name => "MainPage";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        // Page is loaded when the title label exists
        return TitleLabel.IsExists();
    }

    #region Labels

    /// <summary>
    /// The main title label "Brinell MAUI Sample".
    /// </summary>
    public MauiControlBase<MainPage> TitleLabel { get; }

    /// <summary>
    /// The subtitle label "UI Test Framework Demo".
    /// </summary>
    public MauiControlBase<MainPage> SubtitleLabel { get; }

    /// <summary>
    /// The counter display label showing "Counter: X".
    /// </summary>
    public MauiControlBase<MainPage> CounterLabel { get; }

    /// <summary>
    /// The greeting label that shows the greeting message.
    /// </summary>
    public MauiControlBase<MainPage> GreetingLabel { get; }

    /// <summary>
    /// The volume percentage label.
    /// </summary>
    public MauiControlBase<MainPage> VolumeLabel { get; }

    #endregion

    #region Buttons

    /// <summary>
    /// The increment (+) button for the counter.
    /// </summary>
    public MauiButtonControl<MainPage> IncrementButton { get; }

    /// <summary>
    /// The decrement (-) button for the counter.
    /// </summary>
    public MauiButtonControl<MainPage> DecrementButton { get; }

    /// <summary>
    /// The reset button for the counter.
    /// </summary>
    public MauiButtonControl<MainPage> ResetButton { get; }

    /// <summary>
    /// The greet button that generates a greeting from the name entry.
    /// </summary>
    public MauiButtonControl<MainPage> GreetButton { get; }

    /// <summary>
    /// The toggle loading button for the activity indicator.
    /// </summary>
    public MauiButtonControl<MainPage> ToggleLoadingButton { get; }

    #endregion

    #region Entry Controls

    /// <summary>
    /// The name entry field.
    /// </summary>
    public MauiEntryControl<MainPage> NameEntry { get; }

    /// <summary>
    /// The email entry field.
    /// </summary>
    public MauiEntryControl<MainPage> EmailEntry { get; }

    #endregion
}
