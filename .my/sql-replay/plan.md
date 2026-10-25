# SQL replay: development plan

Agent document. Builds [proposal.md](proposal.md). Status: step 0 in progress: 0.1 to 0.4 done (P1: 4.9% for queries, 37% with the reset, on the spike
app; see [spike-results.md](spike-results.md)); next the spike's replay (P2, P3a). Steps 1 to
8 are an outline, detailed after the spike reports. Each step ends green:
`dotnet build Brinell.sln -p:NuGetAudit=false`, `dotnet build samples/Todo/Brinell.Samples.Todo.slnx`
when Todo is touched, the step's own tests, and the suites it touches on every head it touches.

## Fixed points

| What | Value |
| --- | --- |
| Target framework, runner | `net10.0`; xUnit 2.9.3 |
| EF Core | `Microsoft.EntityFrameworkCore` 10.0.12 (root `Directory.Packages.props`); add `Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite` at 10.0.12 |
| SQLite | `Microsoft.Data.Sqlite` 10.0.12 |
| bUnit head | `srcnew/Brinell.Html.Bunit`: `BunitTestContext(BunitTestContextOptions)` with `RootComponent`, `ConfigureServices`, `FallbackServices`, `User`, `Roles`, `JsInterop`; example `testsnew/Brinell.Blazor.UITests/TestBase/BlazorSampleBunit.cs` |
| Head switch | `Brinell.Core.Testing.WebHeads`: `BRINELL_HEAD=bunit`, else the browser |
| Replay switch | `BRINELL_SQL_REPLAY` = `Off` (default) \| `Record` \| `Replay` |
| SQL Server | Local instance; `BRINELL_SQL_CONNECTION`, default `Server=.;Database=BrinellSqlSpike;Integrated Security=True;TrustServerCertificate=True`; never LocalDB |
| Todo.Web | Blazor Interactive Server; composition `AddTodoServices(settings)` plus `WebNavigator`, `WebDialogs`; root `Components/Routes.razor`; data `TodoDatabase` (returns `SqliteConnection`), `SqliteTodoRepository`, `SqliteUserRepository` |
| Todo.Web suite | `samples/Todo/tests/Brinell.Samples.Todo.Web.UITests`: `TodoWebFixture` (seeded SQLite file, `MockApiServer` for `Todo.Api`, a pinned clock), host as its own process through `WebAppHost`; `Tests/JourneyTests.cs` 10 tests; `Live/` skipped without the live API |
| Not changed in step 0 | `srcnew/Brinell.Mocking`, every package's public API; the spike's replay code lives in the spike's test project |

## Step 0: the spike

It tries to disprove the proposal's three risks (EF on SQL Server, timing-dependent traffic,
churn) and measures whether replay is worth building. Its replay code is spike quality and is
thrown away by step 2; the app and the bUnit head of Todo.Web stay.

### 0.1 The spike app

| Item | Value |
| --- | --- |
| Project | `samples/Brinell.Samples.Sql.App` (Blazor Interactive Server, prerendering on) |
| Data | `SpikeDbContext` on `UseSqlServer(connection, o => o.UseNetTopologySuite())`; migrations in the project; database created by `Database.MigrateAsync()` at start |
| Entity `Place` | `Id` `Guid` (`Guid.NewGuid()` in the app, not the database), `Name` `nvarchar(100)` with a unique index, `Kind` `nvarchar(30)`, `Location` `geography` point, `Population` `decimal(12,2)`, `CreatedAt` `datetimeoffset` from `TimeProvider.System` |
| Seed | 5 places, written by the fixture before a run, not by the app |
| Page `/places` | The list: `Kinds` filter (a `Contains` over the chosen kinds), ordered by `Name`; loads in `OnInitializedAsync` |
| Page `/places/new` | A form; inserts a place; on a unique-key violation catches `DbUpdateException` whose inner `SqlException.Number` is 2601 or 2627 and shows "That name is taken"; on success navigates to `/places/{id}` |
| Page `/places/{id}` | The place read back by `Id`, with its distance to the first seeded place (`Location.Distance`) |
| Test ids | `data-testid` on every control, as the Blazor sample |

### 0.2 The spike's tests

| Item | Value |
| --- | --- |
| Project | `testsnew/Brinell.Sql.Spike.Tests` |
| Page objects | `PlacesPage`, `NewPlacePage`, `PlacePage` over `IHtmlTestContext` |
| Fixture | `SpikeFixture`: seeds the database, makes the context by head: bUnit (`BunitTestContext` over the app's `Routes`, the app's services registered in `ConfigureServices`) or browser (`WebAppHost` over the app's `.csproj`, a host per test in replay) |
| Scenarios | `ListShowsSeededPlaces`, `FilterByKind`, `AddPlaceShowsIt` (insert, then the detail page by the new `Id`), `AddDuplicateNameIsRefused` (the `SqlException` path), `DistanceIsShown` (spatial) |
| Spike replay | `Spike/ReplayInterceptor.cs` (`DbConnectionInterceptor` suppressing the open, `DbCommandInterceptor` answering reader, non-query and scalar with `SuppressWithResult`, errors rethrown as `SqlException` by reflection), `Spike/Recording.cs` (JSON per test and head, `Recordings/<head>/<test>.json`), `Spike/Matcher.cs` (occurrence keys, writes as barriers, values bound from two recordings) |
| Command log | `Spike/CommandLog.cs`: every command's text, parameters, start and duration, written to `TestResults/sql-log/<head>/<test>.jsonl`, on in every mode |

### 0.3 Todo.Web's bUnit head

The database's share only means something on the head replay would run on, and Todo.Web's host is
its own process. So Todo.Web.UITests gets a bUnit head as `Brinell.Blazor.UITests` has one.

- `TodoWebFixture` makes the device's context by `HtmlHeads`: on bUnit, a `BunitTestContext` over
  Todo.Web's `Routes` with `AddTodoServices(settings)`, `WebNavigator` and `WebDialogs` registered,
  the same seeded SQLite file, `MockApiServer` and pinned clock.
- A test that needs the browser or two hosts (the two-device scenarios) is tagged `@browser` or
  skipped on bUnit with that reason.
- Time box: one day. If the journey tests don't run on bUnit within it, the measurement uses the
  spike app alone and `spike-results.md` says why.

### 0.4 The measurement

| Item | Value |
| --- | --- |
| Database time, Todo | `Brinell.Samples.Todo.Web.UITests/Spike/DatabaseClock.cs` (`TimedTodoRepository`, `TimedUserRepository` over the real repositories in the bUnit head's composition), `Spike/DatabaseShare.cs` (`BRINELL_DB_SHARE` folder, `[MeasureDatabaseShare]`, run with `-- xUnit.ParallelizeTestCollections=false`); plus the fixture's seed |
| Database time, spike app | The command log's durations, plus the seed |
| Test time | A `Stopwatch` per test in the fixture, from the test's start to its end |
| Runs | Each suite on the bUnit head 3 times; the median per test |
| Result | Per test: database ms, test ms, share. Per suite: the median share |

### 0.5 Churn: the ten changes

Recorded once on the bUnit head at the base commit, then each change made as its own commit on a
branch `spike/sql-replay-churn` and the five scenarios replayed. Expected outcome per change:

| # | Change | Expected |
| --- | --- | --- |
| 1 | A nullable column `Notes` on `Place`, with its migration | Stale, by the migrations hash, before any miss |
| 2 | `.AsNoTracking()` on the list query | No miss |
| 3 | `.Include` of a new `Region` (seeded) on the list query | Misses on the list query only, naming it |
| 4 | Property `Kind` renamed `Category`, column kept with `HasColumnName` | No miss |
| 5 | Column renamed too, with its migration | Stale, by the migrations hash |
| 6 | The list ordered by `CreatedAt` instead of `Name` | Misses on the list query only, naming the `ORDER BY` |
| 7 | The `Kinds` filter as an `==` chain instead of `Contains` | Misses on the list query, naming the parameters |
| 8 | The EF Core packages moved to another 10.0.x patch | Stale, by the EF version, or no miss if the SQL is unchanged |
| 9 | A count badge on `/places` querying `CountAsync` | Misses naming the new command |
| 10 | `Id` from `Guid.CreateVersion7()` instead of `Guid.NewGuid()` | No miss: the value is bound |

Measured per change: misses, recordings touched, and whether the first message names the cause
(the stamp's part, or the command and the part that differed). A miss in a command the change
didn't touch counts as a confusing miss.

### 0.6 Pass lines

| # | Check | Passes when | If it fails |
| --- | --- | --- | --- |
| P1 | Database share | Median share on the bUnit head at least 15% in Todo.Web or the spike app | Only the log is built: steps 1, 5 and 8; replay stays on the shelf |
| P2 | EF on SQL Server | All five scenarios replay on bUnit with `BRINELL_SQL_CONNECTION` pointing at a server that doesn't exist; the duplicate name shows "That name is taken"; the distance shows | The EF adapter's design changes before step 2 |
| P3a | Determinism, bUnit | 20 replays in a row of the five scenarios: 0 misses | The matcher's design changes before step 2 |
| P3b | Determinism, browser | 20 replays with a host per test: at most 1 miss | Step 7 is dropped |
| P4 | Churn | Every change gives its expected outcome; no confusing miss | The stamp, keys or summary change before step 2 |

### 0.7 The report

`.my/sql-replay/spike-results.md` (human document): the numbers per pass line, what failed and
why, and the changes to the proposal and to steps 1 to 8. The proposal is amended in the same
commit.

## Steps 1 to 8: outline

Detailed after the spike, from `spike-results.md`.

| Step | What | Projects |
| --- | --- | --- |
| 1 | The log: ADO.NET and EF adapters in record-only mode; counts, N+1, SQL snapshot assertions; Todo moves to `DbDataSource` | `srcnew/Brinell.Mocking.Data`, `srcnew/Brinell.Mocking.Data.EntityFrameworkCore`, their tests, Todo |
| 2 | Replay core: occurrence keys, write barriers, SQLite and SqlClient rules, errors, the stamp, the miss message | the same |
| 3 | Value binding, volatile values found by recording twice | the same |
| 4 | The bUnit head: replay in the context's container, recordings per head, the shared startup recording | the same, `Brinell.Html.Bunit` if it needs a hook |
| 5 | Maintenance: the re-record summary, the check on unread re-records | the same, a check in the build |
| 6 | Todo's suites and the spike app's suite on replay on bUnit, measured against their real-database runs | Todo, `samples/Brinell.Samples.Sql.App` and its suite |
| 7 | Browser scoping, if P3b passed: a host per test, the test-id header | `Brinell.Html.Playwright` fixtures |
| 8 | `docs/guides/sql-replay.md` and an AD on when a test replays and when it needs the database | `docs/`, `.docs/decisions/` |
