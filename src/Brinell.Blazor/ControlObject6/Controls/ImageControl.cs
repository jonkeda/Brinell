using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// Image control for Blazor.
/// Wraps &lt;img&gt; elements.
/// Uses async-only API since Playwright is async.
/// </summary>
public class ImageControl : AsyncClickableControlBase
{
    /// <summary>
    /// Creates a new Image control.
    /// </summary>
    public ImageControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new Image control using TestId.
    /// </summary>
    public ImageControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    #region Async Methods

    /// <summary>
    /// Gets the image source URL.
    /// </summary>
    public virtual async Task<string?> GetSourceAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().GetAttributeAsync("src");
    }

    /// <summary>
    /// Gets the alt text.
    /// </summary>
    public virtual async Task<string?> GetAltTextAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().GetAttributeAsync("alt");
    }

    /// <summary>
    /// Checks if the image has loaded successfully.
    /// </summary>
    public virtual async Task<bool> IsLoadedAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().EvaluateAsync<bool>("img => img.complete && img.naturalWidth > 0");
    }

    /// <summary>
    /// Waits for the image to load.
    /// </summary>
    public virtual async Task<bool> WaitLoadedAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);

        while (DateTime.UtcNow < deadline)
        {
            if (await IsLoadedAsync(timeoutMs, ct) == expected.Value)
                return true;

            await Task.Delay(Context.DefaultPollingIntervalMs, ct);
        }

        return false;
    }

    /// <summary>
    /// Gets the natural width of the image.
    /// </summary>
    public virtual async Task<int> GetNaturalWidthAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().EvaluateAsync<int>("img => img.naturalWidth");
    }

    /// <summary>
    /// Gets the natural height of the image.
    /// </summary>
    public virtual async Task<int> GetNaturalHeightAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().EvaluateAsync<int>("img => img.naturalHeight");
    }

    /// <summary>
    /// Asserts the image source.
    /// </summary>
    public virtual async Task AssertSourceAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetSourceAsync(timeoutMs, ct);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected source '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertSource");
        }
    }

    /// <summary>
    /// Asserts the source contains the expected string.
    /// </summary>
    public virtual async Task AssertSourceContainsAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetSourceAsync(timeoutMs, ct);
        if (actual is null || !actual.Contains(expected, StringComparison.Ordinal))
        {
            throw new AssertionException(
                message ?? $"Expected source to contain '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertSourceContains");
        }
    }

    /// <summary>
    /// Asserts the alt text.
    /// </summary>
    public virtual async Task AssertAltTextAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetAltTextAsync(timeoutMs, ct);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected alt text '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertAltText");
        }
    }

    #endregion
}
