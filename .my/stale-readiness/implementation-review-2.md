# Stale readiness: second implementation review

Reviewed 2026-09-20, against [plan.md](plan.md) (status "done", steps 0-9 plus the review fixes)
and [design.md](design.md). The first review is [implementation-review.md](implementation-review.md);
this one is independent of it and re-checks its seven fixes as well.

The code reviewed is **`d511793`**, which is where this work now lives. The working tree holds
uncommitted changes that belong to the next project (`.my/android/`), and it **does not compile**
(see "Plan and document hygiene"), so everything below was built, read and run from a throwaway
worktree at `d511793`.

## Verdict

The seven findings of the first review are all fixed, and the fixes hold up: I re-read each one,
found the pins that hold them down, and the unit suite is **176 passed, 1 skipped, 0 failed** at
`d511793`, exactly as plan 2.1 reports.

Two new defects matter. Both are the *practical cost* of an open decision in design section 11
that was never revisited with the numbers step 2 measured:

- **S1** left `Assert*` holding one element for the whole call. An `Assert` on a replaced element
  now fails where the matching `Get` passes. This is the same class of bug as the first review's
  finding 1, one level up from rows, and the plan's own goal list ("it finds its element again on
  every attempt") says it should not happen.
- **D3** left every unknown exception retried. `NotSupportedException`, which this layer uses to
  mean "no route, ever", is therefore waited out for the whole budget and reported as a timeout.

Both are confirmed by throwaway tests (since deleted). Neither is caught by the pins.

## What was checked, and holds

| Claim | How checked | Result |
| --- | --- | --- |
| `Brinell.Maui.Tests` at `d511793` | `dotnet test` in a clean worktree | **176 passed, 1 skipped, 0 failed**, 6 s |
| `Brinell.Maui` builds | `dotnet build` in the same worktree | 0 errors |
| Core untouched (R9) | `git diff --stat 4455f79..d511793 -- srcnew/Brinell.Core` | empty |
| Step 1 grep: no Core element, scope, page, driver or `ControlObjectBase` type in `srcnew/Brinell.Maui*` | grep | 0 matches |
| Fix 1: a row's cached root must hold the row's item | read `ItemObjectBase.IsCachedRootValid` | `IsUsable(root) && Key.IsHeldBy(root)`; pins `Row_OwnRead_...` and `Row_OwnClick_...` are there |
| Fix 2: `SelectItem` resolves by polling, then activates once | read `CollectionObjectBase.SelectItem` | the activation is outside the poll; only `false` is asked again; every exception leaves the call |
| Fix 3: scrolls get the call's remaining budget | grep every `ScrollIntoView` call site in `Brinell.Maui` | all of them pass `CallRemainingMs` or `attempt.Deadline.RemainingMs`; `IMauiElement.ScrollIntoView(int)` has no default left |
| Fix 4: a retried unexpected exception names its type and count | read `ObservationLog.Unexpected`; probe test | `WaitTimeoutException`: "48 of 48 attempts raised NotSupportedException. Last: ...", with the exception inside |
| Fix 5: a bridge answer of "nothing" on an exited app is `AppUnavailableException` | read `FlaUIMauiDriver.Exchange`, `ReadState`, `WhileTheAppRuns`, `FlaUIDeclaredElement.DeclaresStateReads` | all four guard on `AppHasExited`; one route does not (finding 3) |
| Fix 6: the dead timeouts are gone | grep the `IsExists` / `IsVisible` / `GetItemCount` / `IsEmpty` / `TrySelectItem` signatures; grep `ObjectBase.Poll` | only the container's `GetAttribute(name, timeoutMs)` keeps one, as recorded; `ObjectBase.Poll` is gone |
| Fix 7: `NearMissSettings` and `IMauiElementScope.DescribeMiss` | read `NearMissSettings`, `ControlCall`, `ViewBase.NotFound`, `ContainerObjectBase.DescribeMiss` | both are there; the thresholds come from `IMauiTestContext.NearMiss`, and are pinned |
| Step 9 "Docs merged" | `git log`, `git status` | met: the work is in `45425f8` and `d511793`; the first review's open hygiene item is closed |
| Plan sections 3 and 4 | read | Q6, Q9 and X5 are recorded as settled; the Android baseline is now stated one way |
| No doc names a removed member | grep `docs/`, `.github/`, `CHANGELOG.md` | the only hits are the CHANGELOG's own "Removed" list |

Not rerun: the Windows, Todo and Android UI suites. The rule is one UI test process at a time,
each run takes minutes, and the working tree cannot be built to run them from. Their numbers in
plan section 4 are taken as reported.

## Findings

Most severe first.

### 1. `Assert*` holds one element for the whole call, so a replaced element fails the assert (confirmed)

**Where:** [ViewBase.tpl.cs:560-621](../../srcnew/Brinell.Maui/Controls/Base/ViewBase.tpl.cs#L560-L621),
`RunAssertWithElement`.

The poll resolves the element once into `held` and reuses it on every later attempt. It is
released only on `StaleElementException`. So:

- an element that is **replaced but still alive** - recycled, re-rendered in place, or answered by
  a new node while the old one still reads - is never looked for again, and the assert compares the
  old one until the budget runs out;
- `EnsureVisible` runs **only on the first lookup**. Once `held` is set, visibility is never
  checked again inside that call;
- an `ElementNotReadyException` raised by `getActual` after `held` is set does not release it
  either: it escapes to the poll as `Failed`, and the next attempt reads the same element.

Every `Get*` does the opposite. `RunGetWithElement` goes through `Attempt`, which calls `Locate`
and `EnsureVisible` on **every** attempt. So `GetText` and `AssertText` disagree about the same
control.

**Proof** (throwaway tests, since deleted; the page's lookup answers element 1 once and element 2
after that, both alive and visible):

| Call | Result |
| --- | --- |
| `Status.GetText(timeoutMs: 500)` | `"second"` - it found the replacement |
| `Status.AssertText("second", timeoutMs: 300)` | `AssertionException: Expected Text to be 'second'`, after the full budget |

This breaks plan section 1's goal - "it finds its element again on every attempt" - for every
`Assert*` member that reads through an element. It is the first review's finding 1 one level up:
there, a row answered for another item; here, a control answers from an element that is no longer
the one on screen.

**The decision behind it.** Design section 11, S1: *"Resolve once in `RunAssertWithElement`?
Resolve once; re-locate on `Stale` / `NotReady`. **Revisit with step 2's numbers.**"* It was never
revisited, and what shipped is weaker than S1's own default - it re-locates on `Stale` only. Step
2 then measured the saving S1 buys: **1.1 ms** for a child lookup under a known root (plan section
4). Against a 5 s default budget that is noise, and it is paid for with the one property the whole
design exists to provide.

**Fix:** resolve per attempt. Drop `held` and give `RunAssertWithElement` the shape
`RunGetWithElement` already has - `Attempt(a, ensureVisible: true, ...)` with the comparison in the
body. Record S1 as settled in design 11 and plan section 3. Add two pins: an assert against a
replaced element, and an assert whose element goes invisible mid-call.

### 2. A permanent `NotSupportedException` is waited out for the whole budget (confirmed)

**Where:** [Poller.cs:70](../../srcnew/Brinell.Maui/Calls/Poller.cs#L70), `IsFatal`.

`IsFatal` covers `AppUnavailableException` and a misconfigured `ScopeNotReadyException`. Everything
else is recorded as `Failed` and retried until the budget is spent.

`NotSupportedException` is not an "unknown" exception in this layer. It is the word the MAUI code
uses for **"there is no route for this, and there never will be"**:

- `FlaUIDeclaredElement.NotInTheTree(...)` - a bridge-only element asked for `Visible`, `Enabled`,
  `Rect` or `ScrollIntoView`;
- `ElementMatch.Matches` - a locator strategy that cannot be evaluated against an element in hand;
- `ScrollHelper.ScrollIntoView` and `CollectionObjectBase.TryActivate`, which both **catch** it and
  treat it as a permanent no.

**Proof** (throwaway test, since deleted): a `GetText` whose element throws
`NotSupportedException("no route for this")` on every read, with `timeoutMs: 800`:

> took 822 ms; threw `WaitTimeoutException`: `'AutomationId:Target' did not complete within 800 ms:
> 48 of 48 attempts raised NotSupportedException. Last: no route for this`

48 attempts at something that cannot change. On the default budget that is 5 s per call, and what
the test author reads is a timeout rather than "this control has no route for that".

The argument design 6 makes for a configuration error - *"No amount of waiting fixes that, so the
call fails at once (R0)"* - applies unchanged.

**The decision behind it.** Design section 11, D3: *"Should `Poller` retry unknown exceptions?
Yes, within the budget, and named in the final message."* That is sound for genuinely unknown
exceptions. It should carve out `NotSupportedException` (and `ArgumentException`, which is always a
test bug), or the layer should stop using `NotSupportedException` for "no route". Settle D3 either
way and record it.

**Fix:** add `or NotSupportedException` to `Poller.IsFatal`. The sites that rely on a retry are
unaffected: `TryActivate` and `ScrollHelper` already catch their own. Pin it: an unsupported read
fails at once, naming the route.

### 3. One bridge route still misses the "app has exited" guard

**Where:** [FlaUIDeclaredElement.cs](../../srcnew/Brinell.Maui.FlaUI/FlaUIDeclaredElement.cs),
`PerformGesture`.

The first review's fix 5 wrapped the driver's bridge verbs: `Exchange`, `ReadState`,
`WhileTheAppRuns` around `FlaUIMauiDriver.PerformGesture`, and `DeclaresStateReads`. A *declared
element's* own `PerformGesture` calls `GestureRunner.Perform` directly, with no
`WhileTheAppRuns`. On an app that has exited it therefore raises `GestureUnavailableException`,
which derives from `NotSupportedException`, not `AppUnavailableException`.

Inside an action that ends the call at once anyway, because `ActOnce` lets everything but
`ElementNotReadyException` through. Inside a poll it is retried for the whole budget - finding 2
again, and exactly the R0 gap fix 5 set out to close.

Code reading only; not reproduced against a real closed app. Route it through `WhileTheAppRuns`.

### 4. `FlaUIMauiDriver.SupportsStateReads` is dead code, and unguarded

**Where:** [FlaUIMauiDriver.cs:511](../../srcnew/Brinell.Maui.FlaUI/FlaUIMauiDriver.cs#L511).

Nothing calls it. `BridgeVerbRunner`'s own comment says why: *"It used to ask `SupportsStateReads`
and then read, walking the bridge twice."* Step 8 folded it into `ReadState`, and
`FlaUIDeclaredElement` now reads the declaration it already holds.

It is also the one bridge entry point without the `AppHasExited` check its neighbours grew in fix
5, so a future caller would reintroduce the gap. Under "no backward compatibility", remove it.

### 5. A row keyed by position still cannot tell it was recycled

**Where:** [ItemKey.cs:51-56](../../srcnew/Brinell.Maui/Containers/ItemKey.cs#L51-L56), `IsHeldBy`.

Fix 1 made `ItemObjectBase.IsCachedRootValid` require `Key.IsHeldBy(root)`. For
`ItemKeyKind.Position`, `IsHeldBy` returns `true` unconditionally - the code says so, and there is
nothing on the element to check. So for a collection whose platform publishes no `PositionInSet`
and whose rows carry no distinguishing automation id, fix 1 is a no-op and R0's "never act on
another row" does not hold.

This is not a defect in the fix; it is the limit of the fix, and it is currently stated only in a
code comment. A test author cannot tell from the outside which key kind their collection got.
Either say it in design 7.6 and AD-009 - "a collection whose rows publish neither a logical index
nor a stable id cannot be protected from recycling; give the item template an id" - or have `KeyOf`
log a warning once when it falls back to `Position`.

### 6. `PageObjectBase.ProbeContentReadiness` ignores the root it is given

**Where:** [PageObjectBase.cs:78-103](../../srcnew/Brinell.Maui/Pages/PageObjectBase.cs#L78-L103).

The probe receives `root` - which `ProbeOwnReadiness` has just resolved and validated - and then
calls `IsLoaded()`, which resolves the root **again** through `ContainerRoot`, re-running
`IsCachedRootValid` and a second `HasUsableBounds` read. Every readiness probe of every page
therefore costs two root resolutions where one would do, and readiness is the first step of every
attempt of every call.

It is also a correctness smell: `IsLoaded()` is `virtual`, so an override can answer about a
different element than the one the probe checked and then reads the busy signal from.

**Fix:** `if (!root.HasUsableBounds())`, or pass `root` to an `IsLoadedCore(root)`, and keep
`IsLoaded()` as the public one-shot member.

### 7. `timeoutMs` bounds the poll, not the call - and that is not written down

`RunDoWithElement(coreOperation, timeoutMs)` gives the poll `timeoutMs`, and the Core method then
calls `Confirm(..., timeoutMs)` with the **same** number, which starts a fresh `Deadline`. So
`Toggle(timeoutMs: 300)` may take 600 ms, and `SetChecked` and `CarouselView.Next` likewise. This
is R3 as designed ("its confirmation has a budget of its own"), and `ControlCall` says so where it
judges near-misses. It is nowhere in the test-facing docs: a test author reading `maui-ui-test` has
no way to know that the number they pass is half the wall time.

Related, and latent: `AttemptContext.Current` is still the poll's (now spent) context while a
`Confirm` runs, so `CallRemainingMs` read inside a Core method *after* it acted returns 0. Nothing
does that today. The Stepper and Slider work in flight (`.my/android/`) is exactly the kind of code
that would.

**Fix:** one line in `maui-ui-test` saying what `timeoutMs` bounds, and either give `Confirm` an
`AttemptContext` of its own or document that `CallRemainingMs` is meaningless after the action.

### 8. `AppRoot.WaitReady` is not a call unit and ignores its timeout

**Where:** [AppRoot.cs:288](../../srcnew/Brinell.Maui/Pages/AppRoot.cs#L288).

`public bool WaitReady(int? timeoutMs = null) => _context.WaitReady(timeoutMs);`, and
`MauiTestContext.WaitReady` is `=> !_disposed`. No log pair, no poll, and `timeoutMs` does
nothing - the same shape fix 6 removed everywhere else. Either drop the parameter, or let it poll
`ProbeReadiness` through `RunProbe` as `RootedScopeBase.WaitReady` does.

## Plan and document hygiene

- **The working tree does not compile.** `srcnew/Brinell.Maui/Controls/Range/Stepper.tpl.cs(240)`
  and `(262)`: `Deadline` and `ConfirmationResult` are not in scope - the file is missing
  `using Brinell.Maui.Calls;` and `using Brinell.Maui.Controls.Base;`. That is the uncommitted
  `.my/android/` work, not this project, but it means nothing can be built or run against the tree
  as it stands. Worth fixing before the next UI run either way.
- **The first review's hygiene items are closed.** Step 9's commit is in (`45425f8`, `d511793`);
  plan section 3 records Q6, Q9 and X5 as settled; the Android baseline is stated one way
  ("Range 6 passed / 16 failed, Picker 0 passed / 8 failed").
- **"Still open: design section 11 (S1, D3, X1, X2)" is accurate, and now has a price.** Findings 1
  and 2 are what S1 and D3 cost in practice. Settle both with this review's numbers rather than
  leaving them for the move down.
- **The step 8 evidence is still 5 runs.** The first review called it convincing but not
  conclusive, and nothing was added. Leaving it is fine, but the plan should say the claim rests on
  5 runs rather than reading as settled.
- **`MauiTestContext.Dispose` keeps a bare `catch { }`** around the driver dispose
  ([MauiTestContext.cs:239](../../srcnew/Brinell.Maui/Context/MauiTestContext.cs#L239)), which
  `Brinell/AGENTS.md` forbids. Pre-existing, and cleanup is the one place it is defensible - but it
  should catch something named, or the comment should say why it cannot.

## Suggested order

1. Fix finding 1 and settle S1. It is a small change to one method, the cost it was avoiding is
   1.1 ms, and the failure is the one the design exists to prevent.
2. Fix finding 2 and settle D3. One line in `Poller.IsFatal`, plus a pin.
3. Findings 3 and 4 together: both are bridge tidy-up left over from fix 5.
4. Findings 5-8: write down the position-key limit, use the root the page probe was given, say what
   `timeoutMs` bounds, and make `AppRoot.WaitReady` honest.
5. Fix the `Stepper.tpl.cs` usings so the tree builds again, then run the Windows suite once before
   starting the Android project in earnest.
