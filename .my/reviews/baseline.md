# Baselines for the 2026-10-21 plan

The numbers each phase of [2026-10-21-plan.md](2026-10-21-plan.md) is measured against. A later
run adds a column; the machine's state is part of each row.

## B1: Tomorrow's UI suite

2026-10-22, `move` at 5a69338e, Tomorrow `game` at 20a87afb. `dotnet test tests/Tomorrow.App.UITests --no-build`
after a build, `BRUINELL_CALL_LOG` set to a folder; CPU by `Get-Counter` every 5 s (28 samples,
the counter itself takes about 2 s), as a share of the machine's 32 logical cores. The app runs
in the test host (`TomorrowApp`), so its CPU is the test host's.

| Measure | Before (2026-10-22) | After F1-F3 (2026-10-22) | After H1 | After H2 | After H3-H4 (two runs) |
| --- | --- | --- | --- | --- | --- |
| Commits | `move` 5a69338e, `game` 20a87afb | `move` 40b70088, the six sleeps gone | `move` 840c7712, one-trip probe | `move` 502657bc, Playwright's actionability | `move` bb4aa5e0, AccessibilityId and screenshots |
| Machine | not quiet: about 70 `dotnet` processes, machine CPU 39% average, 61% peak | the same: 40% average, 68% peak | 32% average, 58% peak | 33% average, 63% peak | 31% average, 61% to 63% peak |
| Tests | 87: 82 passed, 4 failed, 1 skipped | 87: 86 passed, 0 failed, 1 skipped | 87: 86 passed, 0 failed, 1 skipped | 87: 86 passed, 0 failed, 1 skipped | 87: 86 passed, 0 failed, 1 skipped, twice |
| Wall time | 199 s (`dotnet test` 3 m 15 s) | 142 s (2 m 19 s) | 128 s (2 m 6 s) | 81 s (1 m 18 s) | 125 s (2 m 2 s) and 127 s (1 m 33 s) |
| Test host CPU | 5.5% average (about 1.8 cores), 13.7% peak | 4.7% average (about 1.5 cores), 16.8% peak | 6.3% average, 20.6% peak | 6.2% average, 20.7% peak | 5.3% and 6.2% average, 16.8% peak |
| SQL Server CPU | 0.7% average, 4.1% peak | 1.0% average, 5.7% peak | 1.0% average, 4.1% peak | 1.3% average, 3.5% peak | 1.2% average, 6.3% peak |
| Calls logged | 2,349, 487 s in all, median 76 ms | 2,394, 371 s in all, median 50 ms | 2,387, 236 s in all, median 48 ms | 2,389, 162 s in all, median 32 ms | 2,391 and 2,387, 188 s and 180 s in all, median 38 ms |
| Near-misses | 0 (see below) | 0 | 0 | 0 | 0 |
| Calls over 2 s | `Route_Note.AssertText` 8.2 s; `login-submit.Click` 2.0 to 2.8 s, seven times; `App_ErrorBanner.AssertVisible` 2.4 s; `Here_Board.AssertAttribute` 2.3 s | `Route_Note.AssertText` 4.6 s; `login-submit.Click` 2.9 s, twice | `Route_Note.AssertText` 5.5 s; `login-submit.Click` 4.7 s twice and 2.7 s; `registerSubmit.Click` 3.8 s | `Route_Note.AssertText` 4.4 s; `Fixture_Panel.AssertExists` 3.0 s (see below); `login-submit.Click` 2.7 s twice | `Route_Note.AssertText` 4.7 s; `Fixture_Panel.AssertExists` 3.0 s twice; `login-submit.Click` 2.2 s twice |

The after column is one run on the same busy machine. The wall time (minus 29%) and the median
call (minus 34%) moved more than the test host's CPU did. The likely cause is the pause: a spinning
pause starved the app's own threads in the same process, rather than showing up as CPU of its
own. One run does not separate F1 from F3 (F3 removed about 10 s of fixed sleeping at most) or
either from load; a second run of each would.

After H1, total call time fell by another 36% (371 s to 236 s) and the wall time by 10%. The
sign-in clicks still come close to their 5 s budget under the start-up rush (4.7 s twice). The
probe does not touch that: the time is spent in Playwright's wait for the POST's navigation,
which is H2's and H6's.

After H2, the wall time fell by another 37% (128 s to 81 s) and the call time by 31% (236 s to
162 s); the median call is 32 ms. The sign-in clicks are 2.7 s, no longer near the budget.
The one error in the log is Tomorrow's own retry: `HerePage.PressE` presses E again when the
verb list has not opened in 3 s, because the board drops a key pressed before it is drawn
(WORLD-RW-51). Faster actions reach that early press more often, and each one costs 3 s.

After H3 and H4 the suite ran twice: 2 m 2 s and 1 m 33 s by `dotnet test`'s clock (H2's run
was 1 m 18 s), with the wall time 125 s and 127 s. Neither change runs on a passing call (H3 is
a locator only no Tomorrow page uses, H4 runs on a failure), and the time inside calls barely
moved (162 s to 180 s and 188 s). The test time outside calls did (366 s summed over tests
after H2, 559 s in the first run after H4). So the spread is the machine: one run per change
is not enough to rank the later steps, and H6 should measure with `tools/Scripts/repeat-run.ps1`
over several runs.

**The four failures pass alone** (14 tests in their groups, 44 s, all green), so they are load:

- `SurvivorTests.Home_AddNewSurvivor_PicksTheStartFirst` and `StartTests.Home_WithoutSurvivor_LinksToTheStartPicker`, both at 53 s into the run: `login-submit.Click` spent its 4,964 ms inside Playwright's "waiting for scheduled navigations to finish", i.e. the sign-in POST answered late while every collection signed in at once. The exception is a raw `System.TimeoutException`, not one of Bruinell's (H2).
- `SRV.12.4 A new survivor beside the others`: `NewSurvivor_Begin` "found but is disabled: not enabled still after 5000 ms".
- `SRV.05.5 Begin waits for loose points and a name`: `AttributeRows` "had no item 'Agility' within 5000 ms … MissingRoot".

**The near-miss rule misses these calls.** `ControlCall.IsNearMiss` counts a call only when it
took more than one attempt (`Attempts > 1`) and the last attempt came after half the budget. A
click that spends 2.8 s of 5 s inside one Playwright action succeeds on its first attempt, so it
is never flagged. That is the exact shape of the failures above. H6 also judges a single attempt
whose action used more than half the budget.

The CSVs and the CPU samples are not committed; `calls.py` in the session scratchpad summarized
them (results per direction, warnings, the slowest exits).

## B2: Bruinell's web suites

2026-10-22, `move` at f4df2ddd, run one after the other, each built first, the same switches as B1.
CPU as a share of 32 cores; "dotnet" sums every `dotnet` process (the Todo app runs as one), so
on a machine this busy it includes other work.

| Suite | Tests | Wall | Calls (median) | Test host CPU avg / peak |
| --- | --- | --- | --- | --- |
| `Bruinell.Html.UITests` | 25 passed | 37 s | 110 (86 ms) | 1.0% / 2.2% |
| `Bruinell.Blazor.UITests` | 35 passed | 44 s | 111 (45 ms) | 0.5% / 1.7% |
| `Bruinell.Samples.Todo.Web.UITests` | 10 passed, 1 skipped | 26 s | 137 (61 ms) | 1.3% / 2.9% |
| `Bruinell.Samples.Todo.Web.Uat` | 35 passed | 105 s | 524 (51 ms) | 1.4% / 6.0% |

- **Machine:** CPU 32% to 53% on average across the four suites, 72% at peak; not quiet, as in B1.
- **Near-misses:** 0 in 882 calls.
- **Errors in the log, all expected:** four, from negative tests. Html's recycled-row `Click` scope not ready in 500 ms and `login-btn` "disabled: not enabled still after 5000 ms"; the Todo scenarios' two `StatusText.WaitText` meant to return false ("6 attempts; last: Pending").
- **First load is the slow call.** Html's six collections each spend 2.8 s in their first `SampleShell.AssertIdle`, all ending at the same instant (the browsers launch together); later `AssertIdle`s take under 300 ms. The Todo suites' slowest calls are their first `TodoListPage.AssertIdle` too (1.9 s, 2.3 s). H1's one-trip probe should show here.
- **Builds:** 12 s, 4 s, 18 s, 11 s.

**After F1-F3** (`move` 40b70088), the same four suites, all green again:

| Suite | Wall before | Wall after | Median call after |
| --- | --- | --- | --- |
| `Bruinell.Html.UITests` | 37 s | 35 s | 62 ms |
| `Bruinell.Blazor.UITests` | 44 s | 41 s | 38 ms |
| `Bruinell.Samples.Todo.Web.UITests` | 26 s | 25 s | 64 ms |
| `Bruinell.Samples.Todo.Web.Uat` | 105 s | 106 s | 54 ms |

After H1 (`move` 840c7712): Html 32 s, Blazor 39 s, Todo web UI tests 18 s, Todo web
scenarios 82 s, all green; calls in all 41 s, 8 s, 12 s, 41 s (from 57, 7, 16, 61). The Todo
suites gained most (a quarter of their wall time), since every scenario opens pages. Html's
slowest calls are still each collection's first, about 2.8 s while the browsers start together.

After H2 (`move` 502657bc): Html 27 s, Blazor 35 s, Todo web UI tests 15 s, Todo web
scenarios 55 s, all green; calls in all 18 s, 8 s, 8 s, 30 s. Html's first-call 2.8 s is gone
(one call over 2 s left). From the first baseline the four suites went from 212 s to 132 s.

After H3 and H4 (`move` bb4aa5e0): Html 28 s, Blazor 41 s, Todo web UI tests 17 s, Todo web
scenarios 81 s, all green; calls in all 18 s, 7 s, 12 s, 41 s. The same spread as B1's: the
Todo scenarios' 55 s after H2 was the low end, not a level.

These suites barely poll (their calls pass on the first attempt), so the pause had little to cost them; within noise.

## H5 and H6: three runs each, and the studio's suite

H5's first form (`move` fae7c5f9, a new browser context per sign-in) and H6 (c3ffdb2e), three
runs of Tomorrow's UI suite: 161, 165 and 159 s; SRV.06.3 failed in one, `Sheet_OnAPhone_ShowsTabs`
in another, both the Sheet page not loading in time. H6's first `near-misses.md` gave the cause:
every login click at 82 to 91% of its budget, from about 50% before. A new context throws away the
HTTP cache, so every sign-in loaded Blazor's scripts, styles and fonts cold. Two phone tests also
failed in the first run, a sign-in in the test closing the page `UsePhone` had resized.

H5 reworked (965eedd0): the same context, its cookies, `localStorage` and `sessionStorage` cleared.
Three runs: 102, 154 and 129 s, 86 of 86 passed in each. `near-misses.md`: none in the first, the
two login clicks of the start-up rush at 87% in the others, 11 to 15 ms of each finding the button
and the rest Playwright waiting for the sign-in POST's navigation (the server, under ten
collections signing in at once; since H2 a click that overruns there is done, not failed).
RW-94, 138, 217 and 237 did not show.

Tomorrow's studio suite (`tests/Tomorrow.Studio.UITests`, the same fixture source), once each:

| Measure | Before today (`game` 20a87afb, `move` a7d989c7) | After H1-H6 (`move` 965eedd0) |
| --- | --- | --- |
| Tests | 103 passed | 102 passed, PLT.03.1 failed (passes alone, twice) |
| Wall time | 602 s (9 m 59 s) | 534 s (8 m 50 s) |
| Test time summed | 3,076 s | 2,955 s |
| Near-misses | not counted | `QuestTry_Cast.SelectByValue` 94%, `World_Results` rows 92%, `BenchSites_Summary.AssertAttribute` 89%, `RulesPage.WaitReady` 54% (5.4 s of 10 s) |

The suite is slow on this machine either way (the register had 2 m 44 s at platform/425, 4 to 6
minutes on 2026-10-20); a test moves by up to a minute between runs with where it falls in the
start-up rush. PLT.03.1's failure is the Rules page's fall rules not loaded in 5 s, a page that
timed out before (Tomorrow's abd8b2e6), and the near-miss list now names the calls to look at next.

## C1: a control's calls in Core

`move` 0ee76775, one run each, all green. Tomorrow's UI suite: 108 s (1 m 45 s), 86 of 86, its
calls 176 s in all, median 36 ms; the only near-misses the two start-up login clicks. Bruinell's
web suites: Html 32 s (27 tests, the two reset tests new), Blazor 39 s, Todo web UI tests 18 s,
Todo web scenarios 82 s, the same as after H4. Html's calls came to 43 s, from 18 s: four
first-call `SampleShell.AssertIdle`s at about 3.5 s while the browsers start together (a page
call, which `ControlCalls` does not run; the reset tests add two browsers to that start), the
rest as before. The MAUI and native Android UI suites need the emulator and were not run; their
unit suites are green (181 and 69).

C1's second step (`move` 4d8f6e4a, MAUI's rooted scopes on `ControlCalls`): MAUI's unit suite 181
of 181 twice; Html's UI suite 27 of 27 three times (one run before them listed three tests at the
same moment, its summary not kept); Tomorrow's UI suite 86 of 86 in 77 s (1 m 14 s), the fastest
run of the day, within the machine's spread.

## C2 to C4

`move` 9112ad95, one run each, all green: Tomorrow's UI suite 86 of 86 in 86 s (1 m 22 s);
Html 30 s (27), Blazor 39 s (35), Todo web UI tests 18 s (10, 1 skipped), Todo web scenarios
82 s (35). Unit suites: Core 49, UAT 222, Html 72, MAUI 181, Narrator 74, Recording 31. FlaUI's
bounded waits and the MAUI recorded-interaction test need a Windows MAUI head or the emulator and
were not run.

## C5

`move` 9b71c97a. Every unit suite green: Core 50 (with `NoCallModelStack_ReferencesTheFirstModel`),
UAT 222, Html 72, Blazor 173, MAUI 181, WPF 10, WinForms 9, Stride automation 3. Tomorrow's UI
suite twice: 83 of 86 at 1 m 59 s on a busy machine (SRV.05.5, the first baseline's load failure;
SRV.05.7 and SRV.08.1, a value and a page late), the three green alone twice; then 86 of 86 in
1 m 36 s. The solution build fails its restore on two NuGet advisories (the Stride sample's
`System.Security.Cryptography.Pkcs`, `Bruinell.Cli.Tests`' `MessagePack`), so it compiles
nothing: build with `-p:NuGetAudit=false` to check every project until the packages are updated.

## B3: MAUI on Android, and M1 on a device

2026-10-22, `move` 140c69fb (everything through M1), `Bruinell_Android` (8 cores), Appium 3.1.2,
both apps installed fresh, `BRUINELL_APP_PREINSTALLED=1`, the call log on.

**M1 on the device.** The emulator's window animation scale was set to 1 before a run of the
button and ActivityIndicator tests (10 of 10, 59 s). It read 0.0 from the session's start and 1.0
again after it: the factory's `disableWindowAnimation` reached the device, so the runbook's three
`adb` commands are no longer needed. On the page with a running ActivityIndicator every call took
0.1 to 0.7 s, so the 100 ms idle bound comes from the factory now that the fixtures do not set it
(about 11 s per lookup without it).

**The full mobile suite:** 259 passed, 24 failed, 20 skipped in 23 m 03 s (wall 23 m 15 s).
The 24 are the known gaps (`.my/bruinell/maui-android.md` in Tomorrow): MediaElement 11, CarouselView
5, RatingView 4, DrawingView 3, ProductCollection `ItemWhere_ScrollsToFindOffscreenRow` 1. The
last full run before today's changes: 258 passed, the same 24, 20 skipped in 23 m 17 s. 1,942
calls, 1,067 s in all, mean 549 ms; `WaitLoaded` 625 times, mean 293 ms.

`near-misses.md` lists 55 calls. The worst are the scroll page's, passed at 130 to 165% of their
5 s budget in one attempt (`ScrollStatusLabel.AssertTextContains` 8.2 s): scroll-to-find runs past
the call's budget, the review's M3, now measured. `TestTimePicker.SetTime` passes at 117 to 125%
with its lookup done in tens of milliseconds, so the time is in the action and its confirmation.

## M2 and M3 on the emulator

**M2** (`move` 4505a4c7): the UiAutomator2 server killed 40 s into a run of five control classes
(29 tests), twice. The test it hit failed at once with `AppUnavailableException` ("the Appium
session has ended", from `AppiumErrors.Live`), the next opened a new session (about 20 s) and the
rest passed: 28 of 29 the second time. The first time the test after the restart also failed (its
page not ready within 5 s), then 13 passed; not seen again.

**M3, not committed.** One scroll routine (`AppiumMauiDriver.ScrollUntil`) for scroll-to-find
and an element's scroll into view, under the call's budget, with one lookup a swipe (the id and
the description in one `UiSelector` query) and swipes of 90% instead of 70%. On `ScrollTests` and
`TimePickerTests` (17): the bottom button's click went from 6.7 s to 5.1 s and the top label after
it from 8.2 s to 6.6 s, but `MiddleButton_Click_UpdatesStatus` fails: from the middle the search
goes down first, reaches the end, comes back up, and is out of its 5 s before the top label. A
swipe with its lookup costs about a second on this emulator, and the page's worst paths need 6 to
7 s of scrolling. Before M3 they passed by overrunning the budget (R2). Decided: the bound stays,
and `ScrollTests`' calls that cross the page pass `timeoutMs` 10 000 (`CrossPageMs`).

**M3 committed** with that: `ScrollTests` 7 of 7 in 1 m 11 s (1 m 32 s in B3); the full mobile
suite 259 passed, the same 24 known gaps, 20 skipped in 22 m 36 s. Calls over their budget in
`near-misses.md`: 8, from 16 in B3; the scroll page's are gone, and all 8 left are
`TestTimePicker.SetTime` (its lookup tens of milliseconds, the time in the action and its
confirmation: M4's picker wait).

**M4**: the picker, wheel and chrome waits on `Settle.Until`; `SetTime` three round trips fewer.
Pickers and Shell (flyout, tabs, stack), the toolbar and navigation controls and `TimePicker` on
the emulator: 33 and 46 tests green. `SetTime` mean 5.46 s to 5.02 s. The full mobile suite 259
passed, the same 24 known gaps, 20 skipped in 22 m 41 s; calls over budget 8 to 2 (one `SetTime`
at 107%, one `TestStepper.SetValue` at 101%), both actions whose lookup took tens of milliseconds.
`FindChrome` keeps a short wait rather than a plain lookup: it runs inside an action, where a miss
is not asked again by the call's poll.

**M5 to M7** (`move` 5fa3f40c, d655afa8, then M7): the full mobile suite 259 passed, the same 24
known gaps, 20 skipped in 24 m 01 s (22 m 41 s after M4). `WaitArrived`, now a logged call, took
680 ms a page opened (266, 180 s in all); its poll back at the fixture's 25 ms brought that to
631 ms on a slice, most of it the lookup that sees the hub leave. The dialog check costs 20 ms an
action. Calls over budget: 5, of which one `SetTime` at 11.3 s (its lookup 0.6 s: one round trip
of the action stalled) and the rest the known `SetTime` and `SetValue` at about 100%. A slice with
M7 (popups, alerts, buttons, entries, pickers, IndicatorView, ContentDialog): 39 of 39. The Windows
head was not run (it needs a live desktop); M7's Windows side rests on the build and the unit tests.

## The Windows head, after M7, and the rest

`move` 9b0cc10b, Bruinell's MAUI suite on Windows (FlaUI, both sample apps built for Windows):
357 passed, 1 failed, 2 skipped in 1 m 48 s. The failure, `ForegroundWatchdogTests.Navigating_
DoesNotTakeTheForeground` (the app took the foreground 4 times), passed alone twice. The last
recorded Windows run (2026-10-21) had 349 passed and 9 failed, put down to a locked screen.

The NuGet advisories (4a8e2d9f): the solution restores and compiles again; `Bruinell.Cli.Tests`
320 of 320 in a separate worktree, since this checkout's `tools/Bruinell.Cli/bin` is held by the
running Bruinell MCP servers. `[UiFact]` (9b0cc10b): a failure made on purpose in
`Bruinell.Html.UITests` left a PNG of the page as it failed; Tomorrow's 29 UI facts use it and its
game suite is 86 of 86.

