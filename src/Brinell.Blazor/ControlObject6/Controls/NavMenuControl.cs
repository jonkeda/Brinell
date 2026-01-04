using Brinell.Blazor.ControlObject6.Context;
using Brinell.Blazor.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;

namespace Brinell.Blazor.ControlObject6.Controls;

/// <summary>
/// Navigation menu control for Blazor.
/// Wraps &lt;nav&gt; elements with navigation links.
/// </summary>
public class NavMenuControl : AsyncControlObjectBase
{
    /// <summary>
    /// Creates a new NavMenu control.
    /// </summary>
    public NavMenuControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new NavMenu control using TestId.
    /// </summary>
    public NavMenuControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page)
    {
    }

    /// <summary>
    /// Gets the number of navigation items.
    /// </summary>
    public virtual async Task<int> GetItemCountAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        return await GetLocator().Locator("a, .nav-link, [role='menuitem']").CountAsync();
    }

    /// <summary>
    /// Gets all navigation item texts.
    /// </summary>
    public virtual async Task<IReadOnlyList<string>> GetItemsAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var items = GetLocator().Locator("a, .nav-link, [role='menuitem']");
        var count = await items.CountAsync();
        var texts = new List<string>();

        for (int i = 0; i < count; i++)
        {
            texts.Add(await items.Nth(i).InnerTextAsync());
        }

        return texts;
    }

    /// <summary>
    /// Gets the currently active navigation item text.
    /// </summary>
    public virtual async Task<string?> GetActiveItemAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        await CheckExistsAsync(true, timeoutMs, ct);
        var activeItem = GetLocator().Locator(".active, [aria-current='page'], [aria-current='true']").First;

        if (await activeItem.CountAsync() == 0)
            return null;

        return await activeItem.InnerTextAsync();
    }

    /// <summary>
    /// Navigates to a menu item by text.
    /// </summary>
    public virtual async Task NavigateToAsync(string? itemText, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (itemText is null) return;

        Log($"NavigateToAsync(\"{itemText}\")");
        await CheckVisibleAsync(true, timeoutMs, ct);

        var item = GetLocator().Locator("a, .nav-link, [role='menuitem']").Filter(new() { HasText = itemText });
        await item.ClickAsync();
    }

    /// <summary>
    /// Navigates to a menu item by index (0-based).
    /// </summary>
    public virtual async Task NavigateToIndexAsync(int index, int? timeoutMs = null, CancellationToken ct = default)
    {
        Log($"NavigateToIndexAsync({index})");
        await CheckVisibleAsync(true, timeoutMs, ct);

        var items = GetLocator().Locator("a, .nav-link, [role='menuitem']");
        await items.Nth(index).ClickAsync();
    }

    /// <summary>
    /// Gets the href for a navigation item by text.
    /// </summary>
    public virtual async Task<string?> GetItemHrefAsync(string? itemText, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (itemText is null) return null;

        await CheckExistsAsync(true, timeoutMs, ct);
        var item = GetLocator().Locator("a, .nav-link").Filter(new() { HasText = itemText });
        return await item.GetAttributeAsync("href");
    }

    /// <summary>
    /// Checks if a menu item exists.
    /// </summary>
    public virtual async Task<bool> HasItemAsync(string? itemText, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (itemText is null) return false;

        await CheckExistsAsync(true, timeoutMs, ct);
        var items = await GetItemsAsync(timeoutMs, ct);
        return items.Contains(itemText);
    }

    /// <summary>
    /// Checks if a menu item is active.
    /// </summary>
    public virtual async Task<bool> IsActiveAsync(string? itemText, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (itemText is null) return false;

        var activeItem = await GetActiveItemAsync(timeoutMs, ct);
        return activeItem == itemText;
    }

    /// <summary>
    /// Asserts the active navigation item.
    /// </summary>
    public virtual async Task AssertActiveItemAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        var actual = await GetActiveItemAsync(timeoutMs, ct);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected active item '{expected}', but was '{actual}'",
                Locator.Value,
                "AssertActiveItem");
        }
    }

    /// <summary>
    /// Asserts the menu has an item.
    /// </summary>
    public virtual async Task AssertHasItemAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;

        if (!await HasItemAsync(expected, timeoutMs, ct))
        {
            throw new AssertionException(
                message ?? $"Expected to have menu item '{expected}'",
                Locator.Value,
                "AssertHasItem");
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
}
