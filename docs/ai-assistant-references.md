---
title: AI Assistant References
description: Reference map for Brinell and Exact.Construction AI-agent instructions
---

## Purpose

This file is a pointer map for AI agents working across Brinell and Exact.Construction UITests. It keeps the authoritative rules in GitHub instruction files and avoids copying large instruction blocks into working notes.

## Brinell Framework Instructions

| File                                                                         | Use when                                                                           |
|------------------------------------------------------------------------------|------------------------------------------------------------------------------------|
| [Brinell Copilot Instructions](../.github/copilot-instructions.md)           | Changing Brinell framework code, tests, docs, waits, controls, drivers, or markdown |
| [Markdown Instructions](../.github/instructions/markdown.instructions.md)    | Creating or editing Brinell markdown                                               |
| [Commit Message Instructions](../.github/instructions/commit-message.instructions.md) | Preparing commits inside the Brinell submodule                                     |
| [Git Merge Instructions](../.github/instructions/git-merge.instructions.md)  | Handling merge/conflict work inside Brinell                                        |

## Exact.Construction UITest Instructions

These files live at the workspace root and apply to `MauiMobile/MAUI-Construction/Exact.Construction.UITests`.

| File                                                                                                             | Use when                                                         |
|------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------|
| [Exact.Construction Copilot Instructions](../../.github/copilot-instructions.md)                                 | Checking allowed project scope and general Construction rules    |
| [Brinell page and ControlObject rules](../../.github/instructions/uitest-brinell-page-controlobject.instructions.md) | Adding or changing UITest page objects, controls, or test methods |
| [Synchronization rules](../../.github/instructions/uitest-brinell-synchronization.instructions.md)               | Adding waits, readiness checks, or fixing flakiness              |
| [Cascading failure prevention](../../.github/instructions/uitest-cascading-failure-prevention.instructions.md)    | Registering pages, recovery, fixture/test-base behavior          |
| [Diagnostics and triage](../../.github/instructions/uitest-diagnostics-triage.instructions.md)                   | Investigating failing UI tests before changing code              |
| [Runtime auth and mock backend](../../.github/instructions/uitest-runtime-auth-mockbackend.instructions.md)       | Working with mock auth, WireMock, fixtures, and runtime modes    |
| [UITest scripts](../../.github/instructions/uitest-scripts.instructions.md)                                      | Running Exact.Construction UI tests                              |
| [xUnit test instructions](../../.github/instructions/test-unit-xunit.instructions.md)                            | Adding or changing unit and request-shape tests                  |

## Sibling Conversion Instructions

These files live in the sibling conversion repositories. Read them when a task
involves conversion fidelity, native Bouw7 behavior, pipeline phase order, or
subagent handoffs.

| File                                                                                                             | Use when                                                          |
|------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------|
| `E:/repos/Clay/ClaiConstructionMobile/.github/copilot-instructions.md`                                           | Applying the active Exact.Construction project-scope rules        |
| `E:/repos/Clay/ClaiConstructionMobile/.github/instructions/*.instructions.md`                                    | Applying local MAUI, test, Brinell, mock, and WireMock rules      |
| `E:/repos/Clay/ClayBouwMobile/.github/documentation/convert-flow-overview.md`                                    | Checking conversion phase order and quality gates                 |
| `E:/repos/Clay/ClayBouwMobile/.github/documentation/convert-feature-guide.md`                                    | Checking per-feature conversion flow, subagent matrix, and outputs |
| `E:/repos/Clay/ClayBouw/.github/agents/conversion/*.yaml`                                                        | Following the legacy subagent-heavy conversion orchestration model |
| [Local Exact.Construction Mobile instructions](../../.github/copilot-instructions.md)                            | Applying the local UITest/mock/WireMock rules                     |

## Current Conversion Factory References

| File                                                                                   | Use when                                                   |
|----------------------------------------------------------------------------------------|------------------------------------------------------------|
| [CNV2 factory](../../.cnv2/factory.md)                                                  | Planning or executing conversion packets                   |
| [CNV2 work queue](../../.cnv2/workqueue.md)                                             | Picking the active or next packet                          |
| [CNV2 current state](../../.cnv2/current-state.md)                                      | Checking validated flows and current risks                 |
| [CNV2 test strategy](../../.cnv2/test-strategy.md)                                      | Choosing focused UI, unit, WireMock, and regression commands |
| [Converted functionality matrix](../../.tests/converted-functionality-test-matrix.md)   | Checking converted-functionality coverage                  |

## Local Rule Summary

* Read the instruction files before implementing.
* Use Brinell page objects and ControlObjects for UI behavior.
* Promote repeated locator or interaction logic into controls.
* Do not add Exact-local direct mouse, geometry, or raw UIA helper code for routine actions.
* Do not add arbitrary waits or increase timeouts before diagnostics.
* Use mock auth and WireMock for current Exact.Construction smoke paths.
* Preserve diagnostics and inspect app logs before changing failing UI tests.
* Cross-check the conversion phases before claiming a module is fully converted.
