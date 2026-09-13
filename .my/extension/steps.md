---
title: Stepped Plan — UIA Bridge and Background Execution
description: The whole programme as numbered, individually-requestable steps
status: plan
---

# Stepped Plan — UIA Bridge and Background Execution

Every piece of work across the three companion documents, as discrete steps. Each is sized for
one sitting, has one verification command, and can be requested on its own: **"do step 14"**.

- Architecture: [design-uia-gesture-pattern.md](design-uia-gesture-pattern.md)
- Gesture phases (narrative): [plan-uia-gesture-pattern.md](plan-uia-gesture-pattern.md)
- Capability catalogue: [beyond-gestures-uia-candidates.md](beyond-gestures-uia-candidates.md)

## How to use this

Steps are ordered by dependency, not importance. `Depends on: —` means it can start now. Update
the **Status** column as steps land; that column is the only state this document carries.

Commands are from the Brinell root. **Which filter to use is not a free choice** — see the next
section before running anything wider than Buttons.

## Which tests to run, and when — read this before widening a filter

**Until the verb catalogue is built, run Buttons and nothing else.**

```powershell
# the working tier, 11 tests in about a second
dotnet test testsnew\Brinell.Maui.UITests --filter "FullyQualifiedName~Tests.Buttons" -v:minimal /nr:false

# steps 17-19 only, once the gestures page exists
dotnet test testsnew\Brinell.Maui.UITests --filter "FullyQualifiedName~Tests.Gestures" -v:minimal /nr:false

# the shared-source guard — must pass from step 7 onward
dotnet build testsnew\Brinell.Maui.UITests.Mobile\Brinell.Maui.UITests.Mobile.csproj -v:minimal /nr:false
```

**Why, and it is not caution for its own sake.** Nearly every failure outside Buttons today is the
same thing wearing different clothes: an area whose verb has not been written yet falls back to a
route that background mode refuses, or to a ladder rung with a fault in it. Scroll wants step 21.
TimePicker and DatePicker want step 20. Picker wants step 24. Running those areas now does not
discover anything — it rediscovers the gap the plan already names, once per area, and each
rediscovery costs a diagnosis.

That is not a hypothetical: this happened repeatedly during stage B. The same navigation flake was
diagnosed from scratch four times because a wider filter kept surfacing it against a different
test, and seven tests now carry a step 36 `Skip` purely for having been next in line.

**Widen one area at a time, as its verbs land.

The two step 16 parallelism tests are the one exception to "narrow is safer": they live in two
different collections and each asserts on the other, so a filter must take both or neither.
`FullyQualifiedName~CollectionParallelismTests` does.** When step 20 is done, add DateTimes and keep it.
When step 21 is done, add Scroll. An area earns its place in the routine tier by having the verbs
it needs, and a failure in a newly added area then means something — it is about the verb just
written, not about a gap everybody already knew was there.

**The full suite stays a stage boundary**, as before: minutes, not seconds, and run to confirm a
stage rather than to develop against.

## Progress

| # | Step | Stage | Depends on | Status |
|---|---|---|---|---|
| 1 | Occluded-window screenshots | 0 | — | **done** — but see the follow-up below: the test became order-dependent in stage B |
| 2 | Off-screen window placement | 0 | — | **done** (`offscreen` needs ToolbarItem activation — see step 3) |
| 3 | Background-mode guard + inventory | 0 | — | **done** |
| 4 | Spike: child HWND raw provider | A | — | **done** |
| 5 | Spike: pattern round-trip, int **and string** | A | — | **done** |
| 6 | Spike: raw-view findability | A | 4 | **done** |
| 7 | Freeze the contract shape | A | 5, 6 | **done** |
| 8 | `Brinell.Uia.Contracts` + unit tests | A | 7 | **done** |
| 9 | Provider skeleton | A | 8 | **done** (folded into 11) |
| 10 | Walking skeleton: one gesture end to end | A | 9 | **done** |
| 11 | Registry + attached property | A | 10 | **done** |
| 12 | FlaUI client extensions | A | 10 | **done** |
| 13 | **Focus verb** | B | 12 | **done** |
| 14 | **Text input verbs** | B | 13 | **done** |
| 15 | Background mode passes | B | 3, 13, 14 | **partly** — zero refusals; blocked on the flaky step 36, and the human check is not done |
| 16 | Windows-only parallelism | B | 15 | **done** |
| 17 | Gestures sample page | C | 11 | **done** |
| 18 | Full gesture vocabulary, bound at publish time | C | 17 | **done** |
| 19 | Gesture control objects | C | 18 | **partly** - semantic verbs and the probe row landed; the id-addressed control object needs generator work |
| 20 | Date and time verbs | D | 14 | **done** — also closes 37 |
| 21 | Scroll verbs | D | 12 | **done** — and the two `Pending` traits are gone |
| 22 | Navigation verbs | D | 12 | **done** — and closes 33A |
| 23 | State-read verbs | D | 12 | **done** |
| 24 | Picker verbs | D | 21 | **done** — and found 39 |
| 25 | Dialog reads | D | 23 | **done** — `DismissAlert` deliberately not built |
| 26 | Menu and flyout verbs | D | 13 | **done** — and found 40 |
| 27 | Security gating | E | 18 | **done** — the first draft of the test passed for the wrong reason; parked 41 |
| 28 | Versioning and lifetime tests | E | 18 | **done** — two teardown gaps, three of them in the harness; parked 42 |
| 29 | Accessibility audit report | E | 18 | **done** — 11 of 33 elements have no route but the pointer |
| 30 | Documentation and AD-008 | E | 19 | todo |
| 31 | Stepper: 11 failing before any of this work | G | — | **parked** — a peer for `MauiStepper` was built and measured unworkable; see the section |
| 32 | Shell app: 13 failing before any of this work | G | — | **closed** — the page object scoped under an id nothing carried; the Shell now publishes its id, and the flyout goes through the app's verbs. 15/15 Shell tests, three runs |
| 33 | Navigation stall: a 2 s grace on the wrong question, and two 10 s negative assertions | G | — | **closed** — A by 22; B by J3 (negative assertions wait 500 ms, not 10 s) |
| 34 | Actions do not Try: remove `Try` from commands, keep it on searches | G | 33 | **closed by 44** |
| 35 | Notice when the framework starts waiting | G | — | **closed** — every test timed in-process; a per-class report with a baseline under `TestResults` per AD-007. A report, not a gate |
| 36 | `ReturnToHub` intermittently reports the hub never arrived (flaky, 1-4 tests per run, both modes) | G | — | **closed by 43** — a detached page answered `NavigationDepth` for a stack it had left |
| 37 | TimePicker reads back 12-hour in background mode (1 test) | G | 20 | **closed** — fixed by 20; the stale `Pending` trait is gone |
| 38 | Clipboard canary asserts on a probe its own helper calls inconclusive (1 test) | G | — | **closed** — the canary is inconclusive, not failed, when another process holds the clipboard |
| 39 | Selecting a repeated Picker item freezes MAUI (upstream) | G | 24 | **closed** — upstream, guarded by 24. Not ours to fix; recorded so nobody re-finds it |
| 40 | `S_FALSE` does not survive the bridge; three verbs rely on it | G | 26 | **closed by 43** — `BRINELL_E_DECLINED` carries the meaning |
| 41 | The bridge's run-time gate is measured on the test host, never on the app | G | 27 | **parked** — the gate that matters is measured; this is the other one |
| 42 | A bridge cached against a closed window is guarded but not measured | G | 28 | **parked** — needs a MAUI window that closes mid-suite |
| 43 | Outcomes travel as values, not as `S_FALSE` | H | 40 | **done** — closes 40; see [plan-the-quiet-run.md](plan-the-quiet-run.md) |
| 44 | Actions do not Try: the remaining three | H | 43 | **done** — `TryAppendText`/`TryClearFocus` are now `Supports…` + a command that throws; no `Try` action remains |
| 45 | `InvokeAnywhere` stops guessing; the seven step-36 skips come back | H | 44 | **done** — the seven step-36 skips are removed; readiness races fixed along the way |
| 46 | Watch the foreground; make "it never took the machine" a test | I | 45 | **done** — `ForegroundGrabs` and `ForegroundWatchdogTests`: navigating must not take the foreground |
| 47 | Launch without taking the foreground | I | 46 | **done** — explicit environment, watchdog from launch, and `WS_EX_NOACTIVATE` stops `InvokePattern` raising the window |
| 48 | Keep the window behind, not just put it behind | I | 46 | **done** — watchdog pushes back any window of the app that becomes foreground; off-screen measured and rejected |
| 49 | Make the quiet run the default | I | 48 | **done** — the MAUI FlaUI stack declares quiet by default; `BRINELL_BACKGROUND_MODE=0` asks for real input. Full suite with nothing set: 266 passed, 0 failed |

---

# Stage 0 — Independent of the bridge

These three depend on nothing and carry no spike risk. Step 3 in particular pays for itself
before any bridge code exists, because it *measures* what the later steps must cover.

### Step 1 — Occluded-window screenshots

**Why.** `GetScreenshot` uses `Capture.Element`, which is `CopyFromScreen` over a bounding
rectangle. Behind another window it captures that window instead. `AGENTS.md` says inspect
screenshots first when a UI test fails, so this silently misleads exactly when it matters most.

**Do.** Replace the capture path with `PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT)`, falling
back to the current behaviour if the result is blank. Try `Windows.Graphics.Capture` if
`PrintWindow` returns black — WinUI 3 composites through DirectComposition and this is the
known failure mode. Keep `Capture.Element` as the fallback rather than deleting it.

**Files.** `srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs` (`GetScreenshot`), plus a native
methods file.

**Verify.** A test that positions the app, covers it with another window, captures, and asserts
the image is not uniform and matches a control capture taken while frontmost.

**Done when** a screenshot of a fully occluded app window shows the app.

#### Result — done

`PrintWindow` with `PW_RENDERFULLCONTENT` **works on WinUI 3**, with no black-frame problem and
no need for `Windows.Graphics.Capture`. Verified by capturing the app under a full-screen black
topmost window: the capture is a complete, correct render of the page. `Capture.Element` is kept
as the fallback for minimized windows and any future GPU-composed content that returns blank.

Delivered: `srcnew/Brinell.Maui.FlaUI/WindowCapture.cs`, the `GetScreenshot` path in
`FlaUIMauiDriver`, and `testsnew/Brinell.Maui.UITests/Tests/Diagnostics/OccludedScreenshotTests.cs`
(excluded by name from the mobile head — the first such exclusion, and the mechanism step 10 will
reuse). Green three runs in a row.

Two things the spike taught, both now encoded in the test:

- **A settled-looking frame is not a rendered frame.** A freshly opened page paints its chrome
  before its content, and two consecutive captures of the unpainted state agree perfectly. The
  first version of this test compared an empty page against a full one and blamed the capture.
  `CaptureWhenSettled` therefore waits for a *change* and then stability, never stability alone.
- **A UIA element exists before its pixels do.** `WaitExists` on a control returned true while the
  client area was still blank, so element presence cannot gate a screenshot.

Also measured: with the window settled and visible, the rendered capture and the screen agree to
~91%, the shortfall being the DWM resize border that `GetWindowRect` includes and the content does
not. That number is reported by the test, not asserted.

#### Follow-up — the test became order-dependent in stage B. **Not yet fixed.**

`Screenshot_OfOccludedWindow_ShowsTheApp` now **passes alone and fails when run after other
tests**, reproducibly: the occluded capture matches the uncovered one by 16-18% where it needs
95%.

**Nothing about the capture mechanism changed.** What changed is the clock. `CaptureWhenSettled`
returns early if two consecutive frames agree for its one-second grace period, and *an unpainted
page is perfectly stable* — the step 1 result above records exactly this trap ("a settled-looking
frame is not a rendered frame", "a UIA element exists before its pixels do"). Stage B removed the
toolbar click from `MauiFixture.ReturnToHub`, navigation stopped costing seconds, and the captures
moved inside the window where the page is realised but not yet drawn. The test had been relying on
incidental slowness.

One fix was attempted and **did not work**: a discarded `CaptureWhenSettled()` after the page's
marker appears and before the occluder goes up, on the theory that it would sample the unpainted
frame first and so register the paint as a change. It still fails. So the diagnosis above is
consistent with the evidence but not confirmed, and the next person should re-establish it rather
than trust it.

Worth noting for whoever picks it up: the test's own remarks already explain why the visible
capture is taken *after* the occluded one, and the reasoning there is the same problem seen from
the other side. A fix probably needs a real "the content is painted" signal rather than another
ordering.

This is a defect in the test, not in `PrintWindow` or in the driver. It is recorded here rather
than fixed inline because it surfaced during stage B and is unrelated to it.

---

### Step 2 — Off-screen window placement

**Why.** Occluded is safe; minimized is not — a minimized WinUI window can stop laying out and
virtualized items may never realize. So the app needs somewhere to be that is out of the way
but still composed.

**Do.** Extend the existing `BRINELL_AUT_PLACE_RIGHT` mechanism with
`BRINELL_AUT_PLACE=right|offscreen|secondary`. `offscreen` moves the window to negative
coordinates via the Transform pattern; `secondary` targets a non-primary monitor when present
and falls back to `right`. Reuse `WriteAutPlacementReport` so a refusal is recorded rather than
silent.

**Files.** `srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs`
(`TryApplyRequestedWindowPlacement`).

**Verify.** `--filter "FullyQualifiedName~AutomationProbeTests"` with each placement value; the
suite must behave identically in all of them.

**Done when** the app can be placed off-screen and the probe tests still pass — proving layout
survives.

#### Result — placement done; `offscreen` gated on Stage B

`BRINELL_AUT_PLACE=right|offscreen|secondary` is implemented, with `BRINELL_AUT_PLACE_RIGHT=1`
still honoured. The report now names the placement and states `moved` or `clamped` by comparing
actual bounds against requested, rather than assuming the move took.

| Mode | Places correctly | Suite usable |
|---|---|---|
| `right` | yes | **yes** |
| `secondary` | yes — degrades to `right` on one monitor, and says so | **yes** |
| `offscreen` | yes | **not yet** — see below |
| unknown value | reported, nothing moved | n/a |

**Three findings, all measured.**

1. **UIA's Transform pattern refuses to move a window off the desktop.** It clamps, silently: a
   request for `x=-1216` came back at `x=0`. Transform is a semantic API and deliberately keeps
   elements reachable. Positioning the app under test is harness business, so the code falls back
   to `SetWindowPos`, which holds no such opinion. The verify-then-correct step is what caught it
   — the original code reported `moved` for a window that had not moved.

2. **A window entirely outside the desktop stops publishing its UIA tree** — `MissingRoot`
   everywhere, exactly as if minimized. An 8px sliver left intersecting the desktop keeps it
   composed and the tree alive. This is why the plan says *move it, don't minimize it*; it turns
   out "fully off-screen" is the same thing as minimized as far as WinUI is concerned.

3. **`offscreen` is blocked by physical input, not by placement.** `MauiFixture.ReturnToHub`
   activates the back button with `IMauiElement.Click`, a real mouse click at the element's
   clickable point. Off-screen there is no such point, so it clicks empty desktop and every test
   after the first is stranded. Probe tests: 1 of 3 off-screen, 3 of 3 on-screen.

**`InvokePattern` is not a safe drop-in** — worth recording, because it is the obvious fix.
Substituting it for the click made the off-screen combined filter pass 4 of 4 and the *on-screen*
one fail 4 of 4. Adding a click as a fallback after a failed invoke was worse again (3 failures in
4): once the invoke lands, the element is on its way out, and clicking it again hits whatever
replaced it. Reverted. Whatever replaces that click has to be the real semantic path — which is
what steps 13 and 14 build.

So `offscreen` waits on a semantic route for this one button. Step 3 then measured it precisely:
that click is the suite's *entire* physical-input footprint, and the Invoke pattern does not
work on a `ToolbarItem` — see [physical-input-inventory.md](physical-input-inventory.md). It is
nearer to steps 18 and 26 than to Stage B. `right` and `secondary` are usable today and already
keep the app out of the way.

Also fixed here: `FindBackToHub` polled in an unpaced loop, putting thousands of UIA round trips
through the app over its timeout. It now paces at 50 ms.

---

### Step 3 — Background-mode guard and inventory

**Why.** This is the enforcement mechanism, and before the bridge exists it is a measurement
instrument. With it on, every failure names a physical-input path the suite actually reaches —
which is the build order for steps 13, 14, 20–26, derived rather than guessed.

**Do.** Add `BRINELL_BACKGROUND_MODE=1`. When set, every physical-input entry point throws
`PhysicalInputRefusedException` naming the call site and the bridge verb that should replace
it. Cover `EnsureRootWindowFocused`'s `SetForeground`, `Mouse.*`, `Keyboard.*`, and
`Clipboard.SetText`.

**Files.** `srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs`,
`srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs`, a new exception in `Brinell.Core`.

**Verify.**
```powershell
$env:BRINELL_BACKGROUND_MODE = "1"
dotnet test testsnew\Brinell.Maui.UITests -v:minimal /nr:false
```
Expect **many failures**. That is the deliverable.

**Done when** the run produces a written inventory — which tests, which call sites, which verbs
— committed alongside this document. Nothing is fixed in this step.

#### Result — instrument done; inventory is a calibration run only

Instrument: `PhysicalInput` in `Brinell.Core`, guarding the Windows drivers — MAUI (15 entry
points), WPF (11), WinForms (10, including the keyboard-driven `DateTimePicker`). Covered by 16
platform-neutral tests in `Brinell.Core.Tests`, which exercise `Allowed`, `Audited` and `Refused`
through a scoped policy override; the gate spans three drivers, so testing it through any one
platform's UI suite would test the least of it.

`BRINELL_BACKGROUND_MODE` has two settings rather than the one specified here. `audit` records and
lets input proceed — verified not to change outcomes (183/24 either way) — so one pass gives
complete data. `1`/`strict` throws, which is the enforcement step 15 needs but aborts each test at
its first offending call. Auditing is the instrument; refusing is the gate.

**The inventory itself proves less than it first appeared.** It measures
`Brinell.Samples.Maui.App`: an app built to be automated, with an `AutomationId` on everything and
the automation handlers registered. Within that best case the suite reaches only four call sites,
396 of 400 uses trace to one raw click, and eleven entry points are never touched — including the
clipboard. **None of that generalises**, and an earlier version of this section wrongly used it to
downgrade steps 13 and 14. A real app has controls with no `AutomationId`, `ValuePattern`
implementations that refuse writes, context menus and hover-dependent UI; the eleven paths that
scored zero here are the ones it is most likely to hit. Steps 13, 14 and 20-26 keep the priority
the capability catalogue gave them until a real suite has been audited.

Two things the run does establish, because both are about Brinell rather than the app:

- **The activation ladder works.** Every control-object click went through a pattern; none fell
  back to the mouse.
- **A blocker no step covers: activating a MAUI `ToolbarItem` without the mouse.** Measured four
  ways — Invoke alone, Invoke with a click fallback, and the shared `ActivationHelper` ladder that
  works for every other control — all fail identically: Invoke reports success and does not raise
  the command, so navigation silently does not happen and the run degrades from 9 s to ~60 s. This
  is what keeps `BRINELL_AUT_PLACE=offscreen` unusable. Nearest existing steps are 18 and 26.

Full inventory, and the recipe for auditing a real app:
[physical-input-inventory.md](physical-input-inventory.md).

---

# Stage A — The bridge exists

## Result — done, and the architecture is proven

Full account, including the code that was written and the numbers behind each answer:
[stage-a-results.md](stage-a-results.md). The short version:

| Step | Answer |
|---|---|
| 4 | **Yes.** A child-HWND raw provider joins the tree, in a bare Win32 host and inside MAUI on WinUI, without disturbing the app's own tree. D-6 not needed. |
| 5 | **Yes, both.** Ints and UTF-16 strings round-trip out of process, surrogate pairs included. Two of seven hand-declared IIDs were wrong; they are now read off the registry. |
| 6 | **Raw yes, control no, content no.** D-5 stands. **`FindFirstDescendant` does not reach raw-view-only elements** — the client walks raw explicitly. |
| 7 | Frozen: `Invoke` at 0, `Exchange` at 1, no custom properties or events, verbs in append-only ranges of a hundred. Pinned by literal assertions in `ContractTests`. |
| 8 | `Brinell.Uia.Contracts` + `Brinell.Uia.Tests` — 43 tests, 0.9 s. |
| 9 | Folded into 11. The "single hardcoded child" would have been deleted by the next step. |
| 10 | **7 end-to-end tests green.** `SwipeDown` on `RefreshView` runs the full circuit; `Bridge_DoesNotDisturbTheExistingTree` passes. |
| 11 | `GestureAutomation.Verbs` / `.Sink`, weak-referenced registry keyed by window handle, two elements working independently. |
| 12 | `IMauiDriver.PerformGesture(automationId, gesture)`, `MauiGesture`, `GestureUnavailableException`, and `DescribeGestureBridge()`. Mobile head builds. |

**Four findings change later steps**, all in the results document:

1. `using:` in shared XAML does not reach a **referenced** assembly — an app that references
   AppSupport needs `clr-namespace:...;assembly=...`, one that copies it does not. Step 30.
2. Provider **property getters** must marshal to the UI thread, not only verb dispatch. Reading
   WinUI layout off-thread kills the app rather than throwing.
3. The **incoming page loads before the outgoing page unloads**, so an unregister must prove it
   is removing its own registration and not its successor's.
4. **`SwipeView.Open` reveals nothing to UI Automation.** Step 18's gesture surface must be
   backed by a `SwipeGestureRecognizer` with a command.

And the bridge found a real defect on its first run: a single pull-to-refresh counted twice in
`ContainerViewModel`, invisible until now because nothing on Windows could drive that gesture.

### Step 4 — Spike: child HWND raw provider

**Why.** The one unknown that can kill the design. `AutomationRemainingHandlers.cs` records
that touching this app's UIA tree collapsed it entirely; a sibling HWND is a different
mechanism, but the precedent says prove it.

**Do.** Throwaway branch. 1×1 `WS_CHILD` window parented to the MAUI window; answer
`WM_GETOBJECT` (`lParam == UiaRootObjectId`) with `UiaReturnRawElementProvider` returning a
minimal `IRawElementProviderSimple` with a distinctive `ClassName`.

**Verify.** Inspect.exe and a FlaUI probe both find it, **and**
`--filter "FullyQualifiedName~AutomationProbeTests"` still passes.

**Done when** both hold, or the alternatives (no `WS_VISIBLE`; owner-window instead of child)
are exhausted and D-6 becomes the design. Record the answer in this file either way.

---

### Step 5 — Spike: pattern round-trip, int and string

**Why.** `Interop.UIAutomationClient` has none of the registrar types — verified. All of it is
hand-written, and struct layout errors are the most likely source of lost time. The string half
decides step 7.

**Do.** Hand-declare `IUIAutomationRegistrar`, `UIAutomationPatternInfo`,
`UIAutomationMethodInfo`, `IUIAutomationPatternHandler`, `IUIAutomationPatternInstance`.
Register the same GUID in the sample app and an xUnit test. Round-trip **an int call and a
string call**.

**Verify.** The app observes both calls; the returned pattern ids differ between processes,
confirming ids are process-local and the GUID is the contract.

**Done when** both marshal cleanly, or the string limitation is documented with its cause.

---

### Step 6 — Spike: raw-view findability

**Do.** Set `IsControlElement = false` and `IsContentElement = false` on the step 4 provider.

**Verify.** `FindFirstDescendant(cf => cf.ByClassName(...))` still finds it; a control-view walk
does not.

**Done when** answered. On failure, D-5 falls back to `IsControlElement = true` with
`ControlType = Custom`, and the accessibility cost is recorded.

---

### Step 7 — Freeze the contract shape

**Why.** The method table freezes at registration. Changing it later means a new GUID and a
simultaneous breaking migration of the app under test and the test assembly. This is the only
genuinely irreversible decision in the programme.

**Do.** Decide and write down:
- `Invoke(int verb, int arg1, int arg2)` at index 0.
- `Exchange(int verb, string arg, out string result)` at index 1 — **recommended**, since steps
  14 and 20–26 all need strings.
- Rename `GestureKind` to `BrinellVerb`, with gestures as one numbered range. Values are wire
  values: append-only, never renumbered.

**Verify.** No code. A decision recorded in this file with its reasoning.

**Done when** the method table and verb ranges are written down and agreed.

---

### Step 8 — `Brinell.Uia.Contracts` and its unit tests

**Do.** The project as specified in Phase 1 of the gesture plan: GUIDs, verbs, the two COM
interfaces, registrar interop, `NativeTable` marshalling helpers, idempotent registration, the
pattern handler and client wrapper, HRESULTs, `GestureFailure`.

Ship the contract as **linked source files**, not a project reference, so
`Brinell.Maui.AppSupport` keeps its "copy me, don't reference me" property.

**Files.** `srcnew/Brinell.Uia.Contracts/**`, `testsnew/Brinell.Uia.Tests/**`.

**Verify.**
```powershell
dotnet test testsnew\Brinell.Uia.Tests -v:minimal /nr:false
dotnet build srcnew\Brinell.sln -v:minimal /nr:false
```

**Done when** GUIDs are asserted literally, the method table matches the `Dispatch` switch, and
registration is proven idempotent under parallel threads.

---

### Step 9 — Provider skeleton

**Do.** `BrinellGestureBridge` (HWND, `WM_GETOBJECT`, `Dispose` with `UiaDisconnectProvider`),
`BrinellGestureFragmentRoot` with a single hardcoded child, `BrinellGestureTargetProvider`, and
`GestureDispatcher` with UI-thread marshalling **and the timeout budget from the start** —
retrofitting a threading model is far harder than getting it right once.

**Files.** `samples/Brinell.Maui.AppSupport/Uia/**`,
`samples/Brinell.Maui.AppSupport/BrinellAutomationSupport.cs` (`UseBrinellGestureBridge`).

**Verify.** `dotnet build srcnew\Brinell.sln`; the bridge appears in Inspect.exe.

**Done when** the provider is visible and the app still starts normally.

---

### Step 10 — Walking skeleton: one gesture end to end

**Do.** No new sample page. One attached property on the **existing** `TestSwipeView`:

```xml
<SwipeView AutomationId="TestSwipeView" uia:GestureAutomation.Gestures="SwipeRight">
```

`SwipeRight`, not left — the items are in `SwipeView.LeftItems`, and a swipe *right* reveals
them. Dispatcher routes to `SwipeView.Open(OpenSwipeItem.LeftItems, animate: false)`.

The outcome is already wired: `SwipeDeleteItem` → `RecordCommand` → `Status` →
`ContainerStatusLabel` → `ContainerTestPage.Status`.

**Files.** `Views/ContainerView.xaml`, `MauiProgram.cs`,
`testsnew/Brinell.Maui.UITests/Tests/Gestures/`, `Pages/ContainerTestPage.cs`.

**Verify.**
```powershell
dotnet test testsnew\Brinell.Maui.UITests --filter "FullyQualifiedName~Tests.Gestures|FullyQualifiedName~AutomationProbeTests" -v:minimal /nr:false
dotnet build testsnew\Brinell.Maui.UITests.Mobile\Brinell.Maui.UITests.Mobile.csproj -v:minimal /nr:false
```

**Done when** the swipe test passes **and** `Bridge_DoesNotDisturbTheExistingTree` passes. The
latter is the §0.2 regression guard and is not deferrable. It is the one test that names FlaUI
types, so it also gets the `Exclude` entry in the mobile csproj, with a comment saying why.

**This is the moment the architecture is proven.** Everything after is breadth.

---

### Step 11 — Registry and attached property

**Do.** `GestureAutomation.Gestures` / `.Sink` attached properties; `GestureRegistry` with weak
references keyed by `AutomationId`; register on `Loaded`, unregister on `Unloaded`; live
`Navigate`; `BoundingRectangle` from the handler's `PlatformView`;
`UiaRaiseStructureChangedEvent` on change; a warning when gestures are declared without an
`AutomationId`.

Add `GestureAutomation.Gestures="SwipeDown"` to the existing `TestRefreshView` for a second
element. Update the now-false comments in `ContainerView.xaml` and `ContainerTestPage`.

**Verify.** Gestures filter, with two elements independently exercised; mobile head builds.

**Done when** two elements work independently and a removed element reports `ElementGone`.

---

### Step 12 — FlaUI client extensions

**Do.** `BrinellGestureExtensions` (`HasGestureBridge`, `SupportedGestures`, `InvokeGesture`,
`TryInvokeGesture`), `GestureUnavailableException` carrying element, verb and reason, and the
pattern-first / pointer-fallback ladder in `FlaUIMauiElement`.

Add the platform-neutral surface on `IMauiElement` now — shared tests compile into the Appium
head and must never name a FlaUI type.

**Files.** `srcnew/Brinell.Maui.FlaUI/Gestures/**`, `srcnew/Brinell.Maui/Interfaces/IMauiElement.cs`.

**Verify.** Gestures filter with the bridge, without it plus `BRINELL_ALLOW_POINTER_INPUT`, and
without either (expect the named exception). Mobile head builds.

**Done when** all three configurations behave as specified.

---

# Stage B — Work while the tests run

The payoff. Steps 13–16 are why the ordering puts these ahead of finishing gestures.

### Step 13 — Focus verb

**Why.** The linchpin. `EnsureRootWindowFocused` calls `SetForeground` before every physical
action, and its own remarks admit that without it "keystrokes meant for the app land in whatever
the user is doing". Foreground is a desktop-global resource; `VisualElement.Focus()` is not.

**Do.** Verbs `Focus`, `Unfocus`, `IsFocused`. Provider routes to `VisualElement.Focus()` on the
UI thread. `FocusForKeyboardInput` prefers the bridge and falls back to the existing ladder.

**Files.** `samples/Brinell.Maui.AppSupport/Uia/` (dispatcher),
`srcnew/Brinell.Maui.FlaUI/FlaUIMauiElement.cs`, `IMauiElement`.

**Verify.** A test that focuses a control while the app is **not** frontmost and asserts focus
landed, with the foreground window unchanged before and after.


#### Result — done

`Focus`, `Unfocus` and `IsFocused` on the provider; `SetFocus` and `Blur` prefer them on the
client. Covered by `Tests/Background/FocusVerbTests.cs`, 6 tests.

**How they prove it, and why the proof is unusual.** Asserting that focus landed says nothing
about *how* — the old path would pass the same assertion having stolen the foreground on the way.
So each test runs inside `PhysicalInputPolicy.Refused`, which turns every real mouse, keyboard,
clipboard and `SetForeground` call into an exception. **Passing is the evidence**: the alternative
would have thrown. `RefusedPolicy_StillRefuses` guards that the guard still bites.

**`FocusForKeyboardInput` deliberately does not use the verb.** Every caller of it is about to
send real keystrokes, and those need the foreground however the focus was obtained. Routing it
through the bridge would have looked tidier and put the keystrokes in the wrong window.

**Finding: `HasKeyboardFocus` is false on an occluded app.** Windows keyboard focus belongs to
the foreground thread, so a window that is not in front has no focused control as far as UI
Automation is concerned — whatever the app thinks. On the one configuration this stage exists to
support, every focus assertion would have been wrong. `IMauiElement.Focused` now reads UI
Automation first and asks the app only when UI Automation says no: a true is already the answer,
and a false is the rarer case worth a round trip.
**Done when** focus works on an occluded window without touching the foreground.

---

### Step 14 — Text input verbs

**Why.** `SendKeys` with `TextInputMethod.Paste` writes the **system clipboard** and sends
Ctrl+V. That destroys whatever the user copied, and two runs corrupt each other.

**Do.** Verbs `SetText`, `AppendText`, `ClearText`, `Submit` via `Exchange`. Provider sets
`Entry.Text` / `Editor.Text` / `SearchBar.Text`.

**Keep the physical path deliberately.** A test of input behaviour — `TextChanged` per
keystroke, `MaxLength` truncation, numeric-keyboard rejection — must still type.
`TextInputMethod.Keys` stays and means "test the input pipeline"; the bridge becomes the
default for *arranging* a field's contents.

**Verify.** Text set on an occluded window; a clipboard-canary test asserting the clipboard is
untouched after a full run.


#### Result — done

`SetText`, `AppendText`, `ClearText`, `GetText` and `Submit`, all on `Exchange`. Covered by
`Tests/Background/TextVerbTests.cs`, 6 tests, under the same refusal policy as step 13.

**Typing is kept and should stay kept.** A keyboard raises `TextChanged` per character, applies
`MaxLength` as it goes and lets a numeric keyboard refuse a letter; setting `Text` raises one
change for the whole value. `TextInputMethod.Keys` still means "type it". What moved to the
bridge is *arrangement* — getting a field into the state a test wants to start from.

The ladder that resulted is worth reading in order, because the bridge is not always first:

| Operation | Rungs |
|---|---|
| `SendKeys(SetValue)` | Value pattern → bridge → type |
| `SendKeys(Paste)` | bridge → clipboard + Ctrl+V |
| `Clear` | Value pattern → bridge → Ctrl+A, Delete |
| `Append` | bridge → type |
| `Submit` | bridge → Enter |

`SetValue` keeps the Value pattern first because both routes are semantic and the pattern is two
calls against an element already in hand. `Paste` puts the bridge first for a different reason:
the fallback is destructive.

**Finding: the clipboard is unreachable from an xUnit thread.** It is OLE and needs an STA; xUnit
runs MTA, so every `Clipboard` call throws `ThreadStateException` before touching anything. That
explains a line in the step 3 inventory that had looked like luck — `SendKeys(Paste)` scored zero
uses because it would have failed before reaching the clipboard. The canary runs its own STA
thread, which is the only way to make it a canary.

**Finding: a read-only field had to be refused explicitly.** The bridge writes a MAUI property,
and MAUI's setter does not enforce `IsReadOnly` — only the platform control does. Left alone, the
bridge would have been a way to put text in a field no user could type in. `ReadOnlyEntry`
declares the write verbs on purpose so the refusal is exercised: declaring a verb says the element
will be *asked*, not that it will agree.
**Done when** no test path writes the system clipboard.

---

### Step 15 — Background mode passes

**Why.** Steps 1–3, 13 and 14 converge here. This is where the claim becomes true rather than
nearly true.

**Do.** Work the inventory from step 3. Every remaining physical-input call is either replaced
with a bridge verb, or explicitly marked as intentionally physical (a test *of* input) and
excluded from background mode by attribute rather than by omission.

**Verify.**
```powershell
$env:BRINELL_BACKGROUND_MODE = "1"
$env:BRINELL_AUT_PLACE = "offscreen"
dotnet test testsnew\Brinell.Maui.UITests -v:minimal /nr:false
```

**Done when** the suite passes at baseline in background mode, and you can type in Visual Studio
throughout without the cursor moving or focus being taken. Test it by doing exactly that.

> **That last check has never been performed, and a person cannot perform it in CI.** Step 46 in
> [plan-the-quiet-run.md](plan-the-quiet-run.md) replaces it with a foreground watchdog, which is
> the only part of this programme's claim still taken on trust.

#### Result — the physical-input claim is proven; the suite is not yet stably green

**Measured, strict background mode, serial:**

```powershell
$env:BRINELL_BACKGROUND_MODE = "1"
dotnet test testsnew\Brinell.Maui.UITests -v:minimal /nr:false `
  --filter "PhysicalInput!=Deliberate&PhysicalInput!=Pending" -- xUnit.ParallelizeTestCollections=false
```

| Run | Failed | Refusals |
|---|---:|---:|
| First pass, nothing excluded | 7 of 236 | **5** |
| After the work below | 4 of 232 | **0** |

**Zero `PhysicalInputRefusedException` across the whole suite.** That is the claim this step
exists to make, and it holds: nothing the suite does needs the mouse, the keyboard, the clipboard
or the foreground window — except the four tests that say so in their own source.

The first pass found five refusals in five tests. Two of the seven failures were cascades, not
refusals: a test that dies mid-page leaves the app on that page, and the next test's navigation
reports the damage.

**One was a real gap and is fixed.** `DatePicker_Focus_IsReported` fell through to a pointer
click, because `TestDatePicker` declared no verbs. It now declares `Focus,Unfocus,IsFocused`.

Worth recording how it hid: `FocusableControlBase.FocusCore` calls `SetFocus()`, which returns
`bool` and swallows the refusal its fallback threw — so the failure surfaced one level down as a
*click* being refused, naming neither focus nor the DatePicker. Another instance of step 34: an
action returning a bool turned a precise failure into a vague one.

**Four are excluded by declaration**, which is what this step asked for — by attribute, not by
omission. `PhysicalInputTrait` carries two values, because they say different things:

| Test | Value | Why |
|---|---|---|
| `BackToHub_Click_ReturnsToTheHub` | `Deliberate` | exercises the click fallback on purpose |
| `BackToHub_IsAbsentAtTheHub` | `Deliberate` | same |
| `ByName_DeletesTheRightRow` | `Pending` | wheel scrolling; step 21 removes it |
| `SearchByContent_FindsTheRightRow` | `Pending` | same |

`Deliberate` is permanent: the fallback is a real feature — it is what every platform without the
bridge uses, and something must exercise it. `Pending` is a work list. One marker for both would
quietly turn the second into the first.

#### Still open — four failures that are not about physical input

**This step is not done.** The suite passes in background mode on every count this step is about,
and still has four failures that it is not about:

- `ImageButton_Tap_ExecutesCommand` and `Button_Reset_ClearsStatus` — both `Could not get back to
  the hub … popped through the bridge`: the pop succeeded and the hub did not appear within ten
  seconds. **Order-dependent, not background-specific** — the Buttons area passes 3 of 3 in
  background mode when run alone. This is the third appearance of this symptom and the cause is
  not yet established.
- `TimePicker_CombinedWithDate_WorksTogether` — `Could not set time 15:30:00 … the control reports
  '03:30:00' after Accept`. A 12-versus-24-hour fault in the TimePicker's own ladder, unrelated to
  this step; step 20 territory.
- `TopButton_Click_AfterScrollingToTheBottom_UpdatesStatus` — not yet diagnosed.

The step's own "done when" also includes typing in Visual Studio throughout a run without the
cursor moving or focus being taken. **That has not been done**, and it is the check that matters
most to whoever has to live with this — it should be performed by a person before this step is
called finished.

---

### Step 16 — Windows-only parallelism

**Do.** Replace the assembly-wide `[assembly: CollectionBehavior(DisableTestParallelization =
true)]` with per-collection control. Windows collections may run in parallel in background mode;
the Android collections stay serialised — two Appium sessions still share one emulator, which is
the half of that comment that remains true.

**Files.** `testsnew/Brinell.Maui.UITests/AssemblyInfo.cs`, `MauiCollection.cs`,
`ShellCollection.cs`.

**Verify.** Two collections running concurrently against two app instances, green, twice in a
row. Run it three times — a parallelism bug that appears once in three is still a bug.

**Done when** two apps under test run simultaneously and pass.

#### Result — done

Two apps, two collections, one run:

```
[FIXTURE] ShellFixture #1 CREATING at 12:33:58.344
[FIXTURE] MauiFixture  #2 CREATING at 12:33:58.344
Hub:   own window 7737598,  other app's window 35000828, stays overlapped.
Shell: own window 35000828, other app's window 7737598,  stays overlapped.
```

Green three times in a row on `Tests.Buttons` plus the two new classes, 28s each.

**The code was already written; what was missing was any way to tell whether it worked.** The
assembly attribute, `DesktopLease` and both fixtures' leases had been in place since the stage B
work, and nothing executed either of the two branches in a way that would notice if they stopped
working. Two things now do.

**`DesktopGateTests` — 4 tests, 386ms, no app.** The lease's mechanism moved out of the static and
into `DesktopGate`, which a test can have its own instance of. That matters for one branch in
particular: *serialising* is the path every background-mode run skips, so the code protecting
whoever runs the suite in the foreground was being shipped unexecuted. It now has the second
collection waiting, the handover after release, the double-dispose guard and the leaked-lease
message pinned, all against a gate that nobody else is holding.

**`HubCollectionParallelismTests` / `ShellCollectionParallelismTests` — the pair.** One in each
collection, each asking whether the other app was up while it was. They compare *window handles*,
so "two fixtures" is shown to be two apps rather than assumed. Note what this gave the Shell
collection: every one of its other tests is skipped under step 32, so before this the second
collection had nothing to run and the parallelism had nothing to be parallel with.

**One design mistake, worth keeping.** The first version asked "is the other app up *right now*",
and it failed - twice, in a way that looked exactly like the feature being broken:

```
Waited 60s for the other collection's app and it never came up.
```

The fixture timestamps said otherwise: both apps started in the same millisecond. The Shell
collection is a single test, so it was gone 2.3 seconds later, while the hub side's test was
scheduled 80 seconds into a 2m24s collection. Both statements were true - the apps did overlap,
and the other app was not up at that instant. **The claim is about intervals, so the probe has to
record intervals**; `ParallelismProbe` keeps each app's arrival and departure and asks whether the
two stays overlapped. It is now order-independent, which a pair of tests in two collections of
wildly different lengths has to be.

That is also the general lesson from this step: *a signal that is set and never cleared answers
"did it ever happen"; a snapshot answers "is it happening now"; neither is the question.*

**Not verified:** the `Allowed` branch end to end, i.e. an actual foreground run of both
collections. It asserts (no overlap may be recorded) but nobody has run it, deliberately - that
run takes the keyboard for its duration. `DesktopGateTests` covers the mechanism.

---

# Stage C — Finish gestures

### Step 17 — Gestures sample page

**Do.** New page via one `SamplePageEntry`; the hub builds itself from `All`, so no navigation
wiring. Rows: `GestureSwipeTarget` (`SwipeGestureRecognizer`), `GestureDoubleTapTarget`
(`TapGestureRecognizer`, 2 taps, `Command`), `GestureLongPressTarget`
(`PointerGestureRecognizer`), `GestureSinkTarget` (`IBrinellGestureSink`),
`GestureNoDeclarationTarget` (no attached property — the negative case).

**Files.** `Navigation/SamplePage.cs`, `Navigation/SamplePages.cs`, `Pages/GesturesPage.xaml`,
`Views/GesturesView.xaml`, `ViewModels/GesturesViewModel.cs`, `Pages/HubPage.cs`,
`Pages/GesturesTestPage.cs`.

**Verify.** The page opens on Windows **and** Android; mobile head builds.

#### Result — done

`GesturesPage` / `GesturesView` / `GesturesViewModel`, seven rows, opened from the hub. Both
heads build; `GesturesPageTests` is green in 0.8s.

Seven rows rather than the five planned, and the two extra are the ones that assert an absence:

| Row | Answers | Why it is there |
|---|---|---|
| `GestureTapTarget` | `Tap` | one recognizer, one command - the simplest binding |
| `GestureDoubleTapTarget` | `DoubleTap` | same recognizer *type*, different meaning |
| `GestureSwipeTarget` | `SwipeLeft`, `SwipeRight` | one recognizer, two directions, flags enum |
| `GestureLongPressTarget` | `LongPress` (sink) | MAUI has no long-press recognizer |
| `GestureSinkTarget` | `Pan` (sink) | `SendPan` is internal; carries arguments |
| `GestureNoDeclarationTarget` | nothing | has a working recognizer and declares nothing |
| `GestureUnbindableTarget` | nothing | declares `DoubleTap`, carries a one-tap recognizer |

The last row did not exist in the plan and is the one step 18 is verified by: it is the case where
markup claims something the element cannot do.

`EveryRow_StartsUntouched` reads all seven before anything is sent. That looks like ceremony and is
not: two of the later assertions are *absence of change*, and an assertion like that passes just as
well when the label was never found.

---

### Step 18 — Full gesture vocabulary, bound at publish time

**Rewritten.** This step used to say *"Ladder: sink -> public MAUI API ->
`TapGestureRecognizer.Command` -> `UIA_E_NOTSUPPORTED`"*, which is the activation ladder again,
one layer down and in the other process. See
[design-gesture-dispatch-binds-at-publish.md](../fix/design-gesture-dispatch-binds-at-publish.md)
for the whole design; the summary is that the app already declares what each element answers, and
the dispatcher should use that declaration instead of re-deriving it at every call.

**Do.**

- `VerbBindings.Resolve(element, declaredVerbs, sink)`, called once from `Publish`: each declared
  verb resolves to exactly one handler, or to nothing and is reported by name at startup.
  Dispatch becomes a dictionary lookup, and `MauiVerbTarget.Capabilities` becomes the bound verbs
  rather than the declared ones - so `GetCapabilities` stops being able to lie.
- Delete the four rungs in `MauiVerbDispatcher.PerformInvoke`, **including the two `when` clauses
  that perform a gesture in order to decide whether they match**. `case BrinellVerb.Tap when
  MauiCapabilities.TryTap(element, 1):` runs the app's command as its own guard and falls through
  on false, having already acted.
- `IBrinellGestureSink` declares `Verbs` and owns them. No `bool`, no "first refusal" - that
  return value is the `Try` that step 34 is about.
- **Six of the nine gesture verbs bind**: `Tap`, `DoubleTap` and the four swipes. `Pan`, `Pinch`
  and `LongPress` have no binding and are reachable only through a sink. `SendPan` and `SendPinch`
  are internal in MAUI 10 and nothing here reflects into MAUI internals; `LongPress` has no MAUI
  surface at all, and binding it to `PointerPressedCommand` would hand a test a press while
  telling it that it got a hold. The table says so out loud instead of letting all three fall
  through to a refusal that looks like a typo.
- `MauiCapabilities.cs` still names every public MAUI API used, so a MAUI upgrade is a compile
  error in one known file. **The `Try` prefix leaves it entirely** - `TryTap`,
  `TrySwipeRecognizer`, `TryOpenSwipeView` and `TryStartRefresh` split into nullable-returning
  questions asked once at bind time (`TapRecognizer`, `SwipeRecognizer`) and void-returning
  commands at call time. Not one half wants the word: a nullable return already says "may be
  absent", and `Try` is only earned where a throwing twin needs telling apart, as `FindElement`
  does for `TryFindElement`.

**Files.** `samples/Brinell.Maui.AppSupport/Uia/{VerbBindings,MauiVerbDispatcher,MauiVerbTarget,
IBrinellGestureSink,BrinellBridgeHost,MauiCapabilities}.cs`.

**Verify.** Gestures filter; `dotnet test testsnew\Brinell.Maui.Tests`. `Resolve` is a pure
function of an element and a declaration, so the whole table is unit-testable with no window -
including the inverted swipe-to-`OpenSwipeItem` mapping, which is the thing most likely to be
"fixed" backwards. Check that a row declaring a verb it cannot perform is named on the debug
output at startup, not by a failing test.

#### Result — done

19 tests green in the Gestures filter, and the report the whole redesign was for:

```
'GestureUnbindableTarget' [Border] declares DoubleTap but nothing can perform it:
no TapGestureRecognizer with NumberOfTapsRequired=2 and a Command. The verb was not published.
'GestureUnbindableTarget' declared 1 verb(s) and bound none, so it was not published.
```

**What landed.** `VerbBindings.Resolve` runs once from `Publish` and returns a `VerbPlan`;
`MauiVerbTarget.Capabilities` is now the *bound* verbs, so `GetCapabilities` cannot advertise
something the element will refuse. `MauiVerbDispatcher.PerformInvoke` is a lookup followed by a
switch over the ranges that have not moved into the table yet - a switch on the verb is dispatch;
it was the switch on *mechanism* that was the ladder. `IBrinellGestureSink` declares `Verbs` and
owns them, with no `bool` anywhere.

**Both `when` clauses are gone.** `case BrinellVerb.Tap when MauiCapabilities.TryTap(element, 1):`
ran the app's command as its own guard and fell through on false, having already acted. That was
the worst thing in the file and it is worth recording that it was found by writing the design
document rather than by a failing test.

**`Try` left `MauiCapabilities` entirely.** `TryTap`, `TrySwipeRecognizer`, `TryOpenSwipeView` and
`TryStartRefresh` became nullable-returning questions asked at bind time (`TapCommand`,
`SwipeCommand`, `SwipeItemFor`) and void commands at call time (`OpenSwipeView`, `StartRefresh`).
Not one half wanted the prefix. `RefreshView`'s "already refreshing" case, which looked like the
strongest argument for keeping a bool, is a call-time question the handler asks directly and
answers with `S_FALSE`.

**Commands are captured weakly**, and that is load-bearing rather than fastidious:
`MauiVerbTarget` holds its element weakly so an unwithdrawn registration cannot pin a page and its
view model for the life of the window, and a binding capturing a recognizer strongly would undo
that through `Parent`.

**Six verbs bind; three need a sink.** `LongPress` joined `Pan` and `Pinch` during implementation.
MAUI has no long-press recognizer, and the obvious binding -
`PointerGestureRecognizer.PointerPressedCommand` - is wrong: pressing is not holding, an app that
starts a timer on press and cancels on release would never see a long press, and the test would be
told it got one.

**Not done: headless unit tests over `Resolve`.** The design document promised them and it was
wrong to. `Resolve` takes MAUI types, and MAUI 10's `Microsoft.Maui.Controls` is a metapackage
with no plain `net10.0` assembly at all - `Brinell.Maui` and `Brinell.Maui.Tests` both target
plain `net10.0` and reference no MAUI, so there is nowhere in this repo such a test could live. It
needs a `net10.0-windows10.0.19041.0` test project with `UseMaui`, which is its own piece of
infrastructure. The table is verified through the page instead, which is slower and tests the real
thing.

---

### Step 19 — Gesture control objects

**Do.** `SwipeView` implements `ISwipeableControlObject<TScope>` on Windows; `RefreshView` gets
`PullToRefresh()`. Move the tests from raw verbs to control-object members. Update the
`ISwipeableControlObject` doc comment — it says "primarily used for mobile platforms", which
this step makes false. Add a gesture-bridge row to `AutomationProbeView`.

**Verify.** Gestures filter; full MAUI UI suite against the established baseline.

#### Result — partly

**Done.** `SwipeView`'s four swipe Cores and `RefreshView.PullToRefreshCore` now name the gesture
(`element.PerformGesture(MauiGesture.SwipeDown)`) instead of calling a pointer swipe - the same
split as `Invoke`/`Toggle`/`Select`, where the control names the operation and the element decides
how its platform performs it. `AppiumMauiElement` implements `PerformGesture`/`SupportsGesture`,
so the mobile half of that split exists rather than being implied. `ISwipeableControlObject`'s
"primarily used for mobile platforms" is corrected. `AutomationProbeView` has its gesture-bridge
row, following the page's own `Probe{Type}` / `Probe{Type}Child` rule so a test can tell "the
bridge element exists" from "the verb reached the app and did something".

**And the `Try` prefix left the swipe extensions.** `TrySwipeLeft`, `TrySwipeRight`, `TrySwipeUp`,
`TrySwipeDown`, `TrySwipeRelative` and the private `TrySwipe` returned a `bool` that was `true`
whenever the element was non-null - a return value carrying no information at all, and the purest
case the rule covers.

**Not done: moving the gesture tests onto control-object members.** It cannot be done for
`SwipeView` and `RefreshView` as the framework stands, and the reason is measured rather than
assumed - `GestureAddressabilityTests` now pins both halves:

- the gestures page's rows **are** findable on Windows (they are `Border`s, and the app registers
  the Brinell automation handlers), so a control object is possible for them;
- `TestSwipeView` is **not** findable and answers its verb anyway.

Every generated member calls `RunDoWithElement`, which finds the element first, so a control
object for an element nothing can find would have to be members that do not look for one. That is
a change to what `Brinell.Generator` emits, not to any control file. The generator round-trips
cleanly (`dotnet run --project tools/Brinell.Generator.Cli -- --input <file>.tpl.cs` reproduces a
checked-in `.gen.cs` byte for byte), so the change is tractable - it is just not a five-minute one,
and it wants its own step rather than being wedged into this one.

**Where to start:** an attribute on the Core method - or a second Core shape - meaning "address
this by id, do not find an element", emitting a member that calls
`Scope.Context.Driver.PerformGesture(AutomationId, gesture)`. That is the one route that reaches a
control Windows cannot see, and it is already the driver's documented purpose.

---

# Stage D — The rest of the catalogue

Each step is self-contained: add verbs to the dispatcher, add client methods, add tests. Order
within the stage is by value, and any of them can be pulled forward on demand.

### Step 20 — Date and time verbs

Retires the largest workaround in the repo: `DatePicker.SetDateCore`'s three-rung ladder ending
in "Could not set date without the pointer", and `TimePicker`'s flyout navigation.
`TimePicker.tpl.cs:106` says the root "publishes no patterns at all on Windows". Verbs
`SetDate`, `SetTime`, `OpenFlyout`, `CloseFlyout`; provider sets `DatePicker.Date` /
`TimePicker.Time`. Keep the ladder as the uninstrumented fallback. Both controls are in the
known-failing baseline — **establish it before claiming a fix**.

#### Result — done, and step 37 with it

`Tests.DateTimes` is **20/20 green, nothing skipped**, from a baseline where both controls were
known-failing and one test was parked under step 37.

**The provider side is two properties.** `SetDate` sets `DatePicker.Date`, `SetTime` sets
`TimePicker.Time`, both on Exchange, both returning what the control then holds. What that
replaced on the client was roughly two hundred lines of WinUI calendar and clock navigation -
open the flyout, read the header, page to the month, find the day, select it, accept. The app
could always just say so.

**`SetDateCore` is now one question and one route.** `element.SupportsSetDate` is asked once; if
the app declares the verb it is used, and if not the calendar is walked. That is not the ladder
with better manners - nothing is performed to find out which route is right. Two of the three
old rungs were dead on the only platform that runs them: WinUI advertises a Value pattern and
refuses the write, and a `CalendarDatePicker` hosts no text to type into. So every call paid for
two failures to reach the one that worked, and a real breakage in the calendar route reported
"could not set the date" with three suspects.

**Step 37 is fixed by absence.** 15:30 read back as 03:30 because the WinUI hour list is a
12-hour clock and the AM/PM half was lost walking it. Setting `TimePicker.Time` has no 12-hour
clock in it to lose. `SetTime_KeepsTheAfternoonHalfOfTheClock` pins it, because a fix that
consists of a mechanism no longer existing is exactly the kind that regresses unnoticed.

**Two things worth writing down.**

`DatePicker.Date` is `DateTime?` in MAUI 10, and `TimePicker.Time` is `TimeSpan?` — easy to miss,
because the pickers always show something. An empty string is the wire reading for a picker
holding no value.

Two `DatePickerTests` broke and were right to: they asserted on the phrase `"Could not set date"`
while the behaviour they were about — a clamped date is refused, not silently accepted — never
changed. They now assert on the date and the clamp, which is what the test is for.

**`OpenFlyout` / `CloseFlyout` are not implemented for the pickers.** MAUI exposes no public way
to open either flyout, and nothing here reflects into internals — the same call the gesture table
makes for `Pan` and `Pinch`. A test that genuinely means "the flyout opens" still drives the
control through its own affordance.

### Step 21 — Scroll verbs

`Swipe` currently substitutes mouse-wheel clicks, an unquantified unit, with a stuck-detection
loop because there is no completion signal. Verbs `ScrollTo`, `ScrollToIndex`,
`ScrollPosition`; provider calls `ScrollView.ScrollToAsync` / `CollectionView.ScrollTo`. Both
are cross-platform MAUI APIs, so the same verb works on Android — the biggest parity win in the
catalogue.

#### Result — done

6 tests green in `ScrollVerbTests`, and the two `Pending` physical-input traits step 15 opened
against this step are removed - `ByName_DeletesTheRightRow` and `SearchByContent_FindsTheRightRow`
now pass under `PhysicalInputPolicy.Refused`, which they could not do while the search was turning
a wheel.

**Three verbs, each bound where MAUI actually offers it.** `ScrollTo` and `ScrollPosition` are
`ScrollView`; `ScrollToIndex` is `ItemsView` and `ListView`. Nothing is bound to a control that
cannot honour it, so the step-18 report catches a misdeclaration here the same way it does a
gesture.

**`ScrollPosition` returns six numbers, not a percentage.** UI Automation offers a scroll percent,
and a percent cannot answer what tests actually ask: *"are we at the bottom"* is true both for a
long list scrolled to its end and for a page too short to scroll at all, and those are different
facts about the app. Offset, viewport and content size together separate them, which is why
`ScrollVerbTests` can assert `CanScrollVertically` as a precondition - a scroll page that had
quietly become short would otherwise pass every test below it.

**The stuck-detection loop is gone from the path that had it.**
`CollectionObjectBase.TryMaterializeMore` now asks `SupportsScrollToIndex` once and takes one
route. The old one scrolled a wheel click - an unquantified unit, meaning whatever the OS and the
control between them decide - then re-read the realized item count and guessed whether anything
had happened. An index is definite, and an index past the end is a quiet no rather than an error,
so the loop terminates on an answer instead of on a lack of change.

**One assertion was wrong before the code was.** `ScrollTo_PutsTheNamedElementOnScreen` first
asserted that scrolling back to the page's first element returned the offset to zero. It does not:
`ScrollToPosition.MakeVisible` scrolls the *minimum* needed, so the element lands at the top of the
viewport rather than the viewport at the top of the content - short by the stack layout's padding.
Visibility is what the verb promises, so visibility is what the test now asks.

---

### Step 22 — Navigation verbs

`NavigateBack` sends desktop-wide **Alt+Left**; `Refresh` sends **F5**; both swallow
`Win32Exception` and may silently no-op. Verbs `NavigateBack`, `NavigateTo`, `CurrentRoute` →
`Shell.Current.GoToAsync` / `Navigation.PopAsync`.

#### Result — done

4 tests green in `NavigationVerbTests`, in 2 s. Verbs `NavigateBack`, `NavigateTo`,
`CurrentRoute`, plus `GetState("NavigationDepth")`.

**The bool became four answers.** `NavigateBack` now returns `S_OK` (popped), `S_FALSE` (at the
root, nothing to pop), `UIA_E_ELEMENTNOTAVAILABLE` (this target is a page that was popped long
ago) and `UIA_E_NOTSUPPORTED` (not a page). The client acts differently on each. That is step
34's rule applied to the one case where the bool provably *caused* a defect rather than merely
permitting one.

**Step 33A is fixed, and the 2 s grace is deleted.** It polled whenever the verb did not answer
`S_OK`, guarded on "does this app have a bridge" - always true - so it fired on the commonest
answer of the four, *we are already at the root*. Every fixture reset starting at the hub paid
it. The race it was added for is real and is now distinguishable: a stale target says
`UIA_E_ELEMENTNOTAVAILABLE`, the root says `S_FALSE`, and waiting is the right answer to one and
wrong for the other.

**The hub now publishes itself.** It declared nothing, so at the root - the exact moment "are we
at the root" is asked - nothing could answer. That was the open item at the end of
`rca-navigation-tests-stall.md`.

**`Refresh` stopped sending F5.** It was desktop-wide keyboard input landing wherever the
foreground happened to be, swallowing its own failure, and doing nothing at all in the common
case. It re-navigates to the current route, and fails loudly on an app that has no routes rather
than silently on every app.

**A test lost forty lines of polling, and the polling was load-bearing.** The old
`NavigateBack_AtTheHub_ReportsNothingToPop` had to establish a fact by *going back* and seeing
what came out, so every reading changed what was being read - it failed about one run in three.
A question does not move the app, so `IsAtNavigationRoot_KnowsWhichEndOfTheStackTheAppIsOn`
asserts the fact directly.

**Step 36 is NOT fixed, and I said it was.** Seven of its tests passed three narrow runs in a row
with the skips removed, which I took as sufficient; a wider run failed a different test each
time. The skips are back. What this step did remove is one *contributor* - the fixture could not
previously tell "nothing to pop" from three real failures - but the flake outlives it.

**Not attempted: unwinding a deeper stack in `ReturnToHub`.** A stack deeper than two leaves a
page open after one pop, and the next `Open` clicks a hub button still findable *underneath* it,
pushing another page - so one failure deepens the stack and guarantees the next. A pop-until-root
loop was written and made things worse, and it was reverted. It is a test helper, and it has
already had more time than it is worth; whoever picks up 36 should start from the app's reported
depth rather than from this.

---

### Step 23 — State-read verbs

The assertions that currently pass for the wrong reason. `Image` — "loaded if it occupies
space", though a failed image still occupies space; expose `Source` and `IsLoading`.
`ProgressBar` — WinUI 0–100 normalised against MAUI's 0–1; expose `Progress` directly.
`IsVisible` vs UIA `IsOffscreen` — report both, since disagreement is often the real bug.
`IsIdle` — a real drained-dispatcher signal beats a sentinel element, and squarely serves
`AD-004`.

**Boundary.** `BindingContext` and arbitrary property reflection stay out. Read what a user
could perceive; if the assertion needs the view model, it belongs in `Brinell.Maui.Tests`.

#### Result — done

4 tests green in `StateReadTests`; Display 15/15. Reads `Source`, `IsLoading`, `Progress`, and a
whole-app `IsIdle`.

**The headline is an assertion that was true for the wrong reason.** `Image.IsLoadedCore` was "the
element occupies space", and the layout reserves that box whether the bitmap arrives or not - so
it was equally true of an image whose source names a file that does not exist. The test asserting
an image had loaded was asserting that MAUI had done arithmetic. `DisplayView` now carries a
`BrokenImage` row for exactly this, and `ImageSource_SeparatesABrokenImageFromAWorkingOne` asserts
both that the two are now distinguishable *and* that they still occupy space alike - so the test
keeps demonstrating why the old check could not work.

**`Progress` lost its second definition.** The client read the UIA range pattern, where WinUI
reports 0-100, and rescaled against the reported minimum and maximum. The arithmetic is right and
is also a second home for what "progress" means: a platform reporting a different range, or none,
quietly changes the number. The app holds one value and now hands it over.

**`IsIdle` is on the driver, not on an element**, and the first attempt had it on the element -
which meant every control that wanted to be waited on had to declare a verb about the app's
dispatcher. A dispatcher belongs to the app and every element gives the same answer.

**A deadlock I wrote and then measured.** `IsIdle` posts to the dispatcher and waits for the
callback - that is what "everything queued before now has finished" means. Run where every other
verb runs, on the UI thread, it was waiting for a post it was itself blocking, and it hung until
its budget expired. It presented as *"no element published on the app's bridge answers IsIdle"*,
which is a plausible-looking declaration problem, and the bridge log disproved that in one line by
showing the verb published. The dispatch is now started from the calling thread, above the
marshalling.

**The boundary held.** `ReadState` takes named properties that the provider has cases for, so
`BindingContext` is refused rather than answered with an empty string - a read that returned blank
for an unimplemented name would let an assertion compare two blanks and pass.

**One caveat.** In a wide filter, `ProgressBar_DecreaseProgress_UpdatesValue` fails at 1 ms while
passing in its own area 15/15. That is step 36 again - a different test each run - and it is
parked.

---

### Step 24 — Picker verbs

Picker tests are 0/8 on Android before any change — one root failure and seven cascades. Verbs
`SelectIndex`, `SelectByText`, plus a separate `OpenFlyout` so a test that genuinely means "the
flyout opens and shows these items" can still say so.

**Result.** `SelectIndex` and `SelectByText` on the app side; `SelectedIndex`, `SelectedItem`,
`ItemCount` and `Items` added to `GetState`. `SelectorControlBase` asks
`SupportsSelectIndex` / `SupportsSelectByText` before choosing a route, and `Picker` overrides the
four reads. 15 passed in the Selection area, 44 in the whole Background stage.

**What the dropdown route was costing.** Every question a test asked a picker was answered by
opening its popup: expand, poll up to two seconds for the popup's items to reach the
accessibility tree, act, collapse. Selecting a value therefore performed a flyout journey on the
way past, and a test that genuinely meant "the flyout opens and shows these items" could not be
told apart from one that only wanted the value changed. `OpenFlyout` / `CloseFlyout` /
`IsFlyoutOpen` on the control object are what that half becomes; they stay on the ExpandCollapse
pattern rather than becoming verbs, because MAUI has no public API to open a Picker's dropdown
and an app could only answer such a verb by reaching into WinUI.

**A read that changed what it read.** `GetItemTexts` expanded the popup to count what was in it,
so asking twice in a row was two different journeys through the app. Worse, the popup realizes
only the items it is showing: on a picker of two hundred, the item list came back as the visible
handful and the selected index — derived by finding the selected text in that short list — was
wrong or null for anything below the fold. Neither read failed. `LongPicker` in the sample makes
that concrete, and the test asserts the contrast rather than describing it: it opens the dropdown,
counts what the popup realized, and fails if a platform ever renders all two hundred.

**`OpenFlyout` is deliberately not a verb**, and the plan's wording above assumed it would be.

### Step 25 — Dialog reads

`Exchange(CurrentAlert)` returning title, message and buttons, so a test asserts *what was
asked* rather than that something was dismissed. `DismissAlert(result)` second, and used
sparingly: if the test is about a user confirming a destructive action, clicking the real button
is the point.

**Result.** `CurrentAlert` on the app side, `IMauiDriver.CurrentAlert()` returning an
`AlertContents`, and `ContentDialog.GetTitle()` / `GetButtonTexts()` / `GetMessage()` on the
control object. Six new tests; the Dialogs area is 9/9 across three consecutive runs.

**Who knows what, measured before deciding.** A probe printed what a WinUI dialog publishes:

```
root Name='Confirm'
  [text] Name='Confirm'          <- the title, again
  [text] Name='Proceed?'         <- the message
  [button] Name='Yes' Id='PrimaryButton'
  [button] Name='No'  Id='SecondaryButton'
  [pane] Id='ContentScrollViewer'
```

So the title is the dialog's own accessible name and each button carries its text - both read
straight off the platform, with nothing required of the app. The message is the exception: it
sits in the content area beside a second copy of the title, so from outside it can only be
identified as *the text that is not the title*, which is silently wrong for an alert whose
message and title read alike. **The split follows: the platform publishes who is asking and what
the choices are; only the question itself needs the app.**

**And the app has to be asked at the call site.** There is no supported way to observe
`DisplayAlert` from outside it - MAUI signals its own platform layer through `MessagingCenter`,
which is `internal` in MAUI 10 (checked by compiling against it, not assumed), and reflecting
into it is the failure mode already ruled out for `Pan` and `Pinch`. So the app raises its alerts
through `BrinellAlerts`, one line per call site, and that is a real cost: an app that cannot be
modified keeps the title and the buttons and loses the message.

**A race that only a wide run showed.** The first combined run reported a *prompt* while nothing
was open. The app clears its record when `DisplayAlert`'s await resumes - a continuation queued
on the UI thread, and therefore some moments after the dialog has already left the tree - so a
test that dismissed one and immediately asked was told about the dialog it had just closed. The
fix is the same boundary again: the client asks the screen whether a dialog is up before it asks
the app what it says, and each end answers only what it can see. The test now asks with nothing
in between, so it exercises the window rather than waiting it out.

**`DismissAlert` was not built, and the plan half expected that.** It cannot be answered by the
app: `DisplayAlert`'s task completes when the platform dialog closes, so the app cannot complete
it without pressing the button, and pressing the button is what `ContentDialog.DialogButton(text)
.Click()` already does through the Invoke pattern - no physical input, and the thing a user does.
A verb would be a second name for it.

**Not changed:** the three existing `ContentDialogTests`. They are about the dismissal routes and
say so; the question is a separate concern and has its own class.

### Step 26 — Menu and flyout verbs

Context menus need `RightClick()` — physical and positional. Verbs `OpenFlyout`, `CloseFlyout`,
`InvokeMenuItem`; `Shell.FlyoutIsPresented` directly, and invoke a `MenuFlyoutItem` by id
without opening the popup.

**Result.** `InvokeMenuItem` and the two flyout verbs on the app side; `IMauiDriver` gains
`InvokeMenuItem`, `OpenFlyout`, `CloseFlyout` and `IsFlyoutOpen`. Seven new tests. Across the
four areas this step touches - Navigation, Shell, Dialogs, Background - 85 passed, 13 skipped
(step 32's Shell suite), 0 failed.

**The menu items were not hard to reach; they were absent.** `NavigationProbeTests` had already
measured it and the new tests re-measure it rather than trusting the old reading:
`PageMenuFileNew` is findable by neither `AutomationId` nor name, because MAUI does not propagate
`AutomationId` to menu chrome on Windows (dotnet/maui#3996). A context flyout is further out of
reach again - it does not exist in the tree until someone right-clicks it into being. So the two
`MenuFlyoutItem`s in the sample had never had handlers: a handler on an unreachable control is
untestable, and there was no reason to write one.

`IMenuItemController.Activate` is MAUI's own entry point for "the user picked this" - it is what
each platform handler calls - so the app cannot tell the verb apart from the real thing. Nothing
opens: an open menu is a different state of the app, and a verb that opened one on the way past
would leave a test asserting about a screen it never asked for.

**`RightClick` stays physical, and stays.** A test that means "right-clicking shows this menu" is
a test about the menu and no verb replaces it. What the verb removes is the commoner case behind
it - reaching an *item*, which took a right-click at one guessed coordinate followed by a click
at another, with the app holding the foreground throughout. `FlaUIMauiElement.RightClick` had
been recording itself as a physical-input use with this step named as its replacement; its note
now names the verb and says which half it does not replace.

**The Shell flyout is one public property.** `FlyoutIsPresented`, against a hamburger button MAUI
gives no `AutomationId` - so every earlier route to it was a guess about a control the platform
draws: by name, by control type, or by clicking where it usually is. The two new tests are live
while the other thirteen Shell tests stay skipped under step 32; they had to skip the fixture's
own `OpenTab`, whose reset opens the flyout by hunting for a button named "Open Navigation" and
throws before any test in the class runs. **A lead for step 32, not taken here:** that reset is
one of the things the verb could now do properly.

---

# Stage E — Hardening and rollout

> **Steps 27-29 are done. What is left of this document - step 30, and the whole of stage G - is
> re-ordered by [plan-the-quiet-run.md](plan-the-quiet-run.md), which also adds stages H and I.**
> That plan states the end this work is for: a MAUI suite driven through FlaUI that never takes
> the keyboard, the pointer or the foreground, and leaves the app under test behind whatever the
> person at the machine is using. It carries the measurements this section is missing - the full
> suite in background mode is 248 passed, 33 skipped, 11 failed, and the eleven are two problems
> rather than eleven.

### Step 27 — Security gating

Wrap the provider in `#if BRINELL_UIA_BRIDGE`, defined only in the instrumented configuration —
a shipping build contains no bridge code, not disabled code. `UseBrinellGestureBridge()`
additionally checks `BRINELL_UIA_BRIDGE=1` at runtime. **Add a test asserting a Release build
exposes no fragment root.** UIA has no per-caller authentication, so absence is the only real
control.

**Result.** Two gates on one decision, stated once in `BrinellBridgeGate` and read by both ends.
`UseBrinellGestureBridge()` exists and the sample app calls it; the driver sets the variable on
the app it launches. Two new tests in `Brinell.Uia.Tests`, 60/60 there. The Windows UI tiers are
unchanged: 83 passed / 13 skipped / 2 failed across Navigation, Shell, Dialogs and Background,
the same 98 as step 26, and a clean 57/57 on Background alone. Three combined runs failed three
*different* pairs - `ScrollTo` + `IsAtNavigationRoot`, then `ImageSource` + `ScrollTo`, then
`IsAtNavigationRoot` + `Fixture_NavigatesBetweenPages` - each of which passes alone. Step 36's
signature, and more evidence for the reading step 40 gave it.

**The first draft of the test passed for the wrong reason, which is the finding.** It built the
host in Release and asserted no fragment root, and it went green - and it went green just as
happily when the constant was forced back on, because it had also left
`BRINELL_UIA_BRIDGE` unset and the *run-time* gate was doing the work. A test of the compile-time
gate that never exercises the compile-time gate is worse than no test: it is a green light over
the one control that actually holds. The test now asks for the bridge and fails if it gets one,
so compile-time absence is the only thing left standing between it and a pass. Checked in both
directions - green with the gate intact, red with the default flipped.

**What is absent, measured on the built assemblies rather than argued:**

| | Debug | Release |
|---|---|---|
| `BridgeFragmentRoot`, `BridgeTargetProvider`, `BrinellUiaBridge` | present | absent |
| `MauiVerbDispatcher`, `MauiCapabilities`, the verb bindings | present | absent |
| `GestureAutomation` and its attached property | present | present |

**The declaration stays and the machinery goes, and that boundary is forced rather than
chosen.** Shared XAML names `uia:GestureAutomation.Verbs`, so the type has to exist on every head
in every configuration or the app stops compiling. What is left is an attached property that
records a string nothing reads. The same applies to `BrinellAlerts`, which the sample's own pages
call, and to `IBrinellGestureSink`, which an app implements.

`MauiCapabilities` and the dispatcher are removed even though neither touches UI Automation and
neither is reachable with the provider gone. A thousand lines whose only purpose is to drive the
app's own controls from outside have no business in a shipping binary on the grounds that nothing
calls them today.

**Removed from the build, not wrapped in `#if`.** The plan said `#if`; the csproj does it by
file, which produces the identical binary and follows the precedent already in this project - the
`Handlers` folder is excluded the same way, with the reason written next to it: these are files
people read and copy, and eight of them under preprocessor directives read worse than a list of
what is in the build. One file carries an `#if`, `BrinellBridgeHost`, because it is the seam
`GestureAutomation` calls into and it has to survive the gate.

It also makes drift a build error rather than a runtime mystery. `RecordingTarget` and the test
host's own `Attach` call both failed to compile the first time Release was tried, which is
exactly the report wanted from a file that quietly stopped being gated.

**The run-time gate is a convenience and is labelled as one.** Anything able to set an
environment variable on the app could have launched a different build of it, so it protects
nobody. It earns its test because a gate that never says no looks exactly like a gate that works
- and because the whole UI suite now depends on it, so a gate broken shut is loud.

**One rule, compiled at both ends.** `BrinellBridgeGate` went into `Brinell.Uia.Contracts`
rather than into the app-side host, so the bridge's own test host reads the same lines the app
under test does. The alternative was a lookalike in the test host, which would have let the test
pass while the app did the opposite - the failure mode the contract project exists to prevent,
applied to the gate instead of to the wire format.

**Copying the sources does not copy the gate**, and that is written down in the README. The
constant comes from `Brinell.Uia.Bridge.props`, which the csproj imports; an app that copies
these files in and imports nothing has no bridge until it defines the constant itself. Silent,
but in the safe direction, and now findable before rather than after an afternoon of an app that
publishes nothing.

**The security analysis it gates had gone stale, and is now corrected.** Section 8 of the design
said "there is no string interpreted as a command" and "no new information is disclosed". Neither
survived stage D. `InvokeMenuItem` takes an `AutomationId` and activates that item - including
one in a context flyout nobody opened, which is the one place the bridge reaches *further* than
the pointer rather than the same distance. `GetState`, `GetText` and `CurrentAlert` return
application data: control values, field contents, and a dialog's title and message. The ceiling
is still what the app's own UI offers, and every verb is still one the element declared in its
own markup, but "no worse than a user with a mouse" is now "no worse than a user with a mouse and
a few more steps", and reading is a second reason absence is the control rather than a nicety.

Recorded in the design rather than quietly left, because a security section that overstates its
bounds is worse than one that admits them - it is the paragraph someone will cite instead of
re-checking.

**What is not covered, parked as step 41.** The run-time gate is measured on the bridge's test
host, not on the sample app. Proving it there would mean launching a second copy of the app under test while the
shared fixture holds the first, and the flake that would buy is not worth the ground it covers -
the compile-time gate is the control, and it is measured on a real build.

### Step 28 — Versioning and lifetime tests

An unknown verb from a newer client yields `NotSupportedByElement`, never a crash or hang.
`UiaDisconnectProvider` runs on window close. A soak test opening and closing the window
repeatedly, asserting the provider count does not grow — a leaked provider hangs every
accessibility client on the desktop, not just ours.

**Result.** Three new tests and one revised, 63/63 in `Brinell.Uia.Tests` over six consecutive
runs with no strays left behind. The Windows UI tiers are unchanged at 83 passed / 13 skipped /
2 failed, the same 98 as steps 26 and 27, with step 36's wanderers accounting for the two; a
clean 57/57 on Background alone. Both teardown fixes were checked by disabling them and watching
the tests go red.

**An unknown verb was answered with the wrong refusal, and the client already said so.** The
vocabulary is append-only - numbers are never renumbered or reused - so a provider that meets a
number above the ones it knows is meeting a client built against a later contract. That is
`UIA_E_NOTSUPPORTED`: this element will never do that, take another route.
`BrinellUiaClient.Describe` had been promising exactly that reading since step 5 - "the app was
built against a contract that predates this verb" - while the provider returned `E_INVALIDARG`,
which the same table reads as "the client and the app disagree about the contract" and sends the
reader hunting a wire fault that is not there. Zero and negatives stay `E_INVALIDARG`, because no
later contract can define them; that is a caller bug and worth naming as one.

Refused by arithmetic, before the liveness check, before the capability lookup, and above all
before any hop onto the app's UI thread. That is what makes "never a hang" structural rather than
hoped for: a newer client cannot make an older app block on a request it cannot even name.

### The teardown was half-built, and the half that was there was unverified

**`UiaDisconnectProvider` ran only on `Dispose`.** Destroying a parent destroys its children, so
an app window closing takes the bridge window with it - and the framework event a host hangs its
teardown off is not guaranteed to arrive first, or at all. On that path nothing was disconnected
and the registry kept a strong reference keyed on a handle Windows is free to reissue, so a later
unrelated window would have been answered by a dead bridge. `WM_DESTROY` is the one notification
that arrives however the window died, and the teardown now runs from there as well. It is
reentrant on purpose: the disposing path calls `DestroyWindow`, which sends `WM_DESTROY`
synchronously, which lands back in the same method.

**And the HRESULT was thrown away.** The call site said "the disconnect is not optional" while
discarding the answer, so a disconnect that quietly failed looked exactly like one that worked -
which is the leak, arriving with a clean bill of health. `DisconnectFailures` counts them and the
tests assert zero. That count is what caught the next thing.

**The same gap on the app side.** `BrinellBridgeHost` caches one bridge per window handle and only
removed it on MAUI's `Destroying`. A stale entry would be handed back for a reissued handle, and
`Register` on a disposed bridge throws - reaching the app as "could not publish", naming neither
the window nor the reason. It now checks the cached bridge still has a window. Reasoned, not
measured, and parked as step 42: closing the app's window mid-suite is not something the MAUI
tier can do to itself.

### The soak found nothing, and the harness found three things

The 25-cycle soak passes and always would have: `Dispose` was the one path that was already
right. What the work around it turned up:

**The harness manufactured the leak it was there to detect.** Driving teardown with
`SendMessage` from the test process made every disconnect fail with
`RPC_E_CANTCALLOUT_ININPUTSYNCCALL` - a cross-process `SendMessage` is an input-synchronous call,
and COM refuses an outgoing call while one is being dispatched;
`UiaDisconnectProvider` has to call out to the client to revoke its interface. Teardown reported
success, every provider stayed connected, and a held pattern went on answering verbs at full
speed. Commands are posted now, which is also the faithful arrangement - a real window closes
from its own message loop - and only queries are sent. **With that fixed the design's claim holds
as written**: `0` disconnect failures, and a stale call returns `0x8000FFFF` in 1 ms rather than
blocking.

**A stray host process hangs the run that started it.** A child inherits stdout, and a test run
that is itself killed never reaches its cleanup, so the pipe stays open and the shell waits ten
minutes on a handle nobody will write to - which looks like a hung suite and is not one. Three
guards: the host's output is redirected, `MSBUILDDISABLENODEREUSE` stops step 27's in-test build
leaving MSBuild worker nodes on the same pipe, and the host now exits on its own after five
minutes. Then one host per run still leaked, because disposing the automation session came before
killing the process and can throw when the window it was attached to has gone. Kill first, guard
everything. Zero strays over four runs.

**A window is not addressable the instant it exists.** `UIA3Automation.FromHandle` throws
`Win32Exception: Unexpected HRESULT` for a handle it cannot resolve yet - not a null, so there is
nothing to test for. Latent while a run had one host; with three it failed about one run in
three, in a constructor, taking a whole class down at once and reading as a broken bridge rather
than a young window. `BridgeClient.AttachToWindow` retries.

**Worth stating plainly:** none of the three was a defect in the bridge, and all three would have
been charged to it. Two of the four combinations of "is this ours" and "is this real" are traps,
and this step spent most of its time in them.

### Step 29 — Accessibility audit report

Enumerate every element declaring verbs that exposes neither a keyboard route nor
`InvokePattern`; write it to `TestResults/<run-id>/suites/<suite>/` per `AD-007`. This is what
makes the programme an accessibility improvement rather than a way to test around a defect —
the list is a backlog.

**Result.** `AccessibilityAudit` in `Brinell.Maui.FlaUI`, reached through
`FlaUIMauiDriver.AuditGestureAccessibility()`, writing Markdown to
`TestResults/<run-id>/suites/<suite>/attachments/accessibility-audit.md` per `AD-007`. Two UI
tests and one contract test; `Brinell.Uia.Tests` 64/64. The Background tier is noisy this
session - 2, 2 and 4 failures across three runs with the audit and 2 and 4 without it, all of
them step 36's usual names - so the audit's eleven extra navigations are not the cause, and its
own two tests passed every run.

**The report, against the sample app: 33 instrumented elements, 11 with no route but the
pointer.**

| Verdict | Count | Which |
|---|---|---|
| Not in the accessibility tree | 2 | `TestSwipeView`, `TestRefreshView` |
| Pointer only | 9 | the five `Gesture*Target` borders, `ProbeGestureBridge`, `ProductCollectionView`, `ScrollTestScroller`, `TestTimePicker` |
| Reachable another way | 8 | the entries, the editor, the search bar, the three pickers, `TestDatePicker` |
| An address, not an affordance | 10 | every instrumented page |
| Read-only | 4 | the images, the progress bar, the hub |

The first row is the sharper finding and it is the same fact that made this whole programme
necessary: those two controls are not merely keyboard-unreachable, they are absent from the tree
that assistive technology walks, which is why the bridge had to reach them by a route of its own.

**And one surprise.** `TestDatePicker` is keyboard focusable and carries `InvokePattern`;
`TestTimePicker`, three lines away in the same markup with the same shape of declaration, has
neither. That is not a choice the sample app made, and it is exactly the kind of thing this
report exists to surface - nobody would have gone looking.

### The classification is the whole design, and the first version of it was wrong

**Two kinds of verb turned out to be three.** The audit's question is "can a person reach this
element's function without a pointer", and that only means something when the element is what is
being acted on. Reads were the obvious exclusion - the sample declares `GetState` on labels and
images, and calling those accessibility defects would be noise. The one I missed is the *app*
action: a page declares `NavigateBack` and `InvokeMenuItem` because a page is somewhere to post
the request, not because a page is what anybody presses.

The first run reported it: **eight of fifteen findings were pages with no keyboard route.** True,
meaningless, and more than half the list. A backlog that is mostly noise is one nobody reads, so
the fix went into the vocabulary - `BrinellVerbKind` is `Read`, `ElementAction` or `AppAction` -
rather than into a filter in the audit, because it is a fact about the verbs rather than about
this report.

`OpenFlyout` and `CloseFlyout` are genuinely ambiguous and are called app-level. On a `Picker`
the subject is the picker; on a `Shell` it is the app's chrome, and asking whether a keyboard can
focus a `Shell` answers nothing. App-level gets both right in practice - a picker is audited
through the selection verbs it also declares, a shell drops out - and the cost is that an element
declaring nothing but a flyout verb escapes the audit.

**Every verb is pinned by name in `ContractTests`.** `KindOf` falls through to `ElementAction`, so
a fall-through test could never fail; the checklist compares the whole enum against three lists
written out by hand, so adding a verb without deciding what it is breaks a test rather than
quietly changing an accessibility report. The default is the loud one: an unclassified verb puts
its element on the backlog rather than dropping it silently out.

**The audit asks the control view, not the raw view.** The bridge lives in the raw view where no
screen reader will ever see it; the question is what assistive technology can reach, so the
lookup is an ordinary `FindFirstDescendant`. An element only a raw walk can find is, for this
purpose, not there - which is the `Unreachable` verdict rather than a gap in the audit.

### Two things the walk taught

**Publication is asynchronous and opening a page does not wait for it.** The first walk audited
the Gestures page - six declarations, the most in the app - and found nothing at all, while the
quicker pages after it looked fine. A silently short audit is the worst failure this report can
have, because a missing element reads as an element with nothing wrong. The walk now waits for
the finding count to stop moving rather than for the first finding, which is the difference
between catching a whole page and catching half of one.

**The backlog is not asserted empty and never will be.** The sample app contains gesture-only
controls on purpose - they are what the bridge exists to reach. A test asserting emptiness would
fail forever and teach everyone to ignore it. What is asserted instead is that the audit still
tells its five cases apart, each pinned to a named element, so a classifier that answered the
same thing everywhere would fail even though its report still looked plausible.

**Coverage is the pages the walk visits**, because elements publish on load and withdraw on
unload. The list is written out in the test; a declaration added to a page not on it narrows the
audit silently. The report names the page each element was seen on, so a reader who notices a
page missing knows what to add.

### Step 30 — Documentation and AD-008

`docs/architecture/decisions.md` gains **AD-008: Gestures And Semantic Actions Go Through UI
Automation**, carrying the three admission tests from the catalogue so the bridge does not
become the app's primary automation surface. Add a "Gestures and background execution on
Windows" section to `docs/platform-guides/maui.md`; add the Gestures filter to the `AGENTS.md`
tier table; link new pages from `docs/README.md`; note the contract-copying requirement in
`Brinell.Maui.AppSupport`.

---

# Stage G — Known, diagnosed, parked

> **Ordering now comes from [plan-the-quiet-run.md](plan-the-quiet-run.md).** Steps 34, 36 and 40
> turned out to be one causal chain rather than three items, and are stage H there. The rest are
> stage J, which blocks nothing. The accounts below stay here; they are what a reader needs
> before picking one up.

Where something real is understood and deliberately not being fixed yet. Two kinds live here:
tests that were already failing before this programme started (steps 31-32), and defects or
costs this programme found and wrote up without acting on (steps 33-35). Both are parked for the
same reason - they are not blocking the step in front of them - and both are here so that
"parked" is a decision with a name rather than something nobody got round to.

## The pre-existing baseline

**These 24 failures predate every step in this document.** They were failing before the bridge
work started, they are unrelated to it, and until now every run has had to be read against them:
"24 failed" meant success, and telling a new failure from the baseline meant knowing the list by
heart. That has already cost real time in this programme — twice a genuine regression was read as
baseline noise, and once the reverse.

**They are skipped rather than deleted.** A skipped test still appears in every run with its
reason, so the count stays visible and the work stays findable. Deleting them would lose the
coverage; leaving them failing loses the signal from everything else.

Each skip names the step below, so `dotnet test` output points here.

### Step 31 — Stepper: 11 tests

`testsnew/Brinell.Maui.UITests/Tests/Range/StepperTests.cs`

All eleven fail the same way: `TestStepper` is not found within `RangeTestPage`. The control is
declared in the markup and does not resolve on Windows, so this is one root cause with ten
cascades rather than eleven defects.

`Stepper_Decrement_ChangesValueByStepSize` · `Stepper_Decrement_StopsAtMinimum` ·
`Stepper_Increment_ChangesValueByStepSize` · `Stepper_Increment_StopsAtMaximum` ·
`Stepper_IsEnabled_ReturnsTrue` · `Stepper_MultipleValueChanges_UpdatesDisplay` ·
`Stepper_Reset_RestoresInitialValue` · `Stepper_SetValue_RespectsBounds_Max` ·
`Stepper_SetValue_RespectsBounds_Min` · `Stepper_SetValue_UpdatesDisplay` ·
`Stepper_SetValue_UpdatesStatus`

**Where to start:** `AutomationProbeTests` already reports which container types are addressable
on Windows. Run it against the Range page and find out whether the Stepper publishes an
`AutomationId` at all — if it does not, this belongs with `SwipeView` and `RefreshView` as a
control the bridge reaches rather than the tree does.

**Tried in stage J, and ruled out - do not repeat it.** MAUI draws a Stepper as `MauiStepper`, a
plain `Control` with no automation peer: that is why `TestStepper` never resolves while its buttons
do. The obvious fix is a handler returning a subclass that overrides `OnCreateAutomationPeer`, the
way the layout handlers work. Measured, it cannot work:

- The subclass renders **empty** - zero bounds, no plus or minus button. `MauiStepper`'s template
  is an implicit style keyed on its exact type, and a subclass does not match it.
- Assigning that style explicitly **crashes the app**: *"Cannot apply a Style with TargetType
  'Microsoft.Maui.Platform.MauiStepper' to an object of type 'Microsoft.UI.Xaml.Controls.Control'."*
  WinUI's type system sees a C# subclass of a C# control only as its nearest native type.

So the peer cannot be added from outside, and it was reverted in full. **Where to start instead:**
leave the platform control alone and resolve from the side that is addressable - the control object
finding `…Plus` and `…Minus` (MAUI names the template buttons after the Stepper's id), and reading
the value through a bridge `GetState("Value")` declared on the Stepper. Both avoid touching WinUI's
styling at all.

### Step 32 — Shell app: 13 tests

`testsnew/Brinell.Maui.UITests/Tests/Shell/`

A different app — `Brinell.Samples.Maui.ShellApp`, driven by `ShellFixture`. Worth stating
plainly, because these are easy to read as failures of the hub app's suite and they are not.

`ShellFlyoutTests` (6): `Flyout_Close_LeavesTheTabsUsable` · `Flyout_Item_NavigatesToItsPage` ·
`Flyout_LastItem_IsReachable` · `Flyout_OpenTwice_StaysOpen` · `Flyout_Open_RevealsItems` ·
`Flyout_StartsShut`

`ShellStackTests` (3): `Shell_FixtureReset_ClearsAPushedPage` · `Shell_PushedPage_PopsBack` ·
`Shell_ReselectingTheTab_DoesNotPop`

`ShellTabTests` (4): `Shell_ReportsItsTabs` · `Shell_ReportsTheCurrentTab` ·
`Shell_SelectTab_ShowsItsPage` · `Shell_SelectingTheCurrentTab_IsHarmless`

Step 16 added a fourteenth that is *not* skipped - `ShellCollectionParallelismTests` - and it
asks nothing of the app's contents, only that it launched. So the Shell app does start, and a
driver does attach to it: whatever is wrong here is above that line.

#### Result — closed in stage J

**All thirteen failed before their bodies ran, for one reason.** *"Page 'ShellSamplePage' is not
loaded, so 'Name:Open Navigation' cannot be found in it."* `ShellSamplePage` scoped every lookup
under a root with that `AutomationId`, and measured, no element in the Shell app carried it - nor
`AppShell`, though the Shell sets that id in code. MAUI does not copy a Shell's `AutomationId` to
the WinUI view that draws it. The tab strip, the flyout and every page were findable from the window,
and none from the root the page object asked for.

- **The Shell app copies its id onto its platform view** (`AppShell.PublishAutomationIdToThePlatformView`)
  and the page object's root is `AppShell`. The view already has a peer; only the id was missing, so
  nothing is replaced - the move that collapses trees.
- **That left two**, both dismissing the flyout by tapping a light-dismiss layer that does not support
  Invoke. `IMauiDriver.SupportsFlyoutVerbs` is now a question the Shell control object asks, and where
  the app declares the verbs it opens, closes and reads the flyout through them - step 26's lead, taken.
- **Also found on the way:** the Shell app never called `UseBrinellGestureBridge()` after step 27, so
  it had published nothing at all.

15 of 15 Shell tests, three consecutive runs, the 13 skips removed.

**Where it had started:** the Shell app is not instrumented with the automation bridge — it references
`Brinell.Maui.AppSupport` but declares no verbs, so it never creates one. Several of these are
about chrome the app did not draw (a flyout, a tab strip), which is exactly the category the
bridge exists for. Check whether the failures are addressability or behaviour before assuming
either.

## Found by this programme, written up, not yet fixed

### Step 33 — The navigation stall

Full account: [../fix/rca-navigation-tests-stall.md](../fix/rca-navigation-tests-stall.md).

Two unrelated causes behind the same symptom — the navigation tests appear to hang. Nothing
actually hangs; every wait is bounded. Both are the framework waiting out a timeout to confirm
something it already knew.

**A. `TryNavigateBack` waits two seconds to say no.** A grace period added for a real race — a
page publishes its bridge target on `Loaded`, later than its root reaching the automation tree —
but guarded on "does this app have a bridge", which is always true. So it fires on the commonest
negative of all: the app is already at its root. `MauiFixture.ReturnToHub` pays it once per
`Open` that starts at the hub. **This one is a defect, and it is mine, introduced during stage
B.**

**B. Two negative assertions pay ten seconds each.** `TabMenu_UnknownCaption_Throws` and
`Toolbar_DoesNotReachItemsOutsideItself` are 20 s of `NavigationControlTests`' 24 s. `Item(key,
timeout)` waits before throwing, deliberately and correctly, so asserting an absence costs the
whole timeout. Not a defect — the author left a comment saying exactly this. Only the number is
wrong: the assertion is about *which exception*, not about how long it waits first, so a 500 ms
timeout proves the same thing and returns 19 s.

**Why parked:** A is a performance regression, not a correctness one, and its proper fix is
entangled with step 34 — the bool return is what forced the guess. Fixing them together is one
change; fixing A alone means retuning a timeout and leaving the cause.

### Step 34 — Actions do not Try

Full account: [../fix/design-actions-do-not-try.md](../fix/design-actions-do-not-try.md).

Remove `TryPerformGesture`, `TryAppendText`, `TryClearFocus` and `TryNavigateBack`. An action
performs or throws; only a *search* may answer "no" — `TryFindElement` stays.

The argument is not naming. A bool-returning action produces the same call site every time — `if
(!TryX()) somethingElse();` — which is a ladder, and the activation ladder deleted this week was
exactly that shape at eleven sites. One already exists: `Entry.AppendCore` types the text when
`TryAppendText` declines, and nothing records which of the two ran.

`TryNavigateBack` is the case that proves it: one bit carries three answers — no bridge, not
published yet, nothing to pop — so the caller must guess, and that guess is step 33's stall.

**Why parked:** it will make paths that currently limp fail outright, which is the intent and the
risk. Worth landing deliberately, on its own, against a green suite — which now exists.

#### Done: `TryPerformGesture`

Deleted from `IMauiElement` and `IMauiDriver`. It was `SupportsGesture` and `PerformGesture`
glued together, and the gluing was the harm: it let a call site carry on as though the gesture had
happened. A caller that wants to branch writes the branch, and the two questions stay apart —
*can this app do it* is a property of the app under test; *did it work* is not something a test
should have to ask.

Its one call site, `UndeclaredGesture_IsRefusedWithAUsefulMessage`, now asserts the question
answers no and the command throws, which is what it was really testing. No other code used it —
the safest of the five to take first, and it is the proof that the shape was never needed.

**Still to do here:** `TryAppendText` → `AppendText`, `TryClearFocus` → `ClearFocus`, and
`TryNavigateBack` → `NavigateBack` plus `IsAtNavigationRoot`. The last one goes with step 33.

### Step 35 — Notice when the framework starts waiting

Nothing in the suite asserts how long anything takes. Step 33's regression shipped **with a green
suite and a lower total runtime**, because an unrelated fix was saving more than it cost, and it
was found by a person watching a run rather than by anything automatic.

The per-filter timings at the top of that RCA took one command and would have caught it:

```powershell
foreach ($f in "Tests.Buttons","Tests.Gestures","NavigationVerbTests","NavigationControlTests") {
    dotnet test testsnew\Brinell.Maui.UITests --filter "FullyQualifiedName~$f" -v:minimal /nr:false
}
```

Buttons runs 11 tests in 1 s. Any area an order of magnitude off that per test is worth a look
before it is worth a fix.

**Why parked:** deciding what to do with the numbers — a report under `TestResults` per `AD-007`,
a threshold that fails a run, or simply a documented habit — is a real decision and a small one,
and it should not be made in passing while chasing something else.

#### Result — closed in stage J: a report, not a gate

`TestTimingAttribute`, applied to the whole assembly, times every test with nothing added to any test.
As the run goes it appends `test-timings.csv`; at the end it writes `test-timings.md` to
`TestResults/<run-id>/suites/<suite>/attachments/` per `AD-007` - every class with its count, total,
mean and slowest test, set against `timing-baseline.json`, with classes more than twice their baseline
mean *and* at least 250 ms slower per test listed first and marked. It also writes
`test-timings-baseline-candidate.json`; refreshing the baseline is copying a good run's candidate over
the checked-in file.

**Not a gate, deliberately.** A threshold that fails a run fails it on a slower machine, a busy agent,
or a first run paying for JIT, and a gate that trips for reasons unrelated to the change teaches
everyone to ignore it. Step 33 needed the slowdown to be *visible*; a marked row at the top of a report
is. `TestTimingReportTests` replays step 33's regression - a class that went from 550 ms to 2.5 s per
test is flagged - and checks a 40 ms test that doubles is not.

### Step 36 — `ReturnToHub` intermittently reports the hub never arrived

In a full run, **one to four tests fail** with the same message:

```
Could not get back to the hub, so the next page cannot be opened.
  popped through the bridge
```

**It is not a fixed set of tests, and it is not confined to background mode.** Five runs blamed
eight different tests between them — `Button_Reset_ClearsStatus`,
`ImageButton_Tap_ExecutesCommand`, `ImageButton_IsExists_ReturnsTrue`,
`Picker_MultipleSelections_UpdatesStatus`, `BottomLabel_Text_IsReadable`,
`TopButton_Click_AfterScrollingToTheBottom_UpdatesStatus` among them — and the last two failed in
the **default** configuration, not under background mode. Every area passes on its own: Scroll is
10 of 10, three runs in a row.

So it lands on whichever test navigates next. An earlier attempt to park three named tests was
removed: naming arbitrary victims makes a moving fault look like a fixed one.

**Seven named skips have since been added back, and they should come out.** Measured while
planning stage H: `ButtonTests`, `ImageButtonTests` (two), `ScrollTests` (two) and `PickerTests`
(two) all carry `Skip = "Stage G step 36..."`. In the same run the fault took down **nine other
tests anyway** - which is this paragraph, demonstrated. They make the suite look healthier than it
is, they protect nothing, and step 45 removes them as part of proving the fix.

#### What is actually failing

Read the `attempts` line carefully — `popped through the bridge` means **`TryNavigateBack`
returned true**. The verb reported `S_OK`, so a live page agreed it was on top and issued the pop.
What then fails is `_hub.WaitLoaded(ShortTestTimeoutMs)`: ten seconds later the hub is still not
reporting itself loaded.

That narrows it considerably, and rules out the first two suspects:

- **Not a stale target answering.** A detached page reports `stack depth 0` and returns
  `S_FALSE`, never `S_OK`.
- **Not a missing live target.** If no target answered, `TryNavigateBack` would have returned
  false and the fallback branch would appear in `attempts`. It does not.

The pop is real. The hub does not become loaded within ten seconds of it.

#### What the trace does and does not cover

`BRINELL_UIA_LOG` over a full run: **205 pops, every one at stack depth 2, zero `PopAsync`
faults, zero in-flight refusals.** The app side is clean, which is consistent with the above — the
provider did its job. The trace has nothing to say about the hub appearing, because that is
entirely a client-side reading.

One thing the trace did establish, and it belongs to step 33 rather than here: a popped page keeps
its bridge target until `Unloaded` fires, and answers `stack depth 0, top is 'none'` in the
meantime. That costs a round trip and feeds the two-second grace period. It is not this failure.

#### Where to start

`HubPage.IsLoaded` resolves the page root and asks `HasUsableBounds()`, and `PageObjectBase`
**caches that root across the whole run** — one `HubPage` instance is created in the fixture
constructor and reused for every navigation. A cached UIA element belonging to an earlier
incarnation of the hub is the first thing to rule out: `IsCachedRootValid` tests
`HasUsableBounds()`, which a stale element can fail in a way that looks like "not loaded yet"
rather than "wrong element".

**One line has been applied and is unverified.** `ReturnToHub` now calls `_hub.InvalidateCache()`
before its postcondition. It is defensible on its own — re-resolving before asking about
readiness is strictly more correct than trusting a cache across a navigation — but **no run has
confirmed it changes anything**, so do not treat this step as half-solved.

If the failure survives it, the cache is exonerated and the next question is whether the app is
sometimes genuinely slow to complete a pop under load, making the postcondition's ten seconds not
always enough.

**A note on priority, from the session that wrote this.** This is a test helper, and chasing it
consumed far more time than it was worth — several rounds of diagnosis, two of which reached
confident and wrong conclusions. It is parked here deliberately. The suite is usable: the failure
costs one to four tests per full run and never the same ones, and re-running the affected area
passes. Anyone picking this up should timebox it.

**The skip list grows every run, and that is the argument for fixing this rather than parking it
further.** Seven tests carry a step 36 `Skip` so far, and each was added because it happened to be
the test that navigated next on some particular run — not because anything is wrong with it.
Every one of them would pass today if the helper were fixed. Skipping the next victim is cheap
and gets cheaper to repeat, which is exactly the trap: at some point the list is large enough that
nobody remembers it is one bug.

### Step 37 — TimePicker reads back 12-hour in background mode

`TimePicker_CombinedWithDate_WorksTogether`:

```
Could not set time 15:30:00 without the pointer:
the control reports '03:30:00' after Accept, not 15:30:00.
```

15:30 read back as 03:30 — the AM/PM half is being lost. It passes in the default mode, so the
difference is which rung of `TimePicker`'s own ladder runs when the pointer rungs are refused.

**Where to start.** This is step 20's territory — the `SetTime` verb replaces the flyout
navigation that produces this. Fixing the ladder rung in place would be work thrown away. Worth
checking first whether the same fault exists in the default mode and is simply masked by a rung
that runs earlier, because that would make it a live bug rather than a background-mode one.

### Step 38 — The clipboard canary fails when another process holds the clipboard

`TextVerbTests.Paste_LeavesTheClipboardAlone`:

```
Assert.Equal() Failure: Strings differ
Expected: "brinell-clipboard-canary-f1cd32d1f8fd44ad"
Actual:   ""
```

**Not about the framework, and not about step 16** - it fails identically with the Shell app out
of the picture, so the parallel run is not the cause. The sentinel this test puts on the clipboard
itself came back empty, which means the test's own `WriteClipboard` did not take.

**And the file already says what to do about that.** `OnStaThread` swallows exceptions on purpose,
with the reasoning written down: *"the clipboard is shared with every process on the desktop and
any of them can hold it locked. An unreadable clipboard makes the canary inconclusive rather than
failed."* The helper honours that; the test body does not. It asserts on the read-back without
first checking the write landed, so an inconclusive probe is reported as a framework regression -
which is the one thing the remarks set out to avoid.

**Where to start.** Read the sentinel back straight after writing it; if it is not there, the
desktop's clipboard is not available and there is nothing to canary. Roughly three lines. The
interesting question is what holds it - a clipboard manager, an editor, RDP redirection - but that
question does not have to be answered to stop the test lying.

### Step 39 — Selecting a repeated item freezes MAUI

Found while building a picker whose two identical items would prove the derived index wrong. The
demonstration worked rather too well.

Setting `Picker.SelectedIndex` to the second of two identically-displayed items hangs the app.
MAUI keeps the pair in step by setting each from the other — `SelectedIndex` sets `SelectedItem`
to the item at that position, `SelectedItem` sets `SelectedIndex` back to `IndexOf(item)` — and
where `IndexOf` cannot return the index it was given, the two never agree.

**Measured, not inferred**, because "the app stopped answering" has four plausible causes and
three of them would have been ours:

- The bridge log showed the verb arriving and timing out: `Invoke SelectIndex(1,0) on
  'DuplicatePicker' -> 0x80131505`.
- Sampling the app process showed CPU climbing 0.72 s per 0.72 s of wall clock with
  `Responding = False` — one core pinned, so a spin rather than a block.
- The app's own `Loaded` handler doing the same assignment, with no automation running at all,
  froze it identically. That is what makes it MAUI's rather than the bridge's.

Every test after it in the collection then failed with "no element published on the app's bridge
answers GetState", which reads exactly like a missing declaration.

**Not parked — guarded.** The app compares the position MAUI's own lookup would return against
the one it was given, before it assigns, and refuses with `UIA_E_NOTSUPPORTED` when they differ.
A verb that reproduced this would take down the app under test and everything after it, and the
comparison costs nothing. `SelectByText` needs no guard: it selects the position `IndexOf` gives
it, so the two already agree. `DuplicatePicker` stays in the sample, carrying a warning, so the
refusal is exercised rather than believed in.

**Still live for a person.** Clicking the second `Repeat` in that picker freezes the sample app,
because it is the same code path. Worth an upstream report; not worth working around further
here.

### Step 40 — `S_FALSE` does not survive the bridge

Found by a test that would not go red. The app answered "I found the menu item and did nothing"
and the client read plain success.

```
app     Exchange InvokeMenuItem('ContextMenuDelete') -> 0x00000001   (S_FALSE)
client  delivered=True hr=0x00000000                                 (S_OK)
```

Confirmed on the other method too, with a verb that has nothing to do with menus:

```
app     Invoke ScrollToIndex(60,0) -> 0x00000001
client  delivered=True hr=0x00000000
```

**Not our bug, and worth having ruled that out.** Both ends pass the value through faithfully -
the provider returns the dispatcher's HRESULT, the client returns what `CallMethod` gave it, and
both interfaces are `[PreserveSig]`. UI Automation's own marshalling of a custom pattern reports
every *success* HRESULT to the client as `S_OK`. Failure HRESULTs cross intact, which is why
`UIA_E_NOTSUPPORTED` and `UIA_E_ELEMENTNOTAVAILABLE` have been working all along and nobody
noticed the other half was gone.

**What it invalidates.** `S_FALSE` was chosen in several places to mean "succeeded, and
truthfully did nothing", and that distinction has never reached a caller:

| Verb | What `S_FALSE` meant | Harmed? |
|---|---|---|
| `SetDate`, `SetTime` | the picker clamped | no - the client compares the value that came back |
| `SetText`, `AppendText` | the field altered the text | no - same |
| `SelectByText` | the picker declined | no - same |
| `SelectIndex` | a binding declined the index | **yes** - reported as success |
| `ScrollToIndex` | index past the end | no - the interface documents this as "nothing moves and nothing throws", which is what happens |
| `RefreshView` swipe | already refreshing | **yes** - a test can assert a refresh it did not cause |
| `NavigateBack` | nothing to pop | **yes** - see below |
| `OpenFlyout`, `CloseFlyout` | already in that state | no - the caller asked for a state |

The pattern in the "no" column is worth naming: **every verb that reads back what landed is
unharmed, and every verb that relies on the HRESULT alone is not.** That is an argument for the
read-back shape rather than for a wire fix.

**And a lead on step 36.** `InvokeAnywhere` walks past a declining target on purpose, because a
page that was popped long ago answers `S_FALSE` to `NavigateBack` - agrees, pops nothing. Since
`S_FALSE` arrives as `S_OK`, that walk now *stops* at the stale page and reports success, and the
caller waits for a hub that was never coming. That is exactly the shape of "Could not get back to
the hub / already at the navigation root", the flake that has been parked as step 36 and blamed
on timing. It fits the evidence better than timing does: the failure is order-dependent, it needs
a previously-visited page to exist, and it wanders because which stale target is walked first
does.

**Not fixed here.** Step 36 is parked and the fix is not a one-liner: the outcomes that matter
have to travel as values or as failure HRESULTs, verb by verb, and `InvokeAnywhere`'s walk needs
rethinking once they do. `InvokeMenuItem` is written that way already - its outcome is in the
payload, which is why the disabled entry is now refused - and it is the pattern the rest should
follow.

### Step 41 - The run-time gate is measured on the test host, not on the app

Step 27 put two gates on the bridge. The compile-time one is measured on a real build:
`BridgeGatingTests` builds the bridge's host in Release, launches it *asking* for the bridge, and
fails if a fragment root appears. The run-time one - `UseBrinellGestureBridge()` plus
`BRINELL_UIA_BRIDGE=1` - is measured on that same host, which reads the decision from the same
lines the app does (`BrinellBridgeGate`, in the contract, compiled at both ends). So the *rule*
is measured on the real implementation; what is not measured is the app under test obeying it.

**Why it was left.** Proving it on the sample app means launching a second copy while the shared
fixture holds the first, and the flake that buys is not worth the ground it covers. The gate that
actually controls anything is the compile-time one - the run-time half protects nobody, because
anything able to set an environment variable on the app could have launched a different build of
it.

**What partly covers it anyway.** Every Windows UI test depends on the run-time gate being read:
the driver sets the variable, and a gate broken *shut* fails the whole suite at once. The
uncovered direction is a gate broken *open* - an app that instruments itself whether or not it
was asked.

**Where to start:** step 29 walks the app's published elements to write its audit. If that walk
can be run against an app launched without the variable, "nothing was published" is the same
assertion for free.

### Step 42 - A bridge cached against a closed window is guarded but not measured

`BrinellBridgeHost` keeps one bridge per window handle and removed it only when MAUI's
`Destroying` fired. Step 28 found the matching gap in the provider - a window can be destroyed
without anyone disposing the bridge - and fixed both: the provider tears down on `WM_DESTROY`,
and the app-side cache now checks the bridge it is about to hand back still has a window.

The provider half is measured, both directions. The app-side half is reasoned only.

**Why it was left.** The failing case needs a MAUI window that closes while the suite keeps
running, and closing the app under test mid-suite is not something the MAUI tier can do to
itself - the fixture owns that window and every later test needs it.

**What it would look like if it were wrong:** a stale entry handed back for a handle Windows had
reissued, `Register` throwing `ObjectDisposedException` into `Publish`'s catch-all, and the app
reporting "could not publish" without naming the window or the reason. Rare, and it would read as
one of step 36's wanderers.

**Where to start:** a second MAUI window rather than a second app. If the sample can open and
close a secondary window on demand, the bridge for it is created and destroyed inside one test
and the shared fixture never loses its own.

## Taking one out of Stage G

For a parked test (31-32): remove the `Skip`, run its area filter, and fix what it reports. For
a written-up item (33-35): read its document first - each one records why it is parked, and in
every case that reason is a dependency or a risk rather than a lack of time.

The point of the stage is that parking is a decision someone makes deliberately, not a number
carried in everyone's head.

---

## Standing rules for every step

- **One UI test process at a time.** Step 16 lets the two *collections* inside a run go side by
  side; it does nothing for two `dotnet test` invocations, because the lease is a static and each
  process has its own. Two runs still produce fictional failures and hour-long stalls.
- **Match the tier to the change**, and keep the tier narrow until the verbs exist - see "Which
  tests to run, and when" above. Buttons is a second; the full suite is minutes and is for stage
  boundaries.
- **Run with `BRINELL_BACKGROUND_MODE=1`** unless the change is specifically about physical
  input. Without it every click calls `SetForeground`, so a run takes the keyboard away from
  whoever is at the machine - which is the thing stage B exists to stop.
- **Establish the baseline before reporting a regression.** DatePicker, TimePicker, Image,
  ProgressBar, Stepper and Switch fail before any of this work starts.
- **The mobile head must keep building** from step 10 onward. It links every `.cs` file from the
  Windows test project and references Appium, not FlaUI.
- **No arbitrary sleeps** (`AD-004`). A verb returns when the action is delivered; waiting for
  the UI to settle is a `WaitFor` on concrete state.
- **Record negative results where the next person will look** — the way
  `AutomationRemainingHandlers.cs` did. Spike answers belong in this file.
