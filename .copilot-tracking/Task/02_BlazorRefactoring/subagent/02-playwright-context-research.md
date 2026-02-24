# Research: Playwright Adapter Layer & Old Blazor Context

**Date:** 2026-02-23  
**Scope:** `srcnew/Brinell.Html.Playwright/`, `srcnew/Brinell.Html/`, `src/Brinell.Blazor/ControlObject6/`

---

## 1. PlaywrightHtmlElement — IHtmlElement Implementation

**File:** `srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs`  
**Class:** `sealed class PlaywrightHtmlElement : IHtmlElement`

### Architecture

- Wraps a single `ILocator` (Playwright)
- **Sync-over-async pattern:** Every Playwright async call uses `.GetAwaiter().GetResult()` to present a synchronous API
- All child finding returns new `PlaywrightHtmlElement` instances wrapping child locators

### Properties (from IElement<IHtmlElement>)

| Line | Signature | Playwright API Used |
|------|-----------|-------------------|
| L20 | `bool Visible` | `_locator.IsVisibleAsync()` |
| L22 | `bool Enabled` | `_locator.IsEnabledAsync()` |
| L24 | `bool Selected => IsChecked` | delegates to `IsChecked` |
| L26 | `string? Text` | `_locator.InnerTextAsync()` |
| L28 | `string? TagName` | `EvaluateAsync("el => el.tagName.toLowerCase()")` |
| L30 | `Point Location` | via `EvaluateRect` → `BoundingBoxAsync()` |
| L32 | `Size Size` | via `EvaluateRect` → `BoundingBoxAsync()` |
| L34 | `Rectangle Rect` | via `EvaluateRect` → `BoundingBoxAsync()` |

### HTML-Specific Properties (from IHtmlElement)

| Line | Signature | Playwright API Used |
|------|-----------|-------------------|
| L153 | `string InnerHtml` | `_locator.InnerHTMLAsync()` |
| L155 | `string OuterHtml` | `EvaluateAsync("el => el.outerHTML")` |
| L157 | `bool IsChecked` | `_locator.IsCheckedAsync()` |
| L159 | `string InputValue` | `_locator.InputValueAsync()` |

### Action Methods

| Line | Signature | Playwright API Used |
|------|-----------|-------------------|
| L36 | `void Click()` | `_locator.ClickAsync()` |
| L38-50 | `void SendKeys(string text, TextInputMethod method)` | `FillAsync` (SetValue/Paste) or `PressSequentiallyAsync` (Keys) |
| L52 | `void Clear()` | `_locator.ClearAsync()` |
| L54 | `void DoubleClick()` | `_locator.DblClickAsync()` |
| L56 | `void RightClick()` | `ClickAsync(Button=Right)` |
| L58 | `void Hover()` | `_locator.HoverAsync()` |
| L60 | `void LongPress(int durationMs)` | `ClickAsync(Delay=durationMs)` |
| L62-63 | `void ScrollIntoView(int timeoutMs)` | `ScrollIntoViewIfNeededAsync()` |
| L65-72 | `void Swipe(...)` | `page.Mouse.MoveAsync/DownAsync/UpAsync` |
| L74 | `string? GetAttribute(string name)` | `_locator.GetAttributeAsync()` |
| L161 | `void Fill(string value)` | `_locator.FillAsync()` |
| L163 | `void SelectOption(string value)` | `_locator.SelectOptionAsync()` |
| L165-169 | `void SelectOption(string[] values)` | `_locator.SelectOptionAsync(options)` |
| L171 | `void Check()` | `_locator.CheckAsync()` |
| L173 | `void Uncheck()` | `_locator.UncheckAsync()` |
| L175 | `void Focus()` | `_locator.FocusAsync()` |
| L177 | `void Blur()` | `_locator.BlurAsync()` |
| L151 | `void Submit()` | `EvaluateAsync("el.form.submit()")` |

### DOM Query Methods

| Line | Signature |
|------|-----------|
| L143 | `string? GetDomAttribute(string attributeName)` — delegates to `GetAttribute` |
| L146 | `string? GetDomProperty(string propertyName)` — `el[propertyName]` via JS eval |
| L149 | `string? GetCssValue(string propertyName)` — `getComputedStyle(el).getPropertyValue(...)` |

### Child Finding Methods

| Line | Signature | Notes |
|------|-----------|-------|
| L76-101 | `IHtmlElement FindElement(Locator locator, int timeoutMs)` | Uses `LocatorExtensions.ToPlaywrightLocator`, waits with `WaitForAsync(Attached)`, throws `ElementNotFoundException` |
| L103-128 | `IReadOnlyList<IHtmlElement> FindElements(Locator locator, int timeoutMs)` | Returns list of `PlaywrightHtmlElement` via `Nth(i)` |
| L130-141 | `bool TryFindElement(Locator locator, out IHtmlElement?, int timeoutMs)` | try/catch around `FindElement` |

### Key Pattern: Sync-over-Async

Every method uses: `playwrightAsyncMethod().GetAwaiter().GetResult()`

This is the central bridging strategy: the Brinell framework presents a **synchronous API** to test writers while Playwright is entirely async under the hood.

---

## 2. PlaywrightTestContext — IHtmlTestContext Implementation

**File:** `srcnew/Brinell.Html.Playwright/PlaywrightTestContext.cs`  
**Class:** `sealed class PlaywrightTestContext : IHtmlTestContext, IAsyncDisposable`

### Internal Fields

| Line | Field | Type |
|------|-------|------|
| L15 | `_page` | `IPage` |
| L16 | `_frame` | `IFrame?` |
| L17 | `_ownsLifecycle` | `bool` |
| L18 | `_timeouts` | `TimeoutSettings` |
| L19 | `_logger` | `ITestLogger` |
| L21 | `_playwright` | `IPlaywright?` |
| L22 | `_browser` | `IBrowser?` |
| L23 | `_browserContext` | `IBrowserContext?` |

### Factory Methods

| Line | Signature | Description |
|------|-----------|-------------|
| L56-89 | `static async Task<PlaywrightTestContext> CreateAsync(HtmlTestContextOptions)` | Full lifecycle: creates Playwright → browser → context → page; navigates to BaseUrl; `ownsLifecycle=true` |
| L91-101 | `static PlaywrightTestContext ForPage(IPage, HtmlTestContextOptions?)` | Wraps existing page; `ownsLifecycle=false` |
| L103-121 | `PlaywrightTestContext ForFrame(string urlPattern)` | Creates sub-context scoped to an iframe |

### Properties (IHtmlTestContext)

| Line | Signature | Source |
|------|-----------|--------|
| L123 | `IHtmlTestContext Context => this` | Self-reference |
| L125 | `TimeoutSettings Timeouts` | From constructor |
| L127 | `ITestLogger Logger` | From constructor |
| L129 | `LocatorStrategy DefaultLocatorStrategy => LocatorStrategy.Css` | Hardcoded |
| L131 | `IPageObject? Page => null` | Not used at context level |
| L133 | `string CurrentUrl` | `_page.Url` |
| L135 | `string PageTitle` | `_page.TitleAsync().GetAwaiter().GetResult()` |

### Element Finding (IElementScope<IHtmlElement>)

| Line | Signature | Notes |
|------|-----------|-------|
| L155-168 | `IHtmlElement? TryFindElement(Locator)` | Uses `LocatorExtensions.ToPlaywrightLocator(this, locator)`, returns null on failure |
| L170-185 | `IHtmlElement FindElement(Locator)` | Polls with `Thread.Sleep(100)` until deadline, throws `ElementNotFoundException` |
| L187-206 | `IReadOnlyList<IHtmlElement> FindElements(Locator)` | Via `LocatorExtensions`, returns `PlaywrightHtmlElement` per `Nth(i)` |

### Navigation & Actions (ITestContext)

| Line | Signature |
|------|-----------|
| L208-212 | `void NavigateTo(string destination)` |
| L214-217 | `void NavigateBack()` |
| L219-222 | `void GoForward()` |
| L224-227 | `void Refresh()` |
| L229-232 | `byte[] TakeScreenshot()` |
| L234-238 | `void SaveScreenshot(string path)` |
| L240-243 | `void ResetAppState()` — clears cookies |

### Lifecycle Management

| Line | Signature | Notes |
|------|-----------|-------|
| L245-249 | `void Dispose()` | Sync dispose via `DisposeAsyncCore(false)` |
| L251-255 | `ValueTask DisposeAsync()` | Async dispose |
| L257-end | `DisposeAsyncCore(bool isAsync)` | Closes browserContext → browser → playwright; only if `_ownsLifecycle` |

---

## 3. LocatorExtensions — Locator Resolution

**File:** `srcnew/Brinell.Html.Playwright/LocatorExtensions.cs`  
**Class:** `internal static class LocatorExtensions`

### Methods

| Line | Signature | Description |
|------|-----------|-------------|
| L8-19 | `static ILocator ToPlaywrightLocator(PlaywrightTestContext, Locator)` | Context-level: resolves from page/frame root, handles parent chain |
| L21-47 | `static ILocator ToPlaywrightLocator(ILocator parent, Locator)` | Element-level: resolves relative to parent locator |
| L49-52 | `private static ILocator Scope(IPage, IFrame?, string)` | Returns `frame.Locator(*)` or `page.Locator(*)` |
| L54 | `private static string EscapeForAttribute(string)` | Escapes `'` |
| L56 | `private static string EscapeForSelectorText(string)` | Escapes `'` |
| L58-61 | `private static string EscapeCss(string)` | Escapes `\`, `.`, `#`, `:` |

### Strategy Mapping

| LocatorStrategy | Playwright Selector |
|----------------|-------------------|
| `Css` | `baseLocator.Locator(value)` |
| `Id` | `#EscapeCss(value)` |
| `DataTestId` | `[data-testid='value']` |
| `DataAutomationId` | `[data-automation-id='value']` |
| `AutomationId` | `[data-automation-id='value'], [id='value']` (dual) |
| `Name` | `[name='value']` |
| `ClassName` | `.EscapeCss(value)` |
| `TagName` | `value` (direct) |
| `XPath` | `xpath=value` |
| `Text` | `GetByText(value)` |
| `LinkText` | `a:has-text('value')` |
| `PartialLinkText` | `a:has-text('value')` |
| `AccessibilityId` | `GetByRole(Generic, Name=value)` |
| `ControlType` | `value` (direct) |

### Key Pattern: Recursive Parent Resolution

Both overloads handle `locator.Parent != null` by recursively resolving the parent first, then scoping the child locator within it. This allows chained locator hierarchies.

---

## 4. Old BlazorTestContext

**File:** `src/Brinell.Blazor/ControlObject6/Context/BlazorTestContext.cs`  
**Class:** `class BlazorTestContext : IAsyncTestContext`

### Properties

| Line | Signature |
|------|-----------|
| L24 | `int DefaultTimeoutMs { get; set; } = 30000` |
| L27 | `int DefaultPollingIntervalMs { get; set; } = 100` |
| L30 | `IAsyncPageObject? CurrentPage { get; private set; }` |
| L35 | `IPage Page => _page` (exposes raw Playwright page) |

### Methods

| Line | Signature | Notes |
|------|-----------|-------|
| L41 | `BlazorTestContext(IPage page)` | Constructor, stores page |
| L48-55 | `async Task NavigateToAsync(string? route, int? timeoutMs, CancellationToken)` | `_page.GotoAsync(route)` |
| L58-65 | `async Task<TPage> NavigateToAsync<TPage>(int?, CancellationToken)` | Creates page via Activator, sets CurrentPage, `WaitLoadedAsync` |
| L68-76 | `async Task TakeScreenshotAsync(string?, CancellationToken)` | Saves to current dir |
| L79-83 | `void Log(string?)` | `Console.WriteLine` |
| L86-90 | `void LogError(string?)` | `Console.Error.WriteLine` |
| L95-98 | `TPage CreatePage<TPage>()` | `Activator.CreateInstance(typeof(TPage), this)` |

### Key Differences from New Architecture

| Aspect | Old BlazorTestContext | New PlaywrightTestContext |
|--------|----------------------|--------------------------|
| API Surface | Async (`IAsyncTestContext`) | Sync (`IHtmlTestContext`) |
| Element Finding | Not provided (controls find their own elements) | `FindElement/FindElements/TryFindElement` on context |
| Page Management | `CurrentPage` tracked | `Page` property (currently null) |
| Browser Lifecycle | Not managed (receives `IPage`) | Full lifecycle via `CreateAsync` or external via `ForPage` |
| Locator System | `ControlLocator` (string-based testId) | `Locator` (strategy-based with parent chaining) |
| Logging | Direct `Console.WriteLine` | `ITestLogger` abstraction |
| Timeouts | Two flat ints | `TimeoutSettings` object |

---

## 5. Old AsyncPageObjectBase

**File:** `src/Brinell.Blazor/ControlObject6/Pages/AsyncPageObjectBase.cs`  
**Class:** `abstract class AsyncPageObjectBase : IAsyncPageObject`

### Properties

| Line | Signature |
|------|-----------|
| L22 | `abstract string Name { get; }` |
| L27 | `protected BlazorTestContext Context` |
| L33 | `protected abstract ControlLocator PageLocator { get; }` |

### Page State Methods

| Line | Signature | Notes |
|------|-----------|-------|
| L42-46 | `virtual async Task<bool> IsLoadedAsync(int?, CancellationToken)` | Creates `ControlObjectPlaceholder` with `PageLocator`, checks `IsVisibleAsync` |
| L49-61 | `async Task<bool> WaitLoadedAsync(bool?, int?, CancellationToken)` | Manual polling loop with `Task.Delay` |
| L64-74 | `async Task AssertLoadedAsync(bool?, string?, int?, CancellationToken)` | Throws `AssertionException` |

### Title Methods

| Line | Signature |
|------|-----------|
| L79-82 | `virtual async Task<string> GetTitleAsync(int?, CancellationToken)` |
| L85-94 | `async Task AssertTitleAsync(string?, string?, int?, CancellationToken)` |

### Control Access Helpers

| Line | Signature | Notes |
|------|-----------|-------|
| L100-101 | `protected ButtonControl Button(string testId)` | `new(_context, testId, this)` |
| L104-105 | `protected ButtonControl Button(ControlLocator locator)` | `new(_context, locator, this)` |
| L108-109 | `protected InputControl Input(string testId)` | `new(_context, testId, this)` |
| L112-113 | `protected InputControl Input(ControlLocator locator)` | `new(_context, locator, this)` |

### Control Query Methods

| Line | Signature |
|------|-----------|
| L119-122 | `async Task<bool> ControlExistsAsync(ControlLocator, int?, CancellationToken)` |
| L127-132 | `async Task<bool> WaitControlExistsAsync(ControlLocator, bool?, int?, CancellationToken)` |
| L137-142 | `async Task AssertControlExistsAsync(ControlLocator, bool?, string?, int?, CancellationToken)` |

### Screenshot/Scroll

| Line | Signature |
|------|-----------|
| L148-151 | `async Task TakeScreenshotAsync(string?, int?, CancellationToken)` |
| L154-161 | `async Task ScrollToControlAsync(ControlLocator?, int?, CancellationToken)` — uses `GetByTestId` |

### Inner Class

| Line | Description |
|------|-------------|
| L167-172 | `private class ControlObjectPlaceholder : AsyncClickableControlBase` — used for page state checks (IsLoaded) |

### Key Differences from New HtmlPageObjectBase

| Aspect | Old AsyncPageObjectBase | New HtmlPageObjectBase<TSelf> |
|--------|------------------------|-------------------------------|
| API | Fully async | Fully sync |
| Context Type | `BlazorTestContext` | `IHtmlTestContext` |
| Self-Type | None | `TSelf` generic parameter |
| IsLoaded | Creates placeholder control | Override `IsLoaded()` returning bool |
| Polling | Manual `Task.Delay` loop | `Poll()` helper from `ObjectBase` |
| Element Finding | Delegated to controls | Delegates to `_context.FindElement/FindElements/TryFindElement` |
| Control Creation | `Button(testId)`, `Input(testId)` helpers | Not built-in (done in subclass) |
| PageLocator | `ControlLocator` (testId string) | N/A (override `IsLoaded`) |

---

## 6. Old Async Interfaces

### IAsyncControlObject

**File:** `src/Brinell.Blazor/ControlObject6/Interfaces/IAsyncControlObject.cs`

**Contains 5 interfaces in one file:**

#### IAsyncControlObject (L8)
- `ControlLocator Locator { get; }`
- `IAsyncPageObject? Page { get; }`
- **Existence:** `IsExistsAsync`, `WaitExistsAsync`, `CheckExistsAsync`, `AssertExistsAsync`
- **Visibility:** `IsVisibleAsync`, `WaitVisibleAsync`, `CheckVisibleAsync`, `AssertVisibleAsync`
- **Text:** `GetTextAsync`, `AssertTextAsync`, `AssertTextContainsAsync`, `AssertTextStartsWithAsync`, `AssertTextEndsWithAsync`, `AssertTextMatchesAsync`, `AssertTextEmptyAsync`

#### IAsyncInteractiveControlObject : IAsyncControlObject (L49)
- **Enabled:** `IsEnabledAsync`, `WaitEnabledAsync`, `CheckEnabledAsync`, `AssertEnabledAsync`

#### IAsyncFocusableControlObject : IAsyncInteractiveControlObject (L60)
- **Focus:** `IsFocusedAsync`, `WaitFocusedAsync`, `CheckFocusedAsync`, `AssertFocusedAsync`
- `FocusAsync`, `BlurAsync`

#### IAsyncClickableControlObject : IAsyncInteractiveControlObject (L73)
- `ClickAsync`, `DoubleClickAsync`, `RightClickAsync`, `HoverAsync`

#### IAsyncTextControlObject : IAsyncFocusableControlObject (L83)
- `EnterAsync`, `ClearAsync`, `ClearAndEnterAsync`, `AppendAsync`
- **ReadOnly:** `IsReadOnlyAsync`, `WaitReadOnlyAsync`, `AssertReadOnlyAsync`
- `GetTextLengthAsync`, `AssertTextLengthAsync`

### IAsyncPageObject

**File:** `src/Brinell.Blazor/ControlObject6/Interfaces/IAsyncPageObject.cs`

- `string Name { get; }`
- **Loaded:** `IsLoadedAsync`, `WaitLoadedAsync`, `AssertLoadedAsync`
- **Title:** `GetTitleAsync`, `AssertTitleAsync`
- **Control Queries:** `ControlExistsAsync`, `WaitControlExistsAsync`, `AssertControlExistsAsync`
- **Actions:** `TakeScreenshotAsync`, `ScrollToControlAsync`

### IAsyncTestContext

**File:** `src/Brinell.Blazor/ControlObject6/Interfaces/IAsyncTestContext.cs`

- `int DefaultTimeoutMs { get; set; }`
- `int DefaultPollingIntervalMs { get; set; }`
- `IAsyncPageObject? CurrentPage { get; }`
- `NavigateToAsync(string? route, ...)`, `NavigateToAsync<TPage>(...)`
- `TakeScreenshotAsync(string?, ...)`, `Log(string?)`, `LogError(string?)`

---

## 7. New Architecture Reference Types

### IHtmlElement (srcnew/Brinell.Html/Interfaces/IHtmlElement.cs)

Extends `IElement<IHtmlElement>` (from Core) with HTML-specific members:
- `GetDomAttribute`, `GetDomProperty`, `GetCssValue`, `Submit`
- `InnerHtml`, `OuterHtml`, `IsChecked`, `InputValue`
- `Fill`, `SelectOption` (single + multi), `Check`, `Uncheck`, `Focus`, `Blur`

### IHtmlTestContext (srcnew/Brinell.Html/Interfaces/IHtmlTestContext.cs)

Extends `ITestContext<IHtmlElement>` + `IHtmlElementScope`:
- `IHtmlTestContext Context { get; }` (self-ref)
- `string CurrentUrl { get; }`
- `string PageTitle { get; }`
- `void GoForward()`
- **Inherited:** `Timeouts`, `Logger`, `NavigateTo`, `NavigateBack`, `Refresh`, `TakeScreenshot`, `SaveScreenshot`, `ResetAppState`
- **Inherited (IElementScope):** `TryFindElement`, `FindElement`, `FindElements`

### HtmlTestContextOptions (srcnew/Brinell.Html/Context/HtmlTestContextOptions.cs)

- `string? BaseUrl`
- `bool Headless = true`
- `string BrowserType = "chromium"`
- `TimeoutSettings Timeouts = Default`
- `ITestLogger? Logger`
- `bool EnableTracing`
- `string? CdpEndpoint`

### HtmlPageObjectBase<TSelf> (srcnew/Brinell.Html/Pages/HtmlPageObjectBase.cs)

- Extends `ObjectBase`, implements `IHtmlPage<TSelf>`
- **Key members:** `Context`, `Self`, `Name`, `DefaultLocatorStrategy`
- **IsLoaded pattern:** virtual `IsLoaded()`, `WaitLoaded()`, `AssertLoaded()` — all sync, uses `Poll()` helper
- **Title:** `GetTitle()`, `WaitTitle()`, `AssertTitle()`
- **Screenshots:** `TakeScreenshot()`
- **Element delegation:** `TryFindElement`, `FindElement`, `FindElements` → delegates to `_context`

### HtmlTestFixtureBase (srcnew/Brinell.Html/Testing/HtmlTestFixtureBase.cs)

- Abstract base for test fixtures
- `CreateOptions()` → `HtmlTestContextOptions`
- `CreateContextAsync(options)` → `Task<IHtmlTestContext>` (abstract)
- `InitializeAsync()` / `DisposeAsync()` lifecycle
- `NavigateTo(path)` helper

### ObjectBase (srcnew/Brinell.Html/ObjectBase.cs)

- `abstract IHtmlTestContext Context { get; }`
- `int DefaultTimeoutMs` → from `Context.Timeouts.DefaultWait`
- `int PollingIntervalMs` → from `Context.Timeouts.PollingInterval`
- `bool Poll(Func<bool> condition, int timeoutMs)` — Stopwatch-based polling loop with `WaitHelper.Pause`

---

## 8. Key Mapping Patterns: Old Blazor → New Architecture

### Pattern 1: Async → Sync Bridge

| Old (Blazor) | New (Html) |
|-------------|-----------|
| `async Task<bool> IsExistsAsync()` | `bool IsExists()` |
| `async Task<bool> WaitExistsAsync(bool?, int?)` | `bool WaitExists(bool?, int?)` |
| `async Task AssertExistsAsync(bool?, string?, int?)` | `void AssertExists(bool?, string?, int?)` |
| `async Task ClickAsync(int?)` | `void Click()` |
| Playwright async: `locator.IsVisibleAsync()` | `locator.IsVisibleAsync().GetAwaiter().GetResult()` |

**The new architecture uses sync-over-async (`.GetAwaiter().GetResult()`) at the element/context level, keeping the test API synchronous.**

### Pattern 2: Context Responsibility Shift

| Old | New |
|-----|-----|
| Context exposes `IPage Page` directly | Context hides Playwright, exposes `IHtmlElement`-based finding |
| Controls access Playwright directly | Controls work through `IHtmlElement` abstraction |
| `ControlLocator` = string testId | `Locator` = strategy + value + optional parent chain |
| No element abstraction | `IHtmlElement` wraps all Playwright interactions |

### Pattern 3: Page Object Evolution

| Old | New |
|-----|-----|
| `AsyncPageObjectBase` with `BlazorTestContext` | `HtmlPageObjectBase<TSelf>` with `IHtmlTestContext` |
| `abstract ControlLocator PageLocator` → creates placeholder | `virtual bool IsLoaded()` → override directly |
| `Button(testId)`, `Input(testId)` helper methods | Controls created by subclasses using new architecture |
| Manual `Task.Delay` polling | `ObjectBase.Poll()` with `WaitHelper.Pause` |

### Pattern 4: Test Fixture Evolution

| Old | New |
|-----|-----|
| Test creates `IPage` externally, passes to `BlazorTestContext(page)` | `HtmlTestFixtureBase.CreateContextAsync()` → `PlaywrightTestContext.CreateAsync()` manages full lifecycle |
| OR: use `PlaywrightTestContext.ForPage(page)` for external page management |

### Pattern 5: Locator Strategy

| Old | New |
|-----|-----|
| `ControlLocator` = flat string (testId) | `Locator` class with `Strategy`, `Value`, `Parent` |
| `_context.Page.GetByTestId(locator.Value)` | `LocatorExtensions.ToPlaywrightLocator(context, locator)` with strategy switch |
| Single strategy (testId) | 14+ strategies (Css, Id, DataTestId, XPath, Text, etc.) |

---

## 9. What Blazor Refactoring Needs

Based on this research, a new `Brinell.Blazor` package in `srcnew/` would need:

1. **No custom element type** — use `IHtmlElement` / `PlaywrightHtmlElement` directly
2. **No custom test context** — use `PlaywrightTestContext` (possibly with Blazor-specific factory helpers)
3. **Blazor page objects** — extend `HtmlPageObjectBase<TSelf>` instead of `AsyncPageObjectBase`
4. **Blazor controls** — use existing Html control base classes, not custom async controls
5. **Blazor test fixture** — extend `HtmlTestFixtureBase`, override `CreateContextAsync` to create Playwright context for Blazor apps
6. **Blazor-specific concerns only:** Server-side rendering awareness, SignalR connection waiting, component hydration detection — things Html layer doesn't handle natively
