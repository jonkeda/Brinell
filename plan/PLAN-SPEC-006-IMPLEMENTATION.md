# PLAN: SPEC-006 ControlObject Framework Implementation

**Version:** 1.2  
**Status:** Draft  
**Date:** January 4, 2026  
**Last Updated:** January 4, 2026 (Aligned with POC implementation approach)  
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
| [SPEC-006-004-TESTING-GUIDE](../specs/SPEC-006-004-TESTING-GUIDE.md) | Testing patterns & mockability |
| [PLAN-SPEC-006b-POC](./PLAN-SPEC-006b-POC.md) | Vertical slice POC implementation |
| [PLAN-SPEC-006c-POC](./PLAN-SPEC-006c-POC.md) | POC updates from review findings |

---

## 2. Project Structure

### Implementation Approach: Coexistence in Existing Projects

The v6 implementation coexists with v1.x in the same projects using `ControlObject6/` folders. This approach:
- Avoids project proliferation
- Enables gradual migration
- Shares existing test infrastructure
- Reuses existing exceptions from `Brinell.Core.Exceptions`

### Folder Structure

| Existing Project | New Folder | Description |
|------------------|------------|-------------|
| `Brinell.Core` | `ControlObject6/` | Interfaces, locators (reuse existing exceptions) |
| `Brinell.Maui` | `ControlObject6/` | MAUI/Appium base classes and controls |
| `Brinell.Blazor` (new) | `ControlObject6/` | Blazor/Playwright implementation |
| Sample UITests | `ControlObject6/` | Page objects and test base classes |

### Namespace Convention

```
Brinell.Core.ControlObject6
Brinell.Core.ControlObject6.Locators
Brinell.Maui.ControlObject6
Brinell.Maui.ControlObject6.Controls
Brinell.Maui.ControlObject6.Pages
Brinell.Blazor.ControlObject6
Brinell.Blazor.ControlObject6.Controls
Brinell.Blazor.ControlObject6.Pages
```

### Exception Reuse

Reuse existing exceptions from `Brinell.Core.Exceptions`:
- `ElementNotFoundException` - Element not found
- `UITestTimeoutException` - Timeout waiting for element
- `AssertionException` - Assertion failures

No new exception classes needed for v6.

---

## 3. Implementation Phases

### Phase 1: Core Foundation

**Goal:** Implement core interfaces, locators (reuse existing exceptions)

**Deliverables:**

| Component | Reference |
|-----------|-----------|
| `ControlLocator` class | [SPEC-006-001 §1](../specs/SPEC-006-001-INTERFACES.md) |
| `LocatorStrategy` enum | [SPEC-006-001 §1](../specs/SPEC-006-001-INTERFACES.md) |
| `By` static factory | [SPEC-006-001 §1](../specs/SPEC-006-001-INTERFACES.md) |
| `IControlObject` | [SPEC-006-001 §2](../specs/SPEC-006-001-INTERFACES.md) |
| `IInteractiveControlObject` | [SPEC-006-001 §2](../specs/SPEC-006-001-INTERFACES.md) |
| `IFocusableControlObject` | [SPEC-006-001 §2](../specs/SPEC-006-001-INTERFACES.md) |

**Files to Create:**
- `src/Brinell.Core/ControlObject6/Locators/ControlLocator.cs`
- `src/Brinell.Core/ControlObject6/Locators/LocatorStrategy.cs`
- `src/Brinell.Core/ControlObject6/Locators/By.cs`
- `src/Brinell.Core/ControlObject6/Interfaces/IControlObject.cs`
- `src/Brinell.Core/ControlObject6/Interfaces/IInteractiveControlObject.cs`
- `src/Brinell.Core/ControlObject6/Interfaces/IFocusableControlObject.cs`

**Note:** Reuse existing exceptions from `Brinell.Core.Exceptions`

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
- `src/Brinell.Core/ControlObject6/Interfaces/Input/IClickableControlObject.cs`
- `src/Brinell.Core/ControlObject6/Interfaces/Input/ITextControlObject.cs`
- `src/Brinell.Core/ControlObject6/Interfaces/Input/IEditableTextControlObject.cs`
- `src/Brinell.Core/ControlObject6/Interfaces/Input/ISearchControlObject.cs`

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
- `src/Brinell.Core/ControlObject6/Interfaces/Toggle/*.cs`
- `src/Brinell.Core/ControlObject6/Interfaces/Selection/*.cs`

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
- `src/Brinell.Core/ControlObject6/Interfaces/Range/*.cs`
- `src/Brinell.Core/ControlObject6/Interfaces/DateTime/*.cs`

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
- `src/Brinell.Core/ControlObject6/Interfaces/Collection/*.cs`
- `src/Brinell.Core/ControlObject6/Interfaces/Container/*.cs`

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
- `src/Brinell.Core/ControlObject6/Interfaces/Display/*.cs`
- `src/Brinell.Core/ControlObject6/Interfaces/Media/*.cs`

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
- `src/Brinell.Core/ControlObject6/Interfaces/Navigation/*.cs`
- `src/Brinell.Core/ControlObject6/Interfaces/Validation/*.cs`

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
- `src/Brinell.Core/ControlObject6/Interfaces/Page/IPageObject.cs`
- `src/Brinell.Core/ControlObject6/Interfaces/Page/IBusyPageObject.cs`
- `src/Brinell.Core/ControlObject6/Interfaces/Context/ITestContext.cs`

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
- `src/Brinell.Core/ControlObject6/Interfaces/Async/*.cs`

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
- `src/Brinell.Maui/ControlObject6/Controls/Base/*.cs`
- `src/Brinell.Maui/ControlObject6/Pages/*.cs`
- `src/Brinell.Maui/ControlObject6/Context/*.cs`

**POC-Discovered Patterns (from PLAN-SPEC-006c-POC):**
- Add string constructor overloads to all controls (e.g., `ButtonControl(string automationId)`)
- Base classes should have `virtual` methods for customization:
  - `ClickableControlBase`: `virtual void PerformClick()`, `virtual void PerformDoubleClick()`
  - `TextControlBase`: `virtual void PerformSetText()`, `virtual void PerformClear()`
- Add `Log()` method calls to all operations for debugging
- Use `new` keyword instead of factory pattern for control instantiation

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
- `src/Brinell.Maui/ControlObject6/Controls/*.cs`

---

### Phase 12: Blazor Base Classes

**Goal:** Implement Blazor/Playwright base classes (new project: Brinell.Blazor)

**Deliverables:**

| Component | Reference |
|-----------|-----------|
| `AsyncControlObjectBase` | [SPEC-006-002-CLASSES-FOUNDATION](../specs/SPEC-006-002-CLASSES-FOUNDATION.md) |
| `AsyncClickableControlBase` | [SPEC-006-002-CLASSES-INPUT](../specs/SPEC-006-002-CLASSES-INPUT.md) |
| `AsyncTextControlBase` | [SPEC-006-002-CLASSES-INPUT](../specs/SPEC-006-002-CLASSES-INPUT.md) |
| `AsyncPageObjectBase` | [SPEC-006-002-CLASSES-FOUNDATION](../specs/SPEC-006-002-CLASSES-FOUNDATION.md) |
| `BlazorTestContext` | [SPEC-006-002-CLASSES-CONTEXT](../specs/SPEC-006-002-CLASSES-CONTEXT.md) |

**Files to Create:**
- `src/Brinell.Blazor/ControlObject6/Controls/Base/*.cs`
- `src/Brinell.Blazor/ControlObject6/Pages/*.cs`
- `src/Brinell.Blazor/ControlObject6/Context/*.cs`

**POC-Discovered Patterns (from PLAN-SPEC-006c-POC):**
- Add string constructor overloads for Playwright selector convenience
- Use `data-testid` attributes for element identification (not `id`)
- Add `virtual` methods for Playwright-specific operations
- Add `Log()` calls for debugging async operations

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
- `src/Brinell.Blazor/ControlObject6/Controls/*.cs`

---

### Phase 14: Testing Infrastructure

**Goal:** Implement test base classes and fixtures (in existing sample projects)

**Deliverables:**

| Component | Description |
|-----------|-------------|
| `UITestBase` | Base class for UI tests |
| `MauiTestBase` | MAUI-specific test base |
| `BlazorTestBase` | Blazor-specific test base |
| `TestFixture` | Test fixture management |

**Files to Create:**
- `samples/Brinell.Samples.Maui.UITests/ControlObject6/UITestBase.cs`
- `samples/Brinell.Samples.Maui.UITests/ControlObject6/MauiTestBase.cs`
- `samples/Brinell.Samples.Blazor.UITests/ControlObject6/BlazorTestBase.cs`
- `samples/Brinell.Samples.Blazor.UITests/ControlObject6/Fixtures/*.cs`

---

### Phase 15: Sample App Page Objects

**Goal:** Create page objects for sample apps to validate implementation

**Deliverables:**

| Sample | Reference |
|--------|-----------|
| MAUI Sample Page Objects | [DES-001c](../specs/DES-001c-MAUI-SAMPLE-APP-DESIGN.md) |
| Blazor Sample Page Objects | [DES-002c](../specs/DES-002c-BLAZOR-SAMPLE-APP-DESIGN.md) |

**Files to Create:**
- `samples/Brinell.Samples.Maui.UITests/ControlObject6/Pages/*.cs`
- `samples/Brinell.Samples.Blazor.UITests/ControlObject6/Pages/*.cs`

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
- `samples/Brinell.Samples.Maui.UITests/ControlObject6/Tests/*.cs`
- `samples/Brinell.Samples.Blazor.UITests/ControlObject6/Tests/*.cs`

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
| MAUI app startup delay blocking tests | High | Use poll-wait pattern (see §10.1) |
| Orphaned app processes after tests | Medium | Use `ms:forcequit` capability (see §10.2) |
| Blazor locator strategy mismatch | High | Use `data-testid` attributes (see §10.3) |

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

## 9. POC-Discovered Design Patterns (PLAN-SPEC-006c-POC)

The POC implementation revealed design patterns that should be applied throughout the framework. See [PLAN-SPEC-006c-POC](./PLAN-SPEC-006c-POC.md) for detailed implementation.

### 9.1 String Constructor Overloads

All controls should have convenience constructors that accept a string AutomationId/TestId:

```csharp
// Instead of requiring ControlLocator
var button = new ButtonControl(By.AutomationId("submit-btn"), page);

// Allow simple string shorthand (assumes AutomationId for MAUI, TestId for Blazor)
var button = new ButtonControl("submit-btn", page);
```

**Implementation:**
```csharp
public class ButtonControl : ClickableControlBase
{
    public ButtonControl(ControlLocator locator, IPageObject page) : base(locator, page) { }
    
    // Convenience constructor - converts string to default locator
    public ButtonControl(string automationId, IPageObject page) 
        : this(By.AutomationId(automationId), page) { }
}
```

### 9.2 Virtual Base Class Methods

Base classes should have `virtual` methods for platform-specific customization:

```csharp
public abstract class ClickableControlBase : ControlObjectBase, IClickableControlObject
{
    public void Click()
    {
        Log($"Click: {Locator}");
        PerformClick();  // Virtual for customization
    }
    
    protected virtual void PerformClick()
    {
        GetElement().Click();
    }
    
    protected virtual void PerformDoubleClick()
    {
        // Platform-specific double-click
    }
}
```

**Pattern applies to:**
- `ClickableControlBase`: `PerformClick()`, `PerformDoubleClick()`
- `TextControlBase`: `PerformSetText()`, `PerformClear()`
- `ToggleControlBase`: `PerformToggle()`
- `SelectorControlBase`: `PerformSelect()`

### 9.3 Logging Pattern

All control operations should include `Log()` calls for debugging:

```csharp
public class ButtonControl : ClickableControlBase
{
    public override void Click()
    {
        Log($"Click button: {Locator}");
        base.Click();
    }
}

public class TextInputControl : TextControlBase
{
    public override void SetText(string text)
    {
        Log($"SetText '{text}' to: {Locator}");
        base.SetText(text);
    }
}
```

**Log placement:**
- Before action (intent)
- After action if result is significant
- On errors with context

### 9.4 Factory Pattern Removal

The POC determined that factory pattern adds unnecessary complexity. Use `new` keyword directly:

```csharp
// ❌ REMOVED - Factory pattern
var button = page.CreateControl<ButtonControl>(By.AutomationId("btn"));
var button = ControlFactory.Create<ButtonControl>(locator, context);

// ✅ PREFERRED - Direct instantiation
var button = new ButtonControl("submit-btn", page);
var input = new TextInputControl(By.AutomationId("username"), page);
```

**Rationale:**
- Simpler and more intuitive
- No hidden complexity
- Type safety at compile time
- Works better with dependency injection

### 9.5 PageObjectBase Control Property Pattern

PageObjectBase should expose a `TControl` factory method for fluent control creation:

```csharp
public abstract class PageObjectBase<TPage> : IPageObject where TPage : PageObjectBase<TPage>
{
    // Control creation helper - uses page as parent
    protected TControl Control<TControl>(ControlLocator locator) 
        where TControl : IControlObject
    {
        return (TControl)Activator.CreateInstance(typeof(TControl), locator, this);
    }
    
    protected TControl Control<TControl>(string automationId) 
        where TControl : IControlObject
    {
        return Control<TControl>(By.AutomationId(automationId));
    }
}

// Usage in page object
public class LoginPage : PageObjectBase<LoginPage>
{
    public ButtonControl SubmitButton => Control<ButtonControl>("submit-btn");
    public TextInputControl UsernameInput => Control<TextInputControl>("username");
}
```

---

## 10. Lessons Learned (POC Testing - January 4, 2026)

### 10.1 MAUI: App Startup Wait Strategy

**Problem:** Using `ms:waitForAppLaunch` with a fixed delay (e.g., 10 seconds) causes unnecessary waiting even when the app is ready in 1-2 seconds. This added 10.6 minutes to a 44-test run.

**Solution:** Use a poll-wait pattern:
1. Set `ms:waitForAppLaunch` to a minimal value (1 second)
2. Implement `WaitForAppReady()` that polls for a known element
3. Poll at 500ms intervals until timeout

```csharp
// In MauiTestContext.Initialize() or test base class
private void WaitForAppReady(int maxWaitSeconds = 30)
{
    var stopwatch = Stopwatch.StartNew();
    while (stopwatch.Elapsed.TotalSeconds < maxWaitSeconds)
    {
        try
        {
            // Check for a known stable element (e.g., navigation bar, main page title)
            var element = Driver.FindElement(MobileBy.AccessibilityId("nav-button"));
            if (element != null && element.Displayed)
            {
                Log($"App ready after {stopwatch.ElapsedMilliseconds}ms");
                return;
            }
        }
        catch (NoSuchElementException)
        {
            // Element not found yet, continue polling
        }
        Thread.Sleep(500);
    }
    throw new TimeoutException($"App not ready after {maxWaitSeconds}s");
}
```

**Result:** Test run reduced from 10.6 minutes to 2 minutes 47 seconds.

---

### 10.2 MAUI: App Process Cleanup

**Problem:** After test runs, orphaned MAUI app processes remain running, consuming resources and potentially causing port conflicts on subsequent runs.

**Solution:** Add `ms:forcequit` capability to Appium options:

```csharp
// In MauiTestSettings or AppiumOptions setup
var options = new AppiumOptions();
options.AddAdditionalAppiumOption("ms:forcequit", true);  // Force quit app after session
```

**Best Practice:** Always verify no orphaned processes after test runs:
```powershell
Get-Process -Name "YourApp.Name" -ErrorAction SilentlyContinue
```

---

### 10.3 Blazor: Locator Strategy with Playwright

**Problem:** Playwright's `GetByTestId()` looks for `data-testid` attribute, not plain `id` attribute. Using `By.TestId("element-id")` fails if the HTML uses `id="element-id"` instead of `data-testid="element-id"`.

**Solution:** Blazor apps must use `data-testid` attributes for test identification:

```html
<!-- ❌ WRONG - Plain id attribute -->
<button id="submit-btn">Submit</button>
<input id="username-input" />

<!-- ✅ CORRECT - data-testid attribute for Playwright -->
<button data-testid="submit-btn">Submit</button>
<input data-testid="username-input" />
```

**Locator Strategy Mapping:**

| By Method | Playwright Implementation | HTML Attribute |
|-----------|---------------------------|----------------|
| `By.TestId("x")` | `page.GetByTestId("x")` | `data-testid="x"` |
| `By.Id("x")` | `page.Locator("#x")` | `id="x"` |
| `By.AutomationId("x")` | `page.Locator("[data-automation-id='x']")` | `data-automation-id="x"` |
| `By.Css(".class")` | `page.Locator(".class")` | `class="class"` |
| `By.Role("button")` | `page.GetByRole(AriaRole.Button)` | `role="button"` |

**Recommendation:** Prefer `data-testid` for Blazor apps as it:
- Separates test concerns from styling/semantics
- Works consistently with Playwright's recommended patterns
- Doesn't conflict with CSS selectors or accessibility requirements

---

### 10.4 Blazor: Page Object PageLocator

**Problem:** Page objects use `PageLocator` to detect when a page is loaded. If the element specified in `PageLocator` doesn't exist in the HTML, `WaitLoadedAsync()` fails.

**Solution:** Ensure every page has a consistent container element with `data-testid`:

```html
<!-- Login.razor -->
<div data-testid="login-form" class="row">
    <!-- form contents -->
</div>
```

```csharp
// LoginPage6.cs
protected override ControlLocator PageLocator => By.TestId("login-form");
```

---

### 10.5 Process Management for External App Windows

**Problem:** When starting Blazor or MAUI apps in separate PowerShell windows for testing, multiple orphaned windows accumulate over test sessions.

**Solution:** Track and clean up spawned processes:

```powershell
# Before running tests - clean up old processes
Stop-Process -Name "dotnet" -Force -ErrorAction SilentlyContinue
Get-Process -Name "powershell" | Where-Object { $_.StartTime -gt (Get-Date).AddHours(-2) -and $_.Id -ne $PID } | Stop-Process -Force

# Alternative: Use background jobs with proper cleanup
$job = Start-Job { dotnet run --urls "http://localhost:5180" }
# ... run tests ...
Stop-Job $job
Remove-Job $job
```

---

### 10.6 Test Execution Summary (POC Results)

| Platform | Tests | Passed | Failed | Duration | Notes |
|----------|-------|--------|--------|----------|-------|
| MAUI | 44 | 39 | 5 | 2m 47s | After poll-wait fix (was 10.6 min) |
| Blazor | 46 | 46 | 0 | 78s | After data-testid fix |

---

## 11. Implementation Checklist Updates

Based on lessons learned, add these verification steps:

### Phase 10-11 (MAUI Implementation)
- [ ] Verify `ms:waitForAppLaunch` uses poll-wait pattern
- [ ] Verify `ms:forcequit` capability is set
- [ ] Verify no orphaned processes after test cleanup
- [ ] Document any control-specific accessibility gaps

### Phase 12-13 (Blazor Implementation)
- [ ] Verify all sample app elements use `data-testid` attributes
- [ ] Verify `By.TestId()` maps to `GetByTestId()` correctly
- [ ] Verify page objects have valid `PageLocator` elements
- [ ] Document locator strategy mapping table

### Phase 14-16 (Testing & Integration)
- [ ] Verify process cleanup in test teardown
- [ ] Add performance benchmarks for app startup
- [ ] Document expected test run times

---

**End of Plan**
