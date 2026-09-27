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
- **MAUI and Html are ahead of Core on purpose.** No adapters between their interfaces and
  Core's; Core is not changed ahead of the move-down. The calls layer, its exceptions and
  `ScopeReadiness` have moved down: one copy, in Core.
  [AD-010](decisions/ad-010-maui-ahead-of-core.md)
- **UAT step vocabulary is attribute-declared.** Every step is a `[UatGiven]`, `[UatWhen]` or
  `[UatThen]` (base `UatPhraseAttribute`): a control verb on a control method, a phrase on a
  fixture or phrase-class method. No hand-written phrase table; every catalog comes from one
  discovery pass.
  [AD-011](decisions/ad-011-uat-vocabulary-is-attribute-declared.md)
- **Test data is typed and validated.** Test sets are read strictly into the app's real types
  and checked by one engine (gate and `brinell-data` alike); mock bodies are contract types,
  never anonymous (`BRINELL001`); a UAT file names its own `TestData`.
  [AD-012](decisions/ad-012-test-data-is-typed-validated-and-chosen-by-the-scenario.md)
- **Html controls have MAUI's shape.** Generated from `.tpl.cs`, MAUI's `Run*` signatures and
  Core interfaces, no async members. [AD-013](decisions/ad-013-html-controls-have-mauis-shape.md)
- **One scenario file serves every head.** Head tags decide where it runs; no per-head copies.
  Apps of several heads meet only in a `mixed` suite, each binding against its own head's catalog.
  [AD-014](decisions/ad-014-one-scenario-file-serves-every-head.md)
- **A step's subject picks its app.** `Daan taps Save` binds to a declared `{user} taps {control}`
  and runs on Daan's app; no verb forms are inferred, names come from the `Apps` table only, and
  the subject sticks.
  [AD-015](decisions/ad-015-a-steps-subject-picks-its-app.md)
- **The generator output is not hand-edited.** `.gen.cs` mirrors `.tpl.cs` through
  `Brinell.Generator`. [control-object](contracts/control-object.md)
