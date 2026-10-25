# SQL replay: step 5 results

Agent document. Step 5 of [plan.md](plan.md): re-records made readable, and reading them a gate.
M1 to M6 pass on 2026-10-25.

## Checks

| # | Result | Test |
| --- | --- | --- |
| M1 | Pass | `RecordingMaintenanceTests.Diff_ShowsEachKindOfChange_OnALineOfItsCommand`: stamp, added, removed, columns, rows, affected, error |
| M2 | Pass | `Diff_ValuesThatDifferOnlyByAVolatileValue_AreNoChange`, `Hash_AndDiff_IgnoreVolatileValuesAndHowOftenAReadRepeats` |
| M3 | Pass | `Diff_AWriteAddedEarly_IsOneAddedCommand_AndLaterOnesStillPair` |
| M4 | Pass | `Review_PassesAfterAccept_FailsOnAChange_AndASameReRecordNeedsNone` |
| M5 | Pass | `Record_OverARecordingWithoutOneOfItsCommands_Saves` |
| M6 | Pass | On the spike, below; `RecordingReviewTests.Recordings_AreAllReviewed` in the spike suite |

M6 on the spike: re-recorded unchanged on both heads, `bruinell sql diff --recordings Recordings`
gives 0 of 10 changed. Medemblik's population changed in the seed and bunit re-recorded: 4
recordings changed, each `~ reader SELECT [p].[Id], ...: values in 1 row (Population)`;
`bruinell sql reviewed` exit 1 and the suite's gate test fails. Seed and recordings put back: both
pass, and the spike replays twice per head with no server.

## What was built

| Part | Where |
| --- | --- |
| `RecordingDiff.Compare(before, after)` | `srcnew/Bruinell.Mocking.Data/Replay/RecordingDiff.cs` |
| `RecordingReview` (`Recordings`, `IsRecording`, `Hash`, `Unreviewed`, `Accept`), `reviewed.json` | `Replay/RecordingReview.cs` |
| `Recording.Parse`; `MarkVolatile` leaves a command without counterpart unmarked | `Replay/Recording.cs` |
| `bruinell sql diff --recordings --base --accept`, `bruinell sql reviewed --recordings` | `tools/Bruinell.Cli/Sql/SqlCommands.cs` |
| The spike's `Recordings/reviewed.json`, `Tests/RecordingReviewTests.cs` | `testsnew/Bruinell.Sql.Spike.Tests` |

## Found while building

- **The first spike diff was all noise.** A re-record of an unchanged suite rewrote 5 files: new
  volatile values, and the browser repeating reads a different number of times. The review hash is
  now of what replay answers: the stamp, writes in order, the reads between two writes as a set,
  volatile values blanked. The diff leaves out reads only repeated more or fewer times.
- **`MarkVolatile` threw after any app change** (a command the replaced recording lacked); such a
  command is now left for the second recording to mark.
- **`git ls-tree` paths are relative to the working directory**; the diff asks for `--full-name`.

## Open

- A value change shows its column, not its old and new values: the recordings hold data, and the
  summary is for review, not for the log.
- The gate is a test in the suite and a CLI command; wiring it into a CI pipeline is the user's.
