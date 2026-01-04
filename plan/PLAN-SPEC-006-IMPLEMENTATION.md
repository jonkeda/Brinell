# PLAN: SPEC-006 ControlObject Framework Implementation

**Version:** 1.0  
**Status:** Draft  
**Date:** January 4, 2026  
**Source:** [SPEC-006-INDEX](../specs/SPEC-006-INDEX.md)

---

## 1. Overview

This plan outlines the implementation of SPEC-006 ControlObject Framework as a new v6 implementation alongside the existing v1.x codebase.

### References

| Document | Purpose |
|----------|---------|
| [SPEC-006-INDEX](../specs/SPEC-006-INDEX.md) | Master specification index |
| [SPEC-006-001-INTERFACES](../specs/SPEC-006-001-INTERFACES.md) | All interface definitions |
| [SPEC-006-002-CLASSES-*](../specs/) | Class implementations (multiple files) |
| [REQ-001](../specs/REQ-001-functional-requirements.md) | Functional requirements |
| [REQ-002](../specs/REQ-002-non-functional-requirements.md) | Non-functional requirements |
| [REQ-CHANGES-SPEC-006](../specs/REQ-CHANGES-SPEC-006.md) | Breaking changes documentation |
| [REVIEW-006](../Reviews/REVIEW-006-SPEC-006-Requirements-Compliance.md) | Requirements compliance review |
| [PROPOSAL-V6-MIGRATION](../plan/PROPOSAL-V6-MIGRATION-STRATEGY.md) | Migration strategy |

---

## 2. Project Structure

### New Projects (src/v6/)

| Project | Description | Dependencies |
|---------|-------------|--------------|
| `Brinell.ControlObject.Core` | Interfaces, locators, exceptions | None |
| `Brinell.ControlObject.Maui` | MAUI/Appium implementation | Core, Appium.WebDriver |
| `Brinell.ControlObject.Blazor` | Blazor/Playwright implementation | Core, Microsoft.Playwright |
| `Brinell.ControlObject.Testing` | Test base classes, fixtures | Core |

### Namespace Convention

```
Brinell.ControlObject.Core
Brinell.ControlObject.Core.Locators
Brinell.ControlObject.Core.Exceptions
Brinell.ControlObject.Maui
Brinell.ControlObject.Maui.Controls
Brinell.ControlObject.Maui.Pages
Brinell.ControlObject.Blazor
Brinell.ControlObject.Blazor.Controls
Brinell.ControlObject.Blazor.Pages
```

---

## 3. Implementation Phases

### Phase 1: Core Foundation

**Goal:** Implement core interfaces, locators, and exceptions

**Deliverables:**

| Component | Reference |
|-----------|-----------|
| `ControlLocator` class | [SPEC-006-001 §1](../specs/SPEC-006-001-INTERFACES.md) |
| `LocatorStrategy` enum | [SPEC-006-001 §1](../specs/SPEC-006-001-INTERFACES.md) |
| `By` static factory | [SPEC-006-001 §1](../specs/SPEC-006-001-INTERFACES.md) |
| `IControlObject` | [SPEC-006-001 §2](../specs/SPEC-006-001-INTERFACES.md) |
| `IInteractiveControlObject` | [SPEC-006-001 §2](../specs/SPEC-006-001-INTERFACES.md) |
| `IFocusableControlObject` | [SPEC-006-001 §2](../specs/SPEC-006-001-INTERFACES.md) |
| Exception hierarchy | [SPEC-006-001 §17](../specs/SPEC-006-001-INTERFACES.md) |

**Files to Create:**
- `src/v6/Brinell.ControlObject.Core/Locators/ControlLocator.cs`
- `src/v6/Brinell.ControlObject.Core/Locators/LocatorStrategy.cs`
- `src/v6/Brinell.ControlObject.Core/Locators/By.cs`
- `src/v6/Brinell.ControlObject.Core/Interfaces/IControlObject.cs`
- `src/v6/Brinell.ControlObject.Core/Interfaces/IInteractiveControlObject.cs`
- `src/v6/Brinell.ControlObject.Core/Interfaces/IFocusableControlObject.cs`
- `src/v6/Brinell.ControlObject.Core/Exceptions/*.cs`

---

### Phase 2: Input Interfaces

**Goal:** Implement clickable, text, and editable text interfaces

**Deliverables:**

| Component | Reference |
|-----------|-----------|
| `IClickableControlObject` | [SPEC-006-001 §3](../specs/SPEC-006-001-INTERFACES.md) |
| `ITextControlObject` | [SPEC-006-001 §3](../specs/SPEC-006-001-INTERFACES.md) |
| `IEditableTextControlObject` | [SPEC-006-001 §3](../specs/SPEC-006-001-INTERFACES.md) |
| `ISearchControlObject` | [SPEC-006-001 §3](../specs/SPEC-006-001-INTERFACES.md) |

**Files to Create:**
- `src/v6/Brinell.ControlObject.Core/Interfaces/Input/IClickableControlObject.cs`
- `src/v6/Brinell.ControlObject.Core/Interfaces/Input/ITextControlObject.cs`
- `src/v6/Brinell.ControlObject.Core/Interfaces/Input/IEditableTextControlObject.cs`
- `src/v6/Brinell.ControlObject.Core/Interfaces/Input/ISearchControlObject.cs`

---

### Phase 3: Toggle & Selection Interfaces

**Goal:** Implement toggle and selector interfaces

**Deliverables:**

| Component | Reference |
|-----------|-----------|
| `IToggleControlObject` | [SPEC-006-001 §4](../specs/SPEC-006-001-INTERFACES.md) |
| `ICheckBoxControlObject` | [SPEC-006-001 §4](../specs/SPEC-006-001-INTERFACES.md) |
| `ISwitchControlObject` | [SPEC-006-001 §4](../specs/SPEC-006-001-INTERFACES.md) |
| `IRadioButtonControlObject` | [SPEC-006-001 §4](../specs/SPEC-006-001-INTERFACES.md) |
| `ISelectorControlObject` | [SPEC-006-001 §5](../specs/SPEC-006-001-INTERFACES.md) |
| `IPickerControlObject` | [SPEC-006-001 §5](../specs/SPEC-006-001-INTERFACES.md) |
| `IMultiSelectorControlObject` | [SPEC-006-001 §5](../specs/SPEC-006-001-INTERFACES.md) |

**Files to Create:**
- `src/v6/Brinell.ControlObject.Core/Interfaces/Toggle/*.cs`
- `src/v6/Brinell.ControlObject.Core/Interfaces/Selection/*.cs`

---

### Phase 4: Range & DateTime Interfaces

**Goal:** Implement range and datetime control interfaces

**Deliverables:**

| Component | Reference |
|-----------|-----------|
| `IRangeControlObject` | [SPEC-006-001 §6](../specs/SPEC-006-001-INTERFACES.md) |
| `ISliderControlObject` | [SPEC-006-001 §6](../specs/SPEC-006-001-INTERFACES.md) |
| `IStepperControlObject` | [SPEC-006-001 §6](../specs/SPEC-006-001-INTERFACES.md) |
| `IDateControlObject` | [SPEC-006-001 §7](../specs/SPEC-006-001-INTERFACES.md) |
| `ITimeControlObject` | [SPEC-006-001 §7](../specs/SPEC-006-001-INTERFACES.md) |
| `IDateTimeControlObject` | [SPEC-006-001 §7](../specs/SPEC-006-001-INTERFACES.md) |

**Files to Create:**
- `src/v6/Brinell.ControlObject.Core/Interfaces/Range/*.cs`
- `src/v6/Brinell.ControlObject.Core/Interfaces/DateTime/*.cs`

---

### Phase 5: Collection & Container Interfaces

**Goal:** Implement collection and container interfaces

**Deliverables:**

| Component | Reference |
|-----------|-----------|
| `IItemsControlObject` | [SPEC-006-001 §8](../specs/SPEC-006-001-INTERFACES.md) |
| `ISelectableItemsControlObject` | [SPEC-006-001 §8](../specs/SPEC-006-001-INTERFACES.md) |
| `IMultiSelectableItemsControlObject` | [SPEC-006-001 §8](../specs/SPEC-006-001-INTERFACES.md) |
| `IScrollableItemsControlObject` | [SPEC-006-001 §8](../specs/SPEC-006-001-INTERFACES.md) |
| `IGroupedItemsControlObject` | [SPEC-006-001 §8](../specs/SPEC-006-001-INTERFACES.md) |
| `IContainerControlObject<T>` | [SPEC-006-001 §9](../specs/SPEC-006-001-INTERFACES.md) |
| `IListContainerControlObject<T>` | [SPEC-006-001 §9](../specs/SPEC-006-001-INTERFACES.md) |
| `IScrollableControlObject` | [SPEC-006-001 §9](../specs/SPEC-006-001-INTERFACES.md) |
| `IExpanderControlObject` | [SPEC-006-001 §9](../specs/SPEC-006-001-INTERFACES.md) |
| `IRefreshableControlObject` | [SPEC-006-001 §9](../specs/SPEC-006-001-INTERFACES.md) |
| `ISwipeableControlObject` | [SPEC-006-001 §9](../specs/SPEC-006-001-INTERFACES.md) |

**Files to Create:**
- `src/v6/Brinell.ControlObject.Core/Interfaces/Collection/*.cs`
- `src/v6/Brinell.ControlObject.Core/Interfaces/Container/*.cs`

---

### Phase 6: Display & Media Interfaces

**Goal:** Implement display and media interfaces

**Deliverables:**

| Component | Reference |
|-----------|-----------|
| `ILabelControlObject` | [SPEC-006-001 §10](../specs/SPEC-006-001-INTERFACES.md) |
| `IImageControlObject` | [SPEC-006-001 §10](../specs/SPEC-006-001-INTERFACES.md) |
| `IProgressControlObject` | [SPEC-006-001 §10](../specs/SPEC-006-001-INTERFACES.md) |
| `IActivityIndicatorControlObject` | [SPEC-006-001 §10](../specs/SPEC-006-001-INTERFACES.md) |
| `IMediaControlObject` | [SPEC-006-001 §11](../specs/SPEC-006-001-INTERFACES.md) |
| `IWebViewControlObject` | [SPEC-006-001 §11](../specs/SPEC-006-001-INTERFACES.md) |

**Files to Create:**
- `src/v6/Brinell.ControlObject.Core/Interfaces/Display/*.cs`
- `src/v6/Brinell.ControlObject.Core/Interfaces/Media/*.cs`

---

### Phase 7: Navigation & Validation Interfaces

**Goal:** Implement navigation and validation interfaces

**Deliverables:**

| Component | Reference |
|-----------|-----------|
| `ITabControlObject` | [SPEC-006-001 §12](../specs/SPEC-006-001-INTERFACES.md) |
| `IMenuControlObject` | [SPEC-006-001 §12](../specs/SPEC-006-001-INTERFACES.md) |
| `IFlyoutControlObject` | [SPEC-006-001 §12](../specs/SPEC-006-001-INTERFACES.md) |
| `IToolbarControlObject` | [SPEC-006-001 §12](../specs/SPEC-006-001-INTERFACES.md) |
| `IValidatableControlObject` | [SPEC-006-001 §13](../specs/SPEC-006-001-INTERFACES.md) |

**Files to Create:**
- `src/v6/Brinell.ControlObject.Core/Interfaces/Navigation/*.cs`
- `src/v6/Brinell.ControlObject.Core/Interfaces/Validation/*.cs`

---

### Phase 8: Page & Context Interfaces

**Goal:** Implement page and test context interfaces

**Deliverables:**

| Component | Reference |
|-----------|-----------|
| `IPageObject` | [SPEC-006-001 §14](../specs/SPEC-006-001-INTERFACES.md) |
| `IBusyPageObject` | [SPEC-006-001 §14](../specs/SPEC-006-001-INTERFACES.md) |
| `ITestContext` | [SPEC-006-001 §15](../specs/SPEC-006-001-INTERFACES.md) |

**Files to Create:**
- `src/v6/Brinell.ControlObject.Core/Interfaces/Page/IPageObject.cs`
- `src/v6/Brinell.ControlObject.Core/Interfaces/Page/IBusyPageObject.cs`
- `src/v6/Brinell.ControlObject.Core/Interfaces/Context/ITestContext.cs`

---

### Phase 9: Async Interfaces (Blazor)

**Goal:** Implement async interfaces for Blazor/Playwright

**Deliverables:**

| Component | Reference |
|-----------|-----------|
| `IAsyncControlObject` | [SPEC-006-001 §16](../specs/SPEC-006-001-INTERFACES.md) |
| `IAsyncClickableControlObject` | [SPEC-006-001 §16](../specs/SPEC-006-001-INTERFACES.md) |
| `IAsyncTextControlObject` | [SPEC-006-001 §16](../specs/SPEC-006-001-INTERFACES.md) |
| `IAsyncSelectorControlObject` | [SPEC-006-001 §16](../specs/SPEC-006-001-INTERFACES.md) |
| `IAsyncToggleControlObject` | [SPEC-006-001 §16](../specs/SPEC-006-001-INTERFACES.md) |
| `IAsyncRangeControlObject` | [SPEC-006-001 §16](../specs/SPEC-006-001-INTERFACES.md) |

**Files to Create:**
- `src/v6/Brinell.ControlObject.Core/Interfaces/Async/*.cs`

---

### Phase 10: MAUI Base Classes

**Goal:** Implement MAUI/Appium base classes

**Deliverables:**

| Component | Reference |
|-----------|-----------|
| `ControlObjectBase` | [SPEC-006-002-CLASSES-FOUNDATION](../specs/SPEC-006-002-CLASSES-FOUNDATION.md) |
| `ClickableControlBase` | [SPEC-006-002-CLASSES-INPUT](../specs/SPEC-006-002-CLASSES-INPUT.md) |
| `TextControlBase` | [SPEC-006-002-CLASSES-INPUT](../specs/SPEC-006-002-CLASSES-INPUT.md) |
| `ToggleControlBase` | [SPEC-006-002-CLASSES-TOGGLE](../specs/SPEC-006-002-CLASSES-TOGGLE.md) |
| `SelectorControlBase` | [SPEC-006-002-CLASSES-SELECTION](../specs/SPEC-006-002-CLASSES-SELECTION.md) |
| `RangeControlBase` | [SPEC-006-002-CLASSES-RANGE](../specs/SPEC-006-002-CLASSES-RANGE.md) |
| `ItemsControlBase` | [SPEC-006-002-CLASSES-COLLECTION](../specs/SPEC-006-002-CLASSES-COLLECTION.md) |
| `ContainerControlBase` | [SPEC-006-002-CLASSES-CONTAINER](../specs/SPEC-006-002-CLASSES-CONTAINER.md) |
| `PageObjectBase` | [SPEC-006-002-CLASSES-FOUNDATION](../specs/SPEC-006-002-CLASSES-FOUNDATION.md) |
| `BusyPageBase` | [SPEC-006-002-CLASSES-FOUNDATION](../specs/SPEC-006-002-CLASSES-FOUNDATION.md) |
| `MauiTestContext` | [SPEC-006-002-CLASSES-CONTEXT](../specs/SPEC-006-002-CLASSES-CONTEXT.md) |

**Files to Create:**
- `src/v6/Brinell.ControlObject.Maui/Controls/Base/*.cs`
- `src/v6/Brinell.ControlObject.Maui/Pages/*.cs`
- `src/v6/Brinell.ControlObject.Maui/Context/*.cs`

---

### Phase 11: MAUI Concrete Controls

**Goal:** Implement MAUI concrete control classes

**Deliverables:**

| Control | MAUI Element | Reference |
|---------|--------------|-----------|
| `ButtonControl` | Button | [SPEC-006-002-CLASSES-INPUT](../specs/SPEC-006-002-CLASSES-INPUT.md) |
| `EntryControl` | Entry | [SPEC-006-002-CLASSES-INPUT](../specs/SPEC-006-002-CLASSES-INPUT.md) |
| `EditorControl` | Editor | [SPEC-006-002-CLASSES-INPUT](../specs/SPEC-006-002-CLASSES-INPUT.md) |
| `LabelControl` | Label | [SPEC-006-002-CLASSES-DISPLAY](../specs/SPEC-006-002-CLASSES-DISPLAY.md) |
| `CheckBoxControl` | CheckBox | [SPEC-006-002-CLASSES-TOGGLE](../specs/SPEC-006-002-CLASSES-TOGGLE.md) |
| `SwitchControl` | Switch | [SPEC-006-002-CLASSES-TOGGLE](../specs/SPEC-006-002-CLASSES-TOGGLE.md) |
| `RadioButtonControl` | RadioButton | [SPEC-006-002-CLASSES-TOGGLE](../specs/SPEC-006-002-CLASSES-TOGGLE.md) |
| `PickerControl` | Picker | [SPEC-006-002-CLASSES-SELECTION](../specs/SPEC-006-002-CLASSES-SELECTION.md) |
| `SliderControl` | Slider | [SPEC-006-002-CLASSES-RANGE](../specs/SPEC-006-002-CLASSES-RANGE.md) |
| `StepperControl` | Stepper | [SPEC-006-002-CLASSES-RANGE](../specs/SPEC-006-002-CLASSES-RANGE.md) |
| `DatePickerControl` | DatePicker | [SPEC-006-002-CLASSES-DATETIME](../specs/SPEC-006-002-CLASSES-DATETIME.md) |
| `TimePickerControl` | TimePicker | [SPEC-006-002-CLASSES-DATETIME](../specs/SPEC-006-002-CLASSES-DATETIME.md) |
| `ProgressBarControl` | ProgressBar | [SPEC-006-002-CLASSES-DISPLAY](../specs/SPEC-006-002-CLASSES-DISPLAY.md) |
| `ActivityIndicatorControl` | ActivityIndicator | [SPEC-006-002-CLASSES-DISPLAY](../specs/SPEC-006-002-CLASSES-DISPLAY.md) |
| `ImageControl` | Image | [SPEC-006-002-CLASSES-DISPLAY](../specs/SPEC-006-002-CLASSES-DISPLAY.md) |
| `CollectionViewControl` | CollectionView | [SPEC-006-002-CLASSES-COLLECTION](../specs/SPEC-006-002-CLASSES-COLLECTION.md) |
| `ListViewControl` | ListView | [SPEC-006-002-CLASSES-COLLECTION](../specs/SPEC-006-002-CLASSES-COLLECTION.md) |
| `ScrollViewControl` | ScrollView | [SPEC-006-002-CLASSES-CONTAINER](../specs/SPEC-006-002-CLASSES-CONTAINER.md) |
| `ExpanderControl` | Expander | [SPEC-006-002-CLASSES-CONTAINER](../specs/SPEC-006-002-CLASSES-CONTAINER.md) |
| `RefreshViewControl` | RefreshView | [SPEC-006-002-CLASSES-CONTAINER](../specs/SPEC-006-002-CLASSES-CONTAINER.md) |
| `SwipeViewControl` | SwipeView | [SPEC-006-002-CLASSES-CONTAINER](../specs/SPEC-006-002-CLASSES-CONTAINER.md) |
| `TabbedPageControl` | TabbedPage | [SPEC-006-002-CLASSES-NAVIGATION](../specs/SPEC-006-002-CLASSES-NAVIGATION.md) |
| `FlyoutControl` | FlyoutPage | [SPEC-006-002-CLASSES-NAVIGATION](../specs/SPEC-006-002-CLASSES-NAVIGATION.md) |
| `MediaElementControl` | MediaElement | [SPEC-006-002-CLASSES-MEDIA](../specs/SPEC-006-002-CLASSES-MEDIA.md) |
| `WebViewControl` | WebView | [SPEC-006-002-CLASSES-MEDIA](../specs/SPEC-006-002-CLASSES-MEDIA.md) |

**Files to Create:**
- `src/v6/Brinell.ControlObject.Maui/Controls/*.cs`

---

### Phase 12: Blazor Base Classes

**Goal:** Implement Blazor/Playwright base classes

**Deliverables:**

| Component | Reference |
|-----------|-----------|
| `AsyncControlObjectBase` | [SPEC-006-002-CLASSES-FOUNDATION](../specs/SPEC-006-002-CLASSES-FOUNDATION.md) |
| `AsyncClickableControlBase` | [SPEC-006-002-CLASSES-INPUT](../specs/SPEC-006-002-CLASSES-INPUT.md) |
| `AsyncTextControlBase` | [SPEC-006-002-CLASSES-INPUT](../specs/SPEC-006-002-CLASSES-INPUT.md) |
| `AsyncPageObjectBase` | [SPEC-006-002-CLASSES-FOUNDATION](../specs/SPEC-006-002-CLASSES-FOUNDATION.md) |
| `BlazorTestContext` | [SPEC-006-002-CLASSES-CONTEXT](../specs/SPEC-006-002-CLASSES-CONTEXT.md) |

**Files to Create:**
- `src/v6/Brinell.ControlObject.Blazor/Controls/Base/*.cs`
- `src/v6/Brinell.ControlObject.Blazor/Pages/*.cs`
- `src/v6/Brinell.ControlObject.Blazor/Context/*.cs`

---

### Phase 13: Blazor Concrete Controls

**Goal:** Implement Blazor concrete control classes

**Deliverables:**

| Control | HTML Element | Reference |
|---------|--------------|-----------|
| `ButtonControl` | `<button>` | [SPEC-006-002-CLASSES-INPUT](../specs/SPEC-006-002-CLASSES-INPUT.md) |
| `InputControl` | `<input type="text">` | [SPEC-006-002-CLASSES-INPUT](../specs/SPEC-006-002-CLASSES-INPUT.md) |
| `TextAreaControl` | `<textarea>` | [SPEC-006-002-CLASSES-INPUT](../specs/SPEC-006-002-CLASSES-INPUT.md) |
| `LinkControl` | `<a>` | [SPEC-006-002-CLASSES-INPUT](../specs/SPEC-006-002-CLASSES-INPUT.md) |
| `CheckBoxControl` | `<input type="checkbox">` | [SPEC-006-002-CLASSES-TOGGLE](../specs/SPEC-006-002-CLASSES-TOGGLE.md) |
| `RadioButtonControl` | `<input type="radio">` | [SPEC-006-002-CLASSES-TOGGLE](../specs/SPEC-006-002-CLASSES-TOGGLE.md) |
| `SelectControl` | `<select>` | [SPEC-006-002-CLASSES-SELECTION](../specs/SPEC-006-002-CLASSES-SELECTION.md) |
| `RangeControl` | `<input type="range">` | [SPEC-006-002-CLASSES-RANGE](../specs/SPEC-006-002-CLASSES-RANGE.md) |
| `DateInputControl` | `<input type="date">` | [SPEC-006-002-CLASSES-DATETIME](../specs/SPEC-006-002-CLASSES-DATETIME.md) |
| `TimeInputControl` | `<input type="time">` | [SPEC-006-002-CLASSES-DATETIME](../specs/SPEC-006-002-CLASSES-DATETIME.md) |
| `ProgressControl` | `<progress>` | [SPEC-006-002-CLASSES-DISPLAY](../specs/SPEC-006-002-CLASSES-DISPLAY.md) |
| `ImageControl` | `<img>` | [SPEC-006-002-CLASSES-DISPLAY](../specs/SPEC-006-002-CLASSES-DISPLAY.md) |
| `TableControl` | `<table>` | [SPEC-006-002-CLASSES-COLLECTION](../specs/SPEC-006-002-CLASSES-COLLECTION.md) |
| `ListControl` | `<ul>`, `<ol>` | [SPEC-006-002-CLASSES-COLLECTION](../specs/SPEC-006-002-CLASSES-COLLECTION.md) |
| `TabControl` | Tab components | [SPEC-006-002-CLASSES-NAVIGATION](../specs/SPEC-006-002-CLASSES-NAVIGATION.md) |
| `NavMenuControl` | `<nav>` | [SPEC-006-002-CLASSES-NAVIGATION](../specs/SPEC-006-002-CLASSES-NAVIGATION.md) |
| `VideoControl` | `<video>` | [SPEC-006-002-CLASSES-MEDIA](../specs/SPEC-006-002-CLASSES-MEDIA.md) |
| `AudioControl` | `<audio>` | [SPEC-006-002-CLASSES-MEDIA](../specs/SPEC-006-002-CLASSES-MEDIA.md) |
| `IFrameControl` | `<iframe>` | [SPEC-006-002-CLASSES-MEDIA](../specs/SPEC-006-002-CLASSES-MEDIA.md) |

**Files to Create:**
- `src/v6/Brinell.ControlObject.Blazor/Controls/*.cs`

---

### Phase 14: Testing Infrastructure

**Goal:** Implement test base classes and fixtures

**Deliverables:**

| Component | Description |
|-----------|-------------|
| `UITestBase` | Base class for UI tests |
| `MauiTestBase` | MAUI-specific test base |
| `BlazorTestBase` | Blazor-specific test base |
| `TestFixture` | Test fixture management |

**Files to Create:**
- `src/v6/Brinell.ControlObject.Testing/UITestBase.cs`
- `src/v6/Brinell.ControlObject.Testing/Maui/MauiTestBase.cs`
- `src/v6/Brinell.ControlObject.Testing/Blazor/BlazorTestBase.cs`
- `src/v6/Brinell.ControlObject.Testing/Fixtures/*.cs`

---

### Phase 15: Sample App Page Objects

**Goal:** Create page objects for sample apps to validate implementation

**Deliverables:**

| Sample | Reference |
|--------|-----------|
| MAUI Sample Page Objects | [DES-001c](../specs/DES-001c-MAUI-SAMPLE-APP-DESIGN.md) |
| Blazor Sample Page Objects | [DES-002c](../specs/DES-002c-BLAZOR-SAMPLE-APP-DESIGN.md) |

**Files to Create:**
- `samples/Brinell.Samples.Maui.UITests.V6/Pages/*.cs`
- `samples/Brinell.Samples.Blazor.UITests.V6/Pages/*.cs`

---

### Phase 16: Integration Tests

**Goal:** Create integration tests using sample apps

**Deliverables:**

| Test Suite | Description |
|------------|-------------|
| MAUI Control Tests | Test all MAUI controls against sample app |
| Blazor Control Tests | Test all Blazor controls against sample app |
| Locator Strategy Tests | Test all locator strategies |
| Page Object Tests | Test page navigation and state |

**Files to Create:**
- `samples/Brinell.Samples.Maui.UITests.V6/Tests/*.cs`
- `samples/Brinell.Samples.Blazor.UITests.V6/Tests/*.cs`

---

## 4. Dependency Graph

```
Phase 1 (Core Foundation)
    ↓
Phases 2-9 (All Interfaces) - Can run in parallel
    ↓
Phase 10-11 (MAUI Implementation) ─────┐
                                       ├→ Phase 14 (Testing)
Phase 12-13 (Blazor Implementation) ───┘
    ↓
Phase 15-16 (Sample Apps & Integration Tests)
```

---

## 5. Validation Criteria

### Per-Phase Validation

| Phase | Validation |
|-------|------------|
| 1-9 | Interfaces compile, XML docs complete |
| 10-11 | MAUI controls work with sample app |
| 12-13 | Blazor controls work with sample app |
| 14 | Test base classes usable |
| 15-16 | All sample app tests pass |

### Requirements Traceability

Each implementation must map to requirements in:
- [REQ-001](../specs/REQ-001-functional-requirements.md)
- [REQ-002](../specs/REQ-002-non-functional-requirements.md)

See [REVIEW-006](../Reviews/REVIEW-006-SPEC-006-Requirements-Compliance.md) for compliance matrix.

---

## 6. Estimated Timeline

| Phase | Effort | Dependencies |
|-------|--------|--------------|
| 1 | 2 days | None |
| 2-9 | 4 days | Phase 1 |
| 10 | 3 days | Phase 1-9 |
| 11 | 5 days | Phase 10 |
| 12 | 2 days | Phase 1-9 |
| 13 | 4 days | Phase 12 |
| 14 | 2 days | Phase 10, 12 |
| 15 | 2 days | Phase 11, 13 |
| 16 | 3 days | Phase 15 |
| **Total** | **~27 days** | |

---

## 7. Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Appium API changes | High | Pin Appium.WebDriver version |
| Playwright API changes | High | Pin Playwright version |
| MAUI control accessibility gaps | Medium | Document unsupported controls |
| Locator strategy platform gaps | Medium | Throw `LocatorNotFoundException` |

---

## 8. Success Criteria

1. ✅ All interfaces implemented per SPEC-006-001
2. ✅ All base classes implemented per SPEC-006-002-*
3. ✅ MAUI sample app tests pass
4. ✅ Blazor sample app tests pass
5. ✅ All locator strategies work on supported platforms
6. ✅ Nullable expected parameter behavior verified
7. ✅ Requirements coverage ≥ 95% per REVIEW-006

---

**End of Plan**
