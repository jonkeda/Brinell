using Microsoft.Playwright;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Playwright.Infrastructure;

namespace Brinell.Html.Playwright.Controls.Base;

/// <summary>
/// Playwright base class for controls that contain collections of items.
/// Used for lists (ul/ol), tables, grids, and other repeated item structures.
/// Playwright locators have built-in retry logic, so stale element handling is automatic.
/// </summary>
public abstract class ItemsControlBase : ControlBase, IItemsControl
{
    /// <summary>
    /// CSS selector for finding individual items within the container.
    /// Override this to specify how to find items (e.g., "li", "tr", ".item").
    /// </summary>
    protected abstract string ItemSelector { get; }

    protected ItemsControlBase(PlaywrightTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected ItemsControlBase(PlaywrightTestContext context, IPageObject? page, ILocator? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected ItemsControlBase(PlaywrightTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get locator for all items within this control.
    /// </summary>
    protected virtual ILocator GetItemsLocator()
    {
        var containerLocator = GetLocator();
        return containerLocator.Locator(ItemSelector);
    }

    /// <summary>
    /// Get all item locators.
    /// </summary>
    protected virtual async Task<IReadOnlyList<ILocator>> FindItemsAsync()
    {
        var itemsLocator = GetItemsLocator();
        var count = await itemsLocator.CountAsync();
        var items = new List<ILocator>(count);
        for (int i = 0; i < count; i++)
        {
            items.Add(itemsLocator.Nth(i));
        }
        return items;
    }

    /// <summary>
    /// Get the count of items.
    /// </summary>
    public virtual int GetItemCount()
    {
        return GetItemsLocator().CountAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get the count of items asynchronously.
    /// </summary>
    public virtual async Task<int> GetItemCountAsync()
    {
        return await GetItemsLocator().CountAsync();
    }

    /// <summary>
    /// Get item text at index.
    /// </summary>
    public virtual string GetItemText(int index)
    {
        return GetItemTextAsync(index).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get item text at index asynchronously.
    /// </summary>
    public virtual async Task<string> GetItemTextAsync(int index)
    {
        var itemsLocator = GetItemsLocator();
        var count = await itemsLocator.CountAsync();
        if (index < 0 || index >= count)
            return string.Empty;
        
        return await itemsLocator.Nth(index).TextContentAsync() ?? string.Empty;
    }

    /// <summary>
    /// Click an item by index.
    /// </summary>
    public virtual void ClickItem(int index)
    {
        ClickItemAsync(index).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Click an item by index asynchronously.
    /// </summary>
    public virtual async Task ClickItemAsync(int index)
    {
        LogAction("ClickItem", index.ToString());
        var itemsLocator = GetItemsLocator();
        var count = await itemsLocator.CountAsync();
        if (index < 0 || index >= count)
            throw new InvalidOperationException($"Item index {index} is out of range (0-{count - 1}).");
        
        await itemsLocator.Nth(index).ClickAsync();
    }

    /// <summary>
    /// Click an item by text.
    /// </summary>
    public virtual void ClickItem(string text)
    {
        ClickItemAsync(text).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Click an item by text asynchronously.
    /// </summary>
    public virtual async Task ClickItemAsync(string text)
    {
        LogAction("ClickItem", text);
        var itemsLocator = GetItemsLocator();
        var item = itemsLocator.Filter(new LocatorFilterOptions { HasText = text }).First;
        
        var count = await item.CountAsync();
        if (count == 0)
            throw new InvalidOperationException($"Item with text '{text}' not found.");
        
        await item.ClickAsync();
    }

    /// <summary>
    /// Check if an item with the specified text exists.
    /// </summary>
    public virtual bool HasItem(string text)
    {
        return HasItemAsync(text).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Check if an item with the specified text exists asynchronously.
    /// </summary>
    public virtual async Task<bool> HasItemAsync(string text)
    {
        var itemsLocator = GetItemsLocator();
        var item = itemsLocator.Filter(new LocatorFilterOptions { HasText = text });
        return await item.CountAsync() > 0;
    }

    /// <summary>
    /// Get all item texts.
    /// </summary>
    public virtual IReadOnlyList<string> GetItemTexts()
    {
        return GetItemTextsAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Get all item texts asynchronously.
    /// </summary>
    public virtual async Task<IReadOnlyList<string>> GetItemTextsAsync()
    {
        var itemsLocator = GetItemsLocator();
        var texts = await itemsLocator.AllTextContentsAsync();
        return texts.ToList();
    }

    /// <summary>
    /// Get item locator at index (for advanced scenarios).
    /// </summary>
    public virtual ILocator? GetItem(int index)
    {
        var itemsLocator = GetItemsLocator();
        var count = itemsLocator.CountAsync().GetAwaiter().GetResult();
        if (index < 0 || index >= count)
            return null;
        return itemsLocator.Nth(index);
    }

    /// <summary>
    /// Wait for item count to equal expected.
    /// </summary>
    public virtual bool WaitItemCount(int expected, int? timeoutMs = null)
    {
        Log($"WaitItemCount(expected={expected})");
        return _context.WaitFor(() => GetItemCount() == expected, timeoutMs,
            $"item count = {expected}");
    }

    /// <summary>
    /// Wait for item count to equal expected asynchronously.
    /// </summary>
    public virtual async Task<bool> WaitItemCountAsync(int expected, int? timeoutMs = null)
    {
        Log($"WaitItemCountAsync(expected={expected})");
        return await _context.WaitForAsync(
            async () => await GetItemCountAsync() == expected, 
            timeoutMs,
            $"item count = {expected}");
    }

    /// <summary>
    /// Wait for at least the specified number of items.
    /// </summary>
    public virtual bool WaitItemCountAtLeast(int minimum, int? timeoutMs = null)
    {
        Log($"WaitItemCountAtLeast(minimum={minimum})");
        return _context.WaitFor(() => GetItemCount() >= minimum, timeoutMs,
            $"item count >= {minimum}");
    }

    /// <summary>
    /// Wait for at least the specified number of items asynchronously.
    /// </summary>
    public virtual async Task<bool> WaitItemCountAtLeastAsync(int minimum, int? timeoutMs = null)
    {
        Log($"WaitItemCountAtLeastAsync(minimum={minimum})");
        return await _context.WaitForAsync(
            async () => await GetItemCountAsync() >= minimum, 
            timeoutMs,
            $"item count >= {minimum}");
    }

    /// <summary>
    /// Assert item count equals expected.
    /// </summary>
    public virtual void AssertItemCount(int expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = GetItemCount();
        if (actual != expected)
        {
            ThrowAssertionFailed("ItemCount", actual.ToString(), expected.ToString(),
                message ?? $"Expected {expected} items but found {actual}.");
        }
        LogAssertPass("ItemCount", actual.ToString(), expected.ToString());
    }

    /// <summary>
    /// Assert item count equals expected asynchronously.
    /// </summary>
    public virtual async Task AssertItemCountAsync(int expected, string? message = null)
    {
        CheckVisible(expected: true);
        var actual = await GetItemCountAsync();
        if (actual != expected)
        {
            ThrowAssertionFailed("ItemCount", actual.ToString(), expected.ToString(),
                message ?? $"Expected {expected} items but found {actual}.");
        }
        LogAssertPass("ItemCount", actual.ToString(), expected.ToString());
    }

    /// <summary>
    /// Assert item with text exists.
    /// </summary>
    public virtual void AssertHasItem(string text, string? message = null)
    {
        CheckVisible(expected: true);
        if (!HasItem(text))
        {
            ThrowAssertionFailed("HasItem", "(not found)", text,
                message ?? $"Expected to find item with text '{text}' but it was not found.");
        }
        LogAssertPass("HasItem", text, text);
    }

    /// <summary>
    /// Assert item with text exists asynchronously.
    /// </summary>
    public virtual async Task AssertHasItemAsync(string text, string? message = null)
    {
        CheckVisible(expected: true);
        if (!await HasItemAsync(text))
        {
            ThrowAssertionFailed("HasItem", "(not found)", text,
                message ?? $"Expected to find item with text '{text}' but it was not found.");
        }
        LogAssertPass("HasItem", text, text);
    }

    /// <summary>
    /// Assert no item with text exists.
    /// </summary>
    public virtual void AssertNotHasItem(string text, string? message = null)
    {
        CheckVisible(expected: true);
        if (HasItem(text))
        {
            ThrowAssertionFailed("NotHasItem", text, "(not found)",
                message ?? $"Expected to not find item with text '{text}' but it was found.");
        }
        LogAssertPass("NotHasItem", "(not found)", "(not found)");
    }

    /// <summary>
    /// Assert no item with text exists asynchronously.
    /// </summary>
    public virtual async Task AssertNotHasItemAsync(string text, string? message = null)
    {
        CheckVisible(expected: true);
        if (await HasItemAsync(text))
        {
            ThrowAssertionFailed("NotHasItem", text, "(not found)",
                message ?? $"Expected to not find item with text '{text}' but it was found.");
        }
        LogAssertPass("NotHasItem", "(not found)", "(not found)");
    }
}
