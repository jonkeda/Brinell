# ISSUE-002: Blazor Page Objects Don't Match Actual Pages

**Date:** January 5, 2026  
**Status:** In Progress  
**Severity:** High  
**Affected Tests:** 55+ Blazor UITests

---

## 1. Problem Description

The Blazor test page objects define controls and selectors that don't match the actual Blazor application pages. This causes tests to fail because they're looking for elements that don't exist or using incorrect selectors.

### Key Issues

1. **Selector Attribute Mismatch:**
   - Counter.razor uses `data-testid="..."` attributes
   - CounterPage.cs used `#id` CSS selectors (fixed: now uses `[data-testid='...']`)

2. **URL Route Mismatch:**
   - FormControls.razor defines route as `/form-controls`
   - Tests navigated to `/formcontrols` (fixed)

3. **Page Content Mismatch (Major):**
   - AdvancedPage.cs defines controls for Canvas, Clipboard, Local Storage, Geolocation sections
   - Advanced.razor has Event Log, Click Events, Mouse Events, Keyboard Events, Focus Events, Drag/Drop, Layout, Tooltip sections
   - Completely different feature set

---

## 2. Root Cause

The page objects were created based on a specification or mockup that differs from what was actually implemented in the Blazor application. The page objects need to be updated to reflect the actual implementation.

---

## 3. Affected Files

### Fixed:
| File | Issue | Status |
|------|-------|--------|
| CounterPage.cs | `#id` → `[data-testid='id']` | ✅ Fixed |
| FormControlsTests.cs | `/formcontrols` → `/form-controls` | ✅ Fixed |

### Needs Update:
| File | Issue | Status |
|------|-------|--------|
| AdvancedPage.cs | Controls don't match page content | 🔄 Needs Rewrite |
| AdvancedTests.cs | Tests reference non-existent controls | 🔄 Needs Rewrite |
| Other pages | Need verification | 🔍 Review needed |

---

## 4. Solution Plan

### 4.1 Update AdvancedPage.cs

Rewrite to match actual Advanced.razor content:

**New Sections:**
- Event Log: `EventLogSection`, `ClearLogButton`, `EventLogContent`
- Click Events: `SingleClickArea`, `DoubleClickArea`, `RightClickArea`, `ContextMenu`
- Mouse Events: `HoverArea`, `MouseTrackArea`, `MousePosition`
- Keyboard Events: `KeyboardInput`, `LastKeyValue`, `KeyCodeValue`, `ModifiersValue`
- Focus Events: `FocusInput1/2/3`, `FocusStatus`
- Drag and Drop: `DraggableContainer`, `Draggable_X`, `DropZone`
- Layout: `GridLayout`, `FlexLayout`
- Tooltip/Popover: `TooltipButton`, `PopoverButton`, `Popover`
- Reset: `ResetButton`

### 4.2 Update AdvancedTests.cs

Rewrite tests to use new controls:
- Test click events (single, double, right-click)
- Test mouse events (hover, position tracking)
- Test keyboard events
- Test focus events
- Test drag and drop
- Test popover toggle

---

## 5. Verification Checklist

- [ ] All page selectors match actual page attributes
- [ ] All page routes match actual page routes
- [ ] All controls in page objects exist in actual pages
- [ ] All tests pass after updates

---

## 6. Resolution Progress

| Date | Action | Result |
|------|--------|--------|
| 2026-01-05 | Issue documented | - |
| 2026-01-05 | Fixed CounterPage selectors | ✅ Changed to data-testid |
| 2026-01-05 | Fixed FormControlsTests URLs | ✅ Changed to /form-controls |
| | AdvancedPage rewrite | In progress |
