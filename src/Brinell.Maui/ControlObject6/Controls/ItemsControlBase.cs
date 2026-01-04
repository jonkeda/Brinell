using System.Collections.ObjectModel;
using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for collection controls in MAUI.
/// Provides common functionality for item enumeration and interaction.
/// </summary>
public abstract class ItemsControlBase : ControlObjectBase, IItemsControlObject
{
    /// <summary>
    /// Creates a new items control.
    /// </summary>
    protected ItemsControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new items control using AutomationId.
    /// </summary>
    protected ItemsControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <summary>
    /// XPath pattern for finding item elements. Override for specific control types.
    /// </summary>
    protected virtual string ItemXPath => ".//*[@ClassName='ListViewItem' or @ClassName='CollectionViewItem' or contains(@ClassName,'Item')]";

    #region Item Count

    /// <inheritdoc/>
    public virtual int GetItemCount(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var items = element.FindElements(OpenQA.Selenium.By.XPath(ItemXPath));
        Log($"GetItemCount: {items.Count}");
        return items.Count;
    }

    /// <inheritdoc/>
    public virtual bool WaitItemCount(int? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;

        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.Now.AddMilliseconds(timeout);

        while (DateTime.Now < deadline)
        {
            if (GetItemCount(timeoutMs) == expected.Value)
                return true;

            Thread.Sleep(DefaultPollingIntervalMs);
        }

        return false;
    }

    /// <inheritdoc/>
    public virtual void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetItemCount(timeoutMs);
        if (actual != expected.Value)
        {
            var msg = message ?? $"Expected item count {expected} but was {actual}";
            throw new AssertionException(msg, Locator.Value, "AssertItemCount");
        }
    }

    #endregion

    #region Item Text

    /// <inheritdoc/>
    public virtual string GetItemText(int index, int? timeoutMs = null)
    {
        var item = GetItemElement(index, timeoutMs);
        var text = item.Text ?? string.Empty;
        Log($"GetItemText({index}): {text}");
        return text;
    }

    /// <inheritdoc/>
    public virtual void AssertItemText(int index, string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetItemText(index, timeoutMs);
        if (actual != expected)
        {
            var msg = message ?? $"Expected item[{index}] text '{expected}' but was '{actual}'";
            throw new AssertionException(msg, Locator.Value, "AssertItemText");
        }
    }

    /// <inheritdoc/>
    public virtual bool HasItem(string text, int? timeoutMs = null)
    {
        var items = GetAllItemTexts(timeoutMs);
        return items.Contains(text);
    }

    /// <inheritdoc/>
    public virtual int GetItemIndex(string text, int? timeoutMs = null)
    {
        var items = GetAllItemTexts(timeoutMs);
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == text)
                return i;
        }
        return -1;
    }

    /// <inheritdoc/>
    public virtual IReadOnlyList<string> GetAllItemTexts(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var items = element.FindElements(OpenQA.Selenium.By.XPath(ItemXPath));
        var texts = items.Select(i => i.Text ?? string.Empty).ToList();
        Log($"GetAllItemTexts: {texts.Count} items");
        return new ReadOnlyCollection<string>(texts);
    }

    #endregion

    #region Click Item

    /// <inheritdoc/>
    public virtual void ClickItem(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        Log($"ClickItem({index})");
        var item = GetItemElement(index.Value, timeoutMs);
        item.Click();
    }

    /// <inheritdoc/>
    public virtual void ClickItem(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        Log($"ClickItem(\"{text}\")");
        var index = GetItemIndex(text, timeoutMs);
        if (index < 0)
            throw new ElementNotFoundException($"Item with text '{text}' not found");
        ClickItem(index, timeoutMs);
    }

    /// <summary>
    /// Double-clicks the item at the specified index.
    /// </summary>
    /// <param name="index">The item index (0-based). If null, no action is taken.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    public virtual void DoubleClickItem(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        Log($"DoubleClickItem({index})");
        var item = GetItemElement(index.Value, timeoutMs);
        var actions = new OpenQA.Selenium.Interactions.Actions(Driver);
        actions.DoubleClick(item).Perform();
    }

    /// <summary>
    /// Right-clicks the item at the specified index.
    /// </summary>
    /// <param name="index">The item index (0-based). If null, no action is taken.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    public virtual void RightClickItem(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        Log($"RightClickItem({index})");
        var item = GetItemElement(index.Value, timeoutMs);
        var actions = new OpenQA.Selenium.Interactions.Actions(Driver);
        actions.ContextClick(item).Perform();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the element for an item at the specified index.
    /// </summary>
    protected virtual AppiumElement GetItemElement(int index, int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var items = element.FindElements(OpenQA.Selenium.By.XPath(ItemXPath));
        if (index < 0 || index >= items.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} out of range (0-{items.Count - 1})");
        return (AppiumElement)items[index];
    }

    #endregion
}
