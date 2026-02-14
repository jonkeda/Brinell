# SPEC-006-002l: Navigation Classes

**Version:** 1.0  
**Status:** Final  
**Date:** January 2026

---

## 1. TabControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class TabControlBase : ContainerControlBase, ITabControlObject
{
    protected TabControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for SelectTab with logging
    public virtual void SelectTab(int? index, int? timeoutMs = null)
    {
        if (index == null) return;
        
        EnsureEnabled(timeoutMs);
        var tabs = GetTabs(timeoutMs);
        if (index.Value < 0 || index.Value >= tabs.Count)
            ThrowCheckFailed("SelectTab", $"Tab index {index.Value} out of range (0-{tabs.Count - 1}).");
        
        (tabs[index.Value] as IClickableControlObject)?.Click(timeoutMs);
        LogAction("SelectTab", index.Value.ToString());
    }

    // Full implementation for GetSelectedIndex
    public virtual int GetSelectedIndex(int? timeoutMs = null)
    {
        var tabs = GetTabs(timeoutMs);
        for (int i = 0; i < tabs.Count; i++)
        {
            if (IsTabSelected(tabs[i]))
            {
                Log($"GetSelectedIndex: {i}");
                return i;
            }
        }
        return -1;
    }

    // Abstract core methods
    protected abstract bool IsTabSelected(IControlObject tab);

    // Method signatures only
    public abstract void SelectTab(string? text, int? timeoutMs = null);
    public abstract IReadOnlyList<IControlObject> GetTabs(int? timeoutMs = null);
    public abstract int GetTabCount(int? timeoutMs = null);
    public abstract string? GetSelectedTabText(int? timeoutMs = null);
    public abstract IControlObject? GetSelectedTab(int? timeoutMs = null);
    public abstract IControlObject? GetTabContent(int? timeoutMs = null);
    public abstract IControlObject? GetTabContent(int index, int? timeoutMs = null);
    public abstract bool WaitSelectedIndex(int? expected, int? timeoutMs = null);
    public abstract bool WaitSelectedTabText(string? expected, int? timeoutMs = null);
    public abstract void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertSelectedTabText(string? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertTabCount(int? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 2. MenuControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class MenuControlBase : ContainerControlBase, IMenuControlObject
{
    protected MenuControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for SelectMenuItem with logging
    public virtual void SelectMenuItem(string? path, int? timeoutMs = null)
    {
        if (path == null) return;
        
        EnsureEnabled(timeoutMs);
        var parts = path.Split('/');
        IControlObject? current = null;
        
        foreach (var part in parts)
        {
            var items = current == null ? GetMenuItems(timeoutMs) : GetSubMenuItems(current, timeoutMs);
            var item = items.FirstOrDefault(i => GetItemText(i) == part);
            
            if (item == null)
                ThrowCheckFailed("SelectMenuItem", $"Menu item '{part}' not found in path '{path}'.");
            
            (item as IClickableControlObject)?.Click(timeoutMs);
            current = item;
        }
        
        LogAction("SelectMenuItem", path);
    }

    // Abstract core methods
    protected abstract string? GetItemText(IControlObject item);
    protected abstract IReadOnlyList<IControlObject> GetSubMenuItems(IControlObject parent, int? timeoutMs = null);

    // Method signatures only
    public abstract IReadOnlyList<IControlObject> GetMenuItems(int? timeoutMs = null);
    public abstract IControlObject? GetMenuItem(string? text, int? timeoutMs = null);
    public abstract bool HasMenuItem(string? text, int? timeoutMs = null);
    public abstract bool IsMenuItemEnabled(string? text, int? timeoutMs = null);
    public abstract void OpenMenu(int? timeoutMs = null);
    public abstract void CloseMenu(int? timeoutMs = null);
    public abstract bool IsMenuOpen(int? timeoutMs = null);
    public abstract bool WaitMenuOpen(bool? expected, int? timeoutMs = null);
    public abstract void AssertMenuOpen(bool? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertHasMenuItem(string? text, bool? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertMenuItemEnabled(string? text, bool? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 3. FlyoutControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class FlyoutControlBase : ContainerControlBase, IFlyoutControlObject
{
    protected FlyoutControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for Open with logging
    public virtual void Open(int? timeoutMs = null)
    {
        if (!IsOpen(timeoutMs))
        {
            OpenCore();
            LogAction("Open");
        }
    }

    // Full implementation for IsOpen
    public virtual bool IsOpen(int? timeoutMs = null)
    {
        var open = IsOpenCore(timeoutMs);
        Log($"IsOpen: {open}");
        return open;
    }

    // Abstract core methods
    protected abstract void OpenCore();
    protected abstract void CloseCore();
    protected abstract bool IsOpenCore(int? timeoutMs = null);

    // Method signatures only
    public abstract void Close(int? timeoutMs = null);
    public abstract void Toggle(int? timeoutMs = null);
    public abstract IControlObject? GetContent(int? timeoutMs = null);
    public abstract bool WaitOpen(bool? expected, int? timeoutMs = null);
    public abstract void AssertOpen(bool? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 4. ToolbarControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class ToolbarControlBase : ContainerControlBase, IToolbarControlObject
{
    protected ToolbarControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Method signatures only
    public abstract IReadOnlyList<IControlObject> GetItems(int? timeoutMs = null);
    public abstract IControlObject? GetItem(string? name, int? timeoutMs = null);
    public abstract IControlObject? GetItem(int index, int? timeoutMs = null);
    public abstract void ClickItem(string? name, int? timeoutMs = null);
    public abstract void ClickItem(int? index, int? timeoutMs = null);
    public abstract bool HasItem(string? name, int? timeoutMs = null);
    public abstract bool IsItemEnabled(string? name, int? timeoutMs = null);
    public abstract int GetItemCount(int? timeoutMs = null);
    public abstract void AssertHasItem(string? name, bool? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertItemEnabled(string? name, bool? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 5. MAUI Implementation

```csharp
namespace Brinell.Maui;

public class MauiTabBar : MauiControlBase, ITabControlObject
{
    public MauiTabBar(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for GetTabs
    public IReadOnlyList<IControlObject> GetTabs(int? timeoutMs = null)
    {
        var parent = WaitForElementVisible(timeoutMs);
        if (parent == null)
            return Array.Empty<IControlObject>();
        
        // Android TabLayout uses LinearLayout with tab items
        var elements = parent.FindElements(OpenQA.Selenium.By.XPath(".//android.widget.LinearLayout[@content-desc]"));
        var tabs = elements.Select((e, i) => new MauiTabBarItem(
            By.Index(i), this.Page, _context, e)).ToList();
        
        Log($"GetTabs: found {tabs.Count} tabs");
        return tabs;
    }

    // Full implementation for SelectTab
    public void SelectTab(int? index, int? timeoutMs = null)
    {
        if (index == null) return;
        
        EnsureEnabled(timeoutMs);
        var tabs = GetTabs(timeoutMs);
        if (index.Value < 0 || index.Value >= tabs.Count)
            ThrowCheckFailed("SelectTab", $"Tab index {index.Value} out of range (0-{tabs.Count - 1}).");
        
        (tabs[index.Value] as IClickableControlObject)?.Click(timeoutMs);
        LogAction("SelectTab", index.Value.ToString());
    }

    // Method signatures only
    public void SelectTab(string? text, int? timeoutMs = null);
    public int GetTabCount(int? timeoutMs = null);
    public int GetSelectedIndex(int? timeoutMs = null);
    public string? GetSelectedTabText(int? timeoutMs = null);
    public IControlObject? GetSelectedTab(int? timeoutMs = null);
    public IControlObject? GetTabContent(int? timeoutMs = null);
    public IControlObject? GetTabContent(int index, int? timeoutMs = null);
    public bool WaitSelectedIndex(int? expected, int? timeoutMs = null);
    public bool WaitSelectedTabText(string? expected, int? timeoutMs = null);
    public void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertSelectedTabText(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertTabCount(int? expected, string? message = null, int? timeoutMs = null);
    public T FindChild<T>(ControlLocator locator) where T : IControlObject;
    public IReadOnlyList<T> FindChildren<T>(ControlLocator locator) where T : IControlObject;
    public IControlObject FindChildByText(string? text, int? timeoutMs = null);
    public IReadOnlyList<IControlObject> GetAllChildren(int? timeoutMs = null);
    public int GetChildCount(int? timeoutMs = null);
    public bool HasChild(ControlLocator locator, int? timeoutMs = null);
    public bool WaitChildExists(ControlLocator locator, bool? expected, int? timeoutMs = null);
    public bool WaitChildCount(int? expected, int? timeoutMs = null);
    public void AssertChildExists(ControlLocator locator, bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertChildCount(int? expected, string? message = null, int? timeoutMs = null);
}

public class MauiFlyout : MauiControlBase, IFlyoutControlObject
{
    public MauiFlyout(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Method signatures only
    public bool IsOpen(int? timeoutMs = null);
    public void Open(int? timeoutMs = null);
    public void Close(int? timeoutMs = null);
    public void Toggle(int? timeoutMs = null);
    public IControlObject? GetContent(int? timeoutMs = null);
    public bool WaitOpen(bool? expected, int? timeoutMs = null);
    public void AssertOpen(bool? expected, string? message = null, int? timeoutMs = null);
    public T FindChild<T>(ControlLocator locator) where T : IControlObject;
    public IReadOnlyList<T> FindChildren<T>(ControlLocator locator) where T : IControlObject;
    public IControlObject FindChildByText(string? text, int? timeoutMs = null);
    public IReadOnlyList<IControlObject> GetAllChildren(int? timeoutMs = null);
    public int GetChildCount(int? timeoutMs = null);
    public bool HasChild(ControlLocator locator, int? timeoutMs = null);
    public bool WaitChildExists(ControlLocator locator, bool? expected, int? timeoutMs = null);
    public bool WaitChildCount(int? expected, int? timeoutMs = null);
    public void AssertChildExists(ControlLocator locator, bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertChildCount(int? expected, string? message = null, int? timeoutMs = null);
}

public class MauiToolbar : MauiControlBase, IToolbarControlObject
{
    public MauiToolbar(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Method signatures only
    public IReadOnlyList<IControlObject> GetItems(int? timeoutMs = null);
    public IControlObject? GetItem(string? name, int? timeoutMs = null);
    public IControlObject? GetItem(int index, int? timeoutMs = null);
    public void ClickItem(string? name, int? timeoutMs = null);
    public void ClickItem(int? index, int? timeoutMs = null);
    public bool HasItem(string? name, int? timeoutMs = null);
    public bool IsItemEnabled(string? name, int? timeoutMs = null);
    public int GetItemCount(int? timeoutMs = null);
    public void AssertHasItem(string? name, bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertItemEnabled(string? name, bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);
    public T FindChild<T>(ControlLocator locator) where T : IControlObject;
    public IReadOnlyList<T> FindChildren<T>(ControlLocator locator) where T : IControlObject;
    public IControlObject FindChildByText(string? text, int? timeoutMs = null);
    public IReadOnlyList<IControlObject> GetAllChildren(int? timeoutMs = null);
    public int GetChildCount(int? timeoutMs = null);
    public bool HasChild(ControlLocator locator, int? timeoutMs = null);
    public bool WaitChildExists(ControlLocator locator, bool? expected, int? timeoutMs = null);
    public bool WaitChildCount(int? expected, int? timeoutMs = null);
    public void AssertChildExists(ControlLocator locator, bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertChildCount(int? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 6. Blazor Implementation

```csharp
namespace Brinell.Blazor;

public class BlazorTabs : BlazorControlBase, ITabControlObject
{
    private readonly string _tabSelector;
    private readonly string _selectedClass;

    public BlazorTabs(ControlLocator locator, IPageObject? page, BlazorTestContext context,
        string? tabSelector = null, string? selectedClass = null)
        : base(locator, page, context)
    {
        _tabSelector = tabSelector ?? "[role='tab'], .nav-link, .tab";
        _selectedClass = selectedClass ?? "active";
    }

    // Full implementation for GetTabs
    public IReadOnlyList<IControlObject> GetTabs(int? timeoutMs = null)
    {
        var parent = GetPlaywrightLocator(timeoutMs);
        var tabLocators = parent.Locator(_tabSelector).AllAsync().GetAwaiter().GetResult();
        
        var tabs = tabLocators.Select((loc, i) => new BlazorTabItem(
            By.Index(i), this.Page, _context, loc)).ToList();
        
        Log($"GetTabs: found {tabs.Count} tabs");
        return tabs;
    }

    // Full implementation for SelectTab
    public void SelectTab(int? index, int? timeoutMs = null)
    {
        if (index == null) return;
        
        EnsureEnabled(timeoutMs);
        var tabs = GetTabs(timeoutMs);
        if (index.Value < 0 || index.Value >= tabs.Count)
            ThrowCheckFailed("SelectTab", $"Tab index {index.Value} out of range (0-{tabs.Count - 1}).");
        
        (tabs[index.Value] as IClickableControlObject)?.Click(timeoutMs);
        LogAction("SelectTab", index.Value.ToString());
    }

    // Full implementation for GetSelectedIndex
    public int GetSelectedIndex(int? timeoutMs = null)
    {
        var parent = GetPlaywrightLocator(timeoutMs);
        var tabLocators = parent.Locator(_tabSelector).AllAsync().GetAwaiter().GetResult();
        
        for (int i = 0; i < tabLocators.Count; i++)
        {
            var classes = tabLocators[i].GetAttributeAsync("class").GetAwaiter().GetResult() ?? "";
            var ariaSelected = tabLocators[i].GetAttributeAsync("aria-selected").GetAwaiter().GetResult();
            
            if (classes.Contains(_selectedClass) || ariaSelected == "true")
            {
                Log($"GetSelectedIndex: {i}");
                return i;
            }
        }
        
        return -1;
    }

    // Method signatures only
    public void SelectTab(string? text, int? timeoutMs = null);
    public int GetTabCount(int? timeoutMs = null);
    public string? GetSelectedTabText(int? timeoutMs = null);
    public IControlObject? GetSelectedTab(int? timeoutMs = null);
    public IControlObject? GetTabContent(int? timeoutMs = null);
    public IControlObject? GetTabContent(int index, int? timeoutMs = null);
    public bool WaitSelectedIndex(int? expected, int? timeoutMs = null);
    public bool WaitSelectedTabText(string? expected, int? timeoutMs = null);
    public void AssertSelectedIndex(int? expected, string? message = null, int? timeoutMs = null);
    public void AssertSelectedTabText(string? expected, string? message = null, int? timeoutMs = null);
    public void AssertTabCount(int? expected, string? message = null, int? timeoutMs = null);
    public T FindChild<T>(ControlLocator locator) where T : IControlObject;
    public IReadOnlyList<T> FindChildren<T>(ControlLocator locator) where T : IControlObject;
    public IControlObject FindChildByText(string? text, int? timeoutMs = null);
    public IReadOnlyList<IControlObject> GetAllChildren(int? timeoutMs = null);
    public int GetChildCount(int? timeoutMs = null);
    public bool HasChild(ControlLocator locator, int? timeoutMs = null);
    public bool WaitChildExists(ControlLocator locator, bool? expected, int? timeoutMs = null);
    public bool WaitChildCount(int? expected, int? timeoutMs = null);
    public void AssertChildExists(ControlLocator locator, bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertChildCount(int? expected, string? message = null, int? timeoutMs = null);
}

public class BlazorDropdown : BlazorControlBase, IMenuControlObject
{
    public BlazorDropdown(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for IsMenuOpen
    public bool IsMenuOpen(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var classes = locator.GetAttributeAsync("class").GetAwaiter().GetResult() ?? "";
        var open = classes.Contains("show") || classes.Contains("open");
        Log($"IsMenuOpen: {open}");
        return open;
    }

    // Method signatures only
    public void SelectMenuItem(string? path, int? timeoutMs = null);
    public IReadOnlyList<IControlObject> GetMenuItems(int? timeoutMs = null);
    public IControlObject? GetMenuItem(string? text, int? timeoutMs = null);
    public bool HasMenuItem(string? text, int? timeoutMs = null);
    public bool IsMenuItemEnabled(string? text, int? timeoutMs = null);
    public void OpenMenu(int? timeoutMs = null);
    public void CloseMenu(int? timeoutMs = null);
    public bool WaitMenuOpen(bool? expected, int? timeoutMs = null);
    public void AssertMenuOpen(bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertHasMenuItem(string? text, bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertMenuItemEnabled(string? text, bool? expected, string? message = null, int? timeoutMs = null);
    public T FindChild<T>(ControlLocator locator) where T : IControlObject;
    public IReadOnlyList<T> FindChildren<T>(ControlLocator locator) where T : IControlObject;
    public IControlObject FindChildByText(string? text, int? timeoutMs = null);
    public IReadOnlyList<IControlObject> GetAllChildren(int? timeoutMs = null);
    public int GetChildCount(int? timeoutMs = null);
    public bool HasChild(ControlLocator locator, int? timeoutMs = null);
    public bool WaitChildExists(ControlLocator locator, bool? expected, int? timeoutMs = null);
    public bool WaitChildCount(int? expected, int? timeoutMs = null);
    public void AssertChildExists(ControlLocator locator, bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertChildCount(int? expected, string? message = null, int? timeoutMs = null);
}

public class BlazorModal : BlazorControlBase, IFlyoutControlObject
{
    public BlazorModal(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for IsOpen
    public bool IsOpen(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var isVisible = locator.IsVisibleAsync().GetAwaiter().GetResult();
        var classes = locator.GetAttributeAsync("class").GetAwaiter().GetResult() ?? "";
        var open = isVisible && (classes.Contains("show") || !classes.Contains("hidden"));
        Log($"IsOpen: {open}");
        return open;
    }

    // Method signatures only
    public void Open(int? timeoutMs = null);
    public void Close(int? timeoutMs = null);
    public void Toggle(int? timeoutMs = null);
    public IControlObject? GetContent(int? timeoutMs = null);
    public bool WaitOpen(bool? expected, int? timeoutMs = null);
    public void AssertOpen(bool? expected, string? message = null, int? timeoutMs = null);
    public T FindChild<T>(ControlLocator locator) where T : IControlObject;
    public IReadOnlyList<T> FindChildren<T>(ControlLocator locator) where T : IControlObject;
    public IControlObject FindChildByText(string? text, int? timeoutMs = null);
    public IReadOnlyList<IControlObject> GetAllChildren(int? timeoutMs = null);
    public int GetChildCount(int? timeoutMs = null);
    public bool HasChild(ControlLocator locator, int? timeoutMs = null);
    public bool WaitChildExists(ControlLocator locator, bool? expected, int? timeoutMs = null);
    public bool WaitChildCount(int? expected, int? timeoutMs = null);
    public void AssertChildExists(ControlLocator locator, bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertChildCount(int? expected, string? message = null, int? timeoutMs = null);
}

public class BlazorNavbar : BlazorControlBase, IToolbarControlObject
{
    public BlazorNavbar(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Method signatures only
    public IReadOnlyList<IControlObject> GetItems(int? timeoutMs = null);
    public IControlObject? GetItem(string? name, int? timeoutMs = null);
    public IControlObject? GetItem(int index, int? timeoutMs = null);
    public void ClickItem(string? name, int? timeoutMs = null);
    public void ClickItem(int? index, int? timeoutMs = null);
    public bool HasItem(string? name, int? timeoutMs = null);
    public bool IsItemEnabled(string? name, int? timeoutMs = null);
    public int GetItemCount(int? timeoutMs = null);
    public void AssertHasItem(string? name, bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertItemEnabled(string? name, bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertItemCount(int? expected, string? message = null, int? timeoutMs = null);
    public T FindChild<T>(ControlLocator locator) where T : IControlObject;
    public IReadOnlyList<T> FindChildren<T>(ControlLocator locator) where T : IControlObject;
    public IControlObject FindChildByText(string? text, int? timeoutMs = null);
    public IReadOnlyList<IControlObject> GetAllChildren(int? timeoutMs = null);
    public int GetChildCount(int? timeoutMs = null);
    public bool HasChild(ControlLocator locator, int? timeoutMs = null);
    public bool WaitChildExists(ControlLocator locator, bool? expected, int? timeoutMs = null);
    public bool WaitChildCount(int? expected, int? timeoutMs = null);
    public void AssertChildExists(ControlLocator locator, bool? expected, string? message = null, int? timeoutMs = null);
    public void AssertChildCount(int? expected, string? message = null, int? timeoutMs = null);
}
```

---

**Next:** [SPEC-006-002m: Context Classes](SPEC-006-002-CLASSES-CONTEXT.md)
