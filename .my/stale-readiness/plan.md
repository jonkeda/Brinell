# Stale elements in the readiness wait - plan

Goal: a control's public call spends its time budget on the control, not on one element
instance. When the platform replaces the element partway through, the next poll finds the new
one; the call honours the caller's `timeoutMs`; and a stale element is recognised as stale on
every platform, not only on Appium.

Status: draft, 2026-09-18. Nothing changed yet beyond the narrow `ToolbarButton` workaround
described in section 1.

Related:

- Found in: `.my/TodoApp/plan.md`, section 9, "Phase 4" (the Brinell `ToolbarButton` fix).
- Rules this touches: AD-004 (wait for state) in `docs/architecture/decisions.md`; the
  `maui-control` skill's R1/R2 and "Actions confirm their effect".
- Discussion: [resolve-ready-element.md](resolve-ready-element.md), on whether actions still need
  `ResolveReadyElement` after this plan, and on the log and budget gaps it found.
- Trace: [find-element-trace.md](find-element-trace.md), on the `FindElement`, `TryFindElement`
  and `FindElements` path from control to driver: two more nested waits and a merged lookup
  surface.
- Tests: `testsnew/Brinell.Maui.Tests/Semantic/ReadinessTests.cs` and `UntilTests.cs` (mocked
  elements, milliseconds); `testsnew/Brinell.Maui.UITests` (Windows, minutes); Todo UI suite.

---

## 1. What happens today

### The observed failure

The Todo edit page's Cancel is a MAUI `ToolbarItem`. On Windows MAUI replaces a toolbar item's
element whenever its command's `CanExecute` changes. Measured by UI Automation runtime id, around
a command that finishes:

```text
   5 ms  42.3410548.4.30  enabled=False
 206 ms  missing
 213 ms  42.3410548.4.65  enabled=True
```

A `Click()` whose lookup landed on `…4.30` waited 5 s for that instance to become visible, then
failed with `Element was not visible within 5000ms after scrolling into view`. It happened in
about one full Todo UI run in two. `ToolbarButton` now opts out of the visibility wait
(`ViewBase.RequiresVisibilityForAction => false`), which fixes toolbar buttons and nothing else.

### The mechanism

`ViewBase.RunPoll` owns the call's budget (`timeoutMs ?? DefaultWait`, 5 s by default) and
catches every exception as "transient". Inside it, four helpers do this on each iteration:

```csharp
var element = FindElement();
EnsureVisible(element, DefaultTimeoutMs);   // a second, full 5 s budget, pinned to this element
```

`EnsureVisible` scrolls once, then calls `WaitVisibleCore(element, true, timeout)`, which is
**another `RunPoll`** on the same element instance. So:

1. **One element can eat the whole budget.** If the found element is being replaced, the inner
   poll spends 5 s reading a dead element, and the outer poll then has no time left to find the
   new one.
2. **The caller's timeout is not honoured.** The inner wait is always `DefaultWait`:
   `Click(timeoutMs: 500)` can take 5 s, and `Click(timeoutMs: 20_000)` can burn its first 5 s
   on one element.
3. **Nested readiness and logging.** The inner `RunPoll` repeats the page-readiness gate and
   writes its own entry/exit log lines, so one call logs as two and waits for the page twice.
4. **The failure message misleads.** It says "not visible", when the element was gone.

### Where it happens

| Site | Pattern | Budget |
| --- | --- | --- |
| `ViewBase.ResolveReadyElement` (every action and setter via `RunDoWithElement`, `RunSetWithElement`) | find, `EnsureVisible(DefaultWait)`, `EnsureReadyForActionCore` | nested 5 s |
| `ViewBase.RunWaitWithElement` (generated `Wait*`) | find, `EnsureVisible(DefaultWait)`, read | nested 5 s |
| `ViewBase.RunGetWithElement` (generated `Get*`) | find, `EnsureVisible(DefaultWait)`, read | nested 5 s |
| `ViewBase.RunAssertWithElement` (generated `Assert*`) | find **once**, `EnsureVisible(DefaultWait)`, re-read; re-find only on `StaleElementReferenceException` | nested 5 s, and see "Stale detection" |
| `ToggleControlBase` Core (line 81), `RadioButton` Core (line 55) | `EnsureVisible(element, timeoutMs ?? DefaultWait)` inside a Core method | caller's, but pinned |
| 8 Core methods that act, then `Until` on the same element: `ToggleControlBase` (x2), `CarouselView`, `Stepper`, `MediaPlayPauseButton`, `DrawingView`, `Expander`, `RatingView` | act, then read back the element they were given | caller's, pinned |
| `ContainerObjectBase.ContainerRoot` cache | `IsCachedRootValid` reads `TagName`; a failure invalidates | to verify on FlaUI |

The `RunWaitWithOptionalElement` / `RunAssertWithOptionalElement` pair (absence-tolerant
members) already re-resolve on every poll and do not force visibility. They are the model.

### Stale detection works on Appium only

The platform-neutral code recognises a stale element by catching
`StaleElementReferenceException`, which is **Selenium's** type (it reaches `Brinell.Maui` through
its `Appium.WebDriver` reference). There are 11 such catches: `ViewBase.RunAssertWithElement`,
`ContainerObjectBase` (x2), `CollectionObjectBase` (x4), `ElementMatch`, `PageObjectBase` (x2) and
`TabMenuMarkup`; the `ItemContainerBase` remarks describe the same contract.

The FlaUI driver never raises that type. A removed UI Automation element fails with UIA's
`UIA_E_ELEMENTNOTAVAILABLE` (`0x80040201`), surfaced by FlaUI as its own exception or a
`COMException`. The driver knows the code (`FlaUIMauiElement` line 1753, `AppWindow`), but only
for bridge answers and window re-attach. So on Windows every one of those catches is dead code,
the error falls into `RunPoll`'s catch-all, and the poll retries **the same dead element** until
it times out.

---

## 2. What we want

1. **One budget per public call:** the caller's `timeoutMs`, or `DefaultWait`. Nothing inside
   the call starts a budget of its own.
2. **Re-resolve on every attempt** that did not succeed, unless there is a measured reason to pin
   (an action already performed must not be repeated: that rule stays).
3. **A stale element is a named, platform-neutral signal:** a Brinell `StaleElementException` in
   `Brinell.Core.Exceptions`, raised by both drivers and caught by the controls. Selenium's type
   stays inside the Appium driver.
4. **Messages say what was last observed:** "found but not visible", "found but disabled",
   "found, then gone (replaced 3 times)", and name the `Locator`.
5. **No change for a control that was working:** same members, same generated API, same
   semantics for null-skip, idempotence and "confirm the effect". Timing changes only where the
   old behaviour was wrong (the nested budget).

---

## 3. Design

### 3.1 `EnsureVisible` becomes one attempt, not a wait

```csharp
/// One attempt: visible now, or scroll once and look again. Throws NotVisibleException
/// (carrying what it saw) when still not visible; the caller's poll decides whether to retry.
protected virtual void EnsureVisible(IMauiElement element)
```

- The `timeout` parameter goes (no backward compatibility: change it outright).
- The four `Run*WithElement` helpers call it inside their existing `RunPoll`, so a not-yet-visible
  element costs one poll interval, and the next attempt finds the element again.
- **Scroll throttling.** Scrolling on every poll interval could fight a list that is still
  moving, and on Android a scroll is a UiScrollable search. Scroll at most once per element
  instance, and at most every `Animation` ms (300 by default) for the same locator; otherwise only
  re-check visibility. The Core pieces this needs (`ScrollIntoViewCore`, `ScrollLookup`) exist.

### 3.2 Resolve per attempt, in all four helpers

`ResolveReadyElement`, `RunWaitWithElement` and `RunGetWithElement` already call `FindElement()`
per iteration; with 3.1 they stop pinning. `RunAssertWithElement` keeps "resolve once, re-read
each tick" (it avoids a tree search per read, which is why it was written that way) but
re-resolves on `StaleElementException` **and** on `NotVisibleException`.

Measure before choosing for `RunAssertWithElement`: if a `FindElement` per poll costs little on
Windows (FlaUI caches) and Android (one Appium round trip), resolve every time and delete the
special case.

### 3.3 The stale signal

- `Brinell.Core.Exceptions.StaleElementException : BrinellException`, with the `Locator` when
  known.
- **FlaUI:** `FlaUIMauiElement` wraps its property reads and actions; `UIA_E_ELEMENTNOTAVAILABLE`
  (FlaUI's exception or a `COMException` with that HRESULT) becomes `StaleElementException`. One
  helper, used everywhere the element touches UIA, not a catch per member.
- **Appium:** `AppiumMauiElement` maps Selenium's `StaleElementReferenceException` the same way.
- The 11 catches switch to `StaleElementException`. `Brinell.Maui` no longer names a Selenium
  type.

### 3.4 Actions that confirm their effect

The 8 act-then-`Until` Core methods keep reading "their own element" (R1). What changes is what
happens when that element is replaced after the action:

- `Until` treats `StaleElementException` as "cannot confirm on this instance". The Core method
  throws it; the generated action's caller does **not** repeat the action. The failure says the
  action ran and the element was replaced before its effect could be read.
- Where a control's element is known to be replaced by its own action (none measured yet besides
  toolbar items, which confirm nothing), give that control a documented re-read through a fresh
  lookup. Not a general mechanism: add it when a control needs it, with the evidence.

### 3.5 Container roots

Verify first: after the element is removed, does `IsCachedRootValid` (a `TagName` read) fail on
FlaUI, or does FlaUI return a cached value? If it passes for a dead root, containers keep
resolving children under a removed root until the poll ends. Fix only if measured: read a property
FlaUI does not cache (`IsOffscreen`, or `GetRuntimeId`) and let `StaleElementException` invalidate.

### 3.6 `ToolbarButton`

Keep `RequiresVisibilityForAction => false`: it is right on its own terms (the click raises the
item by id, so the instance on screen does not matter). After 3.1-3.3 it is no longer what keeps
the Todo suite green; phase 5 proves that by running with it removed.

---

## 4. Risks

| Risk | Mitigation |
| --- | --- |
| A control relied on the nested 5 s to wait out a slow scroll or animation | The outer budget is still 5 s by default; only the nesting goes. Phase 5's full-suite comparison against `timing-baseline.json` shows any test that got slower or started timing out. |
| Scroll storms (scrolling every poll) | Throttle as in 3.1. Unit-test: at most one scroll per element instance per `Animation` ms. |
| `FindElement` per poll is expensive on Android | Measure in phase 0; if needed, keep resolve-once in `RunAssertWithElement` and re-resolve only on the two signals. |
| Mapping too much to "stale" on FlaUI (not every COM error is a removed element) | Map exactly `UIA_E_ELEMENTNOTAVAILABLE`; everything else keeps its type. |
| Callers that caught `StaleElementReferenceException` outside `srcnew/` (tests, Construction) | Grep `testsnew/`, `samples/`, and `construction/Exact.Construction.UITests` in phase 2 and change them; no deprecation shim (no-backward-compatibility rule). |
| Behaviour differs Windows vs Android | Android runs its known baseline subset before and after (6/16 Range, 0/8 Picker fail before any change); compare, do not expect green. |

---

## 5. Phases

One UI test process at a time; the smallest tier that can falsify each change; the full Windows
suite at the end only.

| Phase | Deliverable | Done when |
| --- | --- | --- |
| **0. Pin it down** | Unit tests in `Semantic/ReadinessTests.cs` that fail today: (a) first lookup returns an element that never becomes visible, second returns a good one -> `Click()` succeeds and invokes the good one once; (b) `Click(timeoutMs: 300)` against a never-visible element fails in < 1 s; (c) one public call writes one log entry/exit pair; (d) same as (a) for `Get*`, `Wait*`, `Assert*`. Measure `FindElement` cost per call on Windows (and Android if an emulator is up). Record the current full-suite timing. | The tests exist and fail for the stated reason; numbers recorded here. |
| **1. Stale signal** | `StaleElementException` in Core; FlaUI and Appium mapping; the 11 catches switched. Unit tests for the mapping at the element level (FlaUI: a mocked `AutomationElement` failing with `0x80040201`; Appium: Selenium's exception). | `Brinell.Maui.Tests` green; `grep StaleElementReferenceException srcnew/Brinell.Maui` finds nothing. |
| **2. Single budget** | `EnsureVisible` as one attempt with scroll throttling (3.1); the four helpers (3.2); `ToggleControlBase`/`RadioButton` Core calls updated; callers outside `srcnew/` updated. | Phase 0 tests pass; the rest of `Brinell.Maui.Tests` green. |
| **3. Confirming actions** | `Until` and the 8 act-then-confirm Core methods handle `StaleElementException` as in 3.4; unit tests in `UntilTests.cs`. | A replaced element fails with the new message and the action is not repeated (verified by `Verify(..., Times.Once)`). |
| **4. Container roots** | Measure 3.5 on FlaUI (a short probe against the sample app's StateContainer, which swaps its children). Fix only if the cache survives a removed root. | Measured result recorded; fix + unit test if needed. |
| **5. Prove it** | Remove `ToolbarButton`'s override on a branch and run the Todo UI suite 5x (it failed 1 in 2 before); run `Brinell.Maui.UITests` on Windows in full (baseline: Range 23/23 green) and compare with `timing-baseline.json`; run the Android baseline subset if an emulator is up. Put the override back (3.6). | Todo 5/5 without the override; Windows suite no new failures and no test slower by more than the report's threshold; Android no worse than its baseline. |
| **6. Write it down** | AD-004 gains the rule "one budget per public call; nothing inside a call waits with its own default"; the `maui-control` skill's R2 says the same; `CHANGELOG.md` names the removed `EnsureVisible(element, timeout)` overload and the exception type change. | Docs merged; links valid. |

---

## 6. Open decisions

| # | Question | Recommendation |
| --- | --- | --- |
| S1 | Re-resolve on every poll everywhere, or keep resolve-once in `RunAssertWithElement`? | Decide on phase 0's measurement; default to resolve-once plus the two re-resolve signals. |
| S2 | Name and home of the stale type | `Brinell.Core.Exceptions.StaleElementException`, next to `ElementNotFoundException`. |
| S3 | Scroll throttle interval | `Timeouts.Animation` (300 ms), not a new setting. |
| S4 | Keep `ToolbarButton.RequiresVisibilityForAction` after the fix? | Keep: it is correct in its own right (3.6). |
| S5 | Also fix the misleading message when the page gate fails first? | Out of scope unless phase 0 shows it on the same path. |

## 7. Out of scope

- Physical-input fallbacks (AD-005): none are added.
- WPF / WinForms controls: they have their own base classes; check them for the same nesting
  afterwards, as a separate item.
- The Construction UI tests beyond changing their `StaleElementReferenceException` catches, if
  any.
