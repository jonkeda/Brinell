<!-- markdownlint-disable-file -->
# Research: src/ and tests/ Migration Completeness

**Date**: 2026-02-23
**Scope**: Determine if `src/` and `tests/` directories can be safely deleted in favor of `srcnew/` and `testsnew/`

## Source Projects Comparison (src/ → srcnew/)

### Migration Status

| # | Old Project (src/) | Files | New Project (srcnew/) | Files | Status |
|---|---|---|---|---|---|
| 1 | Brinell.Core | 84 | Brinell.Core | 53 | **MIGRATED** (redesigned, unified) |
| 2 | Brinell.Blazor | 27 | Brinell.Blazor | 23 | **MIGRATED** |
| 3 | Brinell.Html | 28 | Brinell.Html | 37 | **MIGRATED** (expanded) |
| 4 | Brinell.Html.Playwright | 38 | Brinell.Html.Playwright | 3 | **MIGRATED** (controls moved to Html) |
| 5 | Brinell.Maui | 120 | Brinell.Maui + .Appium + .FlaUI + .CommunityToolkit | 79 | **SPLIT** into 4 projects |
| 6 | Brinell.FlaUI | 12 | Brinell.Maui.FlaUI (+ WinForms + Wpf) | 4 | **RENAMED** + inlined |
| 7 | Brinell.Mocking | 2 | Brinell.Mocking | 2 | **MIGRATED** (identical) |
| 8 | Brinell.Stride | 29 | Brinell.Stride | 33 | **MIGRATED** (expanded) |
| 9 | Brinell.Stride.Automation | 5 | Brinell.Automation | 9 | **RENAMED** (expanded) |
| 10 | Brinell.WinForms | 17 | Brinell.WinForms | 38 | **MIGRATED** (expanded) |
| 11 | Brinell.Wpf | 16 | Brinell.Wpf | 34 | **MIGRATED** (expanded) |
| 12 | **Brinell.Testing** | **21** | *(none)* | 0 | **DEAD CODE** — never referenced, never in .sln |

**Source total: 399 old → 311 new files** (reduction from architecture cleanup + two-generation unification)

### Brinell.Testing Assessment

21 files containing generic testing infrastructure (benchmarks, distributed coordinators, cloud runners, DB analyzers, visual regression, accessibility). **Not a single file is referenced by any project.** Not listed in either solution file. Contains `EF Core`, `Serilog`, `Moq` dependencies unrelated to UI test automation. AI-generated placeholder code. The new architecture has proper per-platform `*TestFixtureBase` classes.

**Verdict: Safe to delete.**

## Test Projects Comparison (tests/ → testsnew/)

| Old Project (tests/) | Old Tests | New Project (testsnew/) | New Tests | Status |
|---|---|---|---|---|
| Blazor.Tests.ControlObject6 | 283 | Blazor.Tests | 172 | **MIGRATED** (API cleanup reduced count) |
| Blazor.UITests.ControlObject6 | 0 | Blazor.UITests | 31 | **OBSOLETE** (was only a .md planning doc) |
| Core.Tests.ControlObject6 | 77 | Core.Tests | 0 | **NOT MIGRATED** (empty shell) |
| Maui.Tests.ControlObject6 | 304 | Maui.Tests + Maui.UITests | 13 + 90 | **SHIFTED** (unit → integration UI tests) |
| Maui.UITests.ControlObject6 | 0 | Maui.UITests | 90 | **OBSOLETE** (was only a .md planning doc) |

### Core.Tests Gap

77 tests for locators (`ByTests`, `ControlLocatorTests`, `LocatorStrategyTests`) and core interfaces (`IControlObjectTests`, `IClickableControlObjectTests`, etc.) have no equivalent in `testsnew/Brinell.Core.Tests/` (only `GlobalUsings.cs`).

**Risk**: Low — these tested the old ControlObject6 interfaces which have been redesigned. The new interface set is tested indirectly through platform-specific unit tests (172 Blazor, 13 Maui, 9 WinForms, 10 Wpf = 204 tests exercising Core interfaces).

## Solution File References

### Root Brinell.sln

- References: 8 srcnew/ + 3 testsnew/ + 6 samples/
- References from src/: **NONE**
- References from tests/: **NONE**

### srcnew/Brinell.sln

- References: 9 srcnew/ + 15 testsnew/ + 2 samples/
- References from src/: **NONE**
- References from tests/: **NONE**

## Blocking Dependencies on src/

**8 sample projects** still reference `src/` via `ProjectReference`:

| Sample Project | src/ References |
|---|---|
| Brinell.Samples.WinForms.App | `src/Brinell.Core` |
| Brinell.Samples.Blazor.UITests | `src/Brinell.Html` |
| Brinell.Samples.Blazor.UITests.ControlObject6 | `src/Brinell.Blazor` |
| Brinell.Samples.Blazor.PlaywrightTests | `src/Brinell.Html.Playwright` |
| Brinell.Samples.Maui.UITests | `src/Brinell.Maui` |
| Brinell.Samples.Maui.UITests.ControlObject6 | `src/Brinell.Maui` |
| Brinell.Samples.Stride.UITests | `src/Brinell.Stride`, `src/Brinell.Core` |
| Brinell.Samples.WinForms.UITests | `src/Brinell.Core`, `src/Brinell.WinForms` |
| Brinell.Samples.Wpf.UITests | `src/Brinell.Wpf` |

These must be updated to reference `srcnew/` before `src/` can be deleted.

## Deletability Assessment

| Directory | Can Delete? | Blocker |
|---|---|---|
| **tests/** | **YES** — safe now | No references from any .sln or .csproj |
| **src/** | **NO** — blocked | 9 sample .csproj files reference src/ projects |

### Path to Deleting src/

1. Update 9 sample .csproj files: change `..\..\src\` → `..\..\srcnew\` project references
2. Handle name mismatches (e.g., `Brinell.FlaUI` → split across Maui.FlaUI/WinForms/Wpf; `Brinell.Testing` → removed; `Brinell.Stride.Automation` → `Brinell.Automation`)
3. Fix any API differences in sample code (old-gen ControlObject6 APIs → new sync APIs)
4. Verify samples build
5. Delete `src/`

### Complexity Estimate for Sample Migration

| Effort | Samples |
|---|---|
| **Trivial** (path swap only) | Samples.WinForms.App (Core ref) |
| **Medium** (path + minor API) | Samples.WinForms.UITests, Samples.Wpf.UITests, Samples.Stride.UITests |
| **High** (full async→sync rewrite) | All Blazor samples (3 projects), all Maui samples (2 projects) |

The ControlObject6 samples (`*.ControlObject6`) use the old async/FluentAssertions pattern and would need full rewrites similar to the `testsnew/Brinell.Blazor.UITests/` migration.
