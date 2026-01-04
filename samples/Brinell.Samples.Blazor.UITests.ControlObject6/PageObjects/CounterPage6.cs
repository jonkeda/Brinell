using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Controls;
using Brinell.Blazor.ControlObject6.Pages;
using Brinell.Core.ControlObject6.Locators;

namespace Brinell.Samples.Blazor.UITests.ControlObject6.PageObjects;

/// <summary>
/// Page object for the Counter page using ControlObject6 async API.
/// </summary>
public class CounterPage6 : AsyncPageObjectBase
{
    public override string Name => "Counter";

    protected override ControlLocator PageLocator => By.TestId("counter-title");

    public CounterPage6(BlazorTestContext context) : base(context)
    {
    }

    #region Controls

    /// <summary>
    /// The counter title element.
    /// </summary>
    public ButtonControl CounterTitle => Button("counter-title");

    /// <summary>
    /// The count display element showing current count.
    /// </summary>
    public ButtonControl CountDisplay => Button("count-display");

    /// <summary>
    /// The increment button.
    /// </summary>
    public ButtonControl IncrementButton => Button("increment-btn");

    /// <summary>
    /// The reset button.
    /// </summary>
    public ButtonControl ResetButton => Button("reset-btn");

    #endregion

    #region Actions

    /// <summary>
    /// Click the increment button.
    /// </summary>
    public async Task<CounterPage6> ClickIncrementAsync()
    {
        await IncrementButton.ClickAsync();
        return this;
    }

    /// <summary>
    /// Click the reset button.
    /// </summary>
    public async Task<CounterPage6> ClickResetAsync()
    {
        await ResetButton.ClickAsync();
        return this;
    }

    /// <summary>
    /// Get the current count value from the display.
    /// </summary>
    public async Task<int> GetCurrentCountAsync()
    {
        var text = await CountDisplay.GetTextAsync();
        // Text format is "Current count: X"
        var parts = text.Split(':');
        if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var count))
        {
            return count;
        }
        return 0;
    }

    /// <summary>
    /// Increment the counter multiple times.
    /// </summary>
    public async Task<CounterPage6> IncrementMultipleAsync(int times)
    {
        for (int i = 0; i < times; i++)
        {
            await ClickIncrementAsync();
        }
        return this;
    }

    #endregion
}
