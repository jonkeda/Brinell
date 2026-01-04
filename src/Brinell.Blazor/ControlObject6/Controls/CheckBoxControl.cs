using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// CheckBox control for Blazor.
/// Wraps &lt;input type="checkbox"&gt; elements.
/// Uses async-only API since Playwright is async.
/// </summary>
public class CheckBoxControl : AsyncClickableControlBase
{
    /// <summary>
    /// Creates a new CheckBox control.
    /// </summary>
    public CheckBoxControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new CheckBox control using TestId.
    /// </summary>
    public CheckBoxControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    #region Async Toggle Methods

    /// <summary>
    /// Checks if the checkbox is checked.
    /// </summary>
    public virtual async Task<bool> IsCheckedAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().IsCheckedAsync();
    }

    /// <summary>
    /// Waits for the checkbox to be in the expected checked state.
    /// </summary>
    public virtual async Task<bool> WaitCheckedAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);

        while (DateTime.UtcNow < deadline)
        {
            if (await IsCheckedAsync(timeoutMs, ct) == expected.Value)
                return true;

            await Task.Delay(Context.DefaultPollingIntervalMs, ct);
        }

        return false;
    }

    /// <summary>
    /// Checks the state of the checkbox with timeout.
    /// </summary>
    public virtual async Task CheckStateAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        if (!await WaitCheckedAsync(expected, timeoutMs, ct))
        {
            var timeout = timeoutMs ?? DefaultTimeoutMs;
            throw new UITestTimeoutException(
                $"Checkbox {(expected.Value ? "is not checked" : "is still checked")}",
                Locator.Value,
                timeout,
                "CheckState",
                $"Checked={await IsCheckedAsync(timeoutMs, ct)}");
        }
    }

    /// <summary>
    /// Asserts the checkbox is in the expected checked state.
    /// </summary>
    public virtual async Task AssertCheckedAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        await CheckStateAsync(expected, timeoutMs, ct);

        var actual = await IsCheckedAsync(timeoutMs, ct);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected checkbox to be {(expected.Value ? "checked" : "unchecked")}",
                Locator.Value,
                "AssertChecked");
        }
    }

    /// <summary>
    /// Sets the checkbox to the specified checked state.
    /// </summary>
    public virtual async Task SetCheckedAsync(bool? value, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (value is null) return;

        Log($"SetCheckedAsync({value})");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        await GetLocator().SetCheckedAsync(value.Value);
    }

    /// <summary>
    /// Checks the checkbox (sets to true).
    /// </summary>
    public virtual async Task CheckAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("CheckAsync()");
        await SetCheckedAsync(true, timeoutMs, ct);
    }

    /// <summary>
    /// Unchecks the checkbox (sets to false).
    /// </summary>
    public virtual async Task UncheckAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("UncheckAsync()");
        await SetCheckedAsync(false, timeoutMs, ct);
    }

    /// <summary>
    /// Toggles the checkbox state.
    /// </summary>
    public virtual async Task ToggleAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("ToggleAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        var current = await IsCheckedAsync(timeoutMs, ct);
        await SetCheckedAsync(!current, timeoutMs, ct);
    }

    #endregion
}
