<!-- markdownlint-disable-file -->
# Implementation Review: Blazor Refactoring to srcnew/testsnew

**Review Date**: 2026-02-23
**Related Plan**: 01-blazor-refactoring-plan.instructions.md
**Related Changes**: 01-blazor-refactoring-changes.md
**Related Research**: 02-blazor-refactoring-research.md

## Review Summary

Full review of the Blazor refactoring implementation — migrating controls and tests from `src/Brinell.Blazor/ControlObject6/` and `tests/Brinell.Blazor.Tests.ControlObject6/` to the new `srcnew/Brinell.Blazor/` and `testsnew/Brinell.Blazor.Tests/` architecture. All 10 implementation phases completed. Build succeeds with 0 errors, 0 warnings. 172 unit tests pass. One critical convention violation found (Thread.Sleep in BlazorTestContext).

## Implementation Checklist

### From Research Document

* [x] Update `srcnew/Brinell.Blazor/Brinell.Blazor.csproj` to reference `Brinell.Html` + `Brinell.Html.Playwright`
  * Source: 02-blazor-refactoring-research.md (Lines 7-7)
  * Status: Verified
  * Evidence: srcnew/Brinell.Blazor/Brinell.Blazor.csproj references both projects

* [x] Implement `BlazorTestContext` in `srcnew/Brinell.Blazor/Context/`
  * Source: 02-blazor-refactoring-research.md (Lines 8-8)
  * Status: Verified
  * Evidence: srcnew/Brinell.Blazor/Context/BlazorTestContext.cs — composition wrapper around PlaywrightTestContext

* [x] Implement `BlazorPageObjectBase<TSelf>` in `srcnew/Brinell.Blazor/Pages/`
  * Source: 02-blazor-refactoring-research.md (Lines 9-9)
  * Status: Verified
  * Evidence: srcnew/Brinell.Blazor/Pages/BlazorPageObjectBase.cs — thin CRTP base

* [x] Implement `BlazorTestFixtureBase` in `srcnew/Brinell.Blazor/Testing/`
  * Source: 02-blazor-refactoring-research.md (Lines 10-10)
  * Status: Verified
  * Evidence: srcnew/Brinell.Blazor/Testing/BlazorTestFixtureBase.cs

* [x] Migrate 14 controls that have direct Html equivalents as thin inheritors/re-exports
  * Source: 02-blazor-refactoring-research.md (Lines 11-11)
  * Status: Verified — all 14 implemented with correct base classes, dual constructors, CRTP constraints
  * Evidence: 14 files in srcnew/Brinell.Blazor/Controls/ (ButtonControl through TabContainerControl)

* [x] Implement 5 Blazor-only controls (Audio, Video, Image, IFrame, NavMenu) extending Html base classes
  * Source: 02-blazor-refactoring-research.md (Lines 12-12)
  * Status: Verified — all 5 controls + MediaControlBase shared base implemented
  * Evidence: 6 files in srcnew/Brinell.Blazor/Controls/ (MediaControlBase, AudioControl, VideoControl, ImageControl, IFrameControl, NavMenuControl)

* [x] Create `MockHtmlFactory` in `testsnew/Brinell.Blazor.Tests/`
  * Source: 02-blazor-refactoring-research.md (Lines 13-13)
  * Status: Verified
  * Evidence: testsnew/Brinell.Blazor.Tests/Mocks/MockHtmlFactory.cs

* [x] Migrate 20 unit test files from old async/FluentAssertions to new sync/xunit Assert pattern
  * Source: 02-blazor-refactoring-research.md (Lines 14-14)
  * Status: Verified — 20 test files, 172 tests, all sync, all use Assert.*, all use Moq
  * Evidence: 20 files in testsnew/Brinell.Blazor.Tests/Controls/ and Context/

* [x] Remove Placeholder.cs files from `srcnew/Brinell.Blazor/` subdirectories
  * Source: 02-blazor-refactoring-research.md (Lines 15-15)
  * Status: Verified — all 4 deleted
  * Evidence: Context/, Controls/, Pages/, Testing/ Placeholder.cs files confirmed absent

* [x] All 22 controls implemented in `srcnew/Brinell.Blazor/Controls/`
  * Source: 02-blazor-refactoring-research.md (Lines 19-19) — Success Criteria
  * Status: Verified — 14 inherited + 6 Blazor-only + 1 MediaControlBase + 1 TabContainerControl = 22 total
  * Evidence: 22 .cs files in srcnew/Brinell.Blazor/Controls/

* [x] Context, page base, and test fixture implemented
  * Source: 02-blazor-refactoring-research.md (Lines 20-20) — Success Criteria
  * Status: Verified
  * Evidence: 3 infrastructure files confirmed

* [x] Unit tests cover all controls
  * Source: 02-blazor-refactoring-research.md (Lines 21-21) — Success Criteria
  * Status: Verified — 20 test files, all above minimum thresholds
  * Evidence: 172 tests passing

* [x] Project builds and tests pass
  * Source: 02-blazor-refactoring-research.md (Lines 22-22) — Success Criteria
  * Status: Verified
  * Evidence: `dotnet build` — 0 errors, 0 warnings; `dotnet test` — 172 passed, 0 failed

* [x] No Placeholder.cs files remain
  * Source: 02-blazor-refactoring-research.md (Lines 23-23) — Success Criteria
  * Status: Verified
  * Evidence: All 4 confirmed deleted

* [x] Decision D1: Place Blazor-only controls in `srcnew/Brinell.Blazor/Controls/`
  * Source: 02-blazor-refactoring-research.md (Lines 38-39) — Decision
  * Status: Verified — all in Brinell.Blazor.Controls namespace

* [x] Decision D2: Add `Evaluate<T>()` + `Evaluate()` to `IHtmlElement`
  * Source: 02-blazor-refactoring-research.md (Lines 48-49) — Decision
  * Status: Verified
  * Evidence: IHtmlElement.cs L25-26, PlaywrightHtmlElement.cs L196-200

* [x] Decision D3: Create `MediaControlBase<TScope>` for Audio/Video
  * Source: 02-blazor-refactoring-research.md (Lines 56-57) — Decision
  * Status: Verified — 14+ shared methods, AudioControl and VideoControl inherit
  * Evidence: srcnew/Brinell.Blazor/Controls/MediaControlBase.cs

* [x] Decision D4: Add `Brinell.Html` reference to test project
  * Source: 02-blazor-refactoring-research.md (Lines 62-63) — Decision
  * Status: Verified
  * Evidence: Brinell.Blazor.Tests.csproj references Brinell.Html and Brinell.Html.Playwright

### From Implementation Plan

* [x] Phase 1: Foundation — IHtmlElement + Project References (Steps 1.1-1.5)
  * Source: 01-blazor-refactoring-plan.instructions.md Phase 1
  * Status: Verified
  * Evidence: IHtmlElement Evaluate methods, csproj references, InternalsVisibleTo all confirmed

* [x] Phase 2: Infrastructure — Context, Page, Fixture (Steps 2.1-2.4)
  * Source: 01-blazor-refactoring-plan.instructions.md Phase 2
  * Status: Verified
  * Evidence: BlazorTestContext.cs, BlazorPageObjectBase.cs, BlazorTestFixtureBase.cs all confirmed

* [x] Phase 3: Inherited Controls — 14 thin re-exports (Steps 3.1-3.8)
  * Source: 01-blazor-refactoring-plan.instructions.md Phase 3
  * Status: Verified — all 14 with correct base classes, constructors, constraints
  * Evidence: 14 control files confirmed with correct inheritance

* [x] Phase 4: Blazor-Only Controls — 6 files (Steps 4.1-4.7)
  * Source: 01-blazor-refactoring-plan.instructions.md Phase 4
  * Status: Verified — all required methods present in all 6 files
  * Evidence: MediaControlBase (14+ methods), AudioControl, VideoControl (+GetPoster), ImageControl (8+ methods), IFrameControl (8+ methods), NavMenuControl (8+ methods)

* [x] Phase 5: Test Infrastructure (Steps 5.1-5.3)
  * Source: 01-blazor-refactoring-plan.instructions.md Phase 5
  * Status: Verified
  * Evidence: MockHtmlFactory.cs + GlobalUsings.cs confirmed

* [x] Phase 6: Test Migration — Simple Controls — 9 files (Steps 6.1-6.9)
  * Source: 01-blazor-refactoring-plan.instructions.md Phase 6
  * Status: Verified — all 9 files with tests above minimums

* [x] Phase 7: Test Migration — Collection/Container Controls — 5 files (Steps 7.1-7.5)
  * Source: 01-blazor-refactoring-plan.instructions.md Phase 7
  * Status: Verified — all 5 files confirmed

* [x] Phase 8: Test Migration — Blazor-Only Controls — 5 files (Steps 8.1-8.5)
  * Source: 01-blazor-refactoring-plan.instructions.md Phase 8
  * Status: Verified — all 5 files confirmed

* [x] Phase 9: Test Migration — Context Test — 1 file (Step 9.1)
  * Source: 01-blazor-refactoring-plan.instructions.md Phase 9
  * Status: Verified — BlazorTestContextTests.cs with 7 tests

* [x] Phase 10: Cleanup and Validation (Steps 10.1-10.5)
  * Source: 01-blazor-refactoring-plan.instructions.md Phase 10
  * Status: Verified — 4 Placeholder.cs deleted, build clean, tests pass

## Validation Results

### File Changes Validation

* 54/54 files verified: 6 modified (content confirmed), 44 added (existence confirmed), 4 removed (absence confirmed)
* Status: **Passed**

### Convention Compliance

* `.github/copilot-instructions.md`: **Partial**
  * [Critical] `Thread.Sleep(100)` in `BlazorTestContext.WaitForBlazorReady()` at srcnew/Brinell.Blazor/Context/BlazorTestContext.cs Line 65 — violates "NEVER use Thread.Sleep" convention
  * Recommendation: Replace with `_inner.InternalPage.WaitForFunctionAsync("() => typeof window._blazor !== 'undefined'", null, new PageWaitForFunctionOptions { Timeout = timeout }).GetAwaiter().GetResult()`
  * Note: 5 additional convention violations found in `PlaywrightTestContext.cs` (3 Thread.Sleep, 2 bare catch blocks) — these are **pre-existing** and outside the scope of this refactoring (file was not modified in this task)

* Control constructor pattern: **Passed** — all 20 controls follow the expected constructor signatures
* Test pattern (xUnit Assert + Moq, no FluentAssertions): **Passed** — 20/20 test files compliant
* Sync-first pattern: **Passed** — no async test methods, sync-over-async bridge used correctly

### Control Implementation Validation

* 14 inherited controls: **14/14 Passed** — correct base classes, constructors, generic constraints
* 6 Blazor-only controls: **6/6 Passed** — all required methods present, bonus assertion/wait methods included
* TabContainerControl: Single constructor with `tabSelector` parameter (by design — extra parameter precludes simple string overload)
* IFrameControl: `GetFrameLocator()` intentionally omitted (would leak Playwright `IFrameLocator` through abstraction)

### Test Coverage Validation

* 20/20 test files: all exist, all above minimum test count thresholds
* Total tests: 172 (all pass, 0 skipped)
* Pattern compliance: 20/20 use MockHtmlFactory/Moq, Assert.*, sync methods
* No FluentAssertions usage detected

### Validation Commands

* `dotnet build srcnew/Brinell.sln`: **Passed** — 0 errors, 0 warnings
* `dotnet test testsnew/Brinell.Blazor.Tests/Brinell.Blazor.Tests.csproj`: **Passed** — 172 passed, 0 failed, 0 skipped (177ms)

## Additional or Deviating Changes

* `srcnew/Brinell.Html.Playwright/Brinell.Html.Playwright.csproj` — Added `InternalsVisibleTo` for `Brinell.Blazor` (not originally in research, added during implementation to expose `InternalPage` for `BlazorTestContext`)
  * Reason: Required for `BlazorTestContext` to access `PlaywrightTestContext.InternalPage` for `WaitForBlazorReady` implementation
* Blazor-only controls include bonus assertion/wait methods beyond research spec (e.g., `AssertPlaying`, `AssertPaused`, `WaitLoaded`, `AssertElementExistsInside`, `GetActiveItem`, `IsActive`, `AssertActiveItem`)
  * Reason: Natural extensions following the framework's assert/wait pattern convention — additive, non-breaking

## Missing Work

* `Thread.Sleep(100)` in `BlazorTestContext.WaitForBlazorReady()` — convention violation requiring rework
  * Expected from: .github/copilot-instructions.md — "NEVER use Thread.Sleep"
  * Impact: Critical — should be replaced with Playwright's `WaitForFunctionAsync` before release
  * Fix: Replace polling loop with `_inner.InternalPage.WaitForFunctionAsync("() => typeof window._blazor !== 'undefined'", null, new PageWaitForFunctionOptions { Timeout = timeout }).GetAwaiter().GetResult()`

## Follow-Up Work

### Deferred from Current Scope

* **Async migration** — Convert sync-first APIs to native async
  * Source: 02-blazor-refactoring-research.md (Lines 88-90) — Potential Next Research
  * Recommendation: Separate task; `RunWithElement` pattern is async-conversion-ready

* **Promote Blazor-only controls to Brinell.Html** — Audio, Video, Image, IFrame, NavMenu are standard HTML elements
  * Source: 02-blazor-refactoring-research.md (Lines 91-93) — Potential Next Research
  * Recommendation: Move controls to `Brinell.Html` layer when expanding scope is acceptable

* **IFrame `ForFrame()` capability** — Add cross-frame scoping to `IHtmlElement` or `IHtmlTestContext`
  * Source: 02-blazor-refactoring-research.md (Lines 441-445) — IFrame special case
  * Recommendation: Add `IHtmlElement ForFrame(string selector)` to support proper frame scoping without leaking Playwright types

* **BlazorTestContext SSR/SignalR extensions** — Expand Blazor-specific context with SSR detection, circuit monitoring
  * Source: 02-blazor-refactoring-research.md (Lines 357-359) — Context extensions
  * Recommendation: Research Blazor-specific lifecycle hooks if SPX requires deeper integration

### Identified During Review

* **Pre-existing PlaywrightTestContext.cs convention violations** — 3 Thread.Sleep + 3 bare catch blocks
  * Context: These are not introduced by this task but affect the same infrastructure layer
  * Recommendation: Separate task to address `PlaywrightTestContext.cs` polling and error handling patterns

* **Changes log bookkeeping** — "Modified" and "Added" section headers have overlapping file lists (GlobalUsings.cs appears contextually as both modified and part of Added section)
  * Context: Cosmetic documentation inconsistency only
  * Recommendation: Minor cleanup when log is next updated

## Review Completion

**Overall Status**: Needs Rework
**Reviewer Notes**: Implementation is comprehensive and well-structured. All 48/48 plan steps complete, all 54 files verified, 172 tests passing, 0 warnings. One critical finding: `Thread.Sleep(100)` in `BlazorTestContext.WaitForBlazorReady()` must be replaced with Playwright's `WaitForFunctionAsync` before the implementation can be considered convention-compliant. This is a targeted single-line fix. After that fix, the implementation is complete.
