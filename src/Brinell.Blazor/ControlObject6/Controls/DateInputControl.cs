using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// Date input control for Blazor.
/// Wraps &lt;input type="date"&gt; elements.
/// </summary>
public class DateInputControl : AsyncClickableControlBase
{
    /// <summary>
    /// Creates a new DateInput control.
    /// </summary>
    public DateInputControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new DateInput control using TestId.
    /// </summary>
    public DateInputControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    /// <summary>
    /// Gets the current date value.
    /// </summary>
    public virtual async Task<DateOnly?> GetDateAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var value = await GetLocator().InputValueAsync();
        return DateOnly.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    /// Sets the date value.
    /// </summary>
    public virtual async Task SetDateAsync(DateOnly? date, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (date is null) return;

        Log($"SetDateAsync({date:yyyy-MM-dd})");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        // HTML date inputs expect yyyy-MM-dd format
        await GetLocator().FillAsync(date.Value.ToString("yyyy-MM-dd"));
    }

    /// <summary>
    /// Gets the minimum allowed date.
    /// </summary>
    public virtual async Task<DateOnly?> GetMinDateAsync(CancellationToken ct = default)
    {
        var min = await GetLocator().GetAttributeAsync("min");
        return DateOnly.TryParse(min, out var result) ? result : null;
    }

    /// <summary>
    /// Gets the maximum allowed date.
    /// </summary>
    public virtual async Task<DateOnly?> GetMaxDateAsync(CancellationToken ct = default)
    {
        var max = await GetLocator().GetAttributeAsync("max");
        return DateOnly.TryParse(max, out var result) ? result : null;
    }

    /// <summary>
    /// Asserts the date value.
    /// </summary>
    public virtual async Task AssertDateAsync(DateOnly? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetDateAsync(timeoutMs, ct);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected date {expected:yyyy-MM-dd}, but was {actual:yyyy-MM-dd}",
                Locator.Value,
                "AssertDate");
        }
    }

    /// <summary>
    /// Clears the date value.
    /// </summary>
    public virtual async Task ClearAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("ClearAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        await GetLocator().ClearAsync();
    }
}
