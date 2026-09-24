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
