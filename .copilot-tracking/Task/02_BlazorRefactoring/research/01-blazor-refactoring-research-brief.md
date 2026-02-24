<!-- markdownlint-disable-file -->
# Research Brief: Blazor Refactoring from src/tests to srcnew/testsnew

**Created:** 2026-02-23
**Status:** ✅ Approved
**Questions Document:** .copilot-tracking/Task/02_BlazorRefactoring/questions/01-blazor-refactoring-questions.md

## Validated Research Questions

1. **How should `srcnew/Brinell.Blazor/` layer on `srcnew/Brinell.Html/`?**
   * Context: The current Blazor project has only placeholder files. The Html layer provides a complete sync control hierarchy with `<TScope>` fluent chaining. Blazor needs to extend it, not duplicate it.
   * Proposed hypothesis: Add `Brinell.Html` + `Brinell.Html.Playwright` as project references. Blazor controls inherit from Html base classes directly. Controls with Html equivalents (17 of 22) are thin wrappers; Blazor-only controls (5) extend `ControlBase<TScope>` / `Control<TScope>` from Html.

2. **What is the sync/async strategy for this refactoring?**
   * Context: User is considering a framework-wide async migration (separate future task, scope TBD). The Blazor refactoring should not be blocked on it.
   * Proposed hypothesis: Implement sync-first, matching the current Html architecture exactly. Use `RunWithElement(Action<IHtmlElement>)` pattern consistently. Do NOT introduce Blazor-specific Playwright shortcuts or async APIs. The architecture is already async-conversion-ready — when `IHtmlElement` goes async, all controls follow mechanically.

3. **How should each old ControlObject6 control map to the new architecture?**
   * Context: 22 controls in old `src/Brinell.Blazor/ControlObject6/Controls/`, 17 have direct Html equivalents, 5 are Blazor-only.
   * Proposed hypothesis: Use the mapping table from the questions document. Controls with Html equivalents inherit from the corresponding Html control class. Blazor-only controls (Audio, Video, IFrame, Image, NavMenu) extend `Control<TScope>` or `ClickableControlBase<TScope>` and go through `IHtmlElement` (not raw Playwright).

4. **How should tests be migrated from `tests/Brinell.Blazor.Tests.ControlObject6/` to `testsnew/Brinell.Blazor.Tests/`?**
   * Context: Old tests mock Playwright `IPage`/`ILocator` via `MockPlaywrightFactory` and use FluentAssertions. New pattern mocks `IHtmlElement`/`IHtmlScope<TScope>` and uses xunit `Assert.*`.
   * Proposed hypothesis: Full conversion — mock at `IHtmlElement`/`IHtmlScope<TScope>` level, convert all FluentAssertions to xunit Assert, follow constructor pattern `(TScope scope, Locator locator)`.

5. **What project references and namespace structure should `srcnew/Brinell.Blazor/` use?**
   * Context: Current csproj references `Brinell.Core` + `Microsoft.Playwright` only. Placeholder namespaces are `Brinell.Blazor.{Context,Controls,Pages,Testing}`.
   * Proposed hypothesis: References become `Brinell.Html` + `Brinell.Html.Playwright` + `Microsoft.Playwright` (Core comes transitively). Keep existing namespace structure — it already mirrors `Brinell.Html.*`.

6. **What about the `BlazorTestContext` and page object base?**
   * Context: Old `BlazorTestContext` wraps `IPage` with timeouts, logging, screenshots. New pattern uses `IHtmlTestContext` / `PlaywrightTestContext`.
   * Proposed hypothesis: Create a `BlazorTestContext` in `srcnew/Brinell.Blazor/Context/` implementing `IHtmlTestContext`, modeled on `PlaywrightTestContext` from `Brinell.Html.Playwright`. Page object base follows `HtmlPageObjectBase<TSelf>` pattern.

## Agreed Scope

* **Include:**
  * `src/Brinell.Blazor/ControlObject6/` — all 22 controls, 3 interfaces, context, page base → `srcnew/Brinell.Blazor/`
  * `tests/Brinell.Blazor.Tests.ControlObject6/` — all 20 test files + mock factory → `testsnew/Brinell.Blazor.Tests/`
* **Exclude:**
  * Changes to `srcnew/Brinell.Html/`, `srcnew/Brinell.Html.Playwright/`, or `srcnew/Brinell.Core/` (stable reference projects)
  * `testsnew/Brinell.Blazor.UITests/` (not in scope for this task)
  * Sample projects under `samples/`
  * Framework-wide async migration (separate future task)

* **Constraints:**
  * Sync API only — match current `Brinell.Html` architecture exactly
  * No `Thread.Sleep` or arbitrary waits (per copilot-instructions.md)
  * No FluentAssertions — use xunit Assert
  * Everything through `IHtmlElement` abstraction — no Blazor-specific Playwright shortcuts
  * Constructor pattern: `(IHtmlScope<TScope> scope, Locator locator)` from Html
  * Fluent chaining: all action methods return `TScope`

## Priority Order

1. **Source Architecture** — Update `Brinell.Blazor.csproj` references, establish the dependency on `Brinell.Html` + `Brinell.Html.Playwright` *(unblocks everything else)*
2. **Context/Page** — Implement `BlazorTestContext` and `BlazorPageObjectBase` in `srcnew/Brinell.Blazor/Context/` and `Pages/` *(controls need a context to function)*
3. **Control Migration** — Implement all 22 controls: 17 as Html inheritors, 5 as Blazor-specific *(the core deliverable)*
4. **Test Migration** — Convert all 20 test files + mock factory to new patterns in `testsnew/Brinell.Blazor.Tests/` *(validates the implementation)*

## Assumptions

* `srcnew/Brinell.Blazor/` is greenfield — placeholder files will be replaced *(verified)*
* `testsnew/Brinell.Blazor.Tests/` is scaffolded but empty — ready for content *(verified)*
* The framework-wide async migration is future, not imminent — Blazor refactoring should not be blocked on it *(user confirmed "still exploring" on scope)*
* The `RunWithElement` pattern is the key async-readiness mechanism — no additional preparation needed *(verified from `ControlBase<TScope>` source)*

## Risks and Concerns

* **Async-to-sync bridging** — `PlaywrightHtmlElement` uses `.GetAwaiter().GetResult()` for all Playwright calls. This works but has theoretical deadlock risk in certain synchronization contexts. The current Html layer accepts this trade-off. Blazor should follow the same pattern, not try to solve it differently. The framework-wide async migration is the proper fix.

## Suggestions

* **Blazor-only controls should use Html base classes** — `AudioControl`, `VideoControl`, `IFrameControl`, `ImageControl`, `NavMenuControl` extend `Control<TScope>` or `ClickableControlBase<TScope>`, ensuring they participate in the same architecture and benefit from future async migration automatically.
* **Control mapping table** — 17 direct matches, 5 Blazor-only. See questions document for the complete mapping.
* **Async migration as separate research** — When the user is ready to explore async scope ("all layers" vs. "control API only" vs. "test-facing only"), create a separate task under `.copilot-tracking/Task/03_AsyncMigration/`. The recommended scope (agent opinion per user request): **all layers** — `IHtmlElement` gets async methods, `RunWithElement` becomes `RunWithElementAsync`, concrete controls follow mechanically. This gives the cleanest result and avoids a half-async state. But this deserves its own research brief.

## User Steering Notes

* User confirmed "sync-first, async-ready" as the sequencing strategy
* User asked "what would you think is best?" for async scope — answered in Suggestions above (all layers recommended, as separate research)
* Round 2 dual-API questions are superseded — no Blazor-specific async extensions needed
* Round 1/2 checked answers on project structure, constructor pattern, test mocking, and assertion conversion are all confirmed and consistent
