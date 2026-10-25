# Migration Notes

These notes help migrate old Bruinell or Oravey-era material to the current
Bruinell source layout.

## Naming

Replace old namespaces and project names with current `Bruinell.*` names.

| Old pattern | Current pattern |
| --- | --- |
| `Oravey.UITestFramework.*` | `Bruinell.*` |
| numbered docs such as `02-framework-overview.md` | docs under named folders |
| `src/Bruinell.*` | `srcnew/Bruinell.*` |
| `tests/Bruinell.*` | `testsnew/Bruinell.*` |

## Docs

- Active docs live under `docs/`.
- Preserved old docs live under `docs2/`.
- Do not copy old links forward unless the target exists.
- Prefer short current docs over large copied legacy pages.

## Tests

- Use xUnit `Assert`.
- Remove FluentAssertions references.
- Replace sleeps with waits for state.
- Move repeated helper behavior into controls or fixtures.

## Artifacts

Move hardcoded output paths to the Bruinell artifact provider where possible.
New output should use `TestResults/<run-id>/suites/<suite>/`.
