# Idea Overview: Markdown-Driven UAT Runner

## Goal

Build a Brinell tool that helps run user acceptance tests from readable Markdown files.

The first target should be MAUI. Other UI technologies can follow later once the format, runner model, and execution experience are proven.

## Core Idea

UATs should be written in a special Markdown format that is easy for humans to read and easy for tooling to parse. A human should be able to open a file and understand the scenario, while the runner should be able to turn the same file into executable steps.

The runner should execute those UATs against existing Brinell page objects. Reflection may be enough for the first version, but the design should allow a stronger page-object registry, command catalog, or typed binding layer if reflection becomes too loose.

## First Product Shape

Create a MAUI application that can:

- Load one UAT Markdown file.
- Load a folder or set of UAT Markdown files.
- Show the parsed scenarios and steps.
- Run all selected UATs automatically.
- Let the user choose the execution speed.
- Let the user switch to manual step mode with Next buttons.
- Show pass, fail, skipped, and current-step status clearly.
- Surface useful failure diagnostics from Brinell.

## Proposed UAT Markdown Direction

The Markdown format should stay readable and predictable. A likely shape:

```md
# UAT: Create Customer

## Metadata

- App: Example.Maui
- Area: Customers
- Tags: smoke, customer

## Scenario: Create a new customer

Given I am on the Customers page
When I tap New Customer
And I enter "Ada Lovelace" into Customer Name
And I tap Save
Then I should see "Ada Lovelace" in the customer list
```

The exact wording can evolve, but the parser should avoid fragile free-form magic. Prefer a small command vocabulary that maps cleanly to page-object actions and assertions.

## Execution Model

The runner should parse Markdown into a structured UAT model:

- Suite
- Scenario
- Step
- Step type such as Given, When, Then
- Target page or control
- Action or assertion
- Arguments
- Tags and metadata

Execution should bind each step to Brinell page objects. Initial options:

- Reflection over page-object methods and control objects.
- A registry of allowed actions and assertions.
- Attributes on page-object methods to expose stable UAT commands.
- A typed command catalog generated from page objects.

Reflection is attractive for speed, but a command catalog may be safer because UAT wording becomes a supported surface instead of an accidental method-name dependency.

## MAUI First

The first implementation should focus on MAUI because that is the immediate platform. Keep the core model technology-neutral where practical, but do not block the MAUI version by over-generalizing early.

Suggested layering:

- Markdown parser and UAT model in a neutral core library.
- Brinell MAUI execution adapter.
- MAUI runner application for loading, selecting, and running UATs.
- Later adapters for other UI technologies.

## Runner UX

The MAUI app should support two execution modes:

- Auto run: executes selected scenarios with a configurable delay between steps.
- Step run: waits for the user to press Next before each step.

Useful controls:

- Open file.
- Open folder.
- Run selected.
- Run all.
- Pause.
- Stop.
- Next step.
- Speed selector.
- Filter by tag or status.

Useful displays:

- Loaded UAT files.
- Scenario list.
- Current scenario.
- Current step.
- Step result timeline.
- Failure details.
- Screenshots or diagnostics when available.

## Open Questions

- Should the Markdown format be closer to Gherkin, or should it be Brinell-specific from the start?
- Should commands bind directly to page-object method names, or to explicit UAT command names?
- How much validation should happen before execution starts?
- Should the runner support variables, test data tables, and reusable step groups in version one?
- How should application startup, login, and environment setup be represented?

## Initial Slice

1. Define a minimal UAT Markdown grammar.
2. Parse one Markdown file into a structured model.
3. Bind a tiny command set to MAUI page objects.
4. Run one scenario in automatic mode.
5. Add step mode with a Next button.
6. Add file/folder loading.
7. Add failure reporting and diagnostics.

The main success condition is simple: a readable Markdown UAT can drive a real MAUI page object flow through Brinell, either automatically or one step at a time.
