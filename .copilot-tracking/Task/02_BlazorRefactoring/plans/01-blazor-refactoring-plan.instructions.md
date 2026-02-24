---
applyTo: '.copilot-tracking/Task/02_BlazorRefactoring/changes/01-blazor-refactoring-changes.md'
defaultEnabled: true
---
<!-- markdownlint-disable-file -->
# Implementation Plan: Blazor Refactoring to srcnew/testsnew

## Overview

Migrate `src/Brinell.Blazor/ControlObject6/` controls and `tests/Brinell.Blazor.Tests.ControlObject6/` tests to the new `srcnew/Brinell.Blazor/` and `testsnew/Brinell.Blazor.Tests/` architecture, inheriting from `srcnew/Brinell.Html` base classes with the CRTP `<TScope>` pattern.

## Objectives

* Implement all 22 controls in `srcnew/Brinell.Blazor/Controls/` — 14 as thin Html inheritors, 5 as Blazor-only controls, plus a `MediaControlBase` shared base
* Implement `BlazorTestContext`, `BlazorPageObjectBase`, and `BlazorTestFixtureBase` infrastructure classes
* Add `Evaluate<T>()` and `Evaluate()` to `IHtmlElement` interface and `PlaywrightHtmlElement` implementation
* Create `MockHtmlFactory` and migrate 20 test files to sync/xunit/IHtmlElement-mocking pattern
* Remove all placeholder files; ensure projects build and tests pass

## Context Summary

### Project Files

* `srcnew/Brinell.Blazor/` — Greenfield project with 4 placeholder-only subdirectories (Context/, Controls/, Pages/, Testing/)
* `testsnew/Brinell.Blazor.Tests/` — Scaffolded project with csproj + GlobalUsings.cs only
* `src/Brinell.Blazor/ControlObject6/Controls/` — 22 old async controls to migrate (source material)
* `tests/Brinell.Blazor.Tests.ControlObject6/` — 20 old test files to migrate (source material)

### References

* .copilot-tracking/Task/02_BlazorRefactoring/research/02-blazor-refactoring-research.md — Full research with decisions, technical scenarios, code examples
* .copilot-tracking/Task/02_BlazorRefactoring/subagent/01-html-architecture-research.md — Html control hierarchy
* .copilot-tracking/Task/02_BlazorRefactoring/subagent/03-old-controls-migration-map.md — Per-control migration mapping

### Standards References

* #file:../../.github/copilot-instructions.md — No Thread.Sleep, no empty catch blocks, sync-first

## Implementation Checklist

### [x] Implementation Phase 1: Foundation — IHtmlElement + Project References

<!-- parallelizable: false -->

* [x] Step 1.1: Add `Evaluate<T>()` and `Evaluate()` to `IHtmlElement` interface
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 17-38)
* [x] Step 1.2: Implement `Evaluate<T>()` and `Evaluate()` in `PlaywrightHtmlElement`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 40-60)
* [x] Step 1.3: Update `srcnew/Brinell.Blazor/Brinell.Blazor.csproj` — add `Brinell.Html` + `Brinell.Html.Playwright` references
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 62-87)
* [x] Step 1.4: Update `testsnew/Brinell.Blazor.Tests/Brinell.Blazor.Tests.csproj` — add `Brinell.Html` + `Brinell.Html.Playwright` references
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 89-120)
* [x] Step 1.5: Validate foundation changes build
  * Run `dotnet build` for `srcnew/Brinell.Html`, `srcnew/Brinell.Html.Playwright`, `srcnew/Brinell.Blazor`, `testsnew/Brinell.Blazor.Tests`

### [x] Implementation Phase 2: Infrastructure — Context, Page, Fixture

<!-- parallelizable: false -->

* [x] Step 2.1: Implement `BlazorTestContext` in `srcnew/Brinell.Blazor/Context/BlazorTestContext.cs`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 126-178)
* [x] Step 2.2: Implement `BlazorPageObjectBase<TSelf>` in `srcnew/Brinell.Blazor/Pages/BlazorPageObjectBase.cs`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 180-203)
* [x] Step 2.3: Implement `BlazorTestFixtureBase` in `srcnew/Brinell.Blazor/Testing/BlazorTestFixtureBase.cs`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 205-234)
* [x] Step 2.4: Validate infrastructure builds
  * Run `dotnet build srcnew/Brinell.Blazor`

### [x] Implementation Phase 3: Inherited Controls (14 thin re-exports)

<!-- parallelizable: true -->

* [x] Step 3.1: Implement `ButtonControl<TScope>` and `LinkControl<TScope>` (Buttons group)
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 240-262)
* [x] Step 3.2: Implement `CheckBoxControl<TScope>` and `RadioButtonControl<TScope>` (Toggle group)
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 264-273)
* [x] Step 3.3: Implement `TextInputControl<TScope>` and `TextAreaControl<TScope>` (Text group)
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 275-284)
* [x] Step 3.4: Implement `SelectControl<TScope>` (Selection group)
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 286-294)
* [x] Step 3.5: Implement `DateInputControl<TScope>` and `TimeInputControl<TScope>` (DateTime group)
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 296-305)
* [x] Step 3.6: Implement `RangeInputControl<TScope>` (Range group)
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 307-315)
* [x] Step 3.7: Implement `ListControl<TScope>`, `TableControl<TScope>`, `ProgressControl<TScope>` (Collection/Display group)
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 317-327)
* [x] Step 3.8: Implement `TabContainerControl<TParent, TScope>` (Container group — dual-generic)
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 329-356)

### [x] Implementation Phase 4: Blazor-Only Controls (6 files)

<!-- parallelizable: true -->

* [x] Step 4.1: Implement `MediaControlBase<TScope>` — shared base for Audio/Video
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 362-440)
* [x] Step 4.2: Implement `AudioControl<TScope>` extending `MediaControlBase`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 442-465)
* [x] Step 4.3: Implement `VideoControl<TScope>` extending `MediaControlBase`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 467-492)
* [x] Step 4.4: Implement `ImageControl<TScope>` — image-specific methods
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 494-554)
* [x] Step 4.5: Implement `IFrameControl<TScope>` — cross-frame interaction
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 556-632)
* [x] Step 4.6: Implement `NavMenuControl<TScope>` — navigation menu
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 634-746)
* [x] Step 4.7: Validate all controls build
  * Run `dotnet build srcnew/Brinell.Blazor`

### [x] Implementation Phase 5: Test Infrastructure

<!-- parallelizable: false -->

* [x] Step 5.1: Create `MockHtmlFactory` in `testsnew/Brinell.Blazor.Tests/Mocks/MockHtmlFactory.cs`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 752-841)
* [x] Step 5.2: Update `testsnew/Brinell.Blazor.Tests/GlobalUsings.cs` with new usings
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 843-873)
* [x] Step 5.3: Validate test infrastructure compiles
  * Run `dotnet build testsnew/Brinell.Blazor.Tests`

### [x] Implementation Phase 6: Test Migration — Simple Controls (9 files)

<!-- parallelizable: true -->

* [x] Step 6.1: Migrate `ButtonControlTests.cs`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 879-953)
* [x] Step 6.2: Migrate `CheckBoxControlTests.cs`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 946-953)
* [x] Step 6.3: Migrate `LinkControlTests.cs`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 946-953)
* [x] Step 6.4: Migrate `RadioButtonControlTests.cs`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 946-953)
* [x] Step 6.5: Migrate `DateInputControlTests.cs`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 946-953)
* [x] Step 6.6: Migrate `TimeInputControlTests.cs`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 946-953)
* [x] Step 6.7: Migrate `TextInputControlTests.cs` (renamed from InputControlTests)
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 946-953)
* [x] Step 6.8: Migrate `TextAreaControlTests.cs`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 946-953)
* [x] Step 6.9: Migrate `ProgressControlTests.cs`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 946-953)

### [x] Implementation Phase 7: Test Migration — Collection/Container Controls (5 files)

<!-- parallelizable: true -->

* [x] Step 7.1: Migrate `ListControlTests.cs`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 959-971)
* [x] Step 7.2: Migrate `TableControlTests.cs`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 959-971)
* [x] Step 7.3: Migrate `SelectControlTests.cs`
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 959-971)
* [x] Step 7.4: Migrate `RangeInputControlTests.cs` (renamed from RangeControlTests)
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 959-971)
* [x] Step 7.5: Migrate `TabContainerControlTests.cs` (renamed from TabControlTests)
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 973-993)

### [x] Implementation Phase 8: Test Migration — Blazor-Only Controls (5 files)

<!-- parallelizable: true -->

* [x] Step 8.1: Migrate `AudioControlTests.cs` — mock `Evaluate()` for media actions
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 999-1050)
* [x] Step 8.2: Migrate `VideoControlTests.cs` — similar to Audio + poster test
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 1052-1067)
* [x] Step 8.3: Migrate `ImageControlTests.cs` — mock `Evaluate<T>` for compound expressions
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 1069-1098)
* [x] Step 8.4: Migrate `IFrameControlTests.cs` — mock `Evaluate` for frame interactions
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 1100-1130)
* [x] Step 8.5: Migrate `NavMenuControlTests.cs` — mock `FindElements` for item discovery
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 1132-1179)

### [x] Implementation Phase 9: Test Migration — Context Test (1 file)

<!-- parallelizable: false -->

* [x] Step 9.1: Implement `BlazorTestContextTests.cs` — rewrite for new context
  * Details: .copilot-tracking/Task/02_BlazorRefactoring/details/01-blazor-refactoring-details.md (Lines 1185-1231)

### [x] Implementation Phase 10: Cleanup and Validation

<!-- parallelizable: false -->

* [x] Step 10.1: Delete all 4 Placeholder.cs files from `srcnew/Brinell.Blazor/`
  * Delete `srcnew/Brinell.Blazor/Context/Placeholder.cs`
  * Delete `srcnew/Brinell.Blazor/Controls/Placeholder.cs`
  * Delete `srcnew/Brinell.Blazor/Pages/Placeholder.cs`
  * Delete `srcnew/Brinell.Blazor/Testing/Placeholder.cs`
* [x] Step 10.2: Run full project build
  * `dotnet build srcnew/Brinell.sln`
* [x] Step 10.3: Run all unit tests
  * `dotnet test testsnew/Brinell.Blazor.Tests/Brinell.Blazor.Tests.csproj`
* [x] Step 10.4: Fix minor validation issues
  * No issues found — 0 warnings, 0 errors, 172 tests pass
* [x] Step 10.5: Report blocking issues
  * No blocking issues

## Dependencies

* `srcnew/Brinell.Html` — Base classes and interfaces (must already be implemented)
* `srcnew/Brinell.Html.Playwright` — `PlaywrightHtmlElement` and `PlaywrightTestContext` (must already be implemented)
* `srcnew/Brinell.Core` — Core abstractions (`Locator`, `IElement`, `ITestContext`)
* `Microsoft.Playwright` — Browser automation (NuGet package)
* `Moq` — Test mocking framework (NuGet package in test project)
* `xunit` — Test framework (via Directory.Build.props / Directory.Packages.props)

## Success Criteria

* `dotnet build srcnew/Brinell.sln` succeeds with no errors
* `dotnet test testsnew/Brinell.Blazor.Tests/` passes all tests
* All 22 controls are implemented in `srcnew/Brinell.Blazor/Controls/`
* Infrastructure classes (BlazorTestContext, BlazorPageObjectBase, BlazorTestFixtureBase) implemented
* `IHtmlElement` has `Evaluate<T>()` + `Evaluate()` methods
* No Placeholder.cs files remain in `srcnew/Brinell.Blazor/`
* 20 test files migrated + MockHtmlFactory created in `testsnew/Brinell.Blazor.Tests/`
