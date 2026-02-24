<!-- markdownlint-disable-file -->

# Questions: Full Async Migration

## Round 1

### 🎯 Research Scope

**What is the primary goal of this migration?**

- [X] Replace all synchronous blocking calls (`.GetAwaiter().GetResult()`) with proper `async`/`await` throughout the entire framework *(currently `PlaywrightHtmlElement` and others block on async Playwright APIs — this is the core problem to solve)*
- [ ] Improve test execution performance by enabling parallel async test runs
- [ ] Both of the above
- [ ] Other:

**What does a successful migration look like?**

- [X] All control/driver interfaces return `Task<T>` or `Task`; all implementations use `async`/`await`; all test methods are `async Task`; no `.GetAwaiter().GetResult()` anywhere *(the complete end state)*
- [ ] Only the Playwright/HTML stack is migrated first as a pilot, other platforms follow later
- [ ] A recommended design with implementation plan only (no code changes yet)
- [ ] Other:

---

## Round 2

### ✅ Scope Confirmation (from conversation)

**Confirmed approach: pragmatic async migration**

- [X] Migrate `IElement<TSelf>` and all core interfaces to async (`Task<T>` / `Task`) *(interfaces are the contract; making them async drives all platforms uniformly)*
- [X] Migrate the HTML / Playwright stack fully — replace all `.GetAwaiter().GetResult()` with proper `async`/`await` *(Playwright is async-native; this is the primary driver)*
- [ ] Desktop platforms (WPF/FlaUI, WinForms/FlaUI) implement async interfaces via `Task.FromResult(...)` wrappers *(COM-synchronous; wrapping is safe and avoids boilerplate overhead)*
- [ ] MAUI / Appium implements async interfaces via `Task.FromResult(...)` wrappers where the underlying call is already synchronous *(maintains interface consistency)*
- [ ] Other:

**Confirmed: no `Async` suffix on method names**

- [ ] `Click()` not `ClickAsync()`, `GetText()` not `GetTextAsync()`, etc. *(user stated this requirement explicitly)*
- [X] Exception: factory/lifecycle methods retain suffix (`CreateAsync`, `DisposeAsync`, `InitializeAsync`)
- [ ] Other:

**Confirmed: async properties become methods**

- [X] `bool Visible { get; }` → `Task<bool> GetVisible()` (or `IsVisible()` following existing naming) *(C# cannot have async properties; Playwright requires async to query state)*
- [ ] Other:

**Confirmed: `Task.Delay` in bounded polling loops is acceptable**

- [X] `Wait*` methods use `await Task.Delay(pollIntervalMs)` between condition checks — this is controlled polling, not an arbitrary sleep *(distinction from anti-pattern: waiting FOR a condition, not waiting for time)*
- [ ] Other:

---

### 📋 Scope Boundaries

**Which platforms should be migrated?**

- [X] HTML / Playwright — current `PlaywrightHtmlElement` has 30+ `.GetAwaiter().GetResult()` calls *(highest priority — Playwright is natively async)*
- [ ] MAUI / Appium — Appium Java client uses sync calls over HTTP; async wrapping is optional
- [ ] WPF / FlaUI — FlaUI is COM-based and fully synchronous; async would be fire-and-forget wrappers only
- [ ] WinForms / FlaUI — same as WPF
- [ ] Stride — game engine integration; needs separate assessment
- [ ] All platforms — consistent interfaces require uniform async signatures across the board *(interfaces are shared; partial async breaks the contract)*
- [ ] Other:

**What should the migration explicitly skip?**

- [ ] The `Async` suffix on method names — user confirmed: `Click()` not `ClickAsync()` *(user requirement stated in the conversation)*
- [ ] Factory/construction methods like `PlaywrightTestContext.CreateAsync()` — these may keep the `Async` suffix since they are lifecycle methods, not UI interaction methods
- [ ] Retry/polling interval logic — `Task.Delay` in bounded polling loops is acceptable (not an arbitrary wait)
- [ ] Other:

**Should factory and lifecycle methods keep the `Async` suffix?**

- [X] Yes — `CreateAsync`, `DisposeAsync`, `InitializeAsync` are lifecycle conventions, not UI interaction patterns; they should keep the suffix *(aligns with .NET conventions: `IAsyncDisposable.DisposeAsync`, `IAsyncLifetime.InitializeAsync`)*
- [ ] No — strip `Async` from all methods uniformly
- [ ] Other:

---

### 🔍 Technical Context

**How should fluent method chaining work after the migration?**

Currently: `page.Button.Click().OtherButton.Click()` (sync chain, returns `TScope`)
After async: `Click()` returns `Task<TScope>` — chaining changes fundamentally.

- [ ] `await` each step individually: `await page.Button.Click(); await page.OtherButton.Click();`
- [X] Keep the fluent chain by `await`-ing at the call site: `(await page.Button.Click()).OtherButton` *(straightforward; tests become readable `async Task` methods)*
- [ ] Introduce a custom `AsyncFluentChain<T>` wrapper that enables `page.Button.Click().Then(s => s.Other.Click())` *(more complex but preserves chain syntax)*
- [ ] Drop fluent chaining entirely — void-returning actions, properties for state *(simplest; breaks current API contract)*
- [ ] Other:

**How should query/state methods be treated?**

Currently: `bool IsExists()`, `bool? IsVisible()` — these go via `TryFindElement()` which calls Playwright's async locator.

- [X] All query methods also become async: `Task<bool> IsExists()`, `Task<bool?> IsVisible()` *(consistent; Playwright `IsVisibleAsync` is inherently async)*
- [ ] Query methods stay synchronous for simpler test assertions (`Assert.True(page.Button.IsExists())`)
- [ ] Split: sync for desktop (FlaUI/WPF) platforms, async for web (Playwright) platforms
- [ ] Other:

**How should `Wait*` polling methods work (e.g., `WaitExists`, `WaitVisible`)?**

Currently they use a synchronous spin-loop. Async polling uses `Task.Delay` between condition checks.

- [X] `Task.Delay` in a bounded polling interval is acceptable — it is NOT an arbitrary sleep; it is a controlled wait between condition evaluations *(correct pattern for async polling; aligns with test framework conventions)*
- [ ] Use `CancellationToken` + `PeriodicTimer` for async polling
- [ ] Keep polling synchronous even in an otherwise async framework
- [ ] Other:

**Should `CancellationToken` be added to method signatures?**

- [ ] Yes — add `CancellationToken cancellationToken = default` to all async methods
- [X] No — the framework uses `timeoutMs` for timeout control; `CancellationToken` adds complexity without current benefit *(existing `timeoutMs` pattern handles timeout; no known consumers need token-based cancellation)*
- [ ] Add only to long-running/waiting methods, not to quick actions like `Click`
- [ ] Other:

---

### 🔄 Migration Strategy

**What migration strategy should be used?**

- [X] Top-down: migrate interfaces first (`IElement`, `IControlObject`, etc.), then base classes, then platform implementations, then tests *(interfaces are the single source of truth; fixing them drives all downstream changes consistently)*
- [ ] Bottom-up: migrate platform implementations first, then work up to interfaces and tests
- [ ] Platform-by-platform: complete one full platform stack before starting the next
- [ ] Other:

**Should sync overloads be retained for backward compatibility?**

- [ ] Yes — keep sync wrappers (`bool IsExists()`) calling `IsExists().GetAwaiter().GetResult()` during transition
- [X] No — clean break; all consumers (tests) must be updated at the same time *(current codebase is self-contained; no external consumers identified; clean migration is safer)*
- [ ] Yes, but as an extension method layer on top of the async interfaces
- [ ] Other:

**How should the `IElement` interface be migrated?**

`IElement<TSelf>` is the lowest-level element abstraction — Playwright, FlaUI, and Appium implement it.

- [X] Convert all `IElement` methods to `async Task` / `Task<T>` — properties that require async calls (e.g., `bool Visible`) become methods: `Task<bool> GetVisible()` *(C# interfaces cannot have async properties; Playwright `IsVisibleAsync` means `Visible` must become a method)*
- [ ] Keep properties as sync (backed by `.GetAwaiter().GetResult()`) and only make action methods async
- [ ] Introduce an `IAsyncElement` parallel interface alongside the existing sync one
- [ ] Other:

---

### 🧪 Test Migration

**How should test methods be updated?**

Currently: `[Fact] public void Button_Click_IncrementsCounter()`
After: `[Fact] public async Task Button_Click_IncrementsCounter()`

- [X] All test methods become `async Task` — xUnit supports this natively *(no framework changes needed; xUnit runs async tests transparently)*
- [ ] Use a custom `AsyncFact` or test adapter
- [ ] Only tests that directly call async methods are changed; others remain sync
- [ ] Other:

**How should `NavigateTo` (currently synchronous) be handled?**

`BlazorSampleTestBase.NavigateToPage()` calls `Context.NavigateTo()` which blocks on Playwright's `GotoAsync`.

- [X] `NavigateTo` becomes `async Task NavigateTo()` — test base method becomes `async Task NavigateToPage()` *(aligns with the overall async migration; no reason to keep it sync)*
- [ ] Keep `NavigateTo` sync via `.GetAwaiter().GetResult()`
- [ ] Other:

---

### 💡 Assumptions

*Inferred from the codebase and conversation context. Correct any that are wrong.*

- [X] The `srcnew/` and `testsnew/` folders contain the active codebase under migration (not the root-level `src/`/`tests/` folders, which appear to be legacy) *(confirmed by current open file path `srcnew\Brinell.Html\Controls\Control.cs`)*
- [X] xUnit is the test framework for all test projects *(BlazorSampleTestBase extends `IAsyncLifetime`; xUnit supports async test methods natively)*
- [X] Playwright is the only natively async underlying library today; FlaUI and Appium are synchronous *(FlaUI is COM/UI Automation — sync; Appium Java client is HTTP but currently sync-wrapped)*
- [X] There are no external consumers of the framework NuGet packages yet — this is an internal codebase under development *(no published packages found; clean break is safe)*
- [X] The `<!-- markdownlint-disable-file -->` approach is used for tracking documents to disable formatting enforcement *(per mode template instruction)*

---

### ⚠️ Risks and Concerns

**Are there known risks or concerns the research should cover?**

- [ ] Deadlock risk when mixing `async`/`await` with `ConfigureAwait(false)` in test contexts
- [ ] xUnit `SynchronizationContext` behavior with async tests — potential for test parallelism issues
- [ ] Playwright's `IPage` and `ILocator` are already async; confirm no hidden sync-over-async patterns remain in `PlaywrightTestContext`
- [ ] FlaUI and Appium platform implementations — wrapping inherently sync APIs with `async Task` methods that `return Task.FromResult(...)` vs running on a dedicated thread
- [ ] The `Assert.*` methods in tests use return values from async calls — chained calls require careful `await` placement
- [ ] No known risks — migration is well-defined
- [ ] Other:

---

### 🔎 Suggestions

*Based on codebase analysis. Check items to carry into the research brief.*

**`PlaywrightHtmlElement` has 30+ `.GetAwaiter().GetResult()` blocking calls** — see [srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs](../../../../../srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs)

- [X] Convert each `.GetAwaiter().GetResult()` call to `await` and mark methods `async Task<T>` — this is a direct mechanical transformation *(primary driver of the whole migration)*
- [ ] Use `ConfigureAwait(false)` on all `await` calls in framework code (not test code) to avoid context switching
- [ ] Dismiss — not relevant to this research

**`IElement<TSelf>` properties like `bool Visible` must become methods because C# async properties are not supported** — see [srcnew/Brinell.Core/Interfaces/IElement.cs](../../../../../srcnew/Brinell.Core/Interfaces/IElement.cs)

- [X] Rename to `GetVisible()`, `GetEnabled()`, `GetSelected()`, `GetText()`, etc. returning `Task<bool>` / `Task<string?>` *(necessary API change; properties cannot be `async`)*
- [ ] Keep as sync properties backed by `.GetAwaiter().GetResult()` in implementations
- [ ] Dismiss — not relevant to this research

**Fluent chaining pattern in `ControlBase<TScope>` uses `RunWithElement(Action<IHtmlElement>)`** — see [srcnew/Brinell.Html/Controls/ControlBase.cs](../../../../../srcnew/Brinell.Html/Controls/ControlBase.cs)

- [X] Replace `RunWithElement(Action<IHtmlElement> action)` with `RunWithElementAsync(Func<IHtmlElement, Task> action)` returning `Task<TScope>` *(central helper; converting it cascades correctly through all derived controls)*
- [ ] Keep `RunWithElement` sync and add a parallel `RunWithElementAsync` during transition
- [ ] Dismiss — not relevant to this research

**Test base class already uses `IAsyncLifetime`** — see [testsnew/Brinell.Html.UITests/TestBase/BlazorSampleTestBase.cs](../../../../../testsnew/Brinell.Html.UITests/TestBase/BlazorSampleTestBase.cs)

- [X] Tests only need test method signatures changed to `async Task`; the lifecycle infrastructure is already async *(low-friction migration for tests)*
- [ ] Dismiss — not relevant to this research

---

## Round 3

### ⚡ Scope Revision (from conversation)

The previous rounds assumed async interfaces across all platforms with `Task.FromResult` wrappers for desktop. The user has revised this:

* **HTML / Blazor (Playwright):** expose **both sync and async**
* **All other platforms (WPF, WinForms, MAUI, Stride):** remain **sync only — no changes**

This changes the interface design fundamentally. Core interfaces (`IControlObject`, `IElement`) must NOT become async, or desktop platforms break.

---

### 🏗️ Interface Architecture

**Where should the async API live — at the core interface level or the HTML layer?**

- [ ] Core interfaces (`IControlObject<TScope>`, `IElement<TSelf>`) become async — desktop platforms implement them with `Task.FromResult` wrappers *(uniform contract; forces boilerplate on platforms that don't need it)*
- [X] Core interfaces stay sync — async methods are added only to HTML-specific interfaces (`IHtmlElement`, `IHtmlControlObject<TScope>`) and HTML base classes *(desktop platforms require zero changes; Playwright stack gets clean async path)* *(recommended: keeps desktop untouched)*
- [ ] Parallel async interface hierarchy: `IAsyncControlObject<TScope>` alongside existing `IControlObject<TScope>`; HTML controls implement both *(maximum separation; doubles the interface surface)*
- [ ] Other:

**How should both sync and async be exposed on HTML controls?**

- [X] HTML controls inherit sync from `ControlBase` (implementing `IControlObject<TScope>`) AND provide async overloads as additional methods — e.g., `Click()` returns `TScope` (sync), `ClickAsync()` ... wait — user said no `Async` suffix. See next question. *(layered approach; sync consumers unchanged)*
- [ ] HTML controls expose async-only; sync is provided via extension methods that call `GetAwaiter().GetResult()` internally
- [ ] Sync and async share one method name (`Click()`) by making the interface method return `Task<TScope>` — sync callers must `await` or `.GetAwaiter().GetResult()`
- [ ] Other:

**Given no `Async` suffix, how are sync and async overloads distinguished?**

Under the no-`Async`-suffix rule, `Click()` (sync) and `Click()` (async, returns `Task<TScope>`) would conflict — two methods with the same name and parameter list cannot coexist.

- [ ] Sync methods are on the base `IControlObject<TScope>` interface; async methods are on a derived `IHtmlAsyncControlObject<TScope>` interface — different interfaces, same name, no conflict *(callers use the interface that matches their intent)*
- [X] Sync methods keep current signatures; async versions go on a separate `IHtmlAsyncControlObject<TScope>` interface using the same method names — resolved via explicit interface implementation or casting *(clean separation; no naming collision)*
- [ ] Abandon no-`Async`-suffix for the dual-stack HTML layer only — sync is `Click()`, async is `ClickAsync()` *(pragmatic; breaks the general rule only where technically necessary)*
- [ ] The HTML layer goes async-only; remove the sync API from HTML controls; desktop platforms keep sync *(simplest final state — no dual API to maintain)*
- [ ] Other:

**Should `IHtmlElement` properties remain sync (backed by `.GetAwaiter().GetResult()`) or become async methods?**

- [X] Keep `bool Visible`, `bool Enabled`, `string? Text` etc. as sync properties on `IHtmlElement` (backed by `.GetAwaiter().GetResult()` in `PlaywrightHtmlElement`) to preserve desktop interface compatibility *(sync properties needed for the shared `IElement<TSelf>` contract)*
- [ ] Add async counterparts: `Task<bool> IsVisible()`, `Task<bool> IsEnabled()` etc. on `IHtmlElement` alongside the sync properties *(dual access; Playwright can then avoid the blocking call for test authors who opt into async)*
- [ ] Other:

---

### 🧪 Test Implications

**Which tests should be async?**

- [X] Only HTML / Blazor UI tests (`testsnew/Brinell.Html.UITests/`) become `async Task` *(scoped to the platforms being migrated)*
- [ ] All test projects across all platforms for consistency
- [ ] No test changes — only the framework-level sync wrappers change
- [ ] Other:

**How should HTML test authors choose between sync and async?**

- [ ] Async is the default; sync is discouraged but available
- [X] Both are first-class — test authors choose based on preference; no deprecation of either *(decision deferred; both patterns supported)*
- [ ] Sync is the default; async is opt-in via the async interface
- [ ] Other:

---

### 💡 Revised Assumptions

- [X] The `Brinell.Core` interfaces remain 100% synchronous — this is the anchor point that keeps desktop platforms untouched
- [X] Async capability is HTML-stack-specific, added in `Brinell.Html` and `Brinell.Html.Playwright`
- [X] `PlaywrightHtmlElement` will have both sync properties (`.GetAwaiter().GetResult()` backed) for `IHtmlElement` compliance AND async methods for the async HTML interface
- [X] The no-`Async`-suffix rule applies to the async HTML interface methods; sync/async are distinguished by the interface they live on, not the method name
