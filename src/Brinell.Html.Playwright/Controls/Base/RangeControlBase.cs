using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls.Base;

/// <summary>
/// Playwright base class for range controls (input type="range", progress).
/// </summary>
public abstract class RangeControlBase : ControlBase, IRangeControl
{
    protected RangeControlBase(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected RangeControlBase(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected RangeControlBase(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get the current value.
    /// </summary>
    public virtual double GetValue()
    {
        return GetValueAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the current value asynchronously.
    /// </summary>
    public virtual async Task<double> GetValueAsync()
    {
        var locator = GetLocator();
        var valueStr = await locator.InputValueAsync();
        return double.TryParse(valueStr, out var result) ? result : 0;
    }

    /// <summary>
    /// Get the minimum value.
    /// </summary>
    public virtual double GetMinimum()
    {
        return GetMinimumAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the minimum value asynchronously.
    /// </summary>
    public virtual async Task<double> GetMinimumAsync()
    {
        var locator = GetLocator();
        var min = await locator.GetAttributeAsync("min");
        return double.TryParse(min, out var result) ? result : 0;
    }

    /// <summary>
    /// Get the maximum value.
    /// </summary>
    public virtual double GetMaximum()
    {
        return GetMaximumAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the maximum value asynchronously.
    /// </summary>
    public virtual async Task<double> GetMaximumAsync()
    {
        var locator = GetLocator();
        var max = await locator.GetAttributeAsync("max");
        return double.TryParse(max, out var result) ? result : 100;
    }

    /// <summary>
    /// Get the step value.
    /// </summary>
    public virtual double GetStep()
    {
        return GetStepAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the step value asynchronously.
    /// </summary>
    public virtual async Task<double> GetStepAsync()
    {
        var locator = GetLocator();
        var step = await locator.GetAttributeAsync("step");
        return double.TryParse(step, out var result) ? result : 1;
    }

    /// <summary>
    /// Set the value.
    /// </summary>
    public virtual void SetValue(double value)
    {
        SetValueAsync(value).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Set the value asynchronously.
    /// </summary>
    public virtual async Task SetValueAsync(double value)
    {
        LogAction("SetValue", value.ToString());
        var locator = GetLocator();
        await locator.EvaluateAsync($"el => {{ el.value = {value}; el.dispatchEvent(new Event('input')); }}");
    }

    /// <summary>
    /// Increment the value by step.
    /// </summary>
    public virtual void Increment()
    {
        IncrementAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Increment the value by step asynchronously.
    /// </summary>
    public virtual async Task IncrementAsync()
    {
        LogAction("Increment");
        var step = await GetStepAsync();
        var current = await GetValueAsync();
        var max = await GetMaximumAsync();
        await SetValueAsync(Math.Min(current + step, max));
    }

    /// <summary>
    /// Decrement the value by step.
    /// </summary>
    public virtual void Decrement()
    {
        DecrementAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Decrement the value by step asynchronously.
    /// </summary>
    public virtual async Task DecrementAsync()
    {
        LogAction("Decrement");
        var step = await GetStepAsync();
        var current = await GetValueAsync();
        var min = await GetMinimumAsync();
        await SetValueAsync(Math.Max(current - step, min));
    }

    /// <summary>
    /// Get value as percentage (0-100).
    /// </summary>
    public virtual double GetPercentage()
    {
        return GetPercentageAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get value as percentage (0-100) asynchronously.
    /// </summary>
    public virtual async Task<double> GetPercentageAsync()
    {
        var value = await GetValueAsync();
        var min = await GetMinimumAsync();
        var max = await GetMaximumAsync();

        if (max - min == 0) return 0;
        return (value - min) / (max - min) * 100;
    }

    /// <summary>
    /// Wait for value to equal expected.
    /// </summary>
    public virtual bool WaitValue(double expected, double tolerance = 0.001, int? timeoutMs = null)
    {
        Log($"WaitValue({expected})");
        return _context.WaitFor(() => Math.Abs(GetValue() - expected) <= tolerance, timeoutMs,
            $"element '{AutomationId}' value = {expected}");
    }

    /// <summary>
    /// Assert value equals expected (within tolerance).
    /// Captures screenshot on failure.
    /// </summary>
    public virtual void AssertValue(double expected, double tolerance = 0.001, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetValue();
        if (Math.Abs(actual - expected) > tolerance)
        {
            ThrowAssertionFailed("Value", actual.ToString(), expected.ToString(),
                message ?? $"Expected value '{expected}' but got '{actual}' for element '{AutomationId}'.");
        }
        LogAssertPass("Value", actual.ToString(), expected.ToString());
    }

    /// <summary>
    /// Assert value equals expected asynchronously.
    /// </summary>
    public virtual async Task AssertValueAsync(double expected, double tolerance = 0.001, string? message = null)
    {
        await WaitVisibleAsync(expected: true);
        var actual = await GetValueAsync();
        if (Math.Abs(actual - expected) > tolerance)
        {
            ThrowAssertionFailed("Value", actual.ToString(), expected.ToString(),
                message ?? $"Expected value '{expected}' but got '{actual}' for element '{AutomationId}'.");
        }
        LogAssertPass("Value", actual.ToString(), expected.ToString());
    }

    /// <summary>
    /// Wait for visible asynchronously (helper for async assertions).
    /// </summary>
    protected async Task<bool> WaitVisibleAsync(bool expected = true, int? timeoutMs = null)
    {
        return await _context.WaitForAsync(
            async () => await IsVisibleAsync() == expected,
            timeoutMs,
            $"element '{AutomationId}' visible = {expected}");
    }
}
