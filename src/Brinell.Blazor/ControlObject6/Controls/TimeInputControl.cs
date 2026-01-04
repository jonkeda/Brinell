using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// Time input control for Blazor.
/// Wraps &lt;input type="time"&gt; elements.
/// </summary>
public class TimeInputControl : AsyncClickableControlBase
{
    /// <summary>
    /// Creates a new TimeInput control.
    /// </summary>
    public TimeInputControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new TimeInput control using TestId.
    /// </summary>
    public TimeInputControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    /// <summary>
    /// Gets the current time value.
    /// </summary>
    public virtual async Task<TimeOnly?> GetTimeAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var value = await GetLocator().InputValueAsync();
        return TimeOnly.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    /// Sets the time value.
    /// </summary>
    public virtual async Task SetTimeAsync(TimeOnly? time, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (time is null) return;

        Log($"SetTimeAsync({time:HH:mm})");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        // HTML time inputs expect HH:mm or HH:mm:ss format
        await GetLocator().FillAsync(time.Value.ToString("HH:mm"));
    }

    /// <summary>
    /// Gets the minimum allowed time.
    /// </summary>
    public virtual async Task<TimeOnly?> GetMinTimeAsync(CancellationToken ct = default)
    {
        var min = await GetLocator().GetAttributeAsync("min");
        return TimeOnly.TryParse(min, out var result) ? result : null;
    }

    /// <summary>
    /// Gets the maximum allowed time.
    /// </summary>
    public virtual async Task<TimeOnly?> GetMaxTimeAsync(CancellationToken ct = default)
    {
        var max = await GetLocator().GetAttributeAsync("max");
        return TimeOnly.TryParse(max, out var result) ? result : null;
    }

    /// <summary>
    /// Asserts the time value.
    /// </summary>
    public virtual async Task AssertTimeAsync(TimeOnly? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetTimeAsync(timeoutMs, ct);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected time {expected:HH:mm}, but was {actual:HH:mm}",
                Locator.Value,
                "AssertTime");
        }
    }

    /// <summary>
    /// Clears the time value.
    /// </summary>
    public virtual async Task ClearAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("ClearAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        await GetLocator().ClearAsync();
    }
}
