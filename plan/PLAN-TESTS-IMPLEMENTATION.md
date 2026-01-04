# ControlObject6 Comprehensive Test Implementation Plan

**Version:** 1.0  
**Created:** January 4, 2026  
**Status:** Draft  
**Based On:** [PLAN-POC-TESTS-IMPLEMENTATION](../tests/PLAN-POC-TESTS-IMPLEMENTATION.md)

---

## Overview

This plan describes the comprehensive unit test implementation for all ControlObject6 controls across MAUI (56 controls) and Blazor (22 controls). Tests extend the POC foundation with full coverage of all control types.

### Scope

| Platform | Control Classes | Base Classes | Test Files |
|----------|-----------------|--------------|------------|
| MAUI | 35 concrete | 21 base | 56+ |
| Blazor | 19 concrete | 3 base | 22+ |
| Core | - | Interfaces | Existing (93 tests) |

---

## Phase 1: MAUI Control Tests

### 1.1 Clickable Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `ButtonControl` | ButtonControlTests.cs | BC-001 to BC-015 | P0 |
| `LabelControl` | LabelControlTests.cs | LC-001 to LC-012 | P0 |
| `ImageControl` | ImageControlTests.cs | IC-001 to IC-018 | P1 |

**Test Categories:**
- Click action execution
- Text retrieval and assertions
- Visibility and enabled state
- Wait operations
- Double-click and long-press

---

### 1.2 Text Input Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `EntryControl` | EntryControlTests.cs | EC-001 to EC-025 | P0 |
| `EditorControl` | EditorControlTests.cs | EDC-001 to EDC-020 | P0 |

**Test Categories:**
- Enter text
- Clear text
- ClearAndEnter
- Append text
- GetText
- Text assertions (Contains, StartsWith, EndsWith, Matches, Empty)
- Placeholder text
- Max length validation

---

### 1.3 Toggle Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `CheckBoxControl` | CheckBoxControlTests.cs | CB-001 to CB-018 | P0 |
| `SwitchControl` | SwitchControlTests.cs | SC-001 to SC-018 | P0 |
| `RadioButtonControl` | RadioButtonControlTests.cs | RB-001 to RB-015 | P1 |

**Test Categories:**
- Check/Uncheck (Toggle)
- SetChecked(bool)
- IsChecked
- Toggle state assertions
- Indeterminate state (CheckBox only)
- Group behavior (RadioButton only)

---

### 1.4 Selection Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `PickerControl` | PickerControlTests.cs | PC-001 to PC-025 | P0 |

**Test Categories:**
- SelectByIndex
- SelectByText
- GetSelectedIndex
- GetSelectedText
- GetItems
- GetItemCount
- Item assertions
- Scroll to item

---

### 1.5 Range Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `SliderControl` | SliderControlTests.cs | SLC-001 to SLC-020 | P1 |
| `StepperControl` | StepperControlTests.cs | STC-001 to STC-018 | P1 |

**Test Categories:**
- SetValue
- GetValue
- GetMinimum/GetMaximum
- Increment/Decrement (Stepper)
- Value assertions
- Range boundary validation

---

### 1.6 DateTime Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `DatePickerControl` | DatePickerControlTests.cs | DP-001 to DP-22 | P1 |
| `TimePickerControl` | TimePickerControlTests.cs | TP-001 to TP-22 | P1 |

**Test Categories:**
- SetDate/SetTime
- GetDate/GetTime
- GetMinimum/GetMaximum
- Date/Time assertions
- Format handling
- Boundary validation

---

### 1.7 Collection Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `ListViewControl` | ListViewControlTests.cs | LV-001 to LV-30 | P0 |
| `CollectionViewControl` | CollectionViewControlTests.cs | CV-001 to CV-30 | P0 |

**Test Categories:**
- GetItemCount
- GetItems
- GetItemAt(index)
- SelectItemAt(index)
- SelectItemByText
- GetSelectedIndex/GetSelectedItem
- ScrollToItem
- Pull-to-refresh (if applicable)
- Group headers (if applicable)

---

### 1.8 Container Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `ScrollViewControl` | ScrollViewControlTests.cs | SV-001 to SV-20 | P1 |
| `ExpanderControl` | ExpanderControlTests.cs | EX-001 to EX-15 | P1 |
| `RefreshViewControl` | RefreshViewControlTests.cs | RV-001 to RV-12 | P2 |
| `SwipeViewControl` | SwipeViewControlTests.cs | SW-001 to SW-15 | P2 |
| `FrameControl` | FrameControlTests.cs | FR-001 to FR-08 | P2 |
| `BorderControl` | BorderControlTests.cs | BO-001 to BO-08 | P2 |

**Test Categories:**
- Scroll operations (ScrollTo, ScrollToEnd)
- Scroll position
- Expand/Collapse
- IsExpanded
- Refresh trigger
- IsRefreshing
- Swipe actions

---

### 1.9 Progress & Activity Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `ProgressBarControl` | ProgressBarControlTests.cs | PB-001 to PB-15 | P1 |
| `ActivityIndicatorControl` | ActivityIndicatorControlTests.cs | AI-001 to AI-10 | P2 |

**Test Categories:**
- GetProgress
- SetProgress (if writable)
- IsRunning (ActivityIndicator)
- Progress assertions

---

### 1.10 Navigation Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `NavigationPageControl` | NavigationPageControlTests.cs | NP-001 to NP-18 | P1 |
| `TabbedPageControl` | TabbedPageControlTests.cs | TB-001 to TB-20 | P1 |
| `TabBarControl` | TabBarControlTests.cs | TBR-001 to TBR-15 | P1 |
| `FlyoutPageControl` | FlyoutPageControlTests.cs | FP-001 to FP-15 | P2 |
| `ShellControl` | ShellControlTests.cs | SH-001 to SH-20 | P2 |
| `ToolbarControl` | ToolbarControlTests.cs | TL-001 to TL-12 | P2 |

**Test Categories:**
- Tab selection
- GetSelectedTab
- GetTabCount
- Navigation push/pop
- Flyout open/close
- Toolbar item actions

---

### 1.11 Media Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `MediaElementControl` | MediaElementControlTests.cs | ME-001 to ME-25 | P2 |
| `WebViewControl` | WebViewControlTests.cs | WV-001 to WV-20 | P2 |

**Test Categories:**
- Play/Pause/Stop
- Seek
- GetCurrentPosition
- GetDuration
- Volume control
- IsPlaying
- WebView navigation
- WebView content assertions

---

## Phase 2: Blazor Control Tests

### 2.1 Clickable Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `ButtonControl` | ButtonControlTests.cs | BBC-001 to BBC-015 | P0 |
| `LinkControl` | LinkControlTests.cs | BLC-001 to BLC-12 | P1 |

**Test Categories:**
- ClickAsync
- GetTextAsync
- Visibility assertions
- Enabled state assertions
- Href attribute (Link)

---

### 2.2 Text Input Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `InputControl` | InputControlTests.cs | BIC-001 to BIC-25 | P0 |
| `TextAreaControl` | TextAreaControlTests.cs | BTA-001 to BTA-20 | P0 |

**Test Categories:**
- EnterAsync
- ClearAsync
- ClearAndEnterAsync
- AppendAsync
- GetTextAsync
- Async text assertions
- Placeholder text

---

### 2.3 Toggle Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `CheckBoxControl` | CheckBoxControlTests.cs | BCB-001 to BCB-18 | P0 |
| `RadioButtonControl` | RadioButtonControlTests.cs | BRB-001 to BRB-15 | P1 |

**Test Categories:**
- CheckAsync/UncheckAsync
- SetCheckedAsync(bool)
- IsCheckedAsync
- Toggle state assertions

---

### 2.4 Selection Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `SelectControl` | SelectControlTests.cs | BSC-001 to BSC-25 | P0 |

**Test Categories:**
- SelectByIndexAsync
- SelectByTextAsync
- SelectByValueAsync
- GetSelectedIndexAsync
- GetSelectedTextAsync
- GetOptionsAsync
- GetOptionCountAsync

---

### 2.5 Range Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `RangeControl` | RangeControlTests.cs | BRC-001 to BRC-20 | P1 |

**Test Categories:**
- SetValueAsync
- GetValueAsync
- GetMinimumAsync/GetMaximumAsync
- Value assertions

---

### 2.6 DateTime Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `DateInputControl` | DateInputControlTests.cs | BDI-001 to BDI-20 | P1 |
| `TimeInputControl` | TimeInputControlTests.cs | BTI-001 to BTI-20 | P1 |

**Test Categories:**
- SetDateAsync/SetTimeAsync
- GetDateAsync/GetTimeAsync
- Date/Time assertions
- HTML5 input type handling

---

### 2.7 Collection Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `ListControl` | ListControlTests.cs | BLI-001 to BLI-25 | P0 |
| `TableControl` | TableControlTests.cs | BTB-001 to BTB-30 | P0 |

**Test Categories:**
- GetItemCountAsync
- GetItemsAsync
- GetItemAtAsync(index)
- SelectItemAsync
- Table: GetRowCountAsync, GetColumnCountAsync
- Table: GetCellAsync(row, col)
- Table: GetHeadersAsync

---

### 2.8 Navigation Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `NavMenuControl` | NavMenuControlTests.cs | BNM-001 to BNM-18 | P1 |
| `TabControl` | TabControlTests.cs | BTC-001 to BTC-20 | P1 |

**Test Categories:**
- SelectTabAsync
- GetSelectedTabAsync
- GetTabCountAsync
- Menu item selection
- Active state detection

---

### 2.9 Media Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `VideoControl` | VideoControlTests.cs | BVC-001 to BVC-20 | P2 |
| `AudioControl` | AudioControlTests.cs | BAC-001 to BAC-18 | P2 |
| `IFrameControl` | IFrameControlTests.cs | BIF-001 to BIF-15 | P2 |

**Test Categories:**
- PlayAsync/PauseAsync
- SeekAsync
- GetCurrentPositionAsync
- Volume control
- IFrame: SwitchToFrameAsync, GetSrcAsync

---

### 2.10 Display Controls

| Control | Test File | Test Cases | Priority |
|---------|-----------|------------|----------|
| `ImageControl` | ImageControlTests.cs | BIM-001 to BIM-15 | P1 |
| `ProgressControl` | ProgressControlTests.cs | BPR-001 to BPR-12 | P1 |

**Test Categories:**
- GetSrcAsync
- GetAltTextAsync
- GetProgressAsync
- Progress assertions

---

## Phase 3: Mock Infrastructure Enhancements

### 3.1 MAUI Mocks

Extend existing wrapper pattern for new control types:

| Mock Class | Purpose |
|------------|---------|
| `MockAppiumElementWrapper.cs` | Enhanced mock for all control types |
| `MockAppiumDriverWrapper.cs` | Enhanced driver mock |
| `MockElementFactory.cs` | Factory for creating pre-configured mocks |

**New Mock Behaviors:**
- Collection element children
- Scrollable content
- Toggle states
- Range values
- DateTime values

---

### 3.2 Blazor Mocks

| Mock Class | Purpose |
|------------|---------|
| `MockLocatorFactory.cs` | Factory for creating ILocator mocks |
| `MockPageFactory.cs` | Factory for creating IPage mocks |
| `MockElementHandleFactory.cs` | Factory for IElementHandle mocks |

**New Mock Behaviors:**
- Async operation simulation
- Form input states
- Selection dropdown options
- Table structure

---

## Phase 4: Test Categories & Traits

### 4.1 Category Traits

```csharp
[Trait("Category", "Clickable")]
[Trait("Category", "TextInput")]
[Trait("Category", "Toggle")]
[Trait("Category", "Selection")]
[Trait("Category", "Range")]
[Trait("Category", "DateTime")]
[Trait("Category", "Collection")]
[Trait("Category", "Container")]
[Trait("Category", "Navigation")]
[Trait("Category", "Media")]
[Trait("Category", "Display")]
```

### 4.2 Priority Traits

```csharp
[Trait("Priority", "P0")]  // Critical - must pass
[Trait("Priority", "P1")]  // Important - should pass
[Trait("Priority", "P2")]  // Nice to have
```

### 4.3 Platform Traits

```csharp
[Trait("Platform", "MAUI")]
[Trait("Platform", "Blazor")]
[Trait("Platform", "Core")]
```

---

## Phase 5: Test Execution Plan

### 5.1 Run by Category

```powershell
# Run all P0 tests
dotnet test --filter "Priority=P0"

# Run all MAUI tests
dotnet test --filter "Platform=MAUI"

# Run all Toggle control tests
dotnet test --filter "Category=Toggle"

# Run P0 MAUI Toggle tests
dotnet test --filter "Priority=P0&Platform=MAUI&Category=Toggle"
```

### 5.2 Run by Project

```powershell
# Core tests (existing)
dotnet test tests/Brinell.Core.Tests.ControlObject6/

# MAUI control tests
dotnet test tests/Brinell.Maui.Tests.ControlObject6/

# Blazor control tests
dotnet test tests/Brinell.Blazor.Tests.ControlObject6/
```

### 5.3 Coverage Targets

| Metric | Target | Minimum |
|--------|--------|---------|
| Line Coverage | 90% | 80% |
| Branch Coverage | 85% | 75% |
| Method Coverage | 95% | 90% |

---

## Test Count Summary

### MAUI Tests

| Category | Controls | Tests per Control | Total Tests |
|----------|----------|-------------------|-------------|
| Clickable | 3 | ~15 | 45 |
| TextInput | 2 | ~22 | 44 |
| Toggle | 3 | ~17 | 51 |
| Selection | 1 | ~25 | 25 |
| Range | 2 | ~19 | 38 |
| DateTime | 2 | ~22 | 44 |
| Collection | 2 | ~30 | 60 |
| Container | 6 | ~13 | 78 |
| Progress/Activity | 2 | ~12 | 24 |
| Navigation | 6 | ~17 | 102 |
| Media | 2 | ~22 | 44 |
| **MAUI Total** | **31** | - | **~555** |

### Blazor Tests

| Category | Controls | Tests per Control | Total Tests |
|----------|----------|-------------------|-------------|
| Clickable | 2 | ~13 | 26 |
| TextInput | 2 | ~22 | 44 |
| Toggle | 2 | ~16 | 32 |
| Selection | 1 | ~25 | 25 |
| Range | 1 | ~20 | 20 |
| DateTime | 2 | ~20 | 40 |
| Collection | 2 | ~27 | 54 |
| Navigation | 2 | ~19 | 38 |
| Media | 3 | ~18 | 54 |
| Display | 2 | ~13 | 26 |
| **Blazor Total** | **19** | - | **~359** |

### Grand Total

| Project | Existing | New | Total |
|---------|----------|-----|-------|
| Core | 93 | 0 | 93 |
| MAUI | 36 | ~519 | ~555 |
| Blazor | 37 | ~322 | ~359 |
| **Total** | **166** | **~841** | **~1,007** |

---

## Implementation Order

### Phase 1: P0 Controls (Week 1)

| Day | MAUI | Blazor |
|-----|------|--------|
| 1 | ButtonControl, LabelControl | ButtonControl |
| 2 | EntryControl, EditorControl | InputControl, TextAreaControl |
| 3 | CheckBoxControl, SwitchControl | CheckBoxControl |
| 4 | PickerControl | SelectControl |
| 5 | ListViewControl, CollectionViewControl | ListControl, TableControl |

### Phase 2: P1 Controls (Week 2)

| Day | MAUI | Blazor |
|-----|------|--------|
| 1 | RadioButtonControl, ImageControl | RadioButtonControl, LinkControl |
| 2 | SliderControl, StepperControl | RangeControl |
| 3 | DatePickerControl, TimePickerControl | DateInputControl, TimeInputControl |
| 4 | NavigationPageControl, TabbedPageControl | NavMenuControl, TabControl |
| 5 | ScrollViewControl, ExpanderControl | ImageControl, ProgressControl |

### Phase 3: P2 Controls (Week 3)

| Day | MAUI | Blazor |
|-----|------|--------|
| 1 | ProgressBarControl, ActivityIndicatorControl | VideoControl |
| 2 | FlyoutPageControl, ShellControl | AudioControl |
| 3 | ToolbarControl, TabBarControl | IFrameControl |
| 4 | RefreshViewControl, SwipeViewControl | - |
| 5 | MediaElementControl, WebViewControl | - |
| 6 | FrameControl, BorderControl | - |

---

## File Structure

```
tests/
├── PLAN-POC-TESTS-IMPLEMENTATION.md (existing)
├── Brinell.Core.Tests.ControlObject6/ (existing - 93 tests)
│   └── ...
├── Brinell.Maui.Tests.ControlObject6/
│   ├── Brinell.Maui.Tests.ControlObject6.csproj
│   ├── Mocks/
│   │   ├── MockAppiumDriverWrapper.cs
│   │   ├── MockAppiumElementWrapper.cs
│   │   └── MockElementFactory.cs
│   ├── Fixtures/
│   │   └── MauiTestFixture.cs
│   ├── Context/
│   │   └── MauiTestContextTests.cs (existing)
│   ├── Controls/
│   │   ├── Clickable/
│   │   │   ├── ButtonControlTests.cs
│   │   │   ├── LabelControlTests.cs
│   │   │   └── ImageControlTests.cs
│   │   ├── TextInput/
│   │   │   ├── EntryControlTests.cs
│   │   │   └── EditorControlTests.cs
│   │   ├── Toggle/
│   │   │   ├── CheckBoxControlTests.cs
│   │   │   ├── SwitchControlTests.cs
│   │   │   └── RadioButtonControlTests.cs
│   │   ├── Selection/
│   │   │   └── PickerControlTests.cs
│   │   ├── Range/
│   │   │   ├── SliderControlTests.cs
│   │   │   └── StepperControlTests.cs
│   │   ├── DateTime/
│   │   │   ├── DatePickerControlTests.cs
│   │   │   └── TimePickerControlTests.cs
│   │   ├── Collection/
│   │   │   ├── ListViewControlTests.cs
│   │   │   └── CollectionViewControlTests.cs
│   │   ├── Container/
│   │   │   ├── ScrollViewControlTests.cs
│   │   │   ├── ExpanderControlTests.cs
│   │   │   ├── RefreshViewControlTests.cs
│   │   │   ├── SwipeViewControlTests.cs
│   │   │   ├── FrameControlTests.cs
│   │   │   └── BorderControlTests.cs
│   │   ├── Progress/
│   │   │   ├── ProgressBarControlTests.cs
│   │   │   └── ActivityIndicatorControlTests.cs
│   │   ├── Navigation/
│   │   │   ├── NavigationPageControlTests.cs
│   │   │   ├── TabbedPageControlTests.cs
│   │   │   ├── TabBarControlTests.cs
│   │   │   ├── FlyoutPageControlTests.cs
│   │   │   ├── ShellControlTests.cs
│   │   │   └── ToolbarControlTests.cs
│   │   └── Media/
│   │       ├── MediaElementControlTests.cs
│   │       └── WebViewControlTests.cs
│   └── Pages/
│       └── PageObjectBaseTests.cs (existing)
└── Brinell.Blazor.Tests.ControlObject6/
    ├── Brinell.Blazor.Tests.ControlObject6.csproj
    ├── Mocks/
    │   ├── MockLocatorFactory.cs
    │   ├── MockPageFactory.cs
    │   └── MockElementHandleFactory.cs
    ├── Fixtures/
    │   └── BlazorTestFixture.cs
    ├── Context/
    │   └── BlazorTestContextTests.cs (existing)
    ├── Controls/
    │   ├── Clickable/
    │   │   ├── ButtonControlTests.cs
    │   │   └── LinkControlTests.cs
    │   ├── TextInput/
    │   │   ├── InputControlTests.cs
    │   │   └── TextAreaControlTests.cs
    │   ├── Toggle/
    │   │   ├── CheckBoxControlTests.cs
    │   │   └── RadioButtonControlTests.cs
    │   ├── Selection/
    │   │   └── SelectControlTests.cs
    │   ├── Range/
    │   │   └── RangeControlTests.cs
    │   ├── DateTime/
    │   │   ├── DateInputControlTests.cs
    │   │   └── TimeInputControlTests.cs
    │   ├── Collection/
    │   │   ├── ListControlTests.cs
    │   │   └── TableControlTests.cs
    │   ├── Navigation/
    │   │   ├── NavMenuControlTests.cs
    │   │   └── TabControlTests.cs
    │   ├── Media/
    │   │   ├── VideoControlTests.cs
    │   │   ├── AudioControlTests.cs
    │   │   └── IFrameControlTests.cs
    │   └── Display/
    │       ├── ImageControlTests.cs
    │       └── ProgressControlTests.cs
    └── Pages/
        └── AsyncPageObjectBaseTests.cs (existing)
```

---

## Success Criteria

- [ ] All P0 tests implemented and passing
- [ ] All P1 tests implemented and passing
- [ ] All P2 tests implemented and passing
- [ ] Line coverage ≥ 80% (target 90%)
- [ ] Branch coverage ≥ 75% (target 85%)
- [ ] Method coverage ≥ 90% (target 95%)
- [ ] No test flakiness
- [ ] CI/CD integration complete
- [ ] Test documentation complete

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| xunit | 2.9.3 | Test framework |
| xunit.runner.visualstudio | 3.1.5 | Test runner |
| Microsoft.NET.Test.Sdk | 17.14.0 | Test SDK |
| FluentAssertions | 6.12.0 | Assertion library |
| Moq | 4.20.70 | Mocking framework |
| coverlet.collector | 6.0.4 | Code coverage |

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| AppiumDriver non-mockable | Use IAppiumDriverWrapper pattern (POC proven) |
| Async test complexity | Follow Blazor POC async patterns |
| Large test suite maintenance | Organize by category, use shared fixtures |
| Platform-specific behavior | Document in test comments, skip if not applicable |
| CI execution time | Parallelize by category, use test filters |

---

## References

| Document | Purpose |
|----------|---------|
| [PLAN-POC-TESTS-IMPLEMENTATION](../tests/PLAN-POC-TESTS-IMPLEMENTATION.md) | POC test plan (completed) |
| [SPEC-006-004-TESTING-GUIDE](../specs/SPEC-006-004-TESTING-GUIDE.md) | Testing patterns & mockability |
| [SPEC-006-001-INTERFACES](../specs/SPEC-006-001-INTERFACES.md) | Interface definitions |
| [PLAN-SPEC-006-IMPLEMENTATION](./PLAN-SPEC-006-IMPLEMENTATION.md) | Control implementation plan |

---

**End of Plan**
