using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using Brinell.Core.Abstractions;
using Brinell.Core.Abstractions.Controls;
using Brinell.Wpf.Infrastructure;

namespace Brinell.Wpf.Controls.Base;

/// <summary>
/// WPF base class for controls that contain collections of items.
/// </summary>
public abstract class ItemsControlBase : ControlBase, IItemsControl
{
    protected ItemsControlBase(FlaUITestContext context, IPageObject? page, string automationId)
        : base(context, page, automationId)
    {
    }

    /// <summary>
    /// Create a control that searches within a container element.
    /// </summary>
    protected ItemsControlBase(FlaUITestContext context, IPageObject? page, AutomationElement container, string automationId)
        : base(context, page, container, automationId)
    {
    }

    protected ItemsControlBase(FlaUITestContext context, string automationId)
        : base(context, automationId)
    {
    }

    /// <summary>
    /// Get items from the control. Override in derived classes.
    /// </summary>
    protected abstract AutomationElement[] GetItemElements();

    /// <summary>
    /// Get the count of items.
    /// </summary>
    public virtual int GetItemCount()
    {
        return GetItemElements().Length;
    }

    /// <summary>
    /// Get item text at index.
    /// </summary>
    public virtual string GetItemText(int index)
    {
        var items = GetItemElements();
        if (index >= 0 && index < items.Length)
        {
            return items[index].Name ?? string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// Click an item by index.
    /// </summary>
    public virtual void ClickItem(int index)
    {
        var items = GetItemElements();
        if (index >= 0 && index < items.Length)
        {
            items[index].Click();
        }
        LogAction("ClickItem", index.ToString());
    }

    /// <summary>
    /// Click an item by text.
    /// </summary>
    public virtual void ClickItem(string text)
    {
        var items = GetItemElements();
        var item = items.FirstOrDefault(i => i.Name == text);
        item?.Click();
        LogAction("ClickItem", text);
    }

    /// <summary>
    /// Check if an item exists.
    /// </summary>
    public virtual bool HasItem(string text)
    {
        var items = GetItemElements();
        return items.Any(i => i.Name == text);
    }

    /// <summary>
    /// Wait for item count to reach expected value.
    /// </summary>
    public virtual bool WaitItemCount(int expected, int? timeoutMs = null)
    {
        var sw = Stopwatch.StartNew();
        var result = _context.WaitFor(
            () => GetItemCount() == expected,
            timeoutMs,
            $"item count = {expected}");
        LogWait($"ItemCount={expected}", result, (int)sw.ElapsedMilliseconds);
        return result;
    }
}
