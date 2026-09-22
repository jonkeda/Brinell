# MVP Task Checklist

Status: Closed — every item delivered
Date: 2026-07-07, closed 2026-09-21
Area: `srcnew/Brinell.Uat`, `testsnew/Brinell.Uat.Tests`
Related:

- [Development roadmap](04%20development%20roadmap.md) — the phases this slice opened
- [UAT diagnostics and config hardening](08%20uat%20diagnostics%20and%20config%20hardening.md) — the slice that followed
- [Attribute-driven UAT catalog](../uat/attribute-catalog/README.md) — what replaced Slice 5's phrase model

This checklist defines the first implementation slice for the Markdown-driven UAT runner.

## MVP Target

Create a working UAT core that can parse, validate, and model UAT Markdown files before any MAUI runner UI is built.

The first slice should not execute real UI automation yet. It should prove that the UAT file format is concrete enough to become code.

## Slice 1: UAT Core

- [x] Add a `Brinell.Uat` core project.
- [x] Add a `Brinell.Uat.Tests` test project.
- [x] Define core models:
  - [x] `UatDocument`
  - [x] `UatMetadata`
  - [x] `UatScenario`
  - [x] `UatStep`
  - [x] `UatTable`
  - [x] `UatDiagnostic`
  - [x] `UatSourceLocation`
- [x] Parse `# UAT:`.
- [x] Parse `## Metadata`.
- [x] Parse `## Background`.
- [x] Parse `## Data:`.
- [x] Parse `## Scenario:`.
- [x] Parse `## Scenario Outline:`.
- [x] Parse `### Examples`.
- [x] Parse tags before scenarios.
- [x] Parse step-attached Markdown tables.
- [x] Expand scenario outlines.
- [x] Preserve source file and line numbers.

## Slice 2: Validation

- [x] Validate exactly one `# UAT:` heading.
- [x] Validate at least one scenario.
- [x] Validate scenario steps.
- [x] Validate metadata tables.
- [x] Validate Markdown table shape.
- [x] Validate scenario outline examples.
- [x] Validate outline parameter references.
- [x] Validate tag placement.
- [x] Return multiple diagnostics where possible.

## Slice 3: Tests

- [x] Add parser tests for a simple scenario.
- [x] Add parser tests for metadata.
- [x] Add parser tests for step tables.
- [x] Add parser tests for scenario outlines.
- [x] Add parser tests based on `05 simple page and tests example.md`.
- [x] Add parser tests based on `05b default naming page example.md`.
- [x] Add validation tests for malformed UAT files.

## Slice 4: Binding Preview

Defer until the parser and validation are green.

- [x] Add `UatCommandCatalog`.
- [x] Add `UatCommandPattern`.
- [x] Add `UatStepInvocation`.
- [x] Add exact phrase matching.
- [x] Add parameterized phrase matching.
- [x] Add unknown-step diagnostics.
- [x] Add ambiguity diagnostics.

## Slice 5: Discovery

Defer until binding preview works.

- [x] Add `[UatName]`.
- [x] Add `[UatPhrase]`.
- [x] Add `[UatAction]`.
- [x] Add `[UatIgnore]`.
- [x] Add default naming rules.
- [x] Add `uat.config.md` parsing.
- [x] Add PageObject discovery tests.
- [x] Add ControlObject discovery tests.

> **Superseded (2026-09):** this slice shipped as written, but the attribute set
> was incomplete in a way that only showed up later. None of these four can
> declare a *built-in* phrase, so the built-in vocabulary went into hand-written
> tables — and by the time there were three copies of it, the cost was obvious.
> `[UatStep]` was added to `Brinell.Core/Testing` and all three tables deleted.
> `[UatAction]` remains, but as a capability marker only; it contributes no
> phrase. See [AD-011](../../.docs/decisions/ad-011-uat-vocabulary-is-attribute-declared.md).

## Slice 6: Execution

Defer until discovery works.

- [x] Add runner execution model.
- [x] Add current page context.
- [x] Add step result model.
- [x] Add cancellation.
- [x] Add step-by-step execution mode.
- [x] Add basic diagnostics.

## Non-Goals For First Slice

- No MAUI runner UI.
- No real UI automation execution.
- No aliases.
- No `if` statements.
- No `for` statements.
- No ANTLR parser.
- No source generation.

## First Done Definition

The first slice is done when:

- A UAT Markdown string parses into a `UatDocument`.
- Valid examples from the design docs parse successfully.
- Invalid examples return line-numbered diagnostics.
- The UAT core test project runs without requiring MAUI, Appium, FlaUI, or a launched app.

## Closed

All six slices are delivered, and `Brinell.Uat.Tests` still runs without MAUI,
Appium, FlaUI or a launched app — the property this checklist was built around.

Every non-goal on the list above also held: no aliases, no `if`/`for`, no ANTLR,
no source generation. The one thing that changed is that "no source generation"
turned out not to mean "no discovery" —
[the vocabulary is now found by reflection over attributes](../uat/attribute-catalog/README.md),
which gets the benefit a generator would have given without adding a build step.

What came next, in order: the diagnostics and config work in
[08](08%20uat%20diagnostics%20and%20config%20hardening.md), the Presenter in
[09](09%20brinell%20presenter%20development%20plan.md) and
[10](10%20presenter%20tabbed%20tree%20redesign.md), the six-technology rollout in
[11](11%20uat%20for%20brinell%20dotnet%20techs.md), and the attribute-catalog
rework.
