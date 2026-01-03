# PLAN-001: Platform Review Summary

**Created:** January 2, 2026  
**Status:** Active  
**Purpose:** Summary of all platform implementations against specs v3.2

---

## 1. Executive Summary

This document provides a comprehensive review of all Brinell platform implementations against the updated specifications (REQ-001 v3.2, REQ-002 v3.1, SPEC-001 v3.1, DES-001 v3.1).

### Platform Compliance Matrix

| Platform | Base Classes | Interface Impl | Is/Wait/Check/Assert | Container Support | BusyPageBase | Scroll Support | Score |
|----------|--------------|----------------|---------------------|-------------------|--------------|----------------|-------|
| **Brinell.Core** | N/A | ✅ Defined | ✅ In interfaces | N/A | N/A | N/A | 85% |
| **Brinell.Maui** | ✅ Complete | ✅ Complete | ✅ Complete | ⚠️ Partial | ✅ Yes | ⚠️ Partial | 90% |
| **Brinell.Wpf** | ✅ Complete | ✅ Complete | ✅ Complete | ⚠️ Partial | ❌ Missing | ⚠️ Partial | 80% |
| **Brinell.WinForms** | ⚠️ Incomplete | ⚠️ Partial | ⚠️ Partial | ❌ Missing | ❌ Missing | ❌ Missing | 55% |
| **Brinell.Html** | ⚠️ Incomplete | ⚠️ Partial | ⚠️ Partial | ❌ Missing | ❌ Missing | ❌ Missing | 60% |
| **Brinell.Html.Playwright** | ⚠️ Incomplete | ⚠️ Partial | ⚠️ Partial | ❌ Missing | ❌ Missing | ❌ Missing | 60% |
| **Brinell.Stride** | ⚠️ Incomplete | ⚠️ Partial | ⚠️ Partial | ❌ Missing | ❌ Missing | N/A | 50% |

---

## 2. Core Review

### Current State

| Component | Status | Notes |
|-----------|--------|-------|
| `ITestContext` | ✅ | Complete interface |
| `IPageObject` | ✅ | Complete interface |
| `IControlObject` | ✅ | Complete interface |
| `ITextControl` | ✅ | Complete interface |
| `IClickableControl` | ✅ | Complete interface |
| `IContentControl` | ✅ | Complete interface |
| `IToggleControl` | ✅ | Complete interface |
| `ISelectorControl` | ✅ | Complete interface |
| `IRangeControl` | ✅ | Complete interface |
| `IItemsControl` | ✅ | Complete interface |
| `IContainerControl` | ✅ | Added in PLAN-002 |
| `IScrollableControl` | ✅ | Added in PLAN-002 |
| `IDriverAdapter` | ✅ | Kept per AD-002 v3.2 (interfaces OK) |
| `IElementAdapter` | ✅ | Kept per AD-002 v3.2 (interfaces OK) |
| Platform extensions | ✅ | Added PlatformExtensions.cs |
| Configuration classes | ✅ | Added UITestConfiguration.cs |
| `UITestTimeoutException` | ✅ | Added in PLAN-002 |
| `InvalidStateException` | ✅ | Added in PLAN-002 |

### Required Actions for Core

**✅ COMPLETE - See [PLAN-002](PLAN-002-Core-Update.md)**

---

## 3. Platform Reviews

### 3.1 Brinell.Maui (Score: 90%)

**Maturity:** Production Ready

#### Base Classes Present
| Class | Status | Notes |
|-------|--------|-------|
| `ControlBase` | ✅ | Complete |
| `PageBase` | ✅ | Complete |
| `BusyPageBase` | ✅ | In PageBase.cs |
| `ContentControlBase` | ✅ | Complete |
| `TextControlBase` | ✅ | Complete |
| `ToggleControlBase` | ✅ | Complete |
| `SelectorControlBase` | ✅ | Complete |
| `RangeControlBase` | ✅ | Complete |
| `ItemsControlBase` | ✅ | Complete |

#### Spec Compliance
| Requirement | Status | Notes |
|-------------|--------|-------|
| FR-002.5 Interface Hierarchy | ✅ | Implements all Core interfaces |
| FR-002.6 Container Support | ⚠️ | Partial - not all controls support containers |
| FR-002.7 Scroll Support | ⚠️ | ScrollViewControl exists but incomplete |
| FR-004.4.1 Assert calls Check | ✅ | Verified |
| FR-005.4.1 BusyPageBase | ✅ | Implemented |
| FR-005.5 Sync Operations | ✅ | All methods synchronous |
| FR-007.2.1 Mobile Gestures | ✅ | Swipe, LongPress, DoubleTap |
| AD-002 No Adapters | ⚠️ | Uses internal AppiumDriverAdapter |

#### Controls (27 total)
Button, Label, Entry, Editor, SearchBar, CheckBox, Switch, Picker, Slider, Stepper, ProgressBar, CollectionView, CarouselView, DatePicker, TimePicker, ScrollView, RefreshView, SwipeView, Image, WebView, ActivityIndicator, Shell, TabBar, FlyoutItem, Frame, Border, ContentView

#### Required Actions
- [ ] Add container constructor to `ContentControlBase`
- [ ] Add container constructor to `TextControlBase`
- [ ] Add container constructor to `RangeControlBase`
- [ ] Complete `ScrollViewControl` with FR-002.7 methods
- [ ] Replace `Thread.Sleep(500)` in SelectorControlBase with configurable wait
- [ ] Remove `IDriverAdapter` interface implementation from `AppiumDriverAdapter`

---

### 3.2 Brinell.Wpf (Score: 80%)

**Maturity:** Production Ready with gaps

#### Base Classes Present
| Class | Status | Notes |
|-------|--------|-------|
| `ControlBase` | ✅ | Complete |
| `PageBase` | ✅ | Complete |
| `BusyPageBase` | ❌ | **Missing** - Required by FR-005.4.1 |
| `ContentControlBase` | ✅ | Complete |
| `TextControlBase` | ✅ | Complete |
| `ToggleControlBase` | ✅ | Complete |
| `SelectorControlBase` | ✅ | Complete |
| `RangeControlBase` | ✅ | Complete |
| `ItemsControlBase` | ✅ | Complete |

#### Spec Compliance
| Requirement | Status | Notes |
|-------------|--------|-------|
| FR-002.5 Interface Hierarchy | ✅ | Implements all Core interfaces |
| FR-002.6 Container Support | ⚠️ | ControlBase supports but needs verification |
| FR-002.7 Scroll Support | ⚠️ | Not implemented |
| FR-004.4.1 Assert calls Check | ✅ | Verified |
| FR-005.4.1 BusyPageBase | ❌ | **Missing** |
| FR-005.5 Sync Operations | ✅ | All methods synchronous |
| FR-007.1 FlaUI/UIA3 | ✅ | Direct access |
| AD-002 No Adapters | ✅ | No adapter in WPF |

#### Controls (12+)
Button, TextBox, Label, CheckBox, RadioButton, ComboBox, ListBox, Slider, ProgressBar, DataGrid, TabControl, TabItem, Window, Menu, MenuItem

#### Required Actions
- [ ] **Add `BusyPageBase` class** - High priority
- [ ] Add container support verification and tests
- [ ] Add scroll-to-element support (`ScrollToElement`, `ScrollToTop`, `ScrollToBottom`)
- [ ] Add `IContainerControl` implementation
- [ ] Verify all controls follow FR-004.4.1 (Assert calls Check)

---

### 3.3 Brinell.WinForms (Score: 55%)

**Maturity:** Early Development

#### Base Classes Present
| Class | Status | Notes |
|-------|--------|-------|
| `ControlBase` | ✅ | Present |
| `PageBase` | ✅ | Present |
| `BusyPageBase` | ❌ | **Missing** |
| `ContentControlBase` | ❌ | **Missing** - uses InputControlBase |
| `TextControlBase` | ❌ | **Missing** - uses InputControlBase |
| `ToggleControlBase` | ✅ | Present |
| `SelectorControlBase` | ✅ | Present |
| `RangeControlBase` | ❌ | **Missing** |
| `ItemsControlBase` | ❌ | **Missing** |

#### Spec Compliance
| Requirement | Status | Notes |
|-------------|--------|-------|
| FR-002.5 Interface Hierarchy | ⚠️ | Partial - missing several interfaces |
| FR-002.6 Container Support | ❌ | Not implemented |
| FR-002.7 Scroll Support | ❌ | Not implemented |
| FR-004.4.1 Assert calls Check | ⚠️ | Needs verification |
| FR-005.4.1 BusyPageBase | ❌ | Missing |
| FR-005.5 Sync Operations | ✅ | Synchronous |
| FR-007.4 FlaUI/UIA3 | ✅ | Direct access |
| AD-002 No Adapters | ✅ | No adapter |

#### Required Actions
- [ ] Rename `InputControlBase` → `TextControlBase` for consistency
- [ ] Add `ContentControlBase` class
- [ ] Add `RangeControlBase` class  
- [ ] Add `ItemsControlBase` class
- [ ] Add `BusyPageBase` class
- [ ] Implement container support in ControlBase
- [ ] Implement full Is/Wait/Check/Assert pattern
- [ ] Add scroll support

---

### 3.4 Brinell.Html (Selenium) (Score: 60%)

**Maturity:** Early Development

#### Base Classes Present
| Class | Status | Notes |
|-------|--------|-------|
| `ControlBase` | ✅ | Present |
| `PageBase` | ✅ | Present |
| `BusyPageBase` | ❌ | **Missing** |
| `ContentControlBase` | ✅ | Present |
| `TextControlBase` | ✅ | Present |
| `ToggleControlBase` | ✅ | Present |
| `SelectorControlBase` | ✅ | Present |
| `RangeControlBase` | ✅ | Present |
| `ItemsControlBase` | ❌ | **Missing** |

#### Spec Compliance
| Requirement | Status | Notes |
|-------------|--------|-------|
| FR-002.5 Interface Hierarchy | ⚠️ | Missing ItemsControl |
| FR-002.6 Container Support | ❌ | Not implemented |
| FR-002.7 Scroll Support | ❌ | Not implemented |
| FR-004.4.1 Assert calls Check | ⚠️ | Needs verification |
| FR-005.4.1 BusyPageBase | ❌ | Missing |
| FR-005.5 Sync Operations | ✅ | Synchronous |
| FR-007.3 Selenium WebDriver | ✅ | Used |
| AD-002 No Adapters | ⚠️ | Has Abstractions folder |

#### Required Actions
- [ ] Add `BusyPageBase` class
- [ ] Add `ItemsControlBase` class
- [ ] Implement container support
- [ ] Implement scroll support (JavaScript scroll)
- [ ] Verify Is/Wait/Check/Assert pattern
- [ ] Add web-specific methods: `GetAttribute()`, `GetCssProperty()`, `ExecuteScript()`

---

### 3.5 Brinell.Html.Playwright (Score: 60%)

**Maturity:** Early Development

#### Base Classes Present
| Class | Status | Notes |
|-------|--------|-------|
| `ControlBase` | ✅ | Present |
| `ControlBaseAsync` | ✅ | Async variant exists |
| `PageBase` | ✅ | Present |
| `BusyPageBase` | ❌ | **Missing** |
| `ContentControlBase` | ✅ | Present |
| `TextControlBase` | ✅ | Present |
| `ToggleControlBase` | ✅ | Present |
| `SelectorControlBase` | ✅ | Present |
| `RangeControlBase` | ✅ | Present |
| `ItemsControlBase` | ❌ | **Missing** |

#### Spec Compliance
| Requirement | Status | Notes |
|-------------|--------|-------|
| FR-002.5 Interface Hierarchy | ⚠️ | Missing ItemsControl |
| FR-002.6 Container Support | ✅ | Implemented with `ILocator? container` |
| FR-002.7 Scroll Support | ❌ | Not implemented |
| FR-004.4.1 Assert calls Check | ⚠️ | Needs verification |
| FR-005.4.1 BusyPageBase | ❌ | Missing |
| FR-005.5 Sync Operations | ✅ | **Dual API** - Both sync and async per AD-009 v3.2 |
| FR-007.3 Playwright | ✅ | Used |
| AD-002 No Adapters | ✅ | No adapter |

#### Architecture Notes

Per AD-009 v3.2, Playwright provides **dual sync/async API**:
- `ControlBase` - Sync wrappers for simple tests
- `ControlBaseAsync` - Native async for async tests (`IControlObjectAsync`)

#### Required Actions
- [ ] Add `BusyPageBase` class
- [ ] Add `ItemsControlBase` class
- [ ] Implement scroll support
- [ ] Verify FR-004.4.1 compliance (Assert calls Check)
- [ ] Add web-specific extension methods

---

### 3.6 Brinell.Stride (Score: 50%)

**Maturity:** Experimental

#### Base Classes Present
| Class | Status | Notes |
|-------|--------|-------|
| `StrideControlBase` | ✅ | Present |
| `StridePageBase` | ❌ | **Missing** - in Pages/ folder? |
| `BusyPageBase` | ❌ | **Missing** |
| `StrideContentControlBase` | ✅ | Present |
| `StrideTextControlBase` | ✅ | Present |
| `StrideToggleControlBase` | ✅ | Present |
| `StrideSelectorControlBase` | ✅ | Present |
| `StrideRangeControlBase` | ✅ | Present |
| `StrideItemsControlBase` | ❌ | **Missing** |

#### Spec Compliance
| Requirement | Status | Notes |
|-------------|--------|-------|
| FR-002.5 Interface Hierarchy | ⚠️ | Missing ItemsControl |
| FR-002.6 Container Support | ❌ | Not implemented |
| FR-002.7 Scroll Support | ❌ | **Required** - Stride UI has scrollable panels |
| FR-004.4.1 Assert calls Check | ⚠️ | Needs verification |
| FR-005.4.1 BusyPageBase | ❌ | Missing |
| FR-005.5 Sync Operations | ✅ | Uses named pipes (sync) |
| FR-007.5 Named Pipes | ✅ | Implemented |

#### Architecture Notes

- **Brinell.Stride** - Test-side framework that communicates with in-game component
- **Brinell.Stride.Automation** - In-game automation handler (Stride has no native automation support)
- Control naming follows `Stride{ControlName}` pattern (e.g., `StrideButtonControl`)

#### Required Actions
- [ ] Add `StridePageBase` class (or rename existing)
- [ ] Add `StrideBusyPageBase` class
- [ ] Add `StrideItemsControlBase` class
- [ ] Add scroll support for scrollable UI panels
- [ ] Verify Is/Wait/Check/Assert pattern
- [ ] Document Stride-specific architecture (Brinell.Stride vs Brinell.Stride.Automation)

---

## 4. Cross-Platform Consistency Check

### FR-004.4.1: Assert Calls Check Pattern

All Assert methods must call corresponding Check method first.

| Platform | AssertExists | AssertVisible | AssertEnabled | AssertText | Status |
|----------|-------------|---------------|---------------|------------|--------|
| MAUI | ✅ | ✅ | ✅ | ✅ | Verified |
| WPF | ✅ | ✅ | ✅ | ✅ | Verified |
| WinForms | ⚠️ | ⚠️ | ⚠️ | ⚠️ | Needs Check |
| Html | ⚠️ | ⚠️ | ⚠️ | ⚠️ | Needs Check |
| Html.Playwright | ⚠️ | ⚠️ | ⚠️ | ⚠️ | Needs Check |
| Stride | ⚠️ | ⚠️ | ⚠️ | ⚠️ | Needs Check |

### FR-002.6: Container Support

All ControlBase classes must accept optional container parameter.

| Platform | ControlBase | TextControl | ContentControl | ToggleControl | Status |
|----------|-------------|-------------|----------------|---------------|--------|
| MAUI | ✅ | ⚠️ | ⚠️ | ✅ | Partial |
| WPF | ⚠️ | ⚠️ | ⚠️ | ⚠️ | Needs Check |
| WinForms | ❌ | ❌ | ❌ | ❌ | Missing |
| Html | ❌ | ❌ | ❌ | ❌ | Missing |
| Html.Playwright | ❌ | ❌ | ❌ | ❌ | Missing |
| Stride | ❌ | ❌ | ❌ | ❌ | Missing |

### FR-005.4.1: BusyPageBase

All platforms should provide BusyPageBase class.

| Platform | BusyPageBase | IsBusy() | WaitForNotBusy() | IsReady() Override |
|----------|--------------|----------|------------------|-------------------|
| MAUI | ✅ | ✅ | ✅ | ✅ |
| WPF | ❌ | ❌ | ❌ | ❌ |
| WinForms | ❌ | ❌ | ❌ | ❌ |
| Html | ❌ | ❌ | ❌ | ❌ |
| Html.Playwright | ❌ | ❌ | ❌ | ❌ |
| Stride | ❌ | ❌ | ❌ | ❌ |

---

## 5. Questions & Answers

### Architecture Questions

- [x] **Q1:** Should `ControlBaseAsync` (Playwright) be removed per AD-009, or is async acceptable for web platforms?
  > **ANSWER:** Async is acceptable. AD-009 has been revised (v3.2) to allow async interfaces when the underlying driver is natively async (Playwright). See [DES-001 AD-009](../specs/DES-001-architectural-decisions.md#8-ad-009-synchronous-control-operations-with-async-exception).

- [x] **Q2:** How should Playwright's inherently async API be reconciled with FR-005.5 (sync operations)?
  > **ANSWER:** Playwright provides DUAL API - both `ControlBase` (sync wrappers) and `ControlBaseAsync` (native async). Users can choose based on preference. This is now documented in AD-009 v3.2.

- [x] **Q3:** Should Stride controls follow `Stride{ControlName}` or `{ControlName}` naming?
  > **ANSWER:** Follow the existing Stride implementation naming pattern (`Stride{ControlName}`), consistent with how other platforms use their own naming conventions.

- [x] **Q4:** What is the relationship between Brinell.Stride and Brinell.Stride.Automation?
  > **ANSWER:** `Brinell.Stride.Automation` is the **in-game automation component** that must be integrated into the Stride application itself. Stride does not support automation natively, so this component enables test communication via named pipes. `Brinell.Stride` is the test-side framework that communicates with the in-game component.

- [x] **Q5:** Does Stride need scroll support for scrollable UI panels?
  > **ANSWER:** Yes. Stride UI has scrollable panels that need scroll support.

### Implementation Questions

- [x] **Q6:** Should we enforce container support in Core interfaces or keep it optional?
  > **ANSWER:** **Enforce** container support in Core interfaces. All platforms must support container-scoped element searching.

- [x] **Q7:** Should `IContainerControl` extend `IItemsControl` or be separate?
  > **ANSWER:** **Separate**. A WPF `ContentControl` can be a container (for its child content) but is not an `IItemsControl`. Keep the interfaces separate.

- [x] **Q8:** How should timeout configuration be shared across platforms?
  > **ANSWER:** Only MAUI tests are truly platform-independent (Windows/Android/iOS). Other platforms are platform-specific, so timeout configuration is per-platform via each platform's `TestContext`.

### Process Questions

- [x] **Q9:** Should we create integration tests that run against all platforms?
  > **ANSWER:** Yes, if possible. Create integration tests to verify cross-platform behavior.

- [x] **Q10:** Should there be a "compliance test suite" that verifies spec adherence?
  > **ANSWER:** Yes. Create a compliance test suite that verifies each platform adheres to specifications.

---

## 6. Recommended Update Order

Based on dependencies and maturity:

1. **Brinell.Core** - Must be updated first (interfaces needed by platforms)
2. **Brinell.Maui** - Most complete, good reference for others
3. **Brinell.Wpf** - Production ready, needs BusyPageBase
4. **Brinell.WinForms** - Needs significant work
5. **Brinell.Html** - Parallel with Playwright
6. **Brinell.Html.Playwright** - Async question must be resolved
7. **Brinell.Stride** - Experimental, lowest priority

---

## 7. Next Steps

1. Create **PLAN-002-Core-Update.md** for Core updates
2. Create **PLAN-003-MAUI-Update.md** for MAUI updates
3. Create **PLAN-004-WPF-Update.md** for WPF updates
4. Create **PLAN-005-WinForms-Update.md** for WinForms updates
5. Create **PLAN-006-Html-Update.md** for Html (Selenium) updates
6. Create **PLAN-007-Playwright-Update.md** for Playwright updates
7. Create **PLAN-008-Stride-Update.md** for Stride updates

---

*Next: [PLAN-002: Core Update Plan](PLAN-002-Core-Update.md)*
