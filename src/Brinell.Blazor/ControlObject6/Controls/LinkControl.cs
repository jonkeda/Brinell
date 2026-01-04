using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// Link control for Blazor.
/// Wraps &lt;a&gt; elements.
/// </summary>
public class LinkControl : AsyncClickableControlBase
{
    /// <summary>
    /// Creates a new Link control.
    /// </summary>
    public LinkControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new Link control using TestId.
    /// </summary>
    public LinkControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    /// <summary>
    /// Gets the href attribute of the link.
    /// </summary>
    public virtual async Task<string?> GetHrefAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().GetAttributeAsync("href");
    }

    /// <summary>
    /// Gets the target attribute of the link.
    /// </summary>
    public virtual async Task<string?> GetTargetAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().GetAttributeAsync("target");
    }

    /// <summary>
    /// Asserts the href matches the expected value.
    /// </summary>
    public virtual async Task AssertHrefAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetHrefAsync(timeoutMs, ct);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected href '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertHref");
        }
    }

    /// <summary>
    /// Asserts the href contains the expected value.
    /// </summary>
    public virtual async Task AssertHrefContainsAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetHrefAsync(timeoutMs, ct);
        if (actual is null || !actual.Contains(expected, StringComparison.Ordinal))
        {
            throw new AssertionException(
                message ?? $"Expected href to contain '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertHrefContains");
        }
    }
}
