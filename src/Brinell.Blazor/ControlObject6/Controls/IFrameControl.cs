using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// IFrame control for Blazor.
/// Wraps &lt;iframe&gt; elements for embedded content.
/// </summary>
public class IFrameControl : AsyncControlObjectBase
{
    /// <summary>
    /// Creates a new IFrame control.
    /// </summary>
    public IFrameControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new IFrame control using TestId.
    /// </summary>
    public IFrameControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    /// <summary>
    /// Gets the iframe source URL.
    /// </summary>
    public virtual async Task<string?> GetSourceAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().GetAttributeAsync("src");
    }

    /// <summary>
    /// Gets the iframe title.
    /// </summary>
    public virtual async Task<string?> GetTitleAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().GetAttributeAsync("title");
    }

    /// <summary>
    /// Gets the iframe name.
    /// </summary>
    public virtual async Task<string?> GetNameAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().GetAttributeAsync("name");
    }

    /// <summary>
    /// Gets the frame locator for interacting with iframe content.
    /// </summary>
    public virtual async Task<Microsoft.Playwright.IFrameLocator> GetFrameLocatorAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return GetLocator().FrameLocator(".");
    }

    /// <summary>
    /// Clicks an element inside the iframe.
    /// </summary>
    public virtual async Task ClickInsideAsync(string selector, int? timeoutMs = null, CancellationToken ct = default)
    {
        Log($"ClickInsideAsync(\"{selector}\")");
        await CheckExistsAsync(true, timeoutMs, ct);

        var frame = GetLocator().FrameLocator(".");
        await frame.Locator(selector).ClickAsync();
    }

    /// <summary>
    /// Fills a text field inside the iframe.
    /// </summary>
    public virtual async Task FillInsideAsync(string selector, string? text, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (text is null) return;

        Log($"FillInsideAsync(\"{selector}\", \"{text}\")");
        await CheckExistsAsync(true, timeoutMs, ct);

        var frame = GetLocator().FrameLocator(".");
        await frame.Locator(selector).FillAsync(text);
    }

    /// <summary>
    /// Gets text content from an element inside the iframe.
    /// </summary>
    public virtual async Task<string?> GetTextInsideAsync(string selector, int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);

        var frame = GetLocator().FrameLocator(".");
        return await frame.Locator(selector).InnerTextAsync();
    }

    /// <summary>
    /// Checks if an element exists inside the iframe.
    /// </summary>
    public virtual async Task<bool> ElementExistsInsideAsync(string selector, int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);

        var frame = GetLocator().FrameLocator(".");
        return await frame.Locator(selector).CountAsync() > 0;
    }

    /// <summary>
    /// Waits for an element inside the iframe to be visible.
    /// </summary>
    public virtual async Task WaitForElementInsideAsync(string selector, int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var frame = GetLocator().FrameLocator(".");
        await frame.Locator(selector).WaitForAsync(new() { Timeout = timeout });
    }

    /// <summary>
    /// Asserts the iframe source.
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
    /// Asserts an element exists inside the iframe.
    /// </summary>
    public virtual async Task AssertElementExistsInsideAsync(string selector, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (!await ElementExistsInsideAsync(selector, timeoutMs, ct))
        {
            throw new AssertionException(
                message ?? $"Expected element '{selector}' to exist inside iframe",
                Locator.Value,
                "AssertElementExistsInside");
        }
    }
}
