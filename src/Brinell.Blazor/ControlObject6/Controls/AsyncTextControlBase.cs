using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// Base class for text input controls (async).
/// Provides virtual async text entry methods that can be overridden.
/// </summary>
public abstract class AsyncTextControlBase : AsyncClickableControlBase, IAsyncTextControlObject
{
    /// <summary>
    /// Creates a new text control.
    /// </summary>
    protected AsyncTextControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new text control using TestId.
    /// </summary>
    protected AsyncTextControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page)
    {
    }

    #region Focus

    /// <inheritdoc />
    public virtual async Task<bool> IsFocusedAsync(CancellationToken ct = default)
    {
        return await GetLocator().EvaluateAsync<bool>("el => el === document.activeElement");
    }

    /// <inheritdoc />
    public virtual async Task<bool> WaitFocusedAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);

        while (DateTime.UtcNow < deadline)
        {
            if (await IsFocusedAsync(ct) == expected.Value)
                return true;

            await Task.Delay(Context.DefaultPollingIntervalMs, ct);
        }

        return false;
    }

    /// <inheritdoc />
    public virtual async Task CheckFocusedAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        if (!await WaitFocusedAsync(expected, timeoutMs, ct))
        {
            var timeout = timeoutMs ?? DefaultTimeoutMs;
            throw new UITestTimeoutException(
                $"Element {(expected.Value ? "does not have focus" : "still has focus")}",
                Locator.Value,
                timeout,
                "CheckFocused",
                $"Focused={await IsFocusedAsync(ct)}");
        }
    }

    /// <inheritdoc />
    public virtual async Task AssertFocusedAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        await CheckFocusedAsync(expected, timeoutMs, ct);

        var actual = await IsFocusedAsync(ct);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected element to {(expected.Value ? "have focus" : "not have focus")}",
                Locator.Value,
                "AssertFocused");
        }
    }

    /// <inheritdoc />
    public virtual async Task FocusAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("FocusAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        await GetLocator().FocusAsync();
    }

    /// <inheritdoc />
    public virtual async Task BlurAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("BlurAsync()");
        await GetLocator().BlurAsync();
    }

    #endregion

    #region Text Input

    /// <inheritdoc />
    public virtual async Task EnterAsync(string? text, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (text is null) return;

        Log($"EnterAsync(\"{text}\")");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        await GetLocator().ClearAsync();
        await GetLocator().FillAsync(text);
    }

    /// <inheritdoc />
    public virtual async Task ClearAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("ClearAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        await GetLocator().ClearAsync();
    }

    /// <inheritdoc />
    public virtual async Task ClearAndEnterAsync(string? text, int? timeoutMs = null, CancellationToken ct = default)
    {
        Log($"ClearAndEnterAsync(\"{text}\")");
        await ClearAsync(timeoutMs, ct);

        if (text is not null)
        {
            await GetLocator().FillAsync(text);
        }
    }

    /// <inheritdoc />
    public virtual async Task AppendAsync(string? text, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (text is null) return;

        Log($"AppendAsync(\"{text}\")");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        await GetLocator().PressSequentiallyAsync(text);
    }

    #endregion

    #region Read-Only

    /// <inheritdoc />
    public virtual async Task<bool> IsReadOnlyAsync(CancellationToken ct = default)
    {
        var readOnly = await GetLocator().GetAttributeAsync("readonly");
        return readOnly is not null;
    }

    /// <inheritdoc />
    public virtual async Task<bool> WaitReadOnlyAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);

        while (DateTime.UtcNow < deadline)
        {
            if (await IsReadOnlyAsync(ct) == expected.Value)
                return true;

            await Task.Delay(Context.DefaultPollingIntervalMs, ct);
        }

        return false;
    }

    /// <inheritdoc />
    public virtual async Task AssertReadOnlyAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        if (!await WaitReadOnlyAsync(expected, timeoutMs, ct))
        {
            throw new AssertionException(
                message ?? $"Expected element to be {(expected.Value ? "read-only" : "editable")}",
                Locator.Value,
                "AssertReadOnly");
        }
    }

    #endregion

    #region Text Length

    /// <inheritdoc />
    public virtual async Task<int> GetTextLengthAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var text = await GetTextAsync(timeoutMs, ct);
        return text.Length;
    }

    /// <inheritdoc />
    public virtual async Task AssertTextLengthAsync(int? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetTextLengthAsync(timeoutMs, ct);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected text length {expected}, but was {actual}",
                Locator.Value,
                "AssertTextLength");
        }
    }

    #endregion

    /// <inheritdoc />
    public override async Task<string> GetTextAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);

        // For input elements, get the value
        var value = await GetLocator().InputValueAsync();
        return value ?? string.Empty;
    }
}
