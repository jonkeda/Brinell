using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Html.Playwright.Controls.Base;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls;

/// <summary>
/// Playwright progress element control wrapper.
/// Works with &lt;progress&gt; elements.
/// </summary>
public class ProgressControl : RangeControlBase
{
    public ProgressControl(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public ProgressControl(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    public ProgressControl(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the current value from the value attribute.
    /// Progress elements use value attribute, not input value.
    /// </summary>
    public override double GetValue()
    {
        return GetValueAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the current value asynchronously.
    /// </summary>
    public override async Task<double> GetValueAsync()
    {
        var locator = GetLocator();
        var valueStr = await locator.GetAttributeAsync("value");
        return double.TryParse(valueStr, out var result) ? result : 0;
    }

    /// <summary>
    /// Get the minimum value (always 0 for progress elements).
    /// </summary>
    public override double GetMinimum()
    {
        return 0;
    }

    /// <summary>
    /// Get the minimum value asynchronously.
    /// </summary>
    public override Task<double> GetMinimumAsync()
    {
        return Task.FromResult(0.0);
    }

    /// <summary>
    /// Get the maximum value (default 1 for progress elements).
    /// </summary>
    public override double GetMaximum()
    {
        return GetMaximumAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the maximum value asynchronously.
    /// </summary>
    public override async Task<double> GetMaximumAsync()
    {
        var locator = GetLocator();
        var max = await locator.GetAttributeAsync("max");
        return double.TryParse(max, out var result) ? result : 1;
    }

    /// <summary>
    /// Progress is always "enabled" (visible = enabled).
    /// </summary>
    public override bool IsEnabled()
    {
        return IsVisible();
    }

    /// <summary>
    /// Progress is always "enabled" asynchronously.
    /// </summary>
    public override async Task<bool> IsEnabledAsync()
    {
        return await IsVisibleAsync();
    }

    /// <summary>
    /// Get current value as percentage string (e.g., "75%").
    /// </summary>
    public override string GetText()
    {
        var percentage = GetPercentage();
        return $"{percentage:F0}%";
    }

    /// <summary>
    /// Get current value as percentage string asynchronously.
    /// </summary>
    public override async Task<string> GetTextAsync()
    {
        var percentage = await GetPercentageAsync();
        return $"{percentage:F0}%";
    }

    /// <summary>
    /// Check if this is an indeterminate progress (no value attribute).
    /// </summary>
    public bool IsIndeterminate()
    {
        return IsIndeterminateAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Check if this is an indeterminate progress asynchronously.
    /// </summary>
    public async Task<bool> IsIndeterminateAsync()
    {
        var locator = GetLocator();
        var value = await locator.GetAttributeAsync("value");
        return string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// Wait for progress to complete (reach maximum value).
    /// </summary>
    public bool WaitForComplete(int? timeoutMs = null)
    {
        Log("WaitForComplete()");
        return _context.WaitFor(() =>
        {
            var value = GetValue();
            var max = GetMaximum();
            return Math.Abs(value - max) < 0.001;
        }, timeoutMs, $"progress '{AutomationId}' complete");
    }

    /// <summary>
    /// Wait for progress to complete asynchronously.
    /// </summary>
    public async Task<bool> WaitForCompleteAsync(int? timeoutMs = null)
    {
        Log("WaitForCompleteAsync()");
        return await _context.WaitForAsync(async () =>
        {
            var value = await GetValueAsync();
            var max = await GetMaximumAsync();
            return Math.Abs(value - max) < 0.001;
        }, timeoutMs, $"progress '{AutomationId}' complete");
    }

    /// <summary>
    /// Assert progress is complete.
    /// </summary>
    public void AssertComplete(string? message = null)
    {
        CheckVisible(expected: true);
        var value = GetValue();
        var max = GetMaximum();
        if (Math.Abs(value - max) > 0.001)
        {
            ThrowAssertionFailed("Complete", $"{value}", $"{max}",
                message ?? $"Expected progress '{AutomationId}' to be complete but value is {value}/{max}.");
        }
        LogAssertPass("Complete", max.ToString(), max.ToString());
    }

    /// <summary>
    /// Assert progress percentage.
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

    /// <summary>
    /// Assert progress is indeterminate.
    /// </summary>
    public void AssertIndeterminate(string? message = null)
    {
        CheckVisible(expected: true);
        if (!IsIndeterminate())
        {
            var value = GetValue();
            ThrowAssertionFailed("Indeterminate", $"value={value}", "indeterminate",
                message ?? $"Expected progress '{AutomationId}' to be indeterminate but has value {value}.");
        }
        LogAssertPass("Indeterminate", "true", "true");
    }

    /// <summary>
    /// Setting value is not supported for progress elements.
    /// </summary>
    public override void SetValue(double value)
    {
        throw new NotSupportedException("Cannot set value on a progress element. Progress values are read-only.");
    }

    /// <summary>
    /// Setting value is not supported for progress elements.
    /// </summary>
    public override Task SetValueAsync(double value)
    {
        throw new NotSupportedException("Cannot set value on a progress element. Progress values are read-only.");
    }

    /// <summary>
    /// Increment is not supported for progress elements.
    /// </summary>
    public override void Increment()
    {
        throw new NotSupportedException("Cannot increment a progress element. Progress values are read-only.");
    }

    /// <summary>
    /// Decrement is not supported for progress elements.
    /// </summary>
    public override void Decrement()
    {
        throw new NotSupportedException("Cannot decrement a progress element. Progress values are read-only.");
    }
}
