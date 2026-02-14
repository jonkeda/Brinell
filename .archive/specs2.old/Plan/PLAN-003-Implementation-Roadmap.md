# PLAN-003: Implementation Roadmap

**Version:** 1.0  
**Created:** January 7, 2026  
**Status:** Draft

---

## 1. Overview

This plan defines the high-level roadmap for implementing the Brinell UI Test Automation Framework after architecture (PLAN-001) and specifications (PLAN-002) are complete.

### Key Principle

> **Specifications first, then implementation. Tests validate both.**

Implementation follows specifications exactly. Any deviation from specification requires a specification change first, then implementation.

---

## 2. Implementation Phases

### Phase 1: Core Infrastructure

**Duration:** 2 weeks  
**Dependencies:** PLAN-002 Level 0 complete

| Task | Description | Deliverables |
|------|-------------|--------------|
| 1.1 | Create Brinell.Core package | Interfaces, exceptions, Locator, TimeoutSettings |
| 1.2 | Create platform packages | Brinell.Maui, Brinell.Blazor, Brinell.Wpf (structure only) |
| 1.3 | Implement base test contexts | IMauiTestContext, IBlazorTestContext, IWpfTestContext |
| 1.4 | Implement logging foundation | ITestLogger, ConsoleLogger, action logging |
| 1.5 | Create unit test projects | With mocking infrastructure |

**Gate Criteria:**
- [ ] All Core interfaces compile
- [ ] Platform contexts can be instantiated
- [ ] Unit tests for logging pass
- [ ] No platform dependencies in Brinell.Core

---

### Phase 2: Base Class Implementation

**Duration:** 2 weeks  
**Dependencies:** Phase 1 complete

| Task | Description | Deliverables |
|------|-------------|--------------|
| 2.1 | Implement MAUI base classes | MauiControlBase, all capability bases |
| 2.2 | Implement Blazor base classes | BlazorControlBase, all capability bases |
| 2.3 | Implement WPF base classes | WpfControlBase, all capability bases |
| 2.4 | Implement PageObjectBase | Per-platform page object bases |
| 2.5 | Unit test base classes | Mock-based tests for all bases |

**Gate Criteria:**
- [ ] All base classes implement their interfaces
- [ ] Template Method pattern works for all platforms
- [ ] Nullable skip pattern implemented consistently
- [ ] Logging integration verified

---

### Phase 3: Level 1 Controls

**Duration:** 2 weeks  
**Dependencies:** Phase 2 complete, PLAN-002 Level 1 specs complete

**Specifications:**
- [250_100_INDEX.md](../250_specifications/250_100_CoreControls/250_100_INDEX.md) — Core Controls Index
- [250_101_Button.spx.md](../250_specifications/250_100_CoreControls/250_101_Button.spx.md) — Button (IClickableControlObject)
- [250_102_Label.spx.md](../250_specifications/250_100_CoreControls/250_102_Label.spx.md) — Label (ITextControlObject)
- [250_103_Entry.spx.md](../250_specifications/250_100_CoreControls/250_103_Entry.spx.md) — Entry (IEditableTextControlObject)
- [250_104_CheckBox.spx.md](../250_specifications/250_100_CoreControls/250_104_CheckBox.spx.md) — CheckBox (IToggleControlObject)
- [250_105_Container.spx.md](../250_specifications/250_100_CoreControls/250_105_Container.spx.md) — Container (IContainerControlObject)

| Task | Description | Deliverables |
|------|-------------|--------------|
| 3.1 | Implement Button control | Per-platform ButtonControl |
| 3.2 | Implement Label control | Per-platform LabelControl |
| 3.3 | Implement Entry control | Per-platform EntryControl |
| 3.4 | Implement CheckBox control | Per-platform CheckBoxControl |
| 3.5 | Implement Container control | Per-platform ContainerControl |
| 3.6 | Create sample apps | Controls in MAUI, Blazor, WPF sample apps |
| 3.7 | Create UI tests | Tests for Level 1 controls |

**Gate Criteria:**
- [ ] All Level 1 controls pass unit tests
- [ ] All Level 1 controls pass UI tests
- [ ] Sample apps contain all Level 1 controls
- [ ] No base class changes required

---

### Phase 4: Level 2-3 Controls

**Duration:** 3 weeks  
**Dependencies:** Phase 3 complete, PLAN-002 Level 2-3 specs complete

| Task | Description | Deliverables |
|------|-------------|--------------|
| 4.1 | Implement selection controls | Dropdown, ListBox, RadioGroup |
| 4.2 | Implement advanced controls | Slider, DatePicker, DataGrid, Tab |
| 4.3 | Update sample apps | Add Level 2-3 controls |
| 4.4 | Create UI tests | Tests for Level 2-3 controls |
| 4.5 | Validate patterns | Verify composition, multiple interfaces work |

**Gate Criteria:**
- [ ] All Level 2-3 controls pass tests
- [ ] Selection pattern validated
- [ ] Complex controls with multiple interfaces work
- [ ] No base class changes required

---

### Phase 5: Platform-Specific Controls

**Duration:** 2 weeks  
**Dependencies:** Phase 4 complete, PLAN-002 Level 4 specs complete

| Task | Description | Deliverables |
|------|-------------|--------------|
| 5.1 | Implement MAUI-specific controls | Switch, CarouselView, etc. |
| 5.2 | Implement Blazor-specific controls | Modal, Toast, etc. |
| 5.3 | Implement WPF-specific controls | Window, Menu, etc. |
| 5.4 | Update sample apps | Platform-specific control pages |
| 5.5 | Platform-specific UI tests | Per-platform test suites |

**Gate Criteria:**
- [ ] All platform-specific controls pass tests
- [ ] Platform sample apps fully functional
- [ ] Cross-platform patterns still work

---

### Phase 6: Documentation and Polish

**Duration:** 2 weeks  
**Dependencies:** Phase 5 complete

| Task | Description | Deliverables |
|------|-------------|--------------|
| 6.1 | API documentation | XML docs, IntelliSense complete |
| 6.2 | User guide | Getting started, best practices |
| 6.3 | Test writing guide | How to write tests with Brinell |
| 6.4 | Sample test projects | Complete example test suites |
| 6.5 | Performance optimization | Review and optimize hot paths |
| 6.6 | NuGet packaging | Package metadata, versioning |

**Gate Criteria:**
- [ ] All public APIs documented
- [ ] User guide reviewed
- [ ] Sample projects run successfully
- [ ] NuGet packages can be published

---

## 3. Next Steps (Immediate)

After completing PLAN-002 specifications:

### Step 1: Create Project Structure

> **Note:** All source code is written to the `srcnew/` folder to avoid conflicts with existing code.

```
srcnew/
├── Brinell.Core/
│   ├── Interfaces/
│   ├── Locators/
│   ├── Exceptions/
│   └── Configuration/
├── Brinell.Maui/
│   ├── Base/
│   ├── Controls/
│   └── Context/
├── Brinell.Blazor/
│   ├── Base/
│   ├── Controls/
│   └── Context/
└── Brinell.Wpf/
    ├── Base/
    ├── Controls/
    └── Context/
```

### Step 2: Set Up Build Infrastructure

- Create solution file
- Configure Directory.Build.props for shared settings
- Set up NuGet package references
- Configure CI/CD pipeline

### Step 3: Implement Core Interfaces

Start with interfaces from 250_001_IControlObject.spx.md and work through Level 0 specifications in order.

---

## 4. Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Specification gaps found during implementation | Update specification first, then implement |
| Platform differences break abstraction | Level 0 defines platform-specific bases explicitly |
| Performance issues with polling/waiting | Profile early, optimize Wait* methods |
| Driver compatibility issues | Pin driver versions, document requirements |
| Test flakiness | Implement robust retry patterns in base classes |

---

## 5. Success Criteria

Implementation is complete when:

1. **All controls implemented** — Levels 1-5 per PLAN-002
2. **All tests passing** — Unit tests, UI tests per platform
3. **Documentation complete** — API docs, guides, samples
4. **Packages published** — NuGet packages available
5. **Sample apps working** — MAUI, Blazor, WPF samples run with tests

---

## 6. Timeline Summary

| Phase | Duration | Cumulative |
|-------|----------|------------|
| PLAN-001 Architecture | 2 weeks | Week 2 |
| PLAN-002 Specifications | 4 weeks | Week 6 |
| Phase 1: Core Infrastructure | 2 weeks | Week 8 |
| Phase 2: Base Classes | 2 weeks | Week 10 |
| Phase 3: Level 1 Controls | 2 weeks | Week 12 |
| Phase 4: Level 2-3 Controls | 3 weeks | Week 15 |
| Phase 5: Platform-Specific | 2 weeks | Week 17 |
| Phase 6: Documentation | 2 weeks | Week 19 |

**Total estimated duration:** ~19 weeks from start

---

## Related Documents

- [PLAN-001-Architecture-Creation](PLAN-001-Architecture-Creation.md) — Architecture plan
- [PLAN-002-Specification-Levels](PLAN-002-Specification-Levels.md) — Specification plan
- [200_000_Overview](../200_architecture/200_000_Overview.spx.md) — Architecture overview

### Level 1 Control Specifications (250_100_CoreControls)

| Spec ID | Control | Interface | Specification |
|---------|---------|-----------|---------------|
| SPC-101 | Button | IClickableControlObject | [250_101_Button.spx.md](../250_specifications/250_100_CoreControls/250_101_Button.spx.md) |
| SPC-102 | Label | ITextControlObject | [250_102_Label.spx.md](../250_specifications/250_100_CoreControls/250_102_Label.spx.md) |
| SPC-103 | Entry | IEditableTextControlObject | [250_103_Entry.spx.md](../250_specifications/250_100_CoreControls/250_103_Entry.spx.md) |
| SPC-104 | CheckBox | IToggleControlObject | [250_104_CheckBox.spx.md](../250_specifications/250_100_CoreControls/250_104_CheckBox.spx.md) |
| SPC-105 | Container | IContainerControlObject | [250_105_Container.spx.md](../250_specifications/250_100_CoreControls/250_105_Container.spx.md) |

### Testing Infrastructure Requirements

- [FR-950 Sample Applications](../100_requirements/120_functional/120_950_SampleApplications.spx.md)
- [FR-960 Unit Tests](../100_requirements/120_functional/120_960_UnitTests.spx.md)
- [FR-961 Unit Tests Framework](../100_requirements/120_functional/120_961_UnitTestsFramework.spx.md)
- [FR-970 UI Tests](../100_requirements/120_functional/120_970_UITests.spx.md)
