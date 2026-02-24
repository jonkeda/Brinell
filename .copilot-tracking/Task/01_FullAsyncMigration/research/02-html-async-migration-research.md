<!-- markdownlint-disable-file -->
# Task Research: HTML/Blazor Async Migration

Add async support to the HTML/Blazor (Playwright) stack while keeping all other platforms and `Brinell.Core` unchanged. Expose both sync and async APIs so existing tests keep working.

## Task Implementation Requests

* Add async interfaces in `Brinell.Html` mirroring each existing sync capability
* Implement async on `PlaywrightHtmlElement` and `PlaywrightTestContext` — eliminate sync-over-async for the async path
* Keep the existing sync API fully intact — nothing breaks
* Update HTML/Blazor UI tests to exercise the async API
* Fix the 2 `Thread.Sleep` violations found in `PlaywrightTestContext`

## Scope and Success Criteria

* Scope: `Brinell.Html`, `Brinell.Html.Playwright`, `testsnew/Brinell.Html.UITests/` only
* Exclusions: `Brinell.Core` (zero changes), `Brinell.Wpf`, `Brinell.WinForms`, `Brinell.Maui`, `Brinell.Stride` (zero changes)
* Assumptions:
  * `srcnew/` and `testsnew/` are the active codebases
  * No external consumers — interface additions are safe
  * xUnit supports `async Task` test methods natively
* Success Criteria:
  * Every public method on every HTML control class has an async counterpart accessible via extension method (`ClickAsync`, `EnterAsync`, etc.)
  * `PlaywrightHtmlElement` implements `IAsyncHtmlElement` with proper `await` (no `.GetAwaiter().GetResult()` in the async path)
  * `PlaywrightTestContext` has async navigation, screenshot, and element-finding methods
  * HTML UI tests demonstrate the async API working end-to-end
  * Existing sync tests compile and pass without modification
  * Zero `Thread.Sleep` calls remain in the codebase

## Decisions

### D1: Interface naming — same names vs Async suffix

How should sync and async method overloads coexist on the same class?

- [x] **Approach A: Same names on separate interfaces + extension method bridge** — Async interfaces declare `Click()` returning `Task<TScope>` (same name as sync). Implementations use explicit interface implementation. Extension methods (`ClickAsync()`) provide ergonomic access. *(Clean interface symmetry; tested and compiles; see subagent/02 proof)*
- [ ] **Approach B: Async suffix on interface methods** — Interfaces declare `ClickAsync()` directly. Both sync and async are regular public methods. Simpler implementation but doubles IntelliSense. *(Pragmatic fallback; standard .NET convention)*

> Evidence: [subagent/02-explicit-interface-pattern.md](.copilot-tracking/Task/01_FullAsyncMigration/subagent/02-explicit-interface-pattern.md) — compilable proof, 4-approach comparison

### D2: `Wait*` async strategy — Playwright-native vs framework polling

How should async `WaitExists`, `WaitVisible`, `WaitEnabled`, `WaitText`, `WaitChecked` be implemented?

- [x] **Hybrid: Playwright `WaitForAsync` for existence/visibility; framework `PollAsync` with short inner timeouts for the rest** — `WaitExistsAsync` → `WaitForAsync(Attached/Detached)`, `WaitVisibleAsync` → `WaitForAsync(Visible/Hidden)` (event-driven, single timeout, efficient). `WaitEnabledAsync`/`WaitTextAsync`/`WaitCheckedAsync` → framework `PollAsync` loop with short 1s per-call Playwright timeouts to prevent the 30s-per-iteration problem. *(Best of both worlds; eliminates timeout conflicts)*
- [ ] **Framework `PollAsync` everywhere** — All Wait methods use `await Task.Delay` loop with Playwright instant or short-timeout APIs. Simpler but less efficient for existence/visibility.
- [ ] **Playwright `WaitForAsync` everywhere** — Only possible for existence/visibility states; Playwright doesn't have `WaitForEnabled` or `WaitForText`.

> Evidence: [subagent/03-playwright-timeout-analysis.md](.copilot-tracking/Task/01_FullAsyncMigration/subagent/03-playwright-timeout-analysis.md) — timeout category analysis, conflict scenarios

### D3: `IHtmlElement` properties — async counterparts via separate `IAsyncHtmlElement`

How should the low-level element layer handle async?

- [x] **New `IAsyncHtmlElement` interface** — Async methods for every `IHtmlElement` member (`Task Click()`, `Task<bool> IsVisible()`, `Task<string?> GetText()`, etc.). `PlaywrightHtmlElement` implements both. Sync properties remain backed by `.GetAwaiter().GetResult()`. *(Enables async all the way down; `RunWithElementAsync` in base classes can use the async element methods)*
- [ ] **No async element interface** — async only at the control-object level; `RunWithElementAsync` wraps sync element calls in `Task.Run`. *(Simpler but defeats the purpose — async path still blocks)*

> Evidence: [subagent/01-html-stack-inventory.md](.copilot-tracking/Task/01_FullAsyncMigration/subagent/01-html-stack-inventory.md) — 40 blocking calls in `PlaywrightHtmlElement`

## Outline

1. Async interface hierarchy design (Brinell.Html)
2. Extension method bridge pattern
3. `ControlBase` async helpers (`RunWithElementAsync`, `PollAsync`)
4. `PlaywrightHtmlElement` async implementation
5. `PlaywrightTestContext` async methods
6. Playwright timeout handling strategy
7. HTML test migration
8. Migration order and file inventory
9. Known issues and pre-migration fixes

### Potential Next Research

* **`ValueTask` vs `Task`** — some methods (like `IsExists` on desktop fake implementations) may benefit from `ValueTask` to avoid allocation. Deferred since scope is HTML-only.
  * Reasoning: HTML Playwright calls are always truly async (I/O bound); `Task` is fine. `ValueTask` matters only if we ever expand to desktop.
* **Parallel test execution** — how xUnit's async test parallelism interacts with Playwright's single-browser context.
  * Reasoning: Out of scope for this migration; can be researched separately.

## Research Executed

### File Analysis

* [srcnew/Brinell.Html/Controls/ControlBase.cs](srcnew/Brinell.Html/Controls/ControlBase.cs) — 14 public methods needing async counterparts; `RunWithElement` (3 overloads) is the central hub
* [srcnew/Brinell.Html/Controls/Control.cs](srcnew/Brinell.Html/Controls/Control.cs) — 4 methods (`Click`, `SendKeys`, `Clear`, `ScrollIntoView`)
* [srcnew/Brinell.Html/Controls/ClickableControlBase.cs](srcnew/Brinell.Html/Controls/ClickableControlBase.cs) — 3 methods (`DoubleClick`, `RightClick`, `Hover`)
* [srcnew/Brinell.Html/Controls/ToggleControlBase.cs](srcnew/Brinell.Html/Controls/ToggleControlBase.cs) — 4 methods
* [srcnew/Brinell.Html/ObjectBase.cs](srcnew/Brinell.Html/ObjectBase.cs) — `Poll()` needs `PollAsync()` counterpart
* [srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs](srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs) — **40** `.GetAwaiter().GetResult()` calls
* [srcnew/Brinell.Html.Playwright/PlaywrightTestContext.cs](srcnew/Brinell.Html.Playwright/PlaywrightTestContext.cs) — **13** `.GetAwaiter().GetResult()` calls + **2** `Thread.Sleep(100)` violations
* [srcnew/Brinell.Html.Playwright/LocatorExtensions.cs](srcnew/Brinell.Html.Playwright/LocatorExtensions.cs) — No changes needed (pure sync mapping)

### Code Search Results

* `GetAwaiter().GetResult()` — **53 total** across `PlaywrightHtmlElement` (40) and `PlaywrightTestContext` (13)
* `Thread.Sleep` — **2 violations** in `PlaywrightTestContext` (`FindElement` line ~200, `WaitReady` line ~162)
* HTML controls implementing `IControlObject<TScope>` — only `ControlBase` implements it directly; no controls implement `IClickableControlObject` or `IToggleControlObject` from Core (HTML has its own non-interface-bound API shapes)

### Project Conventions

* .NET conventions: `ConfigureAwait(false)` in library code, `Async` suffix on factory/lifecycle methods only
* Brinell rules: no `Thread.Sleep`, no empty catch blocks, no arbitrary `Task.Delay`
* Copilot instructions followed: `copilot-instructions.md`, `markdown.instructions.md`, `writing-style.instructions.md`

## Key Discoveries

### Project Structure

**22 control/base classes** across the HTML hierarchy with **~95 public methods** and **~15 properties** needing async counterparts. The inheritance chain is:

```text
ObjectBase (Poll, PollAsync)
└── ControlBase<TScope>         → IControlObject<TScope> [sync] + IHtmlAsyncControlObject<TScope> [async]
    ├── Control<TScope>         → Click, SendKeys, Clear, ScrollIntoView
    │   ├── ClickableControlBase<TScope>  → DoubleClick, RightClick, Hover
    │   │   ├── FocusableControlBase<TScope>
    │   │   │   ├── SelectorControlBase<TScope> → SelectByValue, SelectByText
    │   │   │   │   ├── SelectControl<TScope>
    │   │   │   │   └── RadioGroupControl<TScope>
    │   │   │   ├── RangeControlBase<TScope>
    │   │   │   │   ├── DateInputControl<TScope>
    │   │   │   │   ├── TimeInputControl<TScope>
    │   │   │   │   └── RangeInputControl<TScope>
    │   │   │   └── TextInputControl<TScope>
    │   │   │       └── TextAreaControl<TScope>
    │   │   ├── ToggleControlBase<TScope> → SetChecked, IsChecked
    │   │   │   ├── CheckBoxControl<TScope>
    │   │   │   └── RadioButtonControl<TScope>
    │   │   └── ScrollableControlBase<TScope>
    │   └── ButtonControl<TScope>
    │       └── LinkControl<TScope>
    ├── LabelControl<TScope>
    ├── ProgressControl<TScope>
    ├── ListControl<TScope>
    ├── TableControl<TScope>
    └── List<TScope>
```

### Implementation Patterns

**Central hub pattern — `RunWithElementAsync`:**

```csharp
// In ControlBase<TScope> — the single async helper all derived controls use
protected async Task<TScope> RunWithElementAsync(Func<IAsyncHtmlElement, Task> action)
{
    var element = FindElement(); // returns IHtmlElement, which PlaywrightHtmlElement also casts to IAsyncHtmlElement
    var asyncElement = (IAsyncHtmlElement)element;
    await action(asyncElement).ConfigureAwait(false);
    return ContainingScope;
}

protected async Task<TResult> RunWithElementAsync<TResult>(Func<IAsyncHtmlElement, Task<TResult>> action)
{
    var element = FindElement();
    var asyncElement = (IAsyncHtmlElement)element;
    return await action(asyncElement).ConfigureAwait(false);
}
```

**Derived control async (example from Control&lt;TScope&gt;):**

```csharp
// Explicit interface implementation for IHtmlAsyncClickable<TScope>
async Task<TScope> IHtmlAsyncClickable<TScope>.Click(int? timeoutMs)
{
    return await RunWithElementAsync(async e => await e.Click().ConfigureAwait(false)).ConfigureAwait(false);
}
```

**Extension method bridge:**

```csharp
public static class HtmlAsyncExtensions
{
    public static Task<TScope> ClickAsync<TScope>(this IHtmlAsyncClickable<TScope> control, int? timeoutMs = null)
        => control.Click(timeoutMs);

    public static Task<bool> IsExistsAsync<TScope>(this IHtmlAsyncControlObject<TScope> control)
        => control.IsExists();

    public static Task<TScope> AssertExistsAsync<TScope>(
        this IHtmlAsyncControlObject<TScope> control,
        bool? expected, string? message = null, int? timeoutMs = null)
        => control.AssertExists(expected, message, timeoutMs);
}
```

**Test consumption:**

```csharp
// Sync (completely unchanged)
[Fact]
public void Button_Click_IncrementsCounter()
{
    NavigateToPage("/counter");
    var page = new CounterPage(Context);
    page.CountDisplay.AssertText("Current count: 0");
    page.IncrementButton.Click();
    page.CountDisplay.AssertText("Current count: 1");
}

// Async (new, using extension methods — no casts)
[Fact]
public async Task Button_Click_IncrementsCounter_Async()
{
    await NavigateToPageAsync("/counter");
    var page = new CounterPage(Context);
    await page.CountDisplay.AssertTextAsync("Current count: 0");
    await page.IncrementButton.ClickAsync();
    await page.CountDisplay.AssertTextAsync("Current count: 1");
}
```

### `PollAsync` implementation

```csharp
// In ObjectBase — async counterpart of Poll()
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
            // Condition threw — treat as false, retry
        }

        await Task.Delay(PollingIntervalMs).ConfigureAwait(false);
    }

    // Final attempt
    try { return await condition().ConfigureAwait(false); }
    catch { return false; }
}
```

### Playwright Timeout Strategy

| Async Wait Method | Strategy | Inner Playwright API |
|---|---|---|
| `WaitExistsAsync(true)` | Playwright native | `WaitForAsync(Attached, timeout)` |
| `WaitExistsAsync(false)` | Playwright native | `WaitForAsync(Detached, timeout)` |
| `WaitVisibleAsync(true)` | Playwright native | `WaitForAsync(Visible, timeout)` |
| `WaitVisibleAsync(false)` | Playwright native | `WaitForAsync(Hidden, timeout)` |
| `WaitEnabledAsync` | Framework `PollAsync` | `IsEnabledAsync(timeout: 1000)` (short inner timeout) |
| `WaitTextAsync` | Framework `PollAsync` | `InnerTextAsync(timeout: 1000)` (short inner timeout) |
| `WaitCheckedAsync` | Framework `PollAsync` | `IsCheckedAsync(timeout: 1000)` (short inner timeout) |

**Why the hybrid:** Playwright's `WaitForAsync` is event-driven (MutationObserver), more efficient than polling for existence/visibility. But Playwright has no `WaitForEnabled` or `WaitForText` state, so the framework must poll those with short inner timeouts to prevent the 30s-per-iteration conflict.

**Safety net:** Call `page.SetDefaultTimeout(timeouts.DefaultWait)` during `PlaywrightTestContext.CreateAsync()` to align Playwright's default with the framework's expectation.

### Pre-migration Fixes

Two `Thread.Sleep(100)` calls violate the codebase anti-pattern rules and should be fixed regardless:

1. `PlaywrightTestContext.FindElement()` (~line 200) — replace with Playwright's `WaitForAsync(Attached, timeout)`
2. `PlaywrightTestContext.WaitReady()` (~line 162) — replace with `WaitHelper.Pause(100)` (sync fix) or fully rewrite as async

## Technical Scenarios

### Scenario 1: Async Interface Hierarchy

**Requirements:**

* Mirror the per-capability structure from Core (`IControlObject` → `IClickableControlObject` → `IToggleControlObject`)
* Live in `Brinell.Html/Interfaces/Async/` — separate folder from sync interfaces
* Use `Task<T>` return types, same method names as sync

**Preferred Approach:** One async interface per sync capability

```text
New files in srcnew/Brinell.Html/Interfaces/Async/:
  IHtmlAsyncControlObject.cs       (mirrors IControlObject methods)
  IHtmlAsyncClickable.cs            (Click, DoubleClick, RightClick, Hover)
  IHtmlAsyncFocusable.cs            (Focus, Blur, HasFocus)
  IHtmlAsyncToggle.cs               (IsChecked, SetChecked, Toggle, Check, Uncheck)
  IHtmlAsyncEditable.cs             (SetText, GetValue, TypeText, Clear)
  IHtmlAsyncSelector.cs             (SelectByValue, SelectByText, GetSelectedValue)
  IHtmlAsyncRange.cs                (GetMin, GetMax, GetStep, GetValue, SetValue)
  IHtmlAsyncScrollable.cs           (ScrollTo, ScrollToTop)
  IAsyncHtmlElement.cs              (async counterpart of IHtmlElement + IElement)

New files in srcnew/Brinell.Html/:
  HtmlAsyncExtensions.cs            (extension methods: ClickAsync, EnterAsync, etc.)
```

**Inheritance chain:**

```text
IHtmlAsyncControlObject<TScope>
├── IHtmlAsyncClickable<TScope>
│   ├── IHtmlAsyncFocusable<TScope>
│   │   ├── IHtmlAsyncEditable<TScope>
│   │   ├── IHtmlAsyncSelector<TScope>
│   │   └── IHtmlAsyncRange<TScope>
│   ├── IHtmlAsyncToggle<TScope>
│   └── IHtmlAsyncScrollable<TScope>
```

#### Considered Alternatives

**Flat interface** — single `IHtmlAsyncControlObject<TScope>` with all methods. Simpler but doesn't model capability composition correctly. A `ButtonControl` shouldn't expose `IsChecked`. Rejected.

### Scenario 2: PlaywrightHtmlElement Dual Implementation

**Requirements:**

* Implement `IHtmlElement` (sync, existing) — `.GetAwaiter().GetResult()` backed
* Implement `IAsyncHtmlElement` (async, new) — proper `await`
* No code duplication between sync and async paths

**Preferred Approach:** Async as the implementation, sync wraps async

```csharp
public sealed class PlaywrightHtmlElement : IHtmlElement, IAsyncHtmlElement
{
    // --- Async (IAsyncHtmlElement) — primary implementation ---
    public async Task Click()
        => await _locator.ClickAsync().ConfigureAwait(false);

    public async Task<bool> IsVisible()
        => await _locator.IsVisibleAsync().ConfigureAwait(false);

    public async Task<string?> GetText()
        => await _locator.InnerTextAsync().ConfigureAwait(false);

    // --- Sync (IHtmlElement) — delegates to async ---  
    void IElement<IHtmlElement>.Click()
        => _locator.ClickAsync().GetAwaiter().GetResult();

    bool IElement<IHtmlElement>.Visible
        => _locator.IsVisibleAsync().GetAwaiter().GetResult();

    string? IElement<IHtmlElement>.Text
        => _locator.InnerTextAsync().GetAwaiter().GetResult();
}
```

Note: The sync path retains `.GetAwaiter().GetResult()` — this is the accepted cost of maintaining dual API. The async path is clean.

#### Considered Alternatives

**Sync as primary, async wraps via `Task.Run`** — defeats the purpose; async path would still block a thread. Rejected.

### Scenario 3: ControlBase Async Hub

**Requirements:**

* Add `RunWithElementAsync` as the async equivalent of `RunWithElement`
* Add `PollAsync` as the async equivalent of `Poll`
* All derived controls use `RunWithElementAsync` for their async implementations

**Preferred Approach:** Added directly to existing `ControlBase<TScope>`

```csharp
public abstract class ControlBase<TScope> : ObjectBase,
    IControlObject<TScope>,
    IHtmlAsyncControlObject<TScope>
    where TScope : IHtmlScope<TScope>
{
    // ... existing sync code unchanged ...

    // New: async element access
    protected IAsyncHtmlElement? TryFindAsyncElement()
        => TryFindElement() as IAsyncHtmlElement;

    protected IAsyncHtmlElement FindAsyncElement()
        => FindElement() as IAsyncHtmlElement
            ?? throw new InvalidOperationException("Element does not support async operations");

    // New: async RunWithElement
    protected async Task<TScope> RunWithElementAsync(Func<IAsyncHtmlElement, Task> action)
    {
        var element = FindAsyncElement();
        await action(element).ConfigureAwait(false);
        return ContainingScope;
    }

    protected async Task<TResult> RunWithElementAsync<TResult>(Func<IAsyncHtmlElement, Task<TResult>> action)
    {
        var element = FindAsyncElement();
        return await action(element).ConfigureAwait(false);
    }

    // Explicit async implementations
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
}
```

## Migration Order

| Step | Files | Changes | Depends On |
|------|-------|---------|------------|
| 0 | `PlaywrightTestContext.cs` | Fix 2x `Thread.Sleep(100)` | Nothing |
| 1 | `Brinell.Html/Interfaces/Async/*.cs` | Create 9 new async interfaces | Nothing |
| 2 | `Brinell.Html/ObjectBase.cs` | Add `PollAsync` | Nothing |
| 3 | `Brinell.Html/Controls/ControlBase.cs` | Add `RunWithElementAsync` + `IHtmlAsyncControlObject<TScope>` explicit impl | Steps 1, 2 |
| 4 | `Brinell.Html.Playwright/PlaywrightHtmlElement.cs` | Implement `IAsyncHtmlElement` | Step 1 |
| 5 | `Brinell.Html/Controls/Control.cs` | Add `IHtmlAsyncClickable<TScope>` explicit impl | Step 3 |
| 6 | `Brinell.Html/Controls/ClickableControlBase.cs` | Add async `DoubleClick`, `RightClick`, `Hover` | Step 5 |
| 7 | `Brinell.Html/Controls/FocusableControlBase.cs` | Add async `Focus`, `Blur`, `HasFocus` | Step 6 |
| 8 | `Brinell.Html/Controls/ToggleControlBase.cs` + children | Add async toggle methods | Step 6 |
| 9 | `Brinell.Html/Controls/Text/*.cs` | Add async text methods | Step 7 |
| 10 | `Brinell.Html/Controls/Selection/*.cs` | Add async selector methods | Step 7 |
| 11 | `Brinell.Html/Controls/Range/*.cs`, `DateTime/*.cs` | Add async range/date methods | Step 7 |
| 12 | `Brinell.Html/Controls/Display/*.cs`, `Collection/*.cs`, `Container/*.cs` | Add async display/collection/container methods | Step 3 |
| 13 | `Brinell.Html/HtmlAsyncExtensions.cs` | Create extension method bridge | Steps 1-12 |
| 14 | `Brinell.Html/Pages/HtmlPageObjectBase.cs` | Add async page methods | Step 3 |
| 15 | `Brinell.Html.Playwright/PlaywrightTestContext.cs` | Add async navigation, screenshot, element-finding | Step 4 |
| 16 | `testsnew/Brinell.Html.UITests/TestBase/BlazorSampleTestBase.cs` | Add `NavigateToPageAsync` | Step 15 |
| 17 | `testsnew/Brinell.Html.UITests/Tests/**/*.cs` | Add async test methods | Steps 13, 16 |

**Total new files:** ~10 (9 interfaces + 1 extension class)
**Total modified files:** ~25 (22 control classes + ObjectBase + PlaywrightHtmlElement + PlaywrightTestContext + test files)
**Files unchanged:** `Brinell.Core/*`, `Brinell.Wpf/*`, `Brinell.WinForms/*`, `Brinell.Maui/*`, `Brinell.Stride/*`, `LocatorExtensions.cs`, page objects, `HtmlTestContextOptions.cs`
