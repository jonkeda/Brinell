using Brinell.Html.Controls;
using Brinell.Html.Controls.Base;
using Brinell.Html.Infrastructure;

namespace Brinell.Samples.Blazor.UITests.PageObjects;

/// <summary>
/// Page object for the Blazor Counter page.
/// </summary>
public class CounterPage : PageBase
{
    /// <summary>
    /// The counter title element.
    /// </summary>
    public LabelControl CounterTitle { get; }

    /// <summary>
    /// The count display element showing current count.
    /// </summary>
    public LabelControl CountDisplay { get; }

    /// <summary>
    /// The increment button.
    /// </summary>
    public ButtonControl IncrementButton { get; }

    /// <summary>
    /// The reset button.
    /// </summary>
    public ButtonControl ResetButton { get; }

    public CounterPage(SeleniumTestContext context)
        : base(context)
    {
        CounterTitle = new LabelControl(context, this, "#counter-title");
        CountDisplay = new LabelControl(context, this, "#count-display");
        IncrementButton = new ButtonControl(context, this, "#increment-btn");
        ResetButton = new ButtonControl(context, this, "#reset-btn");
    }

    /// <summary>
    /// CSS selector that identifies this page.
    /// </summary>
    public override string AutomationId => "#counter-title";

    /// <summary>
    /// Check if the counter page is displayed.
    /// </summary>
    public override bool IsDisplayed()
    {
        return CounterTitle.IsVisible() && CounterTitle.GetText() == "Counter";
    }

    /// <summary>
    /// Click the increment button.
    /// </summary>
    public CounterPage ClickIncrement()
    {
        Log("ClickIncrement()");
        IncrementButton.Click();
        return this;
    }

    /// <summary>
    /// Click the reset button.
    /// </summary>
    public CounterPage ClickReset()
    {
        Log("ClickReset()");
        ResetButton.Click();
        return this;
    }

    /// <summary>
    /// Get the current count value from the display.
    /// </summary>
    public int GetCurrentCount()
    {
        var text = CountDisplay.GetText();
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
    public CounterPage IncrementMultiple(int times)
    {
        Log($"IncrementMultiple({times})");
        for (int i = 0; i < times; i++)
        {
            ClickIncrement();
        }
        return this;
    }

    /// <summary>
    /// Wait for the count to reach a specific value.
    /// </summary>
    public bool WaitForCount(int expectedCount, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? _context.DefaultTimeoutMs;
        Log($"WaitForCount({expectedCount}, timeout: {timeout}ms)");
        return _context.WaitFor(
            () => GetCurrentCount() == expectedCount,
            timeout,
            $"count = {expectedCount}");
    }
    
    /// <summary>
    /// Assert count equals expected value.
    /// </summary>
    public void AssertCount(int expected, string? message = null)
    {
        var actual = GetCurrentCount();
        if (actual != expected)
        {
            throw new Brinell.Core.Exceptions.AssertionException(
                message ?? $"Expected count {expected} but got {actual}.");
        }
    }
}
