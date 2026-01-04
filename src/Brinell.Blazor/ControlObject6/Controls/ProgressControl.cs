using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// Progress control for Blazor.
/// Wraps &lt;progress&gt; elements.
/// Uses async-only API since Playwright is async.
/// </summary>
public class ProgressControl : AsyncControlObjectBase
{
    /// <summary>
    /// Creates a new Progress control.
    /// </summary>
    public ProgressControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new Progress control using TestId.
    /// </summary>
    public ProgressControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    #region Async Methods

    /// <summary>
    /// Gets the current progress value (0-1).
    /// </summary>
    public virtual async Task<double> GetProgressAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);

        var value = await GetLocator().GetAttributeAsync("value");
        var max = await GetLocator().GetAttributeAsync("max");

        if (!double.TryParse(value, out var valueNum))
            return 0;

        var maxNum = double.TryParse(max, out var m) ? m : 1;
        return maxNum > 0 ? valueNum / maxNum : 0;
    }

    /// <summary>
    /// Gets whether the progress is indeterminate.
    /// </summary>
    public virtual async Task<bool> IsIndeterminateAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);

        // A progress element is indeterminate when it has no value attribute
        var value = await GetLocator().GetAttributeAsync("value");
        return value is null;
    }

    /// <summary>
    /// Waits for the progress to reach the expected value.
    /// </summary>
    public virtual async Task<bool> WaitProgressAsync(double? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);

        while (DateTime.UtcNow < deadline)
        {
            var actual = await GetProgressAsync(timeoutMs, ct);
            if (Math.Abs(actual - expected.Value) < 0.01)
                return true;

            await Task.Delay(Context.DefaultPollingIntervalMs, ct);
        }

        return false;
    }

    /// <summary>
    /// Asserts the progress value.
    /// </summary>
    public virtual async Task AssertProgressAsync(double? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetProgressAsync(timeoutMs, ct);
        if (Math.Abs(actual - expected.Value) > 0.01)
        {
            throw new AssertionException(
                message ?? $"Expected progress {expected:P0}, but was {actual:P0}",
                Locator.Value,
                "AssertProgress");
        }
    }

    /// <summary>
    /// Asserts the progress is within the specified range.
    /// </summary>
    public virtual async Task AssertProgressInRangeAsync(double? min, double? max, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (min is null && max is null) return;

        var actual = await GetProgressAsync(timeoutMs, ct);

        if (min.HasValue && actual < min.Value)
        {
            throw new AssertionException(
                message ?? $"Expected progress >= {min:P0}, but was {actual:P0}",
                Locator.Value,
                "AssertProgressInRange");
        }

        if (max.HasValue && actual > max.Value)
        {
            throw new AssertionException(
                message ?? $"Expected progress <= {max:P0}, but was {actual:P0}",
                Locator.Value,
                "AssertProgressInRange");
        }
    }

    /// <summary>
    /// Waits for the progress to complete (reach 1.0 or 100%).
    /// </summary>
    public virtual async Task WaitCompleteAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await WaitProgressAsync(1.0, timeoutMs, ct);
    }

    #endregion
}
