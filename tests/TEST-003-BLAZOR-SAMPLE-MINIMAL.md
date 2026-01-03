# TEST-003: Blazor Sample App - Minimal Test Set

**Version:** 1.0  
**Status:** Design  
**Date:** January 2026

---

## Overview

Minimal test coverage for Blazor sample app validating core functionality of each page type.

---

## Dashboard Page Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| BLAZOR-MIN-001 | Page loads | Dashboard displays KPI cards |
| BLAZOR-MIN-002 | Dynamic component | StatusIndicator component renders |
| BLAZOR-MIN-003 | Navigation link | NavLink to Form page navigates |
| BLAZOR-MIN-004 | Navigation link | NavLink to Data Table navigates |

---

## User Form Page Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| BLAZOR-MIN-005 | Text input | InputText accepts and displays text |
| BLAZOR-MIN-006 | Select control | InputSelect shows options and selection updates |
| BLAZOR-MIN-007 | Date input | InputDate selects and displays date |
| BLAZOR-MIN-008 | Checkbox | InputCheckbox toggles value |
| BLAZOR-MIN-009 | Radio button | InputRadioGroup selects one option |
| BLAZOR-MIN-010 | Form submission | EditForm submits with valid data |
| BLAZOR-MIN-011 | Form validation | ValidationSummary shows errors |
| BLAZOR-MIN-012 | Validation message | ValidationMessage displays field error |

---

## Data Table Page Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| BLAZOR-MIN-013 | Table displays | Virtualize renders list of items |
| BLAZOR-MIN-014 | Search filter | InputText filters results |
| BLAZOR-MIN-015 | Sorting | Select dropdown sorts by column |
| BLAZOR-MIN-016 | Pagination | Previous/Next buttons navigate pages |

---

## File Upload Page Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| BLAZOR-MIN-017 | File input | InputFile opens file picker |
| BLAZOR-MIN-018 | File select | Selected file displays |
| BLAZOR-MIN-019 | Upload button | Upload initiates |
| BLAZOR-MIN-020 | File list | Uploaded file appears in list |

---

## Navigation Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| BLAZOR-MIN-021 | NavMenu renders | Navigation menu displays all links |
| BLAZOR-MIN-022 | NavLink active | Active link highlighted correctly |
| BLAZOR-MIN-023 | Page routing | Navigation routes to correct page |

---

## Advanced Features Tests

| Test ID | Scenario | Validation |
|---------|----------|-----------|
| BLAZOR-MIN-024 | Error boundary | Error displays in error content |
| BLAZOR-MIN-025 | Dynamic component | DynamicComponent loads correctly |

---

## Summary

- **Total Tests:** 25
- **Coverage:** Core functionality per page
- **Execution Time:** ~5-10 minutes
- **Focus:** Happy path scenarios

