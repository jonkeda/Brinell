using Brinell.Core.ControlObject6.Interfaces;
using Brinell.Core.ControlObject6.Locators;
using Brinell.Maui.ControlObject6.Context;

namespace Brinell.Maui.ControlObject6.Controls;

/// <summary>
/// CollectionView control for MAUI.
/// Supports scrolling with optional selection.
/// </summary>
public class CollectionViewControl : ScrollableItemsControlBase, ISelectableItemsControlObject
{
    /// <summary>
    /// Creates a new CollectionView control.
    /// </summary>
    public CollectionViewControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page)
    {
    }

    /// <summary>
    /// Creates a new CollectionView control using AutomationId.
    /// </summary>
    public CollectionViewControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page)
    {
    }

    /// <inheritdoc/>
    protected override string ItemXPath => ".//*[@ClassName='CollectionViewItem' or contains(@ClassName,'DataItem') or contains(@ClassName,'Item')]";

    #region Selection (from ISelectableItemsControlObject)

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

    /// <inheritdoc/>
    public virtual int GetSelectedItemIndex(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var items = element.FindElements(OpenQA.Selenium.By.XPath(ItemXPath));

        for (int i = 0; i < items.Count; i++)
        {
            var item = (OpenQA.Selenium.Appium.AppiumElement)items[i];
            var isSelected = item.GetAttribute("SelectionItem.IsSelected");
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
            throw new Brinell.Core.Exceptions.AssertionException(msg, Locator.Value, "AssertSelectedItemIndex");
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
            throw new Brinell.Core.Exceptions.AssertionException(msg, Locator.Value, "AssertSelectedItemText");
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
            throw new Brinell.Core.Exceptions.AssertionException(msg, Locator.Value, "AssertItemSelected");
        }
    }

    #endregion

    #region Multi-Select Support

    /// <summary>
    /// Selects multiple items by their indices.
    /// </summary>
    /// <param name="indices">The indices to select. If null or empty, no action is taken.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    public virtual void SelectItems(int[]? indices, int? timeoutMs = null)
    {
        if (indices is null || indices.Length == 0) return;
        Log($"SelectItems([{string.Join(", ", indices)}])");

        foreach (var index in indices)
        {
            SelectItem(index, timeoutMs);
        }
    }

    /// <summary>
    /// Gets the indices of all selected items.
    /// </summary>
    public virtual IReadOnlyList<int> GetSelectedItemIndices(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var items = element.FindElements(OpenQA.Selenium.By.XPath(ItemXPath));
        var selected = new List<int>();

        for (int i = 0; i < items.Count; i++)
        {
            var item = (OpenQA.Selenium.Appium.AppiumElement)items[i];
            var isSelected = item.GetAttribute("SelectionItem.IsSelected");
            if (isSelected == "True" || isSelected == "true")
            {
                selected.Add(i);
            }
        }

        Log($"GetSelectedItemIndices: [{string.Join(", ", selected)}]");
        return selected.AsReadOnly();
    }

    #endregion

    #region Grouping Support

    /// <summary>
    /// Gets the group header text at the specified group index.
    /// </summary>
    /// <param name="groupIndex">The group index (0-based). If null, no action is taken.</param>
    /// <param name="timeoutMs">Optional timeout in milliseconds.</param>
    public virtual string? GetGroupHeaderText(int? groupIndex, int? timeoutMs = null)
    {
        if (groupIndex is null) return null;

        var element = FindElementRequired(timeoutMs);
        var headers = element.FindElements(OpenQA.Selenium.By.XPath(".//*[contains(@ClassName,'GroupHeader') or contains(@ClassName,'Header')]"));

        if (groupIndex.Value < 0 || groupIndex.Value >= headers.Count)
            return null;

        var text = headers[groupIndex.Value].Text;
        Log($"GetGroupHeaderText({groupIndex}): {text}");
        return text;
    }

    /// <summary>
    /// Gets the count of groups in the CollectionView.
    /// </summary>
    public virtual int GetGroupCount(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var headers = element.FindElements(OpenQA.Selenium.By.XPath(".//*[contains(@ClassName,'GroupHeader') or contains(@ClassName,'Header')]"));
        Log($"GetGroupCount: {headers.Count}");
        return headers.Count;
    }

    #endregion
}
