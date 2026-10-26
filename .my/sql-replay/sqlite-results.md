# SQL replay: step 7 results

Agent document. Step 7 of [plan.md](plan.md): the schema snapshot on SQLite, so a SQLite suite gets
the stamp and stale reports. Q1 to Q6 pass on 2026-10-26, with no server.

## Checks

| # | Result | Test |
| --- | --- | --- |
| Q1 | Pass | `SqliteSchemaTests.Objects_AreTheTablesAndViewsInMain_NotTheCatalogsIndexesOrTriggers`: no `sqlite_sequence` (an `AUTOINCREMENT` table), no `sqlite_stat1` (after `ANALYZE`) |
| Q2 | Pass | `Hash_ChangesWithAColumnAnIndexOrATrigger_OnlyForItsTable` |
| Q3 | Pass | `References_FollowViewsAndTriggers_AndDropNamesThatAreNoObject`: a CTE's name dropped |
| Q4 | Pass | `ReferenceData_ChangedRowsChangeTheHash_TheSameRowsInAnotherOrderDont` |
| Q5 | Pass | `Stamp_TakesEfsQuotedNamesInMain_AndNotItsCatalogProbe`: stamped `main.places` and, through its trigger, `main.audit`; the `sqlite_master` probe nothing |
| Q6 | Pass | `Stale_AfterAColumnIsAdded_IsOnlyTheRecordingThatReadsItsTable`; and by hand through the CLI, below |

Q6 by hand: a file made with Python's `sqlite3` (two tables, a view), `bruinet sql schema --provider
sqlite` wrote 3 objects of `main`, the view's reference `main.places`. Two recordings stamped from
it: `stale` 0 of 2, exit 0. `ALTER TABLE Kinds ADD COLUMN Code` and the snapshot written again: 1 of
2, `bunit\Kinds.json  main.kinds changed`, exit 1.

The data suite (53, the SQL Server snapshot tests included), the CLI's SQL tests and the net48
tests pass.

## What was built

| Part | Where |
| --- | --- |
| `SqliteSchema.ReadAsync` (internal): `sqlite_master`, hashes, parsed references, data hashes in C# | `srcnew/Bruinet.Mocking.Data/SqliteSchema.cs` |
| `SnapshotProvider` (`Auto`, `SqlServer`, `Sqlite`), `SnapshotOptions.Provider`; `ReadAsync` picks the reader by the connection's type | `SchemaSnapshot.cs` |
| `sqlite_*` names left out of a command's objects | `Statements.cs` |
| `bruinet sql schema --provider sqlite`; the CLI references `Microsoft.Data.Sqlite` | `tools/Bruinet.Cli` |
| `--provider sqlite` in "Stale Recordings" | `docs/guides/sql-replay.md` |

## Notes

- A trigger's references are its table's, so Q5's read of `Places` is stamped with `main.audit`
  too, as on SQL Server: a change to the audit table stales the tests that write places.
- Attached databases (`aux.table`) and `temp` objects are out of scope; names are in `main`.
- The machine's .NET was updated during the step (10.0.14, 11.0 RC): two builds failed in MSBuild
  and the host until the install finished; reused build nodes were avoided
  (`--disable-build-servers`) and one stale `obj` reference assembly rebuilt.
