---
title: Brinell Agent Instructions
description: Local entry point for AI agents working in the Brinell submodule
---

# Brinell Agent Instructions

## Read First

For Brinell work, read:

- [.github/copilot-instructions.md](.github/copilot-instructions.md)
- [docs/README.md](docs/README.md)

Then read the focused doc for the task:

- architecture or project layout: [docs/architecture/structure.md](docs/architecture/structure.md)
- tests or artifacts: [docs/architecture/testing.md](docs/architecture/testing.md)
- controls: [docs/controls/index.md](docs/controls/index.md)
- UAT: [docs/guides/uat-template-guide.md](docs/guides/uat-template-guide.md)
- commands: [docs/run/build-and-test.md](docs/run/build-and-test.md)

The previous docs tree is preserved in [docs2](docs2/README.md). Treat it as an
archive, not the active source of truth.

## Scope

- Brinell-only work stays inside `Brinell/`.
- Parent BodyCam work follows `../.github/copilot-instructions.md`.
- Exact.Construction, `.cnv2`, or sibling conversion references are optional and
  task-specific; do not require missing files for normal Brinell work.

## Core Rules

- Build tests through Brinell page objects and ControlObjects.
- Put repeated interaction behavior in Brinell controls, not local test helpers.
- Keep test methods focused on user intent and assertions.
- Prefer semantic operations such as `Click`, `SetText`, `SelectItem`,
  `WaitReady`, and assertions over raw driver operations.
- Prefer UI Automation patterns before pointer or coordinate strategies.
- Do not expose direct mouse movement as a normal public test API.
- Pointer input is opt-in for gesture-only surfaces and remains gated by
  `BRINELL_ALLOW_POINTER_INPUT`.

## Synchronization

- Do not add arbitrary sleeps or longer waits to fix tests.
- Wait for concrete UI state: loaded pages, visible/enabled controls, text,
  busy sentinel changes, request observation, or navigation completion.
- Named polling intervals, cancellation-aware retries, host startup loops, mock
  cadence, and debug-only pauses are acceptable when intentional.
- When a UI test fails, inspect screenshots, diagnostics, runner output, and app
  logs before changing code.

## Code Style

- Use xUnit `Assert`; do not add FluentAssertions.
- Do not add empty catches. Optional platform probing belongs in named `Try*`
  helpers with clear fallback behavior.
- Keep platform element types out of `Brinell.Core`.
- Match existing project layout under `srcnew/` and `testsnew/`.

## Docs Rules

- Active docs live in `docs/`.
- Historical docs live in `docs2/`.
- Keep links valid and relative.
- Use `Brinell.*` namespaces in examples.
- Mark commands with their working directory.
- Update `docs/README.md` when adding or moving active docs.

## Verification

Commands are from the Brinell root:

```powershell
dotnet build srcnew\Brinell.sln -v:minimal /nr:false
dotnet test testsnew\Brinell.Core.Tests\Brinell.Core.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Maui.Tests\Brinell.Maui.Tests.csproj -v:minimal /nr:false
dotnet build srcnew\Brinell.Maui.FlaUI\Brinell.Maui.FlaUI.csproj -f net10.0-windows -v:minimal /nr:false
```

### Match the test scope to the change

**Do not run the full UI suite to verify a narrow change.** `Brinell.Maui.UITests`
launches a real Windows app and drives it through UI Automation; it takes minutes,
and most of it is irrelevant to any given edit. Run the smallest tier that can
actually falsify the change, and only widen if that tier fails or the change is
genuinely broad.

| Tier | Command (filter on `testsnew\Brinell.Maui.UITests`) | Cost |
|---|---|---|
| 1. One area | `--filter "FullyQualifiedName~AutomationProbeTests"` | ~7 s |
| 2. Related areas | `--filter "FullyQualifiedName~Tests.Container\|FullyQualifiedName~Tests.Collection"` | ~11 s |
| 3. Full suite | no filter | minutes |

Use tier 3 only when finishing a phase of work, when the change touches shared
infrastructure (fixture, navigation, `MauiProgram`, handler registration), or when a
narrower tier fails in a way that suggests wider breakage.

Notes:

- `dotnet test` takes **one** `--filter`; passing two silently drops the first. Use
  `|` for OR and `&` for AND inside a single filter string.
- The suite has **pre-existing failures unrelated to container work** (DatePicker,
  TimePicker, Image, ProgressBar, Stepper, Switch). Before reporting a regression,
  establish the baseline — stash the change, rebuild, re-run the same filter — rather
  than assuming a failure is yours.
- A mechanical refactor (moving files, extracting a project) is verified by *the same
  tests passing identically*, not by running more of them.
