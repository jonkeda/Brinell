# Tasks Document: Interface Hierarchy Consolidation

## Task Format

- `[ ]` = Pending, `[-]` = In-progress, `[x]` = Completed
- Include File path, Purpose, _Leverage, _Requirements, and _Prompt fields

---

## Phase 1: Core Interface Enhancements

### [x] 1. Add Hover and LongPress to IClickableControlObject
- File: `srcnew/Brinell.Core/Interfaces/IClickableControlObject.cs`
- Purpose: Enable hover and long-press gestures for clickable controls
- _Leverage: Existing IClickableControlObject pattern_
- _Requirements: REQ-002_
- **Completed:** Added Hover(int? timeoutMs) and LongPress(int? durationMs, int? timeoutMs) methods

### [x] 1.1 Implement Hover and LongPress in MAUI controls
- Files: `srcnew/Brinell.Maui/Controls/MauiButtonControl.cs`, `MauiTabControl.cs`, `MauiFlyoutItemControl.cs`
- Purpose: Implement hover and long-press for MAUI clickable controls
- **Completed:** Added HoverCore/LongPressCore using Appium Actions API

### [x] 2. Add text assertion methods to ITextControlObject
- File: `srcnew/Brinell.Core/Interfaces/ITextControlObject.cs`
- Purpose: Add missing text assertion methods for test verification
- **Completed:** Added AssertTextStartsWith, AssertTextEndsWith, AssertTextEmpty methods (AssertTextContains already in IControlObject)

### [x] 2.1 Implement text assertions in MauiControlBase
- File: `srcnew/Brinell.Maui/Controls/MauiControlBase.cs`
- Purpose: Implement text assertion methods for all MAUI controls
- **Completed:** Added AssertTextStartsWith, AssertTextEndsWith, AssertTextEmpty using Poll pattern

### [x] 3. Add Append method to IEditableTextControlObject
- File: `srcnew/Brinell.Core/Interfaces/IEditableTextControlObject.cs`
- Purpose: Enable appending text without clearing existing content
- **Completed:** Added Append(string? text, int? timeoutMs) method

### [x] 3.1 Implement Append in MauiEntryControl
- File: `srcnew/Brinell.Maui/Controls/MauiEntryControl.cs`
- Purpose: Implement text append for MAUI entry controls
- **Completed:** Added Append/AppendCore using SendKeys without Clear

### [x] 4. Move ITabControlObject to standard location
- File: `srcnew/Brinell.Core/Interfaces/ITabControlObject.cs` (new)
- Delete: `srcnew/Brinell.Core/Abstractions/Controls/ITabControlObject.cs`
- Purpose: Standardize interface location in Interfaces folder
- **Completed:** Moved to Interfaces, updated namespace and MauiTabControl using

---

## Phase 2: New Specialized Interfaces

### [x] 5. Create IExpandableControlObject interface
- File: `srcnew/Brinell.Core/Interfaces/IExpandableControlObject.cs` (new)
- Purpose: Interface for expanders, accordions, tree nodes
- **Completed:** Created with IsExpanded, WaitExpanded, AssertExpanded, Expand, Collapse, ToggleExpanded

### [x] 6. Create IFocusableControlObject interface
- File: `srcnew/Brinell.Core/Interfaces/IFocusableControlObject.cs` (new)
- Purpose: Interface for focus management
- **Completed:** Created with IsFocused, WaitFocused, AssertFocused, Focus, Blur

### [x] 7. Create IProgressControlObject interface
- File: `srcnew/Brinell.Core/Interfaces/IProgressControlObject.cs` (new)
- Purpose: Interface for progress indicators and loading states
- **Completed:** Created with IsIndeterminate, GetProgress, WaitProgress, AssertProgress, WaitComplete, AssertComplete

---

## Phase 3: Date/Time Interfaces

### [x] 8. Create IDateControlObject interface
- File: `srcnew/Brinell.Core/Interfaces/IDateControlObject.cs` (new)
- Purpose: Interface for date picker controls
- **Completed:** Created with GetDate, SetDate, WaitDate, AssertDate using DateTime?

### [x] 9. Create ITimeControlObject interface
- File: `srcnew/Brinell.Core/Interfaces/ITimeControlObject.cs` (new)
- Purpose: Interface for time picker controls
- **Completed:** Created with GetTime, SetTime, WaitTime, AssertTime using TimeSpan?

---

## Phase 4: Mobile-Specific Interfaces

### [x] 10. Create ISwipeableControlObject interface
- File: `srcnew/Brinell.Core/Interfaces/ISwipeableControlObject.cs` (new)
- Purpose: Interface for swipe gestures (primarily mobile)
- **Completed:** Created with SwipeLeft, SwipeRight, SwipeUp, SwipeDown, Swipe(startX, startY, endX, endY)

### [x] 11. Create IRefreshableControlObject interface
- File: `srcnew/Brinell.Core/Interfaces/IRefreshableControlObject.cs` (new)
- Purpose: Interface for pull-to-refresh controls (primarily mobile)
- **Completed:** Created with IsRefreshing, WaitRefreshing, AssertRefreshing, PullToRefresh

---

## Phase 5: Testing and Verification

### [x] 12. Build solution and verify no compilation errors
- Command: `dotnet build srcnew/Brinell.Maui/Brinell.Maui.csproj`
- Purpose: Verify all interfaces compile correctly
- **Completed:** Solution builds successfully with no errors

### [x] 13. Update GlobalUsings.cs if needed
- File: `srcnew/Brinell.Core/GlobalUsings.cs`
- Purpose: Ensure new interfaces are accessible via global usings
- **Completed:** Already includes `global using Brinell.Core.Interfaces;` - no changes needed

### [ ] 14. Run existing tests to verify no regressions
- Command: `dotnet test testsnew/Brinell.sln --filter "Category!=UITest"`
- Purpose: Ensure existing functionality still works
- _Leverage: Existing test infrastructure_
- _Requirements: All_

### [ ] 15. Create interface unit tests
- File: `testsnew/Brinell.Core.Tests/Interfaces/InterfaceContractTests.cs` (new)
- Purpose: Verify interface contracts are correct
- _Leverage: Existing test patterns in Brinell.Core.Tests_
- _Requirements: All_

---

## Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| Phase 1: Core Enhancements | 1, 1.1, 2, 2.1, 3, 3.1, 4 | ✅ Complete |
| Phase 2: Specialized Interfaces | 5, 6, 7 | ✅ Complete |
| Phase 3: Date/Time Interfaces | 8, 9 | ✅ Complete |
| Phase 4: Mobile Interfaces | 10, 11 | ✅ Complete |
| Phase 5: Testing | 12, 13, 14, 15 | 🔄 In Progress (2/4) |

**Total Tasks:** 15 main tasks + 3 sub-tasks = 18 tasks  
**Completed:** 16/18

---

**Document Version:** 1.1  
**Created:** January 19, 2026  
**Updated:** January 19, 2026  
**Spec ID:** 003  
**Status:** Implementation Complete  
**Workflow:** spec_workflow/tasks
