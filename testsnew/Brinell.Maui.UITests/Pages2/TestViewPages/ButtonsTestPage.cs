namespace Brinell.Maui.UITests.Pages2.TestViewPages;

/// <summary>
/// Page object for the ButtonsTestView. Exposes all button controls and their interactions.
/// Demonstrates the page object pattern with control locators and action methods.
/// </summary>
public class ButtonsTestPage : PageObjectBase<ButtonsTestPage>
{
    public ButtonsTestPage(IMauiTestContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public override string Name => "ButtonsTestPage";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null)
    {
        // Page is loaded when the status label exists
        return StatusLabel.IsExists();
    }

    #region Buttons

    /// <summary>
    /// The basic Button test control.
    /// </summary>
    public Button<ButtonsTestPage> TestButton => Button("TestButton");

    /// <summary>
    /// The IconCommandButton test control.
    /// </summary>
    public Button<ButtonsTestPage> TestIconCommandButton => Button("TestIconCommandButton");

    /// <summary>
    /// The ImageButton test control.
    /// </summary>
    public Button<ButtonsTestPage> TestImageButton => Button("TestImageButton");

    /// <summary>
    /// The Link test control.
    /// </summary>
    public Button<ButtonsTestPage> TestLinkButton => Button("TestLink");

    /// <summary>
    /// The RoundButton test control.
    /// </summary>
    public Button<ButtonsTestPage> TestRoundButton => Button("TestRoundButton");

    /// <summary>
    /// The Reset button.
    /// </summary>
    public Button<ButtonsTestPage> ResetButton => Button("ResetButton");

    #endregion

    #region Labels

    /// <summary>
    /// The status message label showing test results.
    /// </summary>
    public Label<ButtonsTestPage> StatusLabel => Label("StatusLabel");

    #endregion

    #region Actions

    /// <summary>
    /// Taps the basic Button and waits for the page to update.
    /// </summary>
    public ButtonsTestPage TapButton()
    {
        TestButton.Click();
        return this;
    }

    /// <summary>
    /// Taps the IconCommandButton and waits for the page to update.
    /// </summary>
    public ButtonsTestPage TapIconCommandButton()
    {
        TestIconCommandButton.Click();
        return this;
    }

    /// <summary>
    /// Taps the ImageButton and waits for the page to update.
    /// </summary>
    public ButtonsTestPage TapImageButton()
    {
        TestImageButton.Click();
        return this;
    }

    /// <summary>
    /// Taps the Link button and waits for the page to update.
    /// </summary>
    public ButtonsTestPage TapLinkButton()
    {
        TestLinkButton.Click();
        return this;
    }

    /// <summary>
    /// Taps the RoundButton and waits for the page to update.
    /// </summary>
    public ButtonsTestPage TapRoundButton()
    {
        TestRoundButton.Click();
        return this;
    }

    /// <summary>
    /// Taps the Reset button to clear all state.
    /// </summary>
    public ButtonsTestPage Reset()
    {
        ResetButton.Click();
        return this;
    }

    /// <summary>
    /// Gets the current status message text.
    /// </summary>
    public string GetStatusMessage()
    {
        return StatusLabel.GetAttribute("text") ?? string.Empty;
    }

    /// <summary>
    /// Verifies that the status message contains the given text.
    /// </summary>
    public ButtonsTestPage VerifyStatusContains(string expectedText)
    {
        var status = GetStatusMessage();
        Assert.Contains(expectedText, status);
        return this;
    }

    #endregion
}
