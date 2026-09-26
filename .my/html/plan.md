# Brinell.Html to the MAUI standard: plan

The work plan and its status. What to build is in [design.md](design.md); the Playwright-specific
deltas are in [differences-from-maui.md](differences-from-maui.md). Why the shape is what it is
lives in `../stale-readiness/`.

Status: **done**, 2026-09-26. Steps 0-9 and T1-T2 are done; the step notes are in 2.1 and the
baseline in section 4. The draft was checked against MAUI's current state on 2026-09-26
(section 7). Deferred on purpose at the time, and done since (2026-09-26, follow-ups 7 and 8 of
[review-html-vs-maui.md](review-html-vs-maui.md)): the Todo `.uat.md` files on the web head
(step T2), and several apps in one scenario ([todo-blazor.md](todo-blazor.md) section 4).

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
| **0. Rename** | done (2026-09-26): nothing to rename; the timeouts went in step 1 | Nothing on the same scale as MAUI's `ItemContainerBase` rename. `HtmlPageObjectBase.IsLoaded(int? timeoutMs)`, `GetTitle(int? timeoutMs)` and any other member with an ignored timeout lose it in step 1; the sync/async split (`IAsync*` shims) is addressed in step 1. If a rename is needed, it lands here | Empty diff after review, or the renames done with tests still passing |
| **1. Html stands alone** | done (2026-09-26, see 2.1) | Html stops implementing Core's `IElement<>`, `IDriver<>`, `IElementScope<>`, `IPageObject<>`, `ITestContext<>`, `IContainerControl<>` and `IContainerObject<>`, and stops using `ControlObjectBase<>`. New or reshaped: `IHtmlElement`, `IHtmlDriver`, `IHtmlElementScope`, `IHtmlPage`, `IHtmlTestContext` (design 4.2). Html copies of the geometry and scope helpers. Finds make **one attempt**, and the timeout finds (`FindElement(locator, timeoutMs)`) are gone. `IsLoaded()`, `GetTitle()`, `TakeScreenshot(string?)` lose their unused timeouts. **UAT discovery kept:** `HtmlPageObjectBase` carries Core's `[TestPage]` once it stops implementing `IPageObject` (MAUI step 1 note). **Otherwise no behaviour change:** `ProbeReadiness` and the readiness chain arrive in step 5. Decision **?A1**: do the `IHtmlAsync*` decorative interfaces survive step 1, or move to a `Brinell.Html.Async` project, or go? See design section 4.6 | Solution builds; `Brinell.Html.Tests`, `Brinell.Html.Uat.Tests`, `Brinell.Blazor.Tests`, `Brinell.Blazor.Uat.Tests` green; the Html and Blazor UAT runs discover the same pages as before; `Brinell.Core` untouched (`git diff --stat srcnew/Brinell.Core` empty); grep finds no `IElement<`, `IElementScope<`, `IPageObject<`, `IDriver<`, `IContainerControl<` or `ControlObjectBase<` in `srcnew/Brinell.Html*` or `srcnew/Brinell.Blazor` |
| **T1. Todo web app** | done (2026-09-26, see 2.1) | The Todo app as a Blazor Server head, `samples/Todo/src/Brinell.Samples.Todo.Web`, on the shared `Core`, `Contracts` and `Infrastructure` ([todo-blazor.md](todo-blazor.md) section 2). `data-testid`s equal to the MAUI AutomationIds; `data-busy` from `IsBusy`; `data-item-key` on rows; `LaunchSettings` from the environment, so one host process is one device. An `X-Todo-Client` header, from a new `TODO_CLIENT_ID` launch setting, sent by the API client and recorded by `TodoServerStub` and `Todo.Api`. No Html UI tests yet (?T1) | The app builds and runs against `Todo.Api` by hand; Todo unit, integration and contract tests green; MAUI Todo UI suite unchanged (the header is additive) |
| **2. Pin it down** | done (2026-09-26, see 2.1) | Failing unit tests that force each following step: (a) `Click` finds the element again (b) the caller's 300 ms budget is honoured (c) one log pair per call (d) `Get` / `WaitVisible` / `AssertText` re-resolve (e) an action that throws is not repeated (f) an attribute read below a call cannot outlast the call. **R0 guard tests:** a button that never re-enables → `Click` fails with "disabled" within its budget; a `Fill` that has no effect on the model → `NotConfirmed`, `Fill` `Times.Once`; a component re-rendered every 100 ms → the call passes **and** a near-miss `Warning` is logged; the Playwright browser closed mid-call → `AppUnavailableException` without waiting out the budget. **Baseline:** the current `Brinell.Html.UITests` and `Brinell.Blazor.UITests` suites run N times and its failures classified (framework race / app bug / environment / driver gap); a per-call count of `Locator.CountAsync` calls; the slowest 5 tests | Tests fail for the stated reason; baseline recorded in section 4 |
| **3. Stale signal** | done (2026-09-26, see 2.1) | Html exceptions (design 4.3): `StaleElementException`, `ElementNotReadyException`, `ScopeNotReadyException`, `AppUnavailableException`. `InstanceKey`; a `Live<T>` wrapper in `PlaywrightTestContext` and `PlaywrightHtmlElement`; a browser-closed / context-closed / target-closed check that raises `AppUnavailableException` (design section 5). Every `catch { }` in `ObjectBase.Poll` and `PlaywrightHtmlElement.TryFindElement` gone. All catch-alls in the ControlBase layer removed. `HtmlPageObjectBase.WaitLoaded` throws `ScopeNotReadyException`, not Core's `PageLoadException`. `RouteUnavailableException` (MAUI review-2 fix D3): a permanent "no route" ends the call; a plain `NotSupportedException` is still retried | Mapping unit tests; grep finds no `catch\s*\(?` without a named type in `srcnew/Brinell.Html*` or `srcnew/Brinell.Blazor` (JSON config and other named ones stay); `PageLoadException` no longer raised by Html or Blazor code |
| **4. Calls** | done (2026-09-26, see 2.1) | `Calls/*` (`ControlCall`, `Poller`, `Deadline`, `AttemptContext`, `Observation`, `ObservationLog`, `ScopeGate`, `Confirmer`), copied from MAUI's shape. `ViewBase` and `RootedScopeBase` moved onto the calls layer, replacing `ControlBase.RunPoll` and its bare `catch (Exception ex)`. `ObjectBase.Poll` deleted. `RunDoWithElement` **no longer retries the action**: phase 1 resolves and gates on readiness within the budget; phase 2 acts **once** (R0). One `LogEntry`/`LogExit` pair per public call. Near-miss `Warning` logging. `Assert*` resolves per attempt, like `Get*` (MAUI review-2 fix S1). `BRINELL_CALL_LOG`, and the context `Name` in the log line (?A10) | The step 2 pinned tests pass; one log pair per call; the near-miss test passes; `RunDoWithElement` shows `Invoke` `Times.Once` on an action that has no effect |
| **5. Scope readiness** | done (2026-09-26, see 2.1) | `ScopeReadiness` and the `ProbeReadiness` chain; `AsksParent`; `ProbeContentReadiness`; `CanResolveElements` and `EnsureLoaded` (if present in Html) removed; `AppRoot` for the browser page/frame; the busy signal for a Blazor page is read from a well-known data attribute (design 7.2, sample-app.md item 3); `ScopeNotReadyException` replaces `PageLoadException` throughout Html; the frame model (`ForFrame`) becomes a scope wrapper. `AppRoot.WaitReady` is a call unit with its own budget (MAUI review-2 fix 8). **The UI test bases:** `BlazorSampleTestBase` (in both UI test projects) loses its `Thread.Yield`/`Task.Delay` loop and its `Context.FindElement`: the page's `WaitReady` covers "circuit up". The fixture starts the sample host on a free port, instead of expecting one at `BLAZOR_APP_URL` | Scope tests pass; a busy page defers the child call within its budget |
| **6. Confirm** | done (2026-09-26, see 2.1) | `Confirm` replaces the "loop the whole call until the effect is seen" pattern that today lives inside `RunPoll`. The act-then-confirm Cores (`Click` on a busy-triggering button, `Fill`, `Check`/`Uncheck`, `SelectOption`, `Focus`/`Blur`) get a second phase that reads the effect once per attempt and never repeats the action | Replaced element: new message, action `Times.Once`; the R0 "no effect" test from step 2 passes |
| **7. Collections** | done (2026-09-26, see 2.1) | `ItemKey` for Html rows (design 7.6, differences item 5: DOM node id, or `data-item-key`, or position). `MaterializeAttempts` for a Blazor `Virtualize` scroll. `ListControl` and `TableControl` move onto a `CollectionObjectBase<TParent, TSelf, TItem>` + `ItemObjectBase<TCollection, TSelf>`. `Item(index)` and `SelectItem` become waiting call units; `TryItem` / `TrySelectItem` answer now (R7) | Collection tests pass; the row-recycling test from sample-app.md item 4 fails when a row is asked to click after its key changed |
| **T2. Todo web tests** | done (2026-09-26, see 2.1); UAT on the web head deferred | `tests/Brinell.Samples.Todo.Web.UITests`: hermetic and live fixtures (`TodoWebFixture` starts the host process on a free port with the test set's launch settings), Html page objects and ControlObjects (`TodoStatus`, `SectionCard`), and the TOD journeys with a `Head=Web` trait. The Todo `.uat.md` files run with `Target: Html` (?T2). `JourneyCoverage` learns the `Head` trait ([todo-blazor.md](todo-blazor.md) section 3) | Hermetic suite green in 5 of 5 runs; live tests green with `BRINELL_UAT_LIVE_API=1`; a `WaitForRequest` on `X-Todo-Client` passes; every UAT file that passes on MAUI passes on the web head, or carries a head tag and a reason |
| **8. Prove it** | done (2026-09-26, see 2.1) | On a branch: (a) re-introduce the login-button `disabled`-forever bug (sample-app.md item 2) and prove the login test fails with "disabled for N ms" instead of timing out silently; (a2) re-introduce the Todo `CanExecuteChanged` bug in the shared view model and prove TOD.04.4 fails on **both** heads, naming what it saw; (b) re-introduce the "action retried" bug in `ControlBase` and prove a step-4 test catches it; (c) run `Brinell.Html.UITests`, `Brinell.Blazor.UITests` and the Todo web suite in full against a saved timing baseline; (d) run the repeat-run script against the step 2 baseline | R0 proofs both fail against their bugs, and name what they saw. No new failures against the timing baseline. Framework-race failures down, and none moved from "app bug" to "pass" |
| **9. Write it down** | done (2026-09-26, see 2.1) | In `.docs/` (a decision is recorded once, there): widen AD-010 from "MAUI ahead of Core" to "MAUI and Html ahead of Core"; widen the call-model and control-object contracts from "the MAUI stack" to both stacks; close X2 with a note under AD-009. AD-004 and AD-009 are already stack-neutral, so no new AD mirrors them, and AD-011 and AD-012 are taken. `samples/Todo/README.md` gains the web head; the two new skills merged (`skills.md`); `AGENTS.md` and `.github/copilot-instructions.md` updated; `CHANGELOG.md` list; a note in `../stale-readiness/move-down.md` that the Html slice is done | Docs merged |

After step 9, and **not in this plan**: several apps in one scenario (MAUI and web against one
backend, multi-user). See [todo-blazor.md](todo-blazor.md) section 4. Steps 1-7 keep to the
constraints in its section 4.2, so that project stays possible.

### 2.1 Step notes

Filled in as steps finish, in the same shape as `../stale-readiness/plan.md` section 2.1: what
was proved, what the design was wrong about, and what was deferred to a later step on purpose.

**Step 9 (2026-09-26).**

- **Decisions.**
  - AD-010 now reads "MAUI and Html ahead of Core, on purpose", and the invariant line and
    the `.docs` index match.
  - AD-009 gains an Html paragraph: X2 closed; driver waits bounded by the call; a busy page
    hides bugs that only show while it is busy, so those are proven below the UI.
  - No new AD: AD-004 and AD-009 are stack-neutral (the decision-record skill says to amend).
- **Contracts.** `call-model.md` covers MAUI and Html, with an "Html specifics" section: the
  driver budget, a confirmation's own budget, the busy signal, rows by key, dialogs over a busy
  page. `control-object.md` gains an "Html controls" section (hand-written, the same rules,
  `ActAndConfirm`).
- **Skills.** `html-control` and `html-ui-test` (with `references/forbidden-apis.md`) in
  `.github/skills`, with stubs in `.claude/skills`.
- **Instructions.** `AGENTS.md` (the skills, the Html forbidden-API pointer, verification
  commands, the Chromium install) and `.github/copilot-instructions.md` (the skills, the build
  and test commands).
- **Docs.** `docs/run/Html.md` rewritten: `Brinell.Html.Tests` is a real suite, the UI suites
  host the sample themselves, and the Todo web tests. `docs/architecture/stack.md` says
  `BLAZOR_APP_URL` is optional.
- **CI.** `Brinell.Html.Tests` is in the unit test list: every pin passes now, 37/37.
- **CHANGELOG** has Added and Removed entries for the Html work. `move-down.md` marks the
  Html slice done, with what it adds to the move down and the one conflict to reconcile
  (Html's `BusySignal` against Core's `BusySignalPolicy`).
- **Not written.** No `docs/architecture/html.md`: `docs-upkeep` says not to store a
  description of a subsystem, which would go stale. The rules live in `.docs/`, and the how-to
  in the skills and `docs/run/Html.md`.

**Step 8 (2026-09-26).**

- **(a) The login that never re-enables.** `/login?login-bug=stuck-disabled` (step 2).
  `LoginFlow_WhenLoginNeverReenables_TheSecondLoginFailsSayingDisabled` passes in every run
  since step 4: the second Login fails within its 5 s budget with `ElementNotReadyException`
  ("was found but is disabled: not enabled still after 5000 ms"), and never clicks. Before
  step 4, the same call would have retried the click (X2).
- **(a2) The Todo `CanExecuteChanged` bug (MAUI's R0 proof), put back in
  `AsyncRelayCommand`.** This notifies before `ExecutionTask` is set, so a running command
  looks executable and a press during the run is dropped.
  - **On the web head, TOD.04.4 passed against the bug, 3 of 3.** That is not the framework
    hiding it. The web head's busy signal comes from the same `IsBusy` that the command sets,
    which the bug does not touch. Every call waits for the page to be idle (R4), so Brinell
    never presses inside the window where the bug drops presses. MAUI's Todo pages have no busy
    signal, so there the test pressed into the window and failed.
  - **The bug is still caught, at the tier `journeys.md` gives it:**
    `TodoListViewModelTests.Sync_WhileRunning_TellsTheUiItIsDisabled` fails against it (113/114).
  - The fix is restored: no diff, and 114/114.
  - **Lesson:** a busy signal protects a journey from racing the app, and also stops the
    journey from seeing a bug that only shows inside the busy window. "Disabled while
    running" belongs in the unit tier, which is where the Todo sample already tests it.
- **(b) X2 put back:** `ActOnce` retries the action on any exception.
  - Pin (e) fails: `Click` called **66** times.
  - `AnElementReplacedAfterTheAction_FailsAsStale_AndTheActionRanOnce` fails: `Fill` called 20
    times.
  - The fix is restored: no diff, and 37/37.
- **(c) and (d) Suites against the baseline** (section 4), 3 repeat runs each after steps 4-7,
  and 5 for the web head:

  | Suite | Baseline (5 runs) | Now |
  | --- | --- | --- |
  | `Brinell.Html.UITests` | 36 tests; 2 framework races (5/5 and 4/5) | 40 tests, **0 failures** in 3 of 3 runs |
  | `Brinell.Blazor.UITests` | 31 tests; 3 framework races (5/5 each) | 31 tests, **0 failures** in 3 of 3 runs |
  | Todo web head | - | 10 hermetic in 5 of 5 runs; live 1/1 |

  - No failure moved from "app bug" to "pass": the baseline had no app bugs, and all five
    races are gone for their stated causes (a waiting `AssertTextContaining`, a page-object
    validation read).
  - **Timing.** The login tests take 2.4 s mean, against 2.2-2.5 s in the baseline.
    The counter tests take 0.3-1.1 s, against 0.3-0.4 s: they now wait for the app to be
    interactive, where before they raced it. The new UI guard takes 7.4 s, which is its 5 s
    budget spent on a button that never enables, by design.

**Step T2 (2026-09-26).**

- **Evidence.**
  - `samples/Todo/tests/Brinell.Samples.Todo.Web.UITests`: hermetic tier **10/10 in 5 of 5
    runs**. The live tier is 1/1 with `BRINELL_UAT_LIVE_API=1`, and skips without it.
  - The TOD.07.10 checks pass at both tiers: the WireMock `PUT` carries
    `X-Todo-Client: web-hermetic`, and the real API's request log has the `PUT` from `web-live`.
- **What was built.**
  - `TodoWebFixture`, the web version of `TodoAppFixture`: a test set seeded into a temp SQLite
    file, the WireMock `TodoServerStub`, a network-state file and a pinned clock. The web head
    runs in its own process through `WebAppHost`, on a free port, with those settings, and
    `StartTest()` resets it to online and the list.
  - Fixtures three-todos, empty and pending-changes.
  - Page objects:
    - `TodoListPage`, with a `TodoRows` collection keyed by `data-item-key`;
    - `TodoEditPage` and `TodoDetailPage`;
    - the parts: `TodoStatus<T>` (component), `SectionCard<T>` (container), and `TodoDialog<T>`
      with `AsksParent => false`, because the page is busy while the command that opened the
      dialog waits for its answer.
  - The tests: TOD.01.2, 01.3, 02.1, 02.2, 02.3, 04.2, 04.4, 05.1, 06.1, 07.2 + 07.10, and
    live 07.4 + 07.10. Each has `Journey`, `Pyramid` and `Head=Web` traits.
- **Found by the tests: a web-head bug.** `WebNavigator.GoBackAsync` navigated *forward* to
  the previous URL. After edit and save, the browser history was list, detail, edit, detail,
  and the browser's back button went to the edit page. It now goes back with
  `history.back()`, as Shell pops.
- **Framework additions T2 needed.**
  - `ControlBase.TryGetText()`: an immediate read (R7). Row predicates use it, so they do not
    run a call inside a call.
  - A `BlazorPageObjectBase(context, url)` constructor.
  - Page objects locate by `Locator.ByDataTestId`. This settles the step 1 note:
    `ByAutomationId` keeps its meaning.
- **Deferred, on purpose (done since, 2026-09-26):** the Todo `.uat.md` files on the web head (?T2).
  `Brinell.Samples.Todo.Web.Uat` runs them, 33/33, through `Head: web` and the `@web` tag. The UAT runtime's
  Todo fixture is MAUI's (`Target: MAUI`), and an Html target for the same scenario files needs
  its own fixture, plus page classes with the MAUI page names. That belongs with several apps
  in one scenario (todo-blazor.md section 4), where a scenario picks its app.
- **Also not done:** `JourneyCoverage` does not learn the `Head` trait. It has been red since
  `a781b11` (a separate task was proposed).

**Step 7 (2026-09-26).**

- **Evidence.**
  - `Brinell.Html.Tests` 37/37, including 5 new `CollectionTests`:
    - a row acts on its own item after the list is reordered;
    - a row whose item is gone fails as `ItemChanged` and acts on nothing;
    - rows without a key are kept by position;
    - `Item(index)` waits, and `TryItem` answers now;
    - `Item` fails as not found when the row never comes.
  - `Brinell.Blazor.Tests` 172/172; both UAT projects 2/2.
  - `Brinell.Html.UITests` **40/40** and `Brinell.Blazor.UITests` 31/31, 3 of 3 runs each.
- **What changed.**
  - `Brinell.Html.Collections`:
    - `ItemKey` (Logical, from `data-item-key`; or Position);
    - `IHtmlCollection`;
    - `CollectionObjectBase<TParent, TSelf, TItem>`: `Item(index)`, `Item(key)` and
      `ItemWhere` wait, as one call each; `TryItem` answers now (R7); `GetItemCount`,
      `WaitItemCount`, `AssertItemCount`;
    - `ItemObjectBase<TSelf>`.
  - A row re-finds its element by key on every lookup, so it follows its item through reordering
    and node reuse. When its item is gone, it reports `ItemChanged`: a call on one of its
    controls waits within its budget, then fails naming the item, and never acts on the row now
    in its place. This follows MAUI's rule, a row never acts on another item. It does not follow
    the design's wording "row 3 now shows another item": with a logical key, the row has no
    position to lose.
  - `ItemKey` has no `AutomationId` or `DomNode` kinds (design 7.6 listed four). A shared
    `data-testid` ("todo-row") names every row alike, so it cannot key one. `InstanceKey` (step 3)
    already covers node identity, where a scope needs it.
- **Sample (sample-app.md item 4):** `/recycling-list`, 200 rows in `<Virtualize>` with
  `data-item-key` and no `@key`, so Blazor reuses row nodes. Recycle rotates the items after
  300 ms; Delete and Reset.
- **UI tests** (`RecyclingListTests`):
  - The row object for position 3 still deletes *its* item after Recycle, when position 3
    shows another title.
  - A row whose item was deleted fails with `ScopeNotReadyException` (`ItemChanged`); no other
    item is deleted.
- **Deliberately not done.**
  - `ListControl` and `TableControl` stay text-reading controls. They are already call units, and
    moving them onto the row base would change their API for no behaviour. New row-level
    work uses `CollectionObjectBase`.
  - No `MaterializeAttempts`. Html rows are in the DOM or not; `<Virtualize>` renders what is in
    view, and a far row needs a scroll. That is a `ScrollToItem` to add when a test needs it.
  - `ContainerRoot` is not renamed to `Root`: the name is only used inside Html.

**Step 6 (2026-09-26).**

- **Evidence.**
  - `Brinell.Html.Tests` **32/32**: every pin from step 2 passes, the step 6 one included, plus
    two new `ConfirmTests`:
    - an element replaced after the action fails as stale, saying the action "ran once";
    - the confirmation has a budget of its own. This test fails with the fix below undone
      (checked).
  - `Brinell.Blazor.Tests` 172/172; both UAT projects 2/2.
  - `Brinell.Html.UITests` 38/38 and `Brinell.Blazor.UITests` 31/31, 3 of 3 runs each.
- **What changed.**
  - `ControlBase.ActAndConfirm`: act once, then `Confirm` the effect with the caller's timeout
    as a second phase (R3). An effect that never shows fails as "accepted X and did not take
    it: expected ..., it reads ...". A replaced element fails as `StaleElementException`.
    Neither repeats the action.
  - Confirmed actions:
    - `TextInputControl.SetText` (value equals) and `TypeText` (value contains);
    - `TextAreaControl.AppendText` (value ends with);
    - `ToggleControlBase.SetChecked`, and so `Check` and `Uncheck`;
    - `SelectControl.SelectByValue` and `SelectByText`. `SelectByText` is now one call; it used
      to look up the options outside any call;
    - `RangeControlBase.SetValue`;
    - `FocusableControlBase.Focus` and `Blur` (`document.activeElement`).
  - `SelectMultiple` is not confirmed yet (it needs a multi-value read).
- **An Html-only finding: a confirmation must run under its own attempt context.** Playwright
  reads take their timeout from the running call (?A5). Inside a confirmation, that must be
  the confirmation's own deadline, not the resolve phase's, which the action may have left all
  but spent. `Confirmer` now sets its own `AttemptContext` while it polls. MAUI's reads take no
  timeout, so MAUI does not need this.
- **Test doubles act like elements now.** `MockHtmlFactory`'s element keeps state: `Fill` sets
  the value, `SendKeys` appends, `SelectOption` selects, `Check` and `Uncheck` toggle, and
  `Focus` and `Blur` move focus. Five Blazor unit tests set fixed return values that ignored the
  action, which is exactly the app bug R0 must report. They now start from a value and let the
  action change it.

**Step 5 (2026-09-26).**

- **Evidence.**
  - `Brinell.Html.Tests`: 29 of 30 pass, including 6 new `ScopeReadinessTests`:
    - a busy page defers the call and it acts once;
    - an unreadable busy signal fails at once, within 500 ms, and never acts;
    - a Required signal that never appears fails as `NotLoaded` within the budget;
    - a container's content readiness is on its child's path (MAUI's A1);
    - `AssertLoaded` throws `ScopeNotReadyException`;
    - `AppRoot.WaitReady` is one call.
    The one still failing is the step 6 pin.
  - `Brinell.Blazor.Tests` 172/172; both UAT projects 2/2.
  - `Brinell.Html.UITests` **38/38** (a new frame test) and `Brinell.Blazor.UITests` **31/31**,
    3 of 3 runs each. The fixtures started the sample themselves.
  - No bare `catch`, no `Thread.Sleep`, and no `PageLoadException` in `srcnew/Brinell.Html*` or
    `Brinell.Blazor`. The one catch left is `ControlCall`'s log-and-rethrow.
- **What changed.**
  - `Brinell.Html.Scopes.ScopeReadiness`, the same type as MAUI's.
  - `IHtmlElementScope.ProbeReadiness()`. `IsReady()` has no timeout any more, and `WaitReady`
    is a call unit.
  - `ScopeGate`: a configuration error ends the call at once.
  - `ScopeNotReadyException` replaces `PageLoadException`.
  - Every page, container and `AppRoot` wait is a call unit (`ObjectBase.RunProbe`).
    `ObjectBase.Poll` and `PollAsync` are gone.
  - **Pages:** the probe checks URL, idle, then `<body data-busy>` through the new
    `BusySignal` (Ignore, Optional, Required). `HtmlPageObjectBase` is Optional.
    `BlazorPageObjectBase` is **Required**: a Blazor app sets the attribute when it becomes
    interactive, so a prerendered page is `NotLoaded` until then. Html's Required differs from
    Core's `BusySignalPolicy.Required`, where a missing signal is a configuration error. That is
    why Html has its own enum.
  - **Containers:** `ContainerBase` asks its parent (`AsksParent`), then its root, then
    `ProbeContentReadiness`.
  - **`FrameScope<TParent>`** (?A6): the iframe's document as a container, through
    `IHtmlElement.TryGetFrameDocument()` (Playwright `ContentFrame`). `PlaywrightTestContext.ForFrame`
    is gone.
  - **`AppRoot`** is the whole page as a scope.
  - `PlaywrightTestContext.ProbeReadiness` throws `AppUnavailableException` on a closed page.
    `BlazorTestContext.WaitForBlazorReady`, a sleep loop, is gone.
  - **`Brinell.Html.Testing.WebAppHost`** starts a web app on a free port with explicit launch
    settings, then waits for a ready path. This is the host T2's `TodoWebFixture` and multi-app
    scenarios need.
- **Samples.**
  - Gallery: `brinell-busy.js`, set by the layout's first interactive render (sample-app.md
    item 3). It only says "interactive"; nothing sets `true` during handlers yet.
  - Gallery: a `/frames` page with a same-origin iframe (item 7).
  - Todo web head: `<body>` no longer starts with a static `data-busy="false"`, which would
    have claimed "interactive" too early.
- **UI test bases (both projects).**
  - The sample is started once per run by `TestBase/BlazorSample` on a free port, and built by
    a `ReferenceOutputAssembly="false"` project reference. `BLAZOR_APP_URL` still selects a
    running one.
  - `NavigateToPage` waits with `SampleShell.AssertIdle()`, a page object with a Required busy
    signal. The `Thread.Yield`/`Task.Delay` loop and the `Context.FindElement` in the test base
    are gone.
  - **"Run the sample by hand before the UI suite" is no longer needed.**
- **Not done here, on purpose.**
  - `BusySignal` stays Html's own; its shape moves down with Core's in step 9's notes.
  - The UAT fixture keeps its own `HtmlSampleHost` (`dotnet run`, with empty catches in test
    code). Switching it to `WebAppHost` is a small follow-up.
  - `ContainerRoot` is not renamed to `Root` (design 7.3); that goes with the collections in
    step 7.

**Step 4 (2026-09-26).**

- **Evidence.**
  - `Brinell.Html.Tests`: 23 of 24 pass. That is every step 3 and step 4 pin, every guard, the
    mapping tests and the measurement. The one still failing is the step 6 pin (`SetText` that
    does not take), for its step 2 reason.
  - `Brinell.Blazor.Tests` 172/172; both UAT projects 2/2.
  - `Brinell.Html.UITests` 37/37 in 3 of 3 runs.
  - `Brinell.Blazor.UITests` **31/31** in 3 of 3 runs, plus one more after the last fix. The
    three counter and button tests the baseline classed as framework races now pass: pin (i)'s
    fix, a waiting `AssertTextContaining`.
  - Playwright calls per completed `Click`: still 4.
- **What changed.**
  - `Brinell.Html/Calls`: `ControlCall`, `Poller`, `Deadline`, `AttemptContext`, `Observation`,
    `ObservationLog`, `Confirmer`, `Confirmation<T>`, copied from MAUI's shape and typed on
    `IHtmlTestContext`. Also a public `CallBudget.RemainingOr` for the driver.
  - `ControlBase` runs every public member as one call: one log pair, one budget, one poll.
    Each attempt finds the element again. `EnsureVisible` is one check and one throttled
    scroll.
  - **X2 is closed.** `RunDoWithElement` and `RunSetWithElement` resolve the control until it
    is ready, then act **once** (`ActOnce`). `RunPoll` and its `catch (Exception)` are gone.
  - Enabled checks moved into `EnsureReadyForActionCore` (clickables, text inputs), as in MAUI.
  - `RunAssert(Action<IHtmlElement>)` waits (R7). `LabelControl`, `ToggleControlBase`,
    `TextInputControl`, and the Blazor `IFrameControl` and `ImageControl` no longer nest one call
    inside another's poll.
  - `WaitExists(false)`, `WaitVisible` and `AssertVisible`/`AssertExists` work on an absent
    element (pin h).
  - Actions take `int? timeoutMs = null`: `Click`, `DoubleClick`, `RightClick`, `Hover`, `Clear`,
    `SetText`, `TypeText`, `SetChecked`. `AssertValue`, `AssertChecked`, `AssertTextContaining`
    and `GetValue` also take one.
  - `PlaywrightTestContext.FindElement` makes one attempt (pin f1).
  - ?A5 (pin f2): every Playwright call that can wait gets `Timeout` = what is left of the call.
    Outside a call it is at most `PlaywrightHtmlElement.OutsideACallMs` (5 s), not Playwright's
    30 s.
  - `IHtmlElement.ScrollIntoView(int)` has no default any more; callers pass `CallRemainingMs`.
  - The context has a `Name` (?A10: the test name in the log line, "Test" when unset) and
    `NearMiss` settings. `BRINELL_CALL_LOG` opens a CSV call log, as in MAUI.
- **An Html-only finding: a cut-off attempt is not an observation.** Once driver calls get the
  remaining budget, the last attempt of an exhausted call starts with about 1 ms, and Playwright's
  own timeout fires mid-read. The UI guard then reported "1 of 44 attempts raised
  TimeoutException", instead of what the other 43 saw ("disabled"). `Poller` now ends the poll on
  a `TimeoutException` raised after the deadline, and reports the last complete attempt. It is
  pinned by `Guard_Click_OnAButtonThatNeverEnables_SaysDisabled_WhenTheLastReadIsCutOff`. MAUI's
  drivers take no timeout on reads, so MAUI does not hit this.
- **Pin setup corrected.** The near-miss pin had used a plain `InvalidOperationException` to
  stand in for "detached", because the stale type did not exist in step 2. It now throws
  `StaleElementException`, which the driver has reported since step 3.
- **Deliberately not done here.**
  - `ObjectBase.Poll` stays for page and container waits (`WaitLoaded`, `WaitTitle`,
    `ContainerBase.WaitReady`). Those become call units in step 5, and `Poll` goes with them.
  - `ControlBase` keeps its name. The design's `ViewBase` rename is churn with no behaviour, so
    it is left for step 9, if at all.
- **Fixed on the way:** `ScrollableControlBase` and `ScrollContainerControl` scrolled with a
  nested 5 s default inside a call.

**Step 3 (2026-09-26).**

- **Evidence.**
  - `Brinell.Html.Tests`: pin (g) now passes, as do 5 new mapping tests
    (`PlaywrightErrorMappingTests`) and 3 guards: the new "browser closed mid-call fails at once
    as `AppUnavailableException`" and step 2's two. The 13 still failing are exactly the step 4
    and step 6 pins, each for its step 2 reason.
  - `Brinell.Blazor.Tests` 172/172; `Brinell.Html.Uat.Tests` 2/2; `Brinell.Blazor.Uat.Tests`
    2/2; `Brinell.Html.UITests` 37/37.
  - `Brinell.Blazor.UITests`: the 3 pinned `AssertTextContaining` failures, nothing else.
- **What changed.**
  - `Brinell.Html.Exceptions`: `StaleElementException`, `ElementNotReadyException`
    (`NotReadyReason`), `AppUnavailableException` and `RouteUnavailableException`, with MAUI's
    shapes and messages.
  - `PlaywrightErrors.Live` wraps every Playwright call in `PlaywrightHtmlElement` and
    `PlaywrightHtmlDriver`. A closed target becomes `AppUnavailableException`; a detached element
    or a disposed handle becomes `StaleElementException`. Anything else passes through.
  - `IHtmlElement.InstanceKey` is a `data-brinell-key` written on the node the first time it is
    read (?A4: option b, not handle identity). A replacement node has no key and gets a new one,
    so "the same element" is a string comparison. Recycling by `<Virtualize>` keeps the node,
    and so the key: that case is `ItemKey`'s job in step 7.
  - The three catch-alls in `PlaywrightTestContext` are gone: `IsReady` reads `IPage.IsClosed`.
  - `ObjectBase.Poll` and `PollAsync` retry only `StaleElementException`,
    `ElementNotFoundException` and `ElementNotReadyException`.
  - `RunPoll` ends the call at once on `AppUnavailableException`, with its log exit.
- **Moved, not done here.**
  - `ScopeNotReadyException`, and replacing `PageLoadException`, move to step 5: the exception
    carries a `ScopeReadiness`, which arrives there (as in MAUI).
  - `RunPoll`'s `catch (Exception)`, the one bare catch left in Html, goes in step 4 with the
    retried action.
- **For step 4:** `System.TimeoutException` from a Playwright call is not mapped. Once calls pass
  a timeout (?A5, pin f2), a timeout on a read means "not there within the budget". Decide there
  whether it becomes `StaleElementException` or ends the attempt.

**Step 2 (2026-09-26).**

- **The pins** are in `testsnew/Brinell.Html.Tests/CallModelPinTests.cs`. `Brinell.Html.Tests`
  is a real test project now. It is **not** in CI while pins fail on purpose; step 9 adds it.
  Each pin fails today for the reason in its summary, checked message by message:

  | Tag | Pin | Fails today because |
  | --- | --- | --- |
  | `step3` | (g) `IsExists` on a throwing lookup | "IsExists() answered false although the lookup threw" (catch-all in `PlaywrightTestContext.TryFindElement`) |
  | `step4` | (a) Click / (d) Get, Wait, Assert find the element again | "Element was not visible within 1000ms after scrolling into view" |
  | `step4` | (b) the caller's 300 ms budget | 1025 ms |
  | `step4` | (c) one log pair | two `LogEntry` calls |
  | `step4` | (e) an action that throws | `Click` called **64 times** (X2) |
  | `step4` | (f1) a missing control on the Playwright context | 3080 ms on a 300 ms budget (`FindElement` loops `ElementFind`) |
  | `step4` | (f2) Playwright calls pass a timeout (?A5) | `GetAttributeAsync` and `ClickAsync` are called without options |
  | `step4` | (h) `WaitExists(false)` on an absent control | `ElementNotFoundException`: the wait needs the element first. **A defect found while pinning.** |
  | `step4` | (i) `AssertTextContaining` waits | "Text does not contain '1'. Actual: 'count: 0'" (one read) |
  | `step4` | near-miss warning | no `Warning` logged |
  | `step6` | `SetText` that does not take | no error: `Fill` once, then success |

  The two `Pin=guard` tests pass and must keep passing: a button that never enables fails
  within its budget saying so, and is never clicked; a hidden element is never clicked.
  `Pin=measure` prints the Playwright call count (section 4).
- **Moved to step 3, as in MAUI:** the guard "the browser closes mid-call and the call fails at
  once" needs `AppUnavailableException`.
- **Html actions take no `timeoutMs`.** `Click()` and `SetText(text)` use `DefaultWait`, so
  budget pins go through `GetText`/`WaitText`/`AssertText`. Step 4 gives the actions a
  `timeoutMs`, as the MAUI generator contract has.
- **Sample (sample-app.md item 2):** `/login?login-bug=stuck-disabled` leaves Login disabled
  after the first submit. The `stuck-enabled` variant is not built, because nothing needs it.
  Item 1 (a status region on every page) is deferred until a test needs it.
- **UI tests:**
  - `LoginFlow_EmptyCredentials_ShowsError` (and `_Async`) now go through a page-object
    control (`LoginPage.ValidationMessage`, `AssertVisible`), instead of a one-shot
    `Context.FindElements`.
  - New UI-level R0 guard: `LoginFlow_WhenLoginNeverReenables_TheSecondLoginFailsSayingDisabled`.
  - `Brinell.Html.UITests`: 37/37 in 3 of 3 runs.
  - `Brinell.Blazor.UITests`: still the three `AssertTextContaining` tests. They stay failing
    until step 4 fixes the control (pin i); the tests are not changed to hide it.
- **A Blazor detail worth knowing:** `class="text-danger"` on a `ValidationMessage` *replaces*
  its `validation-message` class. Locate validation messages by the class the page sets.

**Step 1 (2026-09-26).**

- **Evidence**, against the section 4 baseline:
  - `srcnew/Brinell.sln` builds with 0 errors. Its 26 warnings are all old ones in the MAUI
    samples.
  - `Brinell.Blazor.Tests` 172/172, `Brinell.Html.Uat.Tests` 2/2, `Brinell.Blazor.Uat.Tests`
    2/2 and `Brinell.Core.Tests` 16/16: identical to the baseline.
  - One run of each UI suite fails exactly the baseline's tests (2 and 3), and nothing else.
  - `git diff --stat srcnew/Brinell.Core` is empty.
  - The step's grep matches only comments.
- **What changed.**
  - `IHtmlElement`, `IHtmlElementScope`, `IHtmlPage`, `IHtmlTestContext` and `IHtmlContainer`
    stand alone.
  - New: `IHtmlDriver` with `PlaywrightHtmlDriver` (?A2). The context delegates lookups,
    navigation and screenshots to it.
  - Element lookups make one attempt: `TryFindElement(Locator)` and `FindElements(Locator)`.
    The throwing form is `HtmlElementExtensions.FindElement`, as in MAUI. The timeout finds, and
    their `catch (PlaywrightException)`, are gone.
  - `IsLoaded()`, `GetTitle()` and `TakeScreenshot(string?)` lost their unused timeouts.
  - `HtmlPageObjectBase` carries `[TestPage]`.
  - The Blazor unit-test mocks changed mechanically (`FindElements(locator)`).
- **Deliberately not changed yet** (behaviour stays for later steps):
  - `PlaywrightTestContext.FindElement` still loops for `ElementFind` (step 4, as MAUI).
  - `IHtmlElement.ScrollIntoView(int timeoutMs = 5000)` keeps its default. It takes
    `CallRemainingMs` in step 4.
  - `ForFrame` stays (step 5, `FrameScope`).
  - The async shims stay (?A1).
  - The context's three catch-alls stay (step 3).
- **One small difference from "no behaviour change".** `IsIdle` moved into the driver and now
  catches only `TimeoutException`. A closed page used to answer "not idle", and now throws.
  That is step 3's direction (R5), and no baseline test depends on it.
- **For T2.** Playwright maps `Locator.ByAutomationId` to `data-automation-id` or `id`, not to
  `data-testid`, which the Todo web head (and `sample-app.md` item 9) use. T2's page objects
  either use `Locator.ByDataTestId`, or `ByAutomationId` learns `data-testid`. Decide in T2.

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

**Playwright calls per completed `Click`:** 4 (`CountAsync`, `IsVisibleAsync`, `IsEnabledAsync`,
`ClickAsync`, one each), for a present, visible, enabled button. Measured in step 2 on the
step 1 code (`Measure_PlaywrightCallsPerCompletedClick`, mocked Playwright locator).

**Correction (step 2).** The counter and button failures are a framework race, as classified,
but the cause given above is wrong. Those tests call `LabelControl.AssertTextContaining`, which
reads the text **once** and fails at once (an R7 breach: a non-`Try` member that does not wait).
The read races the last click's render, which is why the count is always exactly one behind.
The passing counter tests use `AssertText`, which polls. The prerender point is still true, but
it does not explain these failures. Pin (i) covers the real cause.

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
