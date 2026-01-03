# PLAN-008: Brinell.Stride Platform Update

**Created:** January 3, 2026
**Status:** In Progress

---

## Overview

Update the Brinell.Stride platform to include missing base classes and controls for spec compliance. Stride is an experimental game engine UI testing platform using named pipes for communication.

---

## Architecture Notes

- **Brinell.Stride** - Test-side framework that communicates with in-game component
- **Brinell.Stride.Automation** - In-game automation handler (Stride has no native automation support)
- Communication via named pipes (synchronous)
- Control naming follows `Stride{ControlName}` pattern

---

## Current State

### Base Classes Present
| Class | Status | File |
|-------|--------|------|
| `StrideControlBase` | ✅ | Controls/Base/StrideControlBase.cs |
| `StridePageBase` | ✅ | Pages/StridePageBase.cs |
| `StrideContentControlBase` | ✅ | Controls/Base/StrideContentControlBase.cs |
| `StrideTextControlBase` | ✅ | Controls/Base/StrideTextControlBase.cs |
| `StrideToggleControlBase` | ✅ | Controls/Base/StrideToggleControlBase.cs |
| `StrideSelectorControlBase` | ✅ | Controls/Base/StrideSelectorControlBase.cs |
| `StrideRangeControlBase` | ✅ | Controls/Base/StrideRangeControlBase.cs |
| `StrideBusyPageBase` | ❌ | **Missing** |
| `StrideItemsControlBase` | ❌ | **Missing** |
| `StrideScrollableControlBase` | ❌ | **Missing** (if needed) |

### Controls Present (11 total)
- StrideButtonControl
- StrideCheckBoxControl
- StrideComboBoxControl
- StrideEditTextControl
- StrideImageControl
- StrideListBoxControl
- StridePanelControl
- StrideProgressBarControl
- StrideSliderControl
- StrideTextBlockControl
- StrideToggleButtonControl

### Sample Tests (5 test files)
- CounterTests.cs
- GameplayTests.cs
- GreetingTests.cs
- SettingsTests.cs
- SimpleAppTest.cs

---

## Implementation Tasks

### Phase 1: Base Classes

| Task | File | Status |
|------|------|--------|
| Create StrideBusyPageBase | Pages/StrideBusyPageBase.cs | ⬜ |
| Create StrideItemsControlBase | Controls/Base/StrideItemsControlBase.cs | ⬜ |
| Create StrideScrollableControlBase (if needed) | Controls/Base/StrideScrollableControlBase.cs | ⬜ |

### Phase 2: Testing

| Task | Status |
|------|--------|
| Build Brinell.Stride | ⬜ |
| Build Brinell.Stride.Automation | ⬜ |
| Build Brinell.Samples.Stride.UITests | ⬜ |
| Run each test individually | ⬜ |

---

## Spec Compliance from PLAN-001

| Requirement | Status | Notes |
|-------------|--------|-------|
| FR-002.5 Interface Hierarchy | ⚠️ | Missing ItemsControl |
| FR-002.6 Container Support | ❌ | Not implemented |
| FR-002.7 Scroll Support | ❌ | Stride UI has scrollable panels |
| FR-004.4.1 Assert calls Check | ⚠️ | Needs verification |
| FR-005.4.1 BusyPageBase | ❌ | Missing |
| FR-005.5 Sync Operations | ✅ | Uses named pipes (sync) |
| FR-007.5 Named Pipes | ✅ | Implemented |

---

## Test Execution

```powershell
# Build
dotnet build src/Brinell.Stride
dotnet build samples/Brinell.Samples.Stride.UITests

# Run tests (requires Stride sample app running)
dotnet test samples/Brinell.Samples.Stride.UITests --filter "FullyQualifiedName~SimpleAppTest"
```

---

## Completion Criteria

- [ ] All base classes created
- [ ] Build succeeds with no errors
- [ ] Tests run (may fail if sample app not available)
- [ ] Documentation updated
