# Tasks Document

## Task Format

Each task follows this structure:
- `[ ]` = Pending, `[-]` = In-progress, `[x]` = Completed
- Includes File path, Purpose, _Leverage, _Requirements, and _Prompt fields

---

## Phase 1: ScrollIntoView Enhancement

### [ ] 1. Add ScrollIntoView to MauiControlBase

- **File:** `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`
- **Purpose:** Enable automatic scrolling before control interactions
- _Leverage: Existing `RunWithElement()`, `IsVisibleCore()`, `Context.Driver`_
- _Requirements: FR-1.1, FR-1.2, FR-1.3, FR-1.4_
- _Prompt: Role: C# MAUI automation developer | Task: Add `ScrollIntoView()`, `ScrollIntoViewCore()`, and `EnsureVisible()` methods to MauiControlBase following design.spc.spx.md section on ScrollIntoView | Restrictions: Do not change public API signatures of existing methods, use `OpenQA.Selenium.Interactions.Actions` for scroll, swallow scroll exceptions | Success: ScrollIntoView compiles, uses MoveToElement, skips if already visible_

### [ ] 1.1. Modify RunWithElement to auto-scroll

- **File:** `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`
- **Purpose:** Integrate scroll into all control interactions automatically
- _Leverage: `EnsureVisible()` from Task 1, existing `RunWithElement()` overloads_
- _Requirements: FR-1.2_
- _Prompt: Role: C# developer | Task: Modify all `RunWithElement()` overloads to call `EnsureVisible(element)` after finding element but before core operation | Restrictions: Must modify all 3 overloads consistently, preserve existing logging behavior | Success: All interactions now scroll before action, existing tests still compile_

### [ ] 1.2. Validate Phase 1 with smoke test

- **Command:** `dotnet test testsnew/Brinell.Maui.UITests --filter "FullyQualifiedName~MainPageTests" --no-build`
- **Purpose:** Verify scroll changes don't break existing functionality
- _Requirements: NFR-1_
- _Prompt: Role: QA engineer | Task: Run MainPageTests to verify scroll integration works | Restrictions: Must pass before proceeding to Phase 2 | Success: Tests pass or fail only due to unrelated issues_

---

## Phase 2: Slider SetValue Fix

### [ ] 2. Override SetValueCore in MauiSliderControl

- **File:** `srcnew/Brinell.Maui/Controls/Range/MauiSliderControl.cs`
- **Purpose:** Use click-based positioning instead of SendKeys for sliders
- _Leverage: `GetMinimumCore()`, `GetMaximumCore()`, `element.Location`, `element.Size`, Actions API_
- _Requirements: FR-2.1, FR-2.2, FR-2.3_
- _Prompt: Role: C# Appium developer | Task: Override `SetValueCore()` in MauiSliderControl to calculate click position from value percentage, use Actions.MoveToLocation().Click() | Restrictions: Clamp value to min/max range, use 5% padding on edges, throw if range invalid | Success: Slider value can be set by clicking at calculated position_

### [ ] 2.1. Validate Phase 2 with SliderControlTests

- **Command:** `dotnet test testsnew/Brinell.Maui.UITests --filter "FullyQualifiedName~SliderControlTests" --no-build`
- **Purpose:** Verify slider interactions now work
- _Requirements: FR-2.1_
- _Prompt: Role: QA engineer | Task: Run SliderControlTests to verify SetValue fix | Restrictions: Target 80%+ pass rate for slider tests | Success: SlideToPercentage, SlideToMinimum, SlideToMaximum tests pass_

---

## Phase 3: Toggle State Verification

### [ ] 3. Add toggle state verification to MauiToggleControlBase

- **File:** `srcnew/Brinell.Maui/Controls/MauiToggleControlBase.cs`
- **Purpose:** Verify toggle state changed and retry if not
- _Leverage: `IsCheckedCore()`, Actions API, existing `ToggleCore()` pattern_
- _Requirements: FR-3.1, FR-3.2_
- _Prompt: Role: C# automation developer | Task: Modify `ToggleCore()` to check state before/after click, retry with Actions-based click if unchanged | Restrictions: Do not throw on retry failure (let assertion catch it), wait 100ms after click for state change | Success: Toggle operations include verification and retry logic_

### [ ] 3.1. Override in MauiSwitchControl if needed

- **File:** `srcnew/Brinell.Maui/Controls/Toggle/MauiSwitchControl.cs`
- **Purpose:** Add Switch-specific toggle handling if base doesn't work
- _Leverage: Base `ToggleCore()` from MauiToggleControlBase_
- _Requirements: FR-3.3_
- _Prompt: Role: C# developer | Task: Review if base ToggleCore works for Switch, override with positioned click if needed | Restrictions: Only override if tests still fail after Task 3 | Success: Switch toggle works reliably_

### [ ] 3.2. Override in MauiCheckBoxControl if needed

- **File:** `srcnew/Brinell.Maui/Controls/Toggle/MauiCheckBoxControl.cs`
- **Purpose:** Ensure click targets checkbox, not adjacent label
- _Leverage: Base `ToggleCore()`, element positioning_
- _Requirements: FR-3.3_
- _Prompt: Role: C# developer | Task: Review if base ToggleCore works for CheckBox, override to click at 25% width if label interference detected | Restrictions: Only override if tests still fail after Task 3 | Success: CheckBox toggle works reliably_

### [ ] 3.3. Validate Phase 3 with toggle tests

- **Command:** `dotnet test testsnew/Brinell.Maui.UITests --filter "FullyQualifiedName~SwitchControlTests|CheckBoxControlTests|RadioButtonControlTests" --no-build`
- **Purpose:** Verify toggle controls now work
- _Requirements: FR-3.1, FR-3.2, FR-3.3_
- _Prompt: Role: QA engineer | Task: Run toggle control tests | Restrictions: Target 80%+ pass rate | Success: Toggle, TurnOn, TurnOff, Check, Uncheck operations work_

---

## Phase 4: Stepper Control Fix

### [ ] 4. Override Increment/Decrement in MauiStepperControl

- **File:** `srcnew/Brinell.Maui/Controls/Range/MauiStepperControl.cs`
- **Purpose:** Use child button clicks instead of keyboard
- _Leverage: `Context.FindElements()`, base increment/decrement patterns_
- _Requirements: FR-4.1_
- _Prompt: Role: C# automation developer | Task: Override `IncrementCore()` and `DecrementCore()` to find RepeatButton children and click them | Restrictions: Fall back to base implementation if buttons not found, assume first button is decrement, last is increment | Success: Stepper increment/decrement clicks actual buttons_

### [ ] 4.1. Override SetValueCore in MauiStepperControl

- **File:** `srcnew/Brinell.Maui/Controls/Range/MauiStepperControl.cs`
- **Purpose:** Set value by repeated button clicks
- _Leverage: `IncrementCore()`, `DecrementCore()`, `GetValueCore()`, `GetStepCore()`_
- _Requirements: FR-4.2_
- _Prompt: Role: C# developer | Task: Override `SetValueCore()` to calculate click count from current value and step, call increment/decrement repeatedly | Restrictions: Limit to 100 clicks max, 20ms delay between clicks | Success: Stepper SetValue works by clicking buttons_

### [ ] 4.2. Validate Phase 4 with StepperControlTests

- **Command:** `dotnet test testsnew/Brinell.Maui.UITests --filter "FullyQualifiedName~StepperControlTests" --no-build`
- **Purpose:** Verify stepper controls now work
- _Requirements: FR-4.1, FR-4.2_
- _Prompt: Role: QA engineer | Task: Run StepperControlTests | Restrictions: Target 80%+ pass rate | Success: Increment, Decrement, SetValue operations work_

---

## Phase 5: Full Validation

### [ ] 5. Run complete UI test suite

- **Command:** `dotnet test testsnew/Brinell.Maui.UITests --no-build`
- **Purpose:** Validate all fixes together
- _Requirements: All_
- _Prompt: Role: QA engineer | Task: Run full UI test suite and compare to baseline (151 passed) | Restrictions: Must achieve 200+ passed (90%+) | Success: 200+ tests pass, <22 fail_

### [ ] 5.1. Address remaining failures

- **Files:** Various based on failure analysis
- **Purpose:** Fix any remaining edge cases
- _Requirements: All_
- _Prompt: Role: C# developer | Task: Analyze remaining failures, categorize by root cause, fix or document as known limitations | Restrictions: Do not spend more than 30 minutes per failure | Success: Pass rate >90% or remaining failures documented_

### [ ] 5.2. Build solution and verify no errors

- **Command:** `dotnet build Brinell.sln --verbosity minimal`
- **Purpose:** Ensure all changes compile cleanly
- _Requirements: NFR-1_
- _Prompt: Role: Build engineer | Task: Verify solution builds without errors or warnings | Restrictions: No new warnings introduced | Success: Build succeeded for all projects_

---

## Summary

| Phase | Tasks | Focus Area |
|-------|-------|------------|
| 1 | 1, 1.1, 1.2 | ScrollIntoView integration |
| 2 | 2, 2.1 | Slider click positioning |
| 3 | 3, 3.1, 3.2, 3.3 | Toggle verification |
| 4 | 4, 4.1, 4.2 | Stepper button clicks |
| 5 | 5, 5.1, 5.2 | Full validation |

**Estimated Time:** 2-3 hours total
