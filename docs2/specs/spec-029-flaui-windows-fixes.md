# SPEC-029: FlaUI Windows Driver Fixes

**Status:** In Progress (15/22 tasks) | **Priority:** High | **Baseline:** 65.5% pass rate (152/232) | **Target:** 85%+

## Overview

Fixes four categories of FlaUI driver issues affecting Windows MAUI UI test automation. Uses a capability detection pattern where control classes check if the underlying element supports specific UI Automation patterns.

## Problem Categories

### 1. Slider/Stepper RangeValue Pattern

Slider manipulation via keyboard doesn't work reliably. MAUI Slider on Windows uses WinUI Slider which implements `IRangeValueProvider`.

**Fix:** Direct value manipulation via `Patterns.RangeValue.Pattern.SetValue()`.

### 2. Picker ComboBox Expansion

Picker item enumeration returns 0 items because items are only visible in the UI Automation tree after the ComboBox is expanded.

**Fix:** Expand ComboBox via `ExpandCollapsePattern`, enumerate `ListItem` descendants, then collapse.

### 3. SearchBar Text Retrieval

`GetText()` returns null after entering text because MAUI SearchBar uses WinUI `AutoSuggestBox` with a nested `TextBox`.

**Fix:** Find inner `TextBox` descendant (ControlType.Edit) and read text from it.

### 4. Editor Clear Operation

`Clear()` doesn't remove text. The Value pattern's `SetValue("")` may fail on some controls.

**Fix:** Focus element, try `SetValue("")` first, fallback to `Ctrl+A` + `Delete` keyboard approach.

## Architecture

### Capability Detection Pattern

```
Control Layer          Interface Layer              FlaUI Implementation
─────────────         ─────────────────            ─────────────────────
MauiSliderControl  ─→  IRangePatternElement    ←─  FlaUIMauiElement
MauiPickerControl  ─→  IExpandCollapseElement  ←─  FlaUIMauiElement
MauiSearchBar      ─→  INestedTextElement      ←─  FlaUIMauiElement
MauiEditorControl  ─→  INestedTextElement      ←─  FlaUIMauiElement
```

Controls check at runtime: `if (element is IRangePatternElement range && range.SupportsRangeValue)` — if supported, use the pattern; otherwise, fall back to existing approach.

### Extension Interfaces

Created in `srcnew/Brinell.Maui/Interfaces/`:

| Interface | File | Methods |
|-----------|------|---------|
| `IRangePatternElement` | `IRangePatternElement.cs` | `SupportsRangeValue`, `SetRangeValue()`, `GetRangeValue()`, `GetRangeMinimum()`, `GetRangeMaximum()`, `GetRangeSmallChange()` |
| `IExpandCollapsePatternElement` | `IExpandCollapsePatternElement.cs` | `SupportsExpandCollapse`, `IsExpanded`, `Expand()`, `Collapse()`, `GetExpandedItems()` |
| `INestedTextElement` | `INestedTextElement.cs` | `FindNestedTextBox()`, `GetNestedText()`, `ClearWithFallback()` |

### Files Changed

| File | Change | Status |
|------|--------|--------|
| `IRangePatternElement.cs` | New interface | ✅ |
| `IExpandCollapsePatternElement.cs` | New interface | ✅ |
| `INestedTextElement.cs` | New interface | ✅ |
| `FlaUIMauiElement.cs` | Implement 3 interfaces | ✅ |
| `MauiRangeControlBase.cs` | Use IRangePatternElement in Get/Set core methods | ✅ |
| `MauiSelectorControlBase.cs` | Use IExpandCollapsePatternElement for item enumeration | ✅ |
| `MauiSearchBarControl.cs` | Override GetTextCore with nested text | ✅ |
| `MauiEditorControl.cs` | Override ClearCore with fallback | ✅ |

## Task Status

| Phase | Description | Tasks | Status |
|-------|-------------|-------|--------|
| 1. Extension Interfaces | Create 3 interfaces | 3/3 | ✅ Complete |
| 2. FlaUIMauiElement | Implement interfaces | 4/4 | ✅ Complete |
| 3. Range Controls | Update MauiRangeControlBase | 3/3 | ✅ Complete |
| 4. Selector Controls | Update MauiSelectorControlBase | 3/3 | ✅ Complete |
| 5. Text Controls | Update SearchBar + Editor | 2/2 | ✅ Complete |
| 6. Testing & Validation | Build, run tests, document | 0/7 | 🔲 Pending |

**Overall:** 15/22 tasks complete. All code changes done. Testing/validation remaining.

## Success Metrics

| Metric | Before | Target |
|--------|--------|--------|
| Overall pass rate | 65.5% (152/232) | 85%+ (197/232) |
| Slider tests | 13/19 | 19/19 |
| Selection tests | 3/8 | 8/8 |
| Text tests | 8/14 | 14/14 |
| Regressions | N/A | 0 |

## Related

- SPEC-026: UI Test Fixes (broader fix effort including ScrollIntoView, toggle timing)
- SPEC-025: MAUI Control UI Tests (tests that will exercise these fixes)
