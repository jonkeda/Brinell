using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// List control for Blazor.
/// Wraps &lt;ul&gt; and &lt;ol&gt; elements.
/// </summary>
public class ListControl : AsyncControlObjectBase
{
    /// <summary>
    /// Creates a new List control.
    /// </summary>
    public ListControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new List control using TestId.
    /// </summary>
    public ListControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    /// <summary>
    /// Gets the number of items in the list.
    /// </summary>
    public virtual async Task<int> GetItemCountAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().Locator("li").CountAsync();
    }

    /// <summary>
    /// Gets all item texts.
    /// </summary>
    public virtual async Task<IReadOnlyList<string>> GetItemsAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var items = GetLocator().Locator("li");
        var count = await items.CountAsync();
        var texts = new List<string>();

        for (int i = 0; i < count; i++)
        {
            texts.Add(await items.Nth(i).InnerTextAsync());
        }

        return texts;
    }

    /// <summary>
    /// Gets the item text at the specified index (0-based).
    /// </summary>
    public virtual async Task<string> GetItemTextAsync(int index, int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var item = GetLocator().Locator($"li:nth-child({index + 1})");
        return await item.InnerTextAsync();
    }

    /// <summary>
    /// Clicks an item at the specified index (0-based).
    /// </summary>
    public virtual async Task ClickItemAsync(int index, int? timeoutMs = null, CancellationToken ct = default)
    {
        Log($"ClickItemAsync({index})");
        await CheckVisibleAsync(true, timeoutMs, ct);

        var item = GetLocator().Locator($"li:nth-child({index + 1})");
        await item.ClickAsync();
    }

    /// <summary>
    /// Clicks an item with the specified text.
    /// </summary>
    public virtual async Task ClickItemByTextAsync(string? text, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (text is null) return;

        Log($"ClickItemByTextAsync(\"{text}\")");
        await CheckVisibleAsync(true, timeoutMs, ct);

        var item = GetLocator().Locator("li").Filter(new() { HasText = text });
        await item.ClickAsync();
    }

    /// <summary>
    /// Checks if the list has an item with the specified text.
    /// </summary>
    public virtual async Task<bool> HasItemAsync(string? text, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (text is null) return false;

        await CheckExistsAsync(true, timeoutMs, ct);
        var items = await GetItemsAsync(timeoutMs, ct);
        return items.Contains(text);
    }

    /// <summary>
    /// Asserts the item count.
    /// </summary>
    public virtual async Task AssertItemCountAsync(int? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetItemCountAsync(timeoutMs, ct);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected item count {expected}, but was {actual}",
                Locator.Value,
                "AssertItemCount");
        }
    }

    /// <summary>
    /// Asserts the list has an item with the specified text.
    /// </summary>
    public virtual async Task AssertHasItemAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var hasItem = await HasItemAsync(expected, timeoutMs, ct);
        if (!hasItem)
        {
            throw new AssertionException(
                message ?? $"Expected to have item '{expected}'",
                Locator.Value,
                "AssertHasItem");
        }
    }

    /// <summary>
    /// Asserts the item text at the specified index.
    /// </summary>
    public virtual async Task AssertItemTextAsync(int index, string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetItemTextAsync(index, timeoutMs, ct);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected item[{index}] text '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertItemText");
        }
    }
}
