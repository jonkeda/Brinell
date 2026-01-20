# PLAN-026: UI Test Incremental Fix Plan

**Created:** January 20, 2026  
**Status:** In Progress  
**Objective:** Run and fix UI tests incrementally by test class

## Strategy

Run tests one class at a time, fix failures immediately, then proceed to next class.
This avoids long wait times from running all 222 tests before fixing.

## Test Classes (Priority Order)

Based on SPEC-026 focus areas (Toggle, Slider, Stepper controls):

| # | Test Class | Focus Area | Est. Tests |
|---|------------|------------|------------|
| 1 | ToggleControlTests6 | Toggle Check/Uncheck | ~20 |
| 2 | RangeControlTests6 | Slider/Stepper SetValue, Increment/Decrement | ~25 |
| 3 | ClickTests6 | Basic click interactions | ~15 |
| 4 | ControlStateTests6 | Is/Wait/Assert patterns | ~30 |
| 5 | TextInputTests6 | Entry/Editor text input | ~25 |
| 6 | SelectionControlTests | Picker/ComboBox selection | ~20 |
| 7 | CollectionControlTests | ListView/CollectionView | ~15 |
| 8 | CounterTests6 | Integration scenarios | ~10 |

## Execution Workflow

For each test class:
1. **Run** - Execute tests for that class only
2. **Analyze** - Review failures and error messages
3. **Fix** - Apply targeted fixes to framework code
4. **Verify** - Re-run to confirm fixes
5. **Proceed** - Move to next class

## Progress Tracking

### Phase 1: Toggle Controls
- [ ] Run ToggleControlTests6
- [ ] Fix any failures
- [ ] Verify pass rate

### Phase 2: Range Controls  
- [ ] Run RangeControlTests6
- [ ] Fix any failures
- [ ] Verify pass rate

### Phase 3: Click Interactions
- [ ] Run ClickTests6
- [ ] Fix any failures
- [ ] Verify pass rate

### Phase 4: Control States
- [ ] Run ControlStateTests6
- [ ] Fix any failures
- [ ] Verify pass rate

### Phase 5: Text Input
- [ ] Run TextInputTests6
- [ ] Fix any failures
- [ ] Verify pass rate

### Phase 6: Selection Controls
- [ ] Run SelectionControlTests
- [ ] Fix any failures
- [ ] Verify pass rate

### Phase 7: Collection Controls
- [ ] Run CollectionControlTests
- [ ] Fix any failures
- [ ] Verify pass rate

### Phase 8: Counter Integration
- [ ] Run CounterTests6
- [ ] Fix any failures
- [ ] Verify pass rate

## Commands Reference

Run single test class:
```powershell
dotnet test samples\Brinell.Samples.Maui.UITests.ControlObject6 --filter "FullyQualifiedName~ToggleControlTests6"
```

Run single test:
```powershell
dotnet test samples\Brinell.Samples.Maui.UITests.ControlObject6 --filter "FullyQualifiedName~TestMethodName"
```

## Success Criteria

- Target: 90%+ pass rate (200+/222 tests)
- Previous baseline: 151/222 passing (68%)
- Focus: Toggle, Slider, Stepper interactions that were failing

## Notes

- Appium server must be running on 127.0.0.1:4723
- MAUI app launches automatically per test
- Each test class runs in isolation
