# SPEC-006-002e: Selection Classes

**Version:** 1.0  
**Status:** Final  
**Date:** January 2026

---

## 1. SelectorControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for single-selection controls (dropdowns, pickers).
/// </summary>
public abstract class SelectorControlBase : InteractiveControlBase, ISelectorControlObject
{
    protected SelectorControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    #region Selection Methods

    // Full implementation for GetSelectedIndex
    public virtual int GetSelectedIndex(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return -1;
        
        var index = GetSelectedIndexCore(element);
        Log($"GetSelectedIndex: {index}");
        return index;
    }

    // Full implementation for GetSelectedItem
    public virtual string? GetSelectedItem(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return null;
        
        var item = GetSelectedItemCore(element);
        Log($"GetSelectedItem: {item}");
        return item;
    }

    // Full implementation for SelectByIndex with logging
    public virtual void SelectByIndex(int? index, int? timeoutMs = null)
    {
        if (index == null) return;
        
        EnsureEnabled(timeoutMs);
        SelectByIndexCore(FindElement(timeoutMs), index.Value);
        LogAction("SelectByIndex", index.Value.ToString());
    }

    // Full implementation for SelectByText with logging
    public virtual void SelectByText(string? text, int? timeoutMs = null)
    {
        if (text == null) return;
        
        EnsureEnabled(timeoutMs);
        SelectByTextCore(FindElement(timeoutMs), text);
        LogAction("SelectByText", text);
    }

    // Full implementation for GetItems
    public virtual IReadOnlyList<string> GetItems(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return Array.Empty<string>();
        
        var items = GetItemsCore(element);
        Log($"GetItems: Count={items.Count}");
        return items;
    }

    // Full implementation for GetItemCount
    public virtual int GetItemCount(int? timeoutMs = null)
    {
        var items = GetItems(timeoutMs);
        Log($"GetItemCount: {items.Count}");
        return items.Count;
    }

    // Abstract helpers
    protected abstract int GetSelectedIndexCore(object element);
    protected abstract string? GetSelectedItemCore(object element);
    protected abstract void SelectByIndexCore(object element, int index);
    protected abstract void SelectByTextCore(object element, string text);
    protected abstract IReadOnlyList<string> GetItemsCore(object element);

    #endregion

    #region Wait Methods

    // Full implementation for WaitSelectedIndex
    public virtual bool WaitSelectedIndex(int? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        Log($"WaitSelectedIndex(expected={expected})");
        var timeout = GetTimeout(timeoutMs);
        return WaitUntil(() => GetSelectedIndex() == expected.Value, timeout);
    }

    // Full implementation for WaitSelectedItem
    public virtual bool WaitSelectedItem(string? expected, int? timeoutMs = null)
    {
        if (expected == null) return true;
        
        Log($"WaitSelectedItem(expected={expected})");
        var timeout = GetTimeout(timeoutMs);
        return WaitUntil(() => GetSelectedItem() == expected, timeout);
    }

    // Method signatures only
    public abstract bool WaitItemCount(int? expected, int? timeoutMs = null);

    #endregion

    #region Assert Methods

    // Full implementation for AssertSelectedIndex
    public virtual void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return;
        
        var success = WaitSelectedIndex(expected, timeoutMs);
        if (!success)
        {
            var actual = GetSelectedIndex();
            ThrowAssertionFailed("SelectedIndex", actual.ToString(), expected.Value.ToString(),
                message ?? $"Expected element '{_locator}' selectedIndex={expected.Value} but was {actual}.");
        }
        LogAssertPass("SelectedIndex", expected.Value.ToString(), expected.Value.ToString());
    }

    // Full implementation for AssertSelectedItem
    public virtual void AssertSelectedItem(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return;
        
        var success = WaitSelectedItem(expected, timeoutMs);
        if (!success)
        {
            var actual = GetSelectedItem();
            ThrowAssertionFailed("SelectedItem", actual ?? "(null)", expected,
                message ?? $"Expected element '{_locator}' selectedItem='{expected}' but was '{actual}'.");
        }
        LogAssertPass("SelectedItem", expected, expected);
    }

    // Method signatures only
    public abstract void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertContainsItem(string? item, string? message = null, int? timeoutMs = null);

    #endregion
}
```

---

## 2. PickerControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for picker controls with expanded/collapsed state.
/// </summary>
public abstract class PickerControlBase : SelectorControlBase, IPickerControlObject
{
    protected PickerControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for IsExpanded
    public virtual bool IsExpanded(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return false;
        
        var isExpanded = GetExpandedState(element);
        Log($"IsExpanded: {isExpanded}");
        return isExpanded;
    }

    // Full implementation for Expand with logging
    public virtual void Expand(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        if (!IsExpanded(timeoutMs))
        {
            Click(timeoutMs);
        }
        LogAction("Expand");
    }

    // Full implementation for Collapse with logging
    public virtual void Collapse(int? timeoutMs = null)
    {
        if (IsExpanded(timeoutMs))
        {
            // Platform-specific collapse behavior
            CollapseCore(FindElement(timeoutMs));
        }
        LogAction("Collapse");
    }

    // Abstract helpers
    protected abstract bool GetExpandedState(object element);
    protected abstract void CollapseCore(object element);

    // Method signatures only
    public abstract bool WaitExpanded(bool? expected, int? timeoutMs = null);
    public abstract void AssertExpanded(bool? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 3. MultiSelectorControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for multi-selection controls.
/// </summary>
public abstract class MultiSelectorControlBase : SelectorControlBase, IMultiSelectorControlObject
{
    protected MultiSelectorControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetSelectedItems
    public virtual IReadOnlyList<string> GetSelectedItems(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return Array.Empty<string>();
        
        var items = GetSelectedItemsCore(element);
        Log($"GetSelectedItems: Count={items.Count}");
        return items;
    }

    // Full implementation for GetSelectedIndices
    public virtual IReadOnlyList<int> GetSelectedIndices(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return Array.Empty<int>();
        
        var indices = GetSelectedIndicesCore(element);
        Log($"GetSelectedIndices: Count={indices.Count}");
        return indices;
    }

    // Full implementation for SelectMultiple with logging
    public virtual void SelectMultiple(IEnumerable<string>? items, int? timeoutMs = null)
    {
        if (items == null) return;
        
        EnsureEnabled(timeoutMs);
        foreach (var item in items)
        {
            SelectByText(item, timeoutMs);
        }
        LogAction("SelectMultiple", string.Join(", ", items));
    }

    // Full implementation for DeselectAll with logging
    public virtual void DeselectAll(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        DeselectAllCore(FindElement(timeoutMs));
        LogAction("DeselectAll");
    }

    // Full implementation for SelectAll with logging
    public virtual void SelectAll(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        SelectAllCore(FindElement(timeoutMs));
        LogAction("SelectAll");
    }

    // Abstract helpers
    protected abstract IReadOnlyList<string> GetSelectedItemsCore(object element);
    protected abstract IReadOnlyList<int> GetSelectedIndicesCore(object element);
    protected abstract void DeselectAllCore(object element);
    protected abstract void SelectAllCore(object element);

    // Method signatures only
    public abstract void DeselectByIndex(int? index, int? timeoutMs = null);
    public abstract void DeselectByText(string? text, int? timeoutMs = null);
    public abstract int GetSelectedCount(int? timeoutMs = null);
    public abstract bool WaitSelectedCount(int? expected, int? timeoutMs = null);
    public abstract void AssertSelectedCount(int? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertContainsSelectedItem(string? item, string? message = null, int? timeoutMs = null);
}
```

---

## 4. MAUI Implementation

```csharp
namespace Brinell.Maui;

/// <summary>
/// MAUI Picker control implementation.
/// </summary>
public class MauiPicker : MauiInteractiveControlBase, IPickerControlObject
{
    public MauiPicker(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetSelectedIndex
    public int GetSelectedIndex(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs) as AppiumElement;
        if (element == null) return -1;
        
        // Android uses "checked" attribute or similar for selected item
        var indexAttr = element.GetAttribute("index");
        var index = int.TryParse(indexAttr, out var i) ? i : -1;
        Log($"GetSelectedIndex: {index}");
        return index;
    }

    // Full implementation for GetSelectedItem
    public string? GetSelectedItem(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs) as AppiumElement;
        if (element == null) return null;
        
        var text = element.Text ?? element.GetAttribute("value");
        Log($"GetSelectedItem: {text}");
        return text;
    }

    // Full implementation for SelectByText
    public void SelectByText(string? text, int? timeoutMs = null)
    {
        if (text == null) return;
        
        EnsureEnabled(timeoutMs);
        Click(timeoutMs); // Open picker
        
        // Find and click the item in the picker popup
        var itemLocator = By.AutomationId(text);
        var itemElement = _context.FindElement(itemLocator);
        itemElement?.Click();
        
        LogAction("SelectByText", text);
    }

    // Method signatures only
    public void SelectByIndex(int? index, int? timeoutMs = null);
    public IReadOnlyList<string> GetItems(int? timeoutMs = null);
    public int GetItemCount(int? timeoutMs = null);
    public bool WaitSelectedIndex(int? expected, int? timeoutMs = null);
    public bool WaitSelectedItem(string? expected, int? timeoutMs = null);
    public bool WaitItemCount(int? expected, int? timeoutMs = null);
    public void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertSelectedItem(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertContainsItem(string? item, string? message = null, int? timeoutMs = null);
    public bool IsExpanded(int? timeoutMs = null);
    public void Expand(int? timeoutMs = null);
    public void Collapse(int? timeoutMs = null);
    public bool WaitExpanded(bool? expected, int? timeoutMs = null);
    public void AssertExpanded(bool? expected, string? message = null, int? timeoutMs = null);
}

/// <summary>
/// MAUI CollectionView with selection support.
/// </summary>
public class MauiCollectionSelector : MauiInteractiveControlBase, IMultiSelectorControlObject
{
    public MauiCollectionSelector(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetSelectedItems
    public IReadOnlyList<string> GetSelectedItems(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs) as AppiumElement;
        if (element == null) return Array.Empty<string>();
        
        // Find child elements with selected state
        var children = element.FindElements(OpenQA.Selenium.By.XPath(".//*[@selected='true']"));
        var items = children.Select(c => c.Text ?? "").ToList();
        Log($"GetSelectedItems: Count={items.Count}");
        return items;
    }

    // Full implementation for SelectByText
    public void SelectByText(string? text, int? timeoutMs = null)
    {
        if (text == null) return;
        
        EnsureEnabled(timeoutMs);
        var element = FindElement(timeoutMs) as AppiumElement;
        var item = element?.FindElement(OpenQA.Selenium.By.XPath($".//*[contains(@text, '{text}')]"));
        item?.Click();
        LogAction("SelectByText", text);
    }

    // Method signatures only
    public int GetSelectedIndex(int? timeoutMs = null);
    public string? GetSelectedItem(int? timeoutMs = null);
    public void SelectByIndex(int? index, int? timeoutMs = null);
    public IReadOnlyList<string> GetItems(int? timeoutMs = null);
    public int GetItemCount(int? timeoutMs = null);
    public bool WaitSelectedIndex(int? expected, int? timeoutMs = null);
    public bool WaitSelectedItem(string? expected, int? timeoutMs = null);
    public bool WaitItemCount(int? expected, int? timeoutMs = null);
    public void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertSelectedItem(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertContainsItem(string? item, string? message = null, int? timeoutMs = null);
    public IReadOnlyList<int> GetSelectedIndices(int? timeoutMs = null);
    public void SelectMultiple(IEnumerable<string>? items, int? timeoutMs = null);
    public void DeselectAll(int? timeoutMs = null);
    public void SelectAll(int? timeoutMs = null);
    public void DeselectByIndex(int? index, int? timeoutMs = null);
    public void DeselectByText(string? text, int? timeoutMs = null);
    public int GetSelectedCount(int? timeoutMs = null);
    public bool WaitSelectedCount(int? expected, int? timeoutMs = null);
    public void AssertSelectedCount(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertContainsSelectedItem(string? item, string? message = null, int? timeoutMs = null);
}
```

---

## 5. Blazor Implementation

```csharp
namespace Brinell.Blazor;

/// <summary>
/// Blazor select/dropdown control implementation.
/// </summary>
public class BlazorSelect : BlazorInteractiveControlBase, ISelectorControlObject
{
    public BlazorSelect(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetSelectedItem
    public string? GetSelectedItem(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var value = locator.InputValueAsync().GetAwaiter().GetResult();
        
        // Get the text of the selected option
        var selectedOption = locator.Locator("option:checked");
        var text = selectedOption.TextContentAsync().GetAwaiter().GetResult();
        Log($"GetSelectedItem: {text}");
        return text;
    }

    // Full implementation for GetSelectedIndex
    public int GetSelectedIndex(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var options = locator.Locator("option").AllAsync().GetAwaiter().GetResult();
        
        for (int i = 0; i < options.Count; i++)
        {
            var isSelected = options[i].GetAttributeAsync("selected").GetAwaiter().GetResult();
            if (isSelected != null)
            {
                Log($"GetSelectedIndex: {i}");
                return i;
            }
        }
        
        Log($"GetSelectedIndex: -1");
        return -1;
    }

    // Full implementation for SelectByText
    public void SelectByText(string? text, int? timeoutMs = null)
    {
        if (text == null) return;
        
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.SelectOptionAsync(new SelectOptionValue { Label = text }).GetAwaiter().GetResult();
        LogAction("SelectByText", text);
    }

    // Full implementation for SelectByIndex
    public void SelectByIndex(int? index, int? timeoutMs = null)
    {
        if (index == null) return;
        
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.SelectOptionAsync(new SelectOptionValue { Index = index.Value }).GetAwaiter().GetResult();
        LogAction("SelectByIndex", index.Value.ToString());
    }

    // Full implementation for GetItems
    public IReadOnlyList<string> GetItems(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var options = locator.Locator("option").AllAsync().GetAwaiter().GetResult();
        var items = options.Select(o => o.TextContentAsync().GetAwaiter().GetResult() ?? "").ToList();
        Log($"GetItems: Count={items.Count}");
        return items;
    }

    // Method signatures only
    public int GetItemCount(int? timeoutMs = null);
    public bool WaitSelectedIndex(int? expected, int? timeoutMs = null);
    public bool WaitSelectedItem(string? expected, int? timeoutMs = null);
    public bool WaitItemCount(int? expected, int? timeoutMs = null);
    public void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertSelectedItem(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertContainsItem(string? item, string? message = null, int? timeoutMs = null);
}

/// <summary>
/// Blazor multi-select control implementation.
/// </summary>
public class BlazorMultiSelect : BlazorSelect, IMultiSelectorControlObject
{
    public BlazorMultiSelect(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetSelectedItems
    public IReadOnlyList<string> GetSelectedItems(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var selectedOptions = locator.Locator("option:checked").AllAsync().GetAwaiter().GetResult();
        var items = selectedOptions.Select(o => o.TextContentAsync().GetAwaiter().GetResult() ?? "").ToList();
        Log($"GetSelectedItems: Count={items.Count}");
        return items;
    }

    // Full implementation for SelectMultiple
    public void SelectMultiple(IEnumerable<string>? items, int? timeoutMs = null)
    {
        if (items == null) return;
        
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.SelectOptionAsync(items.Select(i => new SelectOptionValue { Label = i }).ToArray())
            .GetAwaiter().GetResult();
        LogAction("SelectMultiple", string.Join(", ", items));
    }

    // Method signatures only
    public IReadOnlyList<int> GetSelectedIndices(int? timeoutMs = null);
    public void DeselectAll(int? timeoutMs = null);
    public void SelectAll(int? timeoutMs = null);
    public void DeselectByIndex(int? index, int? timeoutMs = null);
    public void DeselectByText(string? text, int? timeoutMs = null);
    public int GetSelectedCount(int? timeoutMs = null);
    public bool WaitSelectedCount(int? expected, int? timeoutMs = null);
    public void AssertSelectedCount(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertContainsSelectedItem(string? item, string? message = null, int? timeoutMs = null);
}

/// <summary>
/// Blazor combobox with search/autocomplete support.
/// </summary>
public class BlazorComboBox : BlazorInteractiveControlBase, IPickerControlObject
{
    public BlazorComboBox(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetSelectedItem
    public string? GetSelectedItem(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var value = locator.InputValueAsync().GetAwaiter().GetResult();
        Log($"GetSelectedItem: {value}");
        return value;
    }

    // Full implementation for SelectByText with typing
    public void SelectByText(string? text, int? timeoutMs = null)
    {
        if (text == null) return;
        
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.FillAsync(text).GetAwaiter().GetResult();
        
        // Wait for and click the matching option
        var option = _context.Page.Locator($"[role='option']:has-text('{text}')").First;
        option.ClickAsync().GetAwaiter().GetResult();
        LogAction("SelectByText", text);
    }

    // Method signatures only
    public int GetSelectedIndex(int? timeoutMs = null);
    public void SelectByIndex(int? index, int? timeoutMs = null);
    public IReadOnlyList<string> GetItems(int? timeoutMs = null);
    public int GetItemCount(int? timeoutMs = null);
    public bool WaitSelectedIndex(int? expected, int? timeoutMs = null);
    public bool WaitSelectedItem(string? expected, int? timeoutMs = null);
    public bool WaitItemCount(int? expected, int? timeoutMs = null);
    public void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertSelectedItem(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertContainsItem(string? item, string? message = null, int? timeoutMs = null);
    public bool IsExpanded(int? timeoutMs = null);
    public void Expand(int? timeoutMs = null);
    public void Collapse(int? timeoutMs = null);
    public bool WaitExpanded(bool? expected, int? timeoutMs = null);
    public void AssertExpanded(bool? expected, string? message = null, int? timeoutMs = null);
}
```

---

**Next:** [SPEC-006-002f: Range Classes](SPEC-006-002-CLASSES-RANGE.md)
