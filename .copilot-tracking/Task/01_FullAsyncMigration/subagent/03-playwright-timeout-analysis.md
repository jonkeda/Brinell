---
title: Playwright Timeout Interaction Analysis
description: Analysis of how Playwright's internal timeout system interacts with Brinell framework-level polling and waiting
author: Copilot
ms.date: 2026-02-24
ms.topic: reference
---

## Scope

This document analyzes the interaction between Playwright's built-in timeout/auto-wait system and the Brinell framework's `Poll()`/`Wait*()` methods in the Html stack. It covers Playwright 1.50.0 as used in the workspace.

## Playwright API Timeout Categories

Playwright's `ILocator` methods fall into three distinct categories based on timeout behavior.

### Category 1: Instant-return methods (no waiting)

These return the current DOM state immediately with no internal polling or waiting.

| Method | Timeout param | Notes |
|---|---|---|
| `CountAsync()` | None | Returns count immediately |
| `IsVisibleAsync()` | Deprecated | Returns current visibility |
| `IsHiddenAsync()` | Deprecated | Returns current hidden state |
| `EvaluateAllAsync()` | None | Runs JS on all matched elements |

These are safe to use inside a framework polling loop because they complete in milliseconds regardless of element state.

### Category 2: Auto-waiting action methods (30s default)

These perform actionability checks (visible, stable, enabled, receives events) and block until the element is ready or timeout expires.

| Method | Default timeout | Actionability checks |
|---|---|---|
| `ClickAsync()` | 30,000ms | Visible, Stable, Enabled, Receives Events |
| `FillAsync()` | 30,000ms | Visible, Enabled, Editable |
| `ClearAsync()` | 30,000ms | Visible, Enabled, Editable |
| `CheckAsync()` / `UncheckAsync()` | 30,000ms | Visible, Stable, Enabled, Receives Events |
| `HoverAsync()` | 30,000ms | Visible, Stable, Receives Events |
| `SelectOptionAsync()` | 30,000ms | Visible, Enabled |

### Category 3: State-reading methods with wait-for-attachment (30s default)

These wait for the element to be attached to the DOM before reading its value. If the element is already attached, they return quickly. If not, they block up to 30 seconds.

| Method | Default timeout | Behavior |
|---|---|---|
| `InnerTextAsync()` | 30,000ms | Waits for attachment, then reads |
| `InnerHTMLAsync()` | 30,000ms | Waits for attachment, then reads |
| `GetAttributeAsync()` | 30,000ms | Waits for attachment, then reads |
| `InputValueAsync()` | 30,000ms | Waits for attachment, then reads |
| `IsEnabledAsync()` | 30,000ms | Waits for attachment, then reads |
| `IsCheckedAsync()` | 30,000ms | Waits for attachment, then reads |
| `EvaluateAsync()` | 30,000ms | Waits for attachment, then runs JS |

### Category 4: Explicit wait method

| Method | Default timeout | States |
|---|---|---|
| `WaitForAsync()` | 30,000ms | Attached, Detached, Visible, Hidden |

Accepts a `WaitForSelectorState` enum and returns when the condition is met or timeout expires. Playwright handles the internal polling using efficient MutationObserver-based mechanisms.

### Timeout override hierarchy

Playwright resolves timeouts in this order (first non-null wins):

1. Per-call `Timeout` option (e.g., `ClickAsync(new() { Timeout = 5000 })`)
2. `Page.SetDefaultTimeout()`
3. `BrowserContext.SetDefaultTimeout()`
4. Hard-coded default: 30,000ms

The Brinell framework does NOT call `SetDefaultTimeout()` anywhere, so all Playwright operations use the 30-second default unless overridden per-call.

## Current Framework Polling Implementation

### `ObjectBase.Poll()`

```csharp
// srcnew/Brinell.Html/ObjectBase.cs
protected bool Poll(Func<bool> condition, int timeoutMs)
{
    var stopwatch = Stopwatch.StartNew();
    while (stopwatch.ElapsedMilliseconds < timeoutMs)
    {
        try { if (condition()) return true; } catch { }
        WaitHelper.Pause(PollingIntervalMs);  // SpinWait, default 100ms
    }
    try { return condition(); } catch { return false; }
}
```

### `PlaywrightTestContext.TryFindElement()`

```csharp
// srcnew/Brinell.Html.Playwright/PlaywrightTestContext.cs
public IHtmlElement? TryFindElement(Locator locator)
{
    var playwrightLocator = LocatorExtensions.ToPlaywrightLocator(this, locator);
    var count = playwrightLocator.CountAsync().GetAwaiter().GetResult();
    return count > 0 ? new PlaywrightHtmlElement(playwrightLocator.First) : null;
}
```

Uses `CountAsync()` (Category 1, instant). Safe for polling.

### `PlaywrightHtmlElement` state properties

```csharp
// srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs
public bool Visible => _locator.IsVisibleAsync().GetAwaiter().GetResult();   // Category 1, instant
public bool Enabled => _locator.IsEnabledAsync().GetAwaiter().GetResult();   // Category 3, 30s default!
public string? Text => _locator.InnerTextAsync().GetAwaiter().GetResult();   // Category 3, 30s default!
```

## Timeout Interaction Analysis

### WaitExists: No conflict

```
WaitExists(true, 5000)
  → Poll(() => IsExists() == true, 5000)
    → TryFindElement() → CountAsync()  [instant, Category 1]
    → returns null or element
  → Loop every 100ms for up to 5s
```

`CountAsync()` has no timeout parameter and returns immediately. The framework's `Poll()` loop is the sole timeout authority. No conflict.

### WaitVisible: No conflict

```
WaitVisible(true, 5000)
  → Poll(() => IsVisible() == true, 5000)
    → TryFindElement()?.Visible
    → TryFindElement() → CountAsync()  [instant]
    → .Visible → IsVisibleAsync()      [instant, Category 1, deprecated timeout]
  → Loop every 100ms for up to 5s
```

Both underlying calls are instant. No conflict.

### WaitEnabled: POTENTIAL CONFLICT

```
WaitEnabled(true, 5000)
  → Poll(() => IsEnabled() == true, 5000)
    → TryFindElement()?.Enabled
    → TryFindElement() → CountAsync()  [instant]
    → .Enabled → IsEnabledAsync()      [Category 3, 30s default!]
```

If `TryFindElement()` returns a non-null element (it existed at count-time) but the element is then removed from the DOM before `IsEnabledAsync()` executes, Playwright waits up to 30 seconds for re-attachment. The framework thinks it's iterating on 100ms intervals, but a single iteration could block for 30 seconds.

**Impact**: A 5-second framework timeout could actually take 30+ seconds in edge cases. The `Poll()` catch block would swallow the `TimeoutError`, but the wall-clock time is far beyond what the caller expected.

**Probability**: Low for stable pages, moderate for pages with dynamic element replacement (SPA route changes, component re-renders).

### WaitText: POTENTIAL CONFLICT

```
WaitText("hello", 5000)
  → Poll(() => GetText() == "hello", 5000)
    → GetText() → TryFindElement()?.Text
    → TryFindElement() → CountAsync()  [instant]
    → .Text → InnerTextAsync()         [Category 3, 30s default!]
```

Same race condition as WaitEnabled. If the element disappears between `CountAsync()` and `InnerTextAsync()`, a single iteration blocks for up to 30 seconds.

### FindElement (PlaywrightTestContext): THREAD.SLEEP VIOLATION

```csharp
// PlaywrightTestContext.FindElement() — lines 190-210
public IHtmlElement FindElement(Locator locator)
{
    var timeout = _timeouts.ElementFind;  // default 3000ms
    var deadline = DateTime.UtcNow.AddMilliseconds(timeout);
    while (DateTime.UtcNow < deadline)
    {
        var element = TryFindElement(locator);
        if (element != null) return element;
        Thread.Sleep(100);  // ❌ Anti-pattern violation
    }
    throw new ElementNotFoundException(locator, timeout);
}
```

Uses `Thread.Sleep(100)` instead of `WaitHelper.Pause()`. Also, this custom polling loop duplicates what Playwright's `WaitForAsync(Attached)` does natively and more efficiently.

### WaitReady (PlaywrightTestContext): THREAD.SLEEP VIOLATION

```csharp
// PlaywrightTestContext.WaitReady() — lines 155-167
public bool WaitReady(int? timeoutMs = null)
{
    var timeout = timeoutMs ?? _timeouts.PageLoad;
    var deadline = DateTime.UtcNow.AddMilliseconds(timeout);
    while (DateTime.UtcNow < deadline)
    {
        if (IsReady()) return true;
        Thread.Sleep(100);  // ❌ Anti-pattern violation
    }
    return IsReady();
}
```

Same `Thread.Sleep` violation.

### Action methods (Click, Fill): No conflict, but double-wait

When the framework calls `element.Click()`, Playwright performs its own 30-second auto-wait for actionability. This is sequential (happens after the framework's Wait/Assert methods), not overlapping. The framework's waiting ensures the element exists/is visible, the action's auto-wait ensures it's stable and receives events. This layering is beneficial, not conflicting.

## Summary of Issues Found

| Issue | Severity | Location |
|---|---|---|
| `IsEnabledAsync()` 30s timeout inside `Poll()` 5s loop | Medium | `PlaywrightHtmlElement.Enabled` property |
| `InnerTextAsync()` 30s timeout inside `Poll()` 5s loop | Medium | `PlaywrightHtmlElement.Text` property |
| `Thread.Sleep(100)` in `FindElement()` | High | `PlaywrightTestContext.cs` line ~200 |
| `Thread.Sleep(100)` in `WaitReady()` | High | `PlaywrightTestContext.cs` line ~162 |
| No `SetDefaultTimeout()` call | Low | `PlaywrightTestContext.CreateAsync()` |

## Recommendation for Async Wait Methods

### Use a hybrid approach: Playwright-native where possible, framework polling for the rest

#### Tier 1: Delegate to Playwright's WaitForAsync (for existence and visibility)

`WaitForAsync()` maps directly to `WaitExists` and `WaitVisible` semantics. Playwright uses efficient MutationObserver-based watching internally instead of polling.

```
WaitExistsAsync(true)  → WaitForAsync(State = Attached, Timeout = timeoutMs)
WaitExistsAsync(false) → WaitForAsync(State = Detached, Timeout = timeoutMs)
WaitVisibleAsync(true)  → WaitForAsync(State = Visible, Timeout = timeoutMs)
WaitVisibleAsync(false) → WaitForAsync(State = Hidden, Timeout = timeoutMs)
```

Advantages:

- Single timeout authority (no conflict)
- More efficient than polling (event-driven vs. 100ms sleep loop)
- Truly async (no thread blocking)
- Playwright already handles re-attachment edge cases

Disadvantage:

- Requires the Playwright backend to expose the underlying `ILocator`, or a `WaitForAsync` method on `IHtmlElement` / `IHtmlTestContext`

#### Tier 2: Framework PollAsync with instant Playwright APIs (for text, enabled, checked, custom conditions)

For conditions Playwright doesn't have a dedicated `WaitFor` state, use a framework-level async polling loop but ensure the inner calls are fast.

```csharp
protected async Task<bool> PollAsync(Func<Task<bool>> condition, int timeoutMs)
{
    var stopwatch = Stopwatch.StartNew();
    while (stopwatch.ElapsedMilliseconds < timeoutMs)
    {
        try { if (await condition()) return true; } catch { }
        await Task.Delay(PollingIntervalMs);
    }
    try { return await condition(); } catch { return false; }
}
```

To prevent the 30-second-per-iteration problem, the inner Playwright calls must use small explicit timeouts or instant APIs:

- **Text**: Use `InnerTextAsync(new() { Timeout = 1000 })` or catch and return null quickly
- **Enabled**: Use `IsEnabledAsync(new() { Timeout = 1000 })` or use `EvaluateAsync("el => !el.disabled")` which is faster
- **Checked**: Use `IsCheckedAsync(new() { Timeout = 1000 })` or `EvaluateAsync("el => el.checked")`

Alternatively, introduce a `ReadStateAsync` method on `IHtmlElement` that returns quickly (with a short per-call timeout) to make all polling iterations fast.

#### Tier 3: Set Page.SetDefaultTimeout during context creation

As a safety net, set a short default timeout on the Playwright page during `PlaywrightTestContext.CreateAsync()`:

```csharp
page.SetDefaultTimeout(5000);  // 5s instead of 30s
```

This prevents any accidental 30-second blocks on state-reading calls within polling loops. Action methods (`ClickAsync`, `FillAsync`) that need longer waits can pass explicit per-call timeouts.

### Summary table

| Wait method | Async strategy | Inner API |
|---|---|---|
| `WaitExistsAsync` | Playwright `WaitForAsync` | `Attached` / `Detached` |
| `WaitVisibleAsync` | Playwright `WaitForAsync` | `Visible` / `Hidden` |
| `WaitEnabledAsync` | Framework `PollAsync` | `IsEnabledAsync(timeout: short)` |
| `WaitTextAsync` | Framework `PollAsync` | `InnerTextAsync(timeout: short)` |
| `WaitCheckedAsync` | Framework `PollAsync` | `IsCheckedAsync(timeout: short)` |
| `FindElementAsync` | Playwright `WaitForAsync` | Replace Thread.Sleep loop |

### Immediate fixes (pre-async migration)

1. Replace `Thread.Sleep(100)` in `PlaywrightTestContext.FindElement()` with `WaitHelper.Pause(100)` or Playwright's `WaitForAsync(Attached, timeout)`
2. Replace `Thread.Sleep(100)` in `PlaywrightTestContext.WaitReady()` with `WaitHelper.Pause(100)`
3. Consider calling `page.SetDefaultTimeout()` in `CreateAsync()` aligned with `_timeouts.DefaultWait`

### Interface design consideration

The framework's `IControlObject` interface is shared across all platforms (MAUI, Html, Blazor, WinForms, Stride). The async `PollAsync` must live in a base class common to all platforms, but the Playwright-specific `WaitForAsync` delegation must be in the Html.Playwright layer only.

Suggested pattern:

- `ObjectBase` (per platform): provides `PollAsync(Func<Task<bool>>, int)` using `await Task.Delay()`
- `PlaywrightControlBase` or override in `ControlBase<TScope>`: overrides `WaitExistsAsync` / `WaitVisibleAsync` to delegate to `WaitForAsync` when the underlying locator supports it
- Other platforms (Appium, WinForms): use the generic `PollAsync` loop since their drivers don't have equivalent built-in waiting

This preserves the cross-platform interface while allowing Playwright-optimized implementations.
