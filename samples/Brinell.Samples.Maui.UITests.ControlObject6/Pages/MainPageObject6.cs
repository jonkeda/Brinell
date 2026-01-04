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

    #region Counter Controls

    /// <summary>
    /// Title label at the top of the page.
    /// </summary>
    public ButtonControl TitleLabel => Button("TitleLabel");

    /// <summary>
    /// The counter display label.
    /// </summary>
    public ButtonControl CounterLabel => Button("CounterLabel");

    /// <summary>
    /// Increment button for the counter.
    /// </summary>
    public ButtonControl IncrementButton => Button("IncrementButton");

    /// <summary>
    /// Decrement button for the counter.
    /// </summary>
    public ButtonControl DecrementButton => Button("DecrementButton");

    /// <summary>
    /// Reset button for the counter.
    /// </summary>
    public ButtonControl ResetButton => Button("ResetButton");

    #endregion

    #region Text Input Controls

    /// <summary>
    /// Name entry field.
    /// </summary>
    public EntryControl NameEntry => Entry("NameEntry");

    /// <summary>
    /// Email entry field.
    /// </summary>
    public EntryControl EmailEntry => Entry("EmailEntry");

    /// <summary>
    /// Greeting label that shows the greeting message.
    /// </summary>
    public ButtonControl GreetingLabel => Button("GreetingLabel");

    /// <summary>
    /// Greet button to trigger greeting.
    /// </summary>
    public ButtonControl GreetButton => Button("GreetButton");

    #endregion

    #region Page Actions

    /// <summary>
    /// Click increment and return new count.
    /// </summary>
    public MainPageObject6 ClickIncrement()
    {
        IncrementButton.Click();
        return this;
    }

    /// <summary>
    /// Click decrement and return new count.
    /// </summary>
    public MainPageObject6 ClickDecrement()
    {
        DecrementButton.Click();
        return this;
    }

    /// <summary>
    /// Click reset button.
    /// </summary>
    public MainPageObject6 ClickReset()
    {
        ResetButton.Click();
        return this;
    }

    /// <summary>
    /// Enter name and click greet.
    /// </summary>
    public MainPageObject6 EnterNameAndGreet(string name)
    {
        NameEntry.Enter(name);
        GreetButton.Click();
        return this;
    }

    /// <summary>
    /// Get the current counter text.
    /// </summary>
    public string GetCounterText()
    {
        return CounterLabel.GetText();
    }

    /// <summary>
    /// Parse the counter value from the label text.
    /// </summary>
    public int GetCounterValue()
    {
        var text = GetCounterText();
        // Expected format: "Count: X" or just "X"
        var parts = text.Split(':');
        var valuePart = parts.Length > 1 ? parts[1].Trim() : text.Trim();
        return int.TryParse(valuePart, out var value) ? value : 0;
    }

    #endregion
}
