# Stale readiness: plan

The work plan and its status. What to build is in [design.md](design.md). Why it is built that way
is in `background/` (see [README.md](README.md)).

Status: **in progress**, 2026-09-19. The design is accepted (section 3).

## 1. Goal

A test's call on any MAUI object (control, page, container, collection or row) is one unit of
work:

- it spends one budget;
- it waits for its scope chain;
- it finds its element again on every attempt;
- it recognizes a replaced element on every platform;
- it never repeats an action;
- when it fails, it says what it last saw.

**App bugs stay failures** (design R0). **MAUI is made right first, on its own interfaces; Core is
not changed.** The proven shape moves down to Core later, as its own project (design 4.5).

This started from the Todo edit page's toolbar Cancel, which failed one full UI run in two.

## 2. Steps

Working rules:

- **One UI test process at a time.** Two runs at once fight over the desktop.
- **The smallest test tier that can falsify each step.** The full suites run only in step 8.
- **Restart Appium after any killed run.**
- **Android starts from a failing baseline** (Range 6/16, Picker 0/8). Compare against it; do not
  expect green.

Status values: `todo`, `doing`, `done`, `blocked`.

| Step | Status | Contents | Done when |
| --- | --- | --- | --- |
| **0. Rename** | done | `ItemContainerBase` → `ItemObjectBase`, `IMauiItemContainer` → `IMauiItemObject`; tests, Todo sample, skills (`maui-control`, `maui-ui-test`) | Builds; `Brinell.Maui.Tests` green; no behaviour change |
| **1. MAUI stands alone** | todo | MAUI stops implementing Core's `IElement`, `IDriver`, `IElementScope`, `IPageObject`, `ITestContext<T>`, `IContainerControl` and `IContainerObject`, and stops using `ControlObjectBase`. New or reshaped: `IMauiElement`, `IMauiDriver`, `IMauiElementScope`, `IMauiPage`, `IMauiTestContext` (design 4.2). MAUI copies of the geometry and scope helpers. Finds make one attempt, and the timeout finds are gone (F7 goes with them). `IsLoaded()`, `GetTitle()` and `TakeScreenshot(string?)` lose their unused timeouts. **Otherwise no behaviour change:** `ProbeReadiness` and the readiness chain come in step 5 | Solution builds; `Brinell.Maui.Tests`, `Brinell.Maui.Uat.Tests` (MAUI pages still discovered) and every other stack's tests green; `Brinell.Core` untouched (`git diff --stat srcnew/Brinell.Core` empty); no `IElement<`, `IElementScope`, `IPageObject`, `IDriver<` or `ControlObjectBase` found in `srcnew/Brinell.Maui*` |
| **2. Pin it down** | todo | **Failing unit tests:** original plan (a)-(e), trace (f)-(h), scopes (dialog on a busy page, content readiness reaching a child, row re-resolves to another item, `ScrollToItem` budget, `Item(index)` waits). **R0 guard tests:** a command that never re-enables → `Click` fails with "disabled" within its budget; an action with no effect → `NotConfirmed`, action `Times.Once`; an element re-rendered every 100 ms → the call passes **and** a near-miss `Warning` is logged; the app exiting mid-call → `AppUnavailableException` without waiting out the budget. **Baseline:** a repeat-run script (a suite run N times, each failure classified as framework race, app bug, environment or driver gap); a `FindElement` and an alive check measured per call; the collection UI tests that take more than 5 s; full-suite timing | Tests fail for the stated reason; baseline recorded in section 4 |
| **3. Stale signal** | todo | MAUI exceptions (design 4.3), with `AppUnavailableException` raised by both drivers. `InstanceKey`; `Live` in both drivers; `MauiTestContext.TryFindElement` narrowed; `WithRoot`; the alive rule (R6); every Selenium catch switched; catch-alls removed | Mapping unit tests; no Selenium type found in `srcnew/Brinell.Maui` |
| **4. Calls** | todo | `Calls/*` (design 6), including near-miss logging through `LogExit(Warning)`. `ViewBase` and `RootedScopeBase` moved onto the calls layer. `ObjectBase.Poll` removed. The `MauiTestContext.FindElement` loop removed. `Menu.OpenCore` | The step 2 plan and trace tests pass; one log pair per call; the near-miss test passes |
| **5. Scope readiness** | todo | `ScopeReadiness` and the `ProbeReadiness` chain; `AsksParent`; `ProbeContentReadiness`; `CanResolveElements` removed; `AppRoot` without sweep; `DriverRootScope` removed; page and container public members as call units; `ScopeNotReadyException` replaces `PageLoadException` (4 tests) | Scope tests pass |
| **6. Confirm** | todo | `Confirm` replaces `Until`; the 8 act-then-confirm Cores; `EnsureClickableCore` calls removed | Replaced element: new message, action `Times.Once`; the R0 "no effect" test passes |
| **7. Collections** | todo | `ItemKey`; `MaterializeAttempts`; the members table in design 7.6 | Collection tests pass; the long-list tests from step 2 given explicit budgets where needed |
| **8. Prove it** | todo | On a branch, remove the `ToolbarButton` override and run Todo 5×. **R0 proof:** re-introduce the Todo `CanExecuteChanged` bug on a branch and run TOD.04.4. Run Windows `Brinell.Maui.UITests` in full against `timing-baseline.json`. Run the repeat-run script against the step 2 baseline. Run the Android baseline subset. Restore the override and the fix | Todo 5/5. **TOD.04.4 still fails against the bug, and names what it saw.** No new Windows failures, and no slowdowns beyond the threshold. Framework-race failures down, and none moved from "app bug" to "pass". Near-misses listed. Android no worse than baseline |
| **9. Write it down** | todo | AD-004 ("one unit per call; only the call's poll waits; a call waits for its scope chain"); a new decision record for R0; a decision record for R9 (MAUI ahead of Core, on purpose); both skills; `CHANGELOG.md` (design section 10 list); a first draft of the "move down" plan (design 4.5) | Docs merged |

## 3. Decisions taken

| Date | Decision | Source |
| --- | --- | --- |
| 2026-09-19 | Q1-Q10 as proposed: scope readiness chain, "loaded" merged into readiness, `AppRoot` the only whole-app scope, one alive rule, object-lifetime root cache, `ItemKey`, scope members as call units, one budget for materializing, non-`Try` waits | [scopes-analysis.md](background/scopes-analysis.md) section 6 |
| 2026-09-19 | Rename `ItemContainerBase` → `ItemObjectBase` (and the interface) | user |
| 2026-09-19 | After the review: keep everything marked "defer", and keep the full diagnostics. Only the two "drop" items go (`Deadline` internal, no `Carry`) | [review.md](background/review.md), outcome |
| 2026-09-19 | R0: app bugs stay failures; near-misses are reported; `AppUnavailableException` fails at once | user; design 2.1 |
| 2026-09-19 | **No bridges on Core interfaces.** MAUI gets its own interfaces now, Core is not changed, and the proven shape moves down later, one stack at a time. This replaces the earlier "Core base interfaces with bridges" decision | user; design R9, 4 |
| 2026-09-18 | Keep `ToolbarButton.RequiresVisibilityForAction => false` (S4) | [original-plan.md](background/original-plan.md) |

Still open: design section 11 (S1, D3, X1, X2, X5, Q6, Q9). Most are settled by step 2's
numbers.

## 4. Baseline and measurements

Filled in during step 2 and compared in step 8.

| Measure | Value | Date | Notes |
| --- | --- | --- | --- |
| Todo UI run failure rate (toolbar Cancel) | ~1 in 2 before the `ToolbarButton` fix | 2026-09-18 | [original-plan.md](background/original-plan.md) section 1 |
| Windows `Brinell.Maui.UITests` Range | 23/23 green | 2026-09-18 | |
| Android Range / Picker | 6/16 fail / 0/8 pass | 2026-09-18 | driver gaps |
| `FindElement` cost per call (Windows / Android) | | | step 2 |
| Alive-check cost per call | | | step 2 (decides Q6) |
| Collection UI tests taking more than 5 s | | | step 2 (decides Q9) |
| Repeat-run failure classes (race / app bug / environment / driver gap) | | | step 2 |
| Full-suite timing | | | step 2 |
