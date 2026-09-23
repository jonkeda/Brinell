# Development Roadmap

Status: Delivered through Phase 13; Phase 14 open, Phase 15 partly open
Date: 2026-07-07, status pass 2026-09-21
Area: `srcnew/Brinell.Uat`, `srcnew/Brinell.Presenter`
Related:

- [Runner code binding](03%20runner%20code%20binding.md) — the binding model as shipped
- [UAT phrases and flows](../../docs/guides/uat-phrases-and-flows.md)
- [The Gherkin language, formalised](../uat/attribute-catalog/01-grammar.md)

This roadmap turns the Markdown-driven UAT runner idea into a first MAUI-capable implementation.

## Phase Status At A Glance

| Phase | State | Note |
| --- | --- | --- |
| 1 Core UAT model | ✅ | |
| 2 Markdown parser | ✅ | Grammar later formalised — see Phase 2 |
| 3 Grammar validation | ✅ | Diagnostics carry `UATB*` / `UATD*` codes |
| 4 Command binding core | ✅ | |
| 5 UAT attributes | ✅ **changed** | `[UatStep]` added; see Phase 5 |
| 6 Assembly discovery | ✅ | `uat.config.md` shipped, 8 in the tree |
| 7 Built-in MAUI commands | ✅ **changed** | Now discovered, not registered; see Phase 7 |
| 8 Execution engine | ✅ | Auto and step modes both ship |
| 9 Diagnostics | ✅ | |
| 10 MAUI runner app shell | ✅ | Shipped as `Brinell.Presenter` |
| 11 MAUI runner execution UI | ✅ | Presenter run toolbar, step mode, delay |
| 12 First real app smoke | ✅ | MAUI sample + an intentional failing scenario |
| 13 Reports | 🟡 partial | Per-scenario JSON + artifact manifest; no Markdown report, no export button |
| 14 CI and headless | 🟡 partial | Runs headless **as xUnit**; no CLI entry point, no CLI flags |
| 15 Hardening | 🟡 partial | Catalog browser and validate-without-running shipped in Presenter; no dry-run/parse-only mode |

The runner was not built as one MAUI app plus a CLI, as Phases 10–14 assumed. It
became **two front ends over one core**: `Brinell.Presenter` for interactive and
demo use, and xUnit test projects (`Brinell.*.Uat.Tests`) for CI. That is why
Phase 14's CLI never appeared — the CI half of its job was already done by
`dotnet test`, and only the standalone command-line entry point is missing.

## Goal

Build a MAUI-first UAT runner that can:

- Load UAT Markdown files.
- Parse the UAT grammar.
- Discover PageObjects, ControlObjects, and command handlers.
- Bind parsed steps to executable Brinell code.
- Run scenarios automatically or step by step.
- Show useful status and diagnostics.

## Architecture Boundary

Brinell remains the automation boundary.

The UAT runner does not implement platform automation for Windows, macOS, Android, or iOS. It calls discovered Brinell PageObjects, ControlObjects, and command handlers. The existing Brinell libraries own app launch, element lookup, input, waits, screenshots, diagnostics, and platform-specific behavior.

The runner UI should be built in MAUI so the runner can run on multiple operating systems. The UAT core should stay platform-neutral, but it should depend on Brinell abstractions for execution rather than introducing a separate automation adapter layer.

## Parser Strategy

Do not design an ANTLR parser for v1.

Use a Markdown document parser or small Markdown AST layer first, then run a strict UAT grammar pass over the parsed headings, paragraphs, step lines, and tables.

```text
Markdown text
  -> Markdown document model
  -> UAT section parser
  -> UAT validation
  -> UAT document model
```

ANTLR is better deferred unless the UAT format stops being Markdown or grows a separate expression language. The current grammar is mostly Markdown structure plus Gherkin-style step lines, so an AST/state-machine parser will be simpler to maintain and easier to align with human-authored Markdown.

## Phase 1: Core UAT Model

Create a neutral UAT core library.

Deliverables:

- `UatDocument`
- `UatScenario`
- `UatStep`
- `UatTable`
- `UatMetadata`
- Source location tracking with file path and line number.

Acceptance checks:

- A UAT file can be represented without execution concerns.
- Every parsed step keeps its original text and source location.
- Tables preserve headers, rows, and cell text.

## Phase 2: Markdown Parser

Implement the grammar from
[01-grammar.md](../uat/attribute-catalog/01-grammar.md).

> **Note (2026-09):** this phase originally cited `02 uat markdown grammar.md`,
> which was never written at the time. The grammar the parser actually accepts was
> written down afterwards, from the shipped parser, as `01-grammar.md` in the
> attribute-catalog folder — document grammar (§1), phrase mini-language (§2) and
> lexicon (§3).
> [02](02%20uat%20markdown%20grammar.md) now exists as a pointer to it.

Deliverables:

- Markdown document parsing through a Markdown AST or equivalent structured reader.
- UAT section parser over Markdown headings and blocks.
- Parser for `# UAT:`.
- Parser for `## Metadata`.
- Parser for `## Background`.
- Parser for `## Data:`.
- Parser for `## Scenario:`.
- Parser for `## Scenario Outline:`.
- Parser for `### Examples`.
- Parser for step-attached Markdown tables.
- Scenario outline expansion.

Acceptance checks:

- Valid example files parse successfully.
- Markdown is parsed as Markdown, not as raw ad-hoc line text.
- Invalid heading order reports a clear error.
- Malformed tables report line-numbered errors.
- Scenario outline parameters substitute into step text and tables.
- No ANTLR grammar is required for the first parser.

## Phase 3: Grammar Validation

Add validation before binding or execution.

Deliverables:

- Required section validation.
- Scenario and step validation.
- Table shape validation.
- Scenario outline validation.
- Tag placement validation.
- Friendly validation diagnostics for the runner UI.

Acceptance checks:

- Unknown or malformed UAT files fail before execution.
- Validation errors include file, line, and message.
- Multiple validation errors can be returned together.

## Phase 4: Command Binding Core

Implement the binding model from `03 runner code binding.md`.

Deliverables:

- `UatCommandCatalog`.
- `UatCommandPattern`.
- `UatStepInvocation`.
- Step pattern matching.
- Parameter extraction.
- Ambiguity detection.
- Required table declarations.
- Validation for unknown steps.

Acceptance checks:

- Exact command phrases match.
- Parameterized phrases match.
- Unknown steps fail validation.
- Ambiguous phrases fail validation.
- Step tables are available to command handlers.

## Phase 5: UAT Attributes

Add the first UAT attribute set.

Deliverables:

- `[UatName]`
- `[UatPhrase]`
- `[UatAction]`
- `[UatIgnore]`
- `[UatPhraseClass]` — added during implementation
- `[UatStep]` — **added 2026-09**, see below

Acceptance checks:

- `[UatName]` overrides inferred names.
- `[UatIgnore]` prevents discovery.
- `[UatPhrase]` methods become command catalog entries.
- No alias support exists in v1.

> **Superseded (2026-09):** this set had no way to declare a *built-in* phrase, so
> the built-in vocabulary went into hand-written tables instead — one in the
> runtime, one in the spec catalog, one in the Presenter. `[UatStep]` closed that
> gap: it lives in `Brinell.Core/Testing`, sits on the control interface method it
> calls, and all three tables were deleted. `[UatAction]` survives but marks a
> capability only — it contributes no phrase. See
> [03](03%20runner%20code%20binding.md) §UAT Attributes and
> [AD-011](../../.docs/decisions/ad-011-uat-vocabulary-is-attribute-declared.md).

## Phase 6: Assembly Discovery

Implement assembly registration and discovery.

Deliverables:

- `uat.config.md` parser.
- Parent-folder config search.
- Page assembly loading.
- Control assembly loading.
- Command assembly loading.
- PageObject discovery.
- ControlObject property discovery.
- Command handler discovery.

Acceptance checks:

- Runner can load a folder with `uat.config.md`.
- PageObject types are discovered from configured assemblies.
- Public ControlObject properties are discovered from pages.
- Duplicate page names fail discovery.
- Duplicate control names fail within a page.
- Missing required assemblies produce clear diagnostics.

## Phase 7: Built-In MAUI Commands

Implement the first MAUI command set.

These commands call existing Brinell PageObject and ControlObject APIs. They do not implement platform automation directly.

Deliverables — as shipped, 18 phrases:

- Page open and page assertion (`I am on the {page} page`, `I should be on the {page} page`).
- Text visible assertion (`I should see {text}`).
- Tap, enter, set, clear.
- Select, check, uncheck.
- Text equality and contains assertions.
- Visible / not visible, enabled, checked / unchecked, selected-value assertions.

Acceptance checks:

- Commands bind only to compatible controls.
- Commands produce structured step results.
- Failed commands include page/control/method diagnostics.

> **Superseded (2026-09):** two of the original deliverables were dropped rather
> than built. There is no **app running assertion** — a scenario starts from a
> page verb — and no built-in **table-driven form entry** or **table visible
> assertion**; a step that needs a table is a custom `[UatPhrase]` method reading
> `invocation.Table`. The "control value assertion" shipped as two phrases,
> `should equal` and `should contain`.
>
> More importantly, these are no longer "MAUI commands" at all. They are declared
> once on the `Brinell.Core` control interfaces and inherited by every technology
> that implements them — which is what made WPF, WinForms, Blazor, HTML and
> STRIDE UAT possible without re-declaring the vocabulary six times. See
> [11](11%20uat%20for%20brinell%20dotnet%20techs.md).
>
> The authoritative phrase list is
> [uat-phrases-and-flows.md](../../docs/guides/uat-phrases-and-flows.md)
> §Built-In Phrases.

## Phase 8: Execution Engine

Create the scenario execution runtime.

The execution engine invokes Brinell objects and records UAT-level results. Brinell remains responsible for the actual UI automation work.

Deliverables:

- Scenario runner.
- Suite runner.
- Current page context.
- Cancellation support.
- Pause support.
- Step-by-step execution mode.
- Automatic execution mode.
- Configurable delay between steps.
- Result model for passed, failed, skipped, waiting, and running.

Acceptance checks:

- One scenario can run end to end.
- Run can be stopped safely.
- Step mode waits before each step.
- Auto mode respects configured delay.
- Current page changes after page assertions/navigation.

## Phase 9: Diagnostics

Add runner diagnostics.

Deliverables:

- Per-step trace.
- Source file and line number in results.
- Resolved command pattern.
- Resolved handler.
- Resolved page.
- Resolved control.
- Failure details.
- Screenshot hook.
- Automation tree hook.
- Runtime log hook.

Acceptance checks:

- Every executed step has a trace.
- Failure output is useful without opening raw logs first.
- Diagnostics are available to the MAUI runner UI.

## Phase 10: MAUI Runner App Shell

Build the first MAUI runner application shell.

Deliverables:

- Open file.
- Open folder.
- Loaded file list.
- Scenario list.
- Step list.
- Run controls.
- Status summary.
- Diagnostics panel.

Acceptance checks:

- User can load one UAT file.
- User can load a folder of UAT files.
- Parsed scenarios appear in the UI.
- Validation errors appear in the UI.

## Phase 11: MAUI Runner Execution UI

Connect the UI to the execution engine.

Deliverables:

- Run selected.
- Run all.
- Pause.
- Stop.
- Next step.
- Auto/step mode selection.
- Speed or delay control.
- Live step status updates.
- Failure detail view.

Acceptance checks:

- User can run all selected scenarios automatically.
- User can run one step at a time.
- Current step is visible.
- Failure stops execution and shows diagnostics.
- Passed and failed counts update live.

## Phase 12: First Real App Smoke

Run against a small real or fixture MAUI app.

Use whichever Brinell MAUI target is easiest to run first. The target is a smoke-test choice, not a UAT runner architecture decision.

Deliverables:

- Example PageObjects.
- Example ControlObjects.
- Example `uat.config.md`.
- Example UAT Markdown file.
- One passing login/navigation scenario.
- One intentional failing scenario for diagnostics.

Acceptance checks:

- The runner can execute a real MAUI flow through Brinell.
- A passing scenario completes green.
- A failing scenario reports the failing step and resolved binding.

## Phase 13: Reports

Add basic report export.

Deliverables:

- Markdown report.
- JSON report.
- Per-scenario result summary.
- Per-step traces.
- Failure diagnostics references.

Acceptance checks:

- Reports can be exported after a run.
- Reports include enough detail for CI artifacts later.

> **Partly delivered (2026-09).** What ships: a per-scenario `__result.json`
> written to `UatConfig.Reporting.OutputDirectory` and registered as a
> `uat-scenario` artifact in the shared artifact manifest, plus screenshots and
> runtime traces gated by `ScreenshotOnFailure` / `IncludeRuntimeTrace` in
> `uat.config.md`. What does not: a **Markdown** report, and any "export report
> after a run" action — reports are a by-product of a run, not something a user
> asks for. See [reporting-artifacts.md](../../docs/guides/reporting-artifacts.md)
> §UAT Reporting.

## Phase 14: CI And Headless Preparation

Prepare the core for non-UI runner execution.

Deliverables:

- Headless runner entry point.
- Command-line file/folder selection.
- Command-line profile/config selection.
- Machine-readable exit codes.
- JSON result output.

Acceptance checks:

- A UAT suite can run without the MAUI runner UI.
- Failed scenarios produce a non-zero exit code.
- JSON output can be consumed by CI.

> **Partly delivered (2026-09) — and the shape changed.** All three acceptance
> checks pass, but none of them via a headless *runner*. A UAT suite runs in CI as
> an **xUnit test project**: `UatScenarioTestBase` turns `Scenarios/**/*.uat.md`
> into a theory, so `dotnet test` gives the headless run, the non-zero exit code
> and the JSON artifacts for free. Seven such projects exist, one per target; the
> gates are listed in [11](11%20uat%20for%20brinell%20dotnet%20techs.md) §Test Gates.
>
> Nothing on the deliverables list was built: there is no headless entry point, no
> command-line file/folder or profile selection. Whether a standalone CLI is still
> wanted is an open question, not a pending task — `dotnet test` covers CI, and
> the Presenter covers interactive use.

## Phase 15: Hardening

Improve reliability and authoring support.

Deliverables:

- Better validation messages.
- Command catalog browser.
- Dry-run mode.
- Parse-only mode.
- Binding preview mode.
- Improved table schema diagnostics.
- More Brinell synchronization integration.

Acceptance checks:

- Users can see why a step will bind before running it.
- Users can validate a folder without launching the app.
- Common binding mistakes produce direct, useful messages.

> **Partly delivered (2026-09).** All three acceptance checks are met by the
> Presenter rather than by modes on a runner: it has a **Command Catalog** tab and
> a **Discovery** tab (so you can see the vocabulary and the resolved pages and
> controls before running), and a **Validate** button that parses and binds a
> folder without launching the app under test. Binding failures carry
> `UATB001`–`UATB004`, and a missing page or control names what *was* found.
>
> Not built as such: named **dry-run** and **parse-only** modes, and improved
> table-schema diagnostics. Given that Validate covers the use case, these are
> probably obsolete rather than pending.

## Suggested First MVP

The smallest useful MVP is:

1. Core model.
2. Markdown parser.
3. Validation.
4. Manual command catalog.
5. Page/control registry.
6. Built-in tap, enter text, and page assertion commands.
7. Folder-level `uat.config.md`.
8. One scenario execution.
9. Step-by-step mode.
10. Basic diagnostics.
11. MAUI runner shell that uses Brinell abstractions for execution.

## Deferred

Defer these until after the first real MAUI smoke works:

- Aliases.
- General `if` statements.
- General `for` statements.
- Full Cucumber compatibility.
- Localised Gherkin keywords.
- ANTLR parser.
- Source generation.
- Rich report dashboards.
- UAT-owned platform automation adapters.
- CI polish.

## Implementation Order

Recommended order:

1. Build parser and validation without any UI.
2. Build command binding without any MAUI app.
3. Add discovery against small fixture page/control classes.
4. Add MAUI command handlers.
5. Run one scenario programmatically.
6. Build the MAUI runner shell.
7. Wire runner shell to parser, binder, and executor.
8. Add diagnostics and reports.

This keeps the hard pieces testable before the UI becomes involved.
