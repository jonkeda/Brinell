# SQL replay: step 4 results

Agent document. Step 4 of [plan.md](plan.md): the third head, an app in a process of its own,
reached by a test-id hook. H1 to H6 pass on 2026-10-25; H6 without IIS.

## Checks

| # | Result | Test |
| --- | --- | --- |
| H1 | Pass | The spike suite replays 3 times per head with `BRUINET_SQL_CONNECTION` on `nowhere.invalid`, no miss |
| H2 | Pass | `HookTests.Hook_RecordsThenReplaysEachTestInParallel_FromAnotherProcess`: the sample started with `dotnet` from its own output folder, recorded, then replayed with its database file deleted |
| H3 | Pass | The same test: 8 tests at once, 3 replay rounds, each `Town<i>:2|409` from its own recording; half name their test by cookie, half by header |
| H4 | Pass | `HookTests.Miss_InTheApp_ComesBackToTheTest`: the app answers 500, `ReplayHook.EndAsync` throws `ReplayMissException` naming the `INSERT` |
| H5 | Pass | `HookTests.Off_TheAppHasNoControlPaths_AndRunsOnItsDatabase`: begin is 404, the app reads and writes its database, no recordings folder made |
| H6 | Pass, without IIS | Net48 `HookTests.Hook_BeginsFindsAndEnds_AndTheFactoryFindsTheSessionThroughTheFallback`; `SqlReplayModule` builds for net48 |

## What was built

| Part | Where |
| --- | --- |
| `ReplayRegistry`, `ReplayHook`, `HookResponse` | `srcnew/Bruinet.Mocking.Data/Replay/ReplayHook.cs` |
| `ReplayScope.Fallback` | `Replay/ReplaySession.cs` |
| `UseSqlReplayHook()` | new `srcnew/Bruinet.Mocking.Data.AspNetCore` |
| `SqlReplayModule` | new `srcnew/Bruinet.Mocking.Data.SystemWeb` (net48) |
| The hook sample: minimal API over SQLite through `ReplayingDbProviderFactory` | new `samples/Bruinet.Samples.Sql.Hook` (solution folder Web) |
| Its tests, `HookApp` starting it as a process | new `testsnew/Bruinet.Mocking.Data.Hook.Tests` |

## How a test uses it

1. The app runs with `BRUINET_SQL_REPLAY=Record|Replay` and `BRUINET_SQL_RECORDINGS=<folder>`
   (optionally `BRUINET_SQL_SCHEMA=<snapshot>`), `UseSqlReplayHook()` or the module in its pipeline,
   and its connections from `ReplayingDbProviderFactory` or EF's `UseSqlReplay()`.
2. A browser test navigates to `ReplayHook.BeginUrl(app, test)` first: the cookie names its test on
   every request after. An API test calls `ReplayHook.BeginAsync` and sends `X-Bruinet-Test`.
3. At its end the test calls `ReplayHook.EndAsync`: the recording is saved, or the misses thrown.

## Open

- The module is not run under IIS here: the owner's legacy system is the first place it runs.
- A `new SqlConnection` in the app's code isn't reached by the hook; such code needs its connections
  from a factory first (the proposal's ADO.NET adapter, as before).
- Not wired into Bruinet's browser fixtures (`Bruinet.Html.Playwright`): a test base calls
  `BeginUrl` itself for now.
- `dotnet build Bruinet.sln` not run whole: running bruinet processes lock the CLI's `bin`; every
  project this step touched was built and tested.
