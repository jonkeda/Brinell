<!-- markdownlint-disable-file -->
# Implementation Review: src/ and tests/ Migration Completeness

**Review Date**: 2026-02-23
**Related Plan**: None (research-only review)
**Related Changes**: None
**Related Research**: 01-migration-completeness-research.md

## Review Summary

Assessed whether the old `src/` (12 projects, 399 files) and `tests/` (5 projects) directories have been fully migrated to `srcnew/` (13 projects, 311 files) and `testsnew/` (15 projects). Both solution files already reference only srcnew/testsnew. The `tests/` directory can be deleted immediately. The `src/` directory cannot yet be deleted because 9 sample projects still reference it.

## Validation Results

### tests/ — SAFE TO DELETE

| Check | Result |
|---|---|
| Referenced by root Brinell.sln? | No |
| Referenced by srcnew/Brinell.sln? | No |
| Referenced by any sample .csproj? | No |
| Referenced by any testsnew/ .csproj? | No |
| Unique content not in testsnew/? | 77 Core.Tests (low risk — tested indirectly via 204+ platform tests) |

**Verdict: `tests/` can be deleted with no breakage.**

### src/ — BLOCKED (9 sample references)

| Check | Result |
|---|---|
| Referenced by root Brinell.sln? | No |
| Referenced by srcnew/Brinell.sln? | No |
| Referenced by sample .csproj files? | **YES — 9 projects** |
| Referenced by any testsnew/ .csproj? | No |
| Unique content not in srcnew/? | Brinell.Testing (dead code, 0 external references) |

**Blocking samples:**

1. `samples/Brinell.Samples.WinForms.App` → refs `src/Brinell.Core`
2. `samples/Brinell.Samples.Blazor.UITests` → refs `src/Brinell.Html`
3. `samples/Brinell.Samples.Blazor.UITests.ControlObject6` → refs `src/Brinell.Blazor`
4. `samples/Brinell.Samples.Blazor.PlaywrightTests` → refs `src/Brinell.Html.Playwright`
5. `samples/Brinell.Samples.Maui.UITests` → refs `src/Brinell.Maui`
6. `samples/Brinell.Samples.Maui.UITests.ControlObject6` → refs `src/Brinell.Maui`
7. `samples/Brinell.Samples.Stride.UITests` → refs `src/Brinell.Stride` + `src/Brinell.Core`
8. `samples/Brinell.Samples.WinForms.UITests` → refs `src/Brinell.Core` + `src/Brinell.WinForms`
9. `samples/Brinell.Samples.Wpf.UITests` → refs `src/Brinell.Wpf`

### src/Brinell.Testing — DEAD CODE

21 files, 0 external references, not in any .sln. Contains generic infrastructure (benchmarks, distributed coordinators, cloud runners) unrelated to UI test automation. Can be ignored — will be removed with `src/`.

## Migration Coverage Summary

### Source Projects (11 of 12 migrated)

| Status | Count | Projects |
|---|---|---|
| MIGRATED | 8 | Core, Blazor, Html, Html.Playwright, Mocking, Stride, WinForms, Wpf |
| SPLIT | 1 | Maui → Maui + Maui.Appium + Maui.FlaUI + Maui.CommunityToolkit |
| RENAMED | 2 | FlaUI → Maui.FlaUI; Stride.Automation → Automation |
| DEAD CODE | 1 | Testing (never referenced) |

### Test Projects (4 of 5 migrated)

| Status | Count | Projects |
|---|---|---|
| MIGRATED | 1 | Blazor.Tests.ControlObject6 → Blazor.Tests (172 tests) |
| SHIFTED | 1 | Maui.Tests.ControlObject6 → Maui.Tests + Maui.UITests (103 tests, unit→UI) |
| OBSOLETE | 2 | Blazor.UITests.ControlObject6, Maui.UITests.ControlObject6 (were only .md planning docs) |
| NOT MIGRATED | 1 | Core.Tests.ControlObject6 (77 tests — covered indirectly by 204+ platform tests) |

## Follow-Up Work

### Immediate (No Blockers)

* [ ] Delete `tests/` directory — safe, no references anywhere

### Requires Sample Migration First

* [ ] Update 9 sample .csproj files to reference `srcnew/` instead of `src/`
* [ ] Fix API differences in sample code (ControlObject6 → new sync pattern)
* [ ] Verify affected samples build after reference changes
* [ ] Delete `src/` directory

### Optional / Low Priority

* [ ] Populate `testsnew/Brinell.Core.Tests/` with new interface tests (covers the 77-test gap)
* [ ] Populate empty test shells: Automation.Tests, Html.Tests, Mocking.Tests, Stride.Tests

## Decisions Needed

1. **Delete `tests/` now?** — Safe to do immediately. The 77 Core.Tests are for the old ControlObject6 interfaces (redesigned in srcnew).

2. **Delete or keep `samples/` old-gen projects?** — The `*.ControlObject6` samples use the old async/FluentAssertions API. Options:
   - (a) Migrate them to new API (high effort, same as Blazor UITests migration)
   - (b) Delete them (they're redundant with testsnew/ UITests)
   - (c) Keep them as legacy references

3. **Migrate non-ControlObject6 samples?** — `Samples.Blazor.UITests`, `Samples.Blazor.PlaywrightTests`, `Samples.Maui.UITests` use first-gen APIs. Same options as above.

## Review Completion

**Overall Status**: Complete
**Reviewer Notes**: `tests/` is immediately deletable. `src/` deletion is blocked by 9 sample project references — user should decide whether to migrate or delete those sample projects first. The core framework migration (srcnew + testsnew) is complete with no remaining dependencies on the old directories.
