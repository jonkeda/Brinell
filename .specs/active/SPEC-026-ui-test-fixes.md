# UI Test Control Interaction Fixes

**Status:** Active | **Priority:** High | **Target:** 90%+ pass rate (from 68%)

## Problem

66 failing UI tests out of 220 (30% failure rate). Three root causes:

### 1. Off-Screen Elements (40% of failures)

Controls below the fold fail because interactions (click/toggle/slide) target invisible elements. Affected: SliderControlTests, CheckboxControlTests, SwitchControlTests.

**Fix:** `ScrollIntoView` before every interaction in `RunWithElement`.

### 2. Slider Value Setting (30% of failures)

Gesture-based slider manipulation is unreliable. Small screen movements don't translate to precise value changes.

**Fix:** Use `IRangePatternElement.SetRangeValue()` on Windows (FlaUI). Gesture fallback with retry/verification loop on mobile.

### 3. Toggle Timing (30% of failures)

Toggle state verification reads stale state immediately after click. The control hasn't updated yet.

**Fix:** After toggle click, poll for expected state change before returning. Built into `SetChecked()` method.

## Requirements

| ID | Requirement | Target |
|----|-------------|--------|
| FR-1 | ScrollIntoView in RunWithElement before interactions | All controls visible before action |
| FR-2 | Position-based slider using IRangePatternElement | Direct value set on FlaUI, gesture+verify on Appium |
| FR-3 | Verified toggle with poll after click | State confirmed within 2s |
| FR-4 | Button-based stepper (find +/- children) | Reliable Increment/Decrement |
| NFR | Overall pass rate | ≥90% (from 68%) |

## Task Status

All tasks pending — 5 phases, ~14 tasks:

- **Phase 1:** ScrollIntoView integration into RunWithElement
- **Phase 2:** Slider IRangePatternElement integration
- **Phase 3:** Toggle poll-verify loop
- **Phase 4:** Stepper button-click approach
- **Phase 5:** Full test suite validation

## Design Spec

See [DES-026-ui-test-fixes-design.md](DES-026-ui-test-fixes-design.md) for implementation details.

## Related

- SPEC-029: FlaUI Windows driver fixes (fixes FlaUI-specific slider/picker/text issues)
- [SPEC-scrollintoview-android.md](SPEC-scrollintoview-android.md): Android ScrollIntoView analysis
