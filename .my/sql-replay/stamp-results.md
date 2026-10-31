# SQL replay: step 3 results

Agent document. Step 3 of [plan.md](plan.md): the schema snapshot's four gaps closed and the stale
check as a command. S1 to S6 pass on 2026-10-25.

## Checks

| # | Result | Test |
| --- | --- | --- |
| S1 | Pass | `SchemaSnapshotTests.Trigger_IsPartOfItsTable_ItsHashAndWhatItWrites`, `DefaultAndCheck_ArePartOfTheirTable` |
| S2 | Pass | `SchemaSnapshotTests.UnqualifiedName_InABody_IsTheOneInItsSchema`: `sales.total` naming `Orders` references `sales.orders`, beside a `dbo.Orders` |
| S3 | Pass | `SchemaSnapshotTests.DynamicSql_IsMarked_AndItsReferencesListedByHand` |
| S4 | Pass | `SchemaSnapshotTests.ReferenceData_ChangedRows_ChangeOnlyTheListedTablesHash` |
| S5 | Pass | `StampTests.Named_UnqualifiedName_IsTakenInTheDefaultSchema_ThenDbo`, `Recording_IsStampedInTheDefaultSchema` |
| S6 | Pass | `StampTests.Check_ListsOnlyTheRecordingsAChangeReaches_AndSkipsOtherFiles`; by hand on the spike below |

S6 on the spike (re-recorded twice per head on the new hashes): `bruinet sql stale --schema
Recordings/schema.json --recordings Recordings` gives 0 of 10, exit 0. `dbo.VisitCount` changed on a
worker database and `bruinet sql schema` written again: 4 of 10 stale, exactly `AddPlaceTests` and
`PlaceDistanceTests` on both heads (`dbo.visitcount changed`), exit 1. Restored: 0 of 10. The spike
then replays 3 times per head with no server, no miss.

## What was built

| Part | Where |
| --- | --- |
| Triggers, defaults and checks as lines of their table; a trigger's references its table's; references resolved by `referenced_id`, else the body's schema, else `dbo`; `Dynamic`; data hashes; `DefaultSchema`; `MergeReferences(path)` returning names it doesn't know | `src/Bruinet.Mocking.Data/SchemaSnapshot.cs` (`SnapshotOptions`) |
| `Statements.Names` (schema null when not written); `Schema.DefaultSchema`, `Schema.Named(text)`; `ReplaySession.Finish` stamps through it | `Statements.cs`, `Replay/Recording.cs`, `Replay/ReplaySession.cs` |
| `RecordingCheck.Check(folder, schema)`, `RecordingState` | `Replay/RecordingCheck.cs` |
| `bruinet sql schema --references --data --default-schema`; `bruinet sql stale --schema --recordings --version name=value` | `tools/Bruinet.Cli/Sql/SqlCommands.cs` |

## Notes

- Table hashes now include defaults, checks and triggers, so every snapshot written before this
  step differs on tables that have them: re-record once after upgrading.
- `stale` compares only the versions given with `--version`; the tool can't know the app's.
- `BINARY_CHECKSUM` skips columns of types it can't compare (`xml`, spatial): a change only there
  isn't seen in a `--data` table.
- A dynamic object touched by a recording with no listed references is a warning, not a failure.
