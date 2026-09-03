# Performance: why `IsExists` was slow, and what to do next

## Method

Measured, not guessed. The Appium server logs every HTTP call with its duration, so the whole run
can be attributed to endpoints and to the selectors behind them. Numbers below come from parsing
that log for one Android run of Scroll + Buttons + Toggle (34 tests).

## It was never "everything is slow"

The typical lookup is fast. The distribution of successful `POST /elements` calls, before the fix:

| bucket | calls | time |
|---|---|---|
| < 100 ms | 618 | 19.4 s |
| 100–500 ms | 85 | 27.7 s |
| 500 ms – 2 s | 24 | 15.7 s |
| **2–5 s** | **39** | **105.3 s** |
| **> 5 s** | **39** | **370.5 s** |

p50 = 34 ms, p90 = 933 ms, p95 = 4.9 s, max = 11.4 s. **78 calls — under 10% — accounted for 88%
of all time.** This is a tail problem, and a tail with one dominant cause.

Note also what was *not* the problem: 4619 lookups returned 404 (element not found) and cost
**2 ms each, 7.2 s in total**. Failing to find something is cheap. Succeeding slowly is what hurt.

## The dominant cause: a readiness check that scrolled

Attributing the slow calls to their selectors:

| slow (>= 2 s) lookups | calls | total | avg |
|---|---|---|---|
| `UiScrollable -> PageHubTitle` | 66 | **429.1 s** | 6502 ms |
| `UiScrollable -> ScrollBottomButton` | 3 | 15.1 s | 5045 ms |
| `UiScrollable -> ScrollStatusLabel` | 3 | 10.9 s | 3621 ms |
| `UiScrollable -> ScrollBottomLabel` | 2 | 10.6 s | 5280 ms |
| others | 4 | 10.2 s | ~2500 ms |

One call site — `PageHubTitle` — was **71% of the entire run**.

`HubPage.IsLoaded()` was `Title.IsExists()`, and `MauiFixture.ReturnToHub` polls it before every
test. Once `IsExists` learned to scroll, every one of those checks became a full `UiScrollable`
sweep of whichever page was open, hunting for a hub control that is not on that page — scrolling
to the bottom, finding nothing, 6.5 seconds at a time.

### The rule this exposes

**A readiness check must never scroll.** If a page's marker is not present now, you are on a
different page, and no amount of scrolling will produce it. Scrolling can only convert "not on
screen" into "on screen"; it cannot convert "not on this page" into "here".

`PageObjectBase.IsLoaded` already obeys this — it checks the page root with a plain
`FindElements`. The hub had overridden it. The override is gone; the base's strict check works
because the hub's root carries `AutomationId="PageHub"`.

## Result

| | Before | After |
|---|---|---|
| Scroll + Buttons + Toggle (34 tests) | 10 m 34 s | **3 m 42 s** |

**2.9× faster from deleting one override.** Passing went from 32/34 to 31/34 — the difference is
`RadioButton_Reset_ClearsSelection`, which is in the family already known to move between runs,
not a new failure.

## What is left, measured

The same run after the fix — 2069 calls, 185 s of Appium time:

| endpoint | calls | total | avg |
|---|---|---|---|
| `/elements` (UiScrollable) | 24 | 63.8 s | 2659 ms |
| `/elements` (plain) | 759 | 58.4 s | 77 ms |
| `/element/<id>/click` | 108 | 33.8 s | 313 ms |
| `/element/<id>/attribute/checked` | 87 | 12.7 s | 147 ms |
| `/element/<id>/displayed` | 386 | 6.5 s | 17 ms |
| `/element/<id>/rect` | 348 | 4.7 s | 14 ms |

Two things stand out. Scroll-to-find still costs **2.7 s a call** — legitimate work, since it
really does scroll, but only 24 calls remain and they are a third of the time. And plain lookups
average 77 ms against a 34 ms median, so they carry a tail of their own.

## Improvements, ranked by expected value

### 1. `waitForIdleTimeout` — tried: fixes the tail, not the total

UiAutomator2 waits for the app to be idle before each command, **defaulting to 10 000 ms**. An
app that animates — which MAUI does — may never report idle, so commands pay the wait. The
observed shape fitted: fast median, long tail, maximum of 11.4 s against a 10 s default.

Set to 100 ms via `appium:settings[waitForIdleTimeout]` and re-measured. The mechanism was real —
the tail collapsed:

| | before | after |
|---|---|---|
| `/elements` p90 | 933 ms | **496 ms** |
| `/elements` p95 | 4938 ms | **816 ms** |
| `/elements` max | 11390 ms | **4921 ms** |
| `/element/<id>/click` avg | 313 ms | **89 ms** |

**But the run barely moved: 3 m 42 s to 3 m 36 s**, and total Appium time 185 s to 182 s — inside
run-to-run noise. Plain lookups even drifted the wrong way (77 ms to 104 ms average), which on its
own is probably emulator variance rather than a real regression.

So the hypothesis was right about the mechanism and wrong about the payoff. The extreme tail was
never where the bulk of the time was: 78 slow calls had already been dealt with by removing the
hub sweep, and what remains is *volume*, not latency.

**Kept anyway.** It makes the worst case 2.3× better and clicks 3.5× faster for one line of
configuration, which is worth having for predictability and timeout headroom even though it does
not shorten the run.

### 1b. What the numbers now say to attack instead

After both changes, Appium accounts for ~182 s of a 216 s run, and inside that:

| | calls | total | avg |
|---|---|---|---|
| `/elements` (plain) | 735 | 76.4 s | 104 ms |
| `/elements` (UiScrollable) | 30 | 69.0 s | 2299 ms |

Two targets, and neither is latency:

- **735 plain lookups for 34 tests — 21 per test.** That is polling: `RunPoll` re-finds the
  element every 100 ms. Fewer, smarter lookups is now the biggest single lever (see 4).
- **30 scroll sweeps costing 69 s.** Each is legitimate work, but 2.3 s each means avoiding one is
  worth more than making twenty lookups faster (see 3).

### 2. `ignoreUnimportantViews`

`appium:settings[ignoreUnimportantViews] = true` compresses the hierarchy Android hands over,
which makes every lookup cheaper. Documented as a speed-up; unmeasured here.

### 3. Bound the scroll sweep

`UiScrollable.setMaxSearchSwipes` caps how far a sweep will travel before giving up. Today a
sweep runs to the end of the container. Bounding it makes the worst case predictable, at the cost
of failing to find something very far down.

### 4. Do not repeat a sweep inside a poll

`RunPoll` re-evaluates its condition every 100 ms. When that condition resolves through
`TryFindElementAfterScroll`, each iteration can trigger another sweep. A negative answer
established once is unlikely to change within the same poll unless the UI changed — a poll could
scroll on its first iteration and use plain lookups thereafter.

### 5. Navigate less

Every test constructor calls `Open(page)`, which returns to the hub and clicks through again.
That is 34 hub round trips for 34 tests. Skipping the round trip when the requested page is
already open would remove most of them; the risk is tests inheriting state from each other, which
is exactly what the hub design was chosen to prevent — so it needs the reset to stay.

## Caveat on all these numbers

The emulator died twice during this session and the UiAutomator2 instrumentation process crashed
once mid-suite, producing 35 cascading failures. Every timing here comes from a run that
completed cleanly, but Android measurements on this setup need a repeat before being trusted, and
the stale-APK hazard (Appium not reinstalling a rebuilt app with an unchanged `versionCode`) can
silently invalidate a whole run.

## Sources

- [Appium settings API](https://appium.io/docs/en/2.1/guides/settings/) — `waitForIdleTimeout`, `ignoreUnimportantViews`, `actionAcknowledgmentTimeout`
- [Appium settings guide](https://github.com/appium/appium/blob/master/packages/appium/docs/en/guides/settings.md) — `appium:settings[...]` capability form


---

## Follow-up: `ReturnToHub` was costing 10–20 s per test on Windows

Reported as "returning to the hub waits a few seconds". Timing each phase of `MauiFixture.Open`:

```
[NAV] Toggle hubLoadedAtStart=False isLoaded=80ms  returnToHub=10058ms openClick=138ms
[NAV] Toggle hubLoadedAtStart=False isLoaded=16ms  returnToHub=20243ms openClick=63ms
```

`IsLoaded()` was **always false** — even at the start of a run, with the app sitting on the hub —
so `ReturnToHub` burned one or two 10 s timeouts on every navigation.

### Cause

Probing the tree directly:

```
[PROBE] PageHub=0  PageHubTitle=1
```

`AutomationId="PageHub"` sat on the `ContentPage`, which **is not a rendered view on Windows**, so
the marker never reached the UIA tree. Android exposed it and Windows did not — which is why
removing `HubPage.IsLoaded` in favour of the base check fixed Android (2.9×) and silently made
Windows far worse. Windows was not re-measured after that change; it should have been.

### Fix

Superseded by the real fix. The marker was first moved onto the hub's `ScrollView`, on the rule
"a page marker belongs on a rendered view". That treated the symptom: the reason a `ContentPage`
is not a rendered view on Windows is that `ContentPage` maps to a peerless `ContentPanel`, which
is exactly the gap `Brinell.Maui.AppSupport` already closes for `Layout`, `ContentView` and
`Border`.

Registering `AutomationPageHandler` for `ContentPage` makes a page findable by its own
`AutomationId`, so every page marker now sits on the `ContentPage` where it belongs and the
`ScrollView` markers are gone.

| | Before | After |
|---|---|---|
| `ReturnToHub` per navigation | 10 000–20 000 ms | **32–156 ms** |
| Windows Toggle + Scroll + Buttons (34 tests) | — | **34 / 34 in 15 s** |
| Windows Buttons+Text+Display+Toggle+Scroll (77) | — | **76 / 77 in 43 s** |

A second fix went in alongside: `ReturnToHub` now waits for the hub after clicking back instead of
re-testing `IsLoaded` mid-transition, which previously sent it round the loop to wait for a Back
button that had already gone.

### The lesson worth keeping

Both halves of this were caused by measuring one platform and assuming the other. The Android
fix was verified on Android only; the Windows cost it introduced went unnoticed for a day.
**A change to shared navigation or readiness needs a run on both platforms before it is called
done.**
