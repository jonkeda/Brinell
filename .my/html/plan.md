# Brinell.Html to the MAUI standard: plan

The work plan and its status. What to build is in [design.md](design.md); the Playwright-specific
deltas are in [differences-from-maui.md](differences-from-maui.md). Why the shape is what it is
lives in `../stale-readiness/`.

Status: **draft**, 2026-09-20; checked against MAUI's current state on 2026-09-26 (section 7).
Nothing built yet. Numbers under "baseline" are TODO.

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
  sample host. Same rule as MAUI. The same holds for the Todo web head.
- **Cover both backends.** Everything must build against `Brinell.Html.Playwright`. If Selenium
  or another driver is added, its adapter is a follow-up.

Status values: `todo`, `doing`, `done`, `blocked`.

| Step | Status | Contents | Done when |
| --- | --- | --- | --- |
| **0. Rename** | todo | Nothing on the same scale as MAUI's `ItemContainerBase` rename. `HtmlPageObjectBase.IsLoaded(int? timeoutMs)`, `GetTitle(int? timeoutMs)` and any other member with an ignored timeout lose it in step 1; the sync/async split (`IAsync*` shims) is addressed in step 1. If a rename is needed, it lands here | Empty diff after review, or the renames done with tests still passing |
| **1. Html stands alone** | todo | Html stops implementing Core's `IElement<>`, `IDriver<>`, `IElementScope<>`, `IPageObject<>`, `ITestContext<>`, `IContainerControl<>` and `IContainerObject<>`, and stops using `ControlObjectBase<>`. New or reshaped: `IHtmlElement`, `IHtmlDriver`, `IHtmlElementScope`, `IHtmlPage`, `IHtmlTestContext` (design 4.2). Html copies of the geometry and scope helpers. Finds make **one attempt**, and the timeout finds (`FindElement(locator, timeoutMs)`) are gone. `IsLoaded()`, `GetTitle()`, `TakeScreenshot(string?)` lose their unused timeouts. **UAT discovery kept:** `HtmlPageObjectBase` carries Core's `[TestPage]` once it stops implementing `IPageObject` (MAUI step 1 note). **Otherwise no behaviour change:** `ProbeReadiness` and the readiness chain arrive in step 5. Decision **?A1**: do the `IHtmlAsync*` decorative interfaces survive step 1, or move to a `Brinell.Html.Async` project, or go? See design section 4.6 | Solution builds; `Brinell.Html.Tests`, `Brinell.Html.Uat.Tests`, `Brinell.Blazor.Tests`, `Brinell.Blazor.Uat.Tests` green; the Html and Blazor UAT runs discover the same pages as before; `Brinell.Core` untouched (`git diff --stat srcnew/Brinell.Core` empty); grep finds no `IElement<`, `IElementScope<`, `IPageObject<`, `IDriver<`, `IContainerControl<` or `ControlObjectBase<` in `srcnew/Brinell.Html*` or `srcnew/Brinell.Blazor` |
| **T1. Todo web app** | done (2026-09-26, see 2.1) | The Todo app as a Blazor Server head, `samples/Todo/src/Brinell.Samples.Todo.Web`, on the shared `Core`, `Contracts` and `Infrastructure` ([todo-blazor.md](todo-blazor.md) section 2). `data-testid`s equal to the MAUI AutomationIds; `data-busy` from `IsBusy`; `data-item-key` on rows; `LaunchSettings` from the environment, so one host process is one device. An `X-Todo-Client` header, from a new `TODO_CLIENT_ID` launch setting, sent by the API client and recorded by `TodoServerStub` and `Todo.Api`. No Html UI tests yet (?T1) | The app builds and runs against `Todo.Api` by hand; Todo unit, integration and contract tests green; MAUI Todo UI suite unchanged (the header is additive) |
| **2. Pin it down** | doing: baseline done 2026-09-26 (section 4); tests todo | Failing unit tests that force each following step: (a) `Click` finds the element again (b) the caller's 300 ms budget is honoured (c) one log pair per call (d) `Get` / `WaitVisible` / `AssertText` re-resolve (e) an action that throws is not repeated (f) an attribute read below a call cannot outlast the call. **R0 guard tests:** a button that never re-enables → `Click` fails with "disabled" within its budget; a `Fill` that has no effect on the model → `NotConfirmed`, `Fill` `Times.Once`; a component re-rendered every 100 ms → the call passes **and** a near-miss `Warning` is logged; the Playwright browser closed mid-call → `AppUnavailableException` without waiting out the budget. **Baseline:** the current `Brinell.Html.UITests` and `Brinell.Blazor.UITests` suites run N times and its failures classified (framework race / app bug / environment / driver gap); a per-call count of `Locator.CountAsync` calls; the slowest 5 tests | Tests fail for the stated reason; baseline recorded in section 4 |
| **3. Stale signal** | todo | Html exceptions (design 4.3): `StaleElementException`, `ElementNotReadyException`, `ScopeNotReadyException`, `AppUnavailableException`. `InstanceKey`; a `Live<T>` wrapper in `PlaywrightTestContext` and `PlaywrightHtmlElement`; a browser-closed / context-closed / target-closed check that raises `AppUnavailableException` (design section 5). Every `catch { }` in `ObjectBase.Poll` and `PlaywrightHtmlElement.TryFindElement` gone. All catch-alls in the ControlBase layer removed. `HtmlPageObjectBase.WaitLoaded` throws `ScopeNotReadyException`, not Core's `PageLoadException`. `RouteUnavailableException` (MAUI review-2 fix D3): a permanent "no route" ends the call; a plain `NotSupportedException` is still retried | Mapping unit tests; grep finds no `catch\s*\(?` without a named type in `srcnew/Brinell.Html*` or `srcnew/Brinell.Blazor` (JSON config and other named ones stay); `PageLoadException` no longer raised by Html or Blazor code |
| **4. Calls** | todo | `Calls/*` (`ControlCall`, `Poller`, `Deadline`, `AttemptContext`, `Observation`, `ObservationLog`, `ScopeGate`, `Confirmer`), copied from MAUI's shape. `ViewBase` and `RootedScopeBase` moved onto the calls layer, replacing `ControlBase.RunPoll` and its bare `catch (Exception ex)`. `ObjectBase.Poll` deleted. `RunDoWithElement` **no longer retries the action**: phase 1 resolves and gates on readiness within the budget; phase 2 acts **once** (R0). One `LogEntry`/`LogExit` pair per public call. Near-miss `Warning` logging. `Assert*` resolves per attempt, like `Get*` (MAUI review-2 fix S1). `BRINELL_CALL_LOG`, and the context `Name` in the log line (?A10) | The step 2 pinned tests pass; one log pair per call; the near-miss test passes; `RunDoWithElement` shows `Invoke` `Times.Once` on an action that has no effect |
| **5. Scope readiness** | todo | `ScopeReadiness` and the `ProbeReadiness` chain; `AsksParent`; `ProbeContentReadiness`; `CanResolveElements` and `EnsureLoaded` (if present in Html) removed; `AppRoot` for the browser page/frame; the busy signal for a Blazor page is read from a well-known data attribute (design 7.2, sample-app.md item 3); `ScopeNotReadyException` replaces `PageLoadException` throughout Html; the frame model (`ForFrame`) becomes a scope wrapper. `AppRoot.WaitReady` is a call unit with its own budget (MAUI review-2 fix 8). **The UI test bases:** `BlazorSampleTestBase` (in both UI test projects) loses its `Thread.Yield`/`Task.Delay` loop and its `Context.FindElement`: the page's `WaitReady` covers "circuit up". The fixture starts the sample host on a free port, instead of expecting one at `BLAZOR_APP_URL` | Scope tests pass; a busy page defers the child call within its budget |
| **6. Confirm** | todo | `Confirm` replaces the "loop the whole call until the effect is seen" pattern that today lives inside `RunPoll`. The act-then-confirm Cores (`Click` on a busy-triggering button, `Fill`, `Check`/`Uncheck`, `SelectOption`, `Focus`/`Blur`) get a second phase that reads the effect once per attempt and never repeats the action | Replaced element: new message, action `Times.Once`; the R0 "no effect" test from step 2 passes |
| **7. Collections** | todo | `ItemKey` for Html rows (design 7.6, differences item 5: DOM node id, or `data-item-key`, or position). `MaterializeAttempts` for a Blazor `Virtualize` scroll. `ListControl` and `TableControl` move onto a `CollectionObjectBase<TParent, TSelf, TItem>` + `ItemObjectBase<TCollection, TSelf>`. `Item(index)` and `SelectItem` become waiting call units; `TryItem` / `TrySelectItem` answer now (R7) | Collection tests pass; the row-recycling test from sample-app.md item 4 fails when a row is asked to click after its key changed |
| **T2. Todo web tests** | todo | `tests/Brinell.Samples.Todo.Web.UITests`: hermetic and live fixtures (`TodoWebFixture` starts the host process on a free port with the test set's launch settings), Html page objects and ControlObjects (`TodoStatus`, `SectionCard`), and the TOD journeys with a `Head=Web` trait. The Todo `.uat.md` files run with `Target: Html` (?T2). `JourneyCoverage` learns the `Head` trait ([todo-blazor.md](todo-blazor.md) section 3) | Hermetic suite green in 5 of 5 runs; live tests green with `BRINELL_UAT_LIVE_API=1`; a `WaitForRequest` on `X-Todo-Client` passes; every UAT file that passes on MAUI passes on the web head, or carries a head tag and a reason |
| **8. Prove it** | todo | On a branch: (a) re-introduce the login-button `disabled`-forever bug (sample-app.md item 2) and prove the login test fails with "disabled for N ms" instead of timing out silently; (a2) re-introduce the Todo `CanExecuteChanged` bug in the shared view model and prove TOD.04.4 fails on **both** heads, naming what it saw; (b) re-introduce the "action retried" bug in `ControlBase` and prove a step-4 test catches it; (c) run `Brinell.Html.UITests`, `Brinell.Blazor.UITests` and the Todo web suite in full against a saved timing baseline; (d) run the repeat-run script against the step 2 baseline | R0 proofs both fail against their bugs, and name what they saw. No new failures against the timing baseline. Framework-race failures down, and none moved from "app bug" to "pass" |
| **9. Write it down** | todo | In `.docs/` (a decision is recorded once, there): widen AD-010 from "MAUI ahead of Core" to "MAUI and Html ahead of Core"; widen the call-model and control-object contracts from "the MAUI stack" to both stacks; close X2 with a note under AD-009. AD-004 and AD-009 are already stack-neutral, so no new AD mirrors them, and AD-011 and AD-012 are taken. `samples/Todo/README.md` gains the web head; the two new skills merged (`skills.md`); `AGENTS.md` and `.github/copilot-instructions.md` updated; `CHANGELOG.md` list; a note in `../stale-readiness/move-down.md` that the Html slice is done | Docs merged |

After step 9, and **not in this plan**: several apps in one scenario (MAUI and web against one
backend, multi-user). See [todo-blazor.md](todo-blazor.md) section 4. Steps 1-7 keep to the
constraints in its section 4.2, so that project stays possible.

### 2.1 Step notes

Filled in as steps finish, in the same shape as `../stale-readiness/plan.md` section 2.1: what
was proved, what the design was wrong about, and what was deferred to a later step on purpose.

**Step T1 (2026-09-26).** Done.

- **Evidence.**
  - `Brinell.Samples.Todo.Web` builds with 0 warnings.
  - The MAUI app (Windows), `Todo.UITests` and `Todo.Uat` still build.
  - Todo unit tests 114/114 and integration tests 43/43 pass. These include three new TOD.07.10
    tests: the header is sent when `TODO_CLIENT_ID` is set, and not sent otherwise (WireMock);
    the real API records which client sent each request (contract).
- **Run by hand**, in the browser pane, against `Todo.Api` (`.claude/launch.json` has `todo-api`,
  `todo-web` and `blazor-sample`). What was checked:
  - Sync on start, and TOD.02.3 ("Title is required", the page stays).
  - Create with a due date and a status step; the row is pending.
  - Sync: `data-busy` went `true` then `false`, the pending dot cleared, and the API held the
    todo.
  - Detail: Next saves at once ("Waiting to sync"). Edit, then Cancel on a dirty form asks
    "Discard changes?"; Keep editing keeps the edit, Discard goes back to detail.
  - Delete asks first; Cancel stays, Delete goes back to the list. After a sync the API is empty.
  - With the API stopped, sync shows "Server unreachable" and the list stays Empty. That is the
    shared rule: only a server error or a wrong key shows the Error state.
  - No server errors and no console errors.
- **`JourneyCoverage` is red, from before this step.** `a781b11` deleted
  `Todo.UITests.Mobile`, and the report still loads it by name. So TOD.07.10's I + C assignment
  is not checked by the report yet. A separate task has been proposed.
- **What the design was wrong about.**
  - `TodoServerStub` needed no change: WireMock already records every header.
  - `Todo.Api` got an in-memory `ServerRequestLog`, exposed as `RunningTodoApi.Requests`
    (in-process only; a deployed API has none).
  - The web head needs no `IConnectivityState` of its own. `AddTodoServices` without device
    connectivity already falls back to the network-state file, or to always online.
- **Shapes worth knowing for T2.**
  - Buttons are `CommandButton`s. They re-ask `CanExecute` only on `CanExecuteChanged`, as a MAUI
    Command binding does, so the TOD.04.4 R0 bug reproduces on this head.
  - `data-busy` is written by JS interop after the render in which the view model's `IsBusy`
    changed. It is not bound on `<body>` directly, because `<body>` is outside the interactive
    components.
  - Prerendering is off, because the list view model is a singleton.
  - The alert is a modal with ids `Dialog`, `Dialog_Title`, `Dialog_Message`, `Dialog_Accept`
    and `Dialog_Cancel`.
  - The due-date switch is `<input type="checkbox" role="switch">`, and the filter is a
    `<select>`.

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
| ?A9 | Html's place in the move-down order (after NativeAndroid, WPF, WinForms) | Html goes first. It has the most to gain (X2), and the Todo web head gives it an app like MAUI's to prove the model on. Nothing in NativeAndroid, WPF or WinForms depends on Html |
| ?A10 | A name on every test context, for logs and failure messages | Yes: `HtmlTestContextOptions.Name`, defaulting to the host name. Several apps in one scenario need it ([todo-blazor.md](todo-blazor.md) 4.2) |
| ?T1-?T4 | The Todo web app | See [todo-blazor.md](todo-blazor.md) section 5 |
| ?A8 | `BusySignalPolicy` for Blazor: the `data-busy` attribute on `<body>`, or the SignalR client state, or a `<div class="loading">` present-check | The attribute, injected once in the sample host by a small JS file. See sample-app.md item 3 |

## 4. Baselines

Recorded 2026-09-26, before any framework change, at `50fcce9`. Not edited after this; a new
baseline is a new file. Playwright 1.62 (Chromium 1234), headless; the gallery sample
(`Brinell.Samples.Blazor.App`) running at `http://localhost:5180` from `.claude/launch.json`.
The repeat runs used `tools/Scripts/repeat-run.ps1` (`-AllowProcess Brinell.Samples.Blazor.App`).
Their summaries are in [baseline/](baseline/).

**Unit and UAT:**

| Project | Result |
| --- | --- |
| `Brinell.Html.Tests` | **no tests.** One `GlobalUsings.cs`, `IsTestProject=false`. Step 2's pinned tests need a real project; this is where they go |
| `Brinell.Blazor.Tests` | 172 passed / 0 failed. This is where the Html framework's unit tests live today |
| `Brinell.Html.Uat.Tests` | 2 passed / 0 failed |
| `Brinell.Blazor.Uat.Tests` | 2 passed / 0 failed |
| `Brinell.Core.Tests` | 16 passed / 0 failed |

**UI, 5 runs each:**

| Suite | Tests | Time per run | Failures |
| --- | ---: | ---: | --- |
| `Brinell.Html.UITests` | 36 | ~15 s | `LoginFlow_EmptyCredentials_ShowsError` 5/5, `..._Async` 4/5 |
| `Brinell.Blazor.UITests` | 31 | ~12 s | `Button_MultipleClicks_AllRegister`, `Counter_CountDisplay_UpdatesAfterClick`, `Counter_MultipleIncrements_AccumulatesCorrectly`, each 5/5 |

Each failure's class, decided by reading it:

- **Counter and button, framework race (readiness).** Exactly one click is lost every time: count
  2 after 3 clicks, 0 after Reset + Increment. The sample prerenders (`InteractiveServer` with the
  default prerender). `BlazorSampleTestBase` counts "`window.Blazor` is defined" as ready, which is
  true before the circuit is connected, so the first click lands on dead prerendered HTML. Steps 5
  and 6 must make these pass by waiting for the circuit, not by retrying the click (R0).
- **Login with empty credentials, framework race (a one-shot read) in the test.** The test reads
  `Context.FindElements(".text-danger")` straight after `Click`, with no wait, so it races the
  render: 5/5 sync, 4/5 async. It is also a forbidden API in a test. Step 2 rewrites it through
  the page object.
- App bugs: none. Environment: none (after installing Chromium 1234; before that, both UAT
  projects failed with "Executable doesn't exist"). Driver gaps: none.

The 5 slowest tests: the login tests, about 2.2-2.5 s each (mean). Everything in
`Brinell.Blazor.UITests` is under 0.7 s. The tables are in [baseline/](baseline/).

**Static counts** (grep, `srcnew`):

| Project | Uses of Core interfaces Html leaves (step 1 list, plus `WaitHelper` and `PageLoadException`) | Catch-alls (`catch` / `catch (Exception)`) | Files |
| --- | ---: | ---: | ---: |
| `Brinell.Html` | 16 | 5 (`ControlBase.RunPoll` 1, `ObjectBase` 4) | 49 |
| `Brinell.Html.Playwright` | 1 | 4 (`PlaywrightTestContext`) | 3 |
| `Brinell.Blazor` | 1 | 0 | 25 |

**Not measured:** Playwright calls per completed `Click`. That needs a counting driver, which
does not exist yet. Step 2 adds it as a fake `IHtmlElement` in the pinned unit tests, and the
number is taken there, on the old code, before step 4 replaces `RunDoWithElement`.

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
dotnet test testsnew\Brinell.Blazor.UITests\Brinell.Blazor.UITests.csproj -v:minimal /nr:false
```

The Todo web head (steps T1 and T2), from `samples/Todo`:

```powershell
dotnet build src\Brinell.Samples.Todo.Web -v:minimal /nr:false
dotnet test tests\Brinell.Samples.Todo.Web.UITests -v:minimal /nr:false
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
- Blazor WebAssembly. Out. The sample and the Todo web head are Blazor Server (interactive server
  rendering).
- Several apps in one scenario, and multi-user identity in `Todo.Api`. These are a later project
  ([todo-blazor.md](todo-blazor.md) section 4). This plan adds only `X-Todo-Client` and the
  context name.

## 7. Checked against MAUI, 2026-09-26

What changed on the MAUI side after this draft (2026-09-20), and where the plan now takes it in:

| MAUI now | In this draft | Now |
| --- | --- | --- |
| Review-2 fixes (`../stale-readiness/review-2-fixes.md`): `Assert*` resolves per attempt (S1); `RouteUnavailableException` is fatal (D3); `AppRoot.WaitReady` is a call unit; "`timeoutMs` bounds the poll, not the call" is in the skill | Missing | Steps 3, 4 and 5; the skill note in [skills.md](skills.md) |
| Review-2's lesson: two defaults from design section 11 were never revisited, and both cost a property the work exists to give | ?A1-?A8 carry defaults | Step 4 re-reads every ?A default before its tests count as green |
| `.docs/` exists: AD-001 to AD-012, invariants and contracts. A decision is recorded once, there | Step 9 planned "AD-011 mirroring AD-004" | AD-011 and AD-012 are taken. Step 9 widens AD-010 and the contracts instead |
| UAT finds pages by `[TestPage]` or `IPageObject` (MAUI step 1 note); AD-011 declares the vocabulary with attributes | Not mentioned | Step 1 keeps discovery with `[TestPage]`, and its Done-when check compares the discovered pages |
| AD-012 test data, `Brinell.TestData`, typed mock bodies (`BRINELL001`) | No test data in the Html sample | The Todo web head uses the Todo test sets unchanged (T1, T2) |
| `BRINELL_CALL_LOG` in `MauiTestContext` | Not mentioned | Step 4 |
| The Todo app is the proving ground (TOD.04.4 is the R0 proof) | Only the control-gallery sample | Steps T1, T2 and 8 (a2) |
| AD-005: no physical input for MAUI on Windows | Not mentioned | Html complies already: Playwright sends input over the browser protocol, with no OS input and no foreground window. [differences-from-maui.md](differences-from-maui.md) item 9 |

Wrong in this draft, found in the Html code while checking:

- `Brinell.Html.UITests` does **not** launch the sample. `BlazorSampleTestBase` expects a host
  already running at `BLAZOR_APP_URL` (default `http://localhost:5180`). It waits for the
  circuit with a hand-written loop over `Context.FindElement`, which breaks AD-004 and the
  forbidden-API rule. Step 5 fixes it.
- There are two Blazor UI test projects, `Brinell.Html.UITests` and `Brinell.Blazor.UITests`,
  with the same test base. Steps 2, 5 and 8 now cover both.
