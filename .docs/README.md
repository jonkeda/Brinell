# .docs

The durable layer to read **before changing Brinell**: decisions, invariants and
contracts - what must stay true. Not a description of the code; that can be regenerated
from the source and goes stale. See `.my/documentation/plan.md` for the reasoning.

How this differs from the other trees:

| Tree | Holds | Reader |
| --- | --- | --- |
| **`.docs/`** | decisions, invariants, contracts: what must stay true | someone **changing** Brinell |
| `docs/` | getting started, guides, control catalogue, how to run | someone **using** Brinell |
| `.my/` | plans, analyses, reviews for work in flight | whoever is doing that work |
| `docs2/` | archive | nobody, until needed |

## Invariants

- [Invariants](invariants.md) - the short list, each linking its decision.

## Decisions

- [AD-001: Core stays platform-neutral](decisions/ad-001-core-stays-platform-neutral.md)
- [AD-002: Page objects own structure](decisions/ad-002-page-objects-own-structure.md)
- [AD-003: Controls own repeated interaction behavior](decisions/ad-003-controls-own-repeated-interaction-behavior.md)
- [AD-004: Wait for state](decisions/ad-004-wait-for-state.md)
- [AD-005: Physical input is opt-in](decisions/ad-005-physical-input-is-opt-in.md)
- [AD-006: xUnit Assert only](decisions/ad-006-xunit-assert-only.md)
- [AD-007: Shared artifact layout](decisions/ad-007-shared-artifact-layout.md)
- [AD-008: Gestures and semantic actions go through UI Automation](decisions/ad-008-gestures-and-semantic-actions-go-through-ui-automation.md)
- [AD-009: App bugs stay failures](decisions/ad-009-app-bugs-stay-failures.md)
- [AD-010: MAUI and Html ahead of Core, on purpose](decisions/ad-010-maui-ahead-of-core.md)
- [AD-011: UAT step vocabulary is declared by attributes on Core interfaces](decisions/ad-011-uat-vocabulary-is-attribute-declared.md)
- [AD-012: Test data is typed, validated, and chosen by the scenario file](decisions/ad-012-test-data-is-typed-validated-and-chosen-by-the-scenario.md)
- [AD-013: Html controls have MAUI's shape](decisions/ad-013-html-controls-have-mauis-shape.md)
- [AD-014: One scenario file serves every head](decisions/ad-014-one-scenario-file-serves-every-head.md)
- [AD-015: A step's subject picks its app](decisions/ad-015-a-steps-subject-picks-its-app.md)
- [AD-016: Phrase arguments are typed by the method](decisions/ad-016-phrase-arguments-are-typed-by-the-method.md)
- [AD-017: Callable phrases are sequences, not programs](decisions/ad-017-callable-phrases-are-sequences-not-programs.md)
- [AD-018: The Presenter workspace is the UAT project](decisions/ad-018-the-presenter-workspace-is-the-uat-project.md)

## Contracts

- [Call model](contracts/call-model.md) - one call, one budget, one log pair (R0-R9); MAUI and Html.
- [Control object](contracts/control-object.md) - the `.tpl.cs` / `.gen.cs` contract,
  act-once and `Confirm`.
- [Bridge](contracts/bridge.md) - `GestureAutomation.Verbs`: what an app must publish.
- [Apps in a test](contracts/uat-apps-host.md) - `TestAppSet` and `IUatAppsHost`: one table of apps, started
  lazily, evidence from every started app.
