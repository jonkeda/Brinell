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

    #endregion

    #region Buttons

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

    #region Entry Controls

    /// <summary>
    /// The name entry field.
    /// </summary>
    public MauiEntryControl<MainPage> NameEntry => Entry("NameEntry");

    /// <summary>
    /// The email entry field.
    /// </summary>
    public MauiEntryControl<MainPage> EmailEntry => Entry("EmailEntry");

    #endregion
}
