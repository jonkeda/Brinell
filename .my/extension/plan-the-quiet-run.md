# The quiet run

**The end state: a MAUI suite driven through FlaUI that never takes the keyboard, never takes the
pointer, never takes the foreground, and leaves the app under test behind whatever the person at
the machine is working in.** Not "mostly", not "if you set a variable and do not touch anything" —
a run you can start and then carry on typing through, and a claim a test makes rather than a
person watching.

This plan replaces the ordering of what is left of
[steps.md](steps.md) — stage E's step 30 and the whole of stage G — and adds the work the goal
needs that no step covers. Numbers 30 through 42 keep their meanings; new work continues at 43.

---

## 1. Where it actually stands

Measured today, not remembered.

**Full MAUI UI suite, `BRINELL_BACKGROUND_MODE=1`:**

```
292 total · 248 passed · 33 skipped · 11 failed · 3 m 17 s
```

**The eleven failures are two problems, not eleven.**

| Failures | What they are |
|---:|---|
| 2 | `AppRootScopeTests.BackToHub_*`, marked `PhysicalInput=Deliberate`. Refused on purpose. Working as designed. |
| 9 | Every one passes on its own. Step 36. |

The nine were re-run individually: `ImageSource_SeparatesABrokenImageFromAWorkingOne`,
`ScrollTo_MovesTheViewport_WithoutPhysicalInput`, `NavigateBack_AtTheHub_FailsWithTheReason`,
`ImageButton_IsEnabled_ReturnsTrue`, `DoubleTap_RaisesTheTwoTapRecognizer`,
`InPageSurfaces_AreAddressable`, `Stepper_IsExists_ReturnsTrue`,
`Slider_MultipleValueChanges_UpdatesDisplay`, `Picker_Reset_ClearsSelection` — nine areas, nine
passes. **There is one fault between this suite and green, and it is step 36.**

**The thirty-three skips:**

| Skips | Step | |
|---:|---|---|
| 13 | 32 | the Shell app |
| 11 | 31 | the Stepper |
| 7 | **36** | named victims of a moving fault |
| 2 | — | MAUI CollectionView row recycling |

**Those seven are a mistake and should come back.** Step 36's own text says why: *"An earlier
attempt to park three named tests was removed: naming arbitrary victims makes a moving fault look
like a fixed one."* Seven were added back anyway, and the fault still took down nine others in
this run — which is the argument, demonstrated. They make the suite look healthier than it is and
they protect nothing.

**Physical input is already proven, and that is worth saying plainly.** Thirteen call sites in the
MAUI driver and element funnel through `PhysicalInput.Used`; in refused mode every one throws;
the suite produces **zero refusals**. Keyboard and pointer are not open questions — they are
closed by construction, and the two tests that need them say so in their own source.

**So the goal is nearer than the failure count suggests.** What is missing is the *window*.

---

## 2. What the goal needs that nothing covers

Four claims. Two hold, one half-holds, one is untested.

| Claim | State |
|---|---|
| The run never synthesizes a keystroke | **Holds.** Refused by policy, zero refusals measured. |
| The run never synthesizes pointer input | **Holds.** Same mechanism. |
| The app never takes the foreground | **Half.** Demoted once, at launch — and it takes the foreground *first*, because `UseShellExecute` gives it to them. Nothing re-demotes if it rises later. |
| The app stays behind the user's windows | **Untested.** Never observed by anything. Step 15's done-when is *"you can type in Visual Studio throughout"* — a human check that has never been performed. |

**The launch flash is real and is not a detail.** `FlaUIMauiDriver` starts the app with
`UseShellExecute = true`, so Windows hands the new process the foreground; `SendAppBehind` then
pushes it down and `SetForegroundWindow` asks politely for the old window back. Between those two
moments the app is in front of whatever the person is typing into. It happens once per collection,
which is several times a run.

**And nothing watches after that.** A dialog, a page transition, or the app calling `Activate` can
raise the window, and no code path notices. Whether that actually happens is unknown, because
nothing has ever looked.

**`EnsureRootWindowFocused` un-minimizes before it checks the policy.** The window-state block
runs `SetWindowVisualState(Normal)` and only then does `PhysicalInput.Used` throw. In refused mode
the refusal is correct and the un-minimize has already happened.

---

## 3. The plan

Three stages. The first is one knot and everything downstream of green depends on it; the second
is the goal; the third is the backlog that blocks nothing.

### Stage H — Untie the knot

**Steps 40, 34 and 36 are one piece of work.** They have been three rows because they were found
three times, but the causal chain is a single line: `S_FALSE` does not survive the bridge, so
`TryNavigateBack` returns `true` for a page that popped nothing, so one bit carries three answers,
so `InvokeAnywhere` stops at a stale page and reports a pop that never happened, so
`ReturnToHub` waits for a hub that was never coming and blames whichever test navigates next.

Doing them in any other order means fixing a symptom.

#### Step 43 — Outcomes travel as values (closes 40)

**Why.** UI Automation reports every *success* HRESULT from a custom pattern as `S_OK`; failure
HRESULTs cross intact. Measured from both ends at once, step 26. So `S_FALSE` — "I succeeded and
truthfully did nothing" — has never reached a caller, and three verbs rely on it:
`SelectIndex` when a binding declines, `RefreshView` when already refreshing, and `NavigateBack`
when there is nothing to pop.

**Do.** Verb by verb, move the outcome off the HRESULT. `InvokeMenuItem` is already written this
way and is the pattern: the payload carries `"disabled"`, and `S_OK` means only that the request
was understood. Two shapes are available and the choice is per verb — a value in the `Exchange`
payload, or a genuine failure HRESULT where the caller should treat it as an error. Prefer the
read-back where one exists: every verb that reports what landed was never harmed by this, which is
an argument about design rather than about the wire.

`NavigateBack` is the one that matters and it is on `Invoke`. Either it moves to `Exchange` — an
append-only change, the number stays — or it answers `UIA_E_ELEMENTNOTAVAILABLE` for "this page is
not the one on top". The second is smaller and says something true.

**Verify.** A test that a declining verb is distinguishable from a succeeding one *at the client*,
not in the app's log. The bridge log showing `0x00000001` is what made this invisible for six
steps.

**Done when** no verb's meaning depends on the difference between `S_OK` and `S_FALSE`, and a test
says so.

#### Step 44 — Actions do not Try (finishes 34)

**Why.** `TryNavigateBack` returning one bit for three states is what forces `InvokeAnywhere` to
guess. Step 34 already argued the general case and deleted `TryPerformGesture`; this is the
remaining three, and the navigation one is only fixable once 43 gives it something honest to
return.

**Do.** `TryAppendText` → `AppendText`, `TryClearFocus` → `ClearFocus`, `TryNavigateBack` →
`NavigateBack` plus `IsAtNavigationRoot`.

**Verify.** Full suite. This will make paths that currently limp fail outright — the intent and
the risk both.

**Done when** no action on `IMauiDriver` or `IMauiElement` returns a bool. Searches still may.

#### Step 45 — `InvokeAnywhere` stops guessing (closes 36)

**Why.** The walk exists to step past a target that declines. With declining invisible, it stops
at the first stale page and reports success. Once 43 makes a decline visible and 44 stops the
answer being a bool, the walk can be rewritten to mean what it says.

**Do.** Rewrite the walk against real outcomes. Then **remove the seven step-36 skips** and run the
full suite repeatedly — the fault is order-dependent, so one green run proves little; five do.

**Verify.**
```powershell
$env:BRINELL_BACKGROUND_MODE = "1"
1..5 | ForEach-Object { dotnet test testsnew\Brinell.Maui.UITests -v:minimal /nr:false }
```

**Done when** five consecutive full runs fail nothing but the two `Deliberate` tests, with the
seven skips removed.

**If it does not close 36**, stop and say so rather than adding skips. Step 40's reading is a
strong lead and not a proof; the alternative causes are still on the table and the RCA should be
updated with what was ruled out.

#### Result so far — 43 done, 36 root-caused and fixed, a tail remaining

**Measured, full MAUI UI suite, background mode:**

| | Before stage H | After |
|---|---:|---:|
| Failed | 11 | **6** |
| Passed | 248 | 253 |
| Duration | 3 m 17 s | **1 m 56 s** |

Two of the six are the `Deliberate` tests, refused by design. Four are a remaining tail, described
below. The Background tier alone runs 59/59 on most runs and 58/59 on some.

**The runtime fell by 40% and nothing was made faster.** That is all timeout-waiting removed: a
caller that was told "success" by a page which had done nothing then waited out its budget for a
change that was never coming.

**Step 36's root cause, found in the bridge log.** Not the reading step 40 offered - that was
close, and wrong about which call. A page part-way through being torn down still answers, and its
`Navigation` reports a stack it is no longer part of:

```
14:03:21.943  withdraw 'PageHub' -> True                       (the next page was pushed)
14:03:21.967  GetState('NavigationDepth') on 'ContainerPage' -> '0'
14:03:22.023  withdraw 'ContainerPage' -> True                 (the stale page, finally)
```

`ExchangeAnywhere` takes the first answer, so `0` won. `IsAtNavigationRoot` said yes, the fixture
did nothing, and then waited ten seconds for a hub that was on nobody's screen - and blamed
whichever test had asked to navigate. `NavigationDepth` now answers only from a page that is in
the stack it is reporting on, the same rule `NavigateBack` already applied one notch tighter.

Worth recording that the method's own remarks claimed the opposite - *"any live target answers
this correctly, stale or not ... there is no wrong element to reach here"* - and had done since
step 22.

**What step 43 changed.** `BRINELL_E_DECLINED`, a customer-defined failure code, carries the
meaning `S_FALSE` could not. Four sites moved to it: going back at the root, a picker binding
refusing an index, a `RefreshView` already refreshing, and a command whose `CanExecute` is false.
`S_FALSE` keeps the cases where nothing reads it, and its doc comment now says why it must never
carry meaning again.

**Two things were measured rather than assumed, and one of them narrowed the design.**
`HResultCrossingTests` pins both: every failure code crosses intact, including ours, on both
methods; every success code arrives as `S_OK`. And **a failing `Exchange` loses its payload
entirely** - COM does not marshal an `[out]` parameter back on failure - so the two channels are
exclusive. A refusal that needs to explain itself must return `S_OK` and put the reason in the
payload, which is what `InvokeMenuItem` does and why it must not be "tidied up" into a failure
code.

**Three further races surfaced once the noise cleared**, all previously hidden inside step 36:

- `ScrollTo` and `ScrollToIndex` returned before the viewport moved. MAUI's scroll is asynchronous
  whatever the animation flag says, and the app cannot await it - the continuation needs the UI
  thread the verb is holding. The client now waits for two equal position reads.
- `NavigateBack` immediately after a navigation found no published target, because a page
  publishes on `Loaded`, later than its root reaching the tree. Bounded retry, and only for that
  window: an answer of "asked and refused" returns at once.
- `Open` returned before the page had arrived, so a test's first line could miss a control that is
  certainly on the page. It now waits for the push and for the app to report idle.

Telling those two "no answer" cases apart needed the count of targets that declined, which
`BridgeVerbResult` now carries. Nothing declining means no page implements the verb and waiting
cannot help; something declining means the app has answered.

**The tail, four tests.** `DatePicker_SetDate_UpdatesDisplay`, two of `AlertReadTests`, and
`UndeclaredVerb_OnADeclaredRow_IsRefused`. Each passes alone; each fails perhaps one run in three
under a full run. They are not step 36 - that signature is gone - and they look like more of the
readiness family, but that is a guess and they have not been diagnosed. **Steps 44 and 45 are not
done**, and step 45's done-when (five consecutive clean runs, the seven skips removed) is not met.

### Stage I — The quiet run

This is the user's goal, and it is mostly new work.

#### Step 46 — Watch the foreground, and make the claim a test

**Why.** Step 15's done-when is a person typing in Visual Studio. That check has never been
performed, and it cannot be performed in CI. Everything else in this programme is measured; this
is the last claim taken on trust, and it is the claim the whole thing is for.

**Do.** A watchdog owned by the fixture, running for the life of the collection.
`SetWinEventHook(EVENT_SYSTEM_FOREGROUND)` is the precise instrument: it fires when any window
takes the foreground, out of process, with no polling. Record every occasion the app under test
takes it, with a timestamp and the test that was running.

Assert at collection teardown: **the app under test never held the foreground.** Launch is the
known exception until step 47, so the first event is expected and named; anything after it is a
finding.

Report the log to `TestResults/<run-id>/suites/<suite>/` per `AD-007`, like the accessibility
audit — a run that stole focus twice should say when.

**Verify.** Run the suite and read the report. The number is unknown today; the point of this step
is that it stops being unknown.

**Done when** a test fails if the app takes the foreground during a collection, and the current
count is written down.

#### Step 47 — Launch without the foreground

**Why.** `UseShellExecute = true` hands the new process the foreground before anything can stop
it, so every collection start puts the app in front of the person at the machine for a moment.
This is the one focus grab that is certain to happen.

**Do.** Launch with `UseShellExecute = false` and a `STARTUPINFO` asking for a non-activating
show. That also makes the environment block explicit, which removes the reason
`BRINELL_UIA_BRIDGE` is currently set on *this* process and inherited — a wart from step 27 worth
paying off here.

Take the same pass over `EnsureRootWindowFocused`, whose un-minimize runs before the policy check.

**Verify.** Step 46's watchdog. This step is exactly what makes its launch exception unnecessary.

**Done when** the watchdog records zero foreground events for a whole run, launch included.

#### Step 48 — Keep it behind, not just put it behind

**Why.** `SendAppBehind` runs once. Nothing re-demotes a window that rises later, and nobody knows
whether one does.

**Do.** Only after 46 has measured it. If the answer is "never", this step is a comment saying so
and nothing else — which is the right outcome and worth the measurement to earn. If it does rise,
re-demote on the same hook that noticed, and count it.

**Consider `BRINELL_AUT_PLACE=offscreen` as the stronger default.** A window beyond every monitor
cannot be raised into anyone's way. It is already built. The reason not to make it the default is
that it makes screenshots harder to reason about for a person debugging — though `WindowCapture`
already handles an obscured window, so that reason may not survive contact.

**Done when** the app's z-order behaviour over a full run is a measured number rather than an
assumption.

#### Step 49 — Decide the default

**Why.** Today the quiet run is opt-in: `BRINELL_BACKGROUND_MODE=1` or the machine gets taken.
The goal is phrased as what Brinell *does*, not what it can be asked to do.

**Do.** Make refused the default for the MAUI FlaUI driver, with the loud path opt-in
(`BRINELL_BACKGROUND_MODE=allow`, or the existing `PhysicalInput.OverridePolicy` for a test that
means it). The two `Deliberate` tests keep working because they already declare themselves.

**This one is a judgement call and should be confirmed rather than assumed.** Flipping it changes
what a local `dotnet test` does for anyone who currently watches a run happen. The recommendation
is to flip it: a framework whose default takes the machine is one people run less often. But the
person who runs this suite daily should say so.

**Done when** a plain `dotnet test` with nothing set takes neither the keyboard, the pointer, nor
the foreground.

### Stage J — Clearing the board

Everything still open in `steps.md`, in the order it is worth doing. Ordered by certainty first:
the cheap and definite go early, because a board with fewer rows on it is easier to reason about
than a board with better rows on it.

**J1. Retire what is already finished (30 min, no risk).** Steps 36, 37, 39 and 40 are resolved
and still read as open. 39 is upstream and guarded - it will never be "done" and should say
**closed** rather than parked, or it stays on the list forever.

**J2. The four flaky tests left after step 43 (the tail).** `DatePicker_SetDate_UpdatesDisplay`,
two of `AlertReadTests`, `UndeclaredVerb_OnADeclaredRow_IsRefused`. Each passes alone, each fails
about one run in three under load. **Diagnose before fixing** - they are assumed to be more of the
readiness family and that is a guess. This blocks step 45's done-when, so it comes before 45.

**J3. Step 33B - two negative assertions pay 10 s each.** `Item(key, timeout)` waits before
throwing, correctly; the assertion is about *which exception*, not how long it waits first. Two
constants, 19 s back.

**J4. Step 38 - the clipboard canary.** Read the sentinel back after writing it; if it is not
there, the desktop's clipboard is unavailable and there is nothing to canary. Three lines, and the
file already says to do it.

**J5. Step 44 - actions do not Try.** `TryAppendText`, `TryClearFocus`, `TryNavigateBack` become
`AppendText`, `ClearFocus`, `NavigateBack` + `IsAtNavigationRoot`. Step 43 gave the last one
something honest to return.

**J6. Step 45 - remove the seven step-36 skips.** They name arbitrary victims of a fault that is
now fixed. Then five consecutive full runs.

**J7. Step 31 - the Stepper, 11 tests.** `TestStepper` does not resolve on Windows.
`AutomationProbeTests` already reports which container types are addressable; run it against the
Range page and find out whether the Stepper publishes an `AutomationId` at all. If it does not,
this belongs with `SwipeView` and `RefreshView` as a control the bridge reaches and the tree does
not - and step 29's audit already lists exactly that category.

**J8. Step 41 - the run-time gate, measured against the app.** Step 29's audit walks the app and
counts published elements. The same walk against an app launched *without* `BRINELL_UIA_BRIDGE=1`
should count zero. One test, reusing what exists.

**J9. Step 42 - a bridge cached against a closed window.** Needs a second window in the sample app
that can be opened and closed on demand. Small feature, small test.

**J10. Step 35 - notice when the suite gets slower.** Per-area timings written to `TestResults`
per `AD-007`. Do it after J2-J6, when a baseline means something.

**J11. Step 32 - the Shell app, 13 tests.** A different app with no bridge. Its fixture's reset
hunts a button named "Open Navigation"; step 26's flyout verb replaces that guess, which is where
to start. The largest item here and the least connected to the goal.

**J12. Step 30 - documentation and AD-008.** Last, as before: it should describe the vocabulary as
it finally is.

#### Stage J and stage I — results

**Confirmed by a person, 2026-09-13.** The user watched a full run while working in the editor
and the app stayed behind it throughout - the check step 15 asked for, performed after the fixes
below rather than before them.

**The window, which is what was actually reported.** "The screen still pops over the editor" was
measured before it was fixed: the watchdog counted **two foreground grabs per button click**,
navigating or not, and none for a read. Clicks go through `InvokePattern`, which is not physical
input and was never refused as such - and WinUI answers an invoke by activating the window.
Refusing input could not have caught it, because no input was sent.

`WS_EX_NOACTIVATE`, set by the driver on the app's window when input is refused, took it to **zero
grabs per click**. Only the launch grab remains, and the watchdog - now started the moment the
process starts, keyed on the process id rather than on a window handle nobody has found yet -
pushes that one back within milliseconds. `ForegroundWatchdogTests` fails if navigating the app
takes the foreground at all.

**Off-screen placement was tried and measured out.** Putting the window beyond every monitor
looked like the complete answer and broke visibility instead: UI Automation's `IsOffscreen`
counts the desktop, not just the scroll viewport, so with the window at x=-1144 a button plainly
inside its page reported `visible=False` and every "scroll into view, then wait" could time out.
Z-order leaves geometry alone; placement does not. `BRINELL_AUT_PLACE=offscreen` still exists for a
run that wants it knowingly.

**Launch.** `UseShellExecute` is now false with an explicit environment block. With the shell in
the middle, roughly one run in six came up with no bridge at all - an app that had simply never
been told to instrument itself, failing every test with "no element published" for seventeen
seconds at a time.

**Readiness, which is most of what "flaky" turned out to mean.**
- `Open` returns when the app names its top page and that page's root is in the tree - "the stack
  is deeper than the root" was tried first and is true the instant a push starts.
- `CurrentRoute` answers only from a page in the stack, the rule `NavigationDepth` got at step 43.
- `NavigationDepth` and `NavigateBack` retry through the moment a transition leaves the bridge
  empty. Telling "nobody can answer" from "nobody answered yet" by the count of decliners was
  tried and is wrong: between one page withdrawing and the next publishing there is nobody to ask.
- `FlaUIMauiDriver(IntPtr)` retries `FromHandle`, which throws rather than returning null for a
  window a few milliseconds young and took whole fixtures down with a COM error.

**Two misses of mine, found by the work above.** The Shell app never called
`UseBrinellGestureBridge()`, so after step 27 it published nothing - hidden until it was rebuilt.
And in Git Bash `/tmp` *is* the Windows temp directory, so one diagnosis overwrote its own bridge
log with the test output.

**J3, J4, J5, J6.** The two negative assertions wait 500 ms. The clipboard canary reports an
unwritable clipboard as inconclusive. `TryAppendText` and `TryClearFocus` are a `Supports…`
question and a command that throws - the old `false` meant both "no route" and "the app refused",
so the Entry typed into a read-only field the app had just declined to change. The seven step-36
skips are gone.

**Full suite, background mode, after all of it:**

| | Stage H start | Now |
|---|---:|---:|
| Passed | 248 | **265** |
| Failed | 11 | **0** |
| Skipped | 33 | 28 - steps 31 and 32, two CollectionView, and the two `Deliberate` tests |
| Duration | 3 m 17 s | 3 m 27 s, with seven more tests running |

Five consecutive full runs came back 265 passed with only the two `Deliberate` tests failing -
step 45's bar - and those two now skip themselves with a sentence under refused input
(`PhysicalInputFact`), because a suite that is only green under the right filter is one whose red
nobody reads. The run after that change: **0 failed**.

**The intermittent "app freeze" is root-caused and fixed** - see
[../fix/rca-app-freeze-was-a-stale-root.md](../fix/rca-app-freeze-was-a-stale-root.md). The app never
froze: the bridge's `UiaDisconnectProvider` on page unload made UI Automation retire the driver's
element for the app's window, and the driver held that one element for the whole run. Withdrawn
targets are now retired instead of disconnected, and the driver re-attaches a retired root. A
150-page stress walk that failed within 3-38 page changes now completes with zero re-attachments;
the full suite with nothing set is 266 passed, 29 skipped, 0 failed.

**After steps 32 and 35, with nothing set: 297 tests, 281 passed, 16 skipped, 0 failed, 3 m 10 s.** The
sixteen skips are the Stepper's eleven (step 31, re-parked with its finding), two CollectionView rows
MAUI recycles, the two tests of real input (skipped because quiet is now the default), and the opt-in
stress reproduction. The leading hypothesis, not yet evidenced, is `UiaDisconnectProvider` on the UI
thread during a page's `Unloaded` - a call step 28 showed reaches out to the client - racing a
client call into the app.

## 4. Order, and why

```
43 (values, not S_FALSE)
     └── 44 (no Try)
              └── 45 (the walk; closes 36; skips removed)   ← the suite goes green here
                        │
                        ├── 46 (watch the foreground)       ← the goal becomes measurable
                        │        └── 47 (launch quietly)
                        │                 └── 48 (keep it behind)
                        │                          └── 49 (make it the default)
                        │
                        └── 30 (document it, once it has stopped moving)

Stage J at any point: 38, 33B, 35, 41, 42, 31, 32, 39
```

**Stage H first because everything after it is measured against a full run**, and a full run that
fails nine tests for one reason cannot tell you whether your change added a tenth. That is not
theoretical: this programme has twice read a real regression as baseline noise, and once the
reverse.

**46 before 47 and 48** because the fixes are cheap and the measurement is the valuable part. It is
entirely possible the window never rises after launch, in which case 48 is a comment — and the
only way to find out costs less than guessing wrong in either direction.

**49 last of the quiet-run steps** because flipping the default before the behaviour is proven
would mean shipping a promise rather than a property.

---

## 5. What this plan does not claim

**Android and iOS are untouched.** The bridge is Windows-only by design; mobile drives gestures
with real touch through Appium, where "taking the machine" means something different and is not
this goal.

**WPF and WinForms keep their physical-input sites** — 19 of them between the two. They record
themselves through the same `PhysicalInput` machinery, so the inventory is honest, but no verb
layer exists for them and none is planned here.

**A green suite is not a fast suite.** Step 35 exists because nothing notices when the suite gets
slower, and nothing in stages H or I changes that.

**The accessibility backlog is output, not work.** Step 29's report lists 11 elements with no
route but the pointer. Acting on it is the app's job, not the framework's — the point of the list
is that somebody can.
