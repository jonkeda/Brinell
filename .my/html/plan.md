# Brinell.Html to the MAUI standard: plan

The work plan and its status. What to build is in [design.md](design.md); the Playwright-specific
deltas are in [differences-from-maui.md](differences-from-maui.md). Why the shape is what it is
lives in `../stale-readiness/`.

Status: **draft**, 2026-09-20. Nothing built yet. Numbers under "baseline" are TODO.

## 1. Goal

A test's call on any Html object (control, page, container or list) is one unit of work:

- it spends one budget;
- it waits for its scope chain, one link at a time;
- it re-resolves its element on every attempt;
- it recognizes a detached or replaced element;
- it never repeats an action once it has run;
- when it fails, it says what it last saw;
- **app bugs stay failures** (R0). No `catch { }` swallows a defect.

**Html is made right first on its own interfaces; Core is not changed** (R9).

The concrete bug this project must not let slip: the same shape as MAUI's Todo toolbar Cancel -
a command that never re-enables, seen as a "flaky" test because `RunDoWithElement`'s
`catch (Exception ex)` retried the whole `Click` until either the button re-enabled or the
budget ran out. `RunDoWithElement` retries actions today (see [design.md](design.md) section 2,
X2).

## 2. Steps

Working rules:

- **One UI test process at a time.** Playwright launches Chromium; two runs at once fight over
  file locks and the sample host port.
- **The smallest test tier that can falsify each step.** The full `Brinell.Html.UITests` suite
  runs only in step 8.
- **Rebuild the Blazor sample when it changes.** `dotnet test` builds the test project, not the
  sample host. Same rule as MAUI.
- **Cover both backends.** Everything must build against `Brinell.Html.Playwright`. If Selenium
  or another driver is added, its adapter is a follow-up.

Status values: `todo`, `doing`, `done`, `blocked`.

| Step | Status | Contents | Done when |
| --- | --- | --- | --- |
| **0. Rename** | todo | Nothing on the same scale as MAUI's `ItemContainerBase` rename. `HtmlPageObjectBase.IsLoaded(int? timeoutMs)`, `GetTitle(int? timeoutMs)` and any other member with an ignored timeout lose it in step 1; the sync/async split (`IAsync*` shims) is addressed in step 1. If a rename is needed, it lands here | Empty diff after review, or the renames done with tests still passing |
| **1. Html stands alone** | todo | Html stops implementing Core's `IElement<>`, `IDriver<>`, `IElementScope<>`, `IPageObject<>`, `ITestContext<>`, `IContainerControl<>` and `IContainerObject<>`, and stops using `ControlObjectBase<>`. New or reshaped: `IHtmlElement`, `IHtmlDriver`, `IHtmlElementScope`, `IHtmlPage`, `IHtmlTestContext` (design 4.2). Html copies of the geometry and scope helpers. Finds make **one attempt**, and the timeout finds (`FindElement(locator, timeoutMs)`) are gone. `IsLoaded()`, `GetTitle()`, `TakeScreenshot(string?)` lose their unused timeouts. **Otherwise no behaviour change:** `ProbeReadiness` and the readiness chain arrive in step 5. Decision **?A1**: do the `IHtmlAsync*` decorative interfaces survive step 1, or move to a `Brinell.Html.Async` project, or go? See design section 4.6 | Solution builds; `Brinell.Html.Tests`, `Brinell.Html.Uat.Tests`, `Brinell.Blazor.Tests`, `Brinell.Blazor.Uat.Tests` green; `Brinell.Core` untouched (`git diff --stat srcnew/Brinell.Core` empty); grep finds no `IElement<`, `IElementScope<`, `IPageObject<`, `IDriver<`, `IContainerControl<` or `ControlObjectBase<` in `srcnew/Brinell.Html*` or `srcnew/Brinell.Blazor` |
| **2. Pin it down** | todo | Failing unit tests that force each following step: (a) `Click` finds the element again (b) the caller's 300 ms budget is honoured (c) one log pair per call (d) `Get` / `WaitVisible` / `AssertText` re-resolve (e) an action that throws is not repeated (f) an attribute read below a call cannot outlast the call. **R0 guard tests:** a button that never re-enables → `Click` fails with "disabled" within its budget; a `Fill` that has no effect on the model → `NotConfirmed`, `Fill` `Times.Once`; a component re-rendered every 100 ms → the call passes **and** a near-miss `Warning` is logged; the Playwright browser closed mid-call → `AppUnavailableException` without waiting out the budget. **Baseline:** the current `Brinell.Html.UITests` suite run N times and its failures classified (framework race / app bug / environment / driver gap); a per-call count of `Locator.CountAsync` calls; the slowest 5 tests | Tests fail for the stated reason; baseline recorded in section 4 |
| **3. Stale signal** | todo | Html exceptions (design 4.3): `StaleElementException`, `ElementNotReadyException`, `ScopeNotReadyException`, `AppUnavailableException`. `InstanceKey`; a `Live<T>` wrapper in `PlaywrightTestContext` and `PlaywrightHtmlElement`; a browser-closed / context-closed / target-closed check that raises `AppUnavailableException` (design section 5). Every `catch { }` in `ObjectBase.Poll` and `PlaywrightHtmlElement.TryFindElement` gone. All catch-alls in the ControlBase layer removed. `HtmlPageObjectBase.WaitLoaded` throws `ScopeNotReadyException`, not Core's `PageLoadException` | Mapping unit tests; grep finds no `catch\s*\(?` without a named type in `srcnew/Brinell.Html*` or `srcnew/Brinell.Blazor` (JSON config and other named ones stay); `PageLoadException` no longer raised by Html or Blazor code |
| **4. Calls** | todo | `Calls/*` (`ControlCall`, `Poller`, `Deadline`, `AttemptContext`, `Observation`, `ObservationLog`, `ScopeGate`, `Confirmer`), copied from MAUI's shape. `ViewBase` and `RootedScopeBase` moved onto the calls layer, replacing `ControlBase.RunPoll` and its bare `catch (Exception ex)`. `ObjectBase.Poll` deleted. `RunDoWithElement` **no longer retries the action**: phase 1 resolves and gates on readiness within the budget; phase 2 acts **once** (R0). One `LogEntry`/`LogExit` pair per public call. Near-miss `Warning` logging | The step 2 pinned tests pass; one log pair per call; the near-miss test passes; `RunDoWithElement` shows `Invoke` `Times.Once` on an action that has no effect |
| **5. Scope readiness** | todo | `ScopeReadiness` and the `ProbeReadiness` chain; `AsksParent`; `ProbeContentReadiness`; `CanResolveElements` and `EnsureLoaded` (if present in Html) removed; `AppRoot` for the browser page/frame; the busy signal for a Blazor page is read from a well-known data attribute (design 7.2, sample-app.md item 3); `ScopeNotReadyException` replaces `PageLoadException` throughout Html; the frame model (`ForFrame`) becomes a scope wrapper | Scope tests pass; a busy page defers the child call within its budget |
| **6. Confirm** | todo | `Confirm` replaces the "loop the whole call until the effect is seen" pattern that today lives inside `RunPoll`. The act-then-confirm Cores (`Click` on a busy-triggering button, `Fill`, `Check`/`Uncheck`, `SelectOption`, `Focus`/`Blur`) get a second phase that reads the effect once per attempt and never repeats the action | Replaced element: new message, action `Times.Once`; the R0 "no effect" test from step 2 passes |
| **7. Collections** | todo | `ItemKey` for Html rows (design 7.6, differences item 5: DOM node id, or `data-item-key`, or position). `MaterializeAttempts` for a Blazor `Virtualize` scroll. `ListControl` and `TableControl` move onto a `CollectionObjectBase<TParent, TSelf, TItem>` + `ItemObjectBase<TCollection, TSelf>`. `Item(index)` and `SelectItem` become waiting call units; `TryItem` / `TrySelectItem` answer now (R7) | Collection tests pass; the row-recycling test from sample-app.md item 4 fails when a row is asked to click after its key changed |
| **8. Prove it** | todo | On a branch: (a) re-introduce the login-button `disabled`-forever bug (sample-app.md item 2) and prove the login test fails with "disabled for N ms" instead of timing out silently; (b) re-introduce the "action retried" bug in `ControlBase` and prove a step-4 test catches it; (c) run `Brinell.Html.UITests` in full against a saved timing baseline; (d) run the repeat-run script against the step 2 baseline | R0 proofs both fail against their bugs, and name what they saw. No new failures against the timing baseline. Framework-race failures down, and none moved from "app bug" to "pass" |
| **9. Write it down** | todo | AD-011 (or the next free number) mirroring MAUI's AD-004 for Html; a decision record for X2 being closed; the two new skills merged (`skills.md`); `AGENTS.md` and `.github/copilot-instructions.md` updated; `CHANGELOG.md` list; a note in `../stale-readiness/move-down.md` that the Html slice is done | Docs merged |

### 2.1 Step notes

Filled in as steps finish, in the same shape as `../stale-readiness/plan.md` section 2.1: what
was proved, what the design was wrong about, and what was deferred to a later step on purpose.

## 3. Decisions taken and open questions

Deferred until enough is known. Kept here so the same questions do not get re-asked in review.

| # | Question | Default (revisit when) |
| --- | --- | --- |
| ?A1 | The `IHtmlAsync*` shim (interfaces that wrap sync methods in `Task.FromResult`) - keep, move to a separate project, or drop | Keep in step 1; revisit in step 4 with the near-miss numbers. If no test uses them beyond `IsExistsAsync` and friends, drop in step 9 |
| ?A2 | `IHtmlDriver` - a new abstraction, or does `IHtmlTestContext` play both roles as today | Add `IHtmlDriver` in step 1: MAUI's split is what lets `AppRoot` and `MauiTestContext` become different objects (design section 4.2), and Html has the same need for a frame model and a "browser is gone" check |
| ?A3 | Which Blazor sample changes ship with which step | See [sample-app.md](sample-app.md); the toolbar-cancel analog and the busy signal ship with step 2, the recycling list with step 7, the replaced-root component with step 6 |
| ?A4 | `InstanceKey` for Playwright: element handle identity via `EvaluateHandle`, a synthesized attribute (`data-brinell-key`), or the DOM node id via `element.tagName + generation counter` | Element handle identity by default; see [differences-from-maui.md](differences-from-maui.md) item 2 for the argument |
| ?A5 | Whether Playwright's own retry loops (`Locator.WaitForAsync`, auto-waiting on actions) are used at all inside the calls layer | **No.** Every wait inside a call comes from the caller's budget through `AttemptContext.CallRemainingMs`, so a Playwright `Timeout` never outlasts the call. Direct Playwright calls in element methods pass `Timeout = 0` (no wait). Revisit if step 8 shows this hurts throughput |
| ?A6 | Frame handling: `ForFrame` returns a whole new context today. Does a frame become a scope (a `FrameScope : IHtmlElementScope`) instead, so a frame stays in the same context and can be a scope in the chain | Design: yes, `FrameScope`. Revisit if any test today depends on `ForFrame` returning a disposable context |
| ?A7 | The heuristic in `ControlBase.IsRawSelector` (`starts with #`, `.`, `[`, contains `>` etc.) | Keep in step 1; consider replacing with explicit `Locator.ByCss` / `Locator.ByAutomationId` at call sites in step 9. Not a rule change |
| ?A8 | `BusySignalPolicy` for Blazor: the `data-busy` attribute on `<body>`, or the SignalR client state, or a `<div class="loading">` present-check | The attribute, injected once in the sample host by a small JS file. See sample-app.md item 3 |

## 4. Baselines

TODO. Recorded before step 2 starts and never edited after that (a new baseline is a new file).

- `Brinell.Html.Tests`: N passed / N failed.
- `Brinell.Html.Uat.Tests`: N passed / N failed.
- `Brinell.Html.UITests`, 5 runs: pass/fail counts and each failure's class (framework race,
  app bug, environment, driver gap).
- The 5 slowest UI tests, in seconds.
- `Locator.CountAsync` and `IsVisibleAsync` calls per completed `Click`, averaged over the suite.

## 5. Verification, per step

Commands are from the Brinell root.

```powershell
dotnet build srcnew\Brinell.sln -v:minimal /nr:false
dotnet test testsnew\Brinell.Core.Tests\Brinell.Core.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Html.Tests\Brinell.Html.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Blazor.Tests\Brinell.Blazor.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Html.Uat.Tests\Brinell.Html.Uat.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Blazor.Uat.Tests\Brinell.Blazor.Uat.Tests.csproj -v:minimal /nr:false
```

The UI suite (step 8 only):

```powershell
dotnet test testsnew\Brinell.Html.UITests\Brinell.Html.UITests.csproj -v:minimal /nr:false
```

**Match the test scope to the change**, same rule as `AGENTS.md`:

- One area first (`--filter "FullyQualifiedName~Tests.Controls.ButtonControlTests"`), then related
  areas, then the full suite. The full suite is only for step 8 and to close a phase of work.

**Rebuild the Blazor sample when it changes.** `Brinell.Samples.Blazor.App` is launched by the
`Brinell.Html.UITests` fixture; a stale sample passes or fails for reasons that no longer exist.

## 6. Not in scope

- Core changes. Done as part of the move-down, later, when Html and the other stacks are all
  standing on their own interfaces (stale-readiness design 4.5).
- Selenium adapter. `Brinell.Html.Playwright` is the only backend today; a Selenium adapter is
  a follow-up that inherits this project's element and driver interfaces.
- Blazor Server SignalR reconnection semantics. Out; the sample keeps the default behaviour.
- Blazor WebAssembly. Out. The sample is Blazor Server (interactive server rendering).
