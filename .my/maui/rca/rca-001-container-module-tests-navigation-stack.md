# RCA-001: ContainerModuleTests Fails 9 of 10 Tests and Takes 7 Minutes

**Reported:** 2026-08-29
**Severity:** High — blocks a whole test class and makes the suite unusably slow
**Status:** Root cause confirmed; fix applied
**Component:**
- `testsnew/Brinell.Maui.UITests/MauiFixture.cs` (`NavigateToContainerModule`)
- `samples/Brinell.Samples.Maui.App/Views/AutomationProbeView.xaml.cs` (`Go`)

**Pre-existing:** Yes. Reproduced identically with the phase-1 helper-removal work stashed.
Not caused by that change.

---

## Symptom

Running the class:

```
dotnet test testsnew\Brinell.Maui.UITests --filter "FullyQualifiedName~ContainerModuleTests"
```

- **9 of 10 tests fail**, each in **1 ms**, each with an **empty error message**.
- The run takes **7 minutes** despite every test having `Timeout = 15_000`.
- Any one of those tests **passes in ~1 second** when run alone.

The last symptom is the one that identifies the bug: the tests are fine, the sequence is not.

---

## Evidence

### E1 — Failures are in the constructor, not the test body

Every failure reports 1 ms with no message. A test that fails in 1 ms never executed. xUnit
reports a constructor throw against each test in the class, with the duration of the test
body (nil) rather than the constructor. Verbose output confirms the stack:

```
at Brinell.Maui.Controls.Base.ViewBase`1.RunPoll(...)          ViewBase.tpl.cs:119
at Brinell.Maui.Controls.Base.ViewBase`1.RunDoWithElement(...) ViewBase.tpl.cs:246
at Brinell.Maui.Controls.Base.ClickableControlBase`1.Click(...) ClickableControlBase.gen.cs:24
at Brinell.Maui.UITests.MauiFixture.NavigateToContainerModule() MauiFixture.cs:157
```

`MauiFixture.cs:157` is `probe.GoToContainerButton.Click();`.

### E2 — The failures are evenly spaced, ~47 s apart

```
00:00:48  Grid_DoesNotReachOtherContainersChildren [FAIL]
00:01:35  ScrollView_ScrollsToItsLastChild        [FAIL]
00:02:22  Border_ContainsItsChild                  [FAIL]
```

47 s per test × 9 ≈ 7 minutes. This is not one hang; it is nine identical waits.

The arithmetic matches the fixture exactly:

| Wait | Source | Budget |
|---|---|---|
| `GoToContainerButton.WaitExists(true, Short)` | `MauiFixture.cs:150` | 10 s |
| `GoToContainerButton.WaitExists(true, Default)` (retry) | `MauiFixture.cs:154` | 15 s |
| `GoToContainerButton.Click()` → `FindElement` poll | `MauiFixture.cs:157` | 15 s |
|  | **total** | **~40–47 s** |

### E3 — The app is alive and on the Container page

A screenshot taken while the run was "stuck" shows the sample app responsive, displaying
**Container Module Test**, with the **Probe** tab selected in the tab bar.

This rules out the two obvious explanations — the app never started, or the app crashed —
and points at the real one: the app is on the wrong page, and nothing brings it back.

### E4 — The first test passes

Exactly one test passes: the first to run. Every later one fails. The failure is therefore
caused by state left behind by the preceding test.

---

## Root Cause

`GoToContainerButton` exists **only on the AutomationProbe page**:

```xml
<!-- AutomationProbeView.xaml:55 -->
<Button AutomationId="GoToContainerButton" Text="Container module" Clicked="OnGoToContainer" />
```

and navigation to the Container page is a **Shell route push**:

```csharp
// AutomationProbeView.xaml.cs:22
private static void Go(string route) => Shell.Current?.GoToAsync(route);
```

`GoToAsync("ContainerPage")` **pushes** ContainerPage onto the Shell navigation stack. It
does not replace the tab's content. So after the first test:

```
Shell stack:  [Probe tab root]  →  [ContainerPage]   ← app sits here
```

The fixture's recovery for the second test is to click the Probe **tab**:

```csharp
_appShell.AutomationProbeTab.Click();                              // MauiFixture.cs:152
probe.GoToContainerButton.WaitExists(true, DefaultTestTimeoutMs);  // MauiFixture.cs:154
```

**Clicking an already-selected Shell tab does not pop that tab's navigation stack.** The
Probe tab is already the current tab — the click is a no-op, the pushed ContainerPage stays
on top, and `GoToContainerButton` is never visible again. This is exactly what E3's
screenshot shows: Container page displayed, Probe tab highlighted.

Three defects compound:

1. **The recovery does not recover.** Re-clicking the current tab cannot undo a route push;
   only popping the stack can.
2. **The failed recovery is not acted on.** Line 154 polls for the button and *discards the
   result*, then line 157 clicks unconditionally. The one place that knows recovery failed
   throws away the evidence, so the failure surfaces 15 s later as an unrelated
   `ElementNotFoundException` from `Click`, with no message explaining why.
3. **Recovery runs after the page-loaded assertion, not before.** `NavigateToAutomationProbe`
   calls `WaitLoaded(true, 15 s)`, but while a pushed route covers the probe page it is *not*
   loaded — so that wait was guaranteed to expire before recovery got a chance to run. The
   pop is what makes the page loaded, so it must precede the check.

Defect 1 makes it fail. Defects 2 and 3 are what make it cost 7 minutes, and defect 3
dominated: it was still costing ~15 s per test after 1 and 2 were fixed.

---

## Fix

**Pop the navigation stack instead of re-clicking the tab**, and **fail fast with a message
that names the problem** when recovery does not work.

`NavigateToContainerModule` now:

1. Clicks the probe tab.
2. **Pops any pushed route before asserting the page is loaded** (defect 3). If
   `GoToContainerButton` is absent, the app is on a pushed route, so it calls
   `Context.Driver.NavigateBack()` — the Shell back button visible in E3's screenshot — and
   re-checks, up to `MaxPops` times since a test may have pushed more than one page.
3. Only then calls `WaitLoaded`, which now succeeds immediately instead of expiring.
4. If the button is still absent after the pops, it **throws immediately** with a message
   naming the likely cause, rather than falling through to a `Click` that times out with an
   empty message.

The presence check uses a short 750 ms probe rather than `ShortTestTimeoutMs`: the button is
either rendered or the app is on a pushed route, and waiting longer cannot change which.
A full timeout there is paid on every test after the first, purely to confirm an absence the
pop is about to fix.

### The recovery belongs in `NavigateToAutomationProbe`, not the container path

Fixing only `NavigateToContainerModule` traded one failure for another. With the container
tests finally reaching the Container page, they began *leaving* the app there — and
`AutomationProbeTests`, whose constructor calls `NavigateToAutomationProbe()`, started
failing its 3 tests with the identical symptom (passes alone, fails after
ContainerModuleTests).

That is the same defect, in the method one level up: `NavigateToAutomationProbe` also only
clicked the tab. It had simply never been exposed, because the container tests used to fail
before they could push a route.

So the recovery moved into `NavigateToAutomationProbe`, which every probe-page caller goes
through, and `NavigateToContainerModule` now just delegates to it. One fix, one place, both
callers correct — and the duplicated retry block disappears.

**This is the part worth remembering:** a masked bug is not one bug. Fixing the visible
failure exposed a second instance that had been hidden behind it, and the fix was only right
once it was placed where both paths share it.

### Why not `Shell.Current.GoToAsync("//route")`

Using an absolute route in the sample app would also avoid the stack push. It was rejected:
the sample app is the system under test, and changing its navigation to suit the test would
hide the very behaviour Brinell exists to exercise. Route pushes are normal MAUI usage, and
the fixture must be able to recover from them.

### Why not `ResetAppState()`

Restarting the app between tests would work and is far slower — it is the sledgehammer this
RCA exists to avoid. `NavigateBack` is the targeted equivalent.

---

## Verification

| Check | Before | After |
|---|---|---|
| `--filter "FullyQualifiedName~ContainerModuleTests"` | 9 failed / 1 passed, **7m02s** | **10 passed / 0 failed, 16 s** |
| A single container test alone | passed, ~1 s | passed, ~1 s (unchanged) |
| Recovery failure diagnosis | empty message after 47 s | named exception, immediate |

Fixing the defects in order showed how much each cost:

| State | Result |
|---|---|
| Original | 9 failed, 7m02s |
| + pop the stack (defect 1 & 2) | 10 passed, 3m55s |
| + short presence probe | 10 passed, 2m31s |
| + recover before `WaitLoaded` (defect 3) | **10 passed, 16 s** |

The correctness fix alone left a 4-minute run that looked "green but slow"; the remaining
time was entirely expired timeouts. A green suite is not evidence that the waits are right.

### Full suite

Measured by stashing the change and re-running, so the comparison is the same machine and
the same app build:

| | Failed | Passed | Duration |
|---|---|---|---|
| Baseline (fix stashed) | 41 | 137 | 12m18s |
| With fix | **28** | **150** | **4m00s** |

13 tests recovered — the 10 `ContainerModuleTests`, the 3 `AutomationProbeTests` — and the
suite runs 8 minutes faster.

The 28 that still fail are the pre-existing set named in `AGENTS.md` (DatePicker, TimePicker,
Image, ProgressBar, Stepper, Switch). They are unrelated to navigation state and out of scope
for this RCA.

**`AGENTS.md` should be updated:** its known-failures note does not mention ContainerModule or
AutomationProbe, so anyone running the suite would have read 41 failures as "mostly expected"
rather than "13 of these are one fixable bug."

---

## Lessons

- **A masked bug is not one bug.** `NavigateToAutomationProbe` had the same defect the whole
  time; it only surfaced once the container tests started succeeding far enough to push a
  route. Fix the shared path, then re-run the neighbours.
- **A 1 ms failure with an empty message means the constructor threw.** It is not a test
  failure and should not be read as one.
- **Evenly spaced failures are a timeout signature.** Spacing that matches the sum of the
  configured waits identifies the stuck call without a debugger.
- **A test that passes alone and fails in a class is leaked state**, not a broken assertion.
- **Never poll and discard the result.** `WaitExists(...)` whose return value is unused
  converts a precise, immediate failure into a slow, anonymous one. Where this fix guards
  recovery, it acts on the result.
- Reaching for a longer timeout here would have made the suite slower and fixed nothing;
  the wait was never going to succeed.
