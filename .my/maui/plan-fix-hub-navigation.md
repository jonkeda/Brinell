# Plan: Fix Hub Navigation (Windows 16 + Android 6)

**Date:** 2026-08-30
**Parent:** [plan-sample-app-recovery-and-phase4.md](plan-sample-app-recovery-and-phase4.md)
**Scope:** `MauiFixture.Open` / `ReturnToHub`, `HubPage` (test-side), and whatever they expose
in the framework. Not the sample app — the app is verified working on both platforms.

---

## 1. The one hypothesis

Both failure sets say the same thing:

| | Failure |
|---|---|
| Windows, 16 tests | Container/Collection scoping, Toggle state, SearchBar, ActivityIndicator |
| Android, 6 tests | `Expected Exists to be 'True'. Locator: AutomationId:TestButton` |

And both sit on top of one fact, established by hand on both platforms:

> **The app is fine.** The Buttons page renders `ButtonsTestPage`, `TestButton`,
> `TestImageButton`, `ResetButton`, `StatusLabel`. A manual tap on `Open_Buttons` reaches it.
> The fixture's `Open(SamplePage.Buttons)` does not.

**Hypothesis: `Open` does not wait for the destination page.**

```csharp
public void Open(SamplePage page)
{
    ReturnToHub();
    _hub.OpenButton(page).Click();   // returns as soon as the click is dispatched
}
```

`PushAsync` is asynchronous and animated. The click returns immediately, the constructor
returns, and the first assertion runs against a page that is still arriving — or against the
hub, which is still on screen. On Windows this reads as flaky scoping and stale state; on
Android, where the push animation is slower, it reads as "the element is simply not there".

This also explains the shape of the Windows failures: they cluster in the classes with the
*most* setup work per test, which is where a race has the most room to land wrong.

**One earlier attempt failed and is informative.** Waiting for `_hub.WaitLoaded(false, …)`
made Windows *worse* (14 → 8 passing in the Display tier). That waited for the **hub to
disappear**, which the animation satisfies early. The wait must be on the **destination
arriving**, which is a different and stronger condition.

---

## 2. Steps

### Step 1 — Prove it, before changing anything

Do not fix on a hypothesis. Instrument first:

1. Add a temporary dump to `Open` after the click: log `_hub.IsLoaded()` and the result of a
   probe for the destination page root, immediately and again after 2 s.
2. Run one Android test and one Windows test.

**Expected if the hypothesis holds:** immediately after the click the destination is absent
and/or the hub is still loaded; two seconds later the destination is present.

**If that is not what it shows, stop and re-diagnose.** The rest of this plan is void.

### Step 2 — Make `Open` wait for the destination

`Open` must not return until the requested page is on screen. The fixture knows which page was
asked for, so it can wait for that page's own root:

```csharp
public void Open(SamplePage page)
{
    ReturnToHub();
    _hub.OpenButton(page).Click();
    WaitForPage(page);            // the missing half
}
```

`WaitForPage` needs the AutomationId of each page root. Those ids now match the page objects
exactly (the `*Tab` rename), so the mapping is mechanical — and putting it in the registry
keeps "adding a page is one line" true.

**Design constraint:** the wait must be on the destination being *present*, never on the hub
being *absent*. The failed attempt is the evidence for why.

### Step 3 — Re-verify both platforms, narrow first

1. Android `ButtonTests` — 6 tests, the case with the clearest signal.
2. Windows `Tests.Display` — the tier whose behaviour is already measured (4 failed / 34 s
   after the `RequiresLoadedPage` fix), so a change shows immediately.
3. Only then the full filtered Windows suite, expecting 137 / 1.

### Step 4 — Record and close

If the fix lands: update the parent plan's step 3 status, and phase 4's Android section with
the first genuinely passing mobile test class.

If Android still fails after Windows is green, that is the **real** platform divergence phase 6
has been waiting for — record it as a §4.5 tier candidate rather than patching it here.

---

---

## 2b. Outcome — hypothesis wrong, real cause found

**Step 1 killed the hypothesis, which is what it was for.** The instrumentation showed:

```
[NAV] immediately: hubLoaded=False dest(ButtonsTestPage)=False
[NAV] after 2s:    hubLoaded=False dest(ButtonsTestPage)=False
[NAV]   BackToHub = True     ← a page WAS pushed
[NAV]   PageHub = False, Open_Buttons = False, TestButton = False
```

Not a race: two seconds changed nothing. The hub was gone and a page *was* pushed
(`BackToHub` proves it), but nothing on that page could be found. Meanwhile a **manual** tap
reached a page where `ButtonsTestPage` and `TestButton` were both plainly present.

One thing was found and everything else was not — and the thing found was the one living in
`content-desc` rather than `resource-id`.

### The bug: the Appium driver resolved every locator as Windows

```csharp
public static By ToBy(this Locator locator)
    => locator.ToBy(MauiPlatform.Windows);   // hardcoded
```

`AppiumMauiDriver` called this platform-less overload from both `FindElement` and
`FindElements`, so on Android every `AutomationId` resolved as `AccessibilityId`
(content-desc) instead of `By.Id` (resource-id) — which is how MAUI actually surfaces it
there. A control was findable only if it happened to carry a content-desc.

**This is why no Android test had ever passed.** It sat underneath every earlier symptom, and
none of the phase-4 fixes could reach it.

**Fix:** pass `_platform` at both call sites and **delete the defaulting overload**, so the
mistake is unrepresentable rather than merely corrected.

### Second fix: the back button is chrome, not content

With locators correct, `BackToHub` stopped resolving — its Android node has an **empty
`resource-id`** and carries the value in `content-desc`. A `ToolbarItem` renders into native
chrome, where MAUI surfaces AutomationId as the accessibility label. The hub page object now
locates it with `Locator.ByAccessibilityId`, which is the same string on all three platforms.

### Result: 5 of 6 ButtonTests pass on Android

The first genuinely passing mobile tests in this project.

| | Before | After |
|---|---|---|
| Android `ButtonTests` | 0 / 6 | **5 / 6** |

**The remaining failure is a real finding, not a bug in this work.**
`Button_MultipleTaps_IncrementsCount` chains two clicks fluently and expects the count to
reach 1 then 2. It fails on the *first* assertion — yet `Button_Tap_ExecutesCommand`
(one click) **passes**, and a manual tap produces `"✓ Button tapped 1 time."`. So a single
click works and the app is correct; two chained clicks do not both register.

That is a control-level platform difference — a §4.5 tier candidate for phase 6, and exactly
the kind of evidence phase 6 has been waiting for. **Do not patch it here.**

### Windows: not fixed by this, and slightly worse (16 → 23)

The plan predicted Windows and Android might share one cause. **They do not.** The locator bug
was Appium-only — `Brinell.Maui.FlaUI` maps `AccessibilityId` to AutomationId, so neither fix
changes Windows behaviour, and the `BackToHub` locator change is a no-op there.

Windows went 16 → 23 failures across this session's runs. The Display tier's failures are the
phase-7 parked set (Image, ProgressBar, ActivityIndicator), so the extra 7 are elsewhere and
**undiagnosed**. Given several of these classes passed in isolation earlier, run-to-run
variance is plausible but unproven — do not assume it.

**Next diagnostic, when resuming:** run the Windows suite twice without changing anything. If
the count moves, the instability is the finding and should get its own RCA rather than being
chased test by test. If it is stable at 23, bisect from the last known 16-failure state.

---

## 3. What this plan will not do

- **Not touch the sample app.** It is verified working on both platforms by hand; changing it
  now would reintroduce the variable the last attempt could not isolate.
- **Not add timeouts as a remedy.** RCA-002's lesson: a slow failure is fixed by failing at
  the point the answer is known, not by waiting longer. If a wait is needed it is because a
  condition is genuinely not yet true — not to paper over a race.
- **Not chase the 16 Windows failures individually** until step 1 says whether they share the
  Android cause. If they do, one fix closes both.

---

## 4. Risks

| Risk | Response |
|---|---|
| The instrumentation shows something else entirely | Then the hypothesis is wrong and this plan is void — that is the point of step 1 |
| Waiting for the destination masks a slower real bug | The wait asserts a condition that must be true anyway; if it times out, the failure names the page rather than an element |
| Windows and Android turn out to have different causes | Step 3 runs them separately and in that order, so they cannot be conflated |
| Emulator RAM regresses the run again | Start the AVD with `-memory 4096`; a default 2 GB AVD gets the app killed mid-startup |
