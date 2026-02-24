<!-- markdownlint-disable-file -->

# Questions: Blazor Refactoring from src/tests to srcnew/testsnew

## Round 1

### 🎯 Research Scope

**What is the main goal of this refactoring?**

- [X] Migrate `src/Brinell.Blazor/ControlObject6/` controls to `srcnew/Brinell.Blazor/` following the `srcnew/Brinell.Html` architecture *(directly stated by user — Blazor controls need to adopt the new Html-based layered design)*
- [X] Migrate `tests/Brinell.Blazor.Tests.ControlObject6/` unit tests to `testsnew/Brinell.Blazor.Tests/` *(directly stated — tests must follow the new test project structure)*
- [ ] Also populate `testsnew/Brinell.Blazor.UITests/` with UI tests modeled on `testsnew/Brinell.Html.UITests/`
- [ ] Other:

**What does a successful outcome look like?**

- [X] `srcnew/Brinell.Blazor/` contains fully implemented controls replacing placeholder files, matching the architecture of `srcnew/Brinell.Html/` *(this is the core deliverable — placeholders must become real implementations)*
- [X] `testsnew/Brinell.Blazor.Tests/` contains unit tests covering the new controls *(test coverage for the refactored code)*
- [ ] Old `src/Brinell.Blazor/ControlObject6/` and `tests/Brinell.Blazor.Tests.ControlObject6/` can be deleted after migration
- [ ] All tests pass on the new projects
- [ ] Other:

---

### 📋 Scope Boundaries

**Which source projects are in scope for refactoring?**

- [X] `src/Brinell.Blazor/ControlObject6/Controls/` — 22 control files *(the primary source of Blazor control logic to migrate)*
- [X] `src/Brinell.Blazor/ControlObject6/Context/BlazorTestContext.cs` — context setup *(context wiring is required for controls to function)*
- [X] `src/Brinell.Blazor/ControlObject6/Interfaces/` — 3 interface files *(interfaces define the async contract, need to map to new sync pattern)*
- [X] `src/Brinell.Blazor/ControlObject6/Pages/AsyncPageObjectBase.cs` — page object base *(page object base is needed for test structure)*
- [ ] Other:

**Which test projects are in scope?**

- [X] `tests/Brinell.Blazor.Tests.ControlObject6/` — 20 test files + mock factory → `testsnew/Brinell.Blazor.Tests/` *(directly requested by user)*
- [ ] `testsnew/Brinell.Blazor.UITests/` — create UI integration tests mirroring `testsnew/Brinell.Html.UITests/`
- [ ] Other:

**What should the research explicitly skip?**

- [X] Changes to `srcnew/Brinell.Html/` or `srcnew/Brinell.Html.Playwright/` — those are the reference, not the target *(these are stable reference projects, not to be modified)*
- [X] Changes to `srcnew/Brinell.Core/` — assumed stable *(core abstractions are shared and should not change for this task)*
- [ ] Changes to sample projects under `samples/`
- [ ] Other:

---

### 🔍 Technical Context

**The old Blazor controls are fully async (Task-based). The new Html controls are synchronous with generic `<TScope>` fluent chaining. How should the migration handle this?**

- [ ] Convert all async methods to synchronous, matching the `Brinell.Html` pattern exactly *(the new architecture is synchronous — `srcnew/Brinell.Html/Controls/` demonstrates this consistently)*
- [X] Keep a dual async/sync API in the Blazor layer
- [ ] Provide async wrappers that delegate to the synchronous Html base classes
- [ ] Other:

**The old controls use `BlazorTestContext` wrapping Playwright `IPage` directly. The new pattern uses `IHtmlTestContext` → `IHtmlElement` abstraction. How should `srcnew/Brinell.Blazor/` relate to `srcnew/Brinell.Html`?**

- [X] `Brinell.Blazor` should depend on and extend `Brinell.Html` — Blazor controls inherit from Html controls and add Blazor-specific behavior *(the Html layer already provides the full base hierarchy; Blazor is a specialization, not a rewrite)*
- [ ] `Brinell.Blazor` should be standalone, copying the Html pattern but independent
- [ ] `Brinell.Blazor` should depend on `Brinell.Html.Playwright` for the Playwright implementation
- [ ] Other:

**The old controls construct with `(BlazorTestContext, string testId, IAsyncPageObject?)`. The new pattern uses `(TScope scope, Locator locator)` with `IHtmlScope`. What constructor pattern should the Blazor controls use?**

- [X] Follow the new `(TScope scope, Locator locator)` pattern from `Brinell.Html` *(consistent architecture — constructors should match the reference implementation)*
- [ ] Add Blazor-specific convenience constructors alongside the standard pattern
- [ ] Other:

**What about Blazor-specific controls not present in `Brinell.Html`?**

The old `ControlObject6/` has controls with no direct Html equivalent: `AudioControl`, `VideoControl`, `IFrameControl`, `ImageControl`, `NavMenuControl`.

- [X] Migrate them as Blazor-specific additions in `srcnew/Brinell.Blazor/Controls/` *(these are legitimate media/navigation controls that belong in Blazor but not in the generic Html layer)*
- [ ] Add them to `srcnew/Brinell.Html/` first, then inherit in Blazor
- [ ] Skip them for now — focus on controls that have Html equivalents
- [ ] Other:

---

### 🧩 Topic Decomposition

**Which sub-topics should the research cover?**

- [X] **Source Architecture** — How `srcnew/Brinell.Blazor/` should layer on `srcnew/Brinell.Html/` (project references, inheritance, namespace mapping) *(fundamental — must decide the dependency model before implementing anything)*
- [X] **Control Migration Plan** — Map each old control to its new equivalent, identify what inherits from Html vs. what's Blazor-only *(need a concrete 1:1 mapping to execute the migration)*
- [X] **Test Migration Plan** — How to adapt old async Playwright-mocking tests to the new synchronous `IHtmlElement`-mocking pattern *(test strategy must change along with the sync/async shift)*
- [ ] **Context/Page Mapping** — How `BlazorTestContext` maps to the new context/scope hierarchy
- [ ] **UITests Scaffolding** — Whether and how to populate `testsnew/Brinell.Blazor.UITests/`
- [ ] Other:

---

### 💡 Assumptions

*Inferred from codebase analysis. Correct any that are wrong.*

- [X] `srcnew/Brinell.Blazor/` currently has only placeholder files — this is a greenfield implementation within the new architecture *(verified: all 4 subdirectories contain only `Placeholder.cs` with namespace declarations)*
- [X] The `srcnew/Brinell.Blazor.csproj` currently references only `Brinell.Core` + `Microsoft.Playwright` — it likely needs a reference to `Brinell.Html` added *(the csproj was created without the Html dependency; adding it enables inheritance from Html base classes)*
- [X] The old `ControlObject6` suffix indicates this was the v6 control object design — the new architecture supersedes it *(naming convention suggests iterative versioning; srcnew is the "next" generation)*
- [X] `testsnew/Brinell.Blazor.Tests/` and `testsnew/Brinell.Blazor.UITests/` are scaffolded but empty — ready to receive migrated content *(both have csproj + GlobalUsings.cs only)*
- [X] FluentAssertions is NOT used in `testsnew/` test projects (only xunit assertions) — migrated tests should use xunit Assert *(testsnew projects don't reference FluentAssertions; old tests use `.Should()` which needs conversion)*
- [ ] None of these — remove all assumptions

---

### ⚠️ Risks and Concerns

**Are there known risks, past failures, or sensitive areas the research should address?**

- [X] **Async-to-sync conversion may lose Playwright timeout behavior** — Playwright's `ILocator` methods are inherently async; wrapping them synchronously requires care *(the Html layer uses `IHtmlElement` abstraction to hide this — need to verify Playwright adapter handles it)*
- [ ] **Breaking changes to public API** — if anyone depends on the old `ControlObject6` interfaces
- [ ] **Test coverage gaps** — some old tests may not have new equivalents
- [ ] **Mermaid/documentation updates needed** — diagrams referencing old Blazor structure
- [ ] No known risks
- [ ] Other:

---

### 🔎 Suggestions

**`srcnew/Brinell.Html.Playwright/` already provides the Playwright-to-IHtmlElement bridge** — see [srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs](srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs)

- [X] `srcnew/Brinell.Blazor/` should reference `Brinell.Html.Playwright` (or `Brinell.Html` alone) rather than wrapping Playwright directly *(avoids duplicating the Playwright abstraction layer — the bridge already exists)*
- [ ] Keep direct Playwright dependency in Blazor for more control
- [ ] Dismiss — not relevant to this research
- [ ] Other:

**The old `ControlObject6` maps almost 1:1 to the new Html controls** — 17 of 22 old controls have direct equivalents in `srcnew/Brinell.Html/Controls/`

- [X] Create a mapping table (old → new) and implement Blazor controls as thin wrappers or direct inheritors of Html controls *(most controls need no Blazor-specific logic — they can inherit directly)*
- [ ] Rewrite each control from scratch in the new pattern
- [ ] Dismiss — not relevant to this research
- [ ] Other:

**`testsnew/Brinell.Html.UITests/` provides a complete reference for UI test structure** — includes TestBase, PageObjects, and Tests directories with working patterns

- [X] Use `Brinell.Html.UITests/` as the template for `Brinell.Blazor.UITests/` structure *(consistent test organization across platforms)*
- [ ] Dismiss — not relevant to this research
- [ ] Other:

**Old to new control mapping (preliminary):**

| Old (`ControlObject6`)      | New Html Equivalent              | Notes           |
| ----------------------------- | -------------------------------- | --------------- |
| `AsyncControlObjectBase`    | `ControlBase<TScope>`          | Base class      |
| `AsyncClickableControlBase` | `ClickableControlBase<TScope>` | Click behavior  |
| `AsyncTextControlBase`      | `FocusableControlBase<TScope>` | Text input base |
| `ButtonControl`             | `ButtonControl<TScope>`        | Direct match    |
| `LinkControl`               | `LinkControl<TScope>`          | Direct match    |
| `InputControl`              | `TextInputControl<TScope>`     | Renamed         |
| `TextAreaControl`           | `TextAreaControl<TScope>`      | Direct match    |
| `CheckBoxControl`           | `CheckBoxControl<TScope>`      | Direct match    |
| `RadioButtonControl`        | `RadioButtonControl<TScope>`   | Direct match    |
| `SelectControl`             | `SelectControl<TScope>`        | Direct match    |
| `RangeControl`              | `RangeInputControl<TScope>`    | Renamed         |
| `ProgressControl`           | `ProgressControl<TScope>`      | Direct match    |
| `ListControl`               | `ListControl<TScope>`          | Direct match    |
| `TableControl`              | `TableControl<TScope>`         | Direct match    |
| `TabControl`                | `TabContainerControl<TScope>`  | Renamed         |
| `DateInputControl`          | `DateInputControl<TScope>`     | Direct match    |
| `TimeInputControl`          | `TimeInputControl<TScope>`     | Direct match    |
| `AudioControl`              | —                               | Blazor-only     |
| `VideoControl`              | —                               | Blazor-only     |
| `IFrameControl`             | —                               | Blazor-only     |
| `ImageControl`              | —                               | Blazor-only     |
| `NavMenuControl`            | —                               | Blazor-only     |

## Round 2

*Follow-up questions based on Round 1 answers. The dual async/sync API choice (vs. sync-only) drives several architectural decisions.*

### ⚡ Conflicts

**Potential tension detected:** You selected both "Brinell.Blazor should depend on and extend Brinell.Html" (sync base classes with `TScope` fluent return) **and** "Keep a dual async/sync API in the Blazor layer." The Html base classes are synchronous — `Click()` returns `TScope`, `IsExists()` returns `bool`. Adding async variants means the Blazor layer needs to define `ClickAsync()` returning `Task<TScope>` alongside the inherited sync `Click()`. This is resolvable but requires a clear design choice on how the async methods relate to the sync base. The questions below explore this.

---

### 🔀 Dual API Design

**How should the async methods relate to the synchronous Html base class methods?**

The sync base (`ControlBase<TScope>`) already works end-to-end with Playwright via `PlaywrightHtmlElement` using `.GetAwaiter().GetResult()`. Adding async means a second path.

- [X] **Async extension methods** — Keep controls inheriting sync Html bases as-is; add `static class BlazorControlExtensions` providing `ClickAsync()`, `IsExistsAsync()` etc. as extension methods that call Playwright directly *(non-invasive — doesn't change the class hierarchy; tests can opt into async via `using Brinell.Blazor.Extensions`)*
- [ ] **Async override layer** — Blazor controls override/shadow sync methods with `new async Task<TScope> ClickAsync()` alongside the inherited sync `Click()` *(keeps async on the control class itself, but adds method bloat)*
- [ ] **Parallel interface** — Define `IAsyncControl<TScope>` with async counterparts for every sync method, implement on Blazor controls alongside sync *(explicit contract but heavy to maintain)*
- [ ] **Async-only for Blazor-specific controls** — Only the 5 Blazor-only controls (Audio, Video, IFrame, Image, NavMenu) get async methods; the 17 Html-mapped controls stay sync-only *(pragmatic compromise — async where Html doesn't cover)*
- [ ] Other:

**Should the async methods return `Task<TScope>` for fluent chaining, or `Task` / `Task<T>` like the old pattern?**

- [X] Return `Task<TScope>` for fluent chaining consistency with the sync API: `await button.ClickAsync()` returns `TScope` *(maintains the fluent pattern established by Html base classes)*
- [ ] Return `Task` / `Task<T>` like the old `ControlObject6` pattern — simpler async but no chaining
- [ ] Other:

**How should Blazor-specific async methods access the Playwright `ILocator` when the base class uses `IHtmlElement`?**

`PlaywrightHtmlElement` wraps `ILocator` and blocks on async calls internally. For true async, the Blazor layer needs access to the underlying `ILocator`.

- [X] Add a `GetPlaywrightLocator()` method or property to `PlaywrightHtmlElement` that exposes the native `ILocator` for async use *(allows the Blazor layer to call Playwright natively for async while sync stays through `IHtmlElement`)*
- [ ] Cast `IHtmlElement` to `PlaywrightHtmlElement` in the Blazor layer to access the locator *(works but couples Blazor to the Playwright implementation)*
- [ ] Introduce an `IAsyncHtmlElement` interface at the `Brinell.Html` level with async methods *(clean but requires changes to the Html layer — marked as out of scope)*
- [ ] Other:

---

### 🏗️ Blazor Project Structure

**The current `srcnew/Brinell.Blazor.csproj` references `Brinell.Core` + `Microsoft.Playwright`. Given the decision to extend `Brinell.Html`, what references should it have?**

- [X] `Brinell.Html` + `Brinell.Html.Playwright` + `Microsoft.Playwright` — inherit Html controls, access Playwright for async *(needs `Brinell.Html.Playwright` to access `PlaywrightHtmlElement` for the async bridge; `Brinell.Core` comes transitively through `Brinell.Html`)*
- [ ] `Brinell.Html` only — no direct Playwright dependency in the Blazor project *(only works if async is abandoned or done purely through extensions)*
- [ ] `Brinell.Html` + `Microsoft.Playwright` — skip `Brinell.Html.Playwright` reference *(would need to duplicate Playwright element wrapping)*
- [ ] Other:

**What namespace structure should `srcnew/Brinell.Blazor/` use?**

The current placeholder namespaces are `Brinell.Blazor.Context`, `Brinell.Blazor.Controls`, `Brinell.Blazor.Pages`, `Brinell.Blazor.Testing`.

- [X] Keep the existing namespace structure — it already mirrors `Brinell.Html.*` *(consistent with the established convention)*
- [ ] Flatten to a single `Brinell.Blazor` namespace
- [ ] Add sub-namespaces for control categories (e.g., `Brinell.Blazor.Controls.Buttons`)
- [ ] Other:

---

### 🧪 Test Migration Details

**The old tests use `FluentAssertions` (`.Should().Be()`). The new `testsnew/` pattern uses xunit `Assert.*`. How thoroughly should assertions be converted?**

- [X] Full conversion — all `.Should()` calls become `Assert.*` equivalents *(consistent with testsnew convention; FluentAssertions isn't referenced in these projects)*
- [ ] Keep FluentAssertions — add the package reference to `testsnew/Brinell.Blazor.Tests/`
- [ ] Other:

**The old tests mock Playwright `IPage`/`ILocator` via `MockPlaywrightFactory`. The new pattern mocks `IHtmlElement`/`IHtmlScope`. How should the test mocking strategy change?**

- [X] Mock at the `IHtmlElement`/`IHtmlScope<TScope>` level — consistent with the Html test pattern and decoupled from Playwright *(the new architecture abstracts away Playwright; tests should mock at the abstraction boundary)*
- [ ] Keep mocking Playwright `IPage`/`ILocator` for the async methods — needed if async calls through to Playwright directly
- [ ] Mock both levels — `IHtmlElement` for sync tests, `ILocator` for async tests
- [ ] Other:

**Should the test project reference `Brinell.Html.Playwright` for testing async behavior?**

- [ ] Yes — needed to test async methods that access Playwright through the element
- [ ] No — unit tests should only mock at the abstraction boundary; integration tests cover Playwright
- [X] Only if the async API requires it — depends on the dual API design chosen above *(pragmatic — the answer depends on where async lives)*
- [ ] Other:

---

### 🔎 Suggestions

**`PlaywrightHtmlElement` already blocks on async via `.GetAwaiter().GetResult()`** — see [srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs](srcnew/Brinell.Html.Playwright/PlaywrightHtmlElement.cs)

- [X] The sync path is fully functional for all Playwright operations — the async API is an *optimization* layer, not a *correctness* layer *(important framing: sync works today, async avoids potential deadlocks and improves perf in concurrent scenarios)*
- [ ] Dismiss — not relevant to this research
- [ ] Other:

**The old `BlazorTestContext` wraps `IPage` and provides `DefaultTimeoutMs`, `Log()`, screenshot support** — see [src/Brinell.Blazor/ControlObject6/Context/BlazorTestContext.cs](src/Brinell.Blazor/ControlObject6/Context/BlazorTestContext.cs)

- [X] Create a `BlazorTestContext` in `srcnew/Brinell.Blazor/Context/` that implements `IHtmlTestContext` and wraps Playwright `IPage`, reusing `PlaywrightTestContext` from `Brinell.Html.Playwright` as a reference *(consistent with the layered approach — Blazor context extends the Html context pattern)*
- [ ] Reuse `PlaywrightTestContext` directly without a Blazor-specific context
- [ ] Dismiss — not relevant to this research
- [ ] Other:

## Round 3

*User is considering moving the entire framework from sync to fully async for all UI tests — not just Blazor. This supersedes the Round 2 dual-API discussion and reframes the Blazor refactoring as a sequencing decision.*

### ⚡ Context Shift

Round 1 asked "sync or dual?" for Blazor specifically. The user now indicates a broader intent: **all of `srcnew/` may go async**. This means `Brinell.Html` base classes (`ControlBase<TScope>`, `Control<TScope>`, etc.) would eventually become async too. The Blazor refactoring must be planned relative to that larger migration.

---

### 🎯 Sequencing Strategy

**Given the planned framework-wide async migration, what should the Blazor refactoring do NOW?**

- [X] **Sync-first, async-ready** — Implement Blazor controls sync (inheriting current Html bases) with the architecture designed so the async conversion is mechanical later. When Html goes async, Blazor follows naturally. *(lowest risk — get the structural refactoring done now; async is a separate concern applied uniformly later. Avoids Blazor diverging from Html during the transition.)*
- [ ] **Blazor as async pilot** — Implement Blazor controls async-first, using this as the prototype for how the full framework async migration will look. Then backport the pattern to `Brinell.Html`. *(faster path to async for Blazor, but Blazor temporarily diverges from Html and may need rework when Html catches up.)*
- [ ] **Pause Blazor refactoring** — Do the framework-wide async migration in `Brinell.Html` first, then refactor Blazor to match the new async Html architecture. *(cleanest result but blocks Blazor work on the larger async decision.)*
- [ ] **Async-only Blazor now** — Build `srcnew/Brinell.Blazor/` as fully async from scratch, independent of `Brinell.Html`. Reconnect when Html goes async. *(fast for Blazor, but loses the Html inheritance benefit and creates a temporary fork.)*
- [ ] Other:

---

### 🔄 Async Migration Scope

**When you say "move to fully async", what scope do you envision?**

- [ ] **All layers** — `IHtmlElement`, `IControlObject<TScope>`, `ControlBase<TScope>`, `Control<TScope>`, all concrete controls, page objects, test fixtures
- [ ] **Control API only** — `Click()` → `ClickAsync()`, `GetText()` → `GetTextAsync()` etc., but keep element abstraction sync internally
- [ ] **Test-facing API only** — Controls expose async methods for tests; internal plumbing stays sync with `.GetAwaiter().GetResult()` at the adapter level
- [X] Still exploring — the scope isn't decided yet
- [ ] Other: What would you think is best?

**Is the async migration a separate task/research topic, or should this Blazor refactoring research encompass it?**

- [X] **Separate task** — The async migration is a broader architectural decision that deserves its own research brief. The Blazor refactoring should proceed with whatever makes it easiest to convert later. *(keeps the Blazor task focused and deliverable)*
- [ ] **Combined** — Research both the Blazor refactoring and the async migration together in one effort
- [ ] Other:

---

### 🏗️ Async-Ready Architecture

**If going sync-first now, what design choices make the future async conversion easiest?**

- [X] Keep the `IHtmlElement` abstraction — when it goes async (`ClickAsync()`), all controls automatically get async via `RunWithElement` / `RunWithElementAsync` *(the element abstraction is the key leverage point — async at the element level propagates up for free)*
- [X] Use the `RunWithElement(Action<IHtmlElement>)` pattern consistently — this becomes `RunWithElementAsync(Func<IHtmlElement, Task>)` mechanically *(the pattern is already designed for this; the conversion is a search-and-replace)*
- [X] Don't introduce Blazor-specific Playwright shortcuts — keep everything going through `IHtmlElement` so async can be applied uniformly *(shortcuts to `ILocator` would become tech debt when the abstraction goes async)*
- [ ] Other:

---

### 💡 Assumptions

- [X] The framework-wide async migration is a future task, not imminent — Blazor refactoring should not be blocked on it *(user said "considering" — this is a direction, not a commitment with a timeline)*
- [X] The Round 2 "dual async/sync API" questions are superseded — the answer is "sync now, async framework-wide later" *(no need for Blazor-specific async extensions or Playwright locator exposure if async comes to all controls via the Html layer)*
- [ ] None of these — remove all assumptions

---

### 🔎 Suggestions

**The `RunWithElement` pattern in `ControlBase<TScope>` is already async-conversion-ready** — see [srcnew/Brinell.Html/Controls/ControlBase.cs](srcnew/Brinell.Html/Controls/ControlBase.cs#L36-L48)

The current pattern:

```csharp
protected TScope RunWithElement(Action<IHtmlElement> action)
{
    var element = FindElement();
    action(element);
    return ContainingScope;
}
```

Future async version (mechanical conversion):

```csharp
protected async Task<TScope> RunWithElementAsync(Func<IHtmlElement, Task> action)
{
    var element = FindElement();
    await action(element);
    return ContainingScope;
}
```

- [X] This confirms sync-first is safe — the control architecture already supports async conversion without redesign *(the abstraction boundary is in the right place)*
- [ ] Dismiss — not relevant
- [ ] Other:

**The Blazor-only controls (Audio, Video, IFrame, Image, NavMenu) should also go through `IHtmlElement` even though they're not in the Html layer** — keeps them on the same async-conversion path

- [X] Implement Blazor-only controls using `ControlBase<TScope>` / `Control<TScope>` from `Brinell.Html` as base classes, not raw Playwright *(they participate in the same architecture and benefit from the future async migration automatically)*
- [ ] Dismiss — not relevant
- [ ] Other:
