# SPEC-006-003b: Selection Controls

**Version:** 1.0  
**Status:** Draft  
**Date:** January 2026  
**Parent:** [SPEC-006-003b-INDEX](SPEC-006-003b-INDEX.md)

---

## 1. MAUI Selection Classes

### 1.1 SelectorControlBase

```csharp
public abstract class SelectorControlBase : ControlObjectBase, ISelectorControlObject
{
    protected SelectorControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected SelectorControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Selection Actions (Example: SelectByIndex)

    public virtual void SelectByIndex(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        Log($"SelectByIndex({index})");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        var element = FindElementRequired(timeoutMs);
        // Implementation varies by control type
        element.SendKeys(Keys.Home);
        for (int i = 0; i < index.Value; i++)
        {
            element.SendKeys(Keys.Down);
        }
        element.SendKeys(Keys.Enter);
    }

    public virtual void SelectByText(string? text, int? timeoutMs = null);
    public virtual void SelectByValue(string? value, int? timeoutMs = null);
    public virtual void ClearSelection(int? timeoutMs = null);

    #endregion

    #region Get Selected (Example: GetSelectedIndex)

    public virtual int GetSelectedIndex(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var value = element.GetAttribute("Selection.Selection");
        return int.TryParse(value, out var index) ? index : -1;
    }

    public virtual void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null);
    public virtual string? GetSelectedText(int? timeoutMs = null);
    public virtual void AssertSelectedText(string? expected, string? message = null, int? timeoutMs = null);
    public virtual string? GetSelectedValue(int? timeoutMs = null);
    public virtual void AssertSelectedValue(string? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region Items (Example: GetItems)

    public virtual IReadOnlyList<string> GetItems(int? timeoutMs = null)
    {
        var items = new List<string>();
        var element = FindElementRequired(timeoutMs);
        var children = element.FindElements(MobileBy.XPath(".//*[@ClassName='ListBoxItem']"));
        foreach (var child in children)
        {
            items.Add(child.Text);
        }
        return items.AsReadOnly();
    }

    public virtual int GetItemCount(int? timeoutMs = null);
    public virtual void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);
    public virtual bool HasItem(string text, int? timeoutMs = null);
    public virtual void AssertHasItem(string text, bool? expected, string? message = null, int? timeoutMs = null);

    #endregion
}
```

### 1.2 PickerControl

```csharp
public class PickerControl : SelectorControlBase, IPickerControlObject
{
    public PickerControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public PickerControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }

    #region Open/Close (Example: IsOpen)

    public virtual bool IsOpen(int? timeoutMs = null)
    {
        // Check if dropdown popup is visible
        try
        {
            var popup = Driver.FindElement(MobileBy.ClassName("Popup"));
            return popup?.Displayed ?? false;
        }
        catch { return false; }
    }

    public virtual bool WaitOpen(bool? expected, int? timeoutMs = null);
    public virtual void AssertOpen(bool? expected, string? message = null, int? timeoutMs = null);
    
    public virtual void Open(int? timeoutMs = null)
    {
        Log("Open()");
        if (!IsOpen(timeoutMs))
        {
            FindElementRequired(timeoutMs).Click();
        }
    }

    public virtual void Close(int? timeoutMs = null);

    #endregion
}
```

### 1.3 MultiSelectorControlBase

```csharp
public abstract class MultiSelectorControlBase : SelectorControlBase, IMultiSelectorControlObject
{
    protected MultiSelectorControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected MultiSelectorControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Multi-Selection (Example: SelectMultiple by indices)

    public virtual void SelectMultiple(IEnumerable<int>? indices, int? timeoutMs = null)
    {
        if (indices is null) return;
        Log($"SelectMultiple({string.Join(",", indices)})");
        CheckVisible(true, timeoutMs);
        
        // Hold Ctrl and click each index
        var actions = new Actions(Driver);
        actions.KeyDown(Keys.Control);
        foreach (var index in indices)
        {
            var item = GetItemElement(index, timeoutMs);
            actions.Click(item);
        }
        actions.KeyUp(Keys.Control);
        actions.Perform();
    }

    public virtual void SelectMultiple(IEnumerable<string>? texts, int? timeoutMs = null);
    public virtual void UnselectByIndex(int? index, int? timeoutMs = null);
    public virtual void UnselectByText(string? text, int? timeoutMs = null);
    public virtual void SelectAll(int? timeoutMs = null);
    public virtual void UnselectAll(int? timeoutMs = null);

    #endregion

    #region Get Selected Multiple

    public virtual IReadOnlyList<int> GetSelectedIndices(int? timeoutMs = null);
    public virtual IReadOnlyList<string> GetSelectedTexts(int? timeoutMs = null);
    public virtual int GetSelectedCount(int? timeoutMs = null);
    public virtual void AssertSelectedCount(int? expected, string? message = null, int? timeoutMs = null);

    #endregion

    protected virtual AppiumElement GetItemElement(int index, int? timeoutMs = null);
}
```

---

## 2. Blazor Selection Classes

### 2.1 AsyncSelectorControlBase

```csharp
public abstract class AsyncSelectorControlBase : AsyncControlObjectBase, IAsyncSelectorControlObject
{
    protected AsyncSelectorControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncSelectorControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    #region Selection Actions (Example: SelectByIndexAsync)

    public virtual async Task SelectByIndexAsync(int? index, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (index is null) return;
        Log($"SelectByIndexAsync({index})");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);
        await GetLocator().SelectOptionAsync(new SelectOptionValue { Index = index.Value });
    }

    public virtual Task SelectByTextAsync(string? text, int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task SelectByValueAsync(string? value, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Get Selected (Example: GetSelectedIndexAsync)

    public virtual async Task<int> GetSelectedIndexAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var value = await GetLocator().EvaluateAsync<int>("el => el.selectedIndex");
        return value;
    }

    public virtual Task AssertSelectedIndexAsync(int? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task<string?> GetSelectedTextAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task AssertSelectedTextAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Items (Example: GetItemsAsync)

    public virtual async Task<IReadOnlyList<string>> GetItemsAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var options = GetLocator().Locator("option");
        var count = await options.CountAsync();
        var items = new List<string>();
        for (int i = 0; i < count; i++)
        {
            items.Add(await options.Nth(i).InnerTextAsync());
        }
        return items.AsReadOnly();
    }

    public virtual Task<int> GetItemCountAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task AssertItemCountAsync(int? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion
}
```

### 2.2 Concrete Blazor Controls

```csharp
/// <summary>HTML select element.</summary>
public class SelectControl : AsyncSelectorControlBase
{
    public SelectControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public SelectControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}
```

---

## 3. Inheritance Summary

```
MAUI:
SelectorControlBase : ControlObjectBase, ISelectorControlObject
├── PickerControl : IPickerControlObject
└── MultiSelectorControlBase : IMultiSelectorControlObject

Blazor:
AsyncSelectorControlBase : AsyncControlObjectBase, IAsyncSelectorControlObject
└── SelectControl
```

---

**Next:** [SPEC-006-003b-RANGE](SPEC-006-003b-RANGE.md)
