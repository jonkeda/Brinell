# SPEC-006-003b: Collection Controls

**Version:** 1.0  
**Status:** Draft  
**Date:** January 2026  
**Parent:** [SPEC-006-003b-INDEX](SPEC-006-003b-INDEX.md)

---

## 1. MAUI Collection Classes

### 1.1 ItemsControlBase

```csharp
public abstract class ItemsControlBase : ControlObjectBase, IItemsControlObject
{
    protected ItemsControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected ItemsControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Item Count (Example: GetItemCount)

    public virtual int GetItemCount(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var items = element.FindElements(MobileBy.XPath(".//*[@ClassName='ListViewItem' or @ClassName='CollectionViewItem']"));
        return items.Count;
    }

    public virtual bool WaitItemCount(int? expected, int? timeoutMs = null);
    public virtual void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region Item Text (Example: GetItemText)

    public virtual string GetItemText(int index, int? timeoutMs = null)
    {
        var item = GetItemElement(index, timeoutMs);
        return item.Text;
    }

    public virtual void AssertItemText(int index, string? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region Has Item (Example: HasItem)

    public virtual bool HasItem(string text, int? timeoutMs = null)
    {
        var items = GetAllItemTexts(timeoutMs);
        return items.Contains(text);
    }

    public virtual void AssertHasItem(string text, bool? expected, string? message = null, int? timeoutMs = null);
    public virtual int GetItemIndex(string text, int? timeoutMs = null);
    public virtual IReadOnlyList<string> GetAllItemTexts(int? timeoutMs = null);

    #endregion

    #region Click Item (Example: ClickItem by index)

    public virtual void ClickItem(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        Log($"ClickItem({index})");
        var item = GetItemElement(index.Value, timeoutMs);
        item.Click();
    }

    public virtual void ClickItem(string? text, int? timeoutMs = null);
    public virtual void DoubleClickItem(int? index, int? timeoutMs = null);
    public virtual void RightClickItem(int? index, int? timeoutMs = null);

    #endregion

    protected virtual AppiumElement GetItemElement(int index, int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var items = element.FindElements(MobileBy.XPath(".//*[@ClassName='ListViewItem' or @ClassName='CollectionViewItem']"));
        if (index < 0 || index >= items.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        return items[index];
    }
}
```

### 1.2 SelectableItemsControlBase

```csharp
public abstract class SelectableItemsControlBase : ItemsControlBase, ISelectableItemsControlObject
{
    protected SelectableItemsControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected SelectableItemsControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Select Item (Example: SelectItem by index)

    public virtual void SelectItem(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        Log($"SelectItem({index})");
        ClickItem(index, timeoutMs);
    }

    public virtual void SelectItem(string? text, int? timeoutMs = null);

    #endregion

    #region Get Selected (Example: GetSelectedItemIndex)

    public virtual int GetSelectedItemIndex(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var items = element.FindElements(MobileBy.XPath(".//*[@ClassName='ListViewItem']"));
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].GetAttribute("SelectionItem.IsSelected") == "True")
                return i;
        }
        return -1;
    }

    public virtual void AssertSelectedItemIndex(int? expected, string? message = null, int? timeoutMs = null);
    public virtual string? GetSelectedItemText(int? timeoutMs = null);
    public virtual void AssertSelectedItemText(string? expected, string? message = null, int? timeoutMs = null);
    public virtual bool IsItemSelected(int index, int? timeoutMs = null);
    public virtual void AssertItemSelected(int index, bool? expected, string? message = null, int? timeoutMs = null);

    #endregion
}
```

### 1.3 ScrollableItemsControlBase

```csharp
public abstract class ScrollableItemsControlBase : ItemsControlBase, IScrollableItemsControlObject
{
    protected ScrollableItemsControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected ScrollableItemsControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Scroll To Item (Example: ScrollToItem by index)

    public virtual void ScrollToItem(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        Log($"ScrollToItem({index})");
        var element = FindElementRequired(timeoutMs);
        
        // Use ScrollIntoView pattern or swipe gestures
        int attempts = 0;
        while (attempts < 10 && !IsItemVisible(index.Value, timeoutMs))
        {
            ScrollDown(timeoutMs: timeoutMs);
            attempts++;
        }
    }

    public virtual void ScrollToItem(string? text, int? timeoutMs = null);
    public virtual void ScrollToTop(int? timeoutMs = null);
    public virtual void ScrollToBottom(int? timeoutMs = null);

    #endregion

    #region Item Visibility

    public virtual bool IsItemVisible(int index, int? timeoutMs = null);
    public virtual bool WaitItemVisible(int index, bool? expected, int? timeoutMs = null);
    public virtual void AssertItemVisible(int index, bool? expected, string? message = null, int? timeoutMs = null);

    #endregion

    protected virtual void ScrollDown(int? distance = null, int? timeoutMs = null);
    protected virtual void ScrollUp(int? distance = null, int? timeoutMs = null);
}
```

### 1.4 GroupedItemsControlBase

```csharp
public abstract class GroupedItemsControlBase : ItemsControlBase, IGroupedItemsControlObject
{
    protected GroupedItemsControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected GroupedItemsControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Group Count (Example: GetGroupCount)

    public virtual int GetGroupCount(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var groups = element.FindElements(MobileBy.XPath(".//*[@ClassName='GroupItem']"));
        return groups.Count;
    }

    public virtual void AssertGroupCount(int? expected, string? message = null, int? timeoutMs = null);
    public virtual IReadOnlyList<string> GetGroupNames(int? timeoutMs = null);
    public virtual int GetGroupItemCount(string groupName, int? timeoutMs = null);

    #endregion

    #region Group Expand/Collapse (Example: IsGroupExpanded)

    public virtual bool IsGroupExpanded(string groupName, int? timeoutMs = null)
    {
        var group = FindGroupElement(groupName, timeoutMs);
        return group?.GetAttribute("ExpandCollapse.ExpandCollapseState") == "Expanded";
    }

    public virtual bool WaitGroupExpanded(string groupName, bool? expected, int? timeoutMs = null);
    public virtual void AssertGroupExpanded(string groupName, bool? expected, string? message = null, int? timeoutMs = null);
    public virtual void ExpandGroup(string? groupName, int? timeoutMs = null);
    public virtual void CollapseGroup(string? groupName, int? timeoutMs = null);
    public virtual void ClickItemInGroup(string? groupName, int? itemIndex, int? timeoutMs = null);

    #endregion

    protected virtual AppiumElement? FindGroupElement(string groupName, int? timeoutMs = null);
}
```

### 1.5 Concrete MAUI Controls

```csharp
public class ListViewControl : SelectableItemsControlBase
{
    public ListViewControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public ListViewControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

public class CollectionViewControl : ScrollableItemsControlBase
{
    public CollectionViewControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public CollectionViewControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}
```

---

## 2. Blazor Collection Classes

### 2.1 AsyncItemsControlBase

```csharp
public abstract class AsyncItemsControlBase : AsyncControlObjectBase
{
    protected AsyncItemsControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncItemsControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    /// <summary>CSS selector for child items.</summary>
    protected virtual string ItemSelector => "li";

    #region Item Count (Example: GetItemCountAsync)

    public virtual async Task<int> GetItemCountAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var items = GetLocator().Locator(ItemSelector);
        return await items.CountAsync();
    }

    public virtual Task AssertItemCountAsync(int? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Item Text (Example: GetItemTextAsync)

    public virtual async Task<string> GetItemTextAsync(int index, int? timeoutMs = null, CancellationToken ct = default)
    {
        var item = GetLocator().Locator(ItemSelector).Nth(index);
        return await item.InnerTextAsync();
    }

    public virtual Task<IReadOnlyList<string>> GetAllItemTextsAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Click Item (Example: ClickItemAsync)

    public virtual async Task ClickItemAsync(int? index, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (index is null) return;
        Log($"ClickItemAsync({index})");
        var item = GetLocator().Locator(ItemSelector).Nth(index.Value);
        await item.ClickAsync(new() { Timeout = timeoutMs ?? DefaultTimeoutMs });
    }

    public virtual Task ClickItemAsync(string? text, int? timeoutMs = null, CancellationToken ct = default);

    #endregion
}
```

### 2.2 Concrete Blazor Controls

```csharp
/// <summary>HTML ul/ol list element.</summary>
public class ListControl : AsyncItemsControlBase
{
    public ListControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public ListControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }

    protected override string ItemSelector => "li";
}

/// <summary>HTML table element.</summary>
public class TableControl : AsyncItemsControlBase
{
    public TableControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public TableControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }

    protected override string ItemSelector => "tbody tr";

    #region Table-Specific (Example: GetCellTextAsync)

    public virtual async Task<string> GetCellTextAsync(int row, int column, int? timeoutMs = null, CancellationToken ct = default)
    {
        var cell = GetLocator().Locator($"tbody tr:nth-child({row + 1}) td:nth-child({column + 1})");
        return await cell.InnerTextAsync();
    }

    public virtual Task<int> GetRowCountAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task<int> GetColumnCountAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task<IReadOnlyList<string>> GetHeaderTextsAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion
}
```

---

## 3. Inheritance Summary

```
MAUI:
ItemsControlBase : ControlObjectBase, IItemsControlObject
├── SelectableItemsControlBase : ISelectableItemsControlObject
│   └── ListViewControl
├── ScrollableItemsControlBase : IScrollableItemsControlObject
│   └── CollectionViewControl
└── GroupedItemsControlBase : IGroupedItemsControlObject

Blazor:
AsyncItemsControlBase : AsyncControlObjectBase
├── ListControl
└── TableControl
```

---

**Next:** [SPEC-006-003b-CONTAINER](SPEC-006-003b-CONTAINER.md)
