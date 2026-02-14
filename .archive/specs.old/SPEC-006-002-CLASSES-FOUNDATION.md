# SPEC-006-002b: Foundation Classes

**Version:** 1.0  
**Status:** Final  
**Date:** January 2026

---

## 1. ControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for all control objects. Provides core Is/Wait/Check/Assert functionality.
/// </summary>
public abstract class ControlBase : IControlObject
{
    protected readonly ITestContext _context;
    protected readonly ControlLocator _locator;
    protected readonly IPageObject? _page;

    protected ControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
    {
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _page = page;
    }

    #region IControlObject Properties

    public ControlLocator Locator => _locator;
    public IPageObject? Page => _page;
    public string PageName => _page?.PageName ?? "Unknown";
    public string TestName => _context.TestName;
    public ITestLogger? Logger => _context.Logger;

    #endregion

    #region Abstract Element Access

    protected abstract object? FindElement(int? timeoutMs = null);
    protected abstract object? WaitForElementVisible(int? timeoutMs = null);

    #endregion

    #region Logging Helpers

    protected void Log(string message)
    {
        Logger?.Log(TestName, PageName, _locator.ToString(), message);
    }

    protected void LogAction(string action, string? parameter = null, bool success = true)
    {
        var paramStr = parameter != null ? $"(\"{parameter}\")" : "()";
        var statusStr = success ? "" : " [FAILED]";
        Log($"{action}{paramStr}{statusStr}");
        Logger?.LogAction(TestName, PageName, _locator.ToString(), action, parameter);
    }

    protected void LogAssertPass(string assertType, string? actual, string? expected)
    {
        Logger?.LogAssertPass(TestName, PageName, _locator.ToString(), assertType, actual, expected);
    }

    protected void LogWait(string waitType, bool success, int elapsedMs)
    {
        Logger?.LogWait(TestName, $"{_locator}: {waitType}", success, elapsedMs);
    }

    protected void ThrowAssertionFailed(string assertType, string? actual, string? expected, string message)
    {
        Logger?.ThrowAssertionFailed(TestName, PageName, _locator.ToString(), assertType, actual, expected, message, _context);
    }

    protected void ThrowCheckFailed(string checkType, string message)
    {
        Logger?.ThrowCheckFailed(TestName, PageName, _locator.ToString(), checkType, message, _context);
    }

    #endregion

    #region Wait Helpers

    protected int GetTimeout(int? timeoutMs) => timeoutMs ?? _context.DefaultTimeout;

    protected bool WaitUntil(Func<bool> condition, int timeoutMs)
    {
        return _context.WaitFor(condition, timeoutMs);
    }

    #endregion

    #region Is Methods

    // Full implementation for IsExists
    public virtual bool IsExists(int? timeoutMs = null)
    {
        try
        {
            var element = FindElement(timeoutMs);
            var exists = element != null;
            Log($"IsExists: {exists}");
            return exists;
        }
        catch
        {
            Log("IsExists: false (exception)");
            return false;
        }
    }

    // Full implementation for IsVisible
    public virtual bool IsVisible(int? timeoutMs = null)
    {
        try
        {
            var element = FindElement(timeoutMs);
            if (element == null) return false;
            
            var visible = GetVisibleState(element);
            Log($"IsVisible: {visible}");
            return visible;
        }
        catch
        {
            Log("IsVisible: false (exception)");
            return false;
        }
    }

    // Method signatures only
    public abstract bool IsEnabled(int? timeoutMs = null);
    public abstract bool IsFocused(int? timeoutMs = null);
    public virtual string? GetText(int? timeoutMs = null) => null;

    // Abstract helper
    protected abstract bool GetVisibleState(object element);
    protected abstract bool GetEnabledState(object element);

    #endregion

    #region Wait Methods

    // Full implementation for WaitExists
    public virtual bool WaitExists(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return IsExists();
        
        Log($"WaitExists(expected={expected})");
        var timeout = GetTimeout(timeoutMs);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        bool result;
        if (expected.Value)
            result = WaitUntil(() => IsExists(), timeout);
        else
            result = WaitUntil(() => !IsExists(), timeout);
        
        LogWait("Exists", result, (int)stopwatch.ElapsedMilliseconds);
        return result;
    }

    // Full implementation for WaitVisible
    public virtual bool WaitVisible(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return IsVisible();
        
        Log($"WaitVisible(expected={expected})");
        var timeout = GetTimeout(timeoutMs);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        bool result;
        if (expected.Value)
            result = WaitUntil(() => IsVisible(), timeout);
        else
            result = WaitUntil(() => !IsVisible(), timeout);
        
        LogWait("Visible", result, (int)stopwatch.ElapsedMilliseconds);
        return result;
    }

    // Method signatures only
    public abstract bool WaitEnabled(bool? expected, int? timeoutMs = null);
    public abstract bool WaitFocused(bool? expected, int? timeoutMs = null);
    public abstract bool WaitText(string? expected, int? timeoutMs = null);
    public abstract bool WaitTextContains(string? expected, int? timeoutMs = null);

    #endregion

    #region Check Methods

    // Full implementation for CheckExists
    public virtual void CheckExists(bool? expected, int? timeoutMs = null)
    {
        if (expected == null) return;
        
        var actual = IsExists(timeoutMs);
        if (actual != expected.Value)
        {
            ThrowCheckFailed("Exists", 
                $"Expected element '{_locator}' exists={expected.Value} but was {actual}.");
        }
    }

    // Method signatures only
    public abstract void CheckVisible(bool? expected, int? timeoutMs = null);
    public abstract void CheckEnabled(bool? expected, int? timeoutMs = null);

    #endregion

    #region Assert Methods

    // Full implementation for AssertExists
    public virtual void AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return;
        
        var success = WaitExists(expected, timeoutMs);
        if (!success)
        {
            var actual = IsExists();
            ThrowAssertionFailed("Exists", actual.ToString(), expected.Value.ToString(),
                message ?? $"Expected element '{_locator}' exists={expected.Value} but was {actual}.");
        }
        LogAssertPass("Exists", expected.Value.ToString(), expected.Value.ToString());
    }

    // Full implementation for AssertVisible
    public virtual void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null) return;
        
        var success = WaitVisible(expected, timeoutMs);
        if (!success)
        {
            var actual = IsVisible();
            ThrowAssertionFailed("Visible", actual.ToString(), expected.Value.ToString(),
                message ?? $"Expected element '{_locator}' visible={expected.Value} but was {actual}.");
        }
        LogAssertPass("Visible", expected.Value.ToString(), expected.Value.ToString());
    }

    // Method signatures only
    public abstract void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertFocused(bool? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertText(string? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);
    public abstract void AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null);

    #endregion

    #region Precondition Helpers

    protected void EnsureVisible(int? timeoutMs = null)
    {
        if (!WaitVisible(true, timeoutMs))
            ThrowCheckFailed("EnsureVisible", $"Element '{_locator}' not visible.");
    }

    protected void EnsureEnabled(int? timeoutMs = null)
    {
        EnsureVisible(timeoutMs);
        if (!IsEnabled(timeoutMs))
            ThrowCheckFailed("EnsureEnabled", $"Element '{_locator}' not enabled.");
    }

    #endregion
}
```

---

## 2. InteractiveControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for controls that support interaction (click, tap, etc.).
/// </summary>
public abstract class InteractiveControlBase : ControlBase, IInteractiveControlObject
{
    protected InteractiveControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    #region Interaction Methods

    // Full implementation for Click with logging
    public virtual void Click(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        var element = WaitForElementVisible(timeoutMs);
        if (element == null)
            ThrowCheckFailed("Click", $"Element '{_locator}' not visible.");
        
        ClickCore(element!);
        LogAction("Click");
    }

    // Abstract core method
    protected abstract void ClickCore(object element);

    // Method signatures only
    public abstract void DoubleClick(int? timeoutMs = null);
    public abstract void RightClick(int? timeoutMs = null);
    public abstract void LongPress(int? durationMs = null, int? timeoutMs = null);
    public abstract void Tap(int? timeoutMs = null);

    #endregion

    #region Gesture Methods

    public abstract void SwipeLeft(int? timeoutMs = null);
    public abstract void SwipeRight(int? timeoutMs = null);
    public abstract void SwipeUp(int? timeoutMs = null);
    public abstract void SwipeDown(int? timeoutMs = null);
    public abstract void DragTo(IControlObject target, int? timeoutMs = null);
    public abstract void DragTo(int x, int y, int? timeoutMs = null);

    #endregion
}
```

---

## 3. FocusableControlBase (Abstract)

```csharp
namespace Brinell.Core;

/// <summary>
/// Base class for controls that can receive focus.
/// </summary>
public abstract class FocusableControlBase : InteractiveControlBase, IFocusableControlObject
{
    protected FocusableControlBase(ControlLocator locator, IPageObject? page, ITestContext context)
        : base(locator, page, context) { }

    // Full implementation for Focus with logging
    public virtual void Focus(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        var element = WaitForElementVisible(timeoutMs);
        if (element == null)
            ThrowCheckFailed("Focus", $"Element '{_locator}' not visible.");
        
        FocusCore(element!);
        LogAction("Focus");
    }

    // Abstract core method
    protected abstract void FocusCore(object element);

    // Method signatures only
    public abstract void Blur(int? timeoutMs = null);

    // Override IsFocused with implementation
    public override bool IsFocused(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return false;
        
        var focused = GetFocusedState(element);
        Log($"IsFocused: {focused}");
        return focused;
    }

    protected abstract bool GetFocusedState(object element);
}
```

---

## 4. MAUI Implementation

```csharp
namespace Brinell.Maui;

using OpenQA.Selenium.Appium;

/// <summary>
/// MAUI/Appium implementation of ControlBase.
/// </summary>
public class MauiControlBase : ControlBase
{
    protected readonly MauiTestContext _mauiContext;
    protected readonly MauiLocatorResolver _resolver;

    public MauiControlBase(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context)
    {
        _mauiContext = context;
        _resolver = new MauiLocatorResolver();
    }

    #region Element Access

    // Full implementation for FindElement
    protected override object? FindElement(int? timeoutMs = null)
    {
        try
        {
            var by = _resolver.Resolve(_locator);
            
            if (by is ChainedLocator chained)
            {
                var parent = _mauiContext.Driver.FindElement(chained.Parent);
                return parent?.FindElement(chained.Child);
            }
            
            return _mauiContext.Driver.FindElement((OpenQA.Selenium.By)by);
        }
        catch (OpenQA.Selenium.NoSuchElementException)
        {
            return null;
        }
    }

    // Full implementation for WaitForElementVisible
    protected override object? WaitForElementVisible(int? timeoutMs = null)
    {
        var timeout = GetTimeout(timeoutMs);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        while (stopwatch.ElapsedMilliseconds < timeout)
        {
            var element = FindElement() as AppiumElement;
            if (element != null && element.Displayed)
                return element;
            
            Thread.Sleep(_context.PollingInterval);
        }
        
        return null;
    }

    #endregion

    #region State Helpers

    protected override bool GetVisibleState(object element)
    {
        return element is AppiumElement appium && appium.Displayed;
    }

    protected override bool GetEnabledState(object element)
    {
        return element is AppiumElement appium && appium.Enabled;
    }

    #endregion

    #region Is Methods

    public override bool IsEnabled(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        if (element == null) return false;
        var enabled = GetEnabledState(element);
        Log($"IsEnabled: {enabled}");
        return enabled;
    }

    public override bool IsFocused(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs) as AppiumElement;
        if (element == null) return false;
        var focused = element.GetAttribute("focused") == "true";
        Log($"IsFocused: {focused}");
        return focused;
    }

    public override string? GetText(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs) as AppiumElement;
        var text = element?.Text;
        Log($"GetText: '{text}'");
        return text;
    }

    #endregion

    // Method signatures only for remaining
    public override bool WaitEnabled(bool? expected, int? timeoutMs = null);
    public override bool WaitFocused(bool? expected, int? timeoutMs = null);
    public override bool WaitText(string? expected, int? timeoutMs = null);
    public override bool WaitTextContains(string? expected, int? timeoutMs = null);
    public override void CheckVisible(bool? expected, int? timeoutMs = null);
    public override void CheckEnabled(bool? expected, int? timeoutMs = null);
    public override void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null);
    public override void AssertFocused(bool? expected, string? message = null, int? timeoutMs = null);
    public override void AssertText(string? expected, string? message = null, int? timeoutMs = null);
    public override void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);
    public override void AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null);
}

/// <summary>
/// MAUI implementation of InteractiveControlBase.
/// </summary>
public class MauiInteractiveControlBase : MauiControlBase, IInteractiveControlObject
{
    public MauiInteractiveControlBase(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for Click
    public virtual void Click(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        var element = WaitForElementVisible(timeoutMs) as AppiumElement;
        if (element == null)
            ThrowCheckFailed("Click", $"Element '{_locator}' not visible.");
        
        element!.Click();
        LogAction("Click");
    }

    // Method signatures only
    public virtual void DoubleClick(int? timeoutMs = null);
    public virtual void RightClick(int? timeoutMs = null);
    public virtual void LongPress(int? durationMs = null, int? timeoutMs = null);
    public virtual void Tap(int? timeoutMs = null);
    public virtual void SwipeLeft(int? timeoutMs = null);
    public virtual void SwipeRight(int? timeoutMs = null);
    public virtual void SwipeUp(int? timeoutMs = null);
    public virtual void SwipeDown(int? timeoutMs = null);
    public virtual void DragTo(IControlObject target, int? timeoutMs = null);
    public virtual void DragTo(int x, int y, int? timeoutMs = null);
}

/// <summary>
/// MAUI implementation of FocusableControlBase.
/// </summary>
public class MauiFocusableControlBase : MauiInteractiveControlBase, IFocusableControlObject
{
    public MauiFocusableControlBase(ControlLocator locator, IPageObject? page, MauiTestContext context)
        : base(locator, page, context) { }

    // Full implementation for Focus
    public virtual void Focus(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        var element = WaitForElementVisible(timeoutMs) as AppiumElement;
        if (element == null)
            ThrowCheckFailed("Focus", $"Element '{_locator}' not visible.");
        
        element!.Click(); // Clicking focuses on MAUI
        LogAction("Focus");
    }

    // Method signatures only
    public virtual void Blur(int? timeoutMs = null);
}
```

---

## 5. Blazor Implementation

```csharp
namespace Brinell.Blazor;

using Microsoft.Playwright;

/// <summary>
/// Blazor/Playwright implementation of ControlBase.
/// </summary>
public class BlazorControlBase : ControlBase
{
    protected readonly BlazorTestContext _blazorContext;
    protected readonly BlazorLocatorResolver _resolver;

    public BlazorControlBase(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context)
    {
        _blazorContext = context;
        _resolver = new BlazorLocatorResolver();
    }

    #region Element Access

    protected ILocator GetPlaywrightLocator(int? timeoutMs = null)
    {
        var selector = (string)_resolver.Resolve(_locator);
        return _blazorContext.Page.Locator(selector);
    }

    // Full implementation for FindElement
    protected override object? FindElement(int? timeoutMs = null)
    {
        try
        {
            var locator = GetPlaywrightLocator(timeoutMs);
            var count = locator.CountAsync().GetAwaiter().GetResult();
            return count > 0 ? locator : null;
        }
        catch
        {
            return null;
        }
    }

    // Full implementation for WaitForElementVisible
    protected override object? WaitForElementVisible(int? timeoutMs = null)
    {
        try
        {
            var locator = GetPlaywrightLocator(timeoutMs);
            var timeout = GetTimeout(timeoutMs);
            locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeout })
                .GetAwaiter().GetResult();
            return locator;
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

    #endregion

    #region State Helpers

    protected override bool GetVisibleState(object element)
    {
        return element is ILocator locator && locator.IsVisibleAsync().GetAwaiter().GetResult();
    }

    protected override bool GetEnabledState(object element)
    {
        return element is ILocator locator && locator.IsEnabledAsync().GetAwaiter().GetResult();
    }

    #endregion

    #region Is Methods

    public override bool IsEnabled(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var enabled = locator.IsEnabledAsync().GetAwaiter().GetResult();
        Log($"IsEnabled: {enabled}");
        return enabled;
    }

    public override bool IsFocused(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var focused = locator.EvaluateAsync<bool>("el => el === document.activeElement").GetAwaiter().GetResult();
        Log($"IsFocused: {focused}");
        return focused;
    }

    public override string? GetText(int? timeoutMs = null)
    {
        var locator = GetPlaywrightLocator(timeoutMs);
        var text = locator.InnerTextAsync().GetAwaiter().GetResult();
        Log($"GetText: '{text}'");
        return text;
    }

    #endregion

    // Method signatures only for remaining
    public override bool WaitEnabled(bool? expected, int? timeoutMs = null);
    public override bool WaitFocused(bool? expected, int? timeoutMs = null);
    public override bool WaitText(string? expected, int? timeoutMs = null);
    public override bool WaitTextContains(string? expected, int? timeoutMs = null);
    public override void CheckVisible(bool? expected, int? timeoutMs = null);
    public override void CheckEnabled(bool? expected, int? timeoutMs = null);
    public override void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null);
    public override void AssertFocused(bool? expected, string? message = null, int? timeoutMs = null);
    public override void AssertText(string? expected, string? message = null, int? timeoutMs = null);
    public override void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);
    public override void AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null);
}

/// <summary>
/// Blazor implementation of InteractiveControlBase.
/// </summary>
public class BlazorInteractiveControlBase : BlazorControlBase, IInteractiveControlObject
{
    public BlazorInteractiveControlBase(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for Click
    public virtual void Click(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.ClickAsync().GetAwaiter().GetResult();
        LogAction("Click");
    }

    // Method signatures only
    public virtual void DoubleClick(int? timeoutMs = null);
    public virtual void RightClick(int? timeoutMs = null);
    public virtual void LongPress(int? durationMs = null, int? timeoutMs = null);
    public virtual void Tap(int? timeoutMs = null);
    public virtual void SwipeLeft(int? timeoutMs = null);
    public virtual void SwipeRight(int? timeoutMs = null);
    public virtual void SwipeUp(int? timeoutMs = null);
    public virtual void SwipeDown(int? timeoutMs = null);
    public virtual void DragTo(IControlObject target, int? timeoutMs = null);
    public virtual void DragTo(int x, int y, int? timeoutMs = null);
}

/// <summary>
/// Blazor implementation of FocusableControlBase.
/// </summary>
public class BlazorFocusableControlBase : BlazorInteractiveControlBase, IFocusableControlObject
{
    public BlazorFocusableControlBase(ControlLocator locator, IPageObject? page, BlazorTestContext context)
        : base(locator, page, context) { }

    // Full implementation for Focus
    public virtual void Focus(int? timeoutMs = null)
    {
        EnsureEnabled(timeoutMs);
        var locator = GetPlaywrightLocator(timeoutMs);
        locator.FocusAsync().GetAwaiter().GetResult();
        LogAction("Focus");
    }

    // Method signatures only
    public virtual void Blur(int? timeoutMs = null);
}
```

---

**Next:** [SPEC-006-002c: Input Classes](SPEC-006-002-CLASSES-INPUT.md)
