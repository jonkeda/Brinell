# Brinell Docs Improvement Research

Date: 2026-06-05

## Executive Summary

Brinell has enough documentation volume, but it does not yet have a clean
documentation system. The current docs mix current `srcnew/` and `testsnew/`
architecture notes with older Oravey-era examples, stale numbered-doc links,
missing instruction references, and duplicated guidance. The most valuable
improvement is not to add more docs. It is to reduce and sharpen the docs so
there is one obvious path for each reader:

- users: install, write one test, run one platform;
- maintainers: understand architecture, add a control, run verification;
- agents: read a short local rule file, then follow current source-of-truth docs.

Recommended direction:

1. Fix the entry points first: `README.md`, `docs/README.md`, `AGENTS.md`, and
   `.github/copilot-instructions.md`.
2. Consolidate overlapping architecture docs into a smaller source-of-truth set.
3. Archive or delete old numbered-doc, Oravey, `.cnv2`, and Exact.Construction
   references unless those external files are restored and intentionally part of
   Brinell work.
4. Keep run guides and control docs, but make them generated or checked against
   `srcnew/`, `testsnew/`, and `Brinell.sln`.
5. Treat `.my/reports` as planning/research only; do not move runtime or user
   docs there.

## Research Evidence

Local audit scope:

- `README.md`, `AGENTS.md`, `.github/copilot-instructions.md`
- all files under `docs/`
- current project layout under `srcnew/`, `testsnew/`, and `samples/`
- existing planning report style under `.my/reports/`

Key findings:

| Finding | Evidence |
| --- | --- |
| `docs/` is large enough to need pruning | 66 markdown files, about 10,479 lines |
| Docs carry many broken internal links | 67 broken relative links across `README.md`, `AGENTS.md`, and `docs/` |
| Root `README.md` is stale | links `.specs/README.md`, `docs/01-quick-start.md`, and `docs/02-framework-overview.md`, none of which exist |
| Old Oravey naming remains | 15 `Oravey` hits and 11 `UITestFramework` hits in docs/root markdown |
| Old numbered doc system remains | links such as `03-architecture.md`, `04-control-objects.md`, `15-test-writing-guide.md`, and `19-*` still appear |
| `AGENTS.md` has broken required links | six required `../.github/instructions/uitest-*.instructions.md` files do not exist in this checkout |
| `docs/ai-assistant-references.md` is mostly broken | 15 broken links, including `.cnv2`, `.tests`, and missing `.github/instructions` paths |
| `.github/copilot-instructions.md` appears corrupted/overgrown | 2,597 lines, 85 KB, 11 `GSD:BEGIN` markers, 673 `GSD:END` markers |
| Current package surface is wider than README shows | current `srcnew` package IDs include `Brinell.Maui.Appium`, `Brinell.Maui.FlaUI`, `Brinell.Blazor`, `Brinell.NativeAndroid`, `Brinell.Presenter`, and `Brinell.Uat` in addition to the packages in `README.md` |
| Some docs contradict current rules | docs say FluentAssertions is banned, but older docs still list it as a dependency or recommend it |
| Some strict instructions are impossible as written | instruction docs ban `Task.Delay`/`Thread.Sleep` anywhere, while current code has polling, host startup, debug, audio simulation, and UI driver cases that use delays |

## Current Documentation Shape

The docs tree currently has these rough sizes:

| Section | Files | Lines | Words | Recommendation |
| --- | ---: | ---: | ---: | --- |
| `docs/guides` | 8 | 5,670 | 16,638 | keep, but merge and refresh heavily |
| `docs/architecture` | 10 | 1,479 | 7,163 | keep fewer source-of-truth files |
| `docs/platform-guides` | 4 | 2,423 | 6,591 | keep, but validate commands and links |
| `docs/run` | 8 | 1,456 | 4,709 | keep as task-focused command pages |
| `docs/controls` | 17 | 783 | 3,773 | keep, preferably generated from source |
| `docs/specs` | 10 | 604 | 2,875 | archive implemented specs or add status |
| `docs/getting-started` | 2 | 736 | 2,141 | rewrite first; this is the front door |
| `docs/requirements` | 3 | 224 | 1,064 | keep only if aligned with active roadmap |
| `docs/design` | 2 | 78 | 477 | archive unless kept as sample app specs |

## Keep

Keep these, but refresh links and examples:

| Area | Why keep | Required changes |
| --- | --- | --- |
| `docs/README.md` | Good navigation hub | remove Oravey footer; make it the canonical table of contents |
| `docs/getting-started/quick-start.md` | Essential user entry point | rewrite from Brinell packages/namespaces; remove Oravey `ProjectReference` samples |
| `docs/getting-started/framework-overview.md` | Good conceptual entry point | replace Oravey diagrams and stale links; keep platform comparison |
| `docs/architecture/stack.md` | Best current dependency snapshot | update package versions from `Directory.Packages.props`; remove any stale notes |
| `docs/architecture/decisions.md` | Useful maintainer source of truth | keep active decisions; add date/status per decision |
| `docs/architecture/testing.md` | Useful current test guidance | update artifact paths to current `TestArtifactPathProvider` design |
| `docs/architecture/structure.md` | Good codebase map | validate against actual `srcnew/`, `testsnew/`, and sample projects |
| `docs/controls/*` | Valuable API reference | generate/check from `srcnew/Brinell.Core/Interfaces` and platform controls |
| `docs/run/*` | Practical command docs | validate each command and make paths relative to Brinell root |
| `docs/guides/uat-template-guide.md` | Current UAT feature needs docs | keep near `Brinell.Uat` examples and UAT config tests |
| `.my/reports/*` | Useful design/research trail | keep as non-user-facing planning notes |

## Merge

These should not all remain separate long-form docs:

| Current docs | Merge target | Reason |
| --- | --- | --- |
| `docs/architecture/architecture.md`, `core-architecture.md`, `project-structure.md`, `structure.md` | `docs/architecture/overview.md` plus `docs/architecture/structure.md` | too much overlap around layers, folders, and dependency rules |
| `docs/guides/test-writing-guide.md`, `best-practices.md`, parts of `troubleshooting.md` | `docs/guides/test-writing.md` and `docs/guides/troubleshooting.md` | repeated wait/assertion guidance and stale numbered links |
| `docs/guides/interface-usage-guide.md`, `controls/interfaces.md`, `controls/classes/*.md` | generated `docs/controls/` reference plus a shorter usage guide | interface docs should not drift from source |
| `docs/platform-guides/*` and `docs/run/*` | keep both, but define roles | platform guide explains concepts; run guide gives commands only |
| `BREAKING-CHANGES-POLICY.md` and `VERSIONING.md` | keep root files, but cross-link from docs | release policy belongs at root; avoid duplicating it under docs |

## Archive Or Remove

Use `docs/archive/` for historically useful notes. Delete only obvious dead
links and copied scaffolding.

| Item | Recommendation | Why |
| --- | --- | --- |
| Old numbered-doc links (`02-framework-overview.md`, `15-test-writing-guide.md`, etc.) | remove or map to current paths | these files do not exist |
| Oravey examples in quick start and overview | replace, then remove | public docs should not teach old namespaces |
| `.specs` references | remove or replace with `docs/specs` | `.specs/README.md` does not exist |
| `docs/ai-assistant-references.md` | rewrite or delete | most links point to missing `.github/instructions`, `.cnv2`, or `.tests` files |
| `docs/run/WINDOWS-TEST-RESULTS.md` | move to `.my/reports` or `docs/archive/results` | test result snapshots are not stable run docs |
| `docs/guides/vscode-install-guide.md` | archive unless actively maintained | tool setup guide is peripheral and likely stale |
| `docs/design/*` | archive unless sample app design is active | very short, not linked to implementation status |
| `docs/specs/*` | add status or archive implemented specs | "Active" specs in `docs/README.md` are not clearly active |
| `.github/copilot-instructions.md` repeated GSD block | delete repeated block | current file is too noisy to be a reliable instruction source |

## Recommended Documentation Model

Target tree:

```text
README.md
AGENTS.md
.github/
  copilot-instructions.md
docs/
  README.md
  getting-started/
    quick-start.md
    framework-overview.md
  architecture/
    overview.md
    structure.md
    stack.md
    testing.md
    decisions.md
  controls/
    index.md
    interfaces.md
    classes/
  guides/
    test-writing.md
    uat-template-guide.md
    troubleshooting.md
    migration.md
  platform-guides/
    maui.md
    playwright.md
    winforms.md
    wpf.md
    stride.md
  run/
    maui.md
    maui-android.md
    playwright.md
    html.md
    winforms.md
    wpf.md
  specs/
    README.md
    active/
    archive/
```

Rules for this model:

- `README.md` sells the framework and links to current docs only.
- `docs/README.md` is navigation, not narrative.
- `getting-started` uses package references and copy-paste examples that compile.
- `architecture` documents current code structure and decisions.
- `controls` is generated or checked from source.
- `guides` teaches patterns.
- `platform-guides` explains platform-specific APIs.
- `run` contains verified commands only.
- `specs` must have status: proposed, active, implemented, superseded, archived.

## Root README Changes

Recommended changes:

- Replace placeholder GitHub badge links using `YOUR_USERNAME`.
- Replace broken documentation links:
  - `.specs/README.md` -> `docs/specs/README.md` or remove until created.
  - `docs/01-quick-start.md` -> `docs/getting-started/quick-start.md`.
  - `docs/02-framework-overview.md` -> `docs/getting-started/framework-overview.md`.
- Update package table to match active package IDs:
  - `Brinell.Core`
  - `Brinell.Maui`
  - `Brinell.Maui.Appium`
  - `Brinell.Maui.FlaUI`
  - `Brinell.Maui.CommunityToolkit`
  - `Brinell.Wpf`
  - `Brinell.WinForms`
  - `Brinell.Html`
  - `Brinell.Html.Playwright`
  - `Brinell.Blazor`
  - `Brinell.Stride`
  - `Brinell.Automation`
  - `Brinell.Mocking`
  - `Brinell.NativeAndroid`
  - `Brinell.Uat`
  - `Brinell.Presenter`
- Make command examples use current project paths and target frameworks.

## AGENTS.md Recommendation

`AGENTS.md` should become the short, reliable local entry point. It should not
force agents to read missing files or unrelated Exact.Construction instructions
for Brinell-only work.

Recommended structure:

````markdown
# Brinell Agent Instructions

## Read First

- For Brinell work, read `.github/copilot-instructions.md`.
- For docs work, also read `docs/README.md` and keep links valid.
- For architecture/control/test work, read the matching source-of-truth doc:
  - `docs/architecture/structure.md`
  - `docs/architecture/testing.md`
  - `docs/controls/index.md`
  - `docs/guides/uat-template-guide.md`

## Scope Split

- Brinell-only tasks stay inside `Brinell/`.
- Parent BodyCam tasks follow `../.github/copilot-instructions.md`.
- Exact.Construction or conversion references are optional and must be verified
  before use; do not list missing files as required.

## Coding Rules

- Prefer Brinell page objects and ControlObjects.
- Put repeated behavior in controls, not local test helpers.
- Wait for concrete UI state, not arbitrary time.
- Do not add public pointer/mouse APIs for routine actions.
- Use xUnit `Assert`; do not add FluentAssertions.
- Do not add new empty catches. Use helpers for tolerated UIA property probes.

## Docs Rules

- No Oravey namespaces in public docs.
- No numbered-doc links unless the files exist.
- Update `docs/README.md` when adding/moving docs.
- Run a markdown link check before finishing docs work.

## Verification

Commands are from the Brinell root:

```powershell
dotnet test testsnew\Brinell.Maui.Tests\Brinell.Maui.Tests.csproj -v:minimal /nr:false
dotnet build srcnew\Brinell.Maui.FlaUI\Brinell.Maui.FlaUI.csproj -f net10.0-windows -v:minimal /nr:false
```
````

Also fix the current verification commands. They start with `Brinell\...`,
which only works from the workspace root, not from the Brinell root where
`AGENTS.md` lives.

## Copilot Instructions Recommendation

Current `.github/copilot-instructions.md` should be replaced, not patched
incrementally. It is 85 KB and contains repeated GSD markers, which makes it
hard for agents to extract the actual Brinell rules.

Keep:

- no arbitrary sleeps as a fix;
- no new empty catches;
- no FluentAssertions;
- page object and ControlObject boundaries;
- docs/link rules;
- Mermaid rules only if Brinell still has Mermaid diagrams.

Remove or move:

- repeated GSD blocks;
- long Mermaid tutorial content from the main instruction file;
- stale generic project-management workflow;
- any instructions not specific to Brinell.

Better split:

| File | Purpose |
| --- | --- |
| `.github/copilot-instructions.md` | short global Brinell rules, under about 200 lines |
| `AGENTS.md` | local entry point and command shortcuts |
| `docs/guides/markdown-style.md` | optional Mermaid and markdown rendering rules |
| `docs/ai-assistant-references.md` | only if regenerated from real existing files |

## Instruction Rule Adjustments

The current strictness around delays and catches needs nuance so agents can
follow it without fighting the codebase.

Recommended wording for waits:

> Do not add arbitrary sleeps or longer waits to fix tests. Prefer Brinell wait,
> poll, readiness, and assertion APIs. A delay is acceptable only when it is a
> named polling interval, host startup loop, cancellation-aware retry, mock
> sensor/audio cadence, or explicit debug-only pause.

Recommended wording for catches:

> Do not add empty catches. If a platform API throws during optional property
> probing, contain that behavior in a named helper such as `TryGetAutomationId`
> or `TryReadProperty`, and return a clear fallback value.

Recommended wording for pointer input:

> Routine UI actions must use semantic operations and UI automation patterns
> first. Pointer input is opt-in for gesture-only surfaces and remains gated by
> `BRINELL_ALLOW_POINTER_INPUT`.

## Docs Quality Gates

Add lightweight checks before docs changes are considered done:

1. Internal markdown links resolve.
2. Root `README.md` and `docs/README.md` both link only to existing files.
3. Code snippets use `Brinell.*` namespaces, not Oravey.
4. Paths use `srcnew/`, `testsnew/`, or real sample project paths.
5. Package names match `PackageId` values in `srcnew/**/*.csproj`.
6. Version/package references match `Directory.Packages.props`.
7. Run commands are marked with their working directory.
8. Specs have status.
9. Agent instructions do not require missing files.

## Suggested Cleanup Plan

### Phase 1: Entry Point Repair

- Fix `README.md` broken links and package table.
- Fix `docs/README.md` footer and active spec list.
- Rewrite `AGENTS.md` around existing files.
- Replace `.github/copilot-instructions.md` with a short Brinell-specific file.

### Phase 2: Link and Naming Cleanup

- Remove old numbered-doc links.
- Replace Oravey namespaces and `UITestFramework` references.
- Remove missing `.cnv2`, `.tests`, and `.github/instructions` references from
  Brinell docs unless those directories are restored.
- Add a simple markdown link checker script under `tools/`.

### Phase 3: Consolidation

- Merge overlapping architecture docs.
- Shorten `interface-usage-guide.md` and move API detail into generated control
  reference docs.
- Move historical test result pages and implemented specs to archive.

### Phase 4: Verification

- Validate run guide commands on Windows.
- Validate non-UI tests with `dotnet test testsnew/Brinell.Core.Tests` and a
  representative platform unit test project.
- Add docs maintenance notes to `CONTRIBUTING.md`.

## Highest-Value First Edits

If only a small cleanup pass is possible, do these first:

1. Repair `README.md` doc links.
2. Remove Oravey from `quick-start.md`.
3. Rewrite `AGENTS.md` to stop requiring missing instruction files.
4. Delete the repeated GSD block from `.github/copilot-instructions.md`.
5. Remove or rewrite `docs/ai-assistant-references.md`.
6. Add `docs/specs/README.md` with status for every spec.

This gives humans and agents a trustworthy front door before deeper docs
consolidation starts.
