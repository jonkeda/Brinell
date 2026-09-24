# Baseline before the test-data work

Date: 2026-09-24. Branch `move` at 2962bfc. See [implementation-plan.md](implementation-plan.md).

| Suite (from `samples/Todo`) | Passed | Failed | Skipped |
| --- | --- | --- | --- |
| `tests/Brinell.Samples.Todo.UnitTests` | 114 | 0 | 0 |
| `tests/Brinell.Samples.Todo.IntegrationTests` | 28 | 0 | 0 |
| `tests/Brinell.Samples.Todo.JourneyCoverage` | 0 | 7 | 0 |
| `tests/Brinell.Samples.Todo.Uat` | 4 | 0 | 0 |
| `tests/Brinell.Samples.Todo.UITests --filter Gate=Smoke` | 2 | 0 | 0 |
| `tests/Brinell.Samples.Todo.UITests` (full) | 36 | 0 | 3 |

Builds: `srcnew/Brinell.sln` 0 errors (22 warnings); Todo app (`net10.0-windows10.0.19041.0`) 0 errors.

Known issue, not caused by this work: all 7 `JourneyCoverage` tests fail with
`FileNotFoundException: Brinell.Samples.Todo.UITests.Mobile`. The coverage report loads the
Android test assembly by reflection, and that assembly is not built on this machine (no Android
build in the baseline). Compare against this, not against green.

## After phases 1-9 (same day)

| Suite | Passed | Failed | Skipped | Note |
| --- | --- | --- | --- | --- |
| Todo UnitTests | 114 | 0 | 0 | |
| Todo IntegrationTests | 40 | 0 | 0 | +12: TestDataTests |
| Todo Uat | 7 | 0 | 0 | one case per scenario: TOD.02 (5), TOD.04.3, TOD.05.1; no restart |
| Todo UITests (full) | 36 | 0 | 3 | as baseline |
| CI unit list (`testsnew`) | all | 0 | | + Brinell.TestData.Tests 15, DataValidator.Tests 10, Mocking.Analyzers.Tests 7, Uat.Tests 116 (was 100) |
| Wpf / Maui / Presenter / Stride UAT | 2 / 4 / 49 / 1 | 0 | | |
| WinForms UAT | 0 | 1 | | pre-existing: same `Access is denied` on Password at commit 350185f |
| Html / Blazor UAT | 0 | 2 / 2 | | environment: Playwright's chromium-headless-shell-1234 not installed; same at 350185f |
| Brinell.Maui.UITests (full) | 334 | 17 | 2 | 15 Shell (`AppShell` MissingRoot; same with this work's only shared change reverted), 1 OccludedScreenshot (occluding window), 1 Background (passes on rerun) |

No Shell baseline exists from before this work on this machine; the Shell failures are
reported as pre-existing on the evidence above, not proven against a full rebuilt baseline
(a worktree build of the MAUI sample fails in the scratch folder: MakePri path too long).
