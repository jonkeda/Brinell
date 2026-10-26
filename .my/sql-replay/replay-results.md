# SQL replay: step 2 results

Agent document. Step 2 of [plan.md](plan.md): the spike's replay moved into the package, parallel
runs first. R1 to R5 pass on 2026-10-25.

## Checks

| # | Result | Test |
| --- | --- | --- |
| R1 | Pass | `ReplayingDbConnectionTests.Replay_AnswersReadsWritesScalarsAndErrors_WithTheDatabaseGone`, `Replay_ACommandNotRecorded_IsAMiss` |
| R2 | Pass, on SQL Server 2022 (`.`) | `SqlServerReplayTests.Replay_GivesOutputsReturnValueResultSetsGeographyAndErrors_WithNoServer`: replayed against `nowhere.invalid` |
| R3 | Pass | `ReplayInterceptorTests.Replay_AnswersTheContext_WithTheDatabaseGone`; the spike suite on the package (spatial through `UseSpatialReplay`) |
| R4 | Pass | `ReplayingDbConnectionTests.Factory_SessionsInParallel_EachReplaysItsOwn` and `ReplayInterceptorTests.Scope_SessionsInParallel_ThroughOneSetOfOptions`: 8 sessions, 5 rounds, 40 replays at once. Spike suite: 20 replays per head, `BRUINET_SQL_CONNECTION` on `nowhere.invalid`, 0 misses (bUnit 46 s, browser 149 s for 20 runs) |
| R5 | Pass | `FrameworkTests.Replay_TheFrameworksSqlClient_OutputsRowsAndErrors_WithNoServer`: net48, `System.Data.SqlClient`, `SqlException` 8134 raised again |

## What was built

| Part | Where |
| --- | --- |
| `Recording`, `Exchange` (with `Outputs`), `Schema`, `Stamp`, `Values` | `srcnew/Bruinet.Mocking.Data/Replay/Recording.cs` |
| `ReplaySession`, `ReplayScope`, `ReplayMissException` | `Replay/ReplaySession.cs` |
| `ReplayDataReader` | `Replay/ReplayDataReader.cs` |
| `ProviderRules`: live exchange, `ReadAll`, outputs, `Replay`, errors raised by reflection | `Replay/ProviderRules.cs` |
| `ReplayingDbConnection`, `ReplayingDbCommand`, `ReplayingDbTransaction`, `ReplayingDbProviderFactory` | `Replay/ReplayingDbConnection.cs` |
| `SchemaSnapshot.ToSchema(versions)` | `SchemaSnapshot.cs` |
| `ReplayInterceptor(Func<ReplaySession?>)`, `UseSqlReplay(session)`, `UseSqlReplay()` on `ReplayScope` | `srcnew/Bruinet.Mocking.Data.EntityFrameworkCore/ReplayInterceptor.cs` |
| `UseSpatialReplay()` | new `srcnew/Bruinet.Mocking.Data.EntityFrameworkCore.SqlServer` (net10.0, EF SqlServer NTS 10.0.12) |

The spike suite uses the package; its `Spike/` copies are deleted (only `DatabaseShare.cs` stays).
Recordings re-recorded twice per head; their stamp's versions are now a map (`ef`, `sqlclient`).

## Found while building

- **EF in replay needs a transaction with nothing behind it.** `SaveChanges` of several rows begins
  a transaction, and the connection isn't open: `UseSqlReplay` adds a transaction interceptor that
  answers `TransactionStarting` with an empty one (commit, rollback and savepoints do nothing).
- **SQLite's `EnsureCreated` probes by opening the file.** In replay the open is suppressed, so it
  sees a database and takes another path than when recorded. A test creates its tables with
  `GenerateCreateScript()` instead. The same holds for any code that branches on whether a connection
  opens; recording connection opens is not done.
- **The session is resolved per command.** One interceptor or factory serves every test in
  parallel; the session comes from the test's container, its host, or `ReplayScope`. Outside a scope
  the factory gives the real connection.
- **Records read whole in both modes.** In record mode the app too reads from a `ReplayDataReader`,
  so it reads alike in both; a `geography` comes back as `SqlBytes` either way.

## Open

- Steps 3 to 6 of the plan: the snapshot as its own command, the heads' hooks, maintenance, the guide.
- Not tried: Dapper over `ReplayingDbConnection`, `DbDataAdapter`, `CommandBehavior.SequentialAccess` on big blobs.
