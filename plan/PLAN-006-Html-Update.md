# PLAN-006: Brinell.Html Update

**Created:** January 3, 2026
**Status:** ✅ Complete (33/33 tests passing)
**Platform:** Brinell.Html (Selenium)

---

## Overview

Update Brinell.Html to align with spec requirements and match patterns from MAUI/WPF/WinForms implementations.

## Current State Analysis

### Existing Base Classes
| Class | Status | Notes |
|-------|--------|-------|
| `ControlBase` | ✅ | Complete with Is/Wait/Check/Assert pattern |
| `PageBase` | ✅ | Complete |
| `LoadingPageBase` | ✅ | Similar to BusyPageBase |
| `ContentControlBase` | ✅ | Complete |
| `TextControlBase` | ✅ | Complete |
| `ToggleControlBase` | ✅ | To verify |
| `SelectorControlBase` | ✅ | To verify |
| `RangeControlBase` | ✅ | To verify |
| `ItemsControlBase` | ❌ | **Missing** |

### Existing Controls (9)
- ButtonControl
- LabelControl
- LinkControl
- CheckBoxControl
- SelectControl
- TextInputControl
- TextAreaControl
- RangeInputControl
- ProgressControl

### Infrastructure
- SeleniumDriverAdapter
- SeleniumElementAdapter
- SeleniumTestContext
- SeleniumScreenshotService

---

## Phase 1: Add Missing Base Classes

### 1.1 Rename LoadingPageBase to BusyPageBase
- Current `LoadingPageBase` already provides busy/loading functionality
- Add `BusyPageBase` as an alias or rename for consistency with other platforms

### 1.2 Add ItemsControlBase
Create base class for controls that contain multiple items (lists, tables, grids).

```csharp
public abstract class ItemsControlBase : ControlBase, IItemsControl
{
    int GetItemCount();
    IReadOnlyList<IWebElement> GetItems();
    IWebElement? GetItem(int index);
    IWebElement? GetItemByText(string text);
    void ClickItem(int index);
    void ClickItemByText(string text);
}
```

### 1.3 Add ScrollableControlBase
Create base class for scrollable containers.

```csharp
public abstract class ScrollableControlBase : ControlBase, IScrollableControl
{
    void ScrollToTop();
    void ScrollToBottom();
    void ScrollToElement(string automationId);
    void ScrollBy(int deltaX, int deltaY);
}
```

---

## Phase 2: Add Container Support

### 2.1 Update TextControlBase
Add container constructor for controls inside list items or repeated templates.

### 2.2 Update ContentControlBase
Add container constructor.

### 2.3 Update All Base Classes
Ensure all base classes support optional container parameter.

---

## Phase 3: Add New Controls

### 3.1 TableControl
For HTML table elements with row/column access.

### 3.2 ListControl
For ul/ol list elements.

### 3.3 ScrollContainerControl
For scrollable div/container elements.

---

## Phase 4: Update Sample Tests

### 4.1 Add ItemsControl Tests
Add tests that verify list/table control functionality.

### 4.2 Add Scroll Tests
Add tests that verify scroll functionality.

---

## Implementation Order

1. [x] Verify existing base classes are complete
2. [x] Add `BusyPageBase` alias/class
3. [x] Add `ItemsControlBase`
4. [x] Add `ScrollableControlBase`
5. [x] Update base classes with container constructors (already have them)
6. [x] Add `TableControl`
7. [x] Add `ListControl`
8. [x] Add `ScrollContainerControl`
9. [x] Update sample tests (added TableTests.cs)
10. [x] Run all tests and verify (33/33 passing)

---

## Success Criteria

- All existing tests pass
- ItemsControlBase provides item enumeration
- Scroll support works for web containers
- Container support allows scoped element finding
- Consistent with MAUI/WPF/WinForms patterns
