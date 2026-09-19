# Stale readiness: plan

The work plan and its status. What to build is in [design.md](design.md). Why it is built that way
is in `background/` (see [README.md](README.md)).

Status: **done**, 2026-09-19. Steps 0-9 done, and the findings of the
[implementation review](implementation-review.md) fixed (2.1, "Review fixes"). The next project is drafted in [move-down.md](move-down.md). The design is accepted (section 3).

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
- **Android starts from a failing baseline** (Range 6 passed / 16 failed, Picker 0 passed / 8 failed). Compare against it; do not
  expect green.

Status values: `todo`, `doing`, `done`, `blocked`.

| Step | Status | Contents | Done when |
| --- | --- | --- | --- |
| **0. Rename** | done | `ItemContainerBase` → `ItemObjectBase`, `IMauiItemContainer` → `IMauiItemObject`; tests, Todo sample, skills (`maui-control`, `maui-ui-test`) | Builds; `Brinell.Maui.Tests` green; no behaviour change |
| **1. MAUI stands alone** | done (2026-09-19, see 2.1) | MAUI stops implementing Core's `IElement`, `IDriver`, `IElementScope`, `IPageObject`, `ITestContext<T>`, `IContainerControl` and `IContainerObject`, and stops using `ControlObjectBase`. New or reshaped: `IMauiElement`, `IMauiDriver`, `IMauiElementScope`, `IMauiPage`, `IMauiTestContext` (design 4.2). MAUI copies of the geometry and scope helpers. Finds make one attempt, and the timeout finds are gone (F7 goes with them). `IsLoaded()`, `GetTitle()` and `TakeScreenshot(string?)` lose their unused timeouts. **Otherwise no behaviour change:** `ProbeReadiness` and the readiness chain come in step 5 | Solution builds; `Brinell.Maui.Tests`, `Brinell.Maui.Uat.Tests` (MAUI pages still discovered) and every other stack's tests green; `Brinell.Core` untouched (`git diff --stat srcnew/Brinell.Core` empty); no `IElement<`, `IElementScope`, `IPageObject`, `IDriver<` or `ControlObjectBase` found in `srcnew/Brinell.Maui*` |
| **2. Pin it down** | done (2026-09-19, see 2.1) | **Failing unit tests:** original plan (a)-(e), trace (f)-(h), scopes (dialog on a busy page, content readiness reaching a child, row re-resolves to another item, `ScrollToItem` budget, `Item(index)` waits). **R0 guard tests:** a command that never re-enables → `Click` fails with "disabled" within its budget; an action with no effect → `NotConfirmed`, action `Times.Once`; an element re-rendered every 100 ms → the call passes **and** a near-miss `Warning` is logged; the app exiting mid-call → `AppUnavailableException` without waiting out the budget. **Baseline:** a repeat-run script (a suite run N times, each failure classified as framework race, app bug, environment or driver gap); a `FindElement` and an alive check measured per call; the collection UI tests that take more than 5 s; full-suite timing | Tests fail for the stated reason; baseline recorded in section 4 |
| **3. Stale signal** | done (2026-09-19, see 2.1) | MAUI exceptions (design 4.3), with `AppUnavailableException` raised by both drivers. `InstanceKey`; `Live` in both drivers; `MauiTestContext.TryFindElement` narrowed; `WithRoot`; the alive rule (R6); every Selenium catch switched; catch-alls removed | Mapping unit tests; no Selenium type found in `srcnew/Brinell.Maui` |
| **4. Calls** | done (2026-09-19, see 2.1) | `Calls/*` (design 6), including near-miss logging through `LogExit(Warning)`. `ViewBase` and `RootedScopeBase` moved onto the calls layer. `ObjectBase.Poll` runs on `Poller` (kept, see 2.1; removed in the review fixes). The `MauiTestContext.FindElement` loop removed. `Menu.OpenCore` | The step 2 plan and trace tests pass; one log pair per call; the near-miss test passes |
| **5. Scope readiness** | done (2026-09-19, see 2.1) | `ScopeReadiness` and the `ProbeReadiness` chain; `AsksParent`; `ProbeContentReadiness`; `CanResolveElements` removed; `AppRoot` without sweep; `DriverRootScope` removed; page and container public members as call units; `ScopeNotReadyException` replaces `PageLoadException` (4 tests) | Scope tests pass |
| **6. Confirm** | done (2026-09-19, see 2.1) | `Confirm` replaces `Until`; the 8 act-then-confirm Cores; `EnsureClickableCore` calls removed | Replaced element: new message, action `Times.Once`; the R0 "no effect" test passes |
| **7. Collections** | done (2026-09-19, see 2.1) | `ItemKey`; `MaterializeAttempts`; the members table in design 7.6 | Collection tests pass; the long-list tests from step 2 given explicit budgets where needed |
| **8. Prove it** | done (2026-09-19, see 2.1) | On a branch, remove the `ToolbarButton` override and run Todo 5×. **R0 proof:** re-introduce the Todo `CanExecuteChanged` bug on a branch and run TOD.04.4. Run Windows `Brinell.Maui.UITests` in full against `timing-baseline.json`. Run the repeat-run script against the step 2 baseline. Run the Android baseline subset. Restore the override and the fix | Todo 5/5. **TOD.04.4 still fails against the bug, and names what it saw.** No new Windows failures, and no slowdowns beyond the threshold. Framework-race failures down, and none moved from "app bug" to "pass". Near-misses listed. Android no worse than baseline |
| **9. Write it down** | done (2026-09-19, see 2.1) | AD-004 ("one unit per call; only the call's poll waits; a call waits for its scope chain"); a new decision record for R0; a decision record for R9 (MAUI ahead of Core, on purpose); both skills; `CHANGELOG.md` (design section 10 list); a first draft of the "move down" plan (design 4.5) | Docs merged |

### 2.1 Step notes

What each finished step showed, and where it departed from the design.

**Step 1 (2026-09-19).**

- **Evidence.** All checks were run against a baseline recorded first:
  - Every MAUI consumer builds: `Brinell.Maui`, `.FlaUI`, `.Appium`, `.CommunityToolkit`,
    `.Extensions`, `Brinell.Presenter`, `Brinell.Maui.Tests`, `.Uat.Tests`, `.UITests`,
    `.UITests.Mobile`, `Brinell.Presenter.Uat.Tests`, and the Todo UI tests for both platforms.
  - `Brinell.Maui.Tests`: 137 passed, 1 skipped, identical to the baseline.
  - `Brinell.Core.Tests` 16/16 and `Brinell.Uat.Tests` 61/61 pass.
  - `git diff --stat srcnew/Brinell.Core` is empty.
  - The step's grep finds 0 matches.
  - The MAUI UAT run discovers the same 20 pages with the same controls as the baseline.
  - The UAT and Presenter UAT failures are the ones already present in the baseline (section 4).
- **The design was wrong about UAT discovery.** MAUI UAT finds pages through Brinell.Core's
  `TestComposition`, which recognizes `[TestPage]` or Core's page interface. It does not use name
  inference. `PageObjectBase` now carries the (inherited, unchanged) Core `[TestPage]` attribute,
  so every concrete page is found as before. The design's section 4.5 is corrected.
- **The throwing lookup on an element is an extension method, not an interface default.**
  `MauiElementExtensions.FindElement` wraps `TryFindElement ?? throw`, so a mocked element needs
  only `TryFindElement` set up. The same class holds MAUI's copies of the geometry and scope
  helpers. The design's section 4.2 is corrected.
- **Deferred to later steps, on purpose.** Step 1 promised no behaviour change, so these keep
  today's semantics for now:
  - `IMauiElementScope` keeps `IsReady(int?)` and `WaitReady(int?)`; `ProbeReadiness` arrives in
    step 5.
  - `IMauiPage.ProbeReadiness()` still returns Core's `PageReadinessSnapshot`, a record type, not
    an interface; `ScopeReadiness` replaces it in step 5.
  - `IMauiElement.ScrollIntoView(int timeoutMs)` keeps its timeout (step 4).
  - `Menu.OpenCore` still waits for its trigger, now through `Until` (step 4).
  - The drivers keep their own waited lookups as internal `FindChrome` / `FindAllChrome`: the
    FlaUI navigation pane and light-dismiss, the Appium drawer opener, a toolbar item raised by
    id, and picker items.
- **A latent test bug, left as found.** Two Presenter UAT tests call
  `IsLoaded(timeoutMs: 30000)` and expect a wait. `IsLoaded` never honoured that timeout, so they
  never waited. They now call `IsLoaded()`, which is the same behaviour. Whether they should wait
  (`WaitLoaded(true, 30000)`) is a test fix to decide separately. It may explain some of the
  Presenter baseline failures.
- **Generator note.** `Brinell.Generator.Cli` writes LF line endings, while the repository stores
  CRLF. Regenerating marks every `.gen.cs` file as modified even when its content is unchanged.
  Check with `git diff --numstat -- '*.gen.cs'`, and restore the files that show no change.

**Step 2 (2026-09-19).**

- **The pinned tests** are in `testsnew/Brinell.Maui.Tests/Semantic/StaleReadinessPinTests.cs`.
  Each is tagged with the step that should make it pass (`--filter "Pin=step4"`). All 16 fail
  today for the reason in their summary, checked message by message:

  | Tag | Tests | Fails today because |
  | --- | --- | --- |
  | `step3` | (g) `WaitExists(false)` on a throwing lookup | `MauiTestContext.TryFindElement` swallows every error, so the call passes |
  | `step4` | (a) Click / (d) Get, Wait, Assert find the element again | "Element was not visible within 1000ms after scrolling into view" |
  | `step4` | (b) the caller's 300 ms budget | the call took 1001 ms |
  | `step4` | (c) one log pair | two `LogEntry` calls |
  | `step4` | (e) an action that throws | no `LogExit(Error)`; `Invoke` is called once |
  | `step4` | (f) missing `AppRoot` control | 3080 ms on a 300 ms budget |
  | `step4` | (h) sweeps | 26 sweeps in 400 ms |
  | `step4` | the near-miss | no `Warning` logged |
  | `step5` | (A3) dialog over a busy page | `PageLoadException`: "Last readiness state: Busy" |
  | `step5` | (A1) a container's readiness on its child's path | checked 0 times |
  | `step7` | (B3) a row answers for another item | no error at all |
  | `step7` | (D1) `ScrollToItem` budget | 1032 ms on 300 ms |
  | `step7` | (D6) `Item(index)` waits | `ElementNotFoundException` at once |

  The two `Pin=guard` tests pass, and must keep passing:
  - a disabled control fails the call within its budget and is never invoked;
  - an action with no effect fails and is done once.
- **Moved from step 2 to step 3:** the guard "the app exits mid-call and the call fails at once".
  It needs `AppUnavailableException`, which step 3 introduces. Written in step 3, it fails there,
  because `RunPoll` still retries everything, and passes in step 4.
- **A1 will change shape.** The A1 test overrides `WaitContentReadyCore`. Step 5 renames that
  hook to `ProbeContentReadiness`, and the test moves with it.
- **Repeat runs:** `tools/Scripts/repeat-run.ps1` runs a project N times and writes
  `repeat-summary.md`: failures per test, flaky or always, and a suggested class. It refuses to
  start while another test host is running.

**Step 3 (2026-09-19).**

- **Evidence:**
  - `Brinell.Maui.Tests`: 147 passed, 1 skipped. That is all 137 earlier tests, the 7 Appium
    classifier tests, the step 3 pin (g) and the 2 guards.
  - The 16 tests still failing are exactly the pins for steps 4, 5 and 7, plus the new
    "app gone" guard.
  - The FlaUI classifier tests pass 6/6, and no Selenium type remains in `srcnew/Brinell.Maui`.
  - Windows `Brinell.Maui.UITests`, run in full, because unit tests cannot exercise real UI
    Automation: 352 tests, 349 passed, 2 failed, 3 min 5 s. The 2 failures are explained under
    "Found", and pass after the fix.
- **The design understated the FlaUI change.** Wrapping members in `Live` was not enough. Most
  FlaUI state getters (`Text`, `Name`, `AutomationId`, `Value`, `Selected`, `Hint`, `Checked`),
  and the pattern and scroll helpers, had catch-alls that answered null or false. So a removed
  element read as "empty" long before any poll could see it. Every such catch now lets
  `UIA_E_ELEMENTNOTAVAILABLE` through (`FlaUIErrors.IsElementGone`), and still treats an
  unsupported property as "none". Appium had the same catch-alls, and got the same treatment
  (`AppiumErrors.IsGone`).
- **`Live` covers** state, identity (`InstanceKey`), lookups, attributes, checked, range and
  dropdown state, text entry, the activation patterns (`Perform`), scrolling, and picker
  selection. It **does not cover**:
  - members that go through the app bridge, which address the element by its already-wrapped
    `AutomationId`;
  - app-level members (flyout, alerts, dialogs), which work on the re-attached window.
- **"App gone" is detected precisely.**
  - FlaUI: a removed element while the launched process has exited.
  - Appium: only the fixed W3C "invalid session id" wording, never broad words (unit-tested
    against "The connection was closed" and "Resource not found").
- **Catch-alls replaced by what each one meant:**
  - `ContainerRoot`: re-find on stale.
  - `ItemObjectBase.IsUsable`: the alive rule plus non-empty bounds.
  - `HasUsableBounds` / `IsControlType`: stale answers false; anything else propagates.
  - `TryScrollItemIntoView` / `TryActivate`: `NotSupportedException` or
    `InvalidOperationException` is an answer; a stale row propagates.
  - `MauiTestContext.TryFindElement`: catches nothing.
- **Found:** the addressability probes had never looked by name.
  `NavigationDemoPage.TryFindByName` used an XPath locator, which the FlaUI driver does not
  support. The lookup threw, the old catch-all in `MauiTestContext.TryFindElement` turned that
  into null, and every by-name probe answered "not found" without looking. So
  `MenuItems_AreInvisibleToTheAccessibilityTree` passed without checking anything. The helper
  now uses `Locator.ByName`, and both tests pass, this time for real. The report changed:
  `PageMenuFile` **is** reachable by name, where the old report always said NO. The menu items
  are still unreachable, so the menu test's claim now holds for real.
- **Small decisions:**
  - `ScopeNotReadyException` waits for step 5, where it carries `ScopeReadiness`.
  - `ClickableControlBase.PressCore` spells out Selenium's `Keys.Space` code (`""`), so the
    control layer names no Selenium type.
  - The FlaUI classifier tests live in `Brinell.Maui.UITests`, because the driver targets Windows
    and `Brinell.Maui.Tests` does not.
- **Left for step 4:** Appium's `ScrollIntoView(timeoutMs)` loops up to 5 s on Android, a wait
  inside the call. Step 4's single visibility attempt passes it the call's remaining budget.

**Step 4 (2026-09-19).**

- **Evidence:**
  - `Brinell.Maui.Tests`: 158 passed, 1 skipped. Every step 3, step 4 and guard pin passes (14/14).
  - The 5 tests still failing are exactly the pins for steps 5 (A1, A3) and 7 (B3, D1, D6).
  - Windows UI suite, 2 full runs. First: 353 tests, 350 passed, 1 failed
    (`ForegroundWatchdogTests.Navigating_DoesNotTakeTheForeground`: "grabs since launch: 1071"),
    1 min 54 s. The failure did not reproduce: that test passed alone twice, with 1 grab, and a full
    rerun passed 351/351 (2 gated skips) in 3 min 7 s, with 1 grab in the whole run. Something
    outside the suite fought for the foreground during the first run; the cause is not known. The
    durations vary too much (1:54 and 3:07) to claim a speed-up.
- **What is built** (`srcnew/Brinell.Maui/Calls/`):
  - `ControlCall`: one log pair per call, action included, plus the near-miss `Warning`.
  - `Poller`: the one loop. At least one attempt; `AppUnavailableException` and
    `PageLoadException` end it at once; anything else is recorded and retried.
  - `Deadline`, `AttemptContext` (the sweep and scroll-into-view throttles), `Observation`,
    `ObservationLog` (the failure message built from what was last seen).
  - `ScopeGate`: the page check that starts every attempt, and its message.
- **`ViewBase` and `RootedScopeBase`** run every `Run*` helper on these. The `Page.WaitReady`
  gate in front of the poll is gone (E1); the page is now the first step of each attempt, inside
  the one budget. Actions and setters resolve by polling, then act once inside the call's log pair
  (rrE 4.1). `RunDo` no longer repeats its operation when it throws (R0); nothing called it.
- **One attempt everywhere:**
  - `EnsureVisible` is private and makes one attempt, and passes the call's remaining budget to
    `ScrollIntoView`. That also caps Appium's up-to-5 s Android scroll loop.
  - `WaitVisibleCore` is removed, and so are the `EnsureVisible` calls inside `ToggleCore` and
    `RadioButton`.
  - `MauiTestContext.FindElement` makes one attempt and no sweep (F1, F2).
  - `Menu.OpenCore` makes one trigger lookup.
  - `ObjectBase.Poll` keeps its signature but runs on `Poller`, so there is one loop. Its callers
    become call units in step 5.
- **Lookups (F3, F5):**
  - `FindElement()` is no longer virtual: `TryFindElement() ?? sweep ?? throw NotFound()`.
  - The three controls that overrode it (`IndicatorView`, `DrawingView`, `Stepper`) now override
    `NotFound()` only.
  - Inside a call, `Locate` sweeps at most once per Animation interval.
- **Behaviour changes by design:** a disabled or not-yet-visible control fails with
  `ElementNotReadyException` ("was found but is disabled: still after 300 ms"), not
  `TimeoutException`. Two older unit tests and the "disabled" guard's wording were updated to
  match.
- **Deliberately left as it is:** `Until` stays until step 6 (`Confirm`), and a compound `Run(...)`
  still logs a pair of its own around the calls it makes.

**Step 5 (2026-09-19).**

- **Evidence:**
  - `Brinell.Maui.Tests`: 159 passed, 1 skipped, 3 failed. The 3 failures are exactly the step 7
    pins (B3, D1, D6); A1 and A3 (`Pin=step5`) pass, and so do the guards and all earlier pins.
  - UAT and Presenter UAT: the baseline failures, unchanged (section 4).
  - Regenerating from the templates changes no `.gen.cs` file (R8).
  - Windows UI suite: 353 tests, 351 passed, 2 failed, 3 min 3 s.
    - `OccludedScreenshotTests`: known, environmental.
    - `ProductCollectionTests.ItemWhere_ScrollsToFindOffscreenRow` ("row is null", 8 s): did not
      reproduce. It passed alone 3 times and in 2 runs of its class (15/15 each). It is the
      `FindItem` materializing loop, which step 7 rewrites (`MaterializeAttempts`); watch it there.
- **What is built:**
  - `Scopes/ScopeReadiness.cs`: `ScopeReadinessState` and `ScopeReadiness` (scope name, state,
    detail, root reacquired). A parent that is not ready is returned unchanged, so a message names
    the scope that actually failed.
  - `RootedScopeBase`:
    - `ProbeReadiness()`: parent (`ProbeParentReadiness`), then root, then
      `ProbeContentReadiness(root)`. A stale root is found once more; a second stale answer is
      `StaleRoot`.
    - `IsReady()` is one probe.
    - `WaitReady` is a call unit (`RunProbe`: one `ControlCall`, one `Poller`).
    - `ContentReady()` / `ContentNotReady(detail)` are the answers an override gives.
  - `ContainerObjectBase`: `AsksParent` (default true).
  - `PageObjectBase`:
    - its content readiness is `IsLoaded()` then the busy signal. The busy value goes in the
      detail ("busy value: 'True'"); a missing or unreadable signal is a configuration error.
    - `WaitBusy`, `WaitLoaded`, `WaitTitle`, `AssertLoaded`, `AssertTitle` and `AssertIdle` are
      call units.
  - Every control attempt starts with `ScopeGate.Check(scope)`: the whole chain, not just the page.
    A container's own members check the parent chain; a page's own members check the page.
  - `ScopeNotReadyException` (carries `Readiness`) replaces `PageLoadException` in MAUI.
    `Poller.IsFatal`: `AppUnavailableException`, and `ScopeNotReadyException` for a configuration
    error.
  - `Popup` and `ContentDialog`: `AsksParent => false`. `Popup.Page => null` is gone, so the log
    names the page again.
  - `AppRoot`: `ProbeReadiness` is the context's; `AllowsScrollLookup => false` (no sweep).
    `ShellFlyout` turns the sweep back on and scrolls its own host.
  - Removed: `CanResolveElements`, `CreateScopeNotReadyException`, `IsParentReady` /
    `WaitParentReady`, `WaitContentReady(Core)`, the page's `EnsureLoaded` guard, and
    `DriverRootScope` with its test.
- **Behaviour changes by design (tests updated):**
  - **A lookup no longer consults the page** (design 7.4, Q2). `IsExists()` / `IsVisible()` on a
    control read the element only. `ReadinessTests.IsExists_DoesNotWaitForThePage_WhenItHasNotArrived`
    and `IsVisible_ReadsTheElementOnce_AndNotThePage` (renamed) now expect 0 page checks.
    `Wait*`/`Assert*`/actions still ask the page on every attempt.
  - `Click_FailsNamingThePage_WhenItNeverLoads` now expects `ScopeNotReadyException` with
    `NotLoaded`, not `ElementNotFoundException`.
  - Container `WaitExists`, `WaitVisible`, `AssertExists` and `AssertVisible` are call units whose
    attempts check the parent chain.
- **Small decisions:**
  - Page waits (`WaitReady`, `WaitBusy`, `WaitLoaded`, `AssertLoaded`, `AssertIdle`) keep
    `PageLoad` as their default budget, not `DefaultWait` as design 7.5 says. Moving to the
    shorter default would change page-load tests for no gain in this work. Title waits use
    `DefaultWait`.
  - `AssertTitle` throws `AssertionException` (a mismatch), not a scope failure.
  - `BusySignalPolicy` stays Core's enum; nothing in Core changed.
- **Not done here:** the root renames of design 7.3 (`TryFindRootElement`, `Root`, `CacheRoot`,
  `IsRootUsable`). Step 5's row does not list them, and they change no behaviour. They are left for
  the move-down draft (step 9).

**Step 6 (2026-09-19).**

- **Evidence:**
  - `Brinell.Maui.Tests`: 161 passed, 1 skipped, 3 failed. The 3 failures are the step 7 pins
    (B3, D1, D6).
  - `Pin=step6` and `Pin=guard`: 4/4 pass. The new pin
    `Toggle_ReplacedAfterTheAction_SaysSoAndActsOnce` gets a `StaleElementException` ("replaced
    after Toggle, which ran once ... not repeated"), sees `Toggle` `Times.Once`, and fails at once,
    without waiting out its budget. The R0 guard "an action with no effect fails and is done once"
    passes.
  - `Brinell.Generator.Tests`: 156/156.
  - Regenerating changes no `.gen.cs` file.
  - Windows UI suite: 353 tests, 351 passed, 2 gated skips, **0 failed**, 2 min 59 s.
  - Todo UI suite, one run: 36 passed, 3 skipped, 0 failed.
- **What is built:**
  - `Controls/Base/Confirmation.cs`: `ConfirmationResult` (`Confirmed`, `NotConfirmed`, `Replaced`)
    and `Confirmation<T>`. `Failure(locator, action, notConfirmed)` gives a `StaleElementException`
    for `Replaced`; otherwise it gives the control's own exception, so the existing exception
    types and messages are kept.
  - `Calls/Confirmer.cs`: the loop. It reads only. A stale read ends it as `Replaced` at once, and
    a fatal error propagates.
  - `Confirm<T>(read, done, timeoutMs)` on `ViewBase` and `RootedScopeBase` replaces both `Until`
    overloads.
  - The 8 act-then-confirm Cores use it: `ToggleCore`, `SetCheckedDirectly`, `CarouselView`
    swipe, `Stepper.SetValueCore`, `MediaPlayPauseButton.SetPlayingCore`, `DrawingView.DrawLineCore`,
    `Expander`, `RatingView.TapStarCore`.
  - `EnsureClickableCore` is gone from the Cores. On controls, `EnsureReadyForActionCore` already
    ran it while the call resolved the element; the Core calls repeated it. Item containers
    (`ClickableItemBase`, `SelectableItemBase`, `TabItem`) had no such hook: `RootedScopeBase`
    now has `EnsureReadyForActionCore(root)`, called in `ResolveReadyRoot`. `ClickableItemBase`
    overrides it. `TabItem` checks the button inside the tab, which is what it invokes. A disabled
    item is now waited for within the call's budget and then fails as `ElementNotReadyException`,
    instead of failing at once.
- **Beyond the row:**
  - The generator's nested-unit-of-work warning said "Wait with Until instead"; it now says
    `Confirm`, and so do its two tests. The generated code is unchanged (R8).
  - The skills taught `Until`: `maui-control` (`SKILL.md` R2, and `component`, `container`,
    `generator-contract`, `routes`, `simple-control`), `convert-control` and `maui-ui-test`
    (`unit-tests`) now show `Confirm`. Their wider update stays in step 9.
  - `UntilTests` became `ConfirmTests`, with a case for the stale read.

**Step 7 (2026-09-19).**

- **Evidence:**
  - `Brinell.Maui.Tests`: 165 passed, 1 skipped, **0 failed**. Every pin passes: B3, D1 and D6,
    plus a new one, `Row_FindsItsItemAgain_WhenItsElementIsRecycled`, which fails on the old code
    because that code read the recycled element's label.
  - One earlier run of the unit tests had a single failure that I could not name, and 6 reruns
    were green. The timing-bound pins (`elapsed < 800`) are the likely suspects. Watch them in
    step 8.
  - Regenerating changes no `.gen.cs` file.
  - Windows UI suite: 353 tests, 351 passed, 2 gated skips, **0 failed**, 3 min 47 s.
    `ProductCollectionTests` 15/15 after the budget edits below.
  - Todo UI suite, one run: 36 passed, 3 skipped, 0 failed.
- **What is built:**
  - `Containers/ItemKey.cs`: `ItemKey` (`Logical`, `AutomationId`, `Position`), with `IsHeldBy(root)`.
    The key is the logical index (`PositionInSet`) where the platform has one. Otherwise it is the
    automation id, where the strategy has stable ids (`ByIndexedId`, new
    `IItemStrategy.HasStableIds`) or no other realized row carries that id. Otherwise it is the
    position.
  - `IItemRootProvider`: `KeyOf(root, position)` and `TryGetItemRoot(ItemKey)`.
  - `IMauiItemObject.Key`, recorded in the `ItemObjectBase` constructor.
  - `ItemObjectBase`:
    - a cached root must be alive and still have a size;
    - the root is re-found by key, never by position alone;
    - `ProbeContentReadiness` reports `ItemChanged` when the element holds another item, and
      forgets it, so the next attempt finds the item by key.
  - `CollectionObjectBase`:
    - `MaterializeAttempts` replaces `TryMaterializeMore`, `HasMoreThan` and
      `WaitForProgressThenSettle`. It is a per-call state machine: look for the target, take one
      scroll step, wait for new rows ("waiting for new rows"), wait for a jump's rows to settle
      ("rows still moving"). The end of the list is a `Missing` observation.
    - `Poller.Until` gains an optional `stop`, so a search can end at the end of the list.
    - Members as design 7.6 lists them:
      - `Item(index)`, `this[int]`, `Item(key)`, `SelectItem` and `ItemWhere` are call units that
        wait;
      - `TryItem`, `TrySelectItem` answer now; `FindItem` scrolls the list once, stops at the end,
        and does not wait for an item to appear;
      - `ScrollToItem`, `ScrollToEnd`, `ScrollToTop` (now bounded) and `WaitForItems` are call units
        on one budget;
      - `WaitItemCount`, `WaitAnyItem`, `AssertItemCount` and `AssertEmpty` are call units;
      - `GetItemCount` is one read.
    - `IMauiCollectionObject.Item(int, int?)`, and `FindItem` / `ItemWhere` gain `timeoutMs`.
- **Budgets (Q9), as expected:**
  - `ItemWhere_ScrollsToFindOffscreenRow` failed on the 5 s default (5 attempts, "last:
    Pending(scrolled) at 7413 ms"). It now passes `DefaultTestTimeoutMs` (15 s), with a comment.
  - `OutOfRange_TryItemReturnsNull_ItemThrows` and `SearchByContent_FindsTheRightRow` expect a
    lookup to fail. Now that `Item(i)` and `ItemWhere` wait (Q10), those lookups pass
    `timeoutMs: 500` (5.0 s and 6.0 s → 1 s each).
- **Slower, and why:** the offscreen search went from 7 s (step 6) to 12–13 s. A trace (removed
  after use) put the time in the predicate:
  - `GetText` scrolls each off-screen row into view first. It did that before this work too.
  - That scrolling recycles rows. A row now notices (`ItemChanged`) and finds its own item again.
    Before, it read whatever item its old element showed.
  - The cost is the price of Q7's correctness, not waiting. Step 8 compares timings.
- **Small decisions:**
  - `SelectItem` resolves by polling, then activates once. A refused activation fails the call
    (`InvalidOperationException`) and is not repeated.
  - The search skips rows already seen by logical index before building a row object.

**Step 8 (2026-09-19).**

No branches were created. Each experiment was a temporary edit in the working tree, restored from
a copy afterwards and checked with `git diff`.

- **Todo 5× without the `ToolbarButton` override: 5/5 (195/195), on the third attempt.** The
  first two attempts each failed one test, and both were real framework defects that this step
  found and fixed:
  1. `SelectItem` (new in step 7) failed "found, and the platform did not activate it"
     (1/195). `ActivateItemCore` answers false only when nothing was activated: a row without a
     size, or a selection the platform refused. `SelectItem` now tries again within its budget,
     and fails naming the refusal when the budget runs out.
  2. A toolbar click failed with "no element published on the app's bridge answers
     InvokeToolbarItem" (1/195). At that moment the page was still publishing its bridge, so
     nothing was pressed. This was not about visibility, which is what the override controls.
     - **New contract (`ActOnce`):** a Core method throws `ElementNotReadyException` only before
       it acts. Actions and sets (`RunDoWithElement`, `RunSetWithElement`, on controls and scopes)
       resolve again and ask again within the budget; any other exception still ends the call at
       once. All `Ensure*Core` guards were checked: each runs before its action.
     - The FlaUI driver now raises `ElementNotReadyException` for toolbar and menu items when no
       bridge target answered, and when the item is disabled. Nothing was performed in either
       case.
     - Pins: `Toolbar_ActionNotPerformedYet_IsAskedAgainWithinTheBudget` (`step8`), and two
       guards: `Guard_Toolbar_FailsWithinItsBudget_WhenTheItemStaysDisabled` and
       `Guard_Toolbar_OtherActionFailure_IsNotRepeated`.
  - The override is restored, as the plan says. Dropping it for good is a separate decision.
- **R0 proof: TOD.04.4 still fails against the bug, 3/3.** The bug was re-introduced in both
  `AsyncRelayCommand` classes (notify before `ExecutionTask` is set). Its unit test
  `Sync_WhileRunning_TellsTheUiItIsDisabled` failed, as it did against the original. The UI
  journey `Cancel_WithChanges_AsksFirst_...` failed every time with "Expected container to
  exist. Locator: ClassName:ContentDialog. Actual: 'False'": the second Cancel raised no
  dialog. The call log shows the Cancel click succeeding (63 ms) and the dialog assertion
  failing after its 5 s budget. The fix is restored (no diff), Todo unit tests pass 114/114, and
  TOD.04.4 passes 2/2.
- **Full Windows suite** (with the call log): 353 tests, 350 passed, 2 gated skips, 1 failed.
  The failure was `ToolbarVerbTests.InvokeToolbarItem_WhenTheItemIsDisabled_IsRefusedAndDoesNothing`,
  which pinned the old `BrinellException` type for a disabled item; it now expects
  `ElementNotReadyException` with reason `Disabled`, as `MenuVerbTests` does. Toolbar and menu
  verb tests: 8/8. Timing report: `TestResults/20260919-172808-908730`.
  3 classes over 2× their `timing-baseline.json` mean:
  - `ProductCollectionTests` 1411 ms vs 420 ms. It was already 1151 ms in the step 2 baseline;
    the rest is the long-list search (12.5 s, step 7).
  - `ScrollVerbTests` 540 ms vs 250 ms. It was already 606 ms in the step 2 baseline.
  - `ShellFlyoutTests` 677 ms vs 163 ms. This is noise, not this work: the class measured 954 ms
    and 777 ms on 2026-09-19 before any behaviour change, 251 ms at step 3, and 241 ms during
    step 7.
- **Repeat run against the step 2 baseline** (Todo 5×, override in place): 0 failures (baseline:
  0). Without the override, also 0 (see above). The failure this work began with, "about one full
  Todo run in two", no longer occurs even without the override.
- **Near-misses:** 1 in the full Windows run (4,833 logged lines):
  `GridCollectionDemoPage / ProductListContainer / FindItem`, 11.7 s of its 15 s budget, 8
  attempts. That is the long-list search, and expected. 0 in the Todo 5× run (6,579 lines).
- **Android** (Range + Selection, `emulator-5554`): 31 tests, 10 passed, 21 failed.
  - The baseline was Range 6 passed / 16 failed and Picker 0/8.
  - The passes are the `IsExists` / `IsVisible` / `IsEnabled` probes (now including the 3 Picker
    probes: the baseline's cascade from a dialog left open is gone), and
    `StepperButtons_ReportInvokeBehavior`.
  - Every failure is a known driver gap:
    - "Stepper does not expose its current value" (10);
    - Appium "Cannot convert '' to float" (6);
    - the Picker's native dialog shows 0 items (5).
  - There are no stale or timeout failures. No worse than the baseline.
- **Built for this step:**
  - `BRINELL_CALL_LOG` (a folder): when set, `MauiTestContext` writes one CSV call log per
    context through Core's `CsvTestLogger`. It is used only when the options give no logger. UI
    fixtures give none, so near-misses could not be listed without it.
  - `tools/Scripts/repeat-run.ps1` sets `MSBUILDDISABLENODEREUSE`. A reused MSBuild node held
    the pipe, and the script hung after its first run.
- **For step 9:** the `ActOnce` contract refines R0's "an action runs once" to "an action
  that did not happen may be asked again". The design (R0, 2.1, 8), the skills (`maui-control`:
  guards before the action) and AD-004 should say so.

**Step 9 (2026-09-19).**

- **Decision records** (`docs/architecture/decisions.md`):
  - AD-004 "Wait For State" now states the MAUI call model: one unit per call, only the call's
    poll waits, the scope chain, finding again, `Try*` vs waiting members, and `Confirm`.
  - New **AD-009 "App Bugs Stay Failures"**: R0, with the `ActOnce` refinement, the at-once
    failures, near-misses and `BRINELL_CALL_LOG`, and the step 8 proof.
  - New **AD-010 "MAUI Ahead Of Core, On Purpose"**: R9.
- **Design:** R0's table has a row for "the action was not performed", and the "never" list reads
  "repeat an action that may have run". Section 10 marks the root renames as not done, and says
  that `ObjectBase.Poll` and `IsCachedRootValid` remain.
- **Skills:**
  - `maui-control`:
    - `generator-contract`: guards come before the action, and `ElementNotReadyException` from a
      Core means "did not act".
    - `container`: `AsksParent` and `ProbeContentReadiness` replace the parent-ready overrides and
      `WaitContentReadyCore`.
    - `collection`: item keys, what waits and what answers now, budgets for long lists, and
      `ActivateItemCore` returning false only when nothing was activated.
    - The `Until` → `Confirm` changes were made in step 6.
  - `maui-ui-test`:
    - expected-failure lookups get a short `timeoutMs`;
    - a raised `timeoutMs` only for an operation long by nature, with a comment;
    - what each failure type means;
    - `BRINELL_CALL_LOG`.
  - `docs/controls/interfaces.md`: `AppRoot` replaces `DriverRootScope`; readiness through
    `ProbeReadiness`.
- **`CHANGELOG.md`** (Unreleased):
  - Added: `Confirm`, the MAUI interfaces and exceptions, scope readiness, `ItemKey`,
    near-misses, `BRINELL_CALL_LOG`, `repeat-run.ps1`, AD-009 and AD-010.
  - Changed: the call model, the Core bases MAUI left, single-attempt finds, the exception types,
    waiting collection members, item keys, `AppRoot` without a sweep, and `Is*` checks.
  - Removed: the list from design section 10, limited to what is actually gone.
  - Fixed: the `NavigationDemoPage` by-name probes.
  - The old "`Until`" entries are corrected, not left standing.
- **Move-down draft:** [move-down.md](move-down.md). It covers:
  - what moves to Core;
  - the order and known work per stack, with usage counts;
  - what the MAUI work left over (root renames, `ObjectBase.Poll`, page-wait budgets, `Is*`
    semantics);
  - risks and open questions.
- No code changed in this step.

**Review fixes (2026-09-19).** From [implementation-review.md](implementation-review.md); each
finding is numbered as there.

1. **A row's own members on a recycled element.** `ItemObjectBase.IsCachedRootValid` now also
   requires `Key.IsHeldBy(root)`, so a cached element that holds another item is dropped and the
   item is found again by key. Pins: `Row_OwnRead_...` and `Row_OwnClick_...` (`Pin=review`). The
   read returned "Item 7" before the fix.
2. **`SelectItem` activating twice.** It now resolves by polling, then activates once. Only a
   `false` ("nothing activated") is asked again, and any exception ends the call. Pin
   `SelectItem_FailureAfterActivating_IsNotRepeated`: 33 selects before the fix, 1 after. The
   step 8 behaviour (asking again after `false`) has its own pin. Windows still tries the
   containing `ListItem` rows before the element: `TryActivate` treats `NotSupportedException` /
   `InvalidOperationException` as "this candidate did nothing", as the drivers document.
3. **Budgets below the call.**
   - `IMauiElement.ScrollIntoView(int timeoutMs)` lost its 5 s default.
   - `ControlCall` publishes its context as `AttemptContext.Current` while its body runs.
     Controls and scopes pass `CallRemainingMs` to every scroll: `ScrollIntoViewCore`, the
     collection's `TryScrollItemIntoView`, `ScrollHelper.ScrollIntoView` (now with a budget)
     and `ScrollView.ScrollTo` (now one call).
   - Appium's Android scroll makes at least one step on a spent budget.
   - The driver settle waits (dropdown, range, flyout, position settling) are kept on purpose,
     as recorded in design 5 and section 3.
4. **Unexpected exceptions.** After retries, `ObservationLog.Unexpected` reports a
   `WaitTimeoutException`: "N of M attempts raised X. Last: ...", with the exception as
   `InnerException`. After a single attempt it is still the exception itself. `RunProbe` uses
   the same rule.
5. **The bridge and a closed app.** `FlaUIMauiDriver.Exchange` wraps every bridge verb, and
   `ReadState`, the gestures and `FlaUIDeclaredElement.DeclaresStateReads` check too. When
   nothing answered because the launched app has exited, each raises `AppUnavailableException`.
   The catch-alls inside `BrinellBridgeLookup` stay: a tree walk that meets a vanished sibling
   must go on walking. Not tested with a real closed app; the FlaUI driver has no unit-test
   seam.
6. **Timeouts that did nothing.**
   - These answer now and lost the parameter: container and page `IsExists()` / `IsVisible()`,
     `GetItemCount()`, `IsEmpty()` and `TrySelectItem(index)`. The Todo fixture's
     `dialog.IsExists(0)` became `IsExists()`.
   - `ObjectBase.Poll` had no callers left and is removed.
   - Container `GetAttribute(name, timeoutMs)` keeps its unused parameter, because Core's
     `IControlObject` declares it ([move-down.md](move-down.md) section 4).
7. **Design departures.**
   - The near-miss thresholds are `NearMissSettings` (`MauiTestContextOptions.NearMiss`, read
     through `IMauiTestContext.NearMiss`).
   - `IMauiElementScope.DescribeMiss` builds a scope's "not found" message without looking;
     `ViewBase.NotFound()` and a container's `RootNotFound()` use it.

- **Evidence:**
  - `Brinell.Maui.Tests`: 176 passed, 1 skipped. That is 168 before plus 8 review pins.
    - The two row pins and the `SelectItem` pin were seen failing on the old code.
    - The scroll-budget, unexpected-exception and near-miss-settings pins fail on it by
      construction: the old 5000 ms default, the raw exception, and the fixed thresholds.
      They were not run against it.
  - `Brinell.Generator.Tests`: 156/156.
  - `Brinell.Core.Tests`: 16/16.
  - Regenerating changes no `.gen.cs` file (R8).
  - Windows UI, affected classes (collections, containers, scroll, background verbs,
    navigation): 105/105.
  - Todo UI suite, one run: 36 passed, 3 skipped, 0 failed.
  - Windows `Brinell.Maui.UITests` in full: 353 tests, 351 passed, 2 gated skips, **0 failed**,
    2 min 57 s.
  - Android was not run: no emulator was up. The Appium changes are the scroll loop's first
    step and the removed default.

## 3. Decisions taken

| Date | Decision | Source |
| --- | --- | --- |
| 2026-09-19 | Q1-Q10 as proposed: scope readiness chain, "loaded" merged into readiness, `AppRoot` the only whole-app scope, one alive rule, object-lifetime root cache, `ItemKey`, scope members as call units, one budget for materializing, non-`Try` waits | [scopes-analysis.md](background/scopes-analysis.md) section 6 |
| 2026-09-19 | Rename `ItemContainerBase` → `ItemObjectBase` (and the interface) | user |
| 2026-09-19 | After the review: keep everything marked "defer", and keep the full diagnostics. Only the two "drop" items go (`Deadline` internal, no `Carry`) | [review.md](background/review.md), outcome |
| 2026-09-19 | R0: app bugs stay failures; near-misses are reported; `AppUnavailableException` fails at once | user; design 2.1 |
| 2026-09-19 | **No bridges on Core interfaces.** MAUI gets its own interfaces now, Core is not changed, and the proven shape moves down later, one stack at a time. This replaces the earlier "Core base interfaces with bridges" decision | user; design R9, 4 |
| 2026-09-19 | Q6: keep a root for the object's lifetime and check it with the alive read (0.2 ms) rather than find it per call (22.7 ms for a page root, Windows) | measured, section 4 |
| 2026-09-19 | `ActOnce`: a Core method throws `ElementNotReadyException` only before it acts, so an action reported as not performed is asked again within the budget. Any other failure still ends the call at once (R0) | found in step 8 (a toolbar bridge not answering yet); AD-009 |
| 2026-09-19 | Page waits keep `PageLoad` as their default budget; the root renames of design 7.3 wait for the move down | step 5 notes; [move-down.md](move-down.md) |
| 2026-09-19 | Q9: materializing loops keep `DefaultWait`; the one long-list test (`ItemWhere_ScrollsToFindOffscreenRow`) passes an explicit budget | step 7 |
| 2026-09-19 | X5: near-miss thresholds stay at 3 replacements or 50% of the budget, as settings (`MauiTestContextOptions.NearMiss`) | step 8 (1 near-miss in 4,833 calls); implementation review |
| 2026-09-19 | Driver settle waits below an action (dropdown, range, flyout) keep short fixed bounds; scrolls take the call's remaining budget | [implementation-review.md](implementation-review.md), finding 3; design 5 |
| 2026-09-19 | An unexpected exception retried to the end of the budget is reported as `WaitTimeoutException` naming its type and count, with the exception inside | implementation review, finding 4; design 6.1 |
| 2026-09-18 | Keep `ToolbarButton.RequiresVisibilityForAction => false` (S4) | [original-plan.md](background/original-plan.md) |

Still open: design section 11 (S1, D3, X1, X2). Q6, Q9 and X5 are settled (above).

## 4. Baseline and measurements

Filled in during step 2 and compared in step 8.

| Measure | Value | Date | Notes |
| --- | --- | --- | --- |
| Todo UI run failure rate (toolbar Cancel) | ~1 in 2 before the `ToolbarButton` fix | 2026-09-18 | [original-plan.md](background/original-plan.md) section 1 |
| Windows `Brinell.Maui.UITests` Range | 23/23 green | 2026-09-18 | |
| Android Range / Picker | Range 6 passed, 16 failed (22); Picker 0 passed, 8 failed | 2026-09-18 (Range first measured 2026-09-05) | driver gaps |
| `Brinell.Maui.Tests` | 137 passed, 1 skipped (138) | 2026-09-19 | before step 1, and unchanged after it |
| `Brinell.Maui.Uat.Tests` | 0/4 pass | 2026-09-19 | pre-existing: the scenarios open pages "Main" and "User Form", which the sample app no longer has. Discovery itself finds 20 pages |
| `Brinell.Presenter.Uat.Tests` | 16 passed, 4 failed (20) | 2026-09-19 | pre-existing: `WorkspaceTree_ShowsMarkdownOnly` and 3 `UatFile_Passes` cases |
| Lookup cost per call, Windows | app-wide (a page root): **22.7 ms**; a child under a known root: **1.1 ms** | 2026-09-19 | probe `LookupCostProbeTests` (`BRINELL_PROBE=1`), 50 calls each. Android not measured (no emulator running) |
| Alive-check cost per call, Windows | **0.2 ms** (one `InstanceKey` read) | 2026-09-19 | decides Q6: about 100x cheaper than finding the page root again, so the root is kept for the object's lifetime and checked on access (the design default) |
| Collection UI tests taking more than 5 s | 1: `ProductCollectionTests.ItemWhere_ScrollsToFindOffscreenRow`, 10.4 s | 2026-09-19 | decides Q9. The other tests over 5 s are not collection tests: `AccessibilityAuditTests` 8.2 s, and the Hub parallelism probe (60 s, a cascade, see below) |
| Todo UI suite, repeated (`repeat-run.ps1`, 5 runs) | 39 tests × 5: **0 failures**, ~48 s a run | 2026-09-19 | with the `ToolbarButton` override still in place; step 8 repeats this without it |
| Full Windows `Brinell.Maui.UITests` | 346 tests (1 skipped), **5 min 3 s**; 327 passed, 18 failed | 2026-09-19 | all 18 are environmental. 17: a `Brinell.Samples.Maui.ShellApp` left running by an earlier run broke the Shell fixture's attach (after killing it, Shell 15/15 pass), plus the Hub parallelism probe that waits for Shell. 1: `OccludedScreenshotTests` (known). Timing report: `TestResults/20260919-135413-e64c96/.../test-timings.md` |
| Full Windows `Brinell.Maui.UITests` after step 3 | 352 tests, **3 min 5 s**; 349 passed, 2 failed (the by-name probes, fixed and passing since) | 2026-09-19 | the Shell failures were gone with the orphaned app; the occluded-screenshot test passed this time. Report: `TestResults/20260919-141052-2afc11` |
| Full Windows `Brinell.Maui.UITests` after step 5 | 353 tests, **3 min 3 s**; 351 passed, 2 failed | 2026-09-19 | `OccludedScreenshotTests` (known) and `ItemWhere_ScrollsToFindOffscreenRow`, which did not reproduce (3/3 alone, 2 class runs green); see 2.1, step 5 |
| Full Windows `Brinell.Maui.UITests` after step 6 | 353 tests, **2 min 59 s**; 351 passed, 2 gated skips, 0 failed | 2026-09-19 | first fully green full run of this work |
| Full Windows `Brinell.Maui.UITests` after step 7 | 353 tests, **3 min 47 s**; 351 passed, 2 gated skips, 0 failed | 2026-09-19 | slower than step 6 (2 min 59 s): the long-list search (+6 s, see 2.1) and two expected-failure lookups (+9 s, since given 500 ms budgets) |
| Full Windows `Brinell.Maui.UITests`, step 8 | 353 tests, **3 min 38 s**; 350 passed, 2 gated skips, 1 failed (an exception-type pin, updated; its class 8/8 since) | 2026-09-19 | report `TestResults/20260919-172808-908730`; see 2.1, step 8 |
| Todo UI suite, repeated, step 8 | with the override: 5 × 39, **0 failures**; without it: 5 × 39, **0 failures** | 2026-09-19 | `TestResults/step8-with-override`, `TestResults/step8-no-override-3` |
| Full Windows `Brinell.Maui.UITests`, after the review fixes | 353 tests, **2 min 57 s**; 351 passed, 2 gated skips, 0 failed | 2026-09-19 | see 2.1, "Review fixes" |
| Android Range + Selection, step 8 | 31 tests: 10 passed, 21 failed (all known driver gaps) | 2026-09-19 | baseline: Range 6 passed, 16 failed; Picker 0 passed, 8 failed |
| Classes over 2x their 2026-09-15 timing | `ProductCollectionTests` 1151 vs 420 ms/test, `StepperTests` 507 vs 161, `TextVerbTests` 893 vs 214, `ScrollVerbTests` 606 vs 250 | 2026-09-19 | before any behaviour change (step 1 changed none), so this is the baseline to compare against, not a regression of this work |
