# SPEC-006-002h: Collection Classes

**Version:** 1.0  
**Status:** Final  
**Date:** January 2026

---

## 1. ItemsControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class ItemsControlBase : InteractiveControlBase, IItemsControlObject
{
    protected ItemsControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetItemCount
    public virtual int GetItemCount(int? timeoutMs = null)
    {
        CheckVisible(true, timeoutMs);
        var count = GetItemsCore(timeoutMs).Count;
        Log($"GetItemCount: {count}");
        return count;
    }

    // Full implementation for AssertItemCount
    public virtual void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return;
        CheckVisible(true, timeoutMs);
        var actual = GetItemCount(timeoutMs);
        if (actual != expected.Value)
        {
            ThrowAssertionFailed("ItemCount", actual.ToString(), expected.Value.ToString(),
                message ?? $"Expected {expected.Value} items but found {actual}.");
        }
        LogAssertPass("ItemCount", actual.ToString(), expected.Value.ToString());
    }

    // Abstract core method
    protected abstract IReadOnlyList<IControlObject> GetItemsCore(int? timeoutMs = null);

    // Method signatures only
    public abstract IReadOnlyList<IControlObject> GetItems(int? timeoutMs = null);
    public abstract IControlObject GetItemAt(int index, int? timeoutMs = null);
    public abstract IControlObject? GetItemByText(string? text, int? timeoutMs = null);
    public abstract IReadOnlyList<string> GetItemTexts(int? timeoutMs = null);
    public abstract bool HasItem(string? text, int? timeoutMs = null);
    public abstract bool WaitItemCount(int? expected, int? timeoutMs = null);
    public abstract bool WaitHasItem(string? text, bool? expected, int? timeoutMs = null);
    public abstract void AssertHasItem(string? text, bool? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertItemCountInRange(int? min, int? max, string? message = null, int? timeoutMs = null);
    public abstract void ScrollToItem(int index, int? timeoutMs = null);
    public abstract void ScrollToItem(string? text, int? timeoutMs = null);
}
```

---

## 2. SelectableItemsControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class SelectableItemsControlBase : ItemsControlBase, ISelectableItemsControlObject
{
    protected SelectableItemsControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for SelectItem with logging
    public virtual void SelectItem(int? index, int? timeoutMs = null)
    {
        if (index == null) return;
        
        EnsureEnabled(timeoutMs);
        var item = GetItemAt(index.Value, timeoutMs);
        if (item == null)
            ThrowCheckFailed("SelectItem", $"Item at index {index.Value} not found.");
        
        (item as IClickableControlObject)?.Click(timeoutMs);
        LogAction("SelectItem", index.Value.ToString());
    }

    // Method signatures only
    public abstract void SelectItem(string? text, int? timeoutMs = null);
    public abstract int GetSelectedIndex(int? timeoutMs = null);
    public abstract string? GetSelectedText(int? timeoutMs = null);
    public abstract IControlObject? GetSelectedItem(int? timeoutMs = null);
    public abstract bool WaitSelectedIndex(int? expected, int? timeoutMs = null);
    public abstract bool WaitSelectedText(string? expected, int? timeoutMs = null);
    public abstract void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null);
    public abstract void ClearSelection(int? timeoutMs = null);
}
```

---

## 3. MultiSelectItemsControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class MultiSelectItemsControlBase : SelectableItemsControlBase, IMultiSelectItemsControlObject
{
    protected MultiSelectItemsControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for SelectItems with logging
    public virtual void SelectItems(IEnumerable<int>? indices, int? timeoutMs = null)
    {
        if (indices == null) return;
        
        EnsureEnabled(timeoutMs);
        foreach (var index in indices)
        {
            SelectItem(index, timeoutMs);
        }
        LogAction("SelectItems", string.Join(",", indices));
    }

    // Method signatures only
    public abstract void SelectItems(IEnumerable<string>? texts, int? timeoutMs = null);
    public abstract void DeselectItem(int? index, int? timeoutMs = null);
    public abstract void DeselectItem(string? text, int? timeoutMs = null);
    public abstract void DeselectItems(IEnumerable<int>? indices, int? timeoutMs = null);
    public abstract void DeselectItems(IEnumerable<string>? texts, int? timeoutMs = null);
    public abstract void SelectAll(int? timeoutMs = null);
    public abstract void DeselectAll(int? timeoutMs = null);
    public abstract IReadOnlyList<int> GetSelectedIndices(int? timeoutMs = null);
    public abstract IReadOnlyList<string> GetSelectedTexts(int? timeoutMs = null);
    public abstract IReadOnlyList<IControlObject> GetSelectedItems(int? timeoutMs = null);
    public abstract int GetSelectedCount(int? timeoutMs = null);
    public abstract bool WaitSelectedCount(int? expected, int? timeoutMs = null);
    public abstract void AssertSelectedCount(int? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertSelectedIndices(IEnumerable<int>? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertSelectedTexts(IEnumerable<string>? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 4. MAUI Implementation

```csharp
namespace Brinell.Maui;

public class MauiCollectionView : MauiInteractiveControlBase, ISelectableItemsControlObject
{
    private readonly string _itemLocator;

    public MauiCollectionView(ControlLocator locator, IPageObject? page, MauiTestContext context, string? itemLocator = null)
        : base(locator, page, context)
    {
        _itemLocator = itemLocator ?? ".//android.view.View";
    }

    // Full implementation for GetItems
    public IReadOnlyList<IControlObject> GetItems(int? timeoutMs = null)
    {
        var parent = WaitForElementVisible(timeoutMs);
        if (parent == null)
            return Array.Empty<IControlObject>();
        
        var elements = parent.FindElements(OpenQA.Selenium.By.XPath(_itemLocator));
        var items = elements.Select((e, i) => new MauiCollectionViewItem(
            By.Index(i), this.Page, _context, e)).ToList();
        
        Log($"GetItems: found {items.Count} items");
        return items;
    }

    // Full implementation for SelectItem
    public void SelectItem(int? index, int? timeoutMs = null)
    {
        if (index == null) return;
        
        EnsureEnabled(timeoutMs);
        var item = GetItemAt(index.Value, timeoutMs);
        if (item == null)
            ThrowCheckFailed("SelectItem", $"Item at index {index.Value} not found.");
        
        (item as IClickableControlObject)?.Click(timeoutMs);
        LogAction("SelectItem", index.Value.ToString());
    }

    // Method signatures only
    public int GetItemCount(int? timeoutMs = null);
    public IControlObject GetItemAt(int index, int? timeoutMs = null);
    public IControlObject? GetItemByText(string? text, int? timeoutMs = null);
    public IReadOnlyList<string> GetItemTexts(int? timeoutMs = null);
    public bool HasItem(string? text, int? timeoutMs = null);
    public bool WaitItemCount(int? expected, int? timeoutMs = null);
    public bool WaitHasItem(string? text, bool? expected, int? timeoutMs = null);
    public void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertHasItem(string? text, bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertItemCountInRange(int? min, int? max, string? message = null, int? timeoutMs = null);
    public void ScrollToItem(int index, int? timeoutMs = null);
    public void ScrollToItem(string? text, int? timeoutMs = null);
    public void SelectItem(string? text, int? timeoutMs = null);
    public int GetSelectedIndex(int? timeoutMs = null);
    public string? GetSelectedText(int? timeoutMs = null);
    public IControlObject? GetSelectedItem(int? timeoutMs = null);
    public bool WaitSelectedIndex(int? expected, int? timeoutMs = null);
    public bool WaitSelectedText(string? expected, int? timeoutMs = null);
    public void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null);
    public void ClearSelection(int? timeoutMs = null);
}

public class MauiListView : MauiCollectionView
{
    public MauiListView(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context, ".//android.widget.TextView") { }
}
```

---

## 5. Blazor Implementation

```csharp
namespace Brinell.Blazor;

public class BlazorListBox : BlazorInteractiveControlBase, ISelectableItemsControlObject
{
    private readonly string _itemSelector;

    public BlazorListBox(ControlLocator locator, IPageObject? page, BlazorTestContext context, string? itemSelector = null)
        : base(locator, page, context)
    {
        _itemSelector = itemSelector ?? "option, li, [role='option']";
    }

    // Full implementation for GetItems
    public IReadOnlyList<IControlObject> GetItems(int? timeoutMs = null)
    {
        var parent = GetPlaywrightLocator(timeoutMs);
        var itemLocators = parent.Locator(_itemSelector).AllAsync().GetAwaiter().GetResult();
        
        var items = itemLocators.Select((loc, i) => new BlazorListBoxItem(
            By.Index(i), this.Page, _context, loc)).ToList();
        
        Log($"GetItems: found {items.Count} items");
        return items;
    }

    // Full implementation for SelectItem
    public void SelectItem(int? index, int? timeoutMs = null)
    {
        if (index == null) return;
        
        EnsureEnabled(timeoutMs);
        var items = GetItems(timeoutMs);
        if (index.Value < 0 || index.Value >= items.Count)
            ThrowCheckFailed("SelectItem", $"Index {index.Value} out of range (0-{items.Count - 1}).");
        
        var item = items[index.Value];
        (item as IClickableControlObject)?.Click(timeoutMs);
        LogAction("SelectItem", index.Value.ToString());
    }

    // Method signatures only
    public int GetItemCount(int? timeoutMs = null);
    public IControlObject GetItemAt(int index, int? timeoutMs = null);
    public IControlObject? GetItemByText(string? text, int? timeoutMs = null);
    public IReadOnlyList<string> GetItemTexts(int? timeoutMs = null);
    public bool HasItem(string? text, int? timeoutMs = null);
    public bool WaitItemCount(int? expected, int? timeoutMs = null);
    public bool WaitHasItem(string? text, bool? expected, int? timeoutMs = null);
    public void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertHasItem(string? text, bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertItemCountInRange(int? min, int? max, string? message = null, int? timeoutMs = null);
    public void ScrollToItem(int index, int? timeoutMs = null);
    public void ScrollToItem(string? text, int? timeoutMs = null);
    public void SelectItem(string? text, int? timeoutMs = null);
    public int GetSelectedIndex(int? timeoutMs = null);
    public string? GetSelectedText(int? timeoutMs = null);
    public IControlObject? GetSelectedItem(int? timeoutMs = null);
    public bool WaitSelectedIndex(int? expected, int? timeoutMs = null);
    public bool WaitSelectedText(string? expected, int? timeoutMs = null);
    public void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null);
    public void ClearSelection(int? timeoutMs = null);
}

public class BlazorTable : BlazorInteractiveControlBase, IItemsControlObject
{
    public BlazorTable(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Method signatures only
    public IReadOnlyList<IControlObject> GetItems(int? timeoutMs = null);
    public int GetItemCount(int? timeoutMs = null);
    public IControlObject GetItemAt(int index, int? timeoutMs = null);
    public IControlObject? GetItemByText(string? text, int? timeoutMs = null);
    public IReadOnlyList<string> GetItemTexts(int? timeoutMs = null);
    public bool HasItem(string? text, int? timeoutMs = null);
    public bool WaitItemCount(int? expected, int? timeoutMs = null);
    public bool WaitHasItem(string? text, bool? expected, int? timeoutMs = null);
    public void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertHasItem(string? text, bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertItemCountInRange(int? min, int? max, string? message = null, int? timeoutMs = null);
    public void ScrollToItem(int index, int? timeoutMs = null);
    public void ScrollToItem(string? text, int? timeoutMs = null);
    
    // Table-specific
    public IReadOnlyList<IControlObject> GetRows(int? timeoutMs = null);
    public IControlObject GetRowAt(int index, int? timeoutMs = null);
    public IControlObject GetCell(int row, int column, int? timeoutMs = null);
    public IReadOnlyList<string> GetColumnHeaders(int? timeoutMs = null);
}
```

---

**Next:** [SPEC-006-002i: Container Classes](SPEC-006-002-CLASSES-CONTAINER.md)
