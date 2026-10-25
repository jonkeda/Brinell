# SQL replay spike: results

The spike of [plan.md](plan.md), step 0. In progress: the spike app and its suite (0.1, 0.2), Todo.Web
on bUnit (0.3), the measurement (0.4), replay with no database (P2), 20 replays on bUnit (P3a) and
churn (0.5, P4) are done. The browser's determinism (P3b) is not started.

## The outcome so far

P4 fails, and it's the result that matters most. Of ten ordinary changes, eight needed every
recording re-recorded: four through the stamp (migrations, an upgrade), four through misses that
reached every test. Only an `AsNoTracking` and a different kind of GUID left the recordings alone.
Every message named its cause, so the failure is the cost, not confusion. Two things make the cost
worse than it has to be: EF orders a query's columns by property name, so renaming a property
alone breaks every query on its entity; and on bUnit every test runs the start page's query first,
so any change to that query touches every recording.


P2 and P3a pass. All five scenarios replay on bUnit with the connection string pointing at a
server that doesn't exist, the unique-key error and the spatial distance included, and 20 replay
runs in a row gave no miss. Replay runs the five tests in 239 ms against 1,245 ms on the real
database with its resets. Two of the review's three risks held up as real, and both have a
workaround: EF's SQL Server spatial mapping casts to `SqlDataReader`, and `SqlException` has to be
built by reflection.


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

## Replay with no database (P2)

The spike's replay is an EF interceptor over a session per test and head
(`Recordings/bunit/<test>.json`, kept in git, 10 to 16 KB each). In record mode each command runs,
its results are read whole and the app reads them back from a `ReplayDataReader`, as it will in
replay. In replay the connection open is suppressed and every command is answered by the session.

| Check | Result |
| --- | --- |
| Five scenarios, server `127.0.0.1,1` | Pass |
| Duplicate name | `SqlException` 2601 rebuilt by reflection; the page's `catch ... when Number is 2601 or 2627` runs |
| Distance | Geography bytes recorded and read back; the distance shows |
| A recorded parameter changed by hand | A miss naming the command, the live parameters and the nearest recorded one |

What it took, beyond the matcher:

- **EF's spatial mapping casts to `SqlDataReader`.** The first record run failed in every test
  with `InvalidCastException` from the query's shaper. The fix swaps SQL Server's NetTopologySuite
  type-mapping plugin (`ReplaceService`) for one whose geometry mapping reads with
  `DbDataReader.GetFieldValue<SqlBytes>`; SQL Server's own converter does the rest. It uses EF's
  internal API (`EF1001`) and reflection for the mapping's parameters, so an EF upgrade may break it.
- **`SqlException` has no public constructor.** `SqlErrors` builds one through SqlClient's internal
  `SqlError`, `SqlErrorCollection` and `SqlException.CreateException`, filling parameters by name.
  Same risk on a SqlClient upgrade.
- **Recording twice finds the volatile values.** The second record run marked exactly the
  app-made values: the insert's `@p0` (the new `Id`) and `@p1` (`CreatedAt`), and `@Id` in the
  two reads after it. Binding carried the live `Id` through those reads and into their rows.
- **Test data has fixed ids.** The seed now uses fixed GUIDs, so only what the app makes differs
  between runs.

## Determinism on bUnit (P3a)

20 replay runs in a row of the five scenarios: 100 passes, no miss, about 1 s per run.

| Test | Real database (test + reset) | Replay |
| --- | --- | --- |
| ListShowsSeededPlaces | 88 + 704 ms (first: EF and the pool warming up) | 92 ms |
| AddDuplicateNameIsRefused | 39 + 25 ms | 84 ms |
| AddPlaceShowsIt | 139 + 69 ms | 43 ms |
| DistanceIsShown | 114 + 30 ms | 4 ms |
| FilterByKind | 8 + 30 ms | 16 ms |
| All five | 1,245 ms | 239 ms |

One real-database run failed on the way: `FilterByKind` timed out once in 9 runs. The likely cause
is a race in the sample, not in replay: the page object took the heading as "loaded" before the
first query was back, so a slow first load could overwrite the filtered list. The list now renders
only once loaded, and the page object waits for it; 6 runs on each head passed after. Replay never
showed the race because its answers come at once, which is also a warning: replay can hide a race
the real database would show.

## Churn (0.5, P4)

The recordings made at a base commit, then ten changes each made on its own on the branch
`spike/sql-replay-churn` (a commit and its revert per change) and the five scenarios replayed with
no database. Two things were added to the spike first: a stamp on each recording (the migrations'
ids hashed, EF Core's and SqlClient's versions), checked before any command; and a miss message
that names where the text parts from the nearest recorded command.

| # | Change | Expected | Got | Recordings hit |
| --- | --- | --- | --- | --- |
| 1 | Nullable column `Notes`, migration | Stale | Stale: migrations | 5 |
| 2 | `AsNoTracking` on the list | No miss | No miss | 0 |
| 3 | `Include` of a new `Region`, migration | Miss on the list | Stale: migrations | 5 |
| 4 | Property `Kind` renamed, column kept | No miss | Miss: column order | 5 |
| 5 | Column renamed too, migration | Stale | Stale: migrations | 5 |
| 6 | List ordered by `CreatedAt` | Miss on the list | Miss: `ORDER BY` | 5 |
| 7 | `==` chain for `Contains` | Miss on the list | Miss: constants in `IN` | 5 |
| 8 | EF 10.0.12 to 10.0.13 | Stale | Stale: EF and SqlClient (6.1.6 to 6.1.7) | 5 |
| 9 | Count badge, own `CountAsync` | Miss naming it | Miss: `SELECT COUNT(*)` | 5 |
| 10 | `Guid.CreateVersion7()` | No miss | No miss: bound | 0 |

Two changes gave another outcome than the plan expected:

- **Change 3 is stale, not a miss.** A new entity needs a migration, so the stamp speaks first; the
  plan's expectation was wrong, not replay.
- **Change 4 misses everywhere.** EF projects columns in property-name order, so after the rename
  `[Kind]` comes before `[CreatedAt]` in every query on `Place`. The schema and the behaviour are the
  same; the recordings aren't. This is the failure of P4.

Replayed with the stamp ignored (`BRINELL_SQL_STAMP=ignore`), changes 1, 3 and 5 each miss on the
first query that loads a `Place`: the new column, join or name is in every projection. So the
stamp hides nothing that would have survived; it only says it sooner and in one line.

Why every change reaches every recording: on bUnit each test's first page load renders the start
address (`/`), so every recording begins with the list query, even `DistanceIsShown`, which never
opens the list. Five of the eight re-records come down to that one query.

Also found on the way: recording twice failed once in 16 runs, because the start page's query and
the detail page's queries came in another order. Replay already took the reads between two writes
as a set; the pairing of the two recordings now does the same.

## After the two fixes

Both are built. The bUnit head renders only the page a navigation opens, and `StartBlank` opens
on an empty page until the first navigation, as a browser context opens on `about:blank`; the
spike and Todo.Web's bUnit head use it. The matcher keys a `SELECT` by its columns as a set and
an `INSERT` by its columns as a set with each parameter matched by the column it fills; replay
gives the recorded columns back in the order the live query asks for. Each recording now holds
only what its test does: `DistanceIsShown` has three commands, no list query.

| # | Change | Before | After |
| --- | --- | --- | --- |
| 4 | Property renamed, column kept | Miss, 5 | No miss, 0 |
| 6 | List ordered by `CreatedAt` | Miss, 5 | Miss, 4 (the tests that open the list) |
| 7 | `==` chain for `Contains` | Miss, 5 | Miss, 4 |
| 9 | Count badge | Miss, 5 | Miss, 4 |
| 1 | New column, stamp ignored | Miss, 5 | Miss, 5 (every query on `Place` selects it) |

Changes 2 and 10 left nothing to fix; the stale ones (1, 3, 5, 8) are the stamp's and unchanged.
The base replays 20 times without a miss, and the suites on the bUnit head pass (Brinell's Blazor
suite 35, Todo.Web 10, the bUnit head's own tests 97 with three new ones).

What's left is the honest cost: a change to a query hits the tests that run it, and a new
column hits every test on its table, because EF selects all columns. On a large schema with
hand-written SQL, both reach far fewer tests than here, where every test reads the one table.

## The stamp per table

The stamp no longer hashes the migrations. Each recording lists the tables its commands touched
(read from `FROM`, `JOIN`, `INTO`, `UPDATE` and `MERGE`), each with a hash of that table's columns,
keys, indexes and foreign keys taken from the EF model, so replay still needs no database. EF's
and SqlClient's versions stay in the stamp for every recording. The sample has a second table,
`Visits`, which only the detail page reads: the two detail tests list `dbo.places` and
`dbo.visits`, the three list tests only `dbo.places`.

| Change, with its migration | Stale |
| --- | --- |
| A column on `Places` | 5 of 5: "table dbo.places changed" |
| A new table `Tags`, read by nothing | 0 of 5: all replay |
| A column on `Visits` | 2 of 5, the detail tests: "table dbo.visits changed" |

Before, each of the three made all five stale. On a schema of 2000 tables this is the
difference that counts: a migration stales only the tests that touch its tables.

What it doesn't cover yet:

- **Stored procedures and views name no table** in the command text. Their tables would come
  from the database's dependencies (`sys.sql_expression_dependencies`), read when recording.
- **A system without EF** has no model to hash. Its schema would come from a checked-in snapshot
  (a SQL project, or a schema file written at recording), hashed the same way per table.
- **Data changed by a migration** (seed rows) isn't in the hash: the test's own data is the
  fixture's, but reference data a migration writes would replay as it was recorded.

## What P4 changes before step 2

- **Match a query's columns as a set.** Compare `SELECT` lists without their order and give the
  recorded columns back in the order asked for. That takes change 4's kind of miss away, and needs
  a light parse of the projection.
- **No start-page render on bUnit.** The bUnit context renders its root at the start address before
  the first navigation; skipping that render when a navigation follows at once takes the list
  query out of every recording, and helps the bUnit head's speed too.
- **Say the cost plainly in the proposal.** With those two, changes 4 and 6 would hit one or two
  recordings instead of five; but migrations and upgrades still re-record everything. Replay pays
  where the schema is quiet and the database is expensive to have, not where the model changes
  every week.
