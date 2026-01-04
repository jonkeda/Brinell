using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Core.Exceptions;
using Brinell.Maui.ControlObject6.Context;
using OpenQA.Selenium.Appium;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// Base class for selectable collection controls in MAUI.
/// Extends ItemsControlBase with selection capabilities.
/// </summary>
public abstract class SelectableItemsControlBase : ItemsControlBase, ISelectableItemsControlObject
{
    /// <summary>
    /// Creates a new selectable items control.
    /// </summary>
    protected SelectableItemsControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new selectable items control using AutomationId.
    /// </summary>
    protected SelectableItemsControlBase(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    #region Select Item

    /// <inheritdoc/>
    public virtual void SelectItem(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        Log($"SelectItem({index})");
        ClickItem(index, timeoutMs);
    }

    /// <inheritdoc/>
    public virtual void SelectItem(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        Log($"SelectItem(\"{text}\")");
        ClickItem(text, timeoutMs);
    }

    #endregion

    #region Get Selected

    /// <inheritdoc/>
    public virtual int GetSelectedItemIndex(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var items = element.FindElements(OpenQA.Selenium.By.XPath(ItemXPath));

        for (int i = 0; i < items.Count; i++)
        {
            var isSelected = ((AppiumElement)items[i]).GetAttribute("SelectionItem.IsSelected");
            if (isSelected == "True" || isSelected == "true")
            {
                Log($"GetSelectedItemIndex: {i}");
                return i;
            }
        }

        Log("GetSelectedItemIndex: -1 (none selected)");
        return -1;
    }

    /// <inheritdoc/>
    public virtual void AssertSelectedItemIndex(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetSelectedItemIndex(timeoutMs);
        if (actual != expected.Value)
        {
            var msg = message ?? $"Expected selected index {expected} but was {actual}";
            throw new AssertionException(msg, Locator.Value, "AssertSelectedItemIndex");
        }
    }

    /// <inheritdoc/>
    public virtual string? GetSelectedItemText(int? timeoutMs = null)
    {
        var index = GetSelectedItemIndex(timeoutMs);
        if (index < 0)
            return null;

        return GetItemText(index, timeoutMs);
    }

    /// <inheritdoc/>
    public virtual void AssertSelectedItemText(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = GetSelectedItemText(timeoutMs);
        if (actual != expected)
        {
            var msg = message ?? $"Expected selected text '{expected}' but was '{actual}'";
            throw new AssertionException(msg, Locator.Value, "AssertSelectedItemText");
        }
    }

    /// <inheritdoc/>
    public virtual bool IsItemSelected(int index, int? timeoutMs = null)
    {
        var item = GetItemElement(index, timeoutMs);
        var isSelected = item.GetAttribute("SelectionItem.IsSelected");
        return isSelected == "True" || isSelected == "true";
    }

    /// <inheritdoc/>
    public virtual void AssertItemSelected(int index, bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;

        var actual = IsItemSelected(index, timeoutMs);
        if (actual != expected.Value)
        {
            var msg = message ?? $"Expected item[{index}] to be {(expected.Value ? "selected" : "not selected")} but was {(actual ? "selected" : "not selected")}";
            throw new AssertionException(msg, Locator.Value, "AssertItemSelected");
        }
    }

    #endregion
}
