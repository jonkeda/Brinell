---
title: RCA — Page readiness gate blocks the hub's back button
description: Why every MAUI UI test after the first fails with MissingRoot, and the smallest correct fix
status: rca
---

# RCA — Page readiness gate blocks the hub's back button

## Symptom

Every test in `Brinell.Maui.UITests` after the first one in a run fails with:

```
Brinell.Core.Exceptions.PageLoadException :
Page 'PageHub' did not become ready for WaitExists on control 'AccessibilityId:BackToHub'
within 10000 ms. Last readiness state: MissingRoot; busy value: '(none)'.
```

The first test of a run passes. Run any single test alone and it passes. Run two and the
second fails.

## Impact

The whole Windows MAUI UI suite is effectively one test deep. `AutomationProbeTests` reports
2 of 3 failing; the third passes only because it happens to run first.

This is **not** an AutomationId rendering problem, which was the first hypothesis and a
reasonable one for this codebase. The probe that does pass reports every standard layout as
addressable:

```
| AutomationContainer | yes |  | Grid | yes |  | VerticalStackLayout | yes |
| StackLayout | yes |          | FlexLayout | yes |  | Border | yes | ContentView | yes |
Not addressable (3): Frame, SwipeView, RefreshView
```

Those three are the long-documented exceptions. `AutomationId="PageHub"` is present on
`HubPage.xaml:20`, and `MauiProgram.cs:33` registers `AddBrinellAutomationHandlers()`. The
AutomationIds render fine.

## Sequence

1. `MauiFixture.Open(page)` calls `ReturnToHub()` before opening anything.
2. `ReturnToHub()` calls `HubPage.TryGoBack()`.
3. `TryGoBack()` calls `BackToHub.WaitExists(true, timeoutMs)`.
4. `BackToHub` is a `Button<HubPage>` — a control **scoped to `HubPage`**.
5. `ViewBase.RunPoll` gates on `Page.WaitReady(timeout)` before doing anything.
6. `HubPage` is not loaded — a sample page is open, so the `PageHub` root is not in the tree.
   `ProbeReadiness()` returns `MissingRoot`.
7. `RunPoll` throws `PageLoadException`.

The first test escapes because the app starts on the hub, so there is nothing to go back from.

## Root cause

**`BackToHub` is scoped to a page object that, by definition, is not loaded when the control
is needed.**

`HubPage` says so itself, in a comment written before the gate existed:

> This page object resolves one element that is deliberately *not* on the hub:
> `BackToHub`, which lives on whichever page is currently open. Gating lookups on the hub
> being loaded would make `TryGoBack` unable to find the only control that gets back to it —
> **the hub reports not-loaded precisely when the back button is needed.**

That comment is an accurate prediction of this defect. It describes the model as a known
tension rather than a problem to fix, and the tension held only while nothing enforced it.

## What changed

`3784e70 Revisited busy page checking` added the enforcement to `ViewBase.RunPoll`:

```diff
-        while (stopwatch.ElapsedMilliseconds < (timeoutMs ?? DefaultTimeoutMs))
+        if (Page != null && !Page.WaitReady(timeout))
+        {
+            var snapshot = Page.ProbeReadiness();
+            throw new PageLoadException(...);
+        }
+
+        do
         {
-                if (condition())
+                if ((Page == null || Page.IsReady()) && condition())
```

`929c7a0 readiness fixes` only improved the message and added the same block to `Run`. It did
not cause this — an earlier statement of mine said it did, and that was wrong.

## Two gates, not one

Worth separating, because it decides which fixes actually work:

| | Gate 1 — resolution | Gate 2 — readiness |
|---|---|---|
| Where | `PageObjectBase.CanResolveElements` → `EnsureLoaded` | `ViewBase.RunPoll` / `Run` |
| Throws | `ElementNotFoundException` "is not loaded" | `PageLoadException` "did not become ready" |
| Added | pre-existing | `3784e70` |
| Applies to | **required** resolution (`FindElement`) | every polled operation |
| Bypassed by | optional resolution (`TryFindElement`) | nothing |

`WaitExists` uses optional resolution, so gate 1 never fired for it — which is why this worked
before `3784e70`. Gate 2 has no such bypass.

**This matters for the fix:** `BackToHub.Click()` resolves through `RunDoWithElement` →
`FindElement()`, which is *required* resolution. So even if gate 2 were relaxed, `TryGoBack`
would get past `WaitExists` and then fail at `Click()` via gate 1. **Relaxing gate 2 alone does
not fix this.** Both gates are correctly refusing to find a control outside its page.

## Why it was not caught

`ReadinessTests` covers gate 2 well — five tests for busy-waiting and one for a page that never
loads. But every one of them uses a page object whose controls are genuinely inside it. There is
no test for a control scoped to a page it does not belong to, because that arrangement was never
intended to exist; it exists only in `HubPage`, in the test project, where unit tests do not
reach.

The `Brinell.Maui.Tests` suite is green. Only the UI suite sees this.

## Options

### Option A — Fix the model: scope `BackToHub` where it actually lives

`BackToHub` belongs to whichever page is open, so resolve it from the app root instead of from
`HubPage`. In `MauiFixture.ReturnToHub`:

```csharp
// BackToHub belongs to whichever page is open, not to the hub. Resolving it through HubPage
// asks a page object for a control that is by definition not in it, and both readiness gates
// correctly refuse. The app root is its real scope.
var back = Context.TryFindElement(Locator.ByAccessibilityId("BackToHub"));
```

`HubPage.BackToHub` and `TryGoBack` then go away, and the apologetic comment with them.

- **Fixes it fully** — clears both gates, because there is no page scope to be unloaded.
- **Smallest blast radius** — one fixture method, in the test project. No framework change, so
  no risk to the busy-signal behaviour `ReadinessTests` pins down.
- **Cost:** a raw `TryFindElement` in the fixture, which `AGENTS.md` discourages in *tests*.
  This is fixture plumbing rather than a test body, and the alternative is inventing a page
  object for a page that has no identity.

### Option B — Fix the gate: separate "not here" from "busy"

Gate 2 currently blocks on any non-`Ready` state, including `MissingRoot`. But gate 1 already
enforces "controls resolve within their page" for required resolution, so gate 2's root check is
a duplicate — and a worse one, because it also fires on the optional path that was deliberately
built to tolerate absence.

```csharp
if (Page != null && !Page.WaitReady(timeout))
{
    var snapshot = Page.ProbeReadiness();
    // Wait out a page that is working. Do not block on a page that is simply not here —
    // gate 1 already owns that, and the optional-resolution path is built to tolerate it.
    if (snapshot.State is not (PageReadinessState.MissingRoot or PageReadinessState.StaleRoot))
        throw new PageLoadException(...);
}
```

- **Does not fix this defect on its own** (gate 1 still blocks `Click`).
- **Worth doing anyway**: it removes a duplicated check and stops two different exception types
  describing the same condition. It also fixes the general class — toolbar items, flyouts and
  dialog chrome all live outside their page root.
- **Risk:** touches the framework path all five `ReadinessTests` busy cases run through. Those
  test `Busy`, not `MissingRoot`, so their intent survives — but they must be re-run.

### Option C — Make `HubPage.IsLoaded` always true

Rejected. It would silence the gate by lying about the page, and every genuine "wrong page"
diagnostic for the hub would be lost.

## Recommendation

**Take Option A now. Consider Option B separately, on its own merits.**

A is the actual fix: it is small, it is confined to the test project, it needs no framework
change, and it resolves the contradiction rather than working around it. The control was never
in the hub; scoping it there was the defect, and the gate only made an existing lie audible.

B is a real cleanup — two gates reporting the same condition with different exception types is
worth removing, and it would prevent the next control that legitimately lives outside its page
from hitting this. But it is not needed to get the suite running, it touches shared framework
code that other tests depend on, and bundling it with A would make a green suite prove two
things at once. Keep them separable.

## Outcome — Option A applied

`MauiFixture.FindBackToHub` now resolves the button from the app root via
`Context.TryFindElement`, polling for the short timeout because a page caught mid-transition has
not attached its toolbar yet. `HubPage.BackToHub` and `HubPage.TryGoBack` are gone, and so is the
comment that apologised for them.

| Check | Before | After |
|---|---|---|
| `AutomationProbeTests` | 1 of 3, 20 s | **3 of 3, 1 s** |
| `Brinell.Maui.Tests` | 119 passed | **120 passed** (new guard) |
| Full MAUI UI suite | first test only | **183 passed, 24 failed** |

The 20 s → 1 s drop is the ten-second readiness timeouts no longer being spent.

The 24 remaining failures are all pre-existing and unrelated:

- **11 × `Range.StepperTests`** — Stepper is on the documented baseline list.
- **13 × `Tests.Shell.*`** — a different fixture driving `Brinell.Samples.Maui.ShellApp`. Baselined
  by stashing this change: **13 failed, 0 passed, identically**, and in one second, so that app is
  not starting. Its own defect, worth its own investigation.

Option B was not applied. It remains worth doing on its own merits, and
`ControlScopedToAnAbsentPage_RefusesAndNamesTheMissingRoot` in `ReadinessTests` now pins the
current behaviour so that relaxing the gate has to be a deliberate, visible decision.

## Verification

1. `dotnet test testsnew\Brinell.Maui.UITests --filter "FullyQualifiedName~AutomationProbeTests"` —
   expect 3 of 3, up from 1 of 3.
2. `dotnet test testsnew\Brinell.Maui.Tests` — must stay green; this is what proves A did not
   touch readiness behaviour.
3. Full MAUI UI suite, against the known baseline (DatePicker, TimePicker, Image, ProgressBar,
   Stepper, Switch fail before any of this). Expect the count to drop sharply — most current
   failures are this one cause.
4. Add a unit test for the arrangement that was never covered: a control scoped to a page whose
   root is absent, asserting the intended behaviour rather than leaving it to a comment.

## Note on scope

Found while verifying step 1 of the UIA bridge work
([../extension/steps.md](../extension/steps.md)); confirmed pre-existing by reverting the step 1
driver change to `aadfbbd` and observing the identical two failures.
