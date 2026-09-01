# Plan: Wait For Readiness — `IsLoaded` First, Then The Element

**Date:** 2026-08-31
**Status:** ✅ **Requirement already satisfied — verified by test, no production change needed.**
The design below was proposed; §0 records what measurement showed instead.

---

## 0. Outcome: throw-and-let-`RunPoll`-poll is already the design

The question raised against §3 was: *should `EnsureLoaded` throw and let `RunPoll` do the
polling, rather than polling internally?*

**Yes — and that is what the code already does.** Reading the chain again, with the
exception-handling in view this time:

```csharp
// PageObjectBase — the action path already throws
IMauiElement IElementScope<IMauiElement>.FindElement(Locator locator)
{
    if (!CanResolveElements()) throw PageNotLoaded(locator);   // ← throws
    return _context.FindElement(locator);
}

// ViewBase.RunPoll — already catches, retries, and rethrows the last failure
catch (Exception ex)
{
    lastException = ex;
    // Polling expects transient failures (stale elements, not-yet-rendered)
}
```

A not-yet-loaded page **is** a not-yet-rendered condition. `RunPoll` was built for exactly
this, and `FindElement` already raises it. The same holds for `EnsureEnabledCore`, which throws
`TimeoutException` and is likewise caught and retried, and for containers, whose
`FindContainerRootElement` throws through `_parentScope.FindElement`.

### Proved, not assumed

`testsnew/Brinell.Maui.Tests/Semantic/ReadinessTests.cs` — **5 tests, all passing against
unmodified production code**:

| Test | Proves |
|---|---|
| `Click_WaitsForThePage_WhenItBecomesLoadedLate` | A page ready only on the 3rd check is waited for; the click lands |
| `Click_FailsNamingThePage_WhenItNeverLoads` | Failure names the **page**, per RCA-002 |
| `Click_WaitsForTheControl_WhenItBecomesEnabledLate` | A control enabled on the 3rd read is waited for |
| `Click_FailsNamingTheLocator_WhenTheControlNeverEnables` | Failure names the **locator**; no click attempted |
| `IsExists_ReturnsFalseImmediately_WhenThePageIsNotLoaded` | Queries stay instantaneous (<500 ms) |

### Why the proposed §3 changes are withdrawn — they would have been harmful

**§3.1 (`EnsureLoaded` polls internally) and §3.2 (`EnsureEnabledCore` polls internally) are
both wrong**, and not merely redundant:

1. **Nested polling multiplies timeouts.** `RunPoll` already loops for the caller's timeout.
   An inner `IsLoaded(PageLoad)` would block for the full page timeout *inside* each outer
   iteration. With `DefaultWait = 5 s` and `PageLoad = 10 s`, the first inner call alone
   exceeds the outer budget — the caller's timeout stops meaning anything.
2. **It takes control away from the caller.** `Click(timeoutMs: 2000)` should mean "give up
   after 2 s", readiness included. A separate internal timeout breaks that.
3. **Two retry loops for one condition.** One is testable and one place to change.

**§3.3 (containers) is unnecessary** for the same reason: the container path throws and is
polled identically.

**What stands from this plan:** §2's rule (an action waits for itself to become possible, in
order), §3.4's constraint (direct queries stay instantaneous), and the tests in §4 step 2 —
which are now written and passing, and pin the behaviour so a future change cannot quietly
remove it.

**Net production change: none.** The deliverable is the test file.

### 0.1 One real gap the tests did find: retry replays the action

Asking "so nothing needs improving?" turned up something the five tests above did not cover.
`RunPoll` retries its **whole body** on any exception, and the body of `RunDoWithElement` ends
with the action:

```csharp
RunPoll(null, () =>
{
    var element = FindElement();
    EnsureVisible(element, DefaultTimeoutMs);
    coreOperation(element);        // ← the click. If anything throws after this…
    return true;
});
```

So a driver that **acts and then throws** has its action replayed. Measured, not argued:
`Click_IsPerformedTwice_WhenTheDriverThrowsAfterActing` shows `clicks == 2`.

The realistic trigger is ordinary: a click that navigates away leaves the element stale, and
some drivers raise on the response *after* the tap has landed. The result is a silent double
action — a counter incremented twice, an item added twice, a form submitted twice. **It is the
same shape as the Android double-tap in
[plan-fix-hub-navigation.md](plan-fix-hub-navigation.md), reached by a different route.**

The test asserts the *current* behaviour deliberately, so a fix breaks it visibly rather than
changing it by accident.

**Not fixing it here**, because the fix is a real design decision and this document is about
readiness. The options, for whoever takes it:

| Option | Trade-off |
|---|---|
| Retry only the *resolution* (find/visible/enabled), never the action | Correct, and the smallest change: split the body so `coreOperation` runs once, outside the poll |
| Make `RunPoll` retry only on a whitelist of resolution exceptions | Keeps one loop, but the whitelist becomes a maintenance surface |
| Leave it, and require Core methods to be idempotent | Cheapest, and wrong — a click is not idempotent and cannot be made so |

The first is almost certainly right: **poll to get ready, then act once.** That is the same
principle as §2, applied one level in.

### 0.2 Built — and it exposed the readiness gap the retry had been hiding

Implemented as option 1. Two changes, in `ViewBase` and `PageObjectBase`.

**Change 1 — split resolution from action.** `RunDoWithElement` and `RunSetWithElement` now
call a private `ResolveReadyElement`, which polls; the action runs after the loop, once.
Readiness that used to live inside the action moved into the poll via a new
`EnsureReadyForActionCore` hook — `ClickableControlBase` overrides it to check *enabled*, so
"waits for a control enabled late" still holds.

**Change 2 — the query path waits for the page.** This is the one the split forced into the
open. With the action no longer retried, `Open()` returned sooner, and
`ImageButton_IsExists_ReturnsTrue` began failing **deterministically, in 1 second**, in
isolation.

That was not a regression. **The old retry loop had been supplying an accidental delay** — a
click that navigated away made the element stale, `RunPoll` caught it and retried, and the
retry's own timeout gave the next page time to render. The double-action bug and the
missing page-wait were the same loop papering over each other.

So the two paths need opposite treatment, and now get it:

| Path | Waits how | Why |
|---|---|---|
| `FindElement` (actions) | Throws; the caller's `RunPoll` retries | A wait here would nest one poll inside another |
| `TryFindElement` (queries) | Waits for the page itself | Nothing polls above a query — `IsExists()` asks once and returns |

The query waits for the **page**, never the **element**. Being on the page is a precondition
for the question meaning anything; the element's absence is the answer. Without that
distinction every `AssertExists(false)` would cost a full timeout.

**One trap found while building:** passing a timeout to `IsLoaded(timeoutMs)` does nothing.
Page objects routinely override it to check a signature control — `ButtonsTestPage` returns
`StatusLabel.IsExists()` — and those overrides ignore the parameter. `EnsureLoaded` therefore
polls the **no-argument** form itself rather than delegating the wait to an override that will
drop it.

### Result

| | Before | After |
|---|---|---|
| Android Buttons + ImageButtons | 10 / 11 (deterministic failure) | **11 / 11**, stable over 3 runs |
| Windows Buttons + ImageButtons | 11 / 11 | **11 / 11** |
| `Brinell.Maui.Tests` | 83 passed / 6 pre-existing | **84 passed / 6 pre-existing** |
| Readiness tests | 6 | **7** |

`Click_IsPerformedOnce_WhenTheDriverThrowsAfterActing` replaces the test that pinned the old
double-action behaviour, so the guarantee is now asserted rather than merely observed.

---

## Original plan (superseded by §0)

**Status:** Plan — to be built and tested before other work continues
**Scope:** `PageObjectBase`, `ContainerObjectBase`, `ViewBase` in `srcnew/Brinell.Maui`
**Verified against:** `Button` and `ImageButton` — the simplest controls, currently 11/11 on
both Windows and Android, so any change that breaks them is visible immediately.

**Supersedes:** the "not currently justified" conclusion in
[improvement-wait-for-next-action.md](improvement-wait-for-next-action.md) §0. That judgement
was made on the narrow question *"does a failing test require this?"*. The requirement here is
different and stronger: **waiting for readiness is part of what an action means**, and it must
be correct by construction rather than correct by luck of timing.

---

## 1. What `Button.Click()` does today

Traced through the actual code, not from memory:

```
page.TestButton.Click()
 └─ ClickableControlBase.gen.cs        Click(timeoutMs)
     └─ ViewBase.RunDoWithElement(...)
         └─ RunPoll(...)                          ← retries the whole body on failure
             ├─ FindElement()
             │   └─ PageObjectBase.FindElement(locator)
             │       ├─ CanResolveElements()
             │       │   └─ EnsureLoaded()  →  IsLoaded()        ← ⚠ NO TIMEOUT
             │       └─ _context.FindElement(locator)            ← polls ElementFind (3 s)
             ├─ EnsureVisible(element, DefaultTimeoutMs)         ← waits
             └─ ClickCore(element)
                 ├─ EnsureClickableCore → EnsureEnabledCore      ← ⚠ NO WAIT, throws
                 └─ TryActivateByPattern → element.Click()
```

**Three rungs of a four-rung ladder already wait.** `_context.FindElement` polls for the
element; `EnsureVisible` polls for visibility; `RunPoll` retries the whole sequence. The two
that do **not** wait are the ones at the ends:

| Rung | Waits? | Consequence when it is early |
|---|---|---|
| Page is loaded | **No** — `IsLoaded()` with no timeout | Throws `PageNotLoaded` immediately, or falls through to a 3 s element search that cannot succeed |
| Element present | Yes (3 s) | — |
| Element visible | Yes | — |
| Element **enabled** | **No** — `EnsureEnabledCore` throws | Throws `TimeoutException` on a control that is one frame from being enabled |

`RunPoll` retrying does *partly* mask both, which is why tests mostly pass. But it masks them
by re-running the whole action, so a failure inside `ClickCore` is retried too — and the
diagnostic is a timeout, not "the page was not ready".

**This is the gap: readiness is checked instantaneously and only survives by retry.**

---

## 2. The rule

> **An action waits for itself to become possible, in order: scope ready → element present →
> visible → enabled → act.**
>
> Every rung waits. A rung that cannot be satisfied within its budget fails naming *that rung*.

Two things this is not:

- **Not "wait longer".** No timeout is increased. `IsLoaded` gets the timeout it already
  accepts and currently defaults to zero.
- **Not "wait after".** Nothing waits for a previous action to settle. Each action asserts its
  own preconditions, which is what makes the ordering composable.

---

## 3. Changes

### 3.1 `EnsureLoaded` waits — the core change

```csharp
// PageObjectBase
private bool EnsureLoaded()
{
    if (_ensuringLoad) return true;
    try
    {
        _ensuringLoad = true;
        return IsLoaded(_context.Timeouts.PageLoad);   // was: IsLoaded()
    }
    finally { _ensuringLoad = false; }
}
```

`IsLoaded` already polls when given a timeout. `_ensuringLoad` already guards the recursion
(`IsLoaded` resolves elements through this same scope). **Only the argument is missing.**

### 3.2 `EnsureEnabledCore` waits

```csharp
// ClickableControlBase — currently throws on the first false reading
protected virtual void EnsureEnabledCore(IMauiElement element)
{
    if (IsEnabledCore(element) == true) return;

    if (!RunPoll(null, () => IsEnabledCore(element) == true, DefaultTimeoutMs))
    {
        throw new TimeoutException(
            $"Element was not enabled within {DefaultTimeoutMs}ms. Locator: {Locator}");
    }
}
```

Mirrors `EnsureVisible`, which already does exactly this shape. A button enabled by a binding
that resolves a frame later is the ordinary case this covers.

### 3.3 `ContainerObjectBase` mirrors the page

Wherever a container resolves its root for an action, it waits for that root the same way. A
container is a scope; the rule is per-scope, not per-page.

### 3.4 What must NOT change

- **`IsLoaded()` and `IsExists()` stay instantaneous when a test calls them directly.** They
  are questions. Making them wait means `IsExists() == false` costs a full timeout to
  establish, and `AssertExists(false)` becomes unusable.
  **Only the internal precondition waits.**
- **`RequiresLoadedPage => false` still opts out**, unchanged — `HubPage` depends on it.
- **The `PageNotLoaded` message stays as RCA-002 wrote it.** After the wait expires it must
  still name the page, not the element.

---

## 4. Build and test order

Buttons are the subject throughout: simplest control, no state, and **currently 11/11 on both
platforms**, so this starts from green and any regression is unambiguous.

### Step 1 — Baseline, both platforms

```
Windows: --filter "Control=Button|Control=ImageButton"     expect 11/11
Android: same, APPIUM_PLATFORM=android                     expect 11/11
```

**Restart the emulator first.** A long-lived AVD has twice produced failures that looked like
product bugs (see [improvement-wait-for-next-action.md](improvement-wait-for-next-action.md)
§0). Confirm `/proc/meminfo` shows healthy free memory.

### Step 2 — Unit-test the rule before wiring it

`Brinell.Maui.Tests` mocks `IMauiElement` and `IMauiTestContext`, so readiness ordering is
testable without a device. Add to a new `ReadinessTests`:

- Page not loaded at first, loaded on the 3rd poll → `Click` succeeds, does not throw.
- Page never loaded → throws, message names the **page**.
- Element disabled at first, enabled on the 3rd poll → `Click` succeeds.
- Element never enabled → throws `TimeoutException` naming the **locator**.
- `IsExists()` on a missing element → returns false **without** consuming the page timeout
  (guards §3.4 — assert on elapsed time).

**These tests are the deliverable, not the UI runs.** They pin the ordering permanently; the
UI runs only confirm nothing regressed.

### Step 3 — Make the changes

§3.1, then §3.2, then §3.3 — one at a time, unit tests green after each.

### Step 4 — Verify on Buttons, both platforms

Windows and Android Button + ImageButton, expect 11/11 on both. Then run each **twice**: a
readiness change that helps must not introduce variance.

### Step 5 — Widen carefully

Run the full Windows suite. Expect no regression, and possibly fewer failures — several of the
current failures are "element not found immediately after navigation", which is what §3.1
addresses. Any *new* failure is a control that was relying on the instantaneous check, and is
a finding to record rather than a reason to revert.

---

## 5. How we will know it works

Not "the tests pass" — they already do. The claims to demonstrate:

1. **A page that becomes ready late is waited for.** Provable in a unit test (step 2), not by
   hoping a device is slow.
2. **A control enabled late is waited for.** Same.
3. **`IsExists()` is still fast when absent.** Measured, because §3.4 is the constraint that
   stops this becoming "wait everywhere".
4. **Buttons stay 11/11 on both platforms**, twice in a row.

---

## 6. Risks

| Risk | Response |
|---|---|
| A genuinely-absent page now costs `PageLoad` before failing | It already cost `ElementFind` with a worse message. Bounded, and the message names the page |
| `IsExists()` becomes slow | Explicitly tested in step 2. If it regresses, the change is wrong |
| Waiting masks a real navigation bug | The wait is bounded; a navigation that never completes still fails and says which page |
| Re-entrancy: `IsLoaded` resolves elements through the scope it gates | `_ensuringLoad` handles it today; step 2 adds a test that it still holds when the outer call polls |
| Emulator degradation misread as a regression | Step 1 restarts the AVD and records free memory before any change |
