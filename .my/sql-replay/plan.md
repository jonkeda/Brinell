# SQL replay: development plan

Agent document. Builds [proposal.md](proposal.md). Status: step 0 done: P2, P3a, P3b pass; P4 failed and was answered (columns as a set, no start-page
render, the stamp per object from a schema snapshot); see [spike-results.md](spike-results.md). The proposal is amended for a
legacy system; steps 1 to 6 below are its outline, detailed one at a time. P5, parallel runs, passes (added after
step 0). Step 1, the log, is built and L1 to L5 pass (L4 in part: see [log-results.md](log-results.md)). Step 2, replay in
the package with parallel runs, is built and R1 to R5 pass ([replay-results.md](replay-results.md)). Step 3, the
snapshot's gaps and `bruinell sql stale`, is built and S1 to S6 pass ([stamp-results.md](stamp-results.md)).
Step 4, the heads, adds the test-id hook for an app in a process of its own; H1 to H6 pass, H6 without IIS
([hook-results.md](hook-results.md)). 1.7 waits until the owner has the legacy system; next: step 5. Each step ends green:
`dotnet build Bruinell.sln -p:NuGetAudit=false`, `dotnet build samples/Todo/Bruinell.Samples.Todo.slnx`
when Todo is touched, the step's own tests, and the suites it touches on every head it touches.

## Fixed points

| What | Value |
| --- | --- |
| Target framework, runner | `net10.0`; xUnit 2.9.3 |
| EF Core | `Microsoft.EntityFrameworkCore` 10.0.12 (root `Directory.Packages.props`); add `Microsoft.EntityFrameworkCore.SqlServer` and `Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite` at 10.0.12 |
| SQLite | `Microsoft.Data.Sqlite` 10.0.12 |
| bUnit head | `srcnew/Bruinell.Html.Bunit`: `BunitTestContext(BunitTestContextOptions)` with `RootComponent`, `ConfigureServices`, `FallbackServices`, `User`, `Roles`, `JsInterop`; example `testsnew/Bruinell.Blazor.UITests/TestBase/BlazorSampleBunit.cs` |
| Head switch | `Bruinell.Core.Testing.WebHeads`: `BRUINELL_HEAD=bunit`, else the browser |
| Replay switch | `BRUINELL_SQL_REPLAY` = `Off` (default) \| `Record` \| `Replay` |
| SQL Server | Local instance; `BRUINELL_SQL_CONNECTION`, default `Server=.;Database=BruinellSqlSpike;Integrated Security=True;TrustServerCertificate=True`; never LocalDB |
| Todo.Web | Blazor Interactive Server; composition `AddTodoServices(settings)` plus `WebNavigator`, `WebDialogs`; root `Components/Routes.razor`; data `TodoDatabase` (returns `SqliteConnection`), `SqliteTodoRepository`, `SqliteUserRepository` |
| Todo.Web suite | `samples/Todo/tests/Bruinell.Samples.Todo.Web.UITests`: `TodoWebFixture` (seeded SQLite file, `MockApiServer` for `Todo.Api`, a pinned clock), host as its own process through `WebAppHost`; `Tests/JourneyTests.cs` 10 tests; `Live/` skipped without the live API |
| Not changed in step 0 | `srcnew/Bruinell.Mocking`, every package's public API; the spike's replay code lives in the spike's test project |

## Step 0: the spike

It tries to disprove the proposal's three risks (EF on SQL Server, timing-dependent traffic,
churn) and measures whether replay is worth building. Its replay code is spike quality and is
thrown away by step 2; the app and the bUnit head of Todo.Web stay.

### 0.1 The spike app

| Item | Value |
| --- | --- |
| Project | `samples/Bruinell.Samples.Sql.App` (Blazor Interactive Server, prerendering on) |
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
| Project | `testsnew/Bruinell.Sql.Spike.Tests` |
| Page objects | `PlacesPage`, `NewPlacePage`, `PlacePage` over `IHtmlTestContext` |
| Fixture | `SpikeFixture`: seeds the database, makes the context by head: bUnit (`BunitTestContext` over the app's `Routes`, the app's services registered in `ConfigureServices`) or browser (`WebAppHost` over the app's `.csproj`, a host per test in replay) |
| Scenarios | `ListShowsSeededPlaces`, `FilterByKind`, `AddPlaceShowsIt` (insert, then the detail page by the new `Id`), `AddDuplicateNameIsRefused` (the `SqlException` path), `DistanceIsShown` (spatial) |
| Spike replay | `Spike/ReplayInterceptor.cs` (suppresses the open, answers reader, non-query and scalar with `SuppressWithResult`), `Spike/SqlErrors.cs` (`SqlException` by reflection), `Spike/SpatialReplay.cs` (the geometry mapping read through `DbDataReader`), `Spike/Recording.cs` (JSON per test and head, `Recordings/<head>/<test>.json`), `Spike/ReplaySession.cs` (occurrence keys, writes as barriers, values bound from two recordings), `Spike/ReplayDataReader.cs`; `BRUINELL_SQL_REPLAY=Record` twice, then `Replay` |
| Command log | `Spike/CommandLog.cs`: every command's text, parameters, start and duration, written to `TestResults/sql-log/<head>/<test>.jsonl`, on in every mode |

### 0.3 Todo.Web's bUnit head

The database's share only means something on the head replay would run on, and Todo.Web's host is
its own process. So Todo.Web.UITests gets a bUnit head as `Bruinell.Blazor.UITests` has one.

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
| Database time, Todo | `Bruinell.Samples.Todo.Web.UITests/Spike/DatabaseClock.cs` (`TimedTodoRepository`, `TimedUserRepository` over the real repositories in the bUnit head's composition), `Spike/DatabaseShare.cs` (`BRUINELL_DB_SHARE` folder, `[MeasureDatabaseShare]`, run with `-- xUnit.ParallelizeTestCollections=false`); plus the fixture's seed |
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
| P2 | EF on SQL Server | All five scenarios replay on bUnit with `BRUINELL_SQL_CONNECTION` pointing at a server that doesn't exist; the duplicate name shows "That name is taken"; the distance shows | The EF adapter's design changes before step 2 |
| P3a | Determinism, bUnit | 20 replays in a row of the five scenarios: 0 misses | The matcher's design changes before step 2 |
| P3b | Determinism, browser | 20 replays with a host per test: at most 1 miss | Step 7 is dropped |
| P4 | Churn | Every change gives its expected outcome; no confusing miss | The stamp, keys or summary change before step 2 |
| P5 | Parallel runs | No statics; the scenarios in several classes; 20 parallel replays on each head, 0 misses; parallel recording on a database per worker gives the recordings a serial one gives; EF's too-many-service-providers warning never raised | The session's scoping or the recording's isolation changes before step 2 |

### 0.7 The report

`.my/sql-replay/spike-results.md` (human document): the numbers per pass line, what failed and
why, and the changes to the proposal and to steps 1 to 8. The proposal is amended in the same
commit.

## Step 1: the log

The log records every command a test sends, with its time, without changing what the app does,
and reports what replay would be worth: the database's share of each test, and how many tests
each table, view and procedure reaches. Its first use is measuring the legacy system. It records
no parameter values and no rows, only their shape, so a log from a database with real data is
safe to keep and share.

### 1.1 Fixed points

| What | Value |
| --- | --- |
| Test scope | `Bruinell.Core.Testing.TestScope` (`AsyncLocal`): `Test`, `Scenario`, `Step`; set by the runners |
| Timings | `Bruinell.Core.Logging.Timing`, `BRUINELL_TIMINGS` folder, `timings-<run>.csv`: `Run;Head;Test;Scenario;Step;Kind;Name;Page;Control;Action;StartMs;DurationMs;Attempts;Result` |
| Touch map | `tools/Bruinell.Cli/Selection/TouchMap.cs`, `touch-map.json` beside a suite; the `bruinell tests` commands |
| Spike code reused | `Statements` (normalize, key, objects) and `SchemaSnapshot` from `testsnew/Bruinell.Sql.Spike.Tests/Spike/`; its `CommandLog` is replaced |
| Log variable | `BRUINELL_SQL_LOG`: a folder; unset, nothing is logged and nothing is wrapped |
| Legacy reach | The ADO.NET part targets `netstandard2.0`, so a .NET Framework app can load it; no dependency on `Bruinell.Core` there |

### 1.2 Three sources, one log

| Source | Where it runs | For | Test of a command |
| --- | --- | --- | --- |
| EF interceptor | The app's process | EF Core apps (net8.0 and later) | `SqlLogScope.Current`, from `TestScope` in-process |
| ADO.NET wrapper | The app's process | Raw ADO.NET, Dapper, `System.Data.SqlClient` or `Microsoft.Data.SqlClient`, .NET Framework too | The same in-process; by time out of process |
| Extended Events | SQL Server | Any app, unchanged, in any process | By time: joined to the timings' test spans |

Extended Events is the legacy system's source: nothing in the app changes. A session on the test
database records `rpc_completed` and `sql_batch_completed` with their duration, row count, object
name and text; `bruinell sql capture` starts, stops and reads it. Commands are given to tests by
time: a test owns the commands that ended inside its span in the timings. That needs the measured
run to be serial (`-- xUnit.ParallelizeTestCollections=false`) and the two clocks to agree: at
start the CLI sends a marker query (`SELECT 'bruinell-marker-<guid>'`) and reads its server
timestamp, which gives the offset.

### 1.3 The log format

One JSON line per command in `<BRUINELL_SQL_LOG>/sql-<run>-<pid>.jsonl` (in-process) or
`sql-xe-<run>.jsonl` (read from Extended Events):

| Field | What |
| --- | --- |
| `run`, `test`, `scenario` | The run and the test scope; `test` null when none is known yet (joined later for XE) |
| `start`, `ms` | UTC start, to the millisecond; duration, a reader's until it is disposed |
| `kind` | `reader`, `non-query`, `scalar`, `error`, `open` |
| `text` | The normalized text; a stored procedure as its `EXEC`; `sp_executesql`'s inner statement for XE |
| `parameters` | Names only |
| `rows`, `affected`, `error` | Rows read, rows affected, the error number |
| `objects` | The objects the text names (`Statements.Objects`) |

### 1.4 Projects

| Project | Targets | Holds |
| --- | --- | --- |
| `srcnew/Bruinell.Mocking.Data` | `netstandard2.0;net8.0;net9.0;net10.0` | `SqlLog` (writer, thread-safe), `SqlLogEntry`, `SqlLogScope` (`Func<string?>` for the test, set by the suite), `LoggingDbConnection`, `LoggingDbCommand`, `LoggingDbDataReader`, `LoggingDbProviderFactory`, `Statements`, `SchemaSnapshot` (over any `DbConnection`), `ExtendedEvents` (the session's SQL; reading through `sys.fn_xe_file_target_read_file`) |
| `srcnew/Bruinell.Mocking.Data.EntityFrameworkCore` | `net10.0` (EF Core 10) | `SqlLogInterceptor` (command and connection), `UseSqlLog(this DbContextOptionsBuilder, SqlLog)` |
| `testsnew/Bruinell.Mocking.Data.Tests` | `net10.0` | Unit tests; wrapper tests on SQLite and on the local SQL Server; the XE tests in category `SqlServer`, skipped without one |
| `tools/Bruinell.Cli/Sql/` | as the CLI | `bruinell sql schema`, `bruinell sql capture start`, `stop`, `read`, `bruinell sql report` |

`SqlLogScope` is how the log learns the test without referencing `Bruinell.Core`: a suite sets
`SqlLogScope.Current = () => TestScope.Current.Test`; the Bruinell fixtures that host an app
in-process do it for the suite.

### 1.5 The commands

| Command | Does |
| --- | --- |
| `bruinell sql schema --connection <c> --out <schema.json>` | The schema snapshot: tables, views, procedures, functions, hashes, references. Read-only |
| `bruinell sql capture start --connection <c> --database <db> --folder <server folder>` | Creates and starts the session `bruinell_sql_<db>`, filtered to the database; sends the marker |
| `bruinell sql capture stop --connection <c> --database <db>` | Stops and drops the session; the `.xel` files stay |
| `bruinell sql capture read --connection <c> --files <server path> --out <folder>` | Reads the `.xel` files through the server into `sql-xe-<run>.jsonl`, the clock offset applied |
| `bruinell sql report --log <folder> --timings <folder> --schema <schema.json> --out <report.md>` | Joins commands to tests, expands objects through the schema's references, writes the report and `sql-touch-map.json` |

### 1.6 The report

`report.md` (a human document) and `report.json`:

- **Per test:** its time; the database's time inside it, split into arrange (the fixture's spans:
  reset and seed) and the test's own steps; the share of each; commands; distinct objects.
- **For the suite:** the median and p90 share, with and without the arrange part; the database's
  time in all.
- **Per object:** the tests that touch it, directly or through a view or procedure. The fan-out:
  for each object, the share of the suite's tests it reaches; its median, p90 and the top 20.
- **Repeats:** a command sent more than 10 times in one test with different parameters (an N+1),
  and one sent more than 3 times with the same ones (a read repeated between writes).
- **Not attributed:** commands outside every test's span (start-up, hosted services), counted.

`sql-touch-map.json` beside the suite maps each object to its tests, as `touch-map.json` does for
pages; selecting tests from a schema change reads it later.

### 1.7 Measuring the legacy system

What the owner runs, once, on a test database:

1. `bruinell sql schema` against the test database: the snapshot, read-only.
2. `bruinell sql capture start`: needs `ALTER ANY EVENT SESSION` on the server and a folder the
   server can write to.
3. The UI suite, serial, with `BRUINELL_TIMINGS` set, as it runs today.
4. `bruinell sql capture stop`, then `read`, then `bruinell sql report`.

What it answers, and the proposed threshold for each:

| Question | Read from | Replay pays when |
| --- | --- | --- |
| Is the database a large part of a test? | Median share with arrange | At least 15% |
| How much does a schema change cost? | Median fan-out per object | A typical object reaches at most 5% of the tests |
| How is the database made today? | Arrange time per test and per fixture | No threshold: it is the baseline |
| Does replay hold for it? | Repeats, errors, unattributed commands, procedures without references (dynamic SQL) | Reported as risks |

The numbers go to `.my/sql-replay/legacy-measure.md` by hand, without names of tables or data;
step 2 and later are decided from them.

### 1.8 Checks before step 1 is done

| # | Check | Passes when |
| --- | --- | --- |
| L1 | Wrapper and interceptor | Unit tests green; the spike suite logs through `UseSqlLog` (its `CommandLog` removed) and Todo through `LoggingDbConnection` at `TodoDatabase.OpenAsync`, on both heads, in parallel |
| L2 | Nothing changes | Every suite touched passes with `BRUINELL_SQL_LOG` set and unset |
| L3 | Extended Events against in-process | The spike suite run serially in the browser with the sample as its own process: the XE log gives each test the same distinct commands as the in-process log, prerender repeats aside |
| L4 | The report | On the spike suite: the shares within 2 points of spike-results (4.9%, 37%); the detail tests reach `dbo.visits` through `dbo.visitcount`; the fan-out table lists the five objects |
| L5 | .NET Framework | The wrapper's `netstandard2.0` build loads in a `net48` test project over `System.Data.SqlClient` (on Windows) |

## Step 2: replay in the package

The spike's replay moved into `Bruinell.Mocking.Data`, with an ADO.NET adapter beside the EF one,
and parallel runs a requirement: nothing of a test is static, and a test's session is found
through its container, its host or its async flow.

### 2.1 What moves, what is new

| Part | From | Where |
| --- | --- | --- |
| `Recording`, `Exchange`, `Values`, `Stamp`, `Schema` | spike `Spike/Recording.cs` | `srcnew/Bruinell.Mocking.Data/Replay/` (netstandard2.0 and up) |
| `ReplaySession` (occurrence keys, writes as barriers, reads answered again, binding, the miss message) | spike `Spike/ReplaySession.cs` | the same |
| `ReplayDataReader` | spike | the same |
| `Statements.Key`, `Projection`, `InsertParameters` | spike `Recording.cs` | `Statements.cs` |
| Provider errors and UDTs (`SqlException` by reflection, `SqliteException`, `GetSqlBytes`) | spike `SqlErrors.cs` | `Replay/ProviderRules.cs`, by reflection, no provider referenced |
| ADO.NET replay: `ReplayingDbConnection`, `ReplayingDbCommand` | new | `Replay/` |
| Output parameters and return values, recorded and given back | new | `Exchange.Outputs` |
| `ReplayScope` (the session of the async flow) and `ReplayingDbProviderFactory` | new | `Replay/` |
| EF replay: `ReplayInterceptor`, `UseSqlReplay(options, session)` | spike `ReplayInterceptor.cs` | `srcnew/Bruinell.Mocking.Data.EntityFrameworkCore` |
| SQL Server's spatial mapping for replay: `UseSpatialReplay()` | spike `SpatialReplay.cs` | new `srcnew/Bruinell.Mocking.Data.EntityFrameworkCore.SqlServer` (it references EF's NetTopologySuite plugin; AD-026: the dependency is the reason) |

### 2.2 Parallel runs

- **A session per test**, never static: a test makes its `ReplaySession` and hands it to the app
  through the app's composition (bUnit's container, a host per test) or the async flow.
- **`ReplayScope`**, an `AsyncLocal` session, for code that makes its connections itself:
  `ReplayingDbProviderFactory` wraps a provider factory and gives each connection the session of
  the flow that asked. Tests in parallel each enter their own scope.
- **Recordings** are a file per test and head; the schema snapshot is read-only; recording runs
  on a database per worker (P5).

### 2.3 Checks before step 2 is done

| # | Check | Passes when |
| --- | --- | --- |
| R1 | ADO.NET on SQLite | Readers, scalars, non-queries, several result sets, a transaction and a `SqliteException` recorded, then replayed with the database file gone |
| R2 | ADO.NET on SQL Server | A procedure's output parameter, return value and two result sets, a `geography` column and a `SqlException` 2627 recorded and replayed |
| R3 | EF | On SQLite through `UseSqlReplay`; the spike app on SQL Server through the package, spatial included |
| R4 | Parallel | 8 sessions at once through one `ReplayingDbProviderFactory`, each replaying its own recording with no cross-talk; the spike suite on the package, 20 parallel replays on each head without a miss |
| R5 | .NET Framework | The ADO.NET replay in the net48 test over `System.Data.SqlClient` |

## Step 3: the schema snapshot and the stamp

The snapshot (`SchemaSnapshot`, `bruinell sql schema`) closes the four gaps the proposal names, and
a command shows which recordings a schema change made stale, without replaying them.

### 3.1 Decisions

| Gap | Decision |
| --- | --- |
| Triggers | A trigger's body is a line of its table's hash; what the trigger's body references is added to its table's references (a write to `dbo.orders` touches the audit table its trigger fills). Defaults and check constraints are lines of their table too |
| Unqualified names in a body | Resolved by `sys.sql_expression_dependencies.referenced_id` to the object SQL Server bound; unresolved, the referencing object's own schema, then `dbo` |
| Dynamic SQL | A body with `sp_executesql` or `EXEC(` is marked `Dynamic`. Its references come from a hand-kept file, `--references <file.json>` (`{ "dbo.search": ["dbo.places"] }`), merged into the snapshot; `bruinell sql stale` warns about a dynamic object with none |
| Reference data | `--data <table,...>`: the listed tables' rows are hashed (count and `CHECKSUM_AGG(BINARY_CHECKSUM(*))`) into their table's hash, so changed lookup rows stale the recordings that read them. Not a shared recording: the stamp already does the job |
| Default schema | `Schema.DefaultSchema` (from `--default-schema`, kept in the snapshot, default `dbo`): a command's unqualified name is taken in it, then `dbo`, as SQL Server resolves it |

### 3.2 What is built

| Part | Where |
| --- | --- |
| `SchemaSnapshot.ReadAsync(connection, SnapshotOptions)`: triggers, defaults, checks, resolved references, `Dynamic`, data hashes; `DefaultSchema`, `Merge(references)` | `srcnew/Bruinell.Mocking.Data/SchemaSnapshot.cs` |
| `Schema.DefaultSchema`, `Schema.Named(text)` over `Statements.Names`; `ReplaySession.Finish` stamps through it | `Replay/Recording.cs`, `Replay/ReplaySession.cs` |
| `RecordingCheck.Check(folder, schema)`: each recording's differences, without replaying | `Replay/RecordingCheck.cs` |
| `bruinell sql schema --references --data --default-schema`; `bruinell sql stale --schema <file> --recordings <folder>`: stale recordings and why, exit 1 when any | `tools/Bruinell.Cli/Sql/SqlCommands.cs` |

### 3.3 Checks

| # | Check | Passes when |
| --- | --- | --- |
| S1 | Triggers | Changing a trigger changes its table's hash; the trigger's target table is in its table's references |
| S2 | Unqualified names | A procedure in `sales` naming `orders` references `sales.orders`, not `dbo.orders` |
| S3 | Dynamic SQL | A procedure with `sp_executesql` is `Dynamic`; merged references follow it to its tables; `stale` warns about one with none |
| S4 | Reference data | Changing a row of a `--data` table changes its hash; a table not listed keeps its hash |
| S5 | Default schema | With default schema `sales`, a recording's command naming `orders` is stamped `sales.orders` |
| S6 | `bruinell sql stale` | On the spike's recordings: none stale on the checked-in snapshot; a changed procedure lists only the recordings that call it, exit 1 |

## Step 4: the heads

bUnit's container and a host per test in the test's process already pass on the package (step 2:
the spike suite, `ConfigureDbContext` with `UseSqlReplay(session)`). Step 4 adds the third head: an
app in a process of its own (IIS, a started exe), where the test's session can't be handed over in
code. 1.7 waits until the owner has the legacy system again; step 4 is proven on a sample here.

### 4.1 Decisions

| Question | Decision |
| --- | --- |
| How a request names its test | Cookie `bruinell-test` (what a browser sends on every request), or header `X-Bruinell-Test` (an API client). Test ids: `[A-Za-z0-9._-]`, so no path escapes the folder |
| Where sessions live | In the app's process: `ReplayRegistry`, one session per test id, its recording `<folder>/<test>.json`. Mode and folder from the app's environment: `BRUINELL_SQL_REPLAY`, `BRUINELL_SQL_RECORDINGS`; an optional `BRUINELL_SQL_SCHEMA` snapshot for the stamp |
| Begin and end | Control paths on the app: `/__bruinell/sql/begin?test=<id>` starts the session and sets the cookie (a browser navigates there first); `/__bruinell/sql/end?test=<id>` finishes it and answers 200, or 409 with the misses. `ReplayHook.EndAsync` throws `ReplayMissException` on 409 |
| Safety | Without `BRUINELL_SQL_REPLAY` in the app's environment the hook adds nothing: no control paths (404), no scope |
| How commands find the session | ASP.NET Core: middleware enters `ReplayScope` for the request. System.Web: an `IHttpModule` keeps it in `HttpContext.Items`, read through `ReplayScope.Fallback`, as `AsyncLocal` doesn't flow through the classic pipeline's events. The app's connections come from `ReplayingDbProviderFactory` or EF's `UseSqlReplay()`; a `new SqlConnection` in app code can't be reached (noted, as in the proposal) |

### 4.2 What is built

| Part | Where |
| --- | --- |
| `ReplayRegistry` (`Begin`, `Find`, `End`, `FromEnvironment`), `ReplayHook` (names, test id of a request, control paths, `BeginUrl`, `EndAsync`), `ReplayScope.Fallback` | `srcnew/Bruinell.Mocking.Data/Replay/ReplayHook.cs`, `ReplaySession.cs` |
| `UseSqlReplayHook()` middleware | new `srcnew/Bruinell.Mocking.Data.AspNetCore` (net10.0, `Microsoft.AspNetCore.App`) |
| `SqlReplayModule : IHttpModule` | new `srcnew/Bruinell.Mocking.Data.SystemWeb` (net48, outside `Bruinell.sln` like the net48 tests) |
| A minimal ASP.NET Core app over SQLite through `ReplayingDbProviderFactory` | new `samples/Bruinell.Samples.Sql.Hook` |
| Its tests: the app started as a process of its own | new `testsnew/Bruinell.Mocking.Data.Hook.Tests` |

### 4.3 Checks

| # | Check | Passes when |
| --- | --- | --- |
| H1 | In-process heads | The spike suite still passes on both heads through the package (step 2's R4) |
| H2 | Another process | The sample started with `dotnet` as its own process: tests record through the hook, then replay with the database file gone |
| H3 | Parallel | 8 tests at once against one app process, 3 rounds, each answered from its own recording; a writing test's error (SQLite 19 as HTTP 409) replayed |
| H4 | Misses come back | A request not recorded fails in the app, and `EndAsync` throws with the miss |
| H5 | Off by default | Without `BRUINELL_SQL_REPLAY` the control paths are 404 and the app runs on its database |
| H6 | System.Web | The module builds for net48; the hook's core and `ReplayScope.Fallback` pass in the net48 tests. Not run under IIS here |

## Steps 1 to 6: outline

From the amended proposal; each is detailed here before it is built.

| Step | What | Projects |
| --- | --- | --- |
| 1 | The log: detailed above | `srcnew/Bruinell.Mocking.Data`, `srcnew/Bruinell.Mocking.Data.EntityFrameworkCore`, their tests |
| 2 | Replay in the package: detailed above |  |
| 3 | The schema snapshot as its own command, the stamp per object; dynamic procedures, triggers, reference data, default schemas | the same, a CLI command |
| 4 | The heads: bUnit's container, a host per test for ASP.NET Core, a test-id hook for older UIs | the same, `Bruinell.Html.Bunit`, `Bruinell.Html.Playwright` fixtures |
| 5 | Maintenance: the re-record summary, the check on unread re-records | the same, a build check |
| 6 | `docs/guides/sql-replay.md` and an AD on when a test replays and when it needs the database | `docs/`, `.docs/decisions/` |
