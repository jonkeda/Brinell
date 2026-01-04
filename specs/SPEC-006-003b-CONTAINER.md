# SPEC-006-003b: Container Controls

**Version:** 1.0  
**Status:** Draft  
**Date:** January 2026  
**Parent:** [SPEC-006-003b-INDEX](SPEC-006-003b-INDEX.md)

---

## 1. MAUI Container Classes

### 1.1 ContainerControlBase

```csharp
public abstract class ContainerControlBase : ControlObjectBase, IContainerControlObject
{
    protected ContainerControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected ContainerControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Child Count (Example: GetChildCount)

    public virtual int GetChildCount(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        return element.FindElements(MobileBy.XPath("./*")).Count;
    }

    public virtual void AssertChildCount(int? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region Find Child (Example: FindChild)

    public virtual T FindChild<T>(ControlLocator locator) where T : IControlObject
    {
        return (T)Activator.CreateInstance(typeof(T), Context, locator, Page)!;
    }

    public virtual T FindChild<T>(string automationId) where T : IControlObject;
    public virtual IReadOnlyList<T> FindChildren<T>(ControlLocator locator) where T : IControlObject;

    #endregion
}
```

### 1.2 ScrollViewControlBase

```csharp
public abstract class ScrollViewControlBase : ContainerControlBase, IScrollableControlObject
{
    protected ScrollViewControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected ScrollViewControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Scroll Position (Example: GetScrollPosition)

    public virtual (double horizontal, double vertical) GetScrollPosition(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        var h = double.Parse(element.GetAttribute("Scroll.HorizontalScrollPercent") ?? "0");
        var v = double.Parse(element.GetAttribute("Scroll.VerticalScrollPercent") ?? "0");
        return (h, v);
    }

    public virtual bool CanScrollHorizontally(int? timeoutMs = null);
    public virtual bool CanScrollVertically(int? timeoutMs = null);

    #endregion

    #region Scroll Actions (Example: ScrollTo)

    public virtual void ScrollTo(double? horizontalPercent, double? verticalPercent, int? timeoutMs = null)
    {
        Log($"ScrollTo({horizontalPercent}, {verticalPercent})");
        var element = FindElementRequired(timeoutMs);
        // Implementation uses scroll pattern or swipe gestures
    }

    public virtual void ScrollToTop(int? timeoutMs = null);
    public virtual void ScrollToBottom(int? timeoutMs = null);
    public virtual void ScrollToLeft(int? timeoutMs = null);
    public virtual void ScrollToRight(int? timeoutMs = null);
    public virtual void ScrollUp(double? amount = null, int? timeoutMs = null);
    public virtual void ScrollDown(double? amount = null, int? timeoutMs = null);
    public virtual void ScrollLeft(double? amount = null, int? timeoutMs = null);
    public virtual void ScrollRight(double? amount = null, int? timeoutMs = null);

    #endregion

    #region Scroll To Element (Example: ScrollToElement)

    public virtual void ScrollToElement(IControlObject? control, int? timeoutMs = null)
    {
        if (control is null) return;
        Log($"ScrollToElement({control})");
        // Scroll until element is visible
    }

    public virtual bool WaitScrollComplete(int? timeoutMs = null);

    #endregion
}
```

### 1.3 ExpanderControlBase

```csharp
public abstract class ExpanderControlBase : ContainerControlBase, IExpandableControlObject
{
    protected ExpanderControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected ExpanderControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Expanded State (Example: IsExpanded)

    public virtual bool IsExpanded(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        return element.GetAttribute("ExpandCollapse.ExpandCollapseState") == "Expanded";
    }

    public virtual bool WaitExpanded(bool? expected, int? timeoutMs = null);
    public virtual void AssertExpanded(bool? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region Expand/Collapse (Example: Expand)

    public virtual void Expand(int? timeoutMs = null)
    {
        Log("Expand()");
        if (!IsExpanded(timeoutMs))
        {
            ClickHeader(timeoutMs);
            WaitExpanded(true, timeoutMs);
        }
    }

    public virtual void Collapse(int? timeoutMs = null);
    public virtual void Toggle(int? timeoutMs = null);

    #endregion

    #region Header (Example: GetHeaderText)

    public virtual string GetHeaderText(int? timeoutMs = null)
    {
        var header = FindHeaderElement(timeoutMs);
        return header?.Text ?? string.Empty;
    }

    protected virtual void ClickHeader(int? timeoutMs = null);
    protected virtual AppiumElement? FindHeaderElement(int? timeoutMs = null);

    #endregion
}
```

### 1.4 RefreshViewControlBase

```csharp
public abstract class RefreshViewControlBase : ContainerControlBase, IRefreshableControlObject
{
    protected RefreshViewControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected RefreshViewControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Refreshing State (Example: IsRefreshing)

    public virtual bool IsRefreshing(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        return element.GetAttribute("IsRefreshing") == "True";
    }

    public virtual bool WaitRefreshing(bool? expected, int? timeoutMs = null);
    public virtual void AssertRefreshing(bool? expected, string? message = null, int? timeoutMs = null);

    #endregion

    #region Refresh Action (Example: Refresh)

    public virtual void Refresh(int? timeoutMs = null)
    {
        Log("Refresh()");
        var element = FindElementRequired(timeoutMs);
        // Pull-to-refresh gesture
        var size = element.Size;
        Context.Driver.ExecuteScript("mobile: swipe", new Dictionary<string, object>
        {
            ["startX"] = size.Width / 2,
            ["startY"] = 100,
            ["endX"] = size.Width / 2,
            ["endY"] = size.Height / 2,
            ["duration"] = 500
        });
    }

    public virtual void WaitRefreshComplete(int? timeoutMs = null);

    #endregion
}
```

### 1.5 SwipeViewControlBase

```csharp
public abstract class SwipeViewControlBase : ContainerControlBase, ISwipeableControlObject
{
    protected SwipeViewControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected SwipeViewControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #region Swipe Actions (Example: SwipeLeft)

    public virtual void SwipeLeft(int? timeoutMs = null)
    {
        Log("SwipeLeft()");
        var element = FindElementRequired(timeoutMs);
        var size = element.Size;
        Context.Driver.ExecuteScript("mobile: swipe", new Dictionary<string, object>
        {
            ["startX"] = size.Width * 0.8,
            ["startY"] = size.Height / 2,
            ["endX"] = size.Width * 0.2,
            ["endY"] = size.Height / 2,
            ["duration"] = 300
        });
    }

    public virtual void SwipeRight(int? timeoutMs = null);
    public virtual void SwipeUp(int? timeoutMs = null);
    public virtual void SwipeDown(int? timeoutMs = null);

    #endregion

    #region Swipe State (Example: IsLeftSwipeRevealed)

    public virtual bool IsLeftSwipeRevealed(int? timeoutMs = null)
    {
        // Check if left swipe items are visible
        var element = FindElementRequired(timeoutMs);
        var leftItems = element.FindElements(MobileBy.XPath(".//*[@AutomationId='LeftSwipeItems']"));
        return leftItems.Count > 0 && leftItems[0].Displayed;
    }

    public virtual bool IsRightSwipeRevealed(int? timeoutMs = null);
    public virtual void CloseSwipe(int? timeoutMs = null);

    #endregion
}
```

### 1.6 Concrete MAUI Controls

```csharp
public class ScrollViewControl : ScrollViewControlBase
{
    public ScrollViewControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public ScrollViewControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

public class ExpanderControl : ExpanderControlBase
{
    public ExpanderControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public ExpanderControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

public class RefreshViewControl : RefreshViewControlBase
{
    public RefreshViewControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public RefreshViewControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

public class SwipeViewControl : SwipeViewControlBase
{
    public SwipeViewControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public SwipeViewControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

public class FrameControl : ContainerControlBase
{
    public FrameControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public FrameControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

public class BorderControl : ContainerControlBase
{
    public BorderControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public BorderControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}
```

---

## 2. Blazor Container Classes

### 2.1 AsyncContainerControlBase

```csharp
public abstract class AsyncContainerControlBase : AsyncControlObjectBase
{
    protected AsyncContainerControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncContainerControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    #region Child Count (Example: GetChildCountAsync)

    public virtual async Task<int> GetChildCountAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var children = GetLocator().Locator("> *");
        return await children.CountAsync();
    }

    public virtual Task AssertChildCountAsync(int? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Find Child (Example: FindChild)

    public virtual T FindChild<T>(ControlLocator locator) where T : class
    {
        return (T)Activator.CreateInstance(typeof(T), Context, locator, Page)!;
    }

    public virtual T FindChild<T>(string testId) where T : class;

    #endregion
}
```

### 2.2 AsyncScrollableControlBase

```csharp
public abstract class AsyncScrollableControlBase : AsyncContainerControlBase
{
    protected AsyncScrollableControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncScrollableControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    #region Scroll Actions (Example: ScrollToAsync)

    public virtual async Task ScrollToAsync(double? top = null, double? left = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        Log($"ScrollToAsync({top}, {left})");
        var element = GetLocator();
        if (top.HasValue)
            await element.EvaluateAsync($"e => e.scrollTop = {top.Value}");
        if (left.HasValue)
            await element.EvaluateAsync($"e => e.scrollLeft = {left.Value}");
    }

    public virtual Task ScrollToTopAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task ScrollToBottomAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task ScrollIntoViewAsync(ILocator element, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Scroll Position (Example: GetScrollPositionAsync)

    public virtual async Task<(double top, double left)> GetScrollPositionAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var element = GetLocator();
        var top = await element.EvaluateAsync<double>("e => e.scrollTop");
        var left = await element.EvaluateAsync<double>("e => e.scrollLeft");
        return (top, left);
    }

    #endregion
}
```

### 2.3 AsyncExpandableControlBase

```csharp
public abstract class AsyncExpandableControlBase : AsyncContainerControlBase
{
    protected AsyncExpandableControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncExpandableControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    /// <summary>CSS selector for the toggle button.</summary>
    protected virtual string ToggleSelector => "button, [role='button']";

    /// <summary>Attribute or class indicating expanded state.</summary>
    protected virtual string ExpandedIndicator => "[aria-expanded='true']";

    #region Expanded State (Example: IsExpandedAsync)

    public virtual async Task<bool> IsExpandedAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var toggle = GetLocator().Locator(ToggleSelector).First;
        var expanded = await toggle.GetAttributeAsync("aria-expanded");
        return expanded == "true";
    }

    public virtual Task AssertExpandedAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default);

    #endregion

    #region Expand/Collapse (Example: ExpandAsync)

    public virtual async Task ExpandAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("ExpandAsync()");
        if (!await IsExpandedAsync(timeoutMs, ct))
        {
            var toggle = GetLocator().Locator(ToggleSelector).First;
            await toggle.ClickAsync(new() { Timeout = timeoutMs ?? DefaultTimeoutMs });
        }
    }

    public virtual Task CollapseAsync(int? timeoutMs = null, CancellationToken ct = default);
    public virtual Task ToggleAsync(int? timeoutMs = null, CancellationToken ct = default);

    #endregion
}
```

### 2.4 Concrete Blazor Controls

```csharp
/// <summary>HTML div with overflow scroll.</summary>
public class ScrollableContainerControl : AsyncScrollableControlBase
{
    public ScrollableContainerControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public ScrollableContainerControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}

/// <summary>HTML details element or accordion pattern.</summary>
public class DetailsControl : AsyncExpandableControlBase
{
    public DetailsControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public DetailsControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }

    protected override string ToggleSelector => "summary";

    public override async Task<bool> IsExpandedAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().GetAttributeAsync("open") is not null;
    }
}

/// <summary>Generic container div/section.</summary>
public class ContainerControl : AsyncContainerControlBase
{
    public ContainerControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public ContainerControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}
```

---

## 3. Inheritance Summary

```
MAUI:
ContainerControlBase : ControlObjectBase, IContainerControlObject
├── ScrollViewControlBase : IScrollableControlObject
│   └── ScrollViewControl
├── ExpanderControlBase : IExpandableControlObject
│   └── ExpanderControl
├── RefreshViewControlBase : IRefreshableControlObject
│   └── RefreshViewControl
├── SwipeViewControlBase : ISwipeableControlObject
│   └── SwipeViewControl
├── FrameControl
└── BorderControl

Blazor:
AsyncContainerControlBase : AsyncControlObjectBase
├── AsyncScrollableControlBase
│   └── ScrollableContainerControl
├── AsyncExpandableControlBase
│   └── DetailsControl
└── ContainerControl
```

---

**Next:** [SPEC-006-003b-DISPLAY](SPEC-006-003b-DISPLAY.md)
