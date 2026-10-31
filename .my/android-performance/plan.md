# Android runs: where the time goes, and a plan to cut it

Date: 2026-10-31. Status: P1 and P2 built and measured (branch `kofflard/shown-page-and-list-search`, not committed); P3 to P8 open. Each step is measured on one test, not a suite.

## How it was measured

One test at a time, on the emulator `Brinell_Android` (Android 16, API 36, x86_64, 1080 × 2400, cold-booted), with Appium 3.1.2 and UiAutomator2. Three readings per test:
- the wall time of `dotnet test --no-build` for that one test;
- Bruinet's call log (`BRUINET_CALL_LOG`): every public call with its duration and nesting;
- Appium's server log (`--log-timestamp --log-no-colors --log-level info`): every HTTP request with its duration, grouped by command.

The scripts used are in the Kofflard repository's scratchpad of 2026-10-31 (`measure.sh`, `calllog.py`). The grouping is ten lines of Python over the `<-- VERB path status ms` lines.

## What was measured

| Test | Wall | Before the first call | In calls | Appium requests |
| --- | --- | --- | --- | --- |
| `ButtonTests.Button_MultipleTaps_IncrementsCount` (Bruinet) | 14.9 s | 11.4 s | 2.5 s | 63 |
| the same, `BRUINET_APP_PREINSTALLED=1` | **10.8 s** | 7.5 s | 2.4 s | — |
| Kofflard `Search_WithNoName_SaysSo` (3 calls) | 12.7 s | 9.7 s | 2.0 s | — |
| Kofflard `LaneDiagnosticTests` (a grouped list, every lane open, a card from each) | 55–58 s | 9.7–11.8 s | 44–45 s | **2,386** |
| Bruinet `Tests.Collection` (24 tests), with and without the 2026-10-31 list changes | 157 s / 161 s | — | — | — |

### The list test, by Appium command

| Command | Count | Time | What asks for it |
| --- | --- | --- | --- |
| `GET element/rect` | **1,182** | 14.7 s | readiness: "present and shown" on every scope of every call (`HasUsableBounds`), page, list, row and child |
| `POST execute/sync` | 11 | 11.0 s | the swipes, about 1 s each |
| `POST session` | 1 | 10.2 s | the session, the app reinstalled (5.6 s when preinstalled) |
| `GET element/enabled` | 449 | 4.8 s | enabled checks, reads included |
| `GET attribute/resource-id` | 188 | 2.1 s | row keys by AutomationId, read again on every attempt |
| `POST element/elements`, `element/element`, `elements` | 249 | 4.6 s | finding rows and their children |
| `GET element/displayed` | 111 | 0.9 s | visibility, where `IShownWhenFound` does not already answer |
| `GET element/text` | 84 | 0.7 s | **what the test asked for** |

In Bruinet's own terms, the six list searches (`FindItem`) took 27 s. Of that, the name and header reads were 8.6 s; the rest was swipes and readiness.

The pattern holds outside Kofflard: half of the button test's 63 requests are `rect`.

## Where the time goes

1. **The session: 9–11 s per fixture.** Most of it is the reinstall each fixture does (`enforceAppInstall`, Bruinet's own fixture as well as Kofflard's). Preinstalled, it is 5.6 s. A suite of many fixture classes pays this per class.
2. **Readiness reads: 70 % of the requests.** A call checks its whole scope chain on every attempt, page root, collection root, row root, then the child, each with `rect` (and often `enabled` and `displayed`). Each read is 8–40 ms; there are fifteen of them for every read the test wanted.
3. **Swipes: about 1 s each.** A swipe moves a fraction of the list, so a search through 15 rows takes 5–10 swipes each way.
4. **Row keys read again.** `KeyOf` reads a row's `resource-id` on every attempt of a search, for every realized row.
5. **Searches done twice** (in page objects): `FindItem` to see if the item is there, then `ItemWhere` to get it. Kofflard did this in four places.

## The plan

Ordered by gain per effort. Each step names the test it is measured on and the reading that shows it worked.

| # | Step | Where | Expected gain | Measured on, and the proof |
| --- | --- | --- | --- | --- |
| P1 | **Reinstall only when the APK changed.** Compare the APK's hash with one stored on the device (`adb shell` a file under `/data/local/tmp`) and pass `noReset` and no `enforceAppInstall` when it matches. This makes `BRUINET_APP_PREINSTALLED` the default without its risk of testing an old build. | `MauiTestFixtureBase` (Android options) | **about 4 s per fixture class**: 14.9 → 10.8 s on the button test | the button test: `POST session` under 6 s on the second run, and an APK change still reinstalls |
| P2 | **Read readiness once per attempt.** Within one attempt of one call, a scope's root answers `rect`, `enabled` and `displayed` once; the next attempt reads afresh (R2 stays: no waits below the poll). On Android, where found means shown (`IShownWhenFound`), skip `rect` for an element found in this attempt unless its size matters (a zero-size check). | `ControlCalls`, `ContainerObjectBase`, `PageObjectBase.IsCachedRootValid` | 1,182 `rect` → a few hundred; **10–12 s** off the list test, about 1 s off the button test | the list test: `rect` count in Appium's log; wall time |
| P3 | **No enabled check on a read.** `GetText`, `GetAttribute`, `IsVisible` do not need the element enabled; only actions do. | `ViewBase` reads, `ControlCalls.GetWithElement` | most of 449 `enabled`: **3–4 s** off the list test | the list test: `enabled` count |
| P4 | **One swipe moves more.** `mobile: scrollGesture` with `percent: 0.85` over the list's bounds, which also says whether it can scroll further (`canScrollMore`), so a step confirms itself and the rows need not be compared. | `AppiumMauiElement.ScrollContent` | swipes per search halved: **4–5 s** off the list test | the list test: `execute/sync` count |
| P5 | **Find a keyed row in one call.** For a predicate that is a text match (a row by its name), UiAutomator's `UiScrollable(...).scrollIntoView(new UiSelector().text("Bill Fay"))` scrolls the list to it on the device, one request, as the native Android head already does. Offer it as `FindItemByText` / `ItemByText` on Android, falling back to the search elsewhere. | `CollectionObjectBase`, `AppiumMauiElement` | a card search from 5–9 s to about 1 s | the list test's `Shown` and `Lane` durations in the call log |
| P6 | **Row keys read once per row instance.** Cache a row root's AutomationId by its element id for the length of one search (a recycled view gets a new element id from UiAutomator2). | `CollectionObjectBase.KeyOf`, `SearchAttempts` | 188 `resource-id` → about 15: **2 s** | the list test: `resource-id` count |
| P7 | **One call for "find and give me the row".** `FindItem` already returns the row; say so in the collection docs and the `maui-ui-test` skill, and fix the `RunWait(FindItem…) then ItemWhere` idiom where it appears. | docs, skills; Kofflard's `SuggestionList` (done 2026-10-31 for `Present()`) | a second search per lookup | a page object's call log: one `FindItem`, no `ItemWhere` |
| P8 | **Reuse the session across fixture classes** in one run, for suites that test one app: the app restarted (`terminateApp`, `activateApp`), not reinstalled, between classes. | `MauiTestFixtureBase`, xUnit collection fixtures | 5–6 s per class after the first | two classes run together: one `POST session` |

## Results

### P1: reinstall only when the APK changed (built)

`AndroidInstalledBuild` (`Bruinet.Maui/Configuration`), used by `MauiDriverFactory` for every Android session that asks for `enforceAppInstall`:
- before the session, it compares the APK's SHA-256 and the package's `lastUpdateTime` with a record on the device (`/data/local/tmp/bruinet-<package>.build`);
- when both match, the session skips the install;
- after an install, the record is written.

Any other install changes `lastUpdateTime`, so a stale record never lets an old build through. The app's data is still cleared at session start (UiAutomator2's `noReset=false`). `BRUINET_ALWAYS_REINSTALL=1` turns it off, and anything adb cannot answer falls back to installing. No fixture changes: Bruinet's MAUI fixtures, Todo's and Kofflard's all benefit.

| Button test | Wall | `POST session` |
| --- | --- | --- |
| first run (installs, records) | 13.7 s | 8.2 s |
| second run (same build) | **11.0–11.2 s** | **5.5 s** |
| after an outside `adb install -r` | 13.7 s | 8.2 s (installs again, as it should) |

Kofflard's lane test run twice in a row passed both times, the second without an install. Its search history was empty, so the data clearing still works.

### P2: readiness read once per attempt (built)

- **`AttemptMemo`** (`Bruinet.Core.Calls`): `Poller.Until` opens one around each attempt, nested attempts included. Each attempt has its own; the enclosing one comes back when it ends. Used for `HasUsableBounds`, a row's bounds (`ItemObjectBase.IsUsable`), a page root's `IsAttached`, and a container root's alive check (`InstanceKey`).
- **A row's usability is one read**, its bounds: the separate liveness read is gone.
- **`AppiumMauiElement.Rect`** is one request (`_element.Rect`) instead of `Location` plus `Size`.

Kofflard `LaneDiagnosticTests` on the emulator, one test at a time:

| After | Wall | Requests | `rect` | `enabled` | `session` |
| --- | --- | --- | --- | --- | --- |
| nothing (before) | 55–58 s | 2,386 | 1,182 | 449 | 8.6–10.2 s |
| P1, one-request `Rect`, single read for a row | 51 s | 2,183 | 1,182 | 247 | 6.2 s |
| + `AttemptMemo` on bounds and attachment | 45 s | ~1,400 | 388 | 250 | 5.3 s |
| + `AttemptMemo` on the alive check (nested attempts sharing the outer memo) | 43.6 s | 1,247 | 386 | 94 | 5.2 s |
| **as built: a memo per attempt, nested ones their own** | **45.7–47.5 s** | **~1,550** | 610 | 181 | 5.3 s |

**Sharing the outer memo was wrong, and was dropped.** A UAT step runs inside one outer attempt and can act between its inner calls: Kofflard's DISC.03.2 long-pressed a card, then read its "Open in" row, and got the card's readiness answers from before the press ("MissingRoot"). With a memo per attempt, DISC.03 passes 3 of 3 again.

So, as built, **55–58 s became 45.7–47.5 s (−17 to −20 %), with 35 % fewer requests**. What remains is mostly the swipes (11 s, P4), the rest of the readiness reads (386 `rect`), and row ids (188 `resource-id`, P6).

Pinned by `ARead_AsksThePageRootForItsBoundsOnce` (`ListAndPageRootPinTests`).

On Windows the memo cuts UI Automation reads the same way: Bruinet's MAUI UI suite ran in 1 min 57 s against 3 min 9 s, with the shared memo; to be re-measured with the memo as built.

### Next: fewer reads per call

Built: [design-fewer-reads.md](design-fewer-reads.md), D1 to D4. Requests 1,550 → 1,205 (−22 %), the wall time 45.7–47.5 → 44.3–45.5 s. **From the start: 2,386 → 1,205 requests (−49 %), 55–58 s → 44–45 s (−20 %).** Next by size: the swipes (P4, P5), then the page check per attempt (a polling interval per head).

Measure after each step, on its test, and keep the numbers in this file. Run a suite only at the end, once, to check nothing broke.

### What not to do

- **No longer timeouts as a fix.** A search that needs 30 s on the emulator (Kofflard's list budget on Android) is a symptom of P2 to P5. Bring the budget back down once they land.
- **No page-source snapshot reads yet.** Reading the whole tree once per attempt (`GET source`, about 300–500 ms) and answering every read from it would cut requests most of all, but it changes how staleness works (R5, R6). It is worth an AD of its own after P2 shows how far per-attempt memoising gets.

## Open questions

- How much of the 1-second swipe is the emulator (software GPU translation, cold-booted), and how much is the gesture's own duration? Measure P4 on a hardware-accelerated snapshot boot too.
- Does UiAutomator2's `waitForIdleTimeout` (500 ms here) add to every action on an animating screen? The call log's `Click` of 1.3 s on a plain button suggests it might. Measure one tap with `waitForIdleTimeout` 0 and 500.
