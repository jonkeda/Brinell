using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// Playwright control for HTML time input elements.
/// Supports input[type="time"] elements.
/// </summary>
public class TimeInputControl : ControlBase, ITimeControl
{
    public TimeInputControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public TimeInputControl(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public TimeInputControl(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the current time value.
    /// </summary>
    public TimeSpan GetTime()
    {
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not found.");

        var value = element.InputValueAsync().GetAwaiter().GetResult();
        if (string.IsNullOrEmpty(value))
            return TimeSpan.Zero;

        return TimeSpan.Parse(value);
    }

    /// <summary>
    /// Get the current time value asynchronously.
    /// </summary>
    public async Task<TimeSpan> GetTimeAsync()
    {
        var element = await FindElementAsync();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not found.");

        var value = await element.InputValueAsync();
        if (string.IsNullOrEmpty(value))
            return TimeSpan.Zero;

        return TimeSpan.Parse(value);
    }

    /// <summary>
    /// Set the time value.
    /// </summary>
    public void SetTime(TimeSpan time)
    {
        LogAction("SetTime", time.ToString(@"hh\:mm"));
        var element = FindElement();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not found.");

        var formatted = time.ToString(@"hh\:mm");
        element.FillAsync(formatted).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Set the time value asynchronously.
    /// </summary>
    public async Task SetTimeAsync(TimeSpan time)
    {
        LogAction("SetTime", time.ToString(@"hh\:mm"));
        var element = await FindElementAsync();
        if (element == null)
            throw new InvalidOperationException($"Element '{AutomationId}' not found.");

        var formatted = time.ToString(@"hh\:mm");
        await element.FillAsync(formatted);
    }

    /// <summary>
    /// Set the time value using hour and minute components.
    /// </summary>
    public void SetTime(int hour, int minute)
    {
        SetTime(new TimeSpan(hour, minute, 0));
    }

    /// <summary>
    /// Set the time value using components asynchronously.
    /// </summary>
    public Task SetTimeAsync(int hour, int minute)
    {
        return SetTimeAsync(new TimeSpan(hour, minute, 0));
    }

    /// <summary>
    /// Assert the time equals expected value.
    /// </summary>
    public void AssertTime(TimeSpan expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetTime();
        // Compare hours and minutes only
        if (actual.Hours != expected.Hours || actual.Minutes != expected.Minutes)
        {
            ThrowAssertionFailed("Time", actual.ToString(@"hh\:mm"), expected.ToString(@"hh\:mm"),
                message ?? $"Expected time '{expected:hh\\:mm}' but got '{actual:hh\\:mm}' for element '{AutomationId}'.");
        }
        LogAssertPass("Time", actual.ToString(@"hh\:mm"), expected.ToString(@"hh\:mm"));
    }

    /// <summary>
    /// Assert the time equals expected value asynchronously.
    /// </summary>
    public async Task AssertTimeAsync(TimeSpan expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = await GetTimeAsync();
        if (actual.Hours != expected.Hours || actual.Minutes != expected.Minutes)
        {
            ThrowAssertionFailed("Time", actual.ToString(@"hh\:mm"), expected.ToString(@"hh\:mm"),
                message ?? $"Expected time '{expected:hh\\:mm}' but got '{actual:hh\\:mm}' for element '{AutomationId}'.");
        }
        LogAssertPass("Time", actual.ToString(@"hh\:mm"), expected.ToString(@"hh\:mm"));
    }

    /// <summary>
    /// Clear the time input.
    /// </summary>
    public void Clear()
    {
        LogAction("Clear");
        var element = FindElement();
        element?.ClearAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Clear the time input asynchronously.
    /// </summary>
    public async Task ClearAsync()
    {
        LogAction("Clear");
        var element = await FindElementAsync();
        if (element != null)
        {
            await element.ClearAsync();
        }
    }
}
