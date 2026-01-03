using OpenQA.Selenium;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Html.Infrastructure;

namespace Brinell.Html.Controls.Base;

/// <summary>
/// HTML/Selenium base class for controls that contain collections of items.
/// Used for lists (ul/ol), tables, grids, and other repeated item structures.
/// </summary>
public abstract class ItemsControlBase : ControlBase, IItemsControl
{
    /// <summary>
    /// CSS selector for finding individual items within the container.
    /// Override this to specify how to find items (e.g., "li", "tr", ".item").
    /// </summary>
    protected abstract string ItemSelector { get; }

    protected ItemsControlBase(SeleniumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected ItemsControlBase(SeleniumTestContext context, IPageObject? page, IWebElement? container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected ItemsControlBase(SeleniumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Find all item elements within this control.
    /// </summary>
    protected virtual IReadOnlyList<IWebElement> FindItems()
    {
        var container = FindElement();
        if (container == null) return Array.Empty<IWebElement>();
        
        return container.FindElements(By.CssSelector(ItemSelector)).ToList();
    }

    /// <summary>
    /// Get the count of items.
    /// </summary>
    public virtual int GetItemCount()
    {
        return FindItems().Count;
    }

    /// <summary>
    /// Get item text at index.
    /// </summary>
    public virtual string GetItemText(int index)
    {
        var items = FindItems();
        if (index < 0 || index >= items.Count)
            return string.Empty;
        
        return items[index].Text ?? string.Empty;
    }

    /// <summary>
    /// Click an item by index.
    /// </summary>
    public virtual void ClickItem(int index)
    {
        LogAction("ClickItem", index.ToString());
        var items = FindItems();
        if (index < 0 || index >= items.Count)
            throw new InvalidOperationException($"Item index {index} is out of range (0-{items.Count - 1}).");
        
        items[index].Click();
    }

    /// <summary>
    /// Click an item by text.
    /// </summary>
    public virtual void ClickItem(string text)
    {
        LogAction("ClickItem", text);
        var items = FindItems();
        var item = items.FirstOrDefault(i => i.Text?.Contains(text) == true);
        
        if (item == null)
            throw new InvalidOperationException($"Item with text '{text}' not found.");
        
        item.Click();
    }

    /// <summary>
    /// Check if an item with the specified text exists.
    /// </summary>
    public virtual bool HasItem(string text)
    {
        var items = FindItems();
        return items.Any(i => i.Text?.Contains(text) == true);
    }

    /// <summary>
    /// Get all item texts.
    /// </summary>
    public virtual IReadOnlyList<string> GetItemTexts()
    {
        return FindItems().Select(i => i.Text ?? string.Empty).ToList();
    }

    /// <summary>
    /// Get item element at index (for advanced scenarios).
    /// </summary>
    public virtual IWebElement? GetItem(int index)
    {
        var items = FindItems();
        if (index < 0 || index >= items.Count)
            return null;
        return items[index];
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
    /// Wait for at least the specified number of items.
    /// </summary>
    public virtual bool WaitItemCountAtLeast(int minimum, int? timeoutMs = null)
    {
        Log($"WaitItemCountAtLeast(minimum={minimum})");
        return _context.WaitFor(() => GetItemCount() >= minimum, timeoutMs,
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
}
