# SPEC-006-003b: Navigation Controls

**Version:** 1.0  
**Status:** Draft  
**Date:** January 2026  
**Parent:** [SPEC-006-003b-INDEX](SPEC-006-003b-INDEX.md)

---

## 1. MAUI Navigation Classes

### 1.1 TabControlBase

```csharp
public abstract class TabControlBase : ControlObjectBase, ITabControlObject
{
    protected TabControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected TabControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Tab Count (Example: GetTabCount)

    public virtual int GetTabCount(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var tabs = element.FindElements(MobileBy.XPath(".//*[@ClassName='TabItem' or @ClassName='ShellTab']"));
        return tabs.Count;
    }

    public virtual void AssertTabCount(int? expected, string? message = null, int? timeoutMs = null);
    public virtual IReadOnlyList<string> GetTabNames(int? timeoutMs = null);

    #endregion

    #region Selected Tab (Example: GetSelectedTabIndex)

    public virtual int GetSelectedTabIndex(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var tabs = element.FindElements(MobileBy.XPath(".//*[@ClassName='TabItem']"));
        for (int i = 0; i < tabs.Count; i++)
        {
            if (tabs[i].GetAttribute("SelectionItem.IsSelected") == "True")
                return i;
        }
        return -1;
    }

    public virtual void AssertSelectedTabIndex(int? expected, string? message = null, int? timeoutMs = null);
    public virtual string? GetSelectedTabName(int? timeoutMs = null);
    public virtual void AssertSelectedTabName(string? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region Select Tab (Example: SelectTab by index)

    public virtual void SelectTab(int? index, int? timeoutMs = null)
    {
        if (index is null) return;
        Log($"SelectTab({index})");
        var element = FindElementRequired(timeoutMs);
        var tabs = element.FindElements(MobileBy.XPath(".//*[@ClassName='TabItem']"));
        if (index < 0 || index >= tabs.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        tabs[index.Value].Click();
    }

    public virtual void SelectTab(string? name, int? timeoutMs = null);
    public virtual bool WaitTabSelected(int index, int? timeoutMs = null);

    #endregion
}
```

### 1.2 MenuControlBase

```csharp
public abstract class MenuControlBase : ControlObjectBase, IMenuControlObject
{
    protected MenuControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected MenuControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Menu Items (Example: GetMenuItemCount)

    public virtual int GetMenuItemCount(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var items = element.FindElements(MobileBy.XPath(".//*[@ClassName='MenuItem']"));
        return items.Count;
    }

    public virtual IReadOnlyList<string> GetMenuItemNames(int? timeoutMs = null);
    public virtual bool HasMenuItem(string name, int? timeoutMs = null);

    #endregion

    #region Click Menu Item (Example: ClickMenuItem)

    public virtual void ClickMenuItem(string? name, int? timeoutMs = null)
    {
        if (name is null) return;
        Log($"ClickMenuItem({name})");
        var element = FindElementRequired(timeoutMs);
        var item = element.FindElement(MobileBy.XPath($".//*[@ClassName='MenuItem' and @Name='{name}']"));
        item.Click();
    }

    public virtual void ClickMenuItem(int? index, int? timeoutMs = null);

    #endregion

    #region Menu State (Example: IsOpen)

    public virtual bool IsOpen(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        return element?.Displayed ?? false;
    }

    public virtual bool WaitOpen(bool? expected, int? timeoutMs = null);
    public virtual void AssertOpen(bool? expected, string? message = null, int? timeoutMs = null);

    #endregion
}
```

### 1.3 FlyoutControlBase

```csharp
public abstract class FlyoutControlBase : ControlObjectBase, IFlyoutControlObject
{
    protected FlyoutControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected FlyoutControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Flyout State (Example: IsOpen)

    public virtual bool IsOpen(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        return element?.Displayed ?? false;
    }

    public virtual bool WaitOpen(bool? expected, int? timeoutMs = null);
    public virtual void AssertOpen(bool? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region Open/Close (Example: Open)

    public virtual void Open(int? timeoutMs = null)
    {
        Log("Open()");
        if (!IsOpen(timeoutMs))
        {
            // Swipe from left edge or click hamburger menu
            Context.Driver.ExecuteScript("mobile: swipe", new Dictionary<string, object>
            {
                ["startX"] = 0,
                ["startY"] = 300,
                ["endX"] = 200,
                ["endY"] = 300,
                ["duration"] = 300
            });
            WaitOpen(true, timeoutMs);
        }
    }

    public virtual void Close(int? timeoutMs = null);
    public virtual void Toggle(int? timeoutMs = null);

    #endregion

    #region Flyout Items (Example: ClickFlyoutItem)

    public virtual void ClickFlyoutItem(string? name, int? timeoutMs = null)
    {
        if (name is null) return;
        Log($"ClickFlyoutItem({name})");
        Open(timeoutMs);
        var element = FindElementRequired(timeoutMs);
        var item = element.FindElement(MobileBy.XPath($".//*[@Name='{name}']"));
        item.Click();
    }

    public virtual IReadOnlyList<string> GetFlyoutItemNames(int? timeoutMs = null);

    #endregion
}
```

### 1.4 ToolbarControlBase

```csharp
public abstract class ToolbarControlBase : ControlObjectBase, IToolbarControlObject
{
    protected ToolbarControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected ToolbarControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Toolbar Items (Example: GetToolbarItemCount)

    public virtual int GetToolbarItemCount(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var items = element.FindElements(MobileBy.XPath(".//*[@ClassName='ToolbarItem' or @ClassName='Button']"));
        return items.Count;
    }

    public virtual IReadOnlyList<string> GetToolbarItemNames(int? timeoutMs = null);
    public virtual bool HasToolbarItem(string name, int? timeoutMs = null);

    #endregion

    #region Click Toolbar Item (Example: ClickToolbarItem)

    public virtual void ClickToolbarItem(string? name, int? timeoutMs = null)
    {
        if (name is null) return;
        Log($"ClickToolbarItem({name})");
        var element = FindElementRequired(timeoutMs);
        var item = element.FindElement(MobileBy.XPath($".//*[(@ClassName='ToolbarItem' or @ClassName='Button') and @Name='{name}']"));
        item.Click();
    }

    public virtual void ClickToolbarItem(int? index, int? timeoutMs = null);

    #endregion

    #region Toolbar Item State (Example: IsToolbarItemEnabled)

    public virtual bool IsToolbarItemEnabled(string name, int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var item = element.FindElement(MobileBy.XPath($".//*[(@ClassName='ToolbarItem' or @ClassName='Button') and @Name='{name}']"));
        return item.Enabled;
    }

    public virtual void AssertToolbarItemEnabled(string name, bool? expected, string? message = null, int? timeoutMs = null);

    #endregion
}
```

### 1.5 NavigationPageControlBase

```csharp
public abstract class NavigationPageControlBase : ControlObjectBase, INavigationPageControlObject
{
    protected NavigationPageControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected NavigationPageControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Navigation Stack (Example: GetCurrentPageTitle)

    public virtual string? GetCurrentPageTitle(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var title = element.FindElement(MobileBy.XPath(".//*[@ClassName='NavigationBar']//*[@ClassName='TextBlock']"));
        return title?.Text;
    }

    public virtual void AssertCurrentPageTitle(string? expected, string? message = null, int? timeoutMs = null);
    public virtual bool CanGoBack(int? timeoutMs = null);

    #endregion

    #region Navigation Actions (Example: GoBack)

    public virtual void GoBack(int? timeoutMs = null)
    {
        Log("GoBack()");
        var element = FindElementRequired(timeoutMs);
        var backButton = element.FindElement(MobileBy.XPath(".//*[@ClassName='NavigationBar']//*[@ClassName='Button']"));
        backButton.Click();
    }

    public virtual void WaitNavigationComplete(int? timeoutMs = null);

    #endregion
}
```

### 1.6 Concrete MAUI Controls

```csharp
public class TabbedPageControl : TabControlBase
{
    public TabbedPageControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public TabbedPageControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

public class TabBarControl : TabControlBase
{
    public TabBarControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public TabBarControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

public class FlyoutPageControl : FlyoutControlBase
{
    public FlyoutPageControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public FlyoutPageControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

public class ShellControl : FlyoutControlBase
{
    public ShellControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public ShellControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

public class NavigationPageControl : NavigationPageControlBase
{
    public NavigationPageControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public NavigationPageControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

public class ToolbarControl : ToolbarControlBase
{
    public ToolbarControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public ToolbarControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}
```

---

## 2. Blazor Navigation Classes

### 2.1 AsyncTabControlBase

```csharp
public abstract class AsyncTabControlBase : AsyncControlObjectBase
{
    protected AsyncTabControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncTabControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    /// <summary>CSS selector for tab buttons.</summary>
    protected virtual string TabSelector => "[role='tab']";

    #region Tab Count (Example: GetTabCountAsync)

    public virtual async Task<int> GetTabCountAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var tabs = GetLocator().Locator(TabSelector);
        return await tabs.CountAsync();
    }

    public virtual Task<IReadOnlyList<string>> GetTabNamesAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Selected Tab (Example: GetSelectedTabIndexAsync)

    public virtual async Task<int> GetSelectedTabIndexAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var tabs = GetLocator().Locator(TabSelector);
        var count = await tabs.CountAsync();
        for (int i = 0; i < count; i++)
        {
            var tab = tabs.Nth(i);
            var selected = await tab.GetAttributeAsync("aria-selected");
            if (selected == "true") return i;
        }
        return -1;
    }

    public virtual Task AssertSelectedTabIndexAsync(int? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task<string?> GetSelectedTabNameAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Select Tab (Example: SelectTabAsync)

    public virtual async Task SelectTabAsync(int? index, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (index is null) return;
        Log($"SelectTabAsync({index})");
        var tab = GetLocator().Locator(TabSelector).Nth(index.Value);
        await tab.ClickAsync(new() { Timeout = timeoutMs ?? DefaultTimeoutMs });
    }

    public virtual Task SelectTabAsync(string? name, int? timeoutMs = null, CancellationToken ct = default);

    #endregion
}
```

### 2.2 AsyncMenuControlBase

```csharp
public abstract class AsyncMenuControlBase : AsyncControlObjectBase
{
    protected AsyncMenuControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncMenuControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    /// <summary>CSS selector for menu items.</summary>
    protected virtual string MenuItemSelector => "[role='menuitem']";

    #region Menu Items (Example: GetMenuItemCountAsync)

    public virtual async Task<int> GetMenuItemCountAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var items = GetLocator().Locator(MenuItemSelector);
        return await items.CountAsync();
    }

    public virtual Task<IReadOnlyList<string>> GetMenuItemNamesAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Click Menu Item (Example: ClickMenuItemAsync)

    public virtual async Task ClickMenuItemAsync(string? name, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (name is null) return;
        Log($"ClickMenuItemAsync({name})");
        var item = GetLocator().Locator(MenuItemSelector).Filter(new() { HasText = name }).First;
        await item.ClickAsync(new() { Timeout = timeoutMs ?? DefaultTimeoutMs });
    }

    public virtual Task ClickMenuItemAsync(int? index, int? timeoutMs = null, CancellationToken ct = default);

    #endregion
}
```

### 2.3 AsyncNavControlBase

```csharp
public abstract class AsyncNavControlBase : AsyncControlObjectBase
{
    protected AsyncNavControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncNavControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    /// <summary>CSS selector for navigation links.</summary>
    protected virtual string NavLinkSelector => "a, [role='link']";

    #region Navigation Links (Example: GetNavLinkCountAsync)

    public virtual async Task<int> GetNavLinkCountAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var links = GetLocator().Locator(NavLinkSelector);
        return await links.CountAsync();
    }

    public virtual Task<IReadOnlyList<string>> GetNavLinkTextsAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Click Link (Example: ClickNavLinkAsync)

    public virtual async Task ClickNavLinkAsync(string? text, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (text is null) return;
        Log($"ClickNavLinkAsync({text})");
        var link = GetLocator().Locator(NavLinkSelector).Filter(new() { HasText = text }).First;
        await link.ClickAsync(new() { Timeout = timeoutMs ?? DefaultTimeoutMs });
    }

    public virtual Task ClickNavLinkAsync(int? index, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Active Link (Example: GetActiveLinkTextAsync)

    public virtual async Task<string?> GetActiveLinkTextAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var active = GetLocator().Locator($"{NavLinkSelector}.active, {NavLinkSelector}[aria-current='page']").First;
        if (await active.CountAsync() == 0) return null;
        return await active.InnerTextAsync();
    }

    public virtual Task AssertActiveLinkAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion
}
```

### 2.4 Concrete Blazor Controls

```csharp
/// <summary>Tab list with role="tablist".</summary>
public class TabListControl : AsyncTabControlBase
{
    public TabListControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public TabListControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}

/// <summary>Menu with role="menu".</summary>
public class MenuControl : AsyncMenuControlBase
{
    public MenuControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public MenuControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}

/// <summary>HTML nav element.</summary>
public class NavControl : AsyncNavControlBase
{
    public NavControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public NavControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}

/// <summary>Breadcrumb navigation.</summary>
public class BreadcrumbControl : AsyncNavControlBase
{
    public BreadcrumbControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public BreadcrumbControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }

    protected override string NavLinkSelector => "a, [role='link'], li";
}
```

---

## 3. Inheritance Summary

```
MAUI:
TabControlBase : ControlObjectBase, ITabControlObject
├── TabbedPageControl
└── TabBarControl

MenuControlBase : ControlObjectBase, IMenuControlObject

FlyoutControlBase : ControlObjectBase, IFlyoutControlObject
├── FlyoutPageControl
└── ShellControl

ToolbarControlBase : ControlObjectBase, IToolbarControlObject
└── ToolbarControl

NavigationPageControlBase : ControlObjectBase, INavigationPageControlObject
└── NavigationPageControl

Blazor:
AsyncTabControlBase : AsyncControlObjectBase
└── TabListControl

AsyncMenuControlBase : AsyncControlObjectBase
└── MenuControl

AsyncNavControlBase : AsyncControlObjectBase
├── NavControl
└── BreadcrumbControl
```

---

**Next:** [SPEC-006-003b-MEDIA](SPEC-006-003b-MEDIA.md)
