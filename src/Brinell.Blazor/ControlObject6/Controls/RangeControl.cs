using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// Range control for Blazor.
/// Wraps &lt;input type="range"&gt; elements.
/// Uses async-only API since Playwright is async.
/// </summary>
public class RangeControl : AsyncClickableControlBase
{
    /// <summary>
    /// Creates a new Range control.
    /// </summary>
    public RangeControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new Range control using TestId.
    /// </summary>
    public RangeControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    #region Async Methods

    /// <summary>
    /// Gets the current value.
    /// </summary>
    public virtual async Task<double> GetValueAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var value = await GetLocator().InputValueAsync();
        return double.TryParse(value, out var result) ? result : 0;
    }

    /// <summary>
    /// Gets the minimum value.
    /// </summary>
    public virtual async Task<double> GetMinimumAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var min = await GetLocator().GetAttributeAsync("min");
        return double.TryParse(min, out var result) ? result : 0;
    }

    /// <summary>
    /// Gets the maximum value.
    /// </summary>
    public virtual async Task<double> GetMaximumAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var max = await GetLocator().GetAttributeAsync("max");
        return double.TryParse(max, out var result) ? result : 100;
    }

    /// <summary>
    /// Gets the step value.
    /// </summary>
    public virtual async Task<double> GetStepAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var step = await GetLocator().GetAttributeAsync("step");
        return double.TryParse(step, out var result) ? result : 1;
    }

    /// <summary>
    /// Sets the value.
    /// </summary>
    public virtual async Task SetValueAsync(double? value, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (value is null) return;

        Log($"SetValueAsync({value})");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        await GetLocator().FillAsync(value.Value.ToString());
    }

    /// <summary>
    /// Increments the value by the specified number of steps.
    /// </summary>
    public virtual async Task IncrementAsync(int? count = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        var steps = count ?? 1;
        Log($"IncrementAsync({steps})");

        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        var step = await GetStepAsync(timeoutMs, ct);
        var current = await GetValueAsync(timeoutMs, ct);
        var max = await GetMaximumAsync(timeoutMs, ct);
        var newValue = Math.Min(current + (step * steps), max);

        await SetValueAsync(newValue, timeoutMs, ct);
    }

    /// <summary>
    /// Decrements the value by the specified number of steps.
    /// </summary>
    public virtual async Task DecrementAsync(int? count = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        var steps = count ?? 1;
        Log($"DecrementAsync({steps})");

        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        var step = await GetStepAsync(timeoutMs, ct);
        var current = await GetValueAsync(timeoutMs, ct);
        var min = await GetMinimumAsync(timeoutMs, ct);
        var newValue = Math.Max(current - (step * steps), min);

        await SetValueAsync(newValue, timeoutMs, ct);
    }

    /// <summary>
    /// Asserts the value equals the expected value.
    /// </summary>
    public virtual async Task AssertValueAsync(double? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetValueAsync(timeoutMs, ct);
        if (Math.Abs(actual - expected.Value) > 0.001)
        {
            throw new AssertionException(
                message ?? $"Expected value {expected}, but was {actual}",
                Locator.Value,
                "AssertValue");
        }
    }

    /// <summary>
    /// Asserts the value is within the specified range.
    /// </summary>
    public virtual async Task AssertValueInRangeAsync(double? min, double? max, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (min is null && max is null) return;

        var actual = await GetValueAsync(timeoutMs, ct);

        if (min.HasValue && actual < min.Value)
        {
            throw new AssertionException(
                message ?? $"Expected value >= {min}, but was {actual}",
                Locator.Value,
                "AssertValueInRange");
        }

        if (max.HasValue && actual > max.Value)
        {
            throw new AssertionException(
                message ?? $"Expected value <= {max}, but was {actual}",
                Locator.Value,
                "AssertValueInRange");
        }
    }

    #endregion
}
