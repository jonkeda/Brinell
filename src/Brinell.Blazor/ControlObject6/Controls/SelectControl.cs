using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Microsoft.Playwright;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// Select control for Blazor.
/// Wraps &lt;select&gt; elements.
/// Uses async-only API since Playwright is async.
/// </summary>
public class SelectControl : AsyncClickableControlBase
{
    /// <summary>
    /// Creates a new Select control.
    /// </summary>
    public SelectControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new Select control using TestId.
    /// </summary>
    public SelectControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    #region Async Methods

    /// <summary>
    /// Gets all items in the select.
    /// </summary>
    public virtual async Task<IReadOnlyList<string>> GetItemsAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var options = GetLocator().Locator("option");
        var count = await options.CountAsync();
        var items = new List<string>();

        for (int i = 0; i < count; i++)
        {
            var text = await options.Nth(i).InnerTextAsync();
            items.Add(text);
        }

        return items;
    }

    /// <summary>
    /// Gets the number of items in the select.
    /// </summary>
    public virtual async Task<int> GetItemCountAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().Locator("option").CountAsync();
    }

    /// <summary>
    /// Gets the selected item text.
    /// </summary>
    public virtual async Task<string?> GetSelectedItemAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var selectedOption = GetLocator().Locator("option:checked");
        if (await selectedOption.CountAsync() == 0)
            return null;

        return await selectedOption.InnerTextAsync();
    }

    /// <summary>
    /// Gets the selected item index.
    /// </summary>
    public virtual async Task<int> GetSelectedIndexAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var index = await GetLocator().EvaluateAsync<int>("el => el.selectedIndex");
        return index;
    }

    /// <summary>
    /// Selects an item by its text.
    /// </summary>
    public virtual async Task SelectItemAsync(string? item, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (item is null) return;

        Log($"SelectItemAsync(\"{item}\")");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        await GetLocator().SelectOptionAsync(new Microsoft.Playwright.SelectOptionValue { Label = item });
    }

    /// <summary>
    /// Selects an item by its index.
    /// </summary>
    public virtual async Task SelectItemByIndexAsync(int? index, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (index is null) return;

        Log($"SelectItemByIndexAsync({index})");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        await GetLocator().SelectOptionAsync(new Microsoft.Playwright.SelectOptionValue { Index = index.Value });
    }

    /// <summary>
    /// Selects an item by its value.
    /// </summary>
    public virtual async Task SelectItemByValueAsync(string? value, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (value is null) return;

        Log($"SelectItemByValueAsync(\"{value}\")");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);

        await GetLocator().SelectOptionAsync(value);
    }

    /// <summary>
    /// Asserts the selected item text.
    /// </summary>
    public virtual async Task AssertSelectedItemAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetSelectedItemAsync(timeoutMs, ct);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected selected item '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertSelectedItem");
        }
    }

    /// <summary>
    /// Asserts the selected item index.
    /// </summary>
    public virtual async Task AssertSelectedIndexAsync(int? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetSelectedIndexAsync(timeoutMs, ct);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected selected index {expected}, but was {actual}",
                Locator.Value,
                "AssertSelectedIndex");
        }
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
    /// Asserts the select has the specified item.
    /// </summary>
    public virtual async Task AssertHasItemAsync(string? item, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (item is null) return;

        var items = await GetItemsAsync(timeoutMs, ct);
        if (!items.Contains(item))
        {
            throw new AssertionException(
                message ?? $"Expected to have item '{item}'",
                Locator.Value,
                "AssertHasItem");
        }
    }

    #endregion
}
