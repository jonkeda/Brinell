# SQL replay: a database stand-in recorded from the real one

A proposal, 2026-10-25: revised after an architecture review, and again after the spike (step 0 of
[plan.md](plan.md), results in [spike-results.md](spike-results.md)). `Brinell.Mocking` stands in
for an HTTP backend with WireMock; nothing like it exists for a database. This records what a test
sends to the real database and what comes back, then replays it with no database. It is built in
Brinell and its samples first. The system it is meant for is about 20 years old, with some 2000
tables.

## Outcome

Replay works. The spike recorded five scenarios of a Blazor app on EF Core and SQL Server, with a
spatial column, a unique key, a view and a stored procedure, and replayed them with no database:
20 runs on the bUnit head and 20 in the browser, without a miss. Every miss it was shown, made by
hand or by a code change, named its command and where it parted from the recording.

What replay buys is not faster queries. Even on SQL Server the app's own queries were about 5% of a
test's time; resetting the database before each test was 37%, for one table. On a system with 2000
tables the database is the expensive part: having one, seeding it, resetting it, sharing it.
Replay takes all of that away for the tests that replay.

It ships in two parts, and the first is useful alone. **The log** records every command a test
sends, with its time: counts, an N+1, an unexpected query, and the database's share of the run.
**Replay** answers the commands from the recording. A suite that replays still runs against the
real database at its gate, since replay can't prove the database still accepts the commands.

## Why it fits a large, old schema

- **Changes stay local.** Each recording is stamped with the objects it touched, so a schema change
  stales only the tests that touch the changed table, view or procedure. In the spike a new table
  staled no recording; a changed procedure staled only the two tests that call it.
- **The SQL is stable.** Hand-written SQL and stored procedures don't move when code is refactored.
  The one way EF moved SQL without a reason (renaming a property reorders its columns) is matched
  away.
- **The database is the cost.** Seeding and resetting 2000 tables, or sharing one test database, is
  where the time and the flakiness are. That is exactly what replay removes.

The spike can't show the fan-out on a real schema: its app has three objects, and every test reads
the main one. How many tests a typical change reaches on the real system is the number that
decides how much replay costs to keep. The log measures it.

## What held from the first proposal

- **EF through interceptors**, not a wrapped connection. Two of the review's risks were real and
  needed workarounds: SQL Server's spatial mapping casts the reader to `SqlDataReader` (a swapped
  type mapping reads it through `DbDataReader`), and `SqlException` has no public constructor (it
  is built by reflection, so `catch ... when Number is 2627` still runs).
- **Volatile values are bound, found by recording twice.** The two runs differed exactly in the
  values the app makes (a new id, a timestamp), and binding carried them through later reads.
- **Writes are barriers, in their recorded order; a miss is loud** and never reaches a database.
- **Recordings per test and head**, kept in git; replay on the bUnit head first.

## What the spike changed

| Before the spike | Now |
| --- | --- |
| A read matched by its n-th occurrence | A read may be answered again between two writes: the browser repeats reads a varying number of times |
| A stamp hashing all migrations | A stamp per object: tables, views and procedures, from a checked-in schema snapshot |
| A command keyed by its text | A `SELECT`'s and an `INSERT`'s columns matched as a set, an insert's parameters by the column they fill |
| bUnit rendered the start page on every page load | It renders only the page opened; `StartBlank` opens on an empty page |
| Browser replay "later, if ever", by a header | A host per test in the test's process (`WebApplicationFactory` on Kestrel) |

The schema snapshot is the change that matters most for a legacy system. Recording reads every
table, view, procedure and function into `Recordings/schema.json`, each with a hash of its columns,
indexes and keys or of its body, and what its body references (`sys.sql_expression_dependencies`).
A recording's stamp follows those references from the objects its commands name, so a test that
calls a procedure is stamped with the tables the procedure reads. Replay reads only the file; after
a schema change, rewriting it shows which recordings went stale.

## What it costs, and what it doesn't prove

- **A change to an object re-records every test that touches it.** In the spike, ordinary changes
  to the main table re-recorded every test; on a large schema the same change reaches far fewer.
- **Replay can hide a race.** A race in the sample showed once on the real database and never in
  replay, since replay answers at once.
- **It leans on library internals.** The spatial mapping uses EF's internal API and the exception
  uses SqlClient's; either may break on an upgrade.
- **It proves nothing about the database**: schema, constraints, query plans and locking stay with
  the gate on a real database.

## Still open for the legacy system

Four facts about the system decide the next design steps. Each has a proposal.

- **How it reaches the database.** Probably raw ADO.NET and stored procedures. Proposal: build the
  ADO.NET adapter (a wrapped `DbConnection`) first; the spike only built the EF path. Procedures
  need output parameters, return values and several result sets, none of which the spike ran.
- **Which runtime.** .NET Framework with `System.Data.SqlClient` changes the adapter and the
  exception factory. Proposal: one core, an adapter per client.
- **Which UI.** The bUnit head is Blazor only, and a host per test needs ASP.NET Core in the test's
  process. Proposal: for WebForms or MVC on IIS, a test id sent by the browser and read by a small
  hook in the app, so commands are scoped per test across processes.
- **How the test database is made today.** It is the baseline replay has to beat. Proposal: the log
  measures it on the real system before replay is built there.

And four gaps in the snapshot: dynamic SQL in procedures has no dependencies to read; triggers
aren't hashed with their tables; reference data written by scripts isn't in any hash; unqualified
names in a body are taken as `dbo`. Proposal: dependencies listed by hand for the few dynamic
procedures, triggers hashed into their table, reference data recorded once as a shared recording.

## Parallel runs

Parallel runs matter as much as replay itself. On a legacy system the shared test database is
usually what keeps UI tests serial: tests collide on each other's rows, so suites run one at a time
or are split by hand. A replayed test shares no database, so replayed tests can run side by side.

- **Nothing is shared between tests.** Each test's session (its recording, bindings and misses) is
  found through its own container on bUnit or its own host in the browser, never through a static.
  Recordings are a file per test and head; the schema snapshot is read-only.
- **Recording runs on a database per worker.** Recording still needs a real database, and
  recording in parallel on one would bring the collisions back. A worker leases a database of its
  own, made once from the migrations (or a template), and resets it per test as before.
- **The cost is the host, not the database.** A host per test in the browser starts on a free port
  and is safe in parallel, but each one takes start-up time and memory; how many workers a machine
  carries is measured, not assumed.
- **EF's internal service providers** must not multiply with every test's interceptors; the spike
  makes that an error to find out.

The spike proves it as pass line P5: the statics removed, the tests split across classes so they
run in parallel, 20 parallel replays on both heads without a miss, and parallel recording on a
database per worker giving the same recordings as a serial one.

## Never in scope

Synthesizing results for a command that wasn't recorded, recordings shared across providers,
re-recording by itself, and TDS on a socket.

## Steps

The spike (step 0) is done. The rest, in order:

1. **The log**, for ADO.NET and EF: commands per test with their time and the objects they touch.
   Its first use is measuring the legacy system: the database's share of a test, and how many
   tests a typical change reaches.
2. **The package**, from the spike's code: `Brinell.Mocking.Data` (core, ADO.NET adapter, SqlClient
   rules) and `Brinell.Mocking.Data.EntityFrameworkCore`; matching, binding, errors, the miss
   message.
3. **The schema snapshot** as a command of its own, the stamp per object, the gaps above.
4. **The heads**: bUnit's container, a host per test for ASP.NET Core, a test-id hook for older UIs.
5. **Maintenance**: a re-record summary (commands and rows changed) and a check that no re-record
   goes in unread.
6. **Guide and decision**: `docs/guides/sql-replay.md`, and an AD on when a test replays and when it
   needs the database.

A trial on the legacy system follows once step 1 has measured it, by the owner's decision.

## Open questions, with proposals

- **One package or two?** Proposal: two, so raw ADO.NET users don't pull in EF (AD-026).
- **Should the spike app stay as a sample?** Proposal: yes; it is Brinell's only sample on EF Core
  and SQL Server, and it now has a view and a procedure.
- **Build replay if the log shows a small database share?** Proposal: decide from step 1 on the
  legacy system, counting the reset and the seeding, not only the queries.
