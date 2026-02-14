# SPEC-006-003b: Foundation Base Classes

**Version:** 1.0  
**Status:** Draft  
**Date:** January 2026  
**Parent:** [SPEC-006-003b-INDEX](SPEC-006-003b-INDEX.md)

---

## Overview

This document defines the foundation base classes that all controls inherit from:
- `ControlObjectBase` - Core functionality for all controls
- `ClickableControlBase` - For controls that can be clicked
- `TextControlBase` - For controls that accept text input

---

## 1. MAUI Foundation Classes

### 1.1 ControlObjectBase

The root base class for all MAUI controls.

```csharp
public abstract class ControlObjectBase : IInteractiveControlObject
{
    protected readonly MauiTestContext Context;
    
    public ControlLocator Locator { get; }
    public IPageObject? Page { get; }

    #region Constructors

    /// <summary>Primary constructor with explicit locator.</summary>
    protected ControlObjectBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Locator = locator ?? throw new ArgumentNullException(nameof(locator));
        Page = page;
    }

    /// <summary>Convenience constructor - uses AutomationId strategy.</summary>
    protected ControlObjectBase(MauiTestContext context, string automationId, IPageObject? page)
        : this(context, By.AutomationId(automationId), page)
    {
    }

    #endregion

    #region Logging

    /// <summary>Logs a message with control context.</summary>
    protected void Log(string message)
    {
        Context.Log($"[{GetType().Name}] {Locator}: {message}");
    }

    #endregion

    #region Element Access

    protected WindowsDriver Driver => Context.Driver;
    protected int DefaultTimeoutMs => Context.DefaultTimeoutMs;

    /// <summary>Finds the element, returns null if not found.</summary>
    protected virtual AppiumElement? FindElement(int? timeoutMs = null)
    {
        try
        {
            return ConvertLocatorToAppium(Locator);
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    /// <summary>Finds the element, throws if not found.</summary>
    protected virtual AppiumElement FindElementRequired(int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var element = WaitForElement(timeout);
        if (element == null)
        {
            throw new ElementNotFoundException(Locator, GetType().Name, timeout);
        }
        return element;
    }

    /// <summary>Waits for element to appear.</summary>
    protected virtual AppiumElement? WaitForElement(int timeoutMs)
    {
        var wait = new WebDriverWait(Driver, TimeSpan.FromMilliseconds(timeoutMs));
        try
        {
            return wait.Until(_ => FindElement());
        }
        catch (WebDriverTimeoutException)
        {
            return null;
        }
    }

    /// <summary>Converts ControlLocator to Appium locator.</summary>
    protected virtual AppiumElement ConvertLocatorToAppium(ControlLocator locator)
    {
        return locator.Strategy switch
        {
            LocatorStrategy.AutomationId => Driver.FindElement(MobileBy.AccessibilityId(locator.Value)),
            LocatorStrategy.Name => Driver.FindElement(MobileBy.Name(locator.Value)),
            LocatorStrategy.Id => Driver.FindElement(MobileBy.Id(locator.Value)),
            LocatorStrategy.ClassName => Driver.FindElement(MobileBy.ClassName(locator.Value)),
            LocatorStrategy.XPath => Driver.FindElement(MobileBy.XPath(locator.Value)),
            LocatorStrategy.AccessibilityId => Driver.FindElement(MobileBy.AccessibilityId(locator.Value)),
            _ => throw new LocatorNotFoundException(locator.Strategy, locator.Value)
        };
    }

    #endregion

    #region Is Methods

    public virtual bool IsExists(int? timeoutMs = null)
    {
        return FindElement(timeoutMs) != null;
    }

    public virtual bool IsVisible(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        return element?.Displayed ?? false;
    }

    public virtual bool IsEnabled(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        return element?.Enabled ?? false;
    }

    public virtual string GetText(int? timeoutMs = null)
    {
        var element = FindElementRequired(timeoutMs);
        return element.Text ?? string.Empty;
    }

    #endregion

    #region Wait Methods

    public virtual bool WaitExists(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var wait = new WebDriverWait(Driver, TimeSpan.FromMilliseconds(timeout));
        try
        {
            return wait.Until(_ => IsExists() == expected.Value);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public virtual bool WaitVisible(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var wait = new WebDriverWait(Driver, TimeSpan.FromMilliseconds(timeout));
        try
        {
            return wait.Until(_ => IsVisible() == expected.Value);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public virtual bool WaitEnabled(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var wait = new WebDriverWait(Driver, TimeSpan.FromMilliseconds(timeout));
        try
        {
            return wait.Until(_ => IsEnabled() == expected.Value);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    #endregion

    #region Check Methods

    public virtual void CheckExists(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return;
        if (!WaitExists(expected, timeoutMs))
        {
            throw new UITestTimeoutException(
                $"Control {Locator} exists={IsExists()} but expected={expected}",
                timeoutMs ?? DefaultTimeoutMs);
        }
    }

    public virtual void CheckVisible(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return;
        if (!WaitVisible(expected, timeoutMs))
        {
            throw new UITestTimeoutException(
                $"Control {Locator} visible={IsVisible()} but expected={expected}",
                timeoutMs ?? DefaultTimeoutMs);
        }
    }

    public virtual void CheckEnabled(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return;
        if (!WaitEnabled(expected, timeoutMs))
        {
            throw new UITestTimeoutException(
                $"Control {Locator} enabled={IsEnabled()} but expected={expected}",
                timeoutMs ?? DefaultTimeoutMs);
        }
    }

    #endregion

    #region Assert Methods

    public virtual void AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        var actual = WaitExists(expected, timeoutMs) ? expected.Value : IsExists();
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected exists={expected} but was {actual}",
                Locator, expected, actual);
        }
    }

    public virtual void AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        var actual = WaitVisible(expected, timeoutMs) ? expected.Value : IsVisible();
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected visible={expected} but was {actual}",
                Locator, expected, actual);
        }
    }

    public virtual void AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        var actual = WaitEnabled(expected, timeoutMs) ? expected.Value : IsEnabled();
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected enabled={expected} but was {actual}",
                Locator, expected, actual);
        }
    }

    public virtual void AssertText(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        var actual = GetText(timeoutMs);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected text='{expected}' but was '{actual}'",
                Locator, expected, actual);
        }
    }

    public virtual void AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        var actual = GetText(timeoutMs);
        if (!actual.Contains(expected))
        {
            throw new AssertionException(
                message ?? $"Expected text to contain '{expected}' but was '{actual}'",
                Locator, expected, actual);
        }
    }

    public virtual void AssertTextStartsWith(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        var actual = GetText(timeoutMs);
        if (!actual.StartsWith(expected))
        {
            throw new AssertionException(
                message ?? $"Expected text to start with '{expected}' but was '{actual}'",
                Locator, expected, actual);
        }
    }

    public virtual void AssertTextEndsWith(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        var actual = GetText(timeoutMs);
        if (!actual.EndsWith(expected))
        {
            throw new AssertionException(
                message ?? $"Expected text to end with '{expected}' but was '{actual}'",
                Locator, expected, actual);
        }
    }

    public virtual void AssertTextMatches(string? pattern, string? message = null, int? timeoutMs = null)
    {
        if (pattern is null) return;
        var actual = GetText(timeoutMs);
        if (!System.Text.RegularExpressions.Regex.IsMatch(actual, pattern))
        {
            throw new AssertionException(
                message ?? $"Expected text to match '{pattern}' but was '{actual}'",
                Locator, pattern, actual);
        }
    }

    public virtual void AssertTextEmpty(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        var actual = string.IsNullOrEmpty(GetText(timeoutMs));
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected text empty={expected} but was {actual}",
                Locator, expected, actual);
        }
    }

    #endregion
}
```

---

### 1.2 ClickableControlBase

Base for all clickable controls.

```csharp
public abstract class ClickableControlBase : ControlObjectBase, IClickableControlObject
{
    #region Constructors

    protected ClickableControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected ClickableControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #endregion

    #region Click Actions

    public virtual void Click(int? timeoutMs = null)
    {
        Log("Click()");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        FindElementRequired(timeoutMs).Click();
    }

    public virtual void DoubleClick(int? timeoutMs = null)
    {
        Log("DoubleClick()");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        var element = FindElementRequired(timeoutMs);
        new Actions(Driver).DoubleClick(element).Perform();
    }

    public virtual void RightClick(int? timeoutMs = null)
    {
        Log("RightClick()");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        var element = FindElementRequired(timeoutMs);
        new Actions(Driver).ContextClick(element).Perform();
    }

    public virtual void Hover(int? timeoutMs = null)
    {
        Log("Hover()");
        CheckVisible(true, timeoutMs);
        var element = FindElementRequired(timeoutMs);
        new Actions(Driver).MoveToElement(element).Perform();
    }

    public virtual void LongPress(int? durationMs = null, int? timeoutMs = null)
    {
        var duration = durationMs ?? 1000;
        Log($"LongPress(duration={duration}ms)");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        
        var element = FindElementRequired(timeoutMs);
        var location = element.Location;
        var size = element.Size;
        var centerX = location.X + size.Width / 2;
        var centerY = location.Y + size.Height / 2;

        // W3C Actions for long press
        var finger = new PointerInputDevice(PointerKind.Touch, "finger");
        var sequence = new ActionSequence(finger);
        sequence.AddAction(finger.CreatePointerMove(element, 0, 0, TimeSpan.Zero));
        sequence.AddAction(finger.CreatePointerDown(MouseButton.Left));
        sequence.AddAction(finger.CreatePause(TimeSpan.FromMilliseconds(duration)));
        sequence.AddAction(finger.CreatePointerUp(MouseButton.Left));
        Driver.PerformActions(new List<ActionSequence> { sequence });
    }

    #endregion
}
```

---

### 1.3 TextControlBase

Base for all text input controls.

```csharp
public abstract class TextControlBase : ClickableControlBase, ITextControlObject, IFocusableControlObject
{
    #region Constructors

    protected TextControlBase(MauiTestContext context, ControlLocator locator, IPageObject? page)
        : base(context, locator, page) { }

    protected TextControlBase(MauiTestContext context, string automationId, IPageObject? page)
        : base(context, automationId, page) { }

    #endregion

    #region Focus

    public virtual bool IsFocused(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        return element?.GetAttribute("focused") == "true" ||
               element?.GetAttribute("HasFocus") == "True";
    }

    public virtual bool WaitFocused(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var wait = new WebDriverWait(Driver, TimeSpan.FromMilliseconds(timeout));
        try
        {
            return wait.Until(_ => IsFocused() == expected.Value);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public virtual void CheckFocused(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return;
        if (!WaitFocused(expected, timeoutMs))
        {
            throw new UITestTimeoutException(
                $"Control {Locator} focused={IsFocused()} but expected={expected}",
                timeoutMs ?? DefaultTimeoutMs);
        }
    }

    public virtual void AssertFocused(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        var actual = WaitFocused(expected, timeoutMs) ? expected.Value : IsFocused();
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected focused={expected} but was {actual}",
                Locator, expected, actual);
        }
    }

    public virtual void Focus(int? timeoutMs = null)
    {
        Log("Focus()");
        Click(timeoutMs); // Clicking focuses the control
    }

    public virtual void Blur(int? timeoutMs = null)
    {
        Log("Blur()");
        var element = FindElementRequired(timeoutMs);
        element.SendKeys(Keys.Tab); // Tab out to blur
    }

    #endregion

    #region Text Input

    public virtual void Enter(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        Log($"Enter(\"{text}\")");
        CheckVisible(true, timeoutMs);
        CheckEnabled(true, timeoutMs);
        var element = FindElementRequired(timeoutMs);
        element.Clear();
        element.SendKeys(text);
    }

    public virtual void Clear(int? timeoutMs = null)
    {
        Log("Clear()");
        CheckVisible(true, timeoutMs);
        FindElementRequired(timeoutMs).Clear();
    }

    public virtual void ClearAndEnter(string? text, int? timeoutMs = null)
    {
        Log($"ClearAndEnter(\"{text}\")");
        Clear(timeoutMs);
        if (text is not null)
        {
            FindElementRequired(timeoutMs).SendKeys(text);
        }
    }

    public virtual void Append(string? text, int? timeoutMs = null)
    {
        if (text is null) return;
        Log($"Append(\"{text}\")");
        CheckVisible(true, timeoutMs);
        FindElementRequired(timeoutMs).SendKeys(text);
    }

    #endregion

    #region Read-Only

    public virtual bool IsReadOnly(int? timeoutMs = null)
    {
        var element = FindElement(timeoutMs);
        return element?.GetAttribute("readonly") == "true" ||
               element?.GetAttribute("IsReadOnly") == "True";
    }

    public virtual bool WaitReadOnly(bool? expected, int? timeoutMs = null)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var wait = new WebDriverWait(Driver, TimeSpan.FromMilliseconds(timeout));
        try
        {
            return wait.Until(_ => IsReadOnly() == expected.Value);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }
    }

    public virtual void AssertReadOnly(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        var actual = WaitReadOnly(expected, timeoutMs) ? expected.Value : IsReadOnly();
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected readOnly={expected} but was {actual}",
                Locator, expected, actual);
        }
    }

    #endregion

    #region Text Length

    public virtual int GetTextLength(int? timeoutMs = null)
    {
        return GetText(timeoutMs)?.Length ?? 0;
    }

    public virtual void AssertTextLength(int? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected is null) return;
        var actual = GetTextLength(timeoutMs);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected text length={expected} but was {actual}",
                Locator, expected, actual);
        }
    }

    #endregion
}
```

---

### 1.4 Concrete MAUI Controls

```csharp
/// <summary>Button control - clickable element.</summary>
public class ButtonControl : ClickableControlBase
{
    public ButtonControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public ButtonControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

/// <summary>Entry control - single-line text input.</summary>
public class EntryControl : TextControlBase
{
    public EntryControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public EntryControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

/// <summary>Editor control - multi-line text input.</summary>
public class EditorControl : TextControlBase
{
    public EditorControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public EditorControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}

/// <summary>Label control - read-only text display.</summary>
public class LabelControl : ControlObjectBase
{
    public LabelControl(MauiTestContext context, ControlLocator locator, IPageObject? page = null)
        : base(context, locator, page) { }

    public LabelControl(MauiTestContext context, string automationId, IPageObject? page = null)
        : base(context, automationId, page) { }
}
```

---

## 2. Blazor Foundation Classes

### 2.1 AsyncControlObjectBase

The root base class for all Blazor controls.

```csharp
public abstract class AsyncControlObjectBase : IAsyncControlObject
{
    protected readonly BlazorTestContext Context;
    
    public ControlLocator Locator { get; }
    public IAsyncPageObject? Page { get; }

    #region Constructors

    /// <summary>Primary constructor with explicit locator.</summary>
    protected AsyncControlObjectBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Locator = locator ?? throw new ArgumentNullException(nameof(locator));
        Page = page;
    }

    /// <summary>Convenience constructor - uses TestId strategy.</summary>
    protected AsyncControlObjectBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : this(context, By.TestId(testId), page)
    {
    }

    #endregion

    #region Logging

    protected void Log(string message)
    {
        Context.Log($"[{GetType().Name}] {Locator}: {message}");
    }

    #endregion

    #region Locator Access

    protected IPage PlaywrightPage => Context.Page;
    protected int DefaultTimeoutMs => Context.DefaultTimeoutMs;

    /// <summary>Converts ControlLocator to Playwright ILocator.</summary>
    protected virtual ILocator GetLocator()
    {
        return Locator.Strategy switch
        {
            LocatorStrategy.TestId => PlaywrightPage.GetByTestId(Locator.Value),
            LocatorStrategy.Id => PlaywrightPage.Locator($"#{Locator.Value}"),
            LocatorStrategy.Css => PlaywrightPage.Locator(Locator.Value),
            LocatorStrategy.XPath => PlaywrightPage.Locator($"xpath={Locator.Value}"),
            LocatorStrategy.Text => PlaywrightPage.GetByText(Locator.Value),
            LocatorStrategy.PartialText => PlaywrightPage.GetByText(Locator.Value, new() { Exact = false }),
            LocatorStrategy.Role => PlaywrightPage.GetByRole(ParseAriaRole(Locator.Value)),
            LocatorStrategy.Label => PlaywrightPage.GetByLabel(Locator.Value),
            LocatorStrategy.Placeholder => PlaywrightPage.GetByPlaceholder(Locator.Value),
            LocatorStrategy.Title => PlaywrightPage.GetByTitle(Locator.Value),
            LocatorStrategy.DataAttribute => PlaywrightPage.Locator($"[{Locator.Value}]"),
            _ => throw new LocatorNotFoundException(Locator.Strategy, Locator.Value)
        };
    }

    private static AriaRole ParseAriaRole(string role)
    {
        return Enum.TryParse<AriaRole>(role, true, out var result) 
            ? result 
            : AriaRole.Generic;
    }

    #endregion

    #region Is Methods (Async)

    public virtual async Task<bool> IsExistsAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var count = await GetLocator().CountAsync();
        return count > 0;
    }

    public virtual async Task<bool> IsVisibleAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        try
        {
            return await GetLocator().IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }

    public virtual async Task<bool> IsEnabledAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        try
        {
            return await GetLocator().IsEnabledAsync();
        }
        catch
        {
            return false;
        }
    }

    public virtual async Task<string> GetTextAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().InnerTextAsync(new() { Timeout = timeoutMs ?? DefaultTimeoutMs });
    }

    #endregion

    #region Wait Methods (Async)

    public virtual async Task<bool> WaitExistsAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        try
        {
            if (expected.Value)
            {
                await GetLocator().WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = timeout });
            }
            else
            {
                await GetLocator().WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = timeout });
            }
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    public virtual async Task<bool> WaitVisibleAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        try
        {
            if (expected.Value)
            {
                await GetLocator().WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = timeout });
            }
            else
            {
                await GetLocator().WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = timeout });
            }
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    public virtual async Task<bool> WaitEnabledAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return true;
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);
        
        while (DateTime.UtcNow < deadline)
        {
            var actual = await IsEnabledAsync(timeoutMs, ct);
            if (actual == expected.Value) return true;
            await Task.Delay(100, ct);
        }
        return false;
    }

    #endregion

    #region Check Methods (Async)

    public virtual async Task CheckExistsAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;
        if (!await WaitExistsAsync(expected, timeoutMs, ct))
        {
            throw new UITestTimeoutException(
                $"Control {Locator} exists={await IsExistsAsync()} but expected={expected}",
                timeoutMs ?? DefaultTimeoutMs);
        }
    }

    public virtual async Task CheckVisibleAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;
        if (!await WaitVisibleAsync(expected, timeoutMs, ct))
        {
            throw new UITestTimeoutException(
                $"Control {Locator} visible={await IsVisibleAsync()} but expected={expected}",
                timeoutMs ?? DefaultTimeoutMs);
        }
    }

    public virtual async Task CheckEnabledAsync(bool? expected, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;
        if (!await WaitEnabledAsync(expected, timeoutMs, ct))
        {
            throw new UITestTimeoutException(
                $"Control {Locator} enabled={await IsEnabledAsync()} but expected={expected}",
                timeoutMs ?? DefaultTimeoutMs);
        }
    }

    #endregion

    #region Assert Methods (Async)

    public virtual async Task AssertExistsAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;
        if (!await WaitExistsAsync(expected, timeoutMs, ct))
        {
            var actual = await IsExistsAsync(timeoutMs, ct);
            throw new AssertionException(
                message ?? $"Expected exists={expected} but was {actual}",
                Locator, expected, actual);
        }
    }

    public virtual async Task AssertVisibleAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;
        if (!await WaitVisibleAsync(expected, timeoutMs, ct))
        {
            var actual = await IsVisibleAsync(timeoutMs, ct);
            throw new AssertionException(
                message ?? $"Expected visible={expected} but was {actual}",
                Locator, expected, actual);
        }
    }

    public virtual async Task AssertTextAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;
        var actual = await GetTextAsync(timeoutMs, ct);
        if (actual != expected)
        {
            throw new AssertionException(
                message ?? $"Expected text='{expected}' but was '{actual}'",
                Locator, expected, actual);
        }
    }

    public virtual async Task AssertTextContainsAsync(string? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;
        var actual = await GetTextAsync(timeoutMs, ct);
        if (!actual.Contains(expected))
        {
            throw new AssertionException(
                message ?? $"Expected text to contain '{expected}' but was '{actual}'",
                Locator, expected, actual);
        }
    }

    #endregion
}
```

---

### 2.2 AsyncClickableControlBase

```csharp
public abstract class AsyncClickableControlBase : AsyncControlObjectBase, IAsyncClickableControlObject
{
    #region Constructors

    protected AsyncClickableControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncClickableControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    #endregion

    #region Click Actions (Async)

    public virtual async Task ClickAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("ClickAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);
        await GetLocator().ClickAsync(new() { Timeout = timeoutMs ?? DefaultTimeoutMs });
    }

    public virtual async Task DoubleClickAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("DoubleClickAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);
        await GetLocator().DblClickAsync(new() { Timeout = timeoutMs ?? DefaultTimeoutMs });
    }

    public virtual async Task RightClickAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("RightClickAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);
        await GetLocator().ClickAsync(new() 
        { 
            Button = MouseButton.Right,
            Timeout = timeoutMs ?? DefaultTimeoutMs 
        });
    }

    public virtual async Task HoverAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("HoverAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await GetLocator().HoverAsync(new() { Timeout = timeoutMs ?? DefaultTimeoutMs });
    }

    #endregion
}
```

---

### 2.3 AsyncTextControlBase

```csharp
public abstract class AsyncTextControlBase : AsyncClickableControlBase, IAsyncTextControlObject
{
    #region Constructors

    protected AsyncTextControlBase(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page)
        : base(context, locator, page) { }

    protected AsyncTextControlBase(BlazorTestContext context, string testId, IAsyncPageObject? page)
        : base(context, testId, page) { }

    #endregion

    #region Focus (Async)

    public virtual async Task<bool> IsFocusedAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().EvaluateAsync<bool>("el => el === document.activeElement");
    }

    public virtual async Task FocusAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("FocusAsync()");
        await GetLocator().FocusAsync();
    }

    public virtual async Task BlurAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("BlurAsync()");
        await GetLocator().BlurAsync();
    }

    #endregion

    #region Text Input (Async)

    public virtual async Task EnterAsync(string? text, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (text is null) return;
        Log($"EnterAsync(\"{text}\")");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await CheckEnabledAsync(true, timeoutMs, ct);
        await GetLocator().ClearAsync();
        await GetLocator().FillAsync(text);
    }

    public virtual async Task ClearAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        Log("ClearAsync()");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await GetLocator().ClearAsync();
    }

    public virtual async Task ClearAndEnterAsync(string? text, int? timeoutMs = null, CancellationToken ct = default)
    {
        await ClearAsync(timeoutMs, ct);
        if (text is not null)
        {
            await GetLocator().FillAsync(text);
        }
    }

    public virtual async Task AppendAsync(string? text, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (text is null) return;
        Log($"AppendAsync(\"{text}\")");
        await CheckVisibleAsync(true, timeoutMs, ct);
        await GetLocator().PressSequentiallyAsync(text);
    }

    #endregion

    #region Read-Only (Async)

    public virtual async Task<bool> IsReadOnlyAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var attr = await GetLocator().GetAttributeAsync("readonly");
        return attr is not null;
    }

    public virtual async Task AssertReadOnlyAsync(bool? expected, string? message = null, int? timeoutMs = null, CancellationToken ct = default)
    {
        if (expected is null) return;
        var actual = await IsReadOnlyAsync(timeoutMs, ct);
        if (actual != expected.Value)
        {
            throw new AssertionException(
                message ?? $"Expected readOnly={expected} but was {actual}",
                Locator, expected, actual);
        }
    }

    #endregion

    #region Text Override

    /// <summary>Override to get input value instead of innerText.</summary>
    public override async Task<string> GetTextAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().InputValueAsync();
    }

    public virtual async Task<int> GetTextLengthAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        var text = await GetTextAsync(timeoutMs, ct);
        return text?.Length ?? 0;
    }

    #endregion
}
```

---

### 2.4 Concrete Blazor Controls

```csharp
/// <summary>Button control - clickable element.</summary>
public class ButtonControl : AsyncClickableControlBase
{
    public ButtonControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public ButtonControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}

/// <summary>Link control - anchor element.</summary>
public class LinkControl : AsyncClickableControlBase
{
    public LinkControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public LinkControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }

    public virtual async Task<string> GetHrefAsync(int? timeoutMs = null, CancellationToken ct = default)
    {
        return await GetLocator().GetAttributeAsync("href") ?? string.Empty;
    }
}

/// <summary>Input control - single-line text input.</summary>
public class InputControl : AsyncTextControlBase
{
    public InputControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public InputControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}

/// <summary>TextArea control - multi-line text input.</summary>
public class TextAreaControl : AsyncTextControlBase
{
    public TextAreaControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public TextAreaControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}

/// <summary>Label control - read-only text display.</summary>
public class LabelControl : AsyncControlObjectBase
{
    public LabelControl(BlazorTestContext context, ControlLocator locator, IAsyncPageObject? page = null)
        : base(context, locator, page) { }

    public LabelControl(BlazorTestContext context, string testId, IAsyncPageObject? page = null)
        : base(context, testId, page) { }
}
```

---

## 3. Inheritance Summary

```
MAUI (Sync):
ControlObjectBase
├── LabelControl
└── ClickableControlBase
    ├── ButtonControl
    └── TextControlBase
        ├── EntryControl
        └── EditorControl

Blazor (Async):
AsyncControlObjectBase
├── LabelControl
└── AsyncClickableControlBase
    ├── ButtonControl
    ├── LinkControl
    └── AsyncTextControlBase
        ├── InputControl
        └── TextAreaControl
```

---

**Next:** [SPEC-006-003b-TOGGLE](SPEC-006-003b-TOGGLE.md)
