using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using Brinell.Core.Abstractions;
using Brinell.Maui.Controls.Base;
using Brinell.Maui.Infrastructure;

namespace Brinell.Maui.Controls;

/// <summary>
/// MAUI CollectionView/ListView control wrapper.
/// Supports MAUI CollectionView, ListView, and CarouselView.
/// Inherits from ItemsControlBase for items collection support.
/// </summary>
public class CollectionViewControl : ItemsControlBase
{
    public CollectionViewControl(AppiumTestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    public CollectionViewControl(AppiumTestContext context, string automationId)
        : base(context, automationId)
    {
    }

    // ===== MAUI-specific methods =====

    /// <summary>
    /// Get count of visible items (alias for GetItemCount).
    /// </summary>
    public int GetVisibleItemCount() => GetItemCount();

    /// <summary>
    /// Tap item at index (alias for ClickItem).
    /// </summary>
    public void TapItemAtIndex(int index) => ClickItem(index);

    /// <summary>
    /// Tap item by its AutomationId.
    /// Useful when items have unique identifiers.
    /// </summary>
    public void TapItemByAutomationId(string itemAutomationId)
    {
        LogAction("TapItemByAutomationId", itemAutomationId);
        CheckVisible(expected: true);
        
        var item = _context.Driver.FindElementDirect(itemAutomationId);
        if (item == null)
            throw new InvalidOperationException($"Item '{itemAutomationId}' not found in collection");
        
        item.Click();
    }

    /// <summary>
    /// Scroll to item by AutomationId.
    /// </summary>
    public void ScrollToItem(string itemAutomationId)
    {
        LogAction("ScrollToItem", itemAutomationId);
        
        var maxAttempts = 10;
        for (var i = 0; i < maxAttempts; i++)
        {
            var item = _context.Driver.FindElementDirect(itemAutomationId);
            if (item != null)
            {
                Log($"Found item after {i} scroll attempts");
                return;
            }
            
            ScrollDown();
            Thread.Sleep(300);
        }
        
        throw new InvalidOperationException(
            $"Could not find item '{itemAutomationId}' after {maxAttempts} scroll attempts");
    }
}
