---
applyTo: '.copilot-tracking/Task/01_WpfWinFormsMigration/changes/01-wpf-winforms-migration-changes.md'
---
<!-- markdownlint-disable-file -->
# Implementation Plan: Migrate Brinell.Wpf and Brinell.WinForms to srcnew

## Overview

Port all WPF (13 controls) and WinForms (16 controls) implementations from `src/` to `srcnew/` following the new generic `TScope` architecture, with fully independent FlaUI driver code inline per platform, matching the `srcnew/Brinell.Maui` reference architecture.

## Objectives

* Fill existing `srcnew/Brinell.Wpf` shell with complete platform interfaces, FlaUI driver, context, pages, control bases, 13 concrete controls, and testing base
* Fill existing `srcnew/Brinell.WinForms` shell with complete platform interfaces, FlaUI driver, context, pages, control bases, 16 concrete controls, and testing base
* All controls implement new Core generic interfaces (`IControlObject<TScope>`, etc.) with fluent `TScope` chaining
* Full solution `srcnew/Brinell.sln` builds with zero errors and zero warnings
* Port existing sample UI tests to `testsnew/` and update sample project references

## Context Summary

### Project Files

* `srcnew/Brinell.Wpf/` - Existing shell project with placeholder files in Context/, Controls/, Pages/, Testing/
* `srcnew/Brinell.WinForms/` - Existing shell project with placeholder files in Context/, Controls/, Pages/, Testing/
* `srcnew/Brinell.Maui/` - Reference architecture (71+ files) to replicate for WPF/WinForms
* `srcnew/Brinell.Maui.FlaUI/` - FlaUI driver reference implementation (5 files) to adapt per-platform
* `srcnew/Brinell.Core/` - Shared interfaces and `ControlObjectBase<TScope>`
* `src/Brinell.FlaUI/` - Old shared FlaUI base classes (13 files) being replaced
* `src/Brinell.Wpf/` - Old WPF controls (17 files) to port
* `src/Brinell.WinForms/` - Old WinForms controls (18 files) to port

### References

* .copilot-tracking/Task/01_WpfWinFormsMigration/research/02-wpf-winforms-migration-research.md - Full architecture analysis
* .copilot-tracking/Task/01_WpfWinFormsMigration/research/01-wpf-winforms-migration-research-brief.md - Locked decisions and scope
* .copilot-tracking/Task/01_WpfWinFormsMigration/questions/01-wpf-winforms-migration-questions.md - User-validated Q&A

### Standards References

* #file:../../.github/copilot-instructions.md - Brinell framework conventions (no Thread.Sleep, no empty catches, wait for conditions)

## Implementation Checklist

### [x] Implementation Phase 1: WPF Platform Interfaces

<!-- parallelizable: false -->

* [x] Step 1.1: Create `srcnew/Brinell.Wpf/Interfaces/` directory with 8 interface files
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 16-40)
* [x] Step 1.2: Create `srcnew/Brinell.Wpf/GlobalUsings.cs` and `srcnew/Brinell.Wpf/ObjectBase.cs`
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 41-53)

### [x] Implementation Phase 2: WPF FlaUI Driver

<!-- parallelizable: false -->

* [x] Step 2.1: Create `srcnew/Brinell.Wpf/FlaUI/` directory with FlaUIWpfDriver, FlaUIWpfElement, LocatorExtensions
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 59-101)

### [x] Implementation Phase 3: WPF Context and Pages

<!-- parallelizable: false -->

* [x] Step 3.1: Replace `Context/Placeholder.cs` with `WpfTestContext.cs` and `WpfTestContextOptions.cs`
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 107-134)
* [x] Step 3.2: Replace `Pages/Placeholder.cs` with `PageObjectBase.cs`
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 136-160)

### [x] Implementation Phase 4: WPF Control Base Classes

<!-- parallelizable: false -->

* [x] Step 4.1: Replace `Controls/Placeholder.cs` with 6 base classes: ControlBase, ClickableControlBase, ToggleControlBase, EditableTextControlBase, RangeControlBase, SelectorControlBase
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 166-221)

### [x] Implementation Phase 5: WPF Concrete Controls

<!-- parallelizable: false -->

* [x] Step 5.1: Create 13 WPF control files in `srcnew/Brinell.Wpf/Controls/`
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 227-257)
* [x] Step 5.2: Validate WPF project builds with zero errors
  * Run `dotnet build srcnew/Brinell.Wpf/Brinell.Wpf.csproj`

### [x] Implementation Phase 6: WPF Testing Base

<!-- parallelizable: false -->

* [x] Step 6.1: Replace `Testing/Placeholder.cs` with `WpfTestFixtureBase.cs`
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 263-285)
* [x] Step 6.2: Validate full WPF project builds
  * Run `dotnet build srcnew/Brinell.Wpf/Brinell.Wpf.csproj` — zero errors, zero warnings

### [x] Implementation Phase 7: WinForms Platform Interfaces

<!-- parallelizable: false -->

* [x] Step 7.1: Create `srcnew/Brinell.WinForms/Interfaces/` directory with 8 interface files (mirror WPF with WinForms naming)
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 291-313)
* [x] Step 7.2: Create `srcnew/Brinell.WinForms/GlobalUsings.cs` and `srcnew/Brinell.WinForms/ObjectBase.cs`
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 315-325)

### [x] Implementation Phase 8: WinForms FlaUI Driver

<!-- parallelizable: false -->

* [x] Step 8.1: Create `srcnew/Brinell.WinForms/FlaUI/` directory with FlaUIWinFormsDriver, FlaUIWinFormsElement, LocatorExtensions
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 331-345)

### [x] Implementation Phase 9: WinForms Context, Pages, Controls, Testing

<!-- parallelizable: false -->

* [x] Step 9.1: Replace `Context/Placeholder.cs` with `WinFormsTestContext.cs` and `WinFormsTestContextOptions.cs`
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 351-364)
* [x] Step 9.2: Replace `Pages/Placeholder.cs` with `PageObjectBase.cs`
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 366-377)
* [x] Step 9.3: Replace `Controls/Placeholder.cs` with 6 base classes + 16 concrete controls
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 379-413)
* [x] Step 9.4: Replace `Testing/Placeholder.cs` with `WinFormsTestFixtureBase.cs`
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 415-426)
* [x] Step 9.5: Validate full WinForms project builds
  * Run `dotnet build srcnew/Brinell.WinForms/Brinell.WinForms.csproj` — zero errors, zero warnings

### [x] Implementation Phase 10: Tests and Samples

<!-- parallelizable: false -->

* [x] Step 10.1: Port WPF sample UI tests to `testsnew/Brinell.Wpf.UITests/`
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 432-454)
* [x] Step 10.2: Port WinForms sample UI tests to `testsnew/Brinell.WinForms.UITests/`
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 456-477)
* [x] Step 10.3: Update sample project references from `src/` to `srcnew/`
  * Details: .copilot-tracking/Task/01_WpfWinFormsMigration/details/01-wpf-winforms-migration-details.md (Lines 479-492)

### [x] Implementation Phase 11: Validation

<!-- parallelizable: false -->

* [x] Step 11.1: Run full solution build
  * Execute `dotnet build srcnew/Brinell.sln` — WPF/WinForms: zero errors, zero warnings (32 pre-existing Maui test errors unrelated to this task)
* [x] Step 11.2: Run test project builds
  * Execute `dotnet build testsnew/Brinell.Wpf.Tests/Brinell.Wpf.Tests.csproj` — 0 errors 0 warnings ✓
  * Execute `dotnet build testsnew/Brinell.Wpf.UITests/Brinell.Wpf.UITests.csproj` — 0 errors 0 warnings ✓
  * Execute `dotnet build testsnew/Brinell.WinForms.Tests/Brinell.WinForms.Tests.csproj` — 0 errors 0 warnings ✓
  * Execute `dotnet build testsnew/Brinell.WinForms.UITests/Brinell.WinForms.UITests.csproj` — 0 errors 0 warnings ✓
* [x] Step 11.3: Fix minor validation issues
  * Fixed CS0108 property-hides-factory-method: renamed PasswordBox→PasswordField, ProgressBar→ProgressBarField
  * Fixed IntPtr? nullable assertions in unit tests
* [x] Step 11.4: Report blocking issues
  * Pre-existing: 32 errors in Brinell.Maui.Tests/Brinell.Maui.UITests (MauiPageObjectBase, MauiControlBase etc. not found) — unrelated to WPF/WinForms migration

## Dependencies

* .NET SDK 10.0 preview (from global.json)
* FlaUI.Core and FlaUI.UIA3 NuGet packages (already referenced in csproj)
* Brinell.Core project (srcnew/) — provides all generic interfaces
* Existing shell projects in srcnew/ — Brinell.Wpf.csproj, Brinell.WinForms.csproj
* Existing test project shells in testsnew/ — already wired with GlobalUsings

## Success Criteria

* `dotnet build srcnew/Brinell.sln` completes with zero errors and zero warnings
* `srcnew/Brinell.Wpf/` contains interfaces, FlaUI driver, context, pages, 6 control bases, 13 concrete controls, and testing base
* `srcnew/Brinell.WinForms/` contains interfaces, FlaUI driver, context, pages, 6 control bases, 16 concrete controls, and testing base
* All controls implement `IControlObject<TScope>` or appropriate subinterfaces with fluent `TScope` returns
* Test projects in `testsnew/` build successfully with ported test classes
* No references to old `src/Brinell.FlaUI` from new projects
