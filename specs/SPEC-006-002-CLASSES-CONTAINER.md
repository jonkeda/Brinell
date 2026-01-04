# SPEC-006-002i: Container Classes

**Version:** 1.0  
**Status:** Final  
**Date:** January 2026

---

## 1. ContainerControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class ContainerControlBase : ControlBase, IContainerControlObject
{
    protected ContainerControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for FindChild with logging
    public virtual T FindChild<T>(ControlLocator locator) where T : IControlObject
    {
        Log($"FindChild<{typeof(T).Name}>({locator})");
        return CreateChild<T>(locator);
    }

    // Full implementation for FindChildren
    public virtual IReadOnlyList<T> FindChildren<T>(ControlLocator locator) where T : IControlObject
    {
        Log($"FindChildren<{typeof(T).Name}>({locator})");
        return FindChildrenCore<T>(locator);
    }

    // Abstract core methods
    protected abstract T CreateChild<T>(ControlLocator locator) where T : IControlObject;
    protected abstract IReadOnlyList<T> FindChildrenCore<T>(ControlLocator locator) where T : IControlObject;

    // Method signatures only
    public abstract IControlObject FindChildByText(string? text, int? timeoutMs = null);
    public abstract IReadOnlyList<IControlObject> GetAllChildren(int? timeoutMs = null);
    public abstract int GetChildCount(int? timeoutMs = null);
    public abstract bool HasChild(ControlLocator locator, int? timeoutMs = null);
    public abstract bool WaitChildExists(ControlLocator locator, bool? expected, int? timeoutMs = null);
    public abstract bool WaitChildCount(int? expected, int? timeoutMs = null);
    public abstract void AssertChildExists(ControlLocator locator, bool? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertChildCount(int? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 2. ScrollableControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class ScrollableControlBase : ContainerControlBase, IScrollableControlObject
{
    protected ScrollableControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for ScrollTo with logging
    public virtual void ScrollTo(ScrollDirection direction, double amount, int? timeoutMs = null)
    {
        EnsureVisible(timeoutMs);
        LogAction("ScrollTo", $"{direction}, {amount}");
        ScrollToCore(direction, amount);
    }

    // Abstract core method
    protected abstract void ScrollToCore(ScrollDirection direction, double amount);

    // Method signatures only
    public abstract void ScrollToTop(int? timeoutMs = null);
    public abstract void ScrollToBottom(int? timeoutMs = null);
    public abstract void ScrollToLeft(int? timeoutMs = null);
    public abstract void ScrollToRight(int? timeoutMs = null);
    public abstract void ScrollToElement(IControlObject control, int? timeoutMs = null);
    public abstract void ScrollToElement(ControlLocator locator, int? timeoutMs = null);
    public abstract double GetScrollPositionX(int? timeoutMs = null);
    public abstract double GetScrollPositionY(int? timeoutMs = null);
    public abstract bool CanScrollHorizontal(int? timeoutMs = null);
    public abstract bool CanScrollVertical(int? timeoutMs = null);
    public abstract bool WaitScrollPosition(double? expectedX, double? expectedY, int? timeoutMs = null);
    public abstract void AssertScrollPosition(double? expectedX, double? expectedY, string? message = null, int? timeoutMs = null);
}

public enum ScrollDirection
{
    Up,
    Down,
    Left,
    Right
}
```

---

## 3. ExpanderControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class ExpanderControlBase : ContainerControlBase, IExpanderControlObject
{
    protected ExpanderControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for IsExpanded
    public virtual bool IsExpanded(int? timeoutMs = null)
    {
        var element = WaitForElementVisible(timeoutMs);
        if (element == null) return false;
        
        var expanded = GetExpandedState(element);
        Log($"IsExpanded: {expanded}");
        return expanded;
    }

    // Full implementation for Expand with logging
    public virtual void Expand(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        if (!IsExpanded(timeoutMs))
        {
            ToggleExpanded();
            LogAction("Expand");
        }
    }

    // Abstract core methods
    protected abstract bool GetExpandedState(object element);
    protected abstract void ToggleExpanded();

    // Method signatures only
    public abstract void Collapse(int? timeoutMs = null);
    public abstract void Toggle(int? timeoutMs = null);
    public abstract bool WaitExpanded(bool? expected, int? timeoutMs = null);
    public abstract void AssertExpanded(bool? expected, string? message = null, int? timeoutMs = null);
    public abstract string? GetHeader(int? timeoutMs = null);
    public abstract IControlObject? GetHeaderControl(int? timeoutMs = null);
    public abstract IControlObject? GetContent(int? timeoutMs = null);
}
```

---

## 4. GroupControlBase (Abstract)

```csharp
namespace Brinell.Core;

public abstract class GroupControlBase : ContainerControlBase, IGroupControlObject
{
    protected GroupControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Method signatures only
    public abstract string? GetTitle(int? timeoutMs = null);
    public abstract bool WaitTitle(string? expected, int? timeoutMs = null);
    public abstract void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);
}
```

---

## 5. MAUI Implementation

```csharp
namespace Brinell.Maui;

public class MauiScrollView : MauiControlBase, IScrollableControlObject
{
    public MauiScrollView(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for ScrollTo
    public void ScrollTo(ScrollDirection direction, double amount, int? timeoutMs = null)
    {
        EnsureVisible(timeoutMs);
        LogAction("ScrollTo", $"{direction}, {amount}");
        
        var element = WaitForElementVisible(timeoutMs);
        if (element == null)
            ThrowCheckFailed("ScrollTo", $"Element '{Locator}' not visible.");
        
        var actions = new OpenQA.Selenium.Appium.TouchAction(_context.Driver);
        var size = element!.Size;
        var center = element.Location;
        center.X += size.Width / 2;
        center.Y += size.Height / 2;
        
        int endX = center.X, endY = center.Y;
        var scrollAmount = (int)(size.Height * amount);
        
        switch (direction)
        {
            case ScrollDirection.Up: endY += scrollAmount; break;
            case ScrollDirection.Down: endY -= scrollAmount; break;
            case ScrollDirection.Left: endX += scrollAmount; break;
            case ScrollDirection.Right: endX -= scrollAmount; break;
        }
        
        actions.Press(center.X, center.Y).MoveTo(endX, endY).Release().Perform();
    }

    // Method signatures only
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
    public void ScrollToTop(int? timeoutMs = null);
    public void ScrollToBottom(int? timeoutMs = null);
    public void ScrollToLeft(int? timeoutMs = null);
    public void ScrollToRight(int? timeoutMs = null);
    public void ScrollToElement(IControlObject control, int? timeoutMs = null);
    public void ScrollToElement(ControlLocator locator, int? timeoutMs = null);
    public double GetScrollPositionX(int? timeoutMs = null);
    public double GetScrollPositionY(int? timeoutMs = null);
    public bool CanScrollHorizontal(int? timeoutMs = null);
    public bool CanScrollVertical(int? timeoutMs = null);
    public bool WaitScrollPosition(double? expectedX, double? expectedY, int? timeoutMs = null);
    public void AssertScrollPosition(double? expectedX, double? expectedY, string? message = null, int? timeoutMs = null);
}

public class MauiExpander : MauiControlBase, IExpanderControlObject
{
    public MauiExpander(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Method signatures only
    public bool IsExpanded(int? timeoutMs = null);
    public void Expand(int? timeoutMs = null);
    public void Collapse(int? timeoutMs = null);
    public void Toggle(int? timeoutMs = null);
    public bool WaitExpanded(bool? expected, int? timeoutMs = null);
    public void AssertExpanded(bool? expected, string? message = null, int? timeoutMs = null);
    public string? GetHeader(int? timeoutMs = null);
    public IControlObject? GetHeaderControl(int? timeoutMs = null);
    public IControlObject? GetContent(int? timeoutMs = null);
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

public class MauiFrame : MauiControlBase, IGroupControlObject
{
    public MauiFrame(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Method signatures only
    public string? GetTitle(int? timeoutMs = null);
    public bool WaitTitle(string? expected, int? timeoutMs = null);
    public void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);
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

public class BlazorScrollContainer : BlazorControlBase, IScrollableControlObject
{
    public BlazorScrollContainer(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for ScrollTo
    public void ScrollTo(ScrollDirection direction, double amount, int? timeoutMs = null)
    {
        EnsureVisible(timeoutMs);
        LogAction("ScrollTo", $"{direction}, {amount}");
        
        var locator = GetPlaywrightLocator(timeoutMs);
        var box = locator.BoundingBoxAsync().GetAwaiter().GetResult();
        if (box == null)
            ThrowCheckFailed("ScrollTo", $"Element '{Locator}' has no bounding box.");
        
        int scrollX = 0, scrollY = 0;
        var scrollAmount = (int)(box.Height * amount);
        
        switch (direction)
        {
            case ScrollDirection.Up: scrollY = -scrollAmount; break;
            case ScrollDirection.Down: scrollY = scrollAmount; break;
            case ScrollDirection.Left: scrollX = -scrollAmount; break;
            case ScrollDirection.Right: scrollX = scrollAmount; break;
        }
        
        locator.EvaluateAsync($"e => e.scrollBy({scrollX}, {scrollY})").GetAwaiter().GetResult();
    }

    // Method signatures only
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
    public void ScrollToTop(int? timeoutMs = null);
    public void ScrollToBottom(int? timeoutMs = null);
    public void ScrollToLeft(int? timeoutMs = null);
    public void ScrollToRight(int? timeoutMs = null);
    public void ScrollToElement(IControlObject control, int? timeoutMs = null);
    public void ScrollToElement(ControlLocator locator, int? timeoutMs = null);
    public double GetScrollPositionX(int? timeoutMs = null);
    public double GetScrollPositionY(int? timeoutMs = null);
    public bool CanScrollHorizontal(int? timeoutMs = null);
    public bool CanScrollVertical(int? timeoutMs = null);
    public bool WaitScrollPosition(double? expectedX, double? expectedY, int? timeoutMs = null);
    public void AssertScrollPosition(double? expectedX, double? expectedY, string? message = null, int? timeoutMs = null);
}

public class BlazorAccordion : BlazorControlBase, IExpanderControlObject
{
    public BlazorAccordion(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Method signatures only
    public bool IsExpanded(int? timeoutMs = null);
    public void Expand(int? timeoutMs = null);
    public void Collapse(int? timeoutMs = null);
    public void Toggle(int? timeoutMs = null);
    public bool WaitExpanded(bool? expected, int? timeoutMs = null);
    public void AssertExpanded(bool? expected, string? message = null, int? timeoutMs = null);
    public string? GetHeader(int? timeoutMs = null);
    public IControlObject? GetHeaderControl(int? timeoutMs = null);
    public IControlObject? GetContent(int? timeoutMs = null);
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

public class BlazorCard : BlazorControlBase, IGroupControlObject
{
    public BlazorCard(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Method signatures only
    public string? GetTitle(int? timeoutMs = null);
    public bool WaitTitle(string? expected, int? timeoutMs = null);
    public void AssertTitle(string? expected, string? message = null, int? timeoutMs = null);
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

**Next:** [SPEC-006-002j: Display Classes](SPEC-006-002-CLASSES-DISPLAY.md)
