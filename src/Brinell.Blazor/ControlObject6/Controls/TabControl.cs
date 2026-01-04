using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// Tab control for Blazor.
/// Wraps tab-based navigation components.
/// </summary>
public class TabControl : AsyncControlObjectBase
{
    /// <summary>
    /// Creates a new Tab control.
    /// </summary>
    public TabControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new Tab control using TestId.
    /// </summary>
    public TabControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    /// <summary>
    /// Gets the number of tabs.
    /// </summary>
    public virtual async Task<int> GetTabCountAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        // Look for common tab patterns: role="tab" or nav items
        var tabs = GetLocator().Locator("[role='tab'], .nav-link, .tab-link, [data-tab]");
        return await tabs.CountAsync();
    }

    /// <summary>
    /// Gets all tab titles.
    /// </summary>
    public virtual async Task<IReadOnlyList<string>> GetTabsAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var tabs = GetLocator().Locator("[role='tab'], .nav-link, .tab-link, [data-tab]");
        var count = await tabs.CountAsync();
        var titles = new List<string>();

        for (int i = 0; i < count; i++)
        {
            titles.Add(await tabs.Nth(i).InnerTextAsync());
        }

        return titles;
    }

    /// <summary>
    /// Gets the currently selected tab index (0-based).
    /// </summary>
    public virtual async Task<int> GetSelectedIndexAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var tabs = GetLocator().Locator("[role='tab'], .nav-link, .tab-link, [data-tab]");
        var count = await tabs.CountAsync();

        for (int i = 0; i < count; i++)
        {
            var tab = tabs.Nth(i);
            var ariaSelected = await tab.GetAttributeAsync("aria-selected");
            var hasActive = await tab.EvaluateAsync<bool>("el => el.classList.contains('active')");

            if (ariaSelected == "true" || hasActive)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Gets the currently selected tab title.
    /// </summary>
    public virtual async Task<string?> GetSelectedTabAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var index = await GetSelectedIndexAsync(timeoutMs, ct);
        if (index < 0) return null;

        var tabs = await GetTabsAsync(timeoutMs, ct);
        return index < tabs.Count ? tabs[index] : null;
    }

    /// <summary>
    /// Selects a tab by index (0-based).
    /// </summary>
    public virtual async Task SelectTabAsync(int index, int? timeoutMs = null, CancellationToken ct = default)
    {
        Log($"SelectTabAsync({index})");
        await CheckVisibleAsync(true, timeoutMs, ct);

        var tabs = GetLocator().Locator("[role='tab'], .nav-link, .tab-link, [data-tab]");
        await tabs.Nth(index).ClickAsync();
    }

    /// <summary>
    /// Selects a tab by title text.
    /// </summary>
    public virtual async Task SelectTabByTextAsync(string? title, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (title is null) return;

        Log($"SelectTabByTextAsync(\"{title}\")");
        await CheckVisibleAsync(true, timeoutMs, ct);

        var tabs = GetLocator().Locator("[role='tab'], .nav-link, .tab-link, [data-tab]");
        var tab = tabs.Filter(new() { HasText = title });
        await tab.ClickAsync();
    }

    /// <summary>
    /// Waits for a specific tab to be selected.
    /// </summary>
    public virtual async Task<bool> WaitSelectedAsync(int? expectedIndex, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expectedIndex is null) return true;

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);

        while (DateTime.UtcNow < deadline)
        {
            if (await GetSelectedIndexAsync(timeoutMs, ct) == expectedIndex.Value)
                return true;

            await Task.Delay(Context.DefaultPollingIntervalMs, ct);
        }

        return false;
    }

    /// <summary>
    /// Asserts the selected tab index.
    /// </summary>
    public virtual async Task AssertSelectedIndexAsync(int? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetSelectedIndexAsync(timeoutMs, ct);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected selected tab index {expected}, but was {actual}",
                Locator.Value,
                "AssertSelectedIndex");
        }
    }

    /// <summary>
    /// Asserts the selected tab title.
    /// </summary>
    public virtual async Task AssertSelectedTabAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetSelectedTabAsync(timeoutMs, ct);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected selected tab '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertSelectedTab");
        }
    }

    /// <summary>
    /// Asserts the tab count.
    /// </summary>
    public virtual async Task AssertTabCountAsync(int? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetTabCountAsync(timeoutMs, ct);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected tab count {expected}, but was {actual}",
                Locator.Value,
                "AssertTabCount");
        }
    }
}
