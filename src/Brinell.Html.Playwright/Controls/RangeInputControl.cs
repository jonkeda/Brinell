using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// Playwright range input (slider) control wrapper.
/// Works with &lt;input type="range"&gt; elements.
/// </summary>
public class RangeInputControl : RangeControlBase
{
    public RangeInputControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public RangeInputControl(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public RangeInputControl(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get current value as string.
    /// </summary>
    public override string GetText()
    {
        return GetValue().ToString();
    }

    /// <summary>
    /// Get current value as string asynchronously.
    /// </summary>
    public override async Task<string> GetTextAsync()
    {
        var value = await GetValueAsync();
        return value.ToString();
    }

    /// <summary>
    /// Set value to a specific percentage (0-100).
    /// </summary>
    public void SetPercentage(double percentage)
    {
        SetPercentageAsync(percentage).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Set value to a specific percentage (0-100) asynchronously.
    /// </summary>
    public async Task SetPercentageAsync(double percentage)
    {
        LogAction("SetPercentage", $"{percentage}%");
        var min = await GetMinimumAsync();
        var max = await GetMaximumAsync();
        var value = min + (max - min) * (percentage / 100.0);
        await SetValueAsync(value);
    }

    /// <summary>
    /// Increment by multiple steps.
    /// </summary>
    public void IncrementBy(int steps)
    {
        IncrementByAsync(steps).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Increment by multiple steps asynchronously.
    /// </summary>
    public async Task IncrementByAsync(int steps)
    {
        LogAction("IncrementBy", steps.ToString());
        var step = await GetStepAsync();
        var current = await GetValueAsync();
        var max = await GetMaximumAsync();
        await SetValueAsync(Math.Min(current + step * steps, max));
    }

    /// <summary>
    /// Decrement by multiple steps.
    /// </summary>
    public void DecrementBy(int steps)
    {
        DecrementByAsync(steps).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Decrement by multiple steps asynchronously.
    /// </summary>
    public async Task DecrementByAsync(int steps)
    {
        LogAction("DecrementBy", steps.ToString());
        var step = await GetStepAsync();
        var current = await GetValueAsync();
        var min = await GetMinimumAsync();
        await SetValueAsync(Math.Max(current - step * steps, min));
    }

    /// <summary>
    /// Assert value is within range.
    /// </summary>
    public void AssertValueInRange(double min, double max, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetValue();
        if (actual < min || actual > max)
        {
            ThrowAssertionFailed("ValueInRange", actual.ToString(), $"[{min}, {max}]",
                message ?? $"Expected value in range [{min}, {max}] but got {actual} for '{AutomationId}'.");
        }
        LogAssertPass("ValueInRange", actual.ToString(), $"[{min}, {max}]");
    }

    /// <summary>
    /// Assert percentage is approximately expected (within tolerance).
    /// </summary>
    public void AssertPercentage(double expected, double tolerance = 1.0, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetPercentage();
        if (Math.Abs(actual - expected) > tolerance)
        {
            ThrowAssertionFailed("Percentage", $"{actual:F1}%", $"{expected}%",
                message ?? $"Expected {expected}% but got {actual:F1}% for '{AutomationId}'.");
        }
        LogAssertPass("Percentage", $"{actual:F1}%", $"{expected}%");
    }
}
