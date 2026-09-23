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
- [AD-010: MAUI ahead of Core, on purpose](decisions/ad-010-maui-ahead-of-core.md)
- [AD-011: UAT step vocabulary is declared by attributes on Core interfaces](decisions/ad-011-uat-vocabulary-is-attribute-declared.md)

## Contracts

- [Call model](contracts/call-model.md) - one call, one budget, one log pair (R0-R9).
- [Control object](contracts/control-object.md) - the `.tpl.cs` / `.gen.cs` contract,
  act-once and `Confirm`.
- [Bridge](contracts/bridge.md) - `GestureAutomation.Verbs`: what an app must publish.
