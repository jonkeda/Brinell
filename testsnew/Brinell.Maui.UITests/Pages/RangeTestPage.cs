namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the RangeTestView. Exposes all range controls and their interactions.
/// Demonstrates the page object pattern with slider and stepper locators and action methods.
/// </summary>
public class RangeTestPage : PageObjectBase<RangeTestPage>
{
    public RangeTestPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "RangeTestPage";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        // Page is loaded when the status label exists
        return StatusLabel.IsExists();
    }

    #region Slider

    /// <summary>
    /// The Slider test control for testing value range adjustments.
    /// </summary>
    public Slider<RangeTestPage> TestSlider => Slider("TestSlider");

    /// <summary>
    /// The Slider value display label.
    /// </summary>
    public Label<RangeTestPage> SliderValueLabel => Label("SliderValueLabel");

    #endregion

    #region Stepper

    /// <summary>
    /// The Stepper test control for testing increment/decrement operations.
    /// </summary>
    public Stepper<RangeTestPage> TestStepper => Stepper("TestStepper");

    /// <summary>
    /// The Stepper value display label.
    /// </summary>
    public Label<RangeTestPage> StepperValueLabel => Label("StepperValueLabel");

    #endregion

    #region Labels

    /// <summary>
    /// The status message label showing test results and current state.
    /// </summary>
    public Label<RangeTestPage> StatusLabel => Label("StatusLabel");

    #endregion

    #region Buttons

    /// <summary>
    /// The Reset button to restore controls to initial state.
    /// </summary>
    public Button<RangeTestPage> ResetButton => Button("ResetButton");

    #endregion
}
