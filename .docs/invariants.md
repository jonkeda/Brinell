# Invariants

The short list that must not be broken. Each links the decision that explains it.

- **Core stays platform-neutral.** No platform element type in `Brinell.Core`.
  [AD-001](decisions/ad-001-core-stays-platform-neutral.md)
- **Tests go through page objects and controls.** No `Locator`, `FindElement` or driver
  access in a test method. [AD-002](decisions/ad-002-page-objects-own-structure.md),
  [AD-003](decisions/ad-003-controls-own-repeated-interaction-behavior.md)
- **No arbitrary waits.** Wait for observable UI state, never a sleep.
  [AD-004](decisions/ad-004-wait-for-state.md),
  [call-model](contracts/call-model.md)
- **MAUI on Windows uses no physical input.** No pointer/keyboard/clipboard/foreground in
  `Brinell.Maui.FlaUI`; every action is a UI Automation pattern or a bridge verb.
  [AD-005](decisions/ad-005-physical-input-is-opt-in.md)
- **xUnit `Assert` only.** No FluentAssertions.
  [AD-006](decisions/ad-006-xunit-assert-only.md)
- **Shared artifact layout.** `TestResults/<run-id>/suites/<suite>/`.
  [AD-007](decisions/ad-007-shared-artifact-layout.md)
- **Gestures go through the bridge, never coordinates.** Declared per element, classified,
  absent from shipping builds. [AD-008](decisions/ad-008-gestures-and-semantic-actions-go-through-ui-automation.md),
  [bridge](contracts/bridge.md)
- **App bugs stay failures.** An action runs once and is confirmed, never repeated;
  defects are reported, not absorbed. [AD-009](decisions/ad-009-app-bugs-stay-failures.md),
  [control-object](contracts/control-object.md)
- **MAUI is ahead of Core on purpose.** No adapters between the MAUI interfaces and Core's;
  Core is not changed ahead of the move-down.
  [AD-010](decisions/ad-010-maui-ahead-of-core.md)
- **UAT step vocabulary is attribute-declared.** Control phrases live as `[UatStep]` on Core
  interfaces; no hand-written phrase table. Every catalog comes from one discovery pass.
  [AD-011](decisions/ad-011-uat-vocabulary-is-attribute-declared.md)
- **The generator output is not hand-edited.** `.gen.cs` mirrors `.tpl.cs` through
  `Brinell.Generator`. [control-object](contracts/control-object.md)
