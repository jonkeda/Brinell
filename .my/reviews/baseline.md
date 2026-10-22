# Baselines for the 2026-10-21 plan

The numbers each phase of [2026-10-21-plan.md](2026-10-21-plan.md) is measured against. A later
run adds a column; the machine's state is part of each row.

## B1: Tomorrow's UI suite

2026-10-22, `move` at 5a69338e, Tomorrow `game` at 20a87afb. `dotnet test tests/Tomorrow.App.UITests --no-build`
after a build, `BRINELL_CALL_LOG` set to a folder; CPU by `Get-Counter` every 5 s (28 samples,
the counter itself takes about 2 s), as a share of the machine's 32 logical cores. The app runs
in the test host (`TomorrowApp`), so its CPU is the test host's.

| Measure | Before (2026-10-22) | After F1-F3 (2026-10-22) | After H1 | After H2 |
| --- | --- | --- | --- | --- |
| Commits | `move` 5a69338e, `game` 20a87afb | `move` 40b70088, the six sleeps gone | `move` 840c7712, one-trip probe | `move` 502657bc, Playwright's actionability |
| Machine | not quiet: about 70 `dotnet` processes, machine CPU 39% average, 61% peak | the same: 40% average, 68% peak | 32% average, 58% peak | 33% average, 63% peak |
| Tests | 87: 82 passed, 4 failed, 1 skipped | 87: 86 passed, 0 failed, 1 skipped | 87: 86 passed, 0 failed, 1 skipped | 87: 86 passed, 0 failed, 1 skipped |
| Wall time | 199 s (`dotnet test` 3 m 15 s) | 142 s (2 m 19 s) | 128 s (2 m 6 s) | 81 s (1 m 18 s) |
| Test host CPU | 5.5% average (about 1.8 cores), 13.7% peak | 4.7% average (about 1.5 cores), 16.8% peak | 6.3% average, 20.6% peak | 6.2% average, 20.7% peak |
| SQL Server CPU | 0.7% average, 4.1% peak | 1.0% average, 5.7% peak | 1.0% average, 4.1% peak | 1.3% average, 3.5% peak |
| Calls logged | 2,349, 487 s in all, median 76 ms | 2,394, 371 s in all, median 50 ms | 2,387, 236 s in all, median 48 ms | 2,389, 162 s in all, median 32 ms |
| Near-misses | 0 (see below) | 0 | 0 | 0 |
| Calls over 2 s | `Route_Note.AssertText` 8.2 s; `login-submit.Click` 2.0 to 2.8 s, seven times; `App_ErrorBanner.AssertVisible` 2.4 s; `Here_Board.AssertAttribute` 2.3 s | `Route_Note.AssertText` 4.6 s; `login-submit.Click` 2.9 s, twice | `Route_Note.AssertText` 5.5 s; `login-submit.Click` 4.7 s twice and 2.7 s; `registerSubmit.Click` 3.8 s | `Route_Note.AssertText` 4.4 s; `Fixture_Panel.AssertExists` 3.0 s (see below); `login-submit.Click` 2.7 s twice |

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

**The four failures pass alone** (14 tests in their groups, 44 s, all green), so they are load:

- `SurvivorTests.Home_AddNewSurvivor_PicksTheStartFirst` and `StartTests.Home_WithoutSurvivor_LinksToTheStartPicker`, both at 53 s into the run: `login-submit.Click` spent its 4,964 ms inside Playwright's "waiting for scheduled navigations to finish", i.e. the sign-in POST answered late while every collection signed in at once. The exception is a raw `System.TimeoutException`, not one of Brinell's (H2).
- `SRV.12.4 A new survivor beside the others`: `NewSurvivor_Begin` "found but is disabled: not enabled still after 5000 ms".
- `SRV.05.5 Begin waits for loose points and a name`: `AttributeRows` "had no item 'Agility' within 5000 ms … MissingRoot".

**The near-miss rule misses these calls.** `ControlCall.IsNearMiss` counts a call only when it
took more than one attempt (`Attempts > 1`) and the last attempt came after half the budget. A
click that spends 2.8 s of 5 s inside one Playwright action succeeds on its first attempt, so it
is never flagged. That is the exact shape of the failures above. H6 also judges a single attempt
whose action used more than half the budget.

The CSVs and the CPU samples are not committed; `calls.py` in the session scratchpad summarized
them (results per direction, warnings, the slowest exits).

## B2: Brinell's web suites

2026-10-22, `move` at f4df2ddd, run one after the other, each built first, the same switches as B1.
CPU as a share of 32 cores; "dotnet" sums every `dotnet` process (the Todo app runs as one), so
on a machine this busy it includes other work.

| Suite | Tests | Wall | Calls (median) | Test host CPU avg / peak |
| --- | --- | --- | --- | --- |
| `Brinell.Html.UITests` | 25 passed | 37 s | 110 (86 ms) | 1.0% / 2.2% |
| `Brinell.Blazor.UITests` | 35 passed | 44 s | 111 (45 ms) | 0.5% / 1.7% |
| `Brinell.Samples.Todo.Web.UITests` | 10 passed, 1 skipped | 26 s | 137 (61 ms) | 1.3% / 2.9% |
| `Brinell.Samples.Todo.Web.Uat` | 35 passed | 105 s | 524 (51 ms) | 1.4% / 6.0% |

- **Machine:** CPU 32% to 53% on average across the four suites, 72% at peak; not quiet, as in B1.
- **Near-misses:** 0 in 882 calls.
- **Errors in the log, all expected:** four, from negative tests. Html's recycled-row `Click` scope not ready in 500 ms and `login-btn` "disabled: not enabled still after 5000 ms"; the Todo scenarios' two `StatusText.WaitText` meant to return false ("6 attempts; last: Pending").
- **First load is the slow call.** Html's six collections each spend 2.8 s in their first `SampleShell.AssertIdle`, all ending at the same instant (the browsers launch together); later `AssertIdle`s take under 300 ms. The Todo suites' slowest calls are their first `TodoListPage.AssertIdle` too (1.9 s, 2.3 s). H1's one-trip probe should show here.
- **Builds:** 12 s, 4 s, 18 s, 11 s.

**After F1-F3** (`move` 40b70088), the same four suites, all green again:

| Suite | Wall before | Wall after | Median call after |
| --- | --- | --- | --- |
| `Brinell.Html.UITests` | 37 s | 35 s | 62 ms |
| `Brinell.Blazor.UITests` | 44 s | 41 s | 38 ms |
| `Brinell.Samples.Todo.Web.UITests` | 26 s | 25 s | 64 ms |
| `Brinell.Samples.Todo.Web.Uat` | 105 s | 106 s | 54 ms |

After H1 (`move` 840c7712): Html 32 s, Blazor 39 s, Todo web UI tests 18 s, Todo web
scenarios 82 s, all green; calls in all 41 s, 8 s, 12 s, 41 s (from 57, 7, 16, 61). The Todo
suites gained most (a quarter of their wall time), since every scenario opens pages. Html's
slowest calls are still each collection's first, about 2.8 s while the browsers start together.

After H2 (`move` 502657bc): Html 27 s, Blazor 35 s, Todo web UI tests 15 s, Todo web
scenarios 55 s, all green; calls in all 18 s, 8 s, 8 s, 30 s. Html's first-call 2.8 s is gone
(one call over 2 s left). From the first baseline the four suites went from 212 s to 132 s.

These suites barely poll (their calls pass on the first attempt), so the pause had little to cost them; within noise.

## B3

Not run yet.
