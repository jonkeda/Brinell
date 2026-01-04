using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// RadioButton control for Blazor.
/// Wraps &lt;input type="radio"&gt; elements.
/// Uses async-only API since Playwright is async.
/// </summary>
public class RadioButtonControl : AsyncClickableControlBase
{
    /// <summary>
    /// Creates a new RadioButton control.
    /// </summary>
    public RadioButtonControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new RadioButton control using TestId.
    /// </summary>
    public RadioButtonControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    #region Async Methods

    /// <summary>
    /// Checks if the radio button is selected.
    /// </summary>
    public virtual async Task<bool> IsCheckedAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().IsCheckedAsync();
    }

    /// <summary>
    /// Waits for the radio button to be in the expected checked state.
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
    /// Asserts the radio button is in the expected checked state.
    /// </summary>
    public virtual async Task AssertCheckedAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        if (!await WaitCheckedAsync(expected, timeoutMs, ct))
        {
            throw new AssertionException(
                message ?? $"Expected radio button to be {(expected.Value ? "selected" : "not selected")}",
                Locator.Value,
                "AssertChecked");
        }
    }

    /// <summary>
    /// Selects the radio button.
    /// </summary>
    public virtual async Task SelectAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("SelectAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        await GetLocator().CheckAsync();
    }

    /// <summary>
    /// Gets the name attribute of the radio button group.
    /// </summary>
    public virtual async Task<string?> GetGroupNameAsync(CancellationToken ct = default)
    {
        return await GetLocator().GetAttributeAsync("name");
    }

    /// <summary>
    /// Gets the value attribute of the radio button.
    /// </summary>
    public virtual async Task<string?> GetValueAsync(CancellationToken ct = default)
    {
        return await GetLocator().GetAttributeAsync("value");
    }

    #endregion
}
