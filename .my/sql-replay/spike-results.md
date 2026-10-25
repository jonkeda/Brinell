# SQL replay spike: results

The spike of [plan.md](plan.md), step 0. In progress: the spike app and its suite (0.1, 0.2), Todo.Web
on bUnit (0.3) and the measurement on both (0.4) are done. Replay itself (P2, P3), churn (0.5) and
the browser's determinism are not started.

## The outcome so far

P1 passes on the spike app, but only because of the reset. The app's own queries are a 4.9% median
share on SQL Server, about what they were on Todo's SQLite (6%). Writing the five places afresh
before each test brings it to 37%. Replay takes both away, since there is no database to reset;
but a cheaper reset (a transaction rolled back, Respawn, a restored template) would also take most
of the second part away, without replay. The proposal should say this plainly: on the bUnit head
replay's gain is "no database to reset or to have", not faster queries.

## Todo.Web on the bUnit head (0.3)

Todo.Web's web suite now runs on either head (`BRINELL_HEAD=bunit`). On bUnit the app's own
composition is the fallback of the context, so its singletons outlive page loads as they do in the
host. The ten journey tests pass on both heads: about 1 s on bUnit, 12 s in the browser.

Two gaps in the bUnit head showed up and are fixed in `Brinell.Html.Bunit`:

- **The page's own `history.back()`.** bUnit's loose mode answered it with nothing, so an app's
  back button stayed put. The context now keeps the circuit's history and goes back in it.
- **An app's own busy script.** Todo.Web says it's busy through `brinellTodo.setBusy`, not
  Brinell's `brinellBusy.set`. `BunitTestContextOptions.BusyScript` names it.

No test needed `@browser`. The two-device scenarios live in other suites and weren't touched.

## The database's share on Todo.Web (0.4)

Measured with timing decorators over the two repositories, three runs one test at a time, the
median per test.

| What | Value |
| --- | --- |
| Median share per test | 6.0% |
| All tests together | 100 ms of database time in 1,681 ms |
| Highest | 50% (`Next_CyclesOpen_InProgress_Done_Open`, 25 ms of 49 ms) |
| Seeding per fixture | 14 ms (empty), 40 ms (three todos) |

So on Todo.Web, P1 fails: replay would save about 6% of the bUnit run. That's because Todo's
database is a local SQLite file, where a call takes about 1 ms. The decision now rests on the
spike app, on SQL Server, where each call crosses a connection.

One finding beside the database: three tests take about 470 ms on bUnit where the others take
10 to 50 ms (the sync, the discard dialog, delete). Most of that is neither the database nor
rendering; it looks like a wait in the page objects or the view models, and is worth a look for
the bUnit head's speed on its own.

## The spike app and its suite (0.1, 0.2)

`samples/Brinell.Samples.Sql.App`: Blazor Interactive Server with prerendering, EF Core 10.0.12 on
the local SQL Server (`BrinellSqlSpike`), migration `Initial`, the three pages of the plan.
`testsnew/Brinell.Sql.Spike.Tests` runs its five scenarios on either head; the bUnit head renders
its `Routes` with its data composition and `CommandLog` as EF interceptors. All five pass on both:
about 1 s on bUnit, 20 s in the browser. The unique-key error reaches the page as `SqlException`
2601 and the distance comes from `geography` on both heads.

## The database's share on the spike app (0.4)

Three bUnit runs one test at a time, the median per test. Database time is the command log's
durations (readers until disposed) plus connection opens; the reset is the migration check, a
delete and five inserts before the test.

| What | Value |
| --- | --- |
| Median share, queries only | 4.9% |
| Median share, with the reset | 37% |
| Queries, all five tests | 19 ms in 387 ms |
| Reset per test | 19 to 57 ms; 780 ms for the run's first (EF and the pool warming up) |

## What the command log showed

Four things the replay design has to take into account, seen in the logs:

- **A bUnit page load renders the start address first.** Opening `/` ran the list query twice:
  once for the root's first render at `http://localhost/`, once for the navigation. So the bUnit
  head has a double load of its own, as the browser has prerendering, and recordings per head stay
  right. The bUnit context could skip the first render when a navigation follows at once.
- **EF 10 expands `Contains` into one parameter per value** (`@_kinds1`, `@_kinds2`), not a JSON
  parameter. The number of parameters follows the data, so a key can't assume a fixed parameter
  list.
- **The spatial parameter is `SqlBytes`**, not a CLR value: the recording has to store its bytes,
  and replay has to give them back as the provider expects.
- **`DateTimeOffset` comes from the app's clock** (`CreatedAt`), so the insert's parameters differ
  every run: a value to bind, beside the new `Id`.
