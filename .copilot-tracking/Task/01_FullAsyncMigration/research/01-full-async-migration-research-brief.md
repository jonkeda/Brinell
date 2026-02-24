<!-- markdownlint-disable-file -->
# Research Brief: HTML/Playwright Async Migration (Revised)

**Created:** 2026-02-24
**Revised:** 2026-02-24
**Status:** ✅ Approved (Round 3 revision)
**Questions Document:** .copilot-tracking/Task/01_FullAsyncMigration/questions/01-full-async-migration-questions.md

## Scope Summary

| Platform | Approach |
|---|---|
| HTML / Blazor (Playwright) | Sync + async — both APIs exposed |
| WPF / FlaUI | Sync only — no changes |
| WinForms / FlaUI | Sync only — no changes |
| MAUI / Appium | Sync only — no changes |
| Stride | Sync only — no changes |

**Core `Brinell.Core` interfaces remain 100% synchronous.** Async capability is added exclusively in the `Brinell.Html` and `Brinell.Html.Playwright` layers.

---

## Validated Research Questions

1. **How should the dual sync+async HTML interface be structured without naming conflicts?**
   * Context: The no-`Async`-suffix rule means sync `Click()` and async `Click()` (returning `Task<TScope>`) cannot coexist on the same type. Two methods with identical names and parameters are a compile error.
   * Proposed hypothesis: Introduce `IHtmlAsyncControlObject<TScope>` (and parallel async variants for each control interface) as a sibling to the existing sync `IControlObject<TScope>`. Both interfaces declare `Click()` — on the sync interface it returns `TScope`, on the async interface it returns `Task<TScope>`. HTML controls implement both via explicit interface implementation. Test authors cast to or consume the interface that matches their intent.

2. **Where in the inheritance chain should async methods live?**
   * Context: `ControlBase<TScope>` implements `IControlObject<TScope>` (sync). Adding async to HTML controls must not touch `Brinell.Core`.
   * Proposed hypothesis: Add an `AsyncControlBase<TScope>` (or mixin pattern) in `Brinell.Html` that adds `RunWithElementAsync` and implements `IHtmlAsyncControlObject<TScope>`. Concrete HTML controls inherit from this and have both sync and async `Click()` etc.

3. **How should `PlaywrightHtmlElement` handle the dual sync/async requirement?**
   * Context: Playwright is async-native. The sync properties on `IHtmlElement` (`bool Visible`, `bool Enabled`, `string? Text`) are backed by `.GetAwaiter().GetResult()` today. The async version would `await` properly.
   * Proposed hypothesis: `PlaywrightHtmlElement` keeps the sync property implementations (`.GetAwaiter().GetResult()` is acceptable here since it serves the sync interface contract). It additionally exposes async methods (`Task<bool> IsVisible()`, `Task<bool> IsEnabled()` etc.) on a new `IAsyncHtmlElement` interface, implemented with proper `await` + `ConfigureAwait(false)`.

4. **How should `Wait*` polling work in the async HTML stack?**
   * Context: `WaitExists`, `WaitVisible`, etc. currently spin-loop. Async equivalents must yield between polls.
   * Proposed hypothesis: Async `Wait*` methods use `await Task.Delay(pollIntervalMs).ConfigureAwait(false)` inside a deadline loop. `Task.Delay` in a bounded polling interval is controlled waiting (waiting FOR a condition) — not an arbitrary sleep. Sync `Wait*` methods remain unchanged.

5. **How should `NavigateTo` and driver-level methods be handled in the HTML layer?**
   * Context: `PlaywrightTestContext.NavigateTo()` blocks on `GotoAsync`. It is HTML-specific; core `IDriver` stays sync.
   * Proposed hypothesis: Keep sync `NavigateTo(string)` on the core `IDriver` interface (backed by `.GetAwaiter().GetResult()` in Playwright). Add an async overload on `IHtmlTestContext` or as an HTML-specific extension: `Task NavigateToAsync(string)`. HTML test authors can use either.

6. **What is the migration order?**
   * Context: Only the HTML stack changes; no cascade to desktop platforms.
   * Proposed hypothesis: (1) Define new async HTML interfaces (`IAsyncHtmlElement`, `IHtmlAsyncControlObject<TScope>` and siblings), (2) add async capability to `ControlBase` in `Brinell.Html`, (3) implement async on `PlaywrightHtmlElement`, (4) update HTML UI tests to use async test methods and async overloads.

---

## Agreed Scope

* **Include:**
  * New async interfaces in `Brinell.Html` — `IAsyncHtmlElement`, `IHtmlAsyncControlObject<TScope>` and per-capability variants (async clickable, async text, async editable, async toggle, etc.)
  * `Brinell.Html` base classes — add `RunWithElementAsync` helper and async method implementations
  * `Brinell.Html.Playwright` — `PlaywrightHtmlElement` implements async interfaces; `PlaywrightTestContext` gets async navigation
  * `testsnew/Brinell.Html.UITests/` — HTML/Blazor test methods become `async Task`

* **Exclude:**
  * `Brinell.Core` — zero changes; interfaces stay 100% sync
  * `Brinell.Wpf`, `Brinell.WinForms`, `Brinell.Maui`, `Brinell.Stride` — zero changes
  * `testsnew/Brinell.Wpf.UITests/`, `Brinell.WinForms.UITests/`, `Brinell.Maui.UITests/`, `Brinell.Stride.UITests/` — zero changes
  * The `Async` suffix on UI interaction method names — no `ClickAsync()`; sync/async are differentiated by interface, not name

* **Constraints:**
  * Core interfaces must not gain `Task` return types — desktop platforms must compile without changes
  * `ConfigureAwait(false)` on all `await` calls in framework library code (not in tests)
  * `Task.Delay` only in bounded polling loops — never arbitrary
  * No empty catch blocks
  * Factory and lifecycle methods retain the `Async` suffix (`CreateAsync`, `DisposeAsync`, `InitializeAsync`)

---

## Priority Order

1. **Design async HTML interface set** — `IAsyncHtmlElement`, `IHtmlAsyncControlObject<TScope>`, and per-capability async interfaces; this defines the contract
2. **`AsyncControlBase<TScope>` in `Brinell.Html`** — adds `RunWithElementAsync` and implements async control interfaces; single hub for all derived HTML controls
3. **`PlaywrightHtmlElement`** — implements `IAsyncHtmlElement`; adds proper `await` paths for each Playwright async API
4. **`PlaywrightTestContext`** — async navigation and driver-level async methods
5. **HTML test files** — convert to `async Task` and use async interface where preferred

---

## Assumptions

* `srcnew/` and `testsnew/` are the active codebases; root-level folders are legacy
* No external consumers — clean interface additions are safe
* xUnit supports `async Task` test methods natively; no configuration needed
* `Task.Delay` in bounded polling loops satisfies the no-arbitrary-sleep rule
* The sync HTML API remains valid and supported — no deprecation of existing sync tests

---

## Risks and Concerns

* **Naming collision** — sync `Click() : TScope` and async `Click() : Task<TScope>` on the same class require explicit interface implementation to avoid ambiguity; the research must document the pattern clearly
* **`ConfigureAwait(false)` in framework code** — library code should use it; test code should not (xUnit manages `SynchronizationContext`)
* **Playwright auto-timeout vs. framework `timeoutMs`** — when using Playwright's own waiting (e.g., `WaitForAsync`), the framework's polling loop must not double-count timeouts; the research should clarify which layer owns the timeout for each method
* **Sync `.GetAwaiter().GetResult()` paths remain** — sync interface methods on `PlaywrightHtmlElement` still block; this is accepted as the cost of maintaining the dual API

---

## Suggestions

* `RunWithElementAsync(Func<IHtmlElement, Task> action)` in a new `AsyncControlBase<TScope>` — mirrors the existing `RunWithElement` pattern and centralizes async element-fetch + action (see `srcnew/Brinell.Html/Controls/ControlBase.cs`)
* Explicit interface implementation pattern for dual sync/async:
  ```csharp
  // Sync (IControlObject<TScope>)
  TScope IControlObject<TScope>.Click() { ... }
  // Async (IHtmlAsyncControlObject<TScope>)
  async Task<TScope> IHtmlAsyncControlObject<TScope>.Click() { ... }
  ```
* Tests in `testsnew/Brinell.Html.UITests/Tests/Controls/` are short — good first targets for the async path validation

---

## User Steering Notes

* No `Async` suffix on UI interaction method names — confirmed
* HTML/Blazor gets both sync and async
* All other platforms (WPF, WinForms, MAUI, Stride) remain sync-only with no changes
* `Brinell.Core` must stay sync — this is the hard constraint that protects desktop platforms
