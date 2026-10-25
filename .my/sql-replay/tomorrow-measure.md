# SQL log on Tomorrow: the studio suite on bUnit

Step 1's measurement, run on a real suite: Tomorrow's studio suite on its bUnit head, captured by
Extended Events with no change to Tomorrow's code. It works, and its numbers say replay would pay
in time but cost in upkeep: the database is a fifth of a test, and the tables most tests touch
are shared.

## The run

The suite serially on bUnit (`BRUINELL_HEAD=bunit`, `BRUINELL_TIMINGS`, one test at a time): 143
passed, 22 skipped (browser only), about a minute. The capture watched every
`TomorrowStudioUITest%` database on `.\MSSQLSERVER01`, as the suite makes one per run: 12,839
commands, 87 tests and scenarios with commands, 13 commands outside every test.

## The numbers

| What | Value |
| --- | --- |
| The database's share of a test's own steps | median 22.8%, p90 64.5% |
| With its arrange | median 22.1%; the arrange's database 1.8 s of 7.5 s |
| Database time in all | 18.8 s in the tests' 67.2 s |
| Fan-out: the tests an object reaches | median 10.3%, p90 70% |

Against the plan's thresholds: the share passes (at least 15%), the fan-out doesn't (a typical
object should reach at most 5% of the tests). The objects most tests touch are the shared ones:
`auth.adminaccounts` 78%, `auth.aspnetusers`, `settings.setting` and `calls.job` about 75%, and
`geo.town`, `geo.playarea` and `geo.playareacell` about 70%. A change to any of those would make
most recordings stale.

Server time sums commands that run at once: one test's database time (1.1 s) is more than its
wall time (0.8 s), as the studio's background work runs beside the page.

## Where the time goes

- **A third is waiting for a lock.** `sp_getapplock` with a timeout took 7.2 s in 393 calls,
  35% of all database time. The raw capture names it: the call queue's claim lock, `calls:claim`
  (392 calls, 7.2 s; the seed's lock once, 0 ms). A claim alone never waits; claims that come
  together wait 18 ms each on average, 7.0 of the 7.2 s. `JobSignal.Wake` wakes all four of the
  studio's claim loops at once, and each claim holds the lock through two sweeping updates, the
  lanes' check and the pick, so the three that find nothing queue behind the one that gets the
  job. It is waiting in the background, not work, and the job itself starts first: it costs the
  tests little time. Fixed in Tomorrow (game `b6a7bab4`): a new job wakes one loop. Two runs
  each way: lock waiting 3.5 and 3.9 s before, 0.7 and 0.8 s after; claims 355 and 342 before,
  273 and 239 after; the suite's time unchanged (50 to 59 s), as the waiting was in the background.
- **The geo tables take 28%**, the call queue alone 7%.
- **Repeats:** in PLT.38.5 one `PlayAreaCell` query ran 161 times and two others 119 times each,
  in PLT.41.4 and PLT.38.3 over 100: candidates for an N+1. `settings.setting` was read 651 times
  over the run.

## What replay would meet in Tomorrow

- **Background work in the host.** The call queue's workers run in the hosted studio and query
  beside the test; replay needs them recorded with the test or kept out.
- **Shared tables.** With the fan-out above, a schema change to the account, settings, queue or
  geo tables re-records most of the suite.
- **App locks and `OPENJSON` parameters**, which replay has to answer like any other command.

## What it took

Three changes in Bruinell, all for real suites: the capture takes a database pattern
(`--database-like`), since a suite makes a database per run; the clock marker passes the
pattern's filter; and the report names a window by its scenario, keeping the innermost, since
every scenario of a suite runs in the one xUnit method.

It also found that the studio suite left a database behind every run: its drop runs on
`ProcessExit`, which the test host cuts short, and the sweep's list of prefixes didn't have the
studio's. 455 were dropped by hand (212 on `.\MSSQLSERVER01`, 243 on the default instance from
before the move), and Tomorrow's sweep now takes any `Tomorrow…Test` prefix (game `698fed2d`).
