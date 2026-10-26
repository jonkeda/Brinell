# SQL replay: step 8 results

Agent document. Step 8 of [plan.md](plan.md): Todo's web suite on bUnit and its MAUI suite on
MauiUnit record and replay the app's SQLite file. T1 to T5 pass on 2026-10-26.

## Checks

| # | Result | What ran |
| --- | --- | --- |
| T1 | Pass | Web on bUnit: recorded twice, 10 of 10 replayed 5 times with an empty file, no miss; with the recordings moved away replay fails on each test ("No recording at ...") |
| T2 | Pass | MAUI on MauiUnit: recorded twice, 44 of 44 replayed 5 times, then 3 more after the review fix |
| T3 | Pass | Replay off: web on bUnit (10) and in the browser (10, 16 s), MAUI on MauiUnit (44, 14 to 17 s); the integration tests (61); Uat, Web.Uat and both Mixed suites build |
| T4 | Pass | `ORDER BY title` added to `SqliteTodoRepository.GetAllAsync`: the web tests that list fail in replay, each naming the command and its recording; reverted |
| T5 | Pass | `bruinet sql stale` 0 of 10 and 0 of 44; recorded twice again from unchanged code, `bruinet sql reviewed` "every recording reviewed" on both; `RecordingReviewTests` in each suite |

## What was built

| Part | Where |
| --- | --- |
| `TodoSqlReplay`: one app's session per test, `Wrap`, `Begin`, `BeginStartup`, `End`, `TestName`, `RecordingsOf`, the schema snapshot from a migrated file | `samples/Todo/tests/Bruinet.Samples.Todo.TestSupport/TodoSqlReplay.cs` |
| An empty seed in a replayed run | `TodoTestSet.SeedDatabaseAsync` |
| Web: the head's `Replay` (bUnit on, browser off), `TodoDatabase.Wrap` through log and replay, `StartTest(output)`, `EndTest()` | `TodoWebHead.cs`, `TodoWebDevice.cs`, `TodoWebFixture.cs`, `Tests/JourneyTests.cs` |
| MAUI: the fixture's replay (a field, so it is there when the base composes the app), the startup session, `StartTest(output)`, `EndTest()`, the wait for the startup sync | `TodoAppFixtureBase.cs`, the 12 test classes |
| Recordings, `schema.json`, `reviewed.json` | `Bruinet.Samples.Todo.Web.UITests/Recordings`, `Bruinet.Samples.Todo.UITests/Recordings` |
| Volatile values shared across a suite: `RecordingReview.VolatileValues`, `Hash(json, shared)`, `RecordingDiff.Compare(..., volatileBefore, volatileAfter)`, used by `bruinet sql diff` | `srcnew/Bruinet.Mocking.Data/Replay`, `tools/Bruinet.Cli/Sql/SqlCommands.cs` |

## Found while building

- **An app that outlives its tests works outside them.** The MAUI fixture starts the app before the
  first test, and its first list load and sync read the file then. Each fixture records a startup
  session (`<Fixture>.startup.json`), ended by its first test.
- **Quiet isn't done while a request is out.** The startup sync's users refresh landed in the startup
  recording or the first test's, depending on HTTP timing. Replayed on MauiUnit, a sync-on-start
  fixture waits for `ISyncService.LastResult` before its first test, and each test waits for quiet
  before its session ends.
- **Theory cases need their own recording.** `TestName` keeps a theory's arguments.
- **Tests that share an app read each other's rows**, with the earlier tests' new ids and titles,
  volatile only in those tests' recordings: the review hash and the diff treat a value volatile
  anywhere in the suite as volatile everywhere (a unit test covers it).
- **Replay isn't faster here.** SQLite in process is cheap; on MauiUnit a replayed run takes 24 s
  against 14 to 17 s, from the waits that keep each test's work inside its session. What it removes
  is the file and its seeding.

## Open

- The browser and Windows heads run the app in a process of its own: not replayed (step 4's hook would serve them).
- The UAT suites read the file directly (`TodoDbReader`); not replayed yet.
- The MAUI suite stays serial (`DisableTestParallelization`); replay doesn't change that here.
