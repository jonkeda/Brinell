using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls.Base;

/// <summary>
/// MAUI base class for items controls (CollectionView, ListView, etc.).
/// </summary>
public abstract class ItemsControlBase : ControlBase, IItemsControl
{
    protected ItemsControlBase(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    protected ItemsControlBase(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get count of visible items. Virtualized lists may not show all items.
    /// </summary>
    public virtual int GetItemCount()
    {
        var element = FindElement();
        if (element != null)
        {
            var items = element.FindElements(By.XPath(".//*[@clickable='true']"));
            return items.Count;
        }
        return 0;
    }

    /// <summary>
    /// Get item text at index.
    /// </summary>
    public virtual string GetItemText(int index)
    {
        var element = FindElement();
        if (element != null)
        {
            var items = element.FindElements(By.XPath(".//*[@clickable='true']"));
            if (index < items.Count)
            {
                return items[index].Text ?? string.Empty;
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// Get all visible item texts.
    /// </summary>
    public virtual string[] GetItems()
    {
        var element = FindElement();
        if (element != null)
        {
            var items = element.FindElements(By.XPath(".//*[@clickable='true']"));
            return items.Select(i => i.Text ?? string.Empty).ToArray();
        }
        return Array.Empty<string>();
    }

    /// <summary>
    /// Click an item by index.
    /// </summary>
    public virtual void ClickItem(int index)
    {
        LogAction("ClickItem", index.ToString());
        
        var element = FindElement();
        if (element != null)
        {
            var items = element.FindElements(By.XPath(".//*[@clickable='true']"));
            if (index < items.Count)
            {
                items[index].Click();
            }
            else
            {
                throw new InvalidOperationException(
                    $"Index {index} out of range. Collection has {items.Count} visible items.");
            }
        }
    }

    /// <summary>
    /// Click an item by text.
    /// </summary>
    public virtual void ClickItem(string text)
    {
        LogAction("ClickItem", text);
        
        var element = FindElement();
        if (element != null)
        {
            var item = element.FindElements(By.XPath($".//*[@text='{text}']")).FirstOrDefault();
            if (item != null)
            {
                item.Click();
            }
            else
            {
                throw new InvalidOperationException($"Item '{text}' not found in collection '{AutomationId}'.");
            }
        }
    }

    /// <summary>
    /// Check if an item with the given text exists.
    /// </summary>
    public virtual bool HasItem(string text)
    {
        var items = GetItems();
        return items.Contains(text);
    }

    /// <summary>
    /// Wait for a specific item count.
    /// </summary>
    public virtual bool WaitForItemCount(int expectedCount, int? timeoutMs = null)
    {
        Log($"WaitForItemCount({expectedCount})");
        return _context.WaitFor(() => GetItemCount() == expectedCount, timeoutMs,
            $"item count equals {expectedCount}");
    }

    /// <summary>
    /// Wait for at least one item.
    /// </summary>
    public virtual bool WaitForItems(int? timeoutMs = null)
    {
        Log("WaitForItems()");
        return _context.WaitFor(() => GetItemCount() > 0, timeoutMs, "items visible");
    }

    /// <summary>
    /// Scroll down within the collection.
    /// </summary>
    public virtual void ScrollDown()
    {
        Log("ScrollDown()");
        var element = FindElement();
        if (element != null)
        {
            var location = element.Location;
            var size = element.Size;
            
            var startX = location.X + size.Width / 2;
            var startY = location.Y + (int)(size.Height * 0.8);
            var endY = location.Y + (int)(size.Height * 0.2);
            
            _context.Swipe(startX, startY, startX, endY);
        }
    }

    /// <summary>
    /// Scroll up within the collection.
    /// </summary>
    public virtual void ScrollUp()
    {
        Log("ScrollUp()");
        var element = FindElement();
        if (element != null)
        {
            var location = element.Location;
            var size = element.Size;
            
            var startX = location.X + size.Width / 2;
            var startY = location.Y + (int)(size.Height * 0.2);
            var endY = location.Y + (int)(size.Height * 0.8);
            
            _context.Swipe(startX, startY, startX, endY);
        }
    }
}
