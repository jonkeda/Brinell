namespace Brinell.Maui.UITests.Pages;

/// <summary>
/// Page object for the ToggleTestView. Exposes all toggle controls and their interactions.
/// Demonstrates the page object pattern with control locators and action methods.
/// </summary>
public class ToggleTestPage : PageObjectBase<ToggleTestPage>
{
    public ToggleTestPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "ToggleTestPage";

    #region CheckBox

    /// <summary>
    /// The CheckBox control test element.
    /// </summary>
    public CheckBox<ToggleTestPage> TestCheckBox => new(this, "TestCheckBox");

    /// <summary>
    /// The status label for CheckBox test results.
    /// </summary>
    public Label<ToggleTestPage> CheckBoxStatusLabel => new(this, "CheckBoxStatusLabel");

    #endregion

    #region RadioButton

    /// <summary>
    /// The first RadioButton (Option 1) control test element.
    /// </summary>
    public RadioButton<ToggleTestPage> TestRadioButton1 => new(this, "TestRadioButton1");

    /// <summary>
    /// The second RadioButton (Option 2) control test element.
    /// </summary>
    public RadioButton<ToggleTestPage> TestRadioButton2 => new(this, "TestRadioButton2");

    /// <summary>
    /// The third RadioButton (Option 3) control test element.
    /// </summary>
    public RadioButton<ToggleTestPage> TestRadioButton3 => new(this, "TestRadioButton3");

    /// <summary>
    /// The status label for RadioButton test results.
    /// </summary>
    public Label<ToggleTestPage> RadioButtonStatusLabel => new(this, "RadioButtonStatusLabel");

    #endregion

    #region Switch

    /// <summary>
    /// The Switch control test element.
    /// </summary>
    public Switch<ToggleTestPage> TestSwitch => new(this, "TestSwitch");

    /// <summary>
    /// The status label for Switch test results.
    /// </summary>
    public Label<ToggleTestPage> SwitchStatusLabel => new(this, "SwitchStatusLabel");

    #endregion

    #region Labels

    /// <summary>
    /// The overall status message label showing test results.
    /// </summary>
    public Label<ToggleTestPage> StatusLabel => new(this, "StatusLabel");

    #endregion

    #region Buttons

    /// <summary>
    /// The Reset button to clear all toggle states.
    /// </summary>
    public Button<ToggleTestPage> ResetButton => new(this, "ResetButton");

    #endregion
}
