# Plan: Android performance

## Where the time is

Baseline for this plan — Scroll + Buttons + Toggle, 34 tests, **3 m 36 s** wall clock, of which
Appium accounts for ~182 s:

| | calls | total | avg |
|---|---|---|---|
| `/elements` (plain) | 735 | 76.4 s | 104 ms |
| `/elements` (UiScrollable sweep) | 30 | 69.0 s | 2299 ms |
| `/element/<id>/attribute/checked` | 87 | 12.3 s | 142 ms |
| `/element/<id>/click` | 108 | 9.6 s | 89 ms |
| `/element/<id>/displayed` | 353 | 4.9 s | 14 ms |
| `/element/<id>/rect` | 314 | 3.9 s | 13 ms |

Two targets dominate, and **neither is latency**: 735 lookups for 34 tests (21 per test) is
polling volume, and 30 sweeps at 2.3 s each is work we may not need to do at all. The ~34 s
outside Appium is test-host and fixture overhead.

Rule for this plan: **every option is measured against the same 34 tests, and kept only if the
measurement justifies it.** An option that does not pay is reverted and recorded, not left in
"because it should help".

---

## A. Cut lookup volume (targets the 76 s)

- [x] **A1. Reuse the resolved element across poll iterations.** Applied to
      `RunAssertWithElement`: resolve once, re-read the value each tick, drop back to
      re-resolving on `StaleElementReferenceException`. Lookups 811 -> 648 (-20%), total calls
      2167 -> 1773. Wall clock 3 m 28 s -> 3 m 26 s — volume fell, time did not, because the cost
      is per-call latency rather than call count. **Kept** (less traffic is still less to go
      wrong), but it is not the lever. `RunAssertWithElement` and
      friends re-run `FindElement` on every 100 ms tick. The element rarely changes identity;
      re-reading the *value* is what the poll is for. Re-resolve only when the element goes stale.
- [ ] **A2. Stop re-resolving between resolve and act.** `RunDoWithElement` resolves a ready
      element, then the core operation often looks things up again (`EnsureVisible`, `rect`,
      `displayed`). Pass the resolved element through.
- [ ] **A3. Collapse `EnsureVisible`'s double read.** It calls `IsVisibleCore`, then
      `WaitVisibleCore` which polls `IsVisibleCore` again — two lookups where one would do when
      the element is already visible.
- [ ] **A4. Adaptive poll interval.** Fixed 100 ms ticks mean a 3 s wait costs 30 lookups. Start
      tight and back off — most conditions resolve on the first or second tick.
- [ ] **A5. Prefer `findElement` over `findElements`.** `MauiTestContext` uses the plural form and
      takes `[0]`. The singular is cheaper on the server for the common single-match case.

## B. Avoid or bound the sweeps (targets the 69 s)

- [x] **B1. Do not sweep twice inside one poll.** `ScrollingOnceResolver` sweeps on the first
      tick, plain lookups thereafter. Sweeps 35 -> 25, sweep time 86.3 s -> 51.5 s, Appium total
      198.5 s -> 167.1 s, wall clock 3 m 36 s -> 3 m 28 s. **Kept — the best result so far.** A poll that resolves through
      `TryFindElementAfterScroll` can sweep on every tick. Sweep once, then plain lookups until
      the poll ends.
- [x] **B2. Bound the sweep with `setMaxSearchSwipes`.** Tried at 10 swipes: sweeps got
      *slower*, 2662 ms -> 2924 ms average. A cap cannot help, because the expensive sweeps are
      the ones that **succeed** — a failed lookup already costs 2 ms. **Reverted.**
- [ ] **B3. Never sweep for a scope that is not ready.** Already applied to `HubPage.IsLoaded` and
      worth enforcing generally: a not-yet-loaded scope answers "absent" for reasons scrolling
      cannot fix.
- [ ] **B4. Remember a sweep's outcome for the duration of an operation.** If a sweep just found
      the element, the next lookup in the same operation should not sweep again.
- [x] **B5. `mobile: scrollGesture` with the container instead of `UiScrollable`.** Built it:
      find the scrollable container, step it a screenful at a time from where it is, re-query
      after each step, search down then up. The mechanism worked — lookup time fell from
      **147.4 s to 89.4 s**, exactly as predicted, because no lookup rewinds the page any more.
      But the gestures themselves cost **48.9 s across 32 calls**, so Appium total moved 194.4 s
      -> 188.7 s: a 6 s gain on a run that varies by 13%. Two scroll tests regressed.
      **Reverted.**

      The conclusion is worth more than the change: **the scrolling is the cost, not the
      strategy.** Moving a long page on an emulator takes what it takes, whoever asks for it. The
      only lever left on sweeps is needing fewer of them — which is B1 (done), B3 (done for the
      hub) and B4.

## C. Driver and server settings (cheap, one line each)

- [x] **C1. `waitForIdleTimeout = 100`.** Done. Tail collapsed (p95 4938 ms → 816 ms, max
      11390 ms → 4921 ms, click avg 313 ms → 89 ms) but total run unchanged, 3 m 42 s → 3 m 36 s.
      **Kept** for predictability, not for speed.
- [x] **C2. `ignoreUnimportantViews = true`.** Measured: **slower** — 3 m 55 s against 3 m 36 s,
      and a scroll test failed that had been passing, which fits a compressed hierarchy hiding
      nodes. **Reverted.**
- [ ] **C3. `disableIdLocatorAutocompletion = true`.** Stops the server expanding bare ids into
      package-qualified ones. We already pass fully-qualified resource ids.
- [ ] **C4. `snapshotMaxDepth`.** Caps how deep a hierarchy snapshot goes. Our pages are shallow;
      a lower cap may cut per-lookup cost.
- [ ] **C5. `enableNotificationListener = false`.** Removes a listener we never use.
- [ ] **C6. `skipDeviceInitialization` / `skipServerInstallation`.** Session-start cost only —
      once per fixture, so it shortens the run but not the per-test cost.

## D. Locator strategy

- [x] **D1. Stop sending a *web* locator strategy.** Tried: sent
      `MobileBy.AndroidUIAutomator("new UiSelector().resourceIdMatches(...)")` directly instead of
      `By.Id`. Plain lookup latency 140 ms -> 131 ms, a 6% move inside a 13% noise floor; Appium
      total 194.4 s -> 190.6 s; wall clock slightly worse. **Reverted** — no measurable gain, and
      a `resourceIdMatches` regex is a weaker match than an exact id. The wire-level observation
      below stands and was worth confirming, but the translation is evidently cheap.
      Original note: Confirmed on the wire: Selenium 4 turns
      `By.Id` into `{"using":"css selector","value":"#CheckBoxStatusLabel"}`, which the driver
      then translates into `new UiSelector().resourceId(...)`. A plain id lookup costs **116 ms**,
      and there are 648 of them — 75 s, the largest remaining item. Worth trying an Appium-native
      id strategy so the server does its own lookup instead of translating a CSS selector.
      **Next up.**
- [ ] **D2. Drop the content-desc fallback when a resource-id matched.** The sweep tries both
      matchers in sequence; the second is wasted whenever the first works.

## E. Test and fixture structure

- [ ] **E1. Skip the hub round trip when the page is already open.** Every test constructor calls
      `Open(page)`, which returns to the hub and clicks through again — 34 round trips for 34
      tests. The risk is tests inheriting each other's state, which the hub design exists to
      prevent, so any change here must keep a real reset.
- [ ] **E2. Reduce fixed pauses.** `WaitHelper.Pause` appears in the scroll loops (150 ms) and
      elsewhere; `TimeoutSettings.Animation` is a flat 300 ms. Fixed sleeps are the thing this
      framework set out to remove from tests, and they are still here in its own internals.
- [ ] **E3. Lower `ElementFind` from 3000 ms.** Absence costs the full timeout. Lower it and let
      the readiness ladder do the waiting where waiting is meaningful.

## F. Measurement hygiene

- [x] **F1. Force reinstall (`enforceAppInstall = true`).** Measured at 3 m 36 s, identical to
      without it: the extra install is paid once per fixture and does not show. **Kept** — it
      removes a hazard that already caused one full misdiagnosis, for no measurable cost.
- [x] **F2. Repeat each measurement.** Done, and it changed the conclusions. The same code
      measured twice on the same emulator gave **4 m 17 s and 3 m 43 s — 13% apart**. Every
      single-run delta earlier in this plan (3 m 36 s -> 3 m 28 s -> 3 m 26 s) is inside that
      band and cannot be called an improvement. Only two results survive: the hub fix (10 m 34 s
      -> 3 m 42 s) and B1, which was measured on Appium's own counters (sweep time 86.3 s ->
      51.5 s) rather than on the clock.

      **Rule from here: measure per-call Appium counters, not wall clock, unless the effect is
      larger than 30 s.**

---

## Hardware

A dedicated AVD, `Brinell_Perf` — 8 GB RAM, 6 cores, 12 GB data partition (the previous
`Medium_Phone` had 983 MB free, and `Bouw7_Phone` was 96% full and could not install the APK).

It is **not faster**: 3 m 43 s warm against 3 m 26 s on the old device, i.e. the same within
noise. It is **more reliable**: 33/34 on both runs, against 31/34 on the small one. Worth keeping
for that alone — the failures it removed were the flaky `Reset` family, not real regressions.

## Results log

| Option | Result | Run time | Verdict |
|---|---|---|---|
| (baseline after hub fix) | — | 3 m 42 s | — |
| C1 `waitForIdleTimeout` | tail collapsed, total unchanged | 3 m 36 s | kept, for predictability |
| F1 `enforceAppInstall` | no measurable cost, removes stale-build hazard | 3 m 36 s | **kept** |
| C2 `ignoreUnimportantViews` | slower, and broke a passing scroll test | 3 m 55 s | **reverted** |
| B1 sweep once per poll | sweeps 35→25, sweep time 86.3 s→51.5 s | 3 m 28 s | **kept** |
| A1 resolve once per assertion | lookups 811→648, time flat | 3 m 26 s | kept, but not the lever |
| *(new emulator: 8 GB, 6 cores)* | more reliable, not faster | 4 m 17 s / 3 m 43 s | kept |
| F2 repeat measurement | **same code, 13% apart** | 4 m 17 s vs 3 m 43 s | invalidates small deltas |
| D1 native selector | plain lookup 140→131 ms (inside noise) | 3 m 52 s | **reverted** |
| B2 `setMaxSearchSwipes` | sweeps *slower*, 2662→2924 ms | 3 m 57 s | **reverted** |
| B5 stepwise `scrollGesture` | lookups 147→89 s, gestures +49 s, net 6 s | 3 m 48 s | **reverted** |
| (after reverts, confirmation) | 32/34, clean tree | 3 m 39 s | — |

### What the sweep experiments settled

`UiScrollable.scrollIntoView` rewinds the container to the top before searching, so a sweep costs
the same wherever the element is — a sweep for the *first* control on a page measured 3.6 s.
Replacing it with stepwise gestures removed that rewind and the lookup time duly collapsed, but
the gestures cost what the rewind had. Two different strategies, the same total: the emulator
spends the time physically moving a long page either way.

So sweeps are not made cheaper, only rarer. B1 (sweep once per poll) is the only change here that
has actually paid, and it paid by removing ten sweeps rather than by speeding any up.

### Where the remaining 168 s of Appium time sits

| | calls | total | avg |
|---|---|---|---|
| `/elements` (plain) | 648 | 74.9 s | **116 ms** |
| `/elements` (UiScrollable) | 23 | 49.7 s | 2163 ms |
| `/element/<id>/attribute/checked` | 87 | 13.0 s | 150 ms |
| `/element/<id>/click` | 108 | 12.4 s | 115 ms |

Two conclusions from four measured options: cutting *volume* barely moves the clock (A1), while
cutting *expensive* calls does (B1). The 116 ms plain lookup is now the thing to attack — hence
D1 — and after that the 23 remaining sweeps.
