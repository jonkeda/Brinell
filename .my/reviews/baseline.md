# Baselines for the 2026-10-21 plan

The numbers each phase of [2026-10-21-plan.md](2026-10-21-plan.md) is measured against. A later
run adds a column; the machine's state is part of each row.

## B1: Tomorrow's UI suite

2026-10-22, `move` at 5a69338e, Tomorrow `game` at 20a87afb. `dotnet test tests/Tomorrow.App.UITests --no-build`
after a build, `BRINELL_CALL_LOG` set to a folder; CPU by `Get-Counter` every 5 s (28 samples,
the counter itself takes about 2 s), as a share of the machine's 32 logical cores. The app runs
in the test host (`TomorrowApp`), so its CPU is the test host's.

| Measure | Before (2026-10-22) |
| --- | --- |
| Machine | not quiet: about 70 `dotnet` processes (build and language servers), machine CPU 39% average, 61% peak during the run |
| Tests | 87: 82 passed, 4 failed, 1 skipped |
| Wall time | 199 s (`dotnet test` reports 3 m 15 s); the build before it 46 s |
| Test host CPU | 5.5% average (about 1.8 cores), 13.7% peak (about 4.4 cores) |
| SQL Server CPU | 0.7% average, 4.1% peak |
| Calls logged | 2,349 in 11 contexts (one per collection), 487 s of calls in all, median 76 ms |
| Near-misses | 0 (see below) |
| Calls over 2 s | `HerePage.Route_Note.AssertText` 8.2 s (passed); `LoginPage.login-submit.Click` 2.0 to 2.8 s, seven times; `HerePage.App_ErrorBanner.AssertVisible` 2.4 s; `HerePage.Here_Board.AssertAttribute` 2.3 s |

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

## B3

Not run yet.
