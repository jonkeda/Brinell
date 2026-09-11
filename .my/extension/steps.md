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

Commands are from the Brinell root. `GESTURES` and `AREA` shorthands:

```powershell
# tier 1, seconds
dotnet test testsnew\Brinell.Maui.UITests --filter "FullyQualifiedName~Tests.Gestures" -v:minimal /nr:false
# the shared-source guard — must pass from step 7 onward
dotnet build testsnew\Brinell.Maui.UITests.Mobile\Brinell.Maui.UITests.Mobile.csproj -v:minimal /nr:false
```

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
| 13 | **Focus verb** | B | 12 | todo |
| 14 | **Text input verbs** | B | 13 | todo |
| 15 | Background mode passes | B | 3, 13, 14 | todo |
| 16 | Windows-only parallelism | B | 15 | todo |
| 17 | Gestures sample page | C | 11 | todo |
| 18 | Full gesture vocabulary + ladder | C | 17 | todo |
| 19 | Gesture control objects | C | 18 | todo |
| 20 | Date and time verbs | D | 14 | todo |
| 21 | Scroll verbs | D | 12 | todo |
| 22 | Navigation verbs | D | 12 | todo |
| 23 | State-read verbs | D | 12 | todo |
| 24 | Picker verbs | D | 21 | todo |
| 25 | Dialog reads | D | 23 | todo |
| 26 | Menu and flyout verbs | D | 13 | todo |
| 27 | Security gating | E | 18 | todo |
| 28 | Versioning and lifetime tests | E | 18 | todo |
| 29 | Accessibility audit report | E | 18 | todo |
| 30 | Documentation and AD-008 | E | 19 | todo |

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

---

### Step 18 — Full gesture vocabulary and dispatcher ladder

**Do.** All seven verbs. `MauiCapabilities.cs` naming every public MAUI API used, so a
MAUI upgrade is a compile error in one known file. Ladder: sink → public MAUI API →
`TapGestureRecognizer.Command` → `UIA_E_NOTSUPPORTED`. Never reflect into `SendTapped`,
`SendPinch` or `SendPan` — all internal in MAUI 10.

**Verify.** Gestures filter; `dotnet test testsnew\Brinell.Maui.Tests`. Unit-test the inverted
swipe-to-`OpenSwipeItem` mapping specifically — it is the thing most likely to be "fixed"
backwards.

---

### Step 19 — Gesture control objects

**Do.** `SwipeView` implements `ISwipeableControlObject<TScope>` on Windows; `RefreshView` gets
`PullToRefresh()`. Move the tests from raw verbs to control-object members. Update the
`ISwipeableControlObject` doc comment — it says "primarily used for mobile platforms", which
this step makes false. Add a gesture-bridge row to `AutomationProbeView`.

**Verify.** Gestures filter; full MAUI UI suite against the established baseline.

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

### Step 21 — Scroll verbs

`Swipe` currently substitutes mouse-wheel clicks, an unquantified unit, with a stuck-detection
loop because there is no completion signal. Verbs `ScrollTo`, `ScrollToIndex`,
`ScrollPosition`; provider calls `ScrollView.ScrollToAsync` / `CollectionView.ScrollTo`. Both
are cross-platform MAUI APIs, so the same verb works on Android — the biggest parity win in the
catalogue.

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

## Standing rules for every step

- **One UI test process at a time** until step 16 says otherwise.
- **Match the tier to the change.** The Gestures filter is seconds; the full suite is minutes
  and is for stage boundaries.
- **Establish the baseline before reporting a regression.** DatePicker, TimePicker, Image,
  ProgressBar, Stepper and Switch fail before any of this work starts.
- **The mobile head must keep building** from step 10 onward. It links every `.cs` file from the
  Windows test project and references Appium, not FlaUI.
- **No arbitrary sleeps** (`AD-004`). A verb returns when the action is delivered; waiting for
  the UI to settle is a `WaitFor` on concrete state.
- **Record negative results where the next person will look** — the way
  `AutomationRemainingHandlers.cs` did. Spike answers belong in this file.
