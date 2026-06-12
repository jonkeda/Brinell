# Copilot Instructions For Brinell

These instructions apply to the Brinell submodule.

## Active Sources

- Active code: `srcnew/`
- Active tests: `testsnew/`
- Samples: `samples/`
- Active docs: `docs/`
- Previous docs archive: `docs2/`
- Planning/research notes: `.my/reports/`

Use [AGENTS.md](../AGENTS.md) as the local entry point.

## Build And Test

Commands are from the Brinell root.

```powershell
dotnet build srcnew\Brinell.sln -v:minimal /nr:false
dotnet test testsnew\Brinell.Core.Tests\Brinell.Core.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Maui.Tests\Brinell.Maui.Tests.csproj -v:minimal /nr:false
dotnet test testsnew\Brinell.Uat.Tests\Brinell.Uat.Tests.csproj -v:minimal /nr:false
```

Use the focused platform run guides under `docs/run/` for UI tests.
`srcnew\Brinell.sln` is a broad compile check, not a complete project inventory.

## Architecture Rules

- Keep `Brinell.Core` platform-neutral.
- Put shared contracts, locators, waits, artifacts, and base abstractions in
  `Brinell.Core`.
- Put driver-specific element handling in the matching platform project.
- Page objects own page or screen structure.
- Controls own repeated interaction behavior.
- Tests express user intent and assertions.

## Synchronization Rules

Do not add arbitrary sleeps or longer waits to fix tests. Wait for concrete UI
state instead:

- page loaded or navigation complete;
- element visible, enabled, selected, or gone;
- text/value/count changed;
- busy sentinel ended;
- request or runtime event observed.

Intentional delays are allowed only when they are named polling intervals,
cancellation-aware retries, host startup loops, mock cadence, or explicit
debug-only pauses.

## UI Interaction Rules

- Prefer semantic control methods such as `Click`, `SetText`, `SelectItem`, and
  `WaitReady`.
- Prefer UI Automation patterns such as Invoke, Value, SelectionItem,
  RangeValue, and ExpandCollapse before pointer strategies.
- Do not expose direct mouse movement as a normal public test API.
- Pointer input is opt-in for gesture-only surfaces and remains gated by
  `BRINELL_ALLOW_POINTER_INPUT`.

## Error Handling

- Do not add empty catches.
- If optional platform property probing may throw, contain it in a named `Try*`
  helper and return a clear fallback.
- Do not use exceptions for normal control flow.
- When wrapping exceptions, include page/control/locator context.

## Test Rules

- Use xUnit `Assert`.
- Do not add FluentAssertions.
- Keep fixture constructors focused on runtime setup.
- Keep page ownership in test classes or page-object composition.
- Do not add ad-hoc cleanup or back-navigation loops inside test methods.

## Documentation Rules

- Active docs live in `docs/`; do not copy `docs2/` back wholesale.
- Use `Brinell.*` namespaces in examples.
- Keep links relative and valid.
- Mark command working directories.
- Do not reference missing `.specs`, `.cnv2`, or old numbered docs.
- Specs need status: proposed, active, implemented, superseded, or archived.

## Package Rules

- Package versions belong in central package files.
- Do not add per-project version pins unless deliberately required.
- Keep package names aligned with `PackageId` values in `srcnew/**/*.csproj`.

## Git Safety

- Do not revert unrelated user changes.
- Do not use destructive git commands unless explicitly requested.
- If the tree is already dirty, work with the existing changes and mention them
  in the final summary when relevant.
