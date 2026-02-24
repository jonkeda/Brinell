---
applyTo: '.copilot-tracking/Task/01_FullAsyncMigration/changes/01-html-async-migration-changes.md'
defaultEnabled: true
---
<!-- markdownlint-disable-file -->
# Implementation Plan: HTML/Blazor Async Migration

## Overview

Add async interfaces and implementations to the Brinell HTML/Playwright stack, enabling test authors to write fully async tests while preserving the existing sync API unchanged.

## Objectives

* Create per-capability async interfaces in `Brinell.Html` mirroring each sync control capability
* Implement `IAsyncHtmlElement` on `PlaywrightHtmlElement` with native `await` (no `.GetAwaiter().GetResult()`)
* Add `RunWithElementAsync` and `PollAsync` hub methods to `ControlBase` and `ObjectBase`
* Add explicit async interface implementations to all 22 control/base classes
* Create an extension method bridge (`HtmlAsyncExtensions`) providing `ClickAsync`, `EnterAsync`, etc.
* Add async methods to `PlaywrightTestContext` (navigation, screenshot, element-finding)
* Add `NavigateToPageAsync` to `BlazorSampleTestBase`
* Create async counterpart tests demonstrating the async API end-to-end
* Fix 2 `Thread.Sleep` violations in `PlaywrightTestContext`
* Achieve zero `Thread.Sleep` calls across the HTML/Playwright stack

## Context Summary

### Project Files

* [srcnew/Brinell.Html/](srcnew/Brinell.Html/) — HTML abstraction layer (interfaces, controls, page objects)
* [srcnew/Brinell.Html.Playwright/](srcnew/Brinell.Html.Playwright/) — Playwright-backed implementation of HTML interfaces
* [testsnew/Brinell.Html.UITests/](testsnew/Brinell.Html.UITests/) — HTML UI tests (7 test classes, 4 page objects)
* [srcnew/Brinell.Core/](srcnew/Brinell.Core/) — Core interfaces (ZERO changes — hard constraint)

### References

* [.copilot-tracking/Task/01_FullAsyncMigration/research/02-html-async-migration-research.md](.copilot-tracking/Task/01_FullAsyncMigration/research/02-html-async-migration-research.md) — Full research document with decisions D1-D3
* [.copilot-tracking/Task/01_FullAsyncMigration/subagent/01-html-stack-inventory.md](.copilot-tracking/Task/01_FullAsyncMigration/subagent/01-html-stack-inventory.md) — Complete file/method inventory
* [.copilot-tracking/Task/01_FullAsyncMigration/subagent/02-explicit-interface-pattern.md](.copilot-tracking/Task/01_FullAsyncMigration/subagent/02-explicit-interface-pattern.md) — Proof of explicit interface implementation pattern
* [.copilot-tracking/Task/01_FullAsyncMigration/subagent/03-playwright-timeout-analysis.md](.copilot-tracking/Task/01_FullAsyncMigration/subagent/03-playwright-timeout-analysis.md) — Playwright timeout category analysis

### Standards References

* #file:../../.github/copilot-instructions.md — No Thread.Sleep, no empty catch blocks, no arbitrary waits
* #file:../../.github/instructions/markdown.instructions.md — Markdown formatting conventions
* #file:../../.github/instructions/writing-style.instructions.md — Writing style conventions

### Key Design Decisions

| # | Decision | Selection | Evidence |
|---|----------|-----------|----------|
| D1 | Naming collision resolution | Same names on separate interfaces + explicit implementation + `ClickAsync()` extension bridge | Research §D1 |
| D2 | Wait* async strategy | Hybrid: Playwright `WaitForAsync` for exists/visible; framework `PollAsync` with short inner timeouts for enabled/text/checked | Research §D2 |
| D3 | Element-level async | New `IAsyncHtmlElement` interface; `PlaywrightHtmlElement` implements both sync and async | Research §D3 |

## Implementation Checklist

### [x] Implementation Phase 0: Pre-migration Fixes

<!-- parallelizable: true -->

* [x] Step 0.1: Fix `Thread.Sleep(100)` in `PlaywrightTestContext.WaitReady()`
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 20-37)
* [x] Step 0.2: Fix `Thread.Sleep(100)` in `PlaywrightTestContext.FindElement()`
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 38-55)
* [x] Step 0.3: Validate — build `Brinell.Html.Playwright` and run existing sync tests
  * Confirm zero `Thread.Sleep` calls in the project
  * Run `dotnet build srcnew/Brinell.Html.Playwright/`
  * Run `dotnet test testsnew/Brinell.Html.UITests/`

### [x] Implementation Phase 1: Async Interfaces (Brinell.Html)

<!-- parallelizable: true -->

* [x] Step 1.1: Create `IAsyncHtmlElement` interface
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 60-124)
* [x] Step 1.2: Create `IHtmlAsyncControlObject<TScope>` interface
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 125-166)
* [x] Step 1.3: Create `IHtmlAsyncClickable<TScope>` interface
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 167-197)
* [x] Step 1.4: Create `IHtmlAsyncFocusable<TScope>` interface
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 198-224)
* [x] Step 1.5: Create `IHtmlAsyncToggle<TScope>` interface
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 225-257)
* [x] Step 1.6: Create `IHtmlAsyncEditable<TScope>` interface
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 258-289)
* [x] Step 1.7: Create `IHtmlAsyncSelector<TScope>` interface
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 290-319)
* [x] Step 1.8: Create `IHtmlAsyncRange<TScope>` interface
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 320-350)
* [x] Step 1.9: Create `IHtmlAsyncScrollable<TScope>` interface
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 351-376)
* [x] Step 1.10: Validate — build `Brinell.Html` to confirm interfaces compile
  * Run `dotnet build srcnew/Brinell.Html/`

### [x] Implementation Phase 2: Core Async Infrastructure

<!-- parallelizable: false -->

Depends on Phase 1 for interface types.

* [x] Step 2.1: Add `PollAsync` to `ObjectBase`
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 381-424)
* [x] Step 2.2: Add `RunWithElementAsync`, `FindAsyncElement`, `TryFindAsyncElement` to `ControlBase`
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 425-479)
* [x] Step 2.3: Add `IHtmlAsyncControlObject<TScope>` explicit implementation to `ControlBase`
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 480-672)
* [x] Step 2.4: Implement `IAsyncHtmlElement` on `PlaywrightHtmlElement`
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 673-820)
* [x] Step 2.5: Validate — build both `Brinell.Html` and `Brinell.Html.Playwright`
  * Run `dotnet build srcnew/Brinell.Html/`
  * Run `dotnet build srcnew/Brinell.Html.Playwright/`

### [x] Implementation Phase 3: Control Async Implementations

<!-- parallelizable: false -->

Depends on Phase 2. Work bottom-up through inheritance chain so each derived class can use `RunWithElementAsync`.

* [x] Step 3.1: Add `IHtmlAsyncClickable<TScope>` explicit implementation to `Control<TScope>`
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 825-884)
* [x] Step 3.2: Add async to `ClickableControlBase<TScope>` (DoubleClick, RightClick, Hover)
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 885-899)
* [x] Step 3.3: Add `IHtmlAsyncFocusable<TScope>` to `FocusableControlBase<TScope>`
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 900-945)
* [x] Step 3.4: Add `IHtmlAsyncToggle<TScope>` to `ToggleControlBase<TScope>`
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 946-1031)
* [x] Step 3.5: Add async to `CheckBoxControl`, `RadioButtonControl`
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 1032-1051)
* [x] Step 3.6: Add `IHtmlAsyncEditable<TScope>` to `TextInputControl`, `TextAreaControl`
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 1052-1145)
* [x] Step 3.7: Add `IHtmlAsyncSelector<TScope>` to `SelectorControlBase`, `SelectControl`, `RadioGroupControl`
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 1146-1204)
* [x] Step 3.8: Add `IHtmlAsyncRange<TScope>` to `RangeControlBase` and derived controls
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 1205-1266)
* [x] Step 3.9: Add `IHtmlAsyncScrollable<TScope>` to `ScrollableControlBase`
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 1267-1310)
* [x] Step 3.10: Add async to leaf controls (`ButtonControl`, `LinkControl`, `LabelControl`, `ProgressControl`, `ListControl`, `TableControl`, `List`)
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 1311-1355)
* [x] Step 3.11: Add async to `ContainerBase`, `ScrollContainerControl`, `TabContainerControl`
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 1356-1388)
* [x] Step 3.12: Validate — build `Brinell.Html`
  * Run `dotnet build srcnew/Brinell.Html/`

### [x] Implementation Phase 4: Extension Method Bridge

<!-- parallelizable: false -->

Depends on Phase 3 (all async interfaces and implementations must exist for the extensions to reference).

* [x] Step 4.1: Create `HtmlAsyncExtensions` class with all `*Async` extension methods
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 1393-1668)
* [x] Step 4.2: Validate — build `Brinell.Html` confirming extensions compile
  * Run `dotnet build srcnew/Brinell.Html/`

### [x] Implementation Phase 5: PlaywrightTestContext + Page Object Async

<!-- parallelizable: false -->

Depends on Phase 2 (IAsyncHtmlElement).

* [x] Step 5.1: Add async methods to `PlaywrightTestContext` (NavigateToAsync, TakeScreenshotAsync, FindElementAsync, etc.)
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 1673-1744)
* [x] Step 5.2: Add async page methods to `HtmlPageObjectBase` (WaitLoadedAsync, AssertLoadedAsync, etc.)
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 1745-1803)
* [x] Step 5.3: Add `NavigateToPageAsync` to `BlazorSampleTestBase`
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 1804-1837)
* [x] Step 5.4: Validate — build all projects
  * Run `dotnet build srcnew/Brinell.Html.Playwright/`
  * Run `dotnet build testsnew/Brinell.Html.UITests/`

### [x] Implementation Phase 6: Test Migration

<!-- parallelizable: false -->

Depends on Phases 4 and 5.

* [x] Step 6.1: Add async versions of `ButtonControlTests`
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 1842-1890)
* [x] Step 6.2: Add async versions of `CounterPageTests`
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 1891-1932)
* [x] Step 6.3: Add async versions of remaining test classes (CheckBox, Select, TextInput, Login, LoginFlow)
  * Details: .copilot-tracking/Task/01_FullAsyncMigration/details/01-html-async-migration-details.md (Lines 1933-1954)
* [x] Step 6.4: Validate — build and run all tests
  * Run `dotnet build testsnew/Brinell.Html.UITests/`
  * Run `dotnet test testsnew/Brinell.Html.UITests/`

### [x] Implementation Phase 7: Final Validation

<!-- parallelizable: false -->

* [x] Step 7.1: Run full project validation
  * Build entire solution: `dotnet build srcnew/Brinell.sln`
  * Run all tests: `dotnet test testsnew/Brinell.Html.UITests/`
  * Verify zero `Thread.Sleep` calls: `grep -r "Thread.Sleep" srcnew/ testsnew/Brinell.Html.UITests/`
  * Verify existing sync tests still pass unchanged
* [x] Step 7.2: Fix minor validation issues
  * Iterate on build errors, warnings, and test failures
  * Apply fixes directly when corrections are straightforward
* [x] Step 7.3: Report blocking issues
  * Document issues requiring additional research
  * Provide next steps and recommended planning
  * Avoid large-scale fixes within this phase

## Dependencies

* .NET 8+ (async/await, `ConfigureAwait`)
* Microsoft.Playwright (Playwright async APIs)
* xUnit (async test method support via `async Task`)
* Brinell.Core interfaces (read-only dependency — no changes)

## Success Criteria

* Every HTML control has both sync (existing) and async (new) API surface
* Async path uses native `await` with zero `.GetAwaiter().GetResult()` calls
* Sync path stays identical — existing sync tests compile and pass without modification
* Extension methods provide `*Async` suffixed entry points for test ergonomics
* Zero `Thread.Sleep` calls in the HTML/Playwright codebase
* All new async tests pass end-to-end against the Blazor sample app
