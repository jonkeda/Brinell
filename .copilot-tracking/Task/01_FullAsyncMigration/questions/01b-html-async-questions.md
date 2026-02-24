<!-- markdownlint-disable-file -->
# Questions: Add Async to HTML/Blazor (Playwright) Stack

**Scope:** `Brinell.Html`, `Brinell.Html.Playwright`, `testsnew/Brinell.Html.UITests`
**Out of scope:** `Brinell.Core`, `Brinell.Wpf`, `Brinell.WinForms`, `Brinell.Maui`, `Brinell.Stride` — zero changes to any of these

---

## Round 1

### 🎯 Goal

**What does success look like for this task?**

- [x] The HTML/Blazor stack exposes async versions of all control interactions — `Click`, `Enter`, `SetText`, `Check`, `Select`, etc. — so Playwright's async API is used properly without `.GetAwaiter().GetResult()` blocking *(core problem: `PlaywrightHtmlElement` blocks on every Playwright call today)*
- [ ] Only the `PlaywrightHtmlElement` internals are cleaned up — sync-over-async removed, but the public API stays sync
- [ ] Both: clean internals AND expose an async public API
- [ ] Other:

**Should the existing sync API be kept alongside async?**

- [x] Yes — keep all existing sync methods untouched; add async on top *(other tests and users of the sync API continue to work without changes; nothing breaks)*
- [ ] No — replace sync with async; migrate all callers
- [ ] Other:

---

### 🏗️ Interface Design

**Where should async methods live — on the existing interfaces or new HTML-specific async interfaces?**

- [x] New HTML-specific interfaces in `Brinell.Html` — e.g., `IHtmlAsyncControlObject<TScope>`, `IHtmlAsyncClickable<TScope>`, etc. *(`Brinell.Core` interfaces stay 100% sync; desktop platforms require zero changes)*
- [ ] Extend the existing `IHtmlElement` and `IHtmlScope` interfaces directly with async methods
- [ ] Add async methods directly to `ControlBase<TScope>` without any new interface
- [ ] Other:

**Should there be a single `IHtmlAsyncControlObject<TScope>` interface or one async interface per capability?**

- [x] Mirroring the existing structure — one async interface per capability: `IHtmlAsyncClickable<TScope>`, `IHtmlAsyncEditable<TScope>`, `IHtmlAsyncToggle<TScope>`, etc. *(matches the current `IClickableControlObject`, `IEditableTextControlObject`, `IToggleControlObject` structure; clean parallel hierarchy)*
- [ ] One flat `IHtmlAsyncControlObject<TScope>` with all async methods *(simpler; fewer files)*
- [ ] Other:

**Should `IHtmlElement` gain async methods for low-level Playwright access?**

- [x] Yes — add a parallel `IAsyncHtmlElement` interface with async versions of all `IHtmlElement` methods (`Task Click()`, `Task<bool> IsVisible()`, etc.); `PlaywrightHtmlElement` implements both *(enables truly async all the way down; eliminates `.GetAwaiter().GetResult()` in the internals of base classes)*
- [ ] No — keep `IHtmlElement` sync; async is only at the control-object level
- [ ] Other:

---

### 🔤 Naming

**How should the naming collision between sync and async be resolved?**

`Click()` today returns `TScope` (sync). An async `Click()` returning `Task<TScope>` would conflict — same name, same parameters, two return types is a compile error.

- [ ] Use the `Async` suffix on async methods only for HTML — `ClickAsync()`, `EnterAsync()` etc. *(pragmatic exception to the no-`Async`-suffix rule; avoids any naming complexity)*
- [x] Keep same method names; separate them by interface — `IClickableControlObject<TScope>.Click()` returns `TScope`; `IHtmlAsyncClickable<TScope>.Click()` returns `Task<TScope>`; implementations use explicit interface implementation to resolve *(no suffix anywhere; sync/async distinguished by which interface the caller uses)*
- [ ] Other:

**Should `Wait*` and `Assert*` methods also have async versions?**

- [x] Yes — `WaitExists`, `WaitVisible`, `AssertExists`, `AssertVisible`, etc. all get async counterparts *(test authors using async will want the full API, not just action methods)*
- [ ] No — only action methods (`Click`, `Enter`, `SetText`, etc.) need async variants; `Wait*` and `Assert*` can remain sync
- [ ] Other:

---

### ⚙️ Implementation

**How should `Wait*` async polling work?**

Current sync `Wait*` uses a spin-loop. Async needs to yield between polls.

- [x] `await Task.Delay(pollIntervalMs).ConfigureAwait(false)` inside a deadline loop — this is controlled polling (waiting FOR a condition), not an arbitrary sleep *(correct async polling pattern; complies with the no-arbitrary-wait rule)*
- [ ] Use `PeriodicTimer` with a `CancellationTokenSource` for the timeout
- [ ] Delegate to Playwright's own built-in waiting (e.g., `WaitForAsync`) instead of polling
- [ ] Other:

**How should `RunWithElement` work in the async path?**

`RunWithElement(Action<IHtmlElement> action)` in `ControlBase<TScope>` is the central helper all controls use. The async path needs an equivalent.

- [x] Add `RunWithElementAsync(Func<IHtmlElement, Task> action)` returning `Task<TScope>` alongside the existing sync `RunWithElement` *(mirrors existing pattern exactly; derived controls add async methods by simply calling `RunWithElementAsync`)*
- [ ] Replace `RunWithElement` with an async version; sync methods call `.GetAwaiter().GetResult()` internally
- [ ] Other:

**Should framework library code use `ConfigureAwait(false)` on `await` calls?**

- [x] Yes — all `await` calls in `Brinell.Html` and `Brinell.Html.Playwright` (non-test code) use `.ConfigureAwait(false)` *(library code should not capture synchronization context; prevents potential deadlocks and unnecessary overhead)*
- [ ] No — keep it simple; omit `ConfigureAwait`
- [ ] Other:

---

### 🧪 Tests

**Should HTML/Blazor UI tests be updated to use async?**

- [x] Yes — test methods in `testsnew/Brinell.Html.UITests/` should become `async Task` and use the new async API *(validates the async path; xUnit supports `async Task` tests natively)*
- [ ] No — keep existing tests sync; only add new async tests alongside
- [ ] Other:

**How should `NavigateTo` in test base classes be handled?**

`BlazorSampleTestBase.NavigateToPage()` calls `Context.NavigateTo()` which today blocks on Playwright's `GotoAsync`.

- [x] Make it async: `Task NavigateToPage(string path)` — test methods that call it must be `async Task` anyway *(consistent; eliminates the last remaining `.GetAwaiter().GetResult()` in test base classes)*
- [ ] Leave it sync — it's test infrastructure, not a control interaction
- [ ] Other:

---

### 📋 Migration Order

**What order should the work be done in?**

- [x] (1) Define async interfaces in `Brinell.Html` → (2) Add `RunWithElementAsync` and async base methods to `ControlBase<TScope>` and base control classes → (3) Implement async on `PlaywrightHtmlElement` and `PlaywrightTestContext` → (4) Update HTML/Blazor tests *(top-down; interfaces define the contract and drive everything below)*
- [ ] Bottom-up: fix `PlaywrightHtmlElement` first, then work up through base classes and interfaces
- [ ] Other:

---

### 💡 Assumptions

- [x] `Brinell.Core` interfaces (`IControlObject<TScope>`, `IElement<TSelf>`, etc.) are unchanged — this is the hard constraint that protects all non-HTML platforms
- [x] `Brinell.Wpf`, `Brinell.WinForms`, `Brinell.Maui`, `Brinell.Stride` and their test projects receive zero changes
- [x] The no-`Async`-suffix rule applies everywhere except optionally the HTML async interface layer if naming conflicts make it necessary
- [x] Factory and lifecycle methods (`CreateAsync`, `DisposeAsync`, `InitializeAsync`) always retain the `Async` suffix — they are not UI interaction methods
- [x] xUnit supports `async Task` test methods natively — no test framework changes needed

---

### ⚠️ Risks and Concerns

**Which risks should the research address?**

- [x] Naming collision between sync `Click() : TScope` and async `Click() : Task<TScope>` — how explicit interface implementation handles it in practice *(requires clear code example in the research output)*
- [x] `PlaywrightHtmlElement` implementing both `IHtmlElement` (sync properties) and `IAsyncHtmlElement` (async methods) — does the dual implementation cause any ambiguity?
- [x] Playwright's own internal timeouts vs. framework `timeoutMs` polling — which layer controls the timeout for `Wait*` methods?
- [ ] Thread safety of async HTML controls when tests run in parallel
- [ ] No significant risks — scope is well-contained
- [ ] Other:

---

### 🔎 Suggestions

**`PlaywrightHtmlElement` has 30+ `.GetAwaiter().GetResult()` blocking calls** — see [srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs](../../../../../srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs)

- [x] These are the primary target — converting them to `async`/`await` + `ConfigureAwait(false)` on the async interface implementation eliminates sync-over-async *(mechanical transformation; each property/method that blocks can gain a proper async counterpart)*
- [ ] Dismiss

**`ControlBase<TScope>` is the single hub all HTML controls inherit from** — see [srcnew/Brinell.Html/Controls/ControlBase.cs](../../../../../srcnew/Brinell.Html/Controls/ControlBase.cs)

- [x] Adding `RunWithElementAsync` here means every derived control (`ClickableControlBase`, `ToggleControlBase`, `TextInputControl`, `CheckBoxControl`, etc.) gets async capability with minimal per-class changes *(high leverage point — one change cascades to ~15 control classes)*
- [ ] Dismiss

**Existing sync controls to gain async equivalents:**
  `Control.cs` (`Click`, `SendKeys`, `Clear`, `ScrollIntoView`),
  `ClickableControlBase.cs` (`DoubleClick`, `RightClick`, `Hover`),
  `ToggleControlBase.cs` (`SetChecked`, `WaitChecked`),
  `CheckBoxControl.cs` (`Check`, `Uncheck`, `Toggle`),
  `Text/TextInputControl.cs`, `Text/TextAreaControl.cs`,
  `Selection/`, `Range/`, `DateTime/`

- [x] Document the full list as the migration scope so nothing is missed *(complete inventory prevents partial migration)*
- [ ] Only migrate the most-used controls first
- [ ] Dismiss
