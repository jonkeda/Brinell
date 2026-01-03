# TEST-002: MAUI Sample App - Full Test Set

**Version:** 1.0  
**Status:** Design  
**Date:** January 2026

---

## Overview

Comprehensive test coverage for MAUI sample app including happy path, edge cases, and error scenarios.

---

## Dashboard Page Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| MAUI-FULL-001 | Page loads | All KPI cards render with data |
| MAUI-FULL-002 | Page refresh | Refresh updates KPI values |
| MAUI-FULL-003 | Status indicator | Status shows current state |
| MAUI-FULL-004 | Last updated timestamp | Timestamp displays correctly |
| MAUI-FULL-005 | Navigation to Form | Link navigates and Form loads |
| MAUI-FULL-006 | Navigation to Data | Link navigates and Data Grid loads |
| MAUI-FULL-007 | Navigation to Upload | Link navigates and Upload page loads |
| MAUI-FULL-008 | Offline state | Page displays offline message when no connection |

---

## User Form Page Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| MAUI-FULL-009 | Entry - text input | Accepts alphanumeric text |
| MAUI-FULL-010 | Entry - max length | Enforces maximum character limit |
| MAUI-FULL-011 | Entry - placeholder | Placeholder displays before input |
| MAUI-FULL-012 | Editor - multiline | Accepts multiline text |
| MAUI-FULL-013 | Editor - newlines | Preserves newlines in text |
| MAUI-FULL-014 | Picker - options | Displays all options |
| MAUI-FULL-015 | Picker - selection | Selection reflects selected value |
| MAUI-FULL-016 | Picker - unselected | Default unselected state |
| MAUI-FULL-017 | DatePicker - selection | Selects date correctly |
| MAUI-FULL-018 | DatePicker - min/max | Respects date range limits |
| MAUI-FULL-019 | DatePicker - format | Displays correct date format |
| MAUI-FULL-020 | TimePicker - selection | Selects time correctly |
| MAUI-FULL-021 | TimePicker - format | Displays correct time format |
| MAUI-FULL-022 | Switch - toggle on | Toggles to on state |
| MAUI-FULL-023 | Switch - toggle off | Toggles to off state |
| MAUI-FULL-024 | CheckBox - check | Checks successfully |
| MAUI-FULL-025 | CheckBox - uncheck | Unchecks successfully |
| MAUI-FULL-026 | RadioButton - select | Selects option |
| MAUI-FULL-027 | RadioButton - deselect | Previous option deselects |
| MAUI-FULL-028 | RadioButton - group behavior | Only one option selected at a time |
| MAUI-FULL-029 | Slider - drag | Slider position updates on drag |
| MAUI-FULL-030 | Slider - min/max | Respects minimum and maximum values |
| MAUI-FULL-031 | Stepper - increment | Increases value |
| MAUI-FULL-032 | Stepper - decrement | Decreases value |
| MAUI-FULL-033 | Form submit - valid | Submits with all valid data |
| MAUI-FULL-034 | Form submit - required field | Shows error for missing required field |
| MAUI-FULL-035 | Form submit - email format | Shows error for invalid email |
| MAUI-FULL-036 | Form submit - validation summary | Displays all validation errors |
| MAUI-FULL-037 | Form clear | Clear button resets all fields |
| MAUI-FULL-038 | Form cancel | Cancel button returns to previous page |

---

## Data Grid Page Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| MAUI-FULL-039 | Grid display | CollectionView renders all items |
| MAUI-FULL-040 | Grid scrolling | Scrolling loads additional items |
| MAUI-FULL-041 | Grid empty | Shows message when no data |
| MAUI-FULL-042 | Search - text | Filters items by text input |
| MAUI-FULL-043 | Search - empty | Clears search and shows all items |
| MAUI-FULL-044 | Search - partial match | Finds items with partial text match |
| MAUI-FULL-045 | Sort - ascending | Sorts items ascending |
| MAUI-FULL-046 | Sort - descending | Sorts items descending |
| MAUI-FULL-047 | Sort - by column | Sort column selection changes order |
| MAUI-FULL-048 | Pagination - first page | First page displays |
| MAUI-FULL-049 | Pagination - next | Next button loads next page |
| MAUI-FULL-050 | Pagination - previous | Previous button loads previous page |
| MAUI-FULL-051 | Pagination - last page | Last page button goes to final page |
| MAUI-FULL-052 | Row selection | Tapping row selects it |
| MAUI-FULL-053 | Row edit | Edit button opens edit form |
| MAUI-FULL-054 | Row delete | Delete button removes row with confirmation |
| MAUI-FULL-055 | Total count | Total record count displays correctly |
| MAUI-FULL-056 | Filter count | Filtered count updates correctly |

---

## File Upload Page Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| MAUI-FULL-057 | File picker | Tapping input opens file picker |
| MAUI-FULL-058 | File selection | Selected file displays filename |
| MAUI-FULL-059 | File type filter | Only allowed file types shown |
| MAUI-FULL-060 | File description | Description field accepts text |
| MAUI-FULL-061 | File category | Category dropdown selects type |
| MAUI-FULL-062 | Upload button | Upload initiates file transfer |
| MAUI-FULL-063 | Upload progress | Progress bar updates during upload |
| MAUI-FULL-064 | Upload complete | Success message displays |
| MAUI-FULL-065 | Upload failure | Error message displays on failure |
| MAUI-FULL-066 | File list | Uploaded file appears in list |
| MAUI-FULL-067 | File download | Download button downloads file |
| MAUI-FULL-068 | File delete | Delete button removes file |
| MAUI-FULL-069 | File preview | Preview button opens file |
| MAUI-FULL-070 | Large file | Handles large file upload |
| MAUI-FULL-071 | Multiple files | Uploads multiple files sequentially |

---

## Navigation Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| MAUI-FULL-072 | Menu open | Navigation menu opens |
| MAUI-FULL-073 | Menu close | Navigation menu closes |
| MAUI-FULL-074 | Menu item - dashboard | Navigates to Dashboard |
| MAUI-FULL-075 | Menu item - form | Navigates to Form |
| MAUI-FULL-076 | Menu item - grid | Navigates to Data Grid |
| MAUI-FULL-077 | Menu item - upload | Navigates to File Upload |
| MAUI-FULL-078 | Back button - Android | Platform back navigates correctly |
| MAUI-FULL-079 | Back button - iOS | Platform back navigates correctly |
| MAUI-FULL-080 | Deep linking | Direct navigation to page works |

---

## Gesture Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| MAUI-FULL-081 | Tap gesture | Tap triggers action |
| MAUI-FULL-082 | Long press | Long press shows context menu |
| MAUI-FULL-083 | Pan gesture | Pan movement captured |
| MAUI-FULL-084 | Pinch gesture | Pinch zoom works |
| MAUI-FULL-085 | Swipe gesture | Swipe reveals content |
| MAUI-FULL-086 | Double tap | Double tap triggers action |
| MAUI-FULL-087 | Pointer enter | Pointer enter triggers hover |
| MAUI-FULL-088 | Pointer exit | Pointer exit clears hover |
| MAUI-FULL-089 | Drop gesture | Drag and drop works |

---

## Performance Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| MAUI-FULL-090 | Page load time | Dashboard loads within 2 seconds |
| MAUI-FULL-091 | Form render | Form renders with 20+ controls in 1 second |
| MAUI-FULL-092 | Grid scroll performance | Grid scrolls smoothly with 100+ items |
| MAUI-FULL-093 | Memory usage | App doesn't leak memory on navigation |
| MAUI-FULL-094 | Battery usage | Idle app uses minimal battery |

---

## Accessibility Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| MAUI-FULL-095 | Screen reader | Controls have accessibility labels |
| MAUI-FULL-096 | Touch target size | Controls are 44x44px minimum |
| MAUI-FULL-097 | Color contrast | Text contrast meets WCAG AA standard |
| MAUI-FULL-098 | Keyboard navigation | All controls accessible via keyboard |
| MAUI-FULL-099 | Focus indicators | Focus state visible |

---

## Error Handling Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| MAUI-FULL-100 | Network timeout | Shows timeout error message |
| MAUI-FULL-101 | Server error | Shows server error message |
| MAUI-FULL-102 | Invalid data | Shows data validation error |
| MAUI-FULL-103 | App crash recovery | App recovers from crash |
| MAUI-FULL-104 | Permission denied | Shows permission error when needed |

---

## Summary

- **Total Tests:** 104
- **Coverage:** All functionality, edge cases, errors
- **Execution Time:** ~2-3 hours
- **Focus:** Comprehensive coverage

