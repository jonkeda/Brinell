# UI Test Control Interaction Fixes

**Status:** Active | **Priority:** High

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

## Design Spec

See DES-026 for implementation details of each fix.
