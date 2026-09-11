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
| 22 | Navigation verbs | D | 12 | todo |
| 23 | State-read verbs | D | 12 | todo |
| 24 | Picker verbs | D | 21 | todo |
| 25 | Dialog reads | D | 23 | todo |
| 26 | Menu and flyout verbs | D | 13 | todo |
| 27 | Security gating | E | 18 | todo |
| 28 | Versioning and lifetime tests | E | 18 | todo |
| 29 | Accessibility audit report | E | 18 | todo |
| 30 | Documentation and AD-008 | E | 19 | todo |
| 31 | Stepper: 11 failing before any of this work | G | — | **parked** — 11 of 13 carry a `Skip`; the other 2 pass and stay live |
| 32 | Shell app: 13 failing before any of this work | G | — | **parked** — all 13 carry a `Skip` |
| 33 | Navigation stall: a 2 s grace on the wrong question, and two 10 s negative assertions | G | — | **parked** |
| 34 | Actions do not Try: remove `Try` from commands, keep it on searches | G | 33 | **started** — `TryPerformGesture` deleted; the rest parked |
| 35 | Notice when the framework starts waiting | G | — | **parked** |
| 36 | `ReturnToHub` intermittently reports the hub never arrived (flaky, 1-4 tests per run, both modes) | G | — | **parked** |
| 37 | TimePicker reads back 12-hour in background mode (1 test) | G | 20 | **fixed by 20** |
| 38 | Clipboard canary asserts on a probe its own helper calls inconclusive (1 test) | G | — | **parked** |

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

### Step 23 — State-read verbs

The assertions that currently pass for the wrong reason. `Image` — "loaded if it occupies
space", though a failed image still occupies space; expose `Source` and `IsLoading`.
`ProgressBar` — WinUI 0–100 normalised against MAUI's 0–1; expose `Progress` directly.
`IsVisible` vs UIA `IsOffscreen` — report both, since disagreement is often the real bug.
`IsIdle` — a real drained-dispatcher signal beats a sentinel element, and squarely serves
`AD-004`.

**Boundary.** `BindingContext` and arbitrary property reflection stay out. Read what a user
could perceive; if the assertion needs the view model, it belongs in `Brinell.Maui.Tests`.

### Step 24 — Picker verbs

Picker tests are 0/8 on Android before any change — one root failure and seven cascades. Verbs
`SelectIndex`, `SelectByText`, plus a separate `OpenFlyout` so a test that genuinely means "the
flyout opens and shows these items" can still say so.

### Step 25 — Dialog reads

`Exchange(CurrentAlert)` returning title, message and buttons, so a test asserts *what was
asked* rather than that something was dismissed. `DismissAlert(result)` second, and used
sparingly: if the test is about a user confirming a destructive action, clicking the real button
is the point.

### Step 26 — Menu and flyout verbs

Context menus need `RightClick()` — physical and positional. Verbs `OpenFlyout`, `CloseFlyout`,
`InvokeMenuItem`; `Shell.FlyoutIsPresented` directly, and invoke a `MenuFlyoutItem` by id
without opening the popup.

---

# Stage E — Hardening and rollout

### Step 27 — Security gating

Wrap the provider in `#if BRINELL_UIA_BRIDGE`, defined only in the instrumented configuration —
a shipping build contains no bridge code, not disabled code. `UseBrinellGestureBridge()`
additionally checks `BRINELL_UIA_BRIDGE=1` at runtime. **Add a test asserting a Release build
exposes no fragment root.** UIA has no per-caller authentication, so absence is the only real
control.

### Step 28 — Versioning and lifetime tests

An unknown verb from a newer client yields `NotSupportedByElement`, never a crash or hang.
`UiaDisconnectProvider` runs on window close. A soak test opening and closing the window
repeatedly, asserting the provider count does not grow — a leaked provider hangs every
accessibility client on the desktop, not just ours.

### Step 29 — Accessibility audit report

Enumerate every element declaring verbs that exposes neither a keyboard route nor
`InvokePattern`; write it to `TestResults/<run-id>/suites/<suite>/` per `AD-007`. This is what
makes the programme an accessibility improvement rather than a way to test around a defect —
the list is a backlog.

### Step 30 — Documentation and AD-008

`docs/architecture/decisions.md` gains **AD-008: Gestures And Semantic Actions Go Through UI
Automation**, carrying the three admission tests from the catalogue so the bridge does not
become the app's primary automation surface. Add a "Gestures and background execution on
Windows" section to `docs/platform-guides/maui.md`; add the Gestures filter to the `AGENTS.md`
tier table; link new pages from `docs/README.md`; note the contract-copying requirement in
`Brinell.Maui.AppSupport`.

---

# Stage G — Known, diagnosed, parked

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

**Where to start:** the Shell app is not instrumented with the automation bridge — it references
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
