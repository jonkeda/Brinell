# Improvement Proposal: Wait For The Next Action, Not After The Last One

**Date:** 2026-08-31
**Status:** Proposal — **not implemented, and not currently justified by evidence.** See §0.

---

## 0. Tested against the motivating failure — which had a different cause

The proposal was written to explain `ImageButton_IsExists_ReturnsTrue` failing on Android
while passing on Windows. Following §7 step 1, `EnsureLoaded` was instrumented to log
`immediate=False afterWait=?`, and the Button suite re-run.

**The diagnostic never fired.** Not once, across three consecutive runs. `EnsureLoaded` was
never false, so there was no window between navigation finishing and the page being findable —
the timing gap this proposal exists to close was not occurring.

The real cause was **a degraded emulator**. The AVD had been running for hours across many
Appium sessions; on a fresh 4 GB instance the failure vanished:

| | Android Buttons + ImageButtons |
|---|---|
| Degraded emulator | 10 / 11 |
| Fresh emulator | **11 / 11**, stable over 3 runs |

Windows is 11 / 11 in the same state.

**So the change in §3.2 is not made.** The mechanism it describes is real and the analysis of
`EnsureLoaded` taking the instantaneous branch is accurate — but no measured failure currently
depends on it, and §5 of the parent plan is explicit that an unused mechanism is a liability.

**This document stays as a design, not a backlog item.** Revisit it when a failure actually
shows `immediate=False afterWait=True` — the instrumentation in §7 step 1 is the test for
that, and it takes one run. Until then, the honest position is that Brinell's readiness
handling is sufficient for the cases measured.

**A separate finding worth keeping:** a long-lived Android emulator degrades in ways that look
exactly like product bugs. This is the second time in this project
([plan-fix-hub-navigation.md](plan-fix-hub-navigation.md) records the first, an
out-of-memory kill read as a silent crash). **Restart the AVD before diagnosing an
Android-only failure**, and check `/proc/meminfo`.

---
**Scope:** `PageObjectBase`, `ViewBase`, `ContainerObjectBase` in `srcnew/Brinell.Maui`
**Related:** [rca-002](rca/rca-002-page-precondition-discarded-slow-failures.md) (the
precondition this builds on), [plan-fix-hub-navigation.md](plan-fix-hub-navigation.md) (the
failures that motivated it)

---

## 1. The idea

> **An action waits for itself to become possible.** Not for a duration, and not for something
> to settle after the previous action.

A test says *click the button*. Brinell's job is to make that mean: the page this button lives
on is present, the button is there, it is visible and enabled — **then** click. Today the
caller is expected to arrange all of that beforehand, and when they forget, the failure is a
timeout against the element rather than a statement about what was missing.

The pieces already exist. `IsLoaded()` is on every page object and is exactly the right
question. It is simply never *waited* on.

---

## 2. What is wrong today

### 2.1 The precondition is checked, but not waited for

[RCA-002](rca/rca-002-page-precondition-discarded-slow-failures.md) fixed the first half of
this: element lookups now consult the page. But look at what they consult:

```csharp
private bool CanResolveElements() => !RequiresLoadedPage || EnsureLoaded();

private bool EnsureLoaded() => IsLoaded();      // no timeout

public virtual bool IsLoaded(int? timeoutMs = null)
{
    var timeout = timeoutMs ?? 0;               // ← defaults to zero
    return timeout > 0 ? Poll(IsVisiblePageRootLoaded, timeout)
                       : IsVisiblePageRootLoaded();
}
```

`EnsureLoaded` passes no timeout, so `IsLoaded` takes the **instantaneous** branch. The page
is asked *"are you there right now?"* at the exact moment a navigation may still be animating.
One millisecond too early and the answer is no — correctly, and uselessly.

`WaitLoaded` exists and does the right thing. Nothing on the action path calls it.

### 2.2 So callers compensate, inconsistently

Every navigation helper in `MauiFixture` ends with the same incantation:

```csharp
Open(SamplePage.AutomationProbe);
var page = AutomationProbePage;
page.WaitLoaded(true, TestConstants.DefaultTestTimeoutMs);   // remembered here
```

`Open` itself does not, because `Open` does not know which page is coming. So a test that uses
`Open` directly — as `ButtonTests` and `ImageButtonTests` do — gets no wait at all.

### 2.3 The measured consequence

`ImageButton_IsExists_ReturnsTrue` fails on Android and passes on Windows. The test is:

```csharp
Assert.True(page.TestImageButton.IsExists());
```

`IsExists()` takes no timeout by design — it is a question, not a wait. Combined with an
`Open` that returns as soon as the click is dispatched, the probe runs while the page is still
arriving. Windows' push animation is fast enough to hide it; Android's is not.

**Same test source, same control, different platform — which is a direct failure of goal (c).**
And the difference is not in the control or the platform: it is that nothing waited.

### 2.4 The wrong fix, already tried

An earlier attempt made `Open` wait for the *hub to disappear*. Windows got **worse** (Display
tier 14 → 8 passing). Waiting for the old thing to go is satisfied by the animation starting;
waiting for the new thing to arrive is not. This proposal is the second kind.

---

## 3. Proposal

### 3.1 The rule

> Before an action or a query touches an element, the scope it belongs to must be **ready**,
> and readiness is **waited for**, not sampled.

Three consequences:

- `Open(page)` needs no trailing wait — the first thing the test does on that page waits for
  the page itself.
- `WaitLoaded` calls at every call site disappear. They are the symptom.
- A test never waits for time. It never waits "after" anything. It states an intent, and the
  intent brings its own precondition.

### 3.2 The change

**One line, in `PageObjectBase`:**

```csharp
// Before: instantaneous — asks whether the page happens to be there this instant.
private bool EnsureLoaded() => IsLoaded();

// After: waits for the page to become ready, because that is what a caller means.
private bool EnsureLoaded() => IsLoaded(_context.Timeouts.PageLoad);
```

`IsLoaded` already polls when given a timeout, and `_ensuringLoad` already guards the
re-entrancy (`IsLoaded` resolves elements through this same scope). **The mechanism is
complete; only the argument is missing.**

`ContainerObjectBase` needs the mirror change wherever it resolves its root, so a container
waits for its own root the same way.

### 3.3 What must not change

- **`IsLoaded()` and `IsExists()` stay instantaneous when called directly.** They are
  questions a test asks deliberately; making them wait would make "is this absent?" take a
  full timeout to answer. Only the *internal precondition* waits.
- **The failure message stays as RCA-002 made it.** After the wait expires, the exception
  still names the page:
  `Page 'ButtonsTestPage' is not loaded, so 'AutomationId:TestImageButton' cannot be found`.
  A wait that times out must say what it was waiting for.
- **`RequiresLoadedPage => false` still opts out**, unchanged, for scopes that legitimately
  resolve elements outside their own root (`HubPage`, `AppShellPage`).

### 3.4 Why this is cheap when things are fine

A polling wait costs nothing when the condition already holds — `Poll` checks before it
sleeps. On Windows, where the page is already there, the added cost is one element lookup that
was happening anyway. The cost is paid only where a wait was genuinely needed, which is
precisely where a test is failing today.

---

## 4. Extending the idea: readiness per action

The page-level fix is the first and most valuable step. The same principle generalizes, and is
worth stating even if only step 1 is built:

| Action | What it should wait for |
|---|---|
| `Click()` | page ready → element present → visible → **enabled** |
| `SetText()` | page ready → element present → visible → editable |
| `AssertX()` | page ready → element present (then poll the assertion) |
| `IsExists()` | page ready only — presence is the question being asked |

`ClickCore` already calls `EnsureClickableCore`, and `RunDoWithElement` already calls
`EnsureVisible`. **The ladder is nearly there.** What is missing is the top rung: the scope
itself.

Note the asymmetry in the last row. `IsExists()` should wait for the *page* but not for the
*element* — otherwise `IsExists() == false` costs a full timeout to establish. That
distinction is what makes this an improvement rather than "wait longer everywhere."

---

## 5. Why this is the right shape

**It moves waiting to where the knowledge is.** The control knows it needs an element; the
element's scope knows whether it is ready. The test knows neither and should not have to.

**It removes a class of test, not an instance.** Any test that navigates then immediately
queries is currently one animation frame from flaky. This makes that construction correct by
default.

**It serves goal (b) directly** — *fewer flaky tests through common handlers*. A wait that
lives in `RunPoll` and `EnsureLoaded` is one implementation, tested once, applied everywhere.
A `WaitLoaded` call remembered at each site is 30 chances to forget.

**It serves goal (c)** — the `ImageButton` failure is a Windows/Android split caused purely by
animation speed. Absorbing it below the test body is exactly what §4.5 asks for, at tier 0:
no adapter, no capability probe, just waiting for the right thing.

---

## 6. Risks

| Risk | Response |
|---|---|
| A genuinely-absent page now costs a full `PageLoad` timeout instead of failing fast | It already did — the caller just paid it in `ElementFind` instead, with a worse message. Net timing is comparable; the message is better |
| Masks a real navigation bug by waiting it out | The wait is bounded and the timeout message names the page. A navigation that never completes still fails, and says so |
| `IsExists()` becomes slow | It must not. §3.3 keeps direct queries instantaneous; only the internal precondition waits |
| Re-entrancy: `IsLoaded` resolves elements through the scope it is gating | Already handled — `_ensuringLoad` short-circuits the inner call. Verify it still holds when the outer call polls |

---

## 7. Suggested order

1. **Prove the diagnosis.** Instrument `EnsureLoaded` to log its result on the failing Android
   `ImageButton_IsExists` run. Expect false-then-true within a second. If not, stop.
2. **Change `EnsureLoaded` to wait** (§3.2). Re-run Android Button tests — expect 11 / 11.
3. **Run the full Windows suite.** Expect no regression, and possibly fewer failures: several
   of the 23 are "element not found immediately after navigation."
4. **Remove the now-redundant `WaitLoaded` calls** from `MauiFixture`, one at a time, checking
   the suite after each. Each removal that stays green is a caller that no longer has to
   remember.
5. **Then consider §4** — per-action readiness — only if step 3 leaves failures that it would
   address.
