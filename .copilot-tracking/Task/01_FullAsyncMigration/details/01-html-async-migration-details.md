<!-- markdownlint-disable-file -->
# Implementation Details: HTML/Blazor Async Migration

## Context Reference

Sources: [02-html-async-migration-research.md](.copilot-tracking/Task/01_FullAsyncMigration/research/02-html-async-migration-research.md), [01-html-stack-inventory.md](.copilot-tracking/Task/01_FullAsyncMigration/subagent/01-html-stack-inventory.md), [02-explicit-interface-pattern.md](.copilot-tracking/Task/01_FullAsyncMigration/subagent/02-explicit-interface-pattern.md), [03-playwright-timeout-analysis.md](.copilot-tracking/Task/01_FullAsyncMigration/subagent/03-playwright-timeout-analysis.md)

## Constraints

* `Brinell.Core` interfaces — ZERO changes. `IControlObject<TScope>`, `IElement<TSelf>`, `ITestContext` are immutable.
* All existing sync method signatures, behaviors, and return types stay identical.
* Use `ConfigureAwait(false)` on every `await` in library code (Brinell.Html and Brinell.Html.Playwright).
* No `Thread.Sleep`, `Task.Delay` as arbitrary waits — only polling-based waits with conditions.
* Explicit interface implementation for all async methods (Decision D1 — same method names on separate interfaces).

## Implementation Phase 0: Pre-migration Fixes

<!-- parallelizable: true -->

### Step 0.1: Fix `Thread.Sleep(100)` in `PlaywrightTestContext.WaitReady()`

Replace the `Thread.Sleep(100)` polling sleep with `WaitHelper.Pause(100)` within the existing sync polling loop.

Files:

* [srcnew/Brinell.Html.Playwright/PlaywrightTestContext.cs](srcnew/Brinell.Html.Playwright/PlaywrightTestContext.cs) (~line 147-162) — Replace `Thread.Sleep(100)` with `WaitHelper.Pause(100)`

Success criteria:

* `Thread.Sleep` removed from `WaitReady` method
* Sync behavior unchanged (same 100ms pause between polls)
* Build succeeds

Context references:

* [subagent/03-playwright-timeout-analysis.md](.copilot-tracking/Task/01_FullAsyncMigration/subagent/03-playwright-timeout-analysis.md) — Thread.Sleep violations section

### Step 0.2: Fix `Thread.Sleep(100)` in `PlaywrightTestContext.FindElement()`

Replace the `Thread.Sleep(100)` retry sleep in the element-finding retry loop with `WaitHelper.Pause(100)`.

Files:

* [srcnew/Brinell.Html.Playwright/PlaywrightTestContext.cs](srcnew/Brinell.Html.Playwright/PlaywrightTestContext.cs) (~line 180-200) — Replace `Thread.Sleep(100)` with `WaitHelper.Pause(100)`

Success criteria:

* `Thread.Sleep` removed from `FindElement` method
* Sync retry behavior unchanged
* Build succeeds

Context references:

* [subagent/03-playwright-timeout-analysis.md](.copilot-tracking/Task/01_FullAsyncMigration/subagent/03-playwright-timeout-analysis.md) — Thread.Sleep violations section

## Implementation Phase 1: Async Interfaces (Brinell.Html)

<!-- parallelizable: true -->

### Step 1.1: Create `IAsyncHtmlElement` interface

Create a new interface that mirrors every member of `IHtmlElement` (which extends `IElement<IHtmlElement>`) but returns `Task` or `Task<T>`. Properties become async methods.

Files:

* [srcnew/Brinell.Html/Interfaces/Async/IAsyncHtmlElement.cs](srcnew/Brinell.Html/Interfaces/Async/IAsyncHtmlElement.cs) — NEW FILE

Interface shape:

```csharp
namespace Brinell.Html.Interfaces.Async;

public interface IAsyncHtmlElement
{
    // State (from IElement<TSelf> properties → async methods)
    Task<bool> IsVisible();
    Task<bool> IsEnabled();
    Task<bool> IsSelected();
    Task<string?> GetText();
    Task<string?> GetTagName();

    // Actions (from IElement<TSelf> void methods → async Task)
    Task Click();
    Task SendKeys(string text, TextInputMethod method = TextInputMethod.Keys);
    Task Clear();
    Task DoubleClick();
    Task RightClick();
    Task Hover();
    Task LongPress(int durationMs = 1000);
    Task ScrollIntoView(int timeoutMs = 5000);

    // Attributes
    Task<string?> GetAttribute(string name);

    // HTML-specific (from IHtmlElement)
    Task<string> GetInnerHtml();
    Task<string> GetOuterHtml();
    Task<bool> GetIsChecked();
    Task<string> GetInputValue();
    Task<string?> GetDomAttribute(string attributeName);
    Task<string?> GetDomProperty(string propertyName);
    Task<string?> GetCssValue(string propertyName);
    Task Submit();
    Task Fill(string value);
    Task SelectOption(string value);
    Task SelectOption(string[] values);
    Task Check();
    Task Uncheck();
    Task Focus();
    Task Blur();
    Task<T?> Evaluate<T>(string expression);
    Task Evaluate(string expression);
}
```

Success criteria:

* Interface compiles and contains async counterpart for every `IHtmlElement` + `IElement<IHtmlElement>` member
* Located in `Interfaces/Async/` subfolder

Dependencies:

* None — standalone interface

### Step 1.2: Create `IHtmlAsyncControlObject<TScope>` interface

Mirrors `IControlObject<TScope>` methods as defined in `ControlBase<TScope>` (the actual HTML implementation, not the Core interface generic shape). Same method names, `Task`-wrapped returns.

Files:

* [srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncControlObject.cs](srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncControlObject.cs) — NEW FILE

Interface shape:

```csharp
namespace Brinell.Html.Interfaces.Async;

public interface IHtmlAsyncControlObject<TScope>
    where TScope : IHtmlScope<TScope>
{
    Task<bool> IsExists();
    Task<bool?> IsVisible();
    Task<bool?> IsEnabled();

    Task<bool> WaitExists(bool? expected, int? timeoutMs = null);
    Task<bool> WaitVisible(bool? expected, int? timeoutMs = null);
    Task<bool> WaitEnabled(bool? expected, int? timeoutMs = null);

    Task<TScope> AssertExists(bool? expected, string? message = null, int? timeoutMs = null);
    Task<TScope> AssertVisible(bool? expected, string? message = null, int? timeoutMs = null);
    Task<TScope> AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null);

    Task<string?> GetText(int? timeoutMs = null);
    Task<bool> WaitText(string? expected, int? timeoutMs = null);
    Task<TScope> AssertText(string? expected, string? message = null, int? timeoutMs = null);
    Task<TScope> AssertTextContains(string? expected, string? message = null, int? timeoutMs = null);

    Task<string?> GetAttribute(string name);
}
```

Success criteria:

* Every public method from `ControlBase<TScope>` has an async counterpart
* Constrained to `where TScope : IHtmlScope<TScope>`

### Step 1.3: Create `IHtmlAsyncClickable<TScope>` interface

Mirrors `Control<TScope>` and `ClickableControlBase<TScope>` action methods.

Files:

* [srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncClickable.cs](srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncClickable.cs) — NEW FILE

Interface shape:

```csharp
namespace Brinell.Html.Interfaces.Async;

public interface IHtmlAsyncClickable<TScope> : IHtmlAsyncControlObject<TScope>
    where TScope : IHtmlScope<TScope>
{
    Task<TScope> Click();
    Task<TScope> SendKeys(string text);
    Task<TScope> Clear();
    Task<TScope> ScrollIntoView(int timeoutMs = 5000);
    Task<TScope> DoubleClick();
    Task<TScope> RightClick();
    Task<TScope> Hover();
}
```

Success criteria:

* Inherits from `IHtmlAsyncControlObject<TScope>`
* Contains all methods from `Control` + `ClickableControlBase`

### Step 1.4: Create `IHtmlAsyncFocusable<TScope>` interface

Mirrors `FocusableControlBase<TScope>`.

Files:

* [srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncFocusable.cs](srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncFocusable.cs) — NEW FILE

Interface shape:

```csharp
namespace Brinell.Html.Interfaces.Async;

public interface IHtmlAsyncFocusable<TScope> : IHtmlAsyncClickable<TScope>
    where TScope : IHtmlScope<TScope>
{
    Task<TScope> Focus();
    Task<TScope> Blur();
    Task<bool> HasFocus();
}
```

Success criteria:

* Inherits from `IHtmlAsyncClickable<TScope>`
* Contains 3 focus-related methods

### Step 1.5: Create `IHtmlAsyncToggle<TScope>` interface

Mirrors `ToggleControlBase<TScope>`.

Files:

* [srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncToggle.cs](srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncToggle.cs) — NEW FILE

Interface shape:

```csharp
namespace Brinell.Html.Interfaces.Async;

public interface IHtmlAsyncToggle<TScope> : IHtmlAsyncClickable<TScope>
    where TScope : IHtmlScope<TScope>
{
    Task<bool> IsChecked();
    Task<TScope> SetChecked(bool value);
    Task<bool> WaitChecked(bool expected, int? timeoutMs = null);
    Task<TScope> AssertChecked(bool expected);

    // CheckBoxControl additions
    Task<TScope> Check();
    Task<TScope> Uncheck();
    Task<TScope> Toggle();
}
```

Success criteria:

* Contains all toggle methods plus check/uncheck/toggle from `CheckBoxControl`
* Inherits from `IHtmlAsyncClickable<TScope>`

### Step 1.6: Create `IHtmlAsyncEditable<TScope>` interface

Mirrors `TextInputControl<TScope>` and `TextAreaControl<TScope>`.

Files:

* [srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncEditable.cs](srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncEditable.cs) — NEW FILE

Interface shape:

```csharp
namespace Brinell.Html.Interfaces.Async;

public interface IHtmlAsyncEditable<TScope> : IHtmlAsyncFocusable<TScope>
    where TScope : IHtmlScope<TScope>
{
    Task<TScope> SetText(string text);
    Task<string> GetValue();
    Task<TScope> TypeText(string text);
    Task<TScope> AssertValue(string? expected);
    Task<TScope> WaitValue(string? expected, int? timeoutMs = null);

    // TextAreaControl addition
    Task<TScope> AppendText(string text);
}
```

Success criteria:

* Inherits from `IHtmlAsyncFocusable<TScope>`
* Contains text-input and text-area methods

### Step 1.7: Create `IHtmlAsyncSelector<TScope>` interface

Mirrors `SelectorControlBase<TScope>`, `SelectControl<TScope>`, `RadioGroupControl<TScope>`.

Files:

* [srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncSelector.cs](srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncSelector.cs) — NEW FILE

Interface shape:

```csharp
namespace Brinell.Html.Interfaces.Async;

public interface IHtmlAsyncSelector<TScope> : IHtmlAsyncFocusable<TScope>
    where TScope : IHtmlScope<TScope>
{
    Task<TScope> SelectByValue(string value);
    Task<TScope> SelectByText(string text);
    Task<string?> GetSelectedValue();

    // SelectControl addition
    Task<TScope> SelectMultiple(params string[] values);
}
```

Success criteria:

* Inherits from `IHtmlAsyncFocusable<TScope>`
* Contains selector and multi-select methods

### Step 1.8: Create `IHtmlAsyncRange<TScope>` interface

Mirrors `RangeControlBase<TScope>` and derived controls (`RangeInputControl`, `DateInputControl`, `TimeInputControl`).

Files:

* [srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncRange.cs](srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncRange.cs) — NEW FILE

Interface shape:

```csharp
namespace Brinell.Html.Interfaces.Async;

public interface IHtmlAsyncRange<TScope> : IHtmlAsyncFocusable<TScope>
    where TScope : IHtmlScope<TScope>
{
    Task<string?> GetMin();
    Task<string?> GetMax();
    Task<string?> GetStep();
    Task<string> GetValue();
    Task<TScope> SetValue(string value);
}
```

Success criteria:

* Inherits from `IHtmlAsyncFocusable<TScope>`
* Contains all range-base methods

Note: `RangeInputControl`-specific `GetNumericValue`/`SetNumericValue`/`AssertNumericValue` and `DateInputControl`/`TimeInputControl` specific methods are implemented as explicit interface members on the concrete classes or as additional interface members added during Step 3.8.

### Step 1.9: Create `IHtmlAsyncScrollable<TScope>` interface

Mirrors `ScrollableControlBase<TScope>`.

Files:

* [srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncScrollable.cs](srcnew/Brinell.Html/Interfaces/Async/IHtmlAsyncScrollable.cs) — NEW FILE

Interface shape:

```csharp
namespace Brinell.Html.Interfaces.Async;

public interface IHtmlAsyncScrollable<TScope> : IHtmlAsyncClickable<TScope>
    where TScope : IHtmlScope<TScope>
{
    Task<TScope> ScrollTo(int x, int y);
    Task<TScope> ScrollToTop();
}
```

Success criteria:

* Inherits from `IHtmlAsyncClickable<TScope>`
* Contains scroll methods

## Implementation Phase 2: Core Async Infrastructure

<!-- parallelizable: false -->

### Step 2.1: Add `PollAsync` to `ObjectBase`

Add an async polling method alongside the existing sync `Poll`. Uses `Task.Delay` between iterations instead of `WaitHelper.Pause`.

Files:

* [srcnew/Brinell.Html/ObjectBase.cs](srcnew/Brinell.Html/ObjectBase.cs) (46 lines) — Add `PollAsync` method after existing `Poll` method (~line 45)

Code to add at end of class (before closing brace):

```csharp
protected async Task<bool> PollAsync(Func<Task<bool>> condition, int timeoutMs)
{
    var stopwatch = Stopwatch.StartNew();
    while (stopwatch.ElapsedMilliseconds < timeoutMs)
    {
        try
        {
            if (await condition().ConfigureAwait(false))
                return true;
        }
        catch
        {
            // Condition threw — treat as not-met, retry
        }

        await Task.Delay(PollingIntervalMs).ConfigureAwait(false);
    }

    // Final attempt
    try { return await condition().ConfigureAwait(false); }
    catch { return false; }
}
```

Success criteria:

* `PollAsync` compiles and mirrors `Poll` behavior with `await Task.Delay` instead of sync pause
* Existing `Poll` method unchanged

Dependencies:

* None — independent addition

### Step 2.2: Add async element access helpers to `ControlBase`

Add `FindAsyncElement`, `TryFindAsyncElement`, and `RunWithElementAsync` (2 overloads) as protected methods.

Files:

* [srcnew/Brinell.Html/Controls/ControlBase.cs](srcnew/Brinell.Html/Controls/ControlBase.cs) (196 lines) — Add new protected methods after existing `RunAssert` (~line 52)

Code to add:

```csharp
// Async element access
protected IAsyncHtmlElement? TryFindAsyncElement()
    => TryFindElement() as IAsyncHtmlElement;

protected IAsyncHtmlElement FindAsyncElement()
    => FindElement() as IAsyncHtmlElement
        ?? throw new InvalidOperationException(
            $"Element for '{Locator}' does not support async operations. " +
            "Ensure the test context uses an async-capable element implementation.");

// Async RunWithElement (action → TScope)
protected async Task<TScope> RunWithElementAsync(Func<IAsyncHtmlElement, Task> action)
{
    var element = FindAsyncElement();
    await action(element).ConfigureAwait(false);
    return ContainingScope;
}

// Async RunWithElement (func → TResult)
protected async Task<TResult> RunWithElementAsync<TResult>(Func<IAsyncHtmlElement, Task<TResult>> action)
{
    var element = FindAsyncElement();
    return await action(element).ConfigureAwait(false);
}

// Async RunAssert (assertion → TScope)
protected async Task<TScope> RunAssertAsync(Func<IAsyncHtmlElement, Task> assertion)
{
    var element = FindAsyncElement();
    await assertion(element).ConfigureAwait(false);
    return ContainingScope;
}
```

Success criteria:

* 5 new protected methods compile
* Existing sync helpers unchanged
* `IAsyncHtmlElement` using added

Dependencies:

* Step 1.1 (`IAsyncHtmlElement` interface must exist)

### Step 2.3: Add `IHtmlAsyncControlObject<TScope>` explicit implementation to `ControlBase`

Modify `ControlBase<TScope>` class declaration to implement `IHtmlAsyncControlObject<TScope>` and add explicit implementations for all async control-object methods.

Files:

* [srcnew/Brinell.Html/Controls/ControlBase.cs](srcnew/Brinell.Html/Controls/ControlBase.cs) — Modify class declaration and add explicit async implementations

Class declaration change:

```csharp
// FROM:
public abstract class ControlBase<TScope> : ObjectBase, IControlObject<TScope>
    where TScope : IHtmlScope<TScope>

// TO:
public abstract class ControlBase<TScope> : ObjectBase,
    IControlObject<TScope>,
    IHtmlAsyncControlObject<TScope>
    where TScope : IHtmlScope<TScope>
```

Explicit implementations to add (after sync methods):

```csharp
#region IHtmlAsyncControlObject<TScope> explicit implementation

async Task<bool> IHtmlAsyncControlObject<TScope>.IsExists()
{
    var element = TryFindAsyncElement();
    return element != null;
}

async Task<bool?> IHtmlAsyncControlObject<TScope>.IsVisible()
{
    var element = TryFindAsyncElement();
    return element != null ? await element.IsVisible().ConfigureAwait(false) : null;
}

async Task<bool?> IHtmlAsyncControlObject<TScope>.IsEnabled()
{
    var element = TryFindAsyncElement();
    return element != null ? await element.IsEnabled().ConfigureAwait(false) : null;
}

async Task<bool> IHtmlAsyncControlObject<TScope>.WaitExists(bool? expected, int? timeoutMs)
{
    if (expected == null) return true;
    var timeout = timeoutMs ?? DefaultTimeoutMs;
    return await PollAsync(async () =>
    {
        var exists = TryFindAsyncElement() != null;
        return exists == expected.Value;
    }, timeout).ConfigureAwait(false);
}

async Task<bool> IHtmlAsyncControlObject<TScope>.WaitVisible(bool? expected, int? timeoutMs)
{
    if (expected == null) return true;
    var timeout = timeoutMs ?? DefaultTimeoutMs;
    return await PollAsync(async () =>
    {
        var element = TryFindAsyncElement();
        if (element == null) return !expected.Value;
        return await element.IsVisible().ConfigureAwait(false) == expected.Value;
    }, timeout).ConfigureAwait(false);
}

async Task<bool> IHtmlAsyncControlObject<TScope>.WaitEnabled(bool? expected, int? timeoutMs)
{
    if (expected == null) return true;
    var timeout = timeoutMs ?? DefaultTimeoutMs;
    return await PollAsync(async () =>
    {
        var element = TryFindAsyncElement();
        if (element == null) return false;
        return await element.IsEnabled().ConfigureAwait(false) == expected.Value;
    }, timeout).ConfigureAwait(false);
}

async Task<TScope> IHtmlAsyncControlObject<TScope>.AssertExists(bool? expected, string? message, int? timeoutMs)
{
    if (expected == null) return ContainingScope;
    var self = (IHtmlAsyncControlObject<TScope>)this;
    if (!await self.WaitExists(expected, timeoutMs).ConfigureAwait(false))
    {
        var actual = await self.IsExists().ConfigureAwait(false);
        throw new AssertionException(
            message ?? $"Expected element '{Locator}' to {(expected.Value ? "exist" : "not exist")} but exists={actual}.");
    }
    return ContainingScope;
}

async Task<TScope> IHtmlAsyncControlObject<TScope>.AssertVisible(bool? expected, string? message, int? timeoutMs)
{
    if (expected == null) return ContainingScope;
    var self = (IHtmlAsyncControlObject<TScope>)this;
    if (!await self.WaitVisible(expected, timeoutMs).ConfigureAwait(false))
    {
        var actual = await self.IsVisible().ConfigureAwait(false);
        throw new AssertionException(
            message ?? $"Expected element '{Locator}' to be {(expected.Value ? "visible" : "hidden")} but visible={actual}.");
    }
    return ContainingScope;
}

async Task<TScope> IHtmlAsyncControlObject<TScope>.AssertEnabled(bool? expected, string? message, int? timeoutMs)
{
    if (expected == null) return ContainingScope;
    var self = (IHtmlAsyncControlObject<TScope>)this;
    if (!await self.WaitEnabled(expected, timeoutMs).ConfigureAwait(false))
    {
        var actual = await self.IsEnabled().ConfigureAwait(false);
        throw new AssertionException(
            message ?? $"Expected element '{Locator}' to be {(expected.Value ? "enabled" : "disabled")} but enabled={actual}.");
    }
    return ContainingScope;
}

async Task<string?> IHtmlAsyncControlObject<TScope>.GetText(int? timeoutMs)
{
    var element = TryFindAsyncElement();
    return element != null ? await element.GetText().ConfigureAwait(false) : null;
}

async Task<bool> IHtmlAsyncControlObject<TScope>.WaitText(string? expected, int? timeoutMs)
{
    if (expected == null) return true;
    var timeout = timeoutMs ?? DefaultTimeoutMs;
    return await PollAsync(async () =>
    {
        var element = TryFindAsyncElement();
        if (element == null) return false;
        var text = await element.GetText().ConfigureAwait(false);
        return text == expected;
    }, timeout).ConfigureAwait(false);
}

async Task<TScope> IHtmlAsyncControlObject<TScope>.AssertText(string? expected, string? message, int? timeoutMs)
{
    if (expected == null) return ContainingScope;
    var self = (IHtmlAsyncControlObject<TScope>)this;
    if (!await self.WaitText(expected, timeoutMs).ConfigureAwait(false))
    {
        var actual = await self.GetText().ConfigureAwait(false);
        throw new AssertionException(
            message ?? $"Expected text '{expected}' but got '{actual ?? "(null)"}' for element '{Locator}'.");
    }
    return ContainingScope;
}

async Task<TScope> IHtmlAsyncControlObject<TScope>.AssertTextContains(string? expected, string? message, int? timeoutMs)
{
    if (expected == null) return ContainingScope;
    var timeout = timeoutMs ?? DefaultTimeoutMs;
    var matched = await PollAsync(async () =>
    {
        var element = TryFindAsyncElement();
        if (element == null) return false;
        var text = await element.GetText().ConfigureAwait(false);
        return text?.Contains(expected, StringComparison.Ordinal) == true;
    }, timeout).ConfigureAwait(false);

    if (!matched)
    {
        var self = (IHtmlAsyncControlObject<TScope>)this;
        var actual = await self.GetText().ConfigureAwait(false);
        throw new AssertionException(
            message ?? $"Expected text to contain '{expected}' but got '{actual ?? "(null)"}' for element '{Locator}'.");
    }
    return ContainingScope;
}

async Task<string?> IHtmlAsyncControlObject<TScope>.GetAttribute(string name)
{
    return await RunWithElementAsync<string?>(async e =>
        await e.GetAttribute(name).ConfigureAwait(false)).ConfigureAwait(false);
}

#endregion
```

Success criteria:

* Class declaration includes both sync and async interfaces
* All 15 methods from `IHtmlAsyncControlObject<TScope>` have explicit implementations
* Existing sync `IControlObject<TScope>` methods unchanged
* Usings added for `Brinell.Html.Interfaces.Async` and `Brinell.Core.Exceptions`

Dependencies:

* Steps 1.2 and 2.1-2.2

### Step 2.4: Implement `IAsyncHtmlElement` on `PlaywrightHtmlElement`

Add `IAsyncHtmlElement` to the class declaration and implement all async methods with native `await` of Playwright APIs.

Files:

* [srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs](srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs) (195 lines) — Add interface implementation

Class declaration change:

```csharp
// FROM:
public sealed class PlaywrightHtmlElement : IHtmlElement

// TO:
public sealed class PlaywrightHtmlElement : IHtmlElement, IAsyncHtmlElement
```

Add explicit `IAsyncHtmlElement` implementations at end of class. Pattern for each member:

```csharp
#region IAsyncHtmlElement explicit implementation

// State
async Task<bool> IAsyncHtmlElement.IsVisible()
    => await _locator.IsVisibleAsync().ConfigureAwait(false);

async Task<bool> IAsyncHtmlElement.IsEnabled()
    => await _locator.IsEnabledAsync().ConfigureAwait(false);

async Task<bool> IAsyncHtmlElement.IsSelected()
    => await _locator.IsCheckedAsync().ConfigureAwait(false);

async Task<string?> IAsyncHtmlElement.GetText()
    => await _locator.InnerTextAsync().ConfigureAwait(false);

async Task<string?> IAsyncHtmlElement.GetTagName()
    => await _locator.EvaluateAsync<string>("e => e.tagName?.toLowerCase()").ConfigureAwait(false);

// Actions
async Task IAsyncHtmlElement.Click()
    => await _locator.ClickAsync().ConfigureAwait(false);

async Task IAsyncHtmlElement.SendKeys(string text, TextInputMethod method)
{
    // Same logic as sync version but with await
    switch (method)
    {
        case TextInputMethod.Keys:
            await _locator.PressSequentiallyAsync(text).ConfigureAwait(false);
            break;
        case TextInputMethod.Paste:
            await _locator.FillAsync(text).ConfigureAwait(false);
            break;
        case TextInputMethod.SetValue:
            await _locator.EvaluateAsync($"e => e.value = {JsonEncodedText.Encode(text)}").ConfigureAwait(false);
            break;
    }
}

async Task IAsyncHtmlElement.Clear()
    => await _locator.ClearAsync().ConfigureAwait(false);

async Task IAsyncHtmlElement.DoubleClick()
    => await _locator.DblClickAsync().ConfigureAwait(false);

async Task IAsyncHtmlElement.RightClick()
    => await _locator.ClickAsync(new() { Button = MouseButton.Right }).ConfigureAwait(false);

async Task IAsyncHtmlElement.Hover()
    => await _locator.HoverAsync().ConfigureAwait(false);

async Task IAsyncHtmlElement.LongPress(int durationMs)
    => await _locator.ClickAsync(new() { Delay = durationMs }).ConfigureAwait(false);

async Task IAsyncHtmlElement.ScrollIntoView(int timeoutMs)
    => await _locator.ScrollIntoViewIfNeededAsync(new() { Timeout = timeoutMs }).ConfigureAwait(false);

// Attributes
async Task<string?> IAsyncHtmlElement.GetAttribute(string name)
    => await _locator.GetAttributeAsync(name).ConfigureAwait(false);

// HTML-specific
async Task<string> IAsyncHtmlElement.GetInnerHtml()
    => await _locator.InnerHTMLAsync().ConfigureAwait(false);

async Task<string> IAsyncHtmlElement.GetOuterHtml()
    => await _locator.EvaluateAsync<string>("e => e.outerHTML").ConfigureAwait(false);

async Task<bool> IAsyncHtmlElement.GetIsChecked()
    => await _locator.IsCheckedAsync().ConfigureAwait(false);

async Task<string> IAsyncHtmlElement.GetInputValue()
    => await _locator.InputValueAsync().ConfigureAwait(false);

async Task<string?> IAsyncHtmlElement.GetDomAttribute(string attributeName)
    => await _locator.GetAttributeAsync(attributeName).ConfigureAwait(false);

async Task<string?> IAsyncHtmlElement.GetDomProperty(string propertyName)
    => await _locator.EvaluateAsync<string?>($"e => e['{propertyName}']?.toString()").ConfigureAwait(false);

async Task<string?> IAsyncHtmlElement.GetCssValue(string propertyName)
    => await _locator.EvaluateAsync<string?>($"e => getComputedStyle(e).getPropertyValue('{propertyName}')").ConfigureAwait(false);

async Task IAsyncHtmlElement.Submit()
    => await _locator.EvaluateAsync("e => e.form?.submit() ?? e.closest('form')?.submit()").ConfigureAwait(false);

async Task IAsyncHtmlElement.Fill(string value)
    => await _locator.FillAsync(value).ConfigureAwait(false);

async Task IAsyncHtmlElement.SelectOption(string value)
    => await _locator.SelectOptionAsync(value).ConfigureAwait(false);

async Task IAsyncHtmlElement.SelectOption(string[] values)
    => await _locator.SelectOptionAsync(values).ConfigureAwait(false);

async Task IAsyncHtmlElement.Check()
    => await _locator.CheckAsync().ConfigureAwait(false);

async Task IAsyncHtmlElement.Uncheck()
    => await _locator.UncheckAsync().ConfigureAwait(false);

async Task IAsyncHtmlElement.Focus()
    => await _locator.FocusAsync().ConfigureAwait(false);

async Task IAsyncHtmlElement.Blur()
    => await _locator.BlurAsync().ConfigureAwait(false);

async Task<T?> IAsyncHtmlElement.Evaluate<T>(string expression)
    => await _locator.EvaluateAsync<T>(expression).ConfigureAwait(false);

async Task IAsyncHtmlElement.Evaluate(string expression)
    => await _locator.EvaluateAsync(expression).ConfigureAwait(false);

#endregion
```

Success criteria:

* `PlaywrightHtmlElement` implements both `IHtmlElement` (sync) and `IAsyncHtmlElement` (async)
* Zero `.GetAwaiter().GetResult()` in the async region
* Every async method uses `ConfigureAwait(false)`
* Sync members remain exactly as-is (still using `.GetAwaiter().GetResult()`)

Dependencies:

* Step 1.1 (`IAsyncHtmlElement` interface)

## Implementation Phase 3: Control Async Implementations

<!-- parallelizable: false -->

### Step 3.1: Add `IHtmlAsyncClickable<TScope>` explicit implementation to `Control<TScope>`

Files:

* [srcnew/Brinell.Html/Controls/Control.cs](srcnew/Brinell.Html/Controls/Control.cs) (35 lines) — Modify class declaration and add explicit implementations

Class declaration change:

```csharp
// FROM:
public abstract class Control<TScope> : ControlBase<TScope>
    where TScope : IHtmlScope<TScope>

// TO:
public abstract class Control<TScope> : ControlBase<TScope>,
    IHtmlAsyncClickable<TScope>
    where TScope : IHtmlScope<TScope>
```

Explicit methods to add:

```csharp
#region IHtmlAsyncClickable<TScope> explicit implementation

async Task<TScope> IHtmlAsyncClickable<TScope>.Click()
    => await RunWithElementAsync(async e => await e.Click().ConfigureAwait(false)).ConfigureAwait(false);

async Task<TScope> IHtmlAsyncClickable<TScope>.SendKeys(string text)
    => await RunWithElementAsync(async e => await e.SendKeys(text).ConfigureAwait(false)).ConfigureAwait(false);

async Task<TScope> IHtmlAsyncClickable<TScope>.Clear()
    => await RunWithElementAsync(async e => await e.Clear().ConfigureAwait(false)).ConfigureAwait(false);

async Task<TScope> IHtmlAsyncClickable<TScope>.ScrollIntoView(int timeoutMs)
    => await RunWithElementAsync(async e => await e.ScrollIntoView(timeoutMs).ConfigureAwait(false)).ConfigureAwait(false);

// DoubleClick, RightClick, Hover defined here for IHtmlAsyncClickable but implemented
// as virtual so ClickableControlBase can provide them
async Task<TScope> IHtmlAsyncClickable<TScope>.DoubleClick()
    => await RunWithElementAsync(async e => await e.DoubleClick().ConfigureAwait(false)).ConfigureAwait(false);

async Task<TScope> IHtmlAsyncClickable<TScope>.RightClick()
    => await RunWithElementAsync(async e => await e.RightClick().ConfigureAwait(false)).ConfigureAwait(false);

async Task<TScope> IHtmlAsyncClickable<TScope>.Hover()
    => await RunWithElementAsync(async e => await e.Hover().ConfigureAwait(false)).ConfigureAwait(false);

#endregion
```

Success criteria:

* All 7 `IHtmlAsyncClickable` methods implemented as explicit
* Class compiles with both sync and async interfaces
* Using directive added for `Brinell.Html.Interfaces.Async`

Dependencies:

* Phase 2 complete

### Step 3.2: Add async to `ClickableControlBase<TScope>`

`ClickableControlBase` inherits from `Control` which already implements `IHtmlAsyncClickable`. No additional async interface needed — the base class handles it. This step simply verifies compilation.

Files:

* [srcnew/Brinell.Html/Controls/ClickableControlBase.cs](srcnew/Brinell.Html/Controls/ClickableControlBase.cs) (31 lines) — No changes needed if `Control<TScope>` already covers all methods

Success criteria:

* `ClickableControlBase` compiles without changes (inherits async from `Control<TScope>`)
* Verify `DoubleClick`, `RightClick`, `Hover` sync methods still work

Note: If `ClickableControlBase` overrides sync methods and the override doesn't propagate through to the explicit async implementation, the explicit implementation in `Control<TScope>` already handles it by calling element-level async directly. No override needed.

### Step 3.3: Add `IHtmlAsyncFocusable<TScope>` to `FocusableControlBase<TScope>`

Files:

* [srcnew/Brinell.Html/Controls/FocusableControlBase.cs](srcnew/Brinell.Html/Controls/FocusableControlBase.cs) (33 lines) — Modify declaration, add explicit implementation

Class declaration change:

```csharp
// FROM:
public abstract class FocusableControlBase<TScope> : ClickableControlBase<TScope>
    where TScope : IHtmlScope<TScope>

// TO:
public abstract class FocusableControlBase<TScope> : ClickableControlBase<TScope>,
    IHtmlAsyncFocusable<TScope>
    where TScope : IHtmlScope<TScope>
```

Explicit methods to add:

```csharp
#region IHtmlAsyncFocusable<TScope> explicit implementation

async Task<TScope> IHtmlAsyncFocusable<TScope>.Focus()
    => await RunWithElementAsync(async e => await e.Focus().ConfigureAwait(false)).ConfigureAwait(false);

async Task<TScope> IHtmlAsyncFocusable<TScope>.Blur()
    => await RunWithElementAsync(async e => await e.Blur().ConfigureAwait(false)).ConfigureAwait(false);

async Task<bool> IHtmlAsyncFocusable<TScope>.HasFocus()
    => await RunWithElementAsync<bool>(async e =>
        await e.Evaluate<bool>("e => document.activeElement === e").ConfigureAwait(false)).ConfigureAwait(false);

#endregion
```

Success criteria:

* 3 focus methods added as explicit interface implementations
* Class compiles with `IHtmlAsyncFocusable<TScope>`

Dependencies:

* Step 3.1 (Control has IHtmlAsyncClickable which IHtmlAsyncFocusable inherits from)

### Step 3.4: Add `IHtmlAsyncToggle<TScope>` to `ToggleControlBase<TScope>`

Files:

* [srcnew/Brinell.Html/Controls/ToggleControlBase.cs](srcnew/Brinell.Html/Controls/ToggleControlBase.cs) (52 lines) — Modify declaration, add explicit implementation

Class declaration change:

```csharp
// FROM:
public abstract class ToggleControlBase<TScope> : ClickableControlBase<TScope>
    where TScope : IHtmlScope<TScope>

// TO:
public abstract class ToggleControlBase<TScope> : ClickableControlBase<TScope>,
    IHtmlAsyncToggle<TScope>
    where TScope : IHtmlScope<TScope>
```

Explicit methods to add:

```csharp
#region IHtmlAsyncToggle<TScope> explicit implementation

async Task<bool> IHtmlAsyncToggle<TScope>.IsChecked()
    => await RunWithElementAsync<bool>(async e =>
        await e.GetIsChecked().ConfigureAwait(false)).ConfigureAwait(false);

async Task<TScope> IHtmlAsyncToggle<TScope>.SetChecked(bool value)
{
    if (value)
        return await RunWithElementAsync(async e => await e.Check().ConfigureAwait(false)).ConfigureAwait(false);
    else
        return await RunWithElementAsync(async e => await e.Uncheck().ConfigureAwait(false)).ConfigureAwait(false);
}

async Task<bool> IHtmlAsyncToggle<TScope>.WaitChecked(bool expected, int? timeoutMs)
{
    var timeout = timeoutMs ?? DefaultTimeoutMs;
    return await PollAsync(async () =>
    {
        var element = TryFindAsyncElement();
        if (element == null) return false;
        return await element.GetIsChecked().ConfigureAwait(false) == expected;
    }, timeout).ConfigureAwait(false);
}

async Task<TScope> IHtmlAsyncToggle<TScope>.AssertChecked(bool expected)
{
    var self = (IHtmlAsyncToggle<TScope>)this;
    if (!await self.WaitChecked(expected).ConfigureAwait(false))
    {
        var actual = await self.IsChecked().ConfigureAwait(false);
        throw new AssertionException(
            $"Expected element '{Locator}' checked={expected} but was {actual}.");
    }
    return ContainingScope;
}

// Check/Uncheck/Toggle — virtual so CheckBoxControl can use them
async Task<TScope> IHtmlAsyncToggle<TScope>.Check()
    => await RunWithElementAsync(async e => await e.Check().ConfigureAwait(false)).ConfigureAwait(false);

async Task<TScope> IHtmlAsyncToggle<TScope>.Uncheck()
    => await RunWithElementAsync(async e => await e.Uncheck().ConfigureAwait(false)).ConfigureAwait(false);

async Task<TScope> IHtmlAsyncToggle<TScope>.Toggle()
{
    var self = (IHtmlAsyncToggle<TScope>)this;
    var current = await self.IsChecked().ConfigureAwait(false);
    return await self.SetChecked(!current).ConfigureAwait(false);
}

#endregion
```

Success criteria:

* 7 toggle methods implemented as explicit interface
* `WaitChecked` uses `PollAsync` with async checked-state reads
* Class compiles

Dependencies:

* Step 3.1

### Step 3.5: Add async to `CheckBoxControl`, `RadioButtonControl`

These inherit from `ToggleControlBase` which already has the `IHtmlAsyncToggle` implementation. `CheckBoxControl` has additional `Check()`/`Uncheck()`/`Toggle()` sync methods — the async versions are covered by `ToggleControlBase`'s explicit implementation of `IHtmlAsyncToggle.Check()`/`Uncheck()`/`Toggle()`.

`RadioButtonControl` has `Select()` which maps to `Click()` — async `Click` is already on `IHtmlAsyncClickable`.

Files:

* [srcnew/Brinell.Html/Controls/Toggle/CheckBoxControl.cs](srcnew/Brinell.Html/Controls/Toggle/CheckBoxControl.cs) (34 lines) — Verify compilation; no additional changes expected
* [srcnew/Brinell.Html/Controls/Toggle/RadioButtonControl.cs](srcnew/Brinell.Html/Controls/Toggle/RadioButtonControl.cs) (24 lines) — Verify compilation; no additional changes expected

Success criteria:

* Both controls compile with inherited async interfaces
* No new explicit implementations needed

Dependencies:

* Step 3.4

### Step 3.6: Add `IHtmlAsyncEditable<TScope>` to `TextInputControl`, `TextAreaControl`

Files:

* [srcnew/Brinell.Html/Controls/Text/TextInputControl.cs](srcnew/Brinell.Html/Controls/Text/TextInputControl.cs) (82 lines) — Modify declaration, add explicit implementation
* [srcnew/Brinell.Html/Controls/Text/TextAreaControl.cs](srcnew/Brinell.Html/Controls/Text/TextAreaControl.cs) (25 lines) — Verify compilation

`TextInputControl` class declaration change:

```csharp
// FROM:
public class TextInputControl<TScope> : FocusableControlBase<TScope>
    where TScope : IHtmlScope<TScope>

// TO:
public class TextInputControl<TScope> : FocusableControlBase<TScope>,
    IHtmlAsyncEditable<TScope>
    where TScope : IHtmlScope<TScope>
```

Explicit methods in `TextInputControl`:

```csharp
#region IHtmlAsyncEditable<TScope> explicit implementation

async Task<TScope> IHtmlAsyncEditable<TScope>.SetText(string text)
    => await RunWithElementAsync(async e =>
    {
        await e.Clear().ConfigureAwait(false);
        await e.Fill(text).ConfigureAwait(false);
    }).ConfigureAwait(false);

async Task<string> IHtmlAsyncEditable<TScope>.GetValue()
    => await RunWithElementAsync<string>(async e =>
        await e.GetInputValue().ConfigureAwait(false)).ConfigureAwait(false);

async Task<TScope> IHtmlAsyncEditable<TScope>.TypeText(string text)
    => await RunWithElementAsync(async e =>
        await e.SendKeys(text).ConfigureAwait(false)).ConfigureAwait(false);

async Task<TScope> IHtmlAsyncEditable<TScope>.AssertValue(string? expected)
{
    if (expected == null) return ContainingScope;
    var self = (IHtmlAsyncEditable<TScope>)this;
    var timeout = DefaultTimeoutMs;
    var matched = await PollAsync(async () =>
    {
        var value = await self.GetValue().ConfigureAwait(false);
        return value == expected;
    }, timeout).ConfigureAwait(false);

    if (!matched)
    {
        var actual = await self.GetValue().ConfigureAwait(false);
        throw new AssertionException(
            $"Expected value '{expected}' but got '{actual}' for element '{Locator}'.");
    }
    return ContainingScope;
}

async Task<TScope> IHtmlAsyncEditable<TScope>.WaitValue(string? expected, int? timeoutMs)
{
    if (expected == null) return ContainingScope;
    var timeout = timeoutMs ?? DefaultTimeoutMs;
    await PollAsync(async () =>
    {
        var self = (IHtmlAsyncEditable<TScope>)this;
        var value = await self.GetValue().ConfigureAwait(false);
        return value == expected;
    }, timeout).ConfigureAwait(false);
    return ContainingScope;
}

async Task<TScope> IHtmlAsyncEditable<TScope>.AppendText(string text)
    => await RunWithElementAsync(async e =>
    {
        await e.Focus().ConfigureAwait(false);
        await e.SendKeys(text).ConfigureAwait(false);
    }).ConfigureAwait(false);

#endregion
```

`TextAreaControl` inherits from `TextInputControl`, so it gets the async interface automatically. It may override `AppendText` sync — the async version from the explicit interface on `TextInputControl` handles the base behavior.

Success criteria:

* 6 editable methods implemented as explicit interface
* Both TextInputControl and TextAreaControl compile

Dependencies:

* Step 3.3

### Step 3.7: Add `IHtmlAsyncSelector<TScope>` to `SelectorControlBase`, `SelectControl`, `RadioGroupControl`

Files:

* [srcnew/Brinell.Html/Controls/SelectorControlBase.cs](srcnew/Brinell.Html/Controls/SelectorControlBase.cs) (23 lines) — Add interface to declaration; leave abstract async methods
* [srcnew/Brinell.Html/Controls/Selection/SelectControl.cs](srcnew/Brinell.Html/Controls/Selection/SelectControl.cs) (53 lines) — Add explicit implementations
* [srcnew/Brinell.Html/Controls/Selection/RadioGroupControl.cs](srcnew/Brinell.Html/Controls/Selection/RadioGroupControl.cs) (76 lines) — Add explicit implementations

`SelectorControlBase` declaration change:

```csharp
// FROM:
public abstract class SelectorControlBase<TScope> : FocusableControlBase<TScope>
    where TScope : IHtmlScope<TScope>

// TO:
public abstract class SelectorControlBase<TScope> : FocusableControlBase<TScope>,
    IHtmlAsyncSelector<TScope>
    where TScope : IHtmlScope<TScope>
```

Note: Since `SelectorControlBase` has abstract sync methods (`SelectByValue`, `SelectByText`, `GetSelectedValue`), the explicit async interface methods are best implemented in the concrete classes (`SelectControl`, `RadioGroupControl`) to match their control-specific logic.

`SelectControl` explicit implementations:

```csharp
#region IHtmlAsyncSelector<TScope> explicit implementation

async Task<TScope> IHtmlAsyncSelector<TScope>.SelectByValue(string value)
    => await RunWithElementAsync(async e =>
        await e.SelectOption(value).ConfigureAwait(false)).ConfigureAwait(false);

async Task<TScope> IHtmlAsyncSelector<TScope>.SelectByText(string text)
    => await RunWithElementAsync(async e =>
        await e.SelectOption(new[] { text }).ConfigureAwait(false)).ConfigureAwait(false);

async Task<string?> IHtmlAsyncSelector<TScope>.GetSelectedValue()
    => await RunWithElementAsync<string?>(async e =>
        await e.Evaluate<string>("e => e.value").ConfigureAwait(false)).ConfigureAwait(false);

async Task<TScope> IHtmlAsyncSelector<TScope>.SelectMultiple(params string[] values)
    => await RunWithElementAsync(async e =>
        await e.SelectOption(values).ConfigureAwait(false)).ConfigureAwait(false);

#endregion
```

`RadioGroupControl` explicit implementations follow the same pattern but with radio-group specific logic (finding radio buttons by value/text within the group using `Evaluate`).

Success criteria:

* `SelectorControlBase` declares `IHtmlAsyncSelector<TScope>`
* `SelectControl` and `RadioGroupControl` provide explicit implementations
* All 4 selector methods implemented

Dependencies:

* Step 3.3

### Step 3.8: Add `IHtmlAsyncRange<TScope>` to `RangeControlBase` and derived controls

Files:

* [srcnew/Brinell.Html/Controls/RangeControlBase.cs](srcnew/Brinell.Html/Controls/RangeControlBase.cs) (40 lines) — Add interface, add explicit implementation
* [srcnew/Brinell.Html/Controls/Range/RangeInputControl.cs](srcnew/Brinell.Html/Controls/Range/RangeInputControl.cs) (44 lines) — Verify compilation
* [srcnew/Brinell.Html/Controls/DateTime/DateInputControl.cs](srcnew/Brinell.Html/Controls/DateTime/DateInputControl.cs) (31 lines) — Verify compilation
* [srcnew/Brinell.Html/Controls/DateTime/TimeInputControl.cs](srcnew/Brinell.Html/Controls/DateTime/TimeInputControl.cs) (31 lines) — Verify compilation

`RangeControlBase` declaration change:

```csharp
// FROM:
public abstract class RangeControlBase<TScope> : FocusableControlBase<TScope>
    where TScope : IHtmlScope<TScope>

// TO:
public abstract class RangeControlBase<TScope> : FocusableControlBase<TScope>,
    IHtmlAsyncRange<TScope>
    where TScope : IHtmlScope<TScope>
```

Explicit methods in `RangeControlBase`:

```csharp
#region IHtmlAsyncRange<TScope> explicit implementation

async Task<string?> IHtmlAsyncRange<TScope>.GetMin()
    => await RunWithElementAsync<string?>(async e =>
        await e.GetAttribute("min").ConfigureAwait(false)).ConfigureAwait(false);

async Task<string?> IHtmlAsyncRange<TScope>.GetMax()
    => await RunWithElementAsync<string?>(async e =>
        await e.GetAttribute("max").ConfigureAwait(false)).ConfigureAwait(false);

async Task<string?> IHtmlAsyncRange<TScope>.GetStep()
    => await RunWithElementAsync<string?>(async e =>
        await e.GetAttribute("step").ConfigureAwait(false)).ConfigureAwait(false);

async Task<string> IHtmlAsyncRange<TScope>.GetValue()
    => await RunWithElementAsync<string>(async e =>
        await e.GetInputValue().ConfigureAwait(false)).ConfigureAwait(false);

async Task<TScope> IHtmlAsyncRange<TScope>.SetValue(string value)
    => await RunWithElementAsync(async e =>
        await e.Fill(value).ConfigureAwait(false)).ConfigureAwait(false);

#endregion
```

Derived controls (`RangeInputControl`, `DateInputControl`, `TimeInputControl`) inherit the async interface from `RangeControlBase`. Their type-specific sync methods (`GetNumericValue`, `SetDate`, `GetTime`, etc.) don't have corresponding async interface — these use the base async methods internally and can be wrapped separately in the extension class if needed.

Success criteria:

* 5 range methods implemented as explicit interface
* All derived controls compile
* No new interfaces needed on derived classes

Dependencies:

* Step 3.3

### Step 3.9: Add `IHtmlAsyncScrollable<TScope>` to `ScrollableControlBase`

Files:

* [srcnew/Brinell.Html/Controls/ScrollableControlBase.cs](srcnew/Brinell.Html/Controls/ScrollableControlBase.cs) (46 lines) — Modify declaration, add explicit implementation

Class declaration change:

```csharp
// FROM:
public abstract class ScrollableControlBase<TScope> : ClickableControlBase<TScope>
    where TScope : IHtmlScope<TScope>

// TO:
public abstract class ScrollableControlBase<TScope> : ClickableControlBase<TScope>,
    IHtmlAsyncScrollable<TScope>
    where TScope : IHtmlScope<TScope>
```

Explicit methods to add:

```csharp
#region IHtmlAsyncScrollable<TScope> explicit implementation

async Task<TScope> IHtmlAsyncScrollable<TScope>.ScrollTo(int x, int y)
    => await RunWithElementAsync(async e =>
        await e.Evaluate($"e => e.scrollTo({x}, {y})").ConfigureAwait(false)).ConfigureAwait(false);

async Task<TScope> IHtmlAsyncScrollable<TScope>.ScrollToTop()
    => await RunWithElementAsync(async e =>
        await e.Evaluate("e => e.scrollTo(0, 0)").ConfigureAwait(false)).ConfigureAwait(false);

#endregion
```

Success criteria:

* 2 scroll methods implemented as explicit interface
* Class compiles

Dependencies:

* Step 3.1

### Step 3.10: Add async to leaf controls

These controls either inherit all async methods from their base class or need minimal additions for their unique sync methods. For most leaf controls, no code changes are needed — they inherit the async interfaces from their base classes.

Files:

* [srcnew/Brinell.Html/Controls/Buttons/ButtonControl.cs](srcnew/Brinell.Html/Controls/Buttons/ButtonControl.cs) (31 lines) — `Submit()` needs async. Inherits `IHtmlAsyncClickable` from `ClickableControlBase`. Add `SubmitAsync` as a public method (no interface — unique to button).
* [srcnew/Brinell.Html/Controls/Buttons/LinkControl.cs](srcnew/Brinell.Html/Controls/Buttons/LinkControl.cs) (43 lines) — `Href` property and `AssertHref` need async. Inherits `IHtmlAsyncClickable`. Add async methods as public.
* [srcnew/Brinell.Html/Controls/Display/LabelControl.cs](srcnew/Brinell.Html/Controls/Display/LabelControl.cs) (59 lines) — Has `IsTextContaining`/`WaitTextContaining`/`AssertTextContaining`. Inherits `IHtmlAsyncControlObject`. Add async as public methods.
* [srcnew/Brinell.Html/Controls/Display/ProgressControl.cs](srcnew/Brinell.Html/Controls/Display/ProgressControl.cs) (51 lines) — Has `GetValue`/`GetMax`/`GetPercentage`/`AssertValue`. Inherits `IHtmlAsyncControlObject`. Add async as public methods.
* [srcnew/Brinell.Html/Controls/Collection/ListControl.cs](srcnew/Brinell.Html/Controls/Collection/ListControl.cs) (39 lines) — Has `ItemCount`/`GetItemText`/`GetItemTexts`. Add async as public methods.
* [srcnew/Brinell.Html/Controls/Collection/TableControl.cs](srcnew/Brinell.Html/Controls/Collection/TableControl.cs) (73 lines) — Has `RowCount`/`ColumnCount`/`GetCellText`/`GetHeaderText`/`GetRowTexts`. Add async as public methods.
* [srcnew/Brinell.Html/Controls/List.cs](srcnew/Brinell.Html/Controls/List.cs) (42 lines) — Has `Count`/`GetItemText`/`GetItemTexts`. Add async as public methods.

Pattern for leaf control async methods (public, not explicit interface — these don't have a matching interface):

```csharp
// ButtonControl
public async Task<TScope> SubmitAsync()
    => await RunWithElementAsync(async e => await e.Submit().ConfigureAwait(false)).ConfigureAwait(false);

// LinkControl
public async Task<string?> GetHrefAsync()
    => await RunWithElementAsync<string?>(async e =>
        await e.GetAttribute("href").ConfigureAwait(false)).ConfigureAwait(false);

public async Task<TScope> AssertHrefAsync(string? expected)
{
    var actual = await GetHrefAsync().ConfigureAwait(false);
    if (actual != expected)
        throw new AssertionException($"Expected href '{expected}' but got '{actual ?? "(null)"}'.");
    return ContainingScope;
}
```

Success criteria:

* All leaf controls compile
* Unique methods get `*Async` public counterparts
* Extension class covers the interface-based methods

Dependencies:

* Phase 2 for `RunWithElementAsync`

### Step 3.11: Add async to `ContainerBase`, `ScrollContainerControl`, `TabContainerControl`

`ContainerBase` does not extend `ControlBase` — it extends `ObjectBase` directly and implements `IHtmlContainer`. It needs async methods for `IsReady`/`WaitReady`/`FindElement`/`TryFindElement`/`FindElements`.

Files:

* [srcnew/Brinell.Html/Controls/ContainerBase.cs](srcnew/Brinell.Html/Controls/ContainerBase.cs) (67 lines) — Add async methods as public
* [srcnew/Brinell.Html/Controls/Container/ScrollContainerControl.cs](srcnew/Brinell.Html/Controls/Container/ScrollContainerControl.cs) (33 lines) — Add async scroll methods
* [srcnew/Brinell.Html/Controls/Container/TabContainerControl.cs](srcnew/Brinell.Html/Controls/Container/TabContainerControl.cs) (48 lines) — Add async tab methods

`ContainerBase` async additions:

```csharp
public async Task<bool> IsReadyAsync(int? timeoutMs = null) => IsReady(timeoutMs);

public async Task<bool> WaitReadyAsync(int? timeoutMs = null)
{
    var timeout = timeoutMs ?? Context.Timeouts.PageLoad;
    return await PollAsync(async () => IsReady(), timeout).ConfigureAwait(false);
}
```

Note: Container element-finding methods delegate to the test context. Async element-finding is covered by `PlaywrightTestContext` async methods (Phase 5).

Success criteria:

* All container classes compile
* Container provides async ready/wait methods

Dependencies:

* Step 2.1 for `PollAsync`

## Implementation Phase 4: Extension Method Bridge

<!-- parallelizable: false -->

### Step 4.1: Create `HtmlAsyncExtensions` class

Create a single static class with extension methods for every async interface method. Methods use `*Async` suffix and delegate directly to the explicit interface method.

Files:

* [srcnew/Brinell.Html/HtmlAsyncExtensions.cs](srcnew/Brinell.Html/HtmlAsyncExtensions.cs) — NEW FILE

Structure:

```csharp
using Brinell.Html.Interfaces;
using Brinell.Html.Interfaces.Async;

namespace Brinell.Html;

public static class HtmlAsyncExtensions
{
    #region IHtmlAsyncControlObject<TScope>

    public static Task<bool> IsExistsAsync<TScope>(this IHtmlAsyncControlObject<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.IsExists();

    public static Task<bool?> IsVisibleAsync<TScope>(this IHtmlAsyncControlObject<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.IsVisible();

    public static Task<bool?> IsEnabledAsync<TScope>(this IHtmlAsyncControlObject<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.IsEnabled();

    public static Task<bool> WaitExistsAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        bool? expected, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.WaitExists(expected, timeoutMs);

    public static Task<bool> WaitVisibleAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        bool? expected, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.WaitVisible(expected, timeoutMs);

    public static Task<bool> WaitEnabledAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        bool? expected, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.WaitEnabled(expected, timeoutMs);

    public static Task<TScope> AssertExistsAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        bool? expected, string? message = null, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.AssertExists(expected, message, timeoutMs);

    public static Task<TScope> AssertVisibleAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        bool? expected, string? message = null, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.AssertVisible(expected, message, timeoutMs);

    public static Task<TScope> AssertEnabledAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        bool? expected, string? message = null, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.AssertEnabled(expected, message, timeoutMs);

    public static Task<string?> GetTextAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.GetText(timeoutMs);

    public static Task<bool> WaitTextAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        string? expected, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.WaitText(expected, timeoutMs);

    public static Task<TScope> AssertTextAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        string? expected, string? message = null, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.AssertText(expected, message, timeoutMs);

    public static Task<TScope> AssertTextContainsAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        string? expected, string? message = null, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.AssertTextContains(expected, message, timeoutMs);

    public static Task<string?> GetAttributeAsync<TScope>(this IHtmlAsyncControlObject<TScope> control,
        string name)
        where TScope : IHtmlScope<TScope>
        => control.GetAttribute(name);

    #endregion

    #region IHtmlAsyncClickable<TScope>

    public static Task<TScope> ClickAsync<TScope>(this IHtmlAsyncClickable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.Click();

    public static Task<TScope> SendKeysAsync<TScope>(this IHtmlAsyncClickable<TScope> control, string text)
        where TScope : IHtmlScope<TScope>
        => control.SendKeys(text);

    public static Task<TScope> ClearAsync<TScope>(this IHtmlAsyncClickable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.Clear();

    public static Task<TScope> ScrollIntoViewAsync<TScope>(this IHtmlAsyncClickable<TScope> control,
        int timeoutMs = 5000)
        where TScope : IHtmlScope<TScope>
        => control.ScrollIntoView(timeoutMs);

    public static Task<TScope> DoubleClickAsync<TScope>(this IHtmlAsyncClickable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.DoubleClick();

    public static Task<TScope> RightClickAsync<TScope>(this IHtmlAsyncClickable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.RightClick();

    public static Task<TScope> HoverAsync<TScope>(this IHtmlAsyncClickable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.Hover();

    #endregion

    #region IHtmlAsyncFocusable<TScope>

    public static Task<TScope> FocusAsync<TScope>(this IHtmlAsyncFocusable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.Focus();

    public static Task<TScope> BlurAsync<TScope>(this IHtmlAsyncFocusable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.Blur();

    public static Task<bool> HasFocusAsync<TScope>(this IHtmlAsyncFocusable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.HasFocus();

    #endregion

    #region IHtmlAsyncToggle<TScope>

    public static Task<bool> IsCheckedAsync<TScope>(this IHtmlAsyncToggle<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.IsChecked();

    public static Task<TScope> SetCheckedAsync<TScope>(this IHtmlAsyncToggle<TScope> control, bool value)
        where TScope : IHtmlScope<TScope>
        => control.SetChecked(value);

    public static Task<bool> WaitCheckedAsync<TScope>(this IHtmlAsyncToggle<TScope> control,
        bool expected, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.WaitChecked(expected, timeoutMs);

    public static Task<TScope> AssertCheckedAsync<TScope>(this IHtmlAsyncToggle<TScope> control,
        bool expected)
        where TScope : IHtmlScope<TScope>
        => control.AssertChecked(expected);

    public static Task<TScope> CheckAsync<TScope>(this IHtmlAsyncToggle<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.Check();

    public static Task<TScope> UncheckAsync<TScope>(this IHtmlAsyncToggle<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.Uncheck();

    public static Task<TScope> ToggleAsync<TScope>(this IHtmlAsyncToggle<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.Toggle();

    #endregion

    #region IHtmlAsyncEditable<TScope>

    public static Task<TScope> SetTextAsync<TScope>(this IHtmlAsyncEditable<TScope> control, string text)
        where TScope : IHtmlScope<TScope>
        => control.SetText(text);

    public static Task<string> GetValueAsync<TScope>(this IHtmlAsyncEditable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.GetValue();

    public static Task<TScope> TypeTextAsync<TScope>(this IHtmlAsyncEditable<TScope> control, string text)
        where TScope : IHtmlScope<TScope>
        => control.TypeText(text);

    public static Task<TScope> AssertValueAsync<TScope>(this IHtmlAsyncEditable<TScope> control,
        string? expected)
        where TScope : IHtmlScope<TScope>
        => control.AssertValue(expected);

    public static Task<TScope> WaitValueAsync<TScope>(this IHtmlAsyncEditable<TScope> control,
        string? expected, int? timeoutMs = null)
        where TScope : IHtmlScope<TScope>
        => control.WaitValue(expected, timeoutMs);

    public static Task<TScope> AppendTextAsync<TScope>(this IHtmlAsyncEditable<TScope> control,
        string text)
        where TScope : IHtmlScope<TScope>
        => control.AppendText(text);

    #endregion

    #region IHtmlAsyncSelector<TScope>

    public static Task<TScope> SelectByValueAsync<TScope>(this IHtmlAsyncSelector<TScope> control,
        string value)
        where TScope : IHtmlScope<TScope>
        => control.SelectByValue(value);

    public static Task<TScope> SelectByTextAsync<TScope>(this IHtmlAsyncSelector<TScope> control,
        string text)
        where TScope : IHtmlScope<TScope>
        => control.SelectByText(text);

    public static Task<string?> GetSelectedValueAsync<TScope>(this IHtmlAsyncSelector<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.GetSelectedValue();

    public static Task<TScope> SelectMultipleAsync<TScope>(this IHtmlAsyncSelector<TScope> control,
        params string[] values)
        where TScope : IHtmlScope<TScope>
        => control.SelectMultiple(values);

    #endregion

    #region IHtmlAsyncRange<TScope>

    public static Task<string?> GetMinAsync<TScope>(this IHtmlAsyncRange<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.GetMin();

    public static Task<string?> GetMaxAsync<TScope>(this IHtmlAsyncRange<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.GetMax();

    public static Task<string?> GetStepAsync<TScope>(this IHtmlAsyncRange<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.GetStep();

    public static Task<string> GetRangeValueAsync<TScope>(this IHtmlAsyncRange<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.GetValue();

    public static Task<TScope> SetRangeValueAsync<TScope>(this IHtmlAsyncRange<TScope> control,
        string value)
        where TScope : IHtmlScope<TScope>
        => control.SetValue(value);

    #endregion

    #region IHtmlAsyncScrollable<TScope>

    public static Task<TScope> ScrollToAsync<TScope>(this IHtmlAsyncScrollable<TScope> control,
        int x, int y)
        where TScope : IHtmlScope<TScope>
        => control.ScrollTo(x, y);

    public static Task<TScope> ScrollToTopAsync<TScope>(this IHtmlAsyncScrollable<TScope> control)
        where TScope : IHtmlScope<TScope>
        => control.ScrollToTop();

    #endregion
}
```

Success criteria:

* One extension method per async interface method (~50 extension methods total)
* All extensions compile and delegate directly to the explicit interface method
* Test code can call `control.ClickAsync()`, `control.AssertTextAsync()` etc. naturally via IntelliSense

Dependencies:

* Phases 1-3 complete (all interfaces and implementations exist)

## Implementation Phase 5: PlaywrightTestContext + Page Object Async

<!-- parallelizable: false -->

### Step 5.1: Add async methods to `PlaywrightTestContext`

Add async counterparts for navigation, screenshot, and element-finding methods.

Files:

* [srcnew/Brinell.Html.Playwright/PlaywrightTestContext.cs](srcnew/Brinell.Html.Playwright/PlaywrightTestContext.cs) (330 lines) — Add async methods at end of class

Methods to add:

```csharp
#region Async Methods

public async Task NavigateToAsync(string destination)
{
    var page = InternalFrame is not null
        ? throw new InvalidOperationException("Cannot navigate within a frame context.")
        : InternalPage;
    await page.GotoAsync(destination).ConfigureAwait(false);
}

public async Task NavigateBackAsync()
    => await InternalPage.GoBackAsync().ConfigureAwait(false);

public async Task GoForwardAsync()
    => await InternalPage.GoForwardAsync().ConfigureAwait(false);

public async Task RefreshAsync()
    => await InternalPage.ReloadAsync().ConfigureAwait(false);

public async Task<byte[]> TakeScreenshotAsync()
    => await InternalPage.ScreenshotAsync().ConfigureAwait(false);

public async Task SaveScreenshotAsync(string path)
    => await InternalPage.ScreenshotAsync(new() { Path = path }).ConfigureAwait(false);

public async Task ResetAppStateAsync()
{
    var browserContext = InternalPage.Context;
    await browserContext.ClearCookiesAsync().ConfigureAwait(false);
    await InternalPage.EvaluateAsync("() => localStorage.clear()").ConfigureAwait(false);
    await InternalPage.EvaluateAsync("() => sessionStorage.clear()").ConfigureAwait(false);
}

public async Task<bool> WaitReadyAsync(int? timeoutMs = null)
{
    var timeout = timeoutMs ?? Timeouts.PageLoad;
    try
    {
        await InternalPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded,
            new() { Timeout = timeout }).ConfigureAwait(false);
        return true;
    }
    catch (TimeoutException)
    {
        return false;
    }
}

#endregion
```

Success criteria:

* All navigation, screenshot, and lifecycle methods have async counterparts
* No `.GetAwaiter().GetResult()` in async methods
* `ConfigureAwait(false)` on every await

Dependencies:

* Phase 0 (Thread.Sleep fixes)

### Step 5.2: Add async page methods to `HtmlPageObjectBase`

Files:

* [srcnew/Brinell.Html/Pages/HtmlPageObjectBase.cs](srcnew/Brinell.Html/Pages/HtmlPageObjectBase.cs) (122 lines) — Add async methods

Methods to add:

```csharp
#region Async Methods

public async Task<bool> WaitLoadedAsync(bool? expected, int? timeoutMs = null)
{
    if (expected == null) return true;
    var timeout = timeoutMs ?? _context.Timeouts.PageLoad;
    return await PollAsync(async () => IsLoaded() == expected.Value, timeout).ConfigureAwait(false);
}

public async Task AssertLoadedAsync(bool? expected, string? message = null, int? timeoutMs = null)
{
    if (expected == null) return;
    if (!await WaitLoadedAsync(expected, timeoutMs).ConfigureAwait(false))
    {
        var actual = IsLoaded();
        throw new PageLoadException(
            message ?? $"Expected page '{Name}' {(expected.Value ? "to be loaded" : "not to be loaded")} but loaded state is {actual}.");
    }
}

public async Task<bool> WaitTitleAsync(string? expected, int? timeoutMs = null)
{
    if (expected == null) return true;
    var timeout = timeoutMs ?? _context.Timeouts.DefaultWait;
    return await PollAsync(async () => GetTitle() == expected, timeout).ConfigureAwait(false);
}

public async Task AssertTitleAsync(string? expected, string? message = null, int? timeoutMs = null)
{
    if (expected == null) return;
    if (!await WaitTitleAsync(expected, timeoutMs).ConfigureAwait(false))
    {
        var actual = GetTitle();
        throw new PageLoadException(
            message ?? $"Expected page title '{expected}' but got '{actual ?? "(null)"}'.");
    }
}

#endregion
```

Success criteria:

* Page object has async wait/assert methods
* Uses `PollAsync` from `ObjectBase`

Dependencies:

* Step 2.1

### Step 5.3: Add `NavigateToPageAsync` to `BlazorSampleTestBase`

Files:

* [testsnew/Brinell.Html.UITests/TestBase/BlazorSampleTestBase.cs](testsnew/Brinell.Html.UITests/TestBase/BlazorSampleTestBase.cs) (39 lines) — Add async navigation method

Method to add (alongside existing `NavigateToPage`):

```csharp
protected async Task NavigateToPageAsync(string path)
{
    var destination = $"{BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    // Cast to PlaywrightTestContext to access async method
    if (Context is PlaywrightTestContext pwContext)
    {
        await pwContext.NavigateToAsync(destination).ConfigureAwait(false);
    }
    else
    {
        Context.NavigateTo(destination);
    }
}
```

Success criteria:

* `NavigateToPageAsync` compiles and uses true async navigation
* Existing `NavigateToPage` stays unchanged
* Using added for `Brinell.Html.Playwright`

Dependencies:

* Step 5.1

## Implementation Phase 6: Test Migration

<!-- parallelizable: false -->

### Step 6.1: Add async versions of `ButtonControlTests`

Files:

* [testsnew/Brinell.Html.UITests/Tests/Controls/ButtonControlTests.cs](testsnew/Brinell.Html.UITests/Tests/Controls/ButtonControlTests.cs) (32 lines) — Add async test methods alongside existing sync

Add after existing sync tests:

```csharp
[Fact]
public async Task Button_Click_IncrementsCounter_Async()
{
    await NavigateToPageAsync("/counter");
    var page = new CounterPage(Context);

    await page.CountDisplay.AssertTextAsync("Current count: 0");
    await page.IncrementButton.ClickAsync();
    await page.CountDisplay.AssertTextAsync("Current count: 1");
}

[Fact]
public async Task Button_IsVisible_ReturnsTrueForVisibleButton_Async()
{
    await NavigateToPageAsync("/counter");
    var page = new CounterPage(Context);

    Assert.True(await page.IncrementButton.IsVisibleAsync());
}

[Fact]
public async Task Button_AssertEnabled_PassesForEnabledButton_Async()
{
    await NavigateToPageAsync("/counter");
    var page = new CounterPage(Context);

    await page.IncrementButton.AssertEnabledAsync(true);
}
```

Success criteria:

* 3 async test methods added
* Use `*Async` extension methods
* Existing 3 sync tests unchanged

Dependencies:

* Phase 4 (extension methods), Phase 5 (NavigateToPageAsync)

### Step 6.2: Add async versions of `CounterPageTests`

Files:

* [testsnew/Brinell.Html.UITests/Tests/Pages/CounterPageTests.cs](testsnew/Brinell.Html.UITests/Tests/Pages/CounterPageTests.cs) (30 lines) — Add async test methods

Add after existing sync tests:

```csharp
[Fact]
public async Task Counter_MultipleIncrements_DisplaysCorrectCount_Async()
{
    await NavigateToPageAsync("/counter");
    var page = new CounterPage(Context);

    await page.IncrementButton.ClickAsync();
    await page.IncrementButton.ClickAsync();
    await page.IncrementButton.ClickAsync();

    await page.CountDisplay.AssertTextAsync("Current count: 3");
}

[Fact]
public async Task Counter_ResetAfterIncrements_DisplaysZero_Async()
{
    await NavigateToPageAsync("/counter");
    var page = new CounterPage(Context);

    await page.IncrementButton.ClickAsync();
    await page.IncrementButton.ClickAsync();
    await page.ResetButton.ClickAsync();

    await page.CountDisplay.AssertTextAsync("Current count: 0");
}
```

Success criteria:

* 2 async test methods added
* Using added for `Brinell.Html` namespace (for extension methods)
* Existing sync tests unchanged

### Step 6.3: Add async versions of remaining test classes

Apply the same pattern to:

* [testsnew/Brinell.Html.UITests/Tests/Controls/CheckBoxControlTests.cs](testsnew/Brinell.Html.UITests/Tests/Controls/CheckBoxControlTests.cs) — Async versions of checkbox tests using `IsCheckedAsync`, `SetCheckedAsync`, `CheckAsync`, `UncheckAsync`
* [testsnew/Brinell.Html.UITests/Tests/Controls/SelectControlTests.cs](testsnew/Brinell.Html.UITests/Tests/Controls/SelectControlTests.cs) — Async versions using `SelectByValueAsync`, `GetSelectedValueAsync`
* [testsnew/Brinell.Html.UITests/Tests/Controls/TextInputControlTests.cs](testsnew/Brinell.Html.UITests/Tests/Controls/TextInputControlTests.cs) — Async versions using `SetTextAsync`, `GetValueAsync`, `AssertValueAsync`
* [testsnew/Brinell.Html.UITests/Tests/Pages/LoginPageTests.cs](testsnew/Brinell.Html.UITests/Tests/Pages/LoginPageTests.cs) — Async login flow tests
* [testsnew/Brinell.Html.UITests/Tests/Scenarios/LoginFlowTests.cs](testsnew/Brinell.Html.UITests/Tests/Scenarios/LoginFlowTests.cs) — Full async E2E scenario

Pattern: For each existing `[Fact] public void TestName()`, add `[Fact] public async Task TestName_Async()` using `*Async` extension methods throughout.

Success criteria:

* Every existing sync test has an async counterpart
* All async tests use `await` and `*Async` extension methods
* No sync tests modified

Dependencies:

* Steps 6.1 and 6.2 establish the pattern

## Implementation Phase 7: Final Validation

<!-- parallelizable: false -->

### Step 7.1: Run full project validation

Execute all validation commands for the project:

* `dotnet build srcnew/Brinell.sln` — full solution build
* `dotnet test testsnew/Brinell.Html.UITests/` — all tests (sync + async)
* Search for remaining `Thread.Sleep` calls: `grep -r "Thread\.Sleep" srcnew/ testsnew/Brinell.Html.UITests/`
* Verify no `.GetAwaiter().GetResult()` in async regions

### Step 7.2: Fix minor validation issues

Iterate on build errors, warnings, and test failures. Apply fixes directly when corrections are straightforward.

### Step 7.3: Report blocking issues

When validation failures require changes beyond minor fixes:

* Document the issues and affected files.
* Provide next steps.
* Recommend additional planning rather than inline fixes.

## Dependencies

* .NET 8+ SDK
* Microsoft.Playwright NuGet package
* xUnit test framework
* Brinell.Core (read-only dependency)

## Success Criteria

* `dotnet build srcnew/Brinell.sln` passes with zero errors
* `dotnet test testsnew/Brinell.Html.UITests/` — all sync tests pass, all async tests pass
* Zero `Thread.Sleep` calls in the entire HTML/Playwright codebase
* Extension methods provide `*Async` IntelliSense for every control method
* No changes to `Brinell.Core`, desktop platform projects, or existing sync API signatures
