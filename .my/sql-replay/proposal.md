# SQL replay: a database stand-in recorded from the real one

A proposal, 2026-10-25. `Brinell.Mocking` stands in for an HTTP backend with WireMock. Nothing
like it exists for a database: SQL Server's wire protocol (TDS) has no maintained fake, and
`sqlmock`-style libraries need every query and its rows written by hand. This proposes recording
what a test sends to the real database and what comes back, then replaying it, so a test can run
with no database at all. The scope is Brinell and its samples; trying it on an app comes later.

## Outcome

A test runs once against the real database in **record** mode. Each command's text, parameters
and results are saved in order to a recording per test. In **replay** mode the same test gets
those results back from the recording, and the database is never opened. If a command doesn't
match the recording, the test fails and names the command, the way a WireMock request with no
mapping fails. Re-recording is one switch.

What this buys is speed and independence: no server to start, no database to reset or restore
between tests. What it costs is fidelity. A replayed test proves the app sends the same commands
as when it was recorded. It doesn't prove the database still accepts them.

## Where it sits

The stand-in is an ADO.NET `DbConnection`, not an EF Core interceptor. Brinell's own samples use
raw ADO (the Todo app's `SqliteTodoRepository`, the API's `ServerTodoStore`), and EF Core accepts
any `DbConnection` too, so one layer covers both. TDS on a socket would also cover code that
builds its own connection, but it means implementing login, TLS and the token stream. That is
out of scope.

| Mode | The connection | Results come from |
| --- | --- | --- |
| Record | `RecordingDbConnection` wraps the real one | The database, saved as they pass |
| Replay | `ReplayDbConnection`, no inner connection | The recording |
| Off | The real connection, untouched | The database |

The app must open connections through a seam the test can replace: a factory, or a `DbConnection`
from the composition. The Todo sample's `TodoDatabase.OpenAsync` returns `SqliteConnection` and
would return `DbConnection` instead.

## The recording

One JSON file per test, beside its other evidence, kept in git like the suites' baselines. It
holds a list of exchanges, in the order they happened:

- **The command:** its text, normalized (whitespace collapsed), its parameters by name, type and
  value, and how it ran (reader, non-query or scalar).
- **The result:** each result set's columns with their types and the rows, or the rows affected,
  or the scalar. Binary values are stored as base64, so provider-specific types (spatial,
  `rowversion`) survive as bytes.
- **Transactions:** begin, commit and rollback as exchanges of their own.

## How matching works

The hard part isn't matching text; it's that a database has state.

- **In order, per connection.** Exchange *n* answers the *n*-th command. The same `SELECT` before
  and after an `INSERT` gets different rows because it's a different exchange, as in WireMock's
  scenarios. Concurrent connections each get their own sequence, keyed by the order they opened.
- **Volatile values.** New GUIDs, the clock and generated identity values change between runs.
  A test marks parameters as volatile by name or pattern, and matching ignores them. The default
  is to ignore nothing, so a miss is loud before it's tuned.
- **Results that echo inputs.** When a replayed `INSERT ... OUTPUT` returns an id, it returns the
  recorded one, which can differ from the id the app just generated. The fix is a capture rule
  that copies an input parameter into the result. Bulk copy and other non-command calls are
  refused in replay with a named exception, not faked.
- **A miss.** The failure lists the expected and the actual command side by side, with the
  parameter that differed. It never falls through to the database silently.

## What stays on the real database

Anything whose point is the database's own behaviour: schema and migrations, constraints and
indexes, query translation after a provider or ORM upgrade, concurrency and locking. A suite that
replays should still run recorded against the real database at its gate. That also catches stale
recordings, the way the test-selection papers catch a selection miss: select on the cheap head,
run in full on the dear one.

## Where it lives in Brinell

A new package, `Brinell.Mocking.Data`, depending only on `System.Data.Common`. `Brinell.Mocking`
pulls in WireMock, which a database-only user shouldn't need. Under AD-026 a project has to earn
its place, and the dependency split is the reason. An EF Core helper (registering the connection
in a `DbContext`'s options) stays a few lines of documentation, not a project.

## Steps

1. **Spike.** `RecordingDbConnection` and `ReplayDbConnection` over SQLite: commands, readers,
   scalars, non-queries, transactions; one JSON recording; one test recorded and replayed.
2. **Matching.** Order per connection, volatile parameters, capture rules, the miss message.
3. **The seam in the Todo sample.** `TodoDatabase` and `ServerTodoStore` open through a factory;
   the fixture picks the mode from configuration (`Brinell:SqlReplay = Record | Replay | Off`).
4. **Todo's suites on replay.** The Todo web and API suites recorded, then replayed. Measured:
   time per test against the SQLite run, and how many exchanges needed a volatile or capture rule.
5. **SQL Server's types.** A small test project against a local SQL Server (`decimal`,
   `datetimeoffset`, `rowversion`, `geography` as bytes, `OUTPUT` clauses), recorded and replayed.
   The Todo sample is SQLite only, so this step is where SQL Server is proven.
6. **Guide and decision.** `docs/guides/sql-replay.md` and an AD on when a test replays and when
   it needs the database.

## Open questions, with proposals

- **Re-record by hand or on a miss?** Proposal: by hand (`Record` mode). Recording on a miss
  hides real regressions, the way auto-accepting a changed baseline would.
- **One file per test, or per scenario and head?** Proposal: per test, named by the xUnit test
  as the call log already is. A scenario run on two heads sends the same commands, so the
  recording is shared.
- **Should the Todo sample move to EF Core to prove the EF path?** Proposal: no. Step 5's test
  project uses EF Core against SQL Server and covers it, and the sample keeps its raw ADO.
- **Speed on SQLite proves little.** The Todo suites already use a local file. Proposal: step 4
  proves the mechanism and the rule counts; step 5 measures the speed against a server.
