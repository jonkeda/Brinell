# TEST-004: Blazor Sample App - Full Test Set

**Version:** 1.0  
**Status:** Design  
**Date:** January 2026

---

## Overview

Comprehensive test coverage for Blazor sample app including happy path, edge cases, and error scenarios.

---

## Dashboard Page Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| BLAZOR-FULL-001 | Page loads | All KPI cards render |
| BLAZOR-FULL-002 | Page title | PageTitle sets document title |
| BLAZOR-FULL-003 | Dynamic component | StatusIndicator loads correctly |
| BLAZOR-FULL-004 | Last updated | Timestamp displays current time |
| BLAZOR-FULL-005 | NavLink accuracy | Links navigate to correct pages |
| BLAZOR-FULL-006 | Quick links | All navigation buttons functional |
| BLAZOR-FULL-007 | Offline behavior | Page handles no connection |
| BLAZOR-FULL-008 | Page refresh | Component preserves state |

---

## User Form Page Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| BLAZOR-FULL-009 | InputText - text | Accepts text input |
| BLAZOR-FULL-010 | InputText - binding | Two-way binding works |
| BLAZOR-FULL-011 | InputTextArea - multiline | Accepts multiline text |
| BLAZOR-FULL-012 | InputNumber - integer | Accepts integer input |
| BLAZOR-FULL-013 | InputNumber - decimal | Accepts decimal input |
| BLAZOR-FULL-014 | InputNumber - boundaries | Respects min/max values |
| BLAZOR-FULL-015 | InputSelect - options | Displays all options |
| BLAZOR-FULL-016 | InputSelect - selection | Selection updates model |
| BLAZOR-FULL-017 | InputSelect - empty option | Default empty option available |
| BLAZOR-FULL-018 | InputDate - selection | Selects date correctly |
| BLAZOR-FULL-019 | InputDate - format | Displays correct format |
| BLAZOR-FULL-020 | InputDate - range | Respects min/max dates |
| BLAZOR-FULL-021 | InputDateRange - selection | Selects date range |
| BLAZOR-FULL-022 | InputCheckbox - check | Checks successfully |
| BLAZOR-FULL-023 | InputCheckbox - uncheck | Unchecks successfully |
| BLAZOR-FULL-024 | InputRadio - select | Selects option |
| BLAZOR-FULL-025 | InputRadioGroup - mutual exclusive | Only one option selected |
| BLAZOR-FULL-026 | InputRadioGroup - value | Selection reflects value |
| BLAZOR-FULL-027 | InputFile - file picker | Opens file selection |
| BLAZOR-FULL-028 | InputFile - file select | Selected file displays |
| BLAZOR-FULL-029 | InputFile - multiple | Allows multiple file selection |
| BLAZOR-FULL-030 | EditForm - valid submit | Submits valid form |
| BLAZOR-FULL-031 | EditForm - invalid submit | Prevents invalid form submission |
| BLAZOR-FULL-032 | DataAnnotationsValidator | Validates using data annotations |
| BLAZOR-FULL-033 | ValidationSummary - display | Shows all validation errors |
| BLAZOR-FULL-034 | ValidationMessage - field | Shows field-specific errors |
| BLAZOR-FULL-035 | CustomValidation - trigger | Custom validation runs |
| BLAZOR-FULL-036 | CustomValidation - error | Custom error displays |
| BLAZOR-FULL-037 | Form reset | Form fields clear on reset |
| BLAZOR-FULL-038 | Form cancel | Cancel button returns to previous page |

---

## Data Table Page Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| BLAZOR-FULL-039 | Virtualize - render | Virtualize renders items |
| BLAZOR-FULL-040 | Virtualize - scroll | Scrolling shows virtual items |
| BLAZOR-FULL-041 | Virtualize - empty | Shows message when no data |
| BLAZOR-FULL-042 | Search - text input | InputText filters items |
| BLAZOR-FULL-043 | Search - case insensitive | Search ignores case |
| BLAZOR-FULL-044 | Search - partial match | Finds partial matches |
| BLAZOR-FULL-045 | Sort - ascending | Sorts ascending |
| BLAZOR-FULL-046 | Sort - descending | Sorts descending |
| BLAZOR-FULL-047 | Sort - column select | Dropdown changes sort column |
| BLAZOR-FULL-048 | Pagination - first | First page displays |
| BLAZOR-FULL-049 | Pagination - next | Next button loads next page |
| BLAZOR-FULL-050 | Pagination - previous | Previous button loads previous page |
| BLAZOR-FULL-051 | Pagination - last | Last button goes to final page |
| BLAZOR-FULL-052 | Table row - click | Row click selectable |
| BLAZOR-FULL-053 | Table row - edit | Edit button opens edit form |
| BLAZOR-FULL-054 | Table row - delete | Delete button removes row |
| BLAZOR-FULL-055 | Record count - total | Total count displays |
| BLAZOR-FULL-056 | Record count - filtered | Filtered count updates |

---

## File Upload Page Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| BLAZOR-FULL-057 | InputFile - open picker | File picker opens |
| BLAZOR-FULL-058 | InputFile - select file | File displays name |
| BLAZOR-FULL-059 | InputFile - multiple files | Multiple files selectable |
| BLAZOR-FULL-060 | File description | InputTextArea accepts text |
| BLAZOR-FULL-061 | File category | InputSelect shows categories |
| BLAZOR-FULL-062 | Upload button | Upload initiates transfer |
| BLAZOR-FULL-063 | Progress display | Progress bar updates |
| BLAZOR-FULL-064 | Success message | Displays upload success |
| BLAZOR-FULL-065 | Error message | Displays upload error |
| BLAZOR-FULL-066 | File list - display | Uploaded file shows in list |
| BLAZOR-FULL-067 | File list - download | Download button works |
| BLAZOR-FULL-068 | File list - delete | Delete button removes file |
| BLAZOR-FULL-069 | File list - preview | Preview button opens file |
| BLAZOR-FULL-070 | Large file | Handles large files |
| BLAZOR-FULL-071 | Multiple files | Uploads multiple files |

---

## Navigation Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| BLAZOR-FULL-072 | NavMenu render | Menu displays all items |
| BLAZOR-FULL-073 | NavLink - dashboard | Dashboard link navigates |
| BLAZOR-FULL-074 | NavLink - form | Form link navigates |
| BLAZOR-FULL-075 | NavLink - table | Table link navigates |
| BLAZOR-FULL-076 | NavLink - upload | Upload link navigates |
| BLAZOR-FULL-077 | NavLink - active | Active link highlighted |
| BLAZOR-FULL-078 | Router - routing | Router correctly routes |
| BLAZOR-FULL-079 | Router - not found | 404 page shown for invalid route |
| BLAZOR-FULL-080 | Focus on navigate | FocusOnNavigate restores focus |

---

## Dynamic Content Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| BLAZOR-FULL-081 | DynamicComponent - load | Component loads correctly |
| BLAZOR-FULL-082 | DynamicComponent - parameters | Parameters pass to component |
| BLAZOR-FULL-083 | DynamicComponent - type change | Changing type reloads component |
| BLAZOR-FULL-084 | CascadingValue - pass | Values cascade to children |
| BLAZOR-FULL-085 | CascadingValue - update | Value updates propagate |

---

## Advanced Features Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| BLAZOR-FULL-086 | ErrorBoundary - catch | ErrorBoundary catches error |
| BLAZOR-FULL-087 | ErrorBoundary - display | Error displays in ErrorContent |
| BLAZOR-FULL-088 | FocusOnNavigate - restore | Focus restores on navigate |
| BLAZOR-FULL-089 | HeadContent - set | HeadContent sets meta tags |
| BLAZOR-FULL-090 | Virtualize - overscan | Overscan loads extra items |
| BLAZOR-FULL-091 | PageTitle - update | PageTitle updates document title |
| BLAZOR-FULL-092 | Route change | Route changes trigger component |

---

## Performance Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| BLAZOR-FULL-093 | Page load | Page loads within 2 seconds |
| BLAZOR-FULL-094 | Form render | Form renders in under 1 second |
| BLAZOR-FULL-095 | Table virtualize | Smooth scroll with 1000+ items |
| BLAZOR-FULL-096 | Memory leak | No memory leak on navigation |
| BLAZOR-FULL-097 | CPU usage | Idle uses minimal CPU |
| BLAZOR-FULL-098 | Network usage | Minimal network requests |

---

## Accessibility Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| BLAZOR-FULL-099 | Screen reader | Controls have labels |
| BLAZOR-FULL-100 | Tab order | Tab navigates logically |
| BLAZOR-FULL-101 | Focus visible | Focus state clear |
| BLAZOR-FULL-102 | Color contrast | Text meets WCAG AA |
| BLAZOR-FULL-103 | Keyboard only | All features accessible via keyboard |

---

## Error Handling Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| BLAZOR-FULL-104 | Network timeout | Shows timeout error |
| BLAZOR-FULL-105 | Server error | Shows server error |
| BLAZOR-FULL-106 | Invalid data | Shows validation error |
| BLAZOR-FULL-107 | App recovery | App recovers from error |
| BLAZOR-FULL-108 | Permission denied | Shows permission error |

---

## Browser Compatibility Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| BLAZOR-FULL-109 | Chrome | Works correctly in Chrome |
| BLAZOR-FULL-110 | Firefox | Works correctly in Firefox |
| BLAZOR-FULL-111 | Edge | Works correctly in Edge |
| BLAZOR-FULL-112 | Safari | Works correctly in Safari |

---

## Summary

- **Total Tests:** 112
- **Coverage:** All functionality, edge cases, errors
- **Execution Time:** ~2-3 hours
- **Focus:** Comprehensive coverage

